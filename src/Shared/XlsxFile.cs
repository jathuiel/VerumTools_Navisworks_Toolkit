using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;

using NavisworksToolkit.Core;
using NavisworksToolkit.Shared;

namespace NavisworksToolkit.Shared
{
    /// <summary>
    /// Leitor e escritor MÍNIMOS de arquivos .xlsx (Office Open XML), sem dependências
    /// externas — um .xlsx é apenas um ZIP de XML, então usamos System.IO.Compression
    /// (já presente no .NET Framework 4.8). Cobre o necessário para o template:
    ///  - Leitura: primeira planilha (ou uma planilha por nome), shared strings e inline
    ///    strings, células esparsas (resolvidas pela referência tipo "B3").
    ///  - Escrita: múltiplas abas com strings inline, cabeçalho em negrito e larguras de
    ///    coluna. Não escreve números/fórmulas (não é preciso para o template de texto).
    /// </summary>
    internal static class XlsxFile
    {
        private static readonly XNamespace Main =
            "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        private static readonly XNamespace RelOffice =
            "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        private static readonly XNamespace PkgRels =
            "http://schemas.openxmlformats.org/package/2006/relationships";
        private static readonly XNamespace ContentTypes =
            "http://schemas.openxmlformats.org/package/2006/content-types";

        private const string TypeWorksheet =
            "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet";
        private const string TypeStyles =
            "http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles";
        private const string TypeOfficeDoc =
            "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument";

        // ===================== Modelo de aba (escrita) =====================

        public sealed class Sheet
        {
            public string Name;
            public readonly List<string[]> Rows = new List<string[]>();
            /// <summary>Larguras de coluna (em "caracteres" do Excel); opcional.</summary>
            public double[] ColumnWidths;
            /// <summary>Quantidade de linhas iniciais a renderizar em negrito (cabeçalho).</summary>
            public int HeaderRows;
        }

        // ===================== Leitura =====================

        /// <summary>
        /// Lê uma planilha como matriz de strings (linhas × colunas). Tenta a planilha
        /// <paramref name="preferredSheetName"/> (case-insensitive); se não existir, usa a
        /// primeira. Células vazias viram string vazia.
        /// </summary>
        public static List<string[]> ReadSheet(string path, string preferredSheetName = null)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Caminho do arquivo vazio.", nameof(path));
            if (!File.Exists(path))
                throw new FileNotFoundException("Arquivo não encontrado.", path);

            using (var fs = File.OpenRead(path))
            using (var zip = new ZipArchive(fs, ZipArchiveMode.Read))
            {
                var shared = ReadSharedStrings(zip);
                var sheetPath = ResolveSheetPath(zip, preferredSheetName);

                var entry = zip.GetEntry(sheetPath);
                if (entry == null)
                    throw new InvalidOperationException(
                        $"Planilha não encontrada dentro do arquivo ({sheetPath}). O arquivo é um .xlsx válido?");

                XDocument doc;
                using (var s = entry.Open())
                    doc = XDocument.Load(s);

                var rows = new List<string[]>();
                var sheetData = doc.Root?.Element(Main + "sheetData");
                if (sheetData == null)
                    return rows;

                foreach (var rowEl in sheetData.Elements(Main + "row"))
                {
                    var values = new Dictionary<int, string>();
                    var maxCol = -1;
                    var seq = 0;
                    foreach (var c in rowEl.Elements(Main + "c"))
                    {
                        var refAttr = (string)c.Attribute("r");
                        var col = refAttr != null ? ColumnIndex(refAttr) : seq;
                        seq = col + 1;
                        if (col > maxCol) maxCol = col;
                        values[col] = ReadCellValue(c, shared);
                    }

                    var arr = new string[maxCol + 1];
                    for (var i = 0; i <= maxCol; i++)
                        arr[i] = values.TryGetValue(i, out var v) ? v : string.Empty;
                    rows.Add(arr);
                }

                return rows;
            }
        }

        private static string ReadCellValue(XElement c, IReadOnlyList<string> shared)
        {
            var t = (string)c.Attribute("t");

            if (t == "inlineStr")
            {
                var isEl = c.Element(Main + "is");
                return isEl == null ? string.Empty : ReadText(isEl);
            }

            var v = c.Element(Main + "v");
            if (t == "s")
            {
                if (v != null && int.TryParse(v.Value, out var idx) && idx >= 0 && idx < shared.Count)
                    return shared[idx];
                return string.Empty;
            }

            // "str" (string de fórmula), número, booleano: valor cru.
            return v?.Value ?? string.Empty;
        }

        private static List<string> ReadSharedStrings(ZipArchive zip)
        {
            var list = new List<string>();
            var entry = zip.GetEntry("xl/sharedStrings.xml");
            if (entry == null)
                return list;

            XDocument doc;
            using (var s = entry.Open())
                doc = XDocument.Load(s);

            if (doc.Root != null)
                foreach (var si in doc.Root.Elements(Main + "si"))
                    list.Add(ReadText(si));

            return list;
        }

        /// <summary>Texto de um &lt;si&gt;/&lt;is&gt;: &lt;t&gt; simples ou runs &lt;r&gt;&lt;t&gt;.</summary>
        private static string ReadText(XElement parent)
        {
            var t = parent.Element(Main + "t");
            if (t != null)
                return t.Value;

            var sb = new StringBuilder();
            foreach (var run in parent.Elements(Main + "r"))
            {
                var rt = run.Element(Main + "t");
                if (rt != null)
                    sb.Append(rt.Value);
            }
            return sb.ToString();
        }

        private static string ResolveSheetPath(ZipArchive zip, string preferredSheetName)
        {
            const string fallback = "xl/worksheets/sheet1.xml";

            var wbEntry = zip.GetEntry("xl/workbook.xml");
            if (wbEntry == null)
                return fallback;

            XDocument wb;
            using (var s = wbEntry.Open())
                wb = XDocument.Load(s);

            var sheets = wb.Root?.Element(Main + "sheets")?.Elements(Main + "sheet").ToList();
            if (sheets == null || sheets.Count == 0)
                return fallback;

            XElement chosen = null;
            if (!string.IsNullOrWhiteSpace(preferredSheetName))
                chosen = sheets.FirstOrDefault(sh =>
                    string.Equals((string)sh.Attribute("name"), preferredSheetName,
                        StringComparison.OrdinalIgnoreCase));
            if (chosen == null)
                chosen = sheets[0];

            var rid = (string)chosen.Attribute(RelOffice + "id");
            var relsEntry = zip.GetEntry("xl/_rels/workbook.xml.rels");
            if (relsEntry != null && !string.IsNullOrEmpty(rid))
            {
                XDocument rels;
                using (var s = relsEntry.Open())
                    rels = XDocument.Load(s);

                var rel = rels.Root?.Elements(PkgRels + "Relationship")
                    .FirstOrDefault(r => (string)r.Attribute("Id") == rid);
                var target = (string)rel?.Attribute("Target");
                if (!string.IsNullOrWhiteSpace(target))
                {
                    target = target.Replace('\\', '/').TrimStart('/');
                    return target.StartsWith("xl/", StringComparison.OrdinalIgnoreCase)
                        ? target
                        : "xl/" + target; // alvos do workbook são relativos a xl/
                }
            }

            return fallback;
        }

        /// <summary>Converte a parte alfabética de "B3"/"AA10" em índice 0-based de coluna.</summary>
        private static int ColumnIndex(string cellRef)
        {
            var col = 0;
            foreach (var ch in cellRef)
            {
                if (ch >= 'A' && ch <= 'Z') col = col * 26 + (ch - 'A' + 1);
                else if (ch >= 'a' && ch <= 'z') col = col * 26 + (ch - 'a' + 1);
                else break; // chegou nos dígitos
            }
            return col - 1;
        }

        // ===================== Escrita =====================

        public static void Write(string path, IList<Sheet> sheets)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Caminho do arquivo vazio.", nameof(path));
            if (sheets == null || sheets.Count == 0)
                throw new ArgumentException("Nenhuma aba para escrever.", nameof(sheets));

            using (var fs = File.Create(path))
            using (var zip = new ZipArchive(fs, ZipArchiveMode.Create))
            {
                AddEntry(zip, "[Content_Types].xml", BuildContentTypes(sheets.Count));
                AddEntry(zip, "_rels/.rels", BuildRootRels());
                AddEntry(zip, "xl/workbook.xml", BuildWorkbook(sheets));
                AddEntry(zip, "xl/_rels/workbook.xml.rels", BuildWorkbookRels(sheets.Count));
                AddEntry(zip, "xl/styles.xml", BuildStyles());
                for (var i = 0; i < sheets.Count; i++)
                    AddEntry(zip, $"xl/worksheets/sheet{i + 1}.xml", BuildSheet(sheets[i]));
            }
        }

        private static void AddEntry(ZipArchive zip, string name, XDocument doc)
        {
            var entry = zip.CreateEntry(name);
            using (var s = entry.Open())
                doc.Save(s);
        }

        private static XDocument BuildContentTypes(int sheetCount)
        {
            var types = new XElement(ContentTypes + "Types",
                new XElement(ContentTypes + "Default",
                    new XAttribute("Extension", "rels"),
                    new XAttribute("ContentType", "application/vnd.openxmlformats-package.relationships+xml")),
                new XElement(ContentTypes + "Default",
                    new XAttribute("Extension", "xml"),
                    new XAttribute("ContentType", "application/xml")),
                new XElement(ContentTypes + "Override",
                    new XAttribute("PartName", "/xl/workbook.xml"),
                    new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml")),
                new XElement(ContentTypes + "Override",
                    new XAttribute("PartName", "/xl/styles.xml"),
                    new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml")));

            for (var i = 0; i < sheetCount; i++)
                types.Add(new XElement(ContentTypes + "Override",
                    new XAttribute("PartName", $"/xl/worksheets/sheet{i + 1}.xml"),
                    new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml")));

            return new XDocument(types);
        }

        private static XDocument BuildRootRels()
        {
            return new XDocument(new XElement(PkgRels + "Relationships",
                new XElement(PkgRels + "Relationship",
                    new XAttribute("Id", "rId1"),
                    new XAttribute("Type", TypeOfficeDoc),
                    new XAttribute("Target", "xl/workbook.xml"))));
        }

        private static XDocument BuildWorkbook(IList<Sheet> sheets)
        {
            var sheetsEl = new XElement(Main + "sheets");
            for (var i = 0; i < sheets.Count; i++)
                sheetsEl.Add(new XElement(Main + "sheet",
                    new XAttribute("name", SafeSheetName(sheets[i].Name, i)),
                    new XAttribute("sheetId", i + 1),
                    new XAttribute(RelOffice + "id", "rId" + (i + 1))));

            return new XDocument(new XElement(Main + "workbook",
                new XAttribute(XNamespace.Xmlns + "r", RelOffice.NamespaceName),
                sheetsEl));
        }

        private static XDocument BuildWorkbookRels(int sheetCount)
        {
            var rels = new XElement(PkgRels + "Relationships");
            for (var i = 0; i < sheetCount; i++)
                rels.Add(new XElement(PkgRels + "Relationship",
                    new XAttribute("Id", "rId" + (i + 1)),
                    new XAttribute("Type", TypeWorksheet),
                    new XAttribute("Target", $"worksheets/sheet{i + 1}.xml")));

            rels.Add(new XElement(PkgRels + "Relationship",
                new XAttribute("Id", "rId" + (sheetCount + 1)),
                new XAttribute("Type", TypeStyles),
                new XAttribute("Target", "styles.xml")));

            return new XDocument(rels);
        }

        private static XDocument BuildStyles()
        {
            return new XDocument(new XElement(Main + "styleSheet",
                new XElement(Main + "fonts", new XAttribute("count", 2),
                    new XElement(Main + "font"),
                    new XElement(Main + "font", new XElement(Main + "b"))),
                new XElement(Main + "fills", new XAttribute("count", 2),
                    new XElement(Main + "fill", new XElement(Main + "patternFill", new XAttribute("patternType", "none"))),
                    new XElement(Main + "fill", new XElement(Main + "patternFill", new XAttribute("patternType", "gray125")))),
                new XElement(Main + "borders", new XAttribute("count", 1), new XElement(Main + "border")),
                new XElement(Main + "cellStyleXfs", new XAttribute("count", 1),
                    new XElement(Main + "xf",
                        new XAttribute("numFmtId", 0), new XAttribute("fontId", 0),
                        new XAttribute("fillId", 0), new XAttribute("borderId", 0))),
                new XElement(Main + "cellXfs", new XAttribute("count", 2),
                    new XElement(Main + "xf",
                        new XAttribute("numFmtId", 0), new XAttribute("fontId", 0),
                        new XAttribute("fillId", 0), new XAttribute("borderId", 0), new XAttribute("xfId", 0)),
                    new XElement(Main + "xf",
                        new XAttribute("numFmtId", 0), new XAttribute("fontId", 1),
                        new XAttribute("fillId", 0), new XAttribute("borderId", 0), new XAttribute("xfId", 0),
                        new XAttribute("applyFont", 1)))));
        }

        private static XDocument BuildSheet(Sheet sheet)
        {
            var ws = new XElement(Main + "worksheet");

            if (sheet.ColumnWidths != null && sheet.ColumnWidths.Length > 0)
            {
                var cols = new XElement(Main + "cols");
                for (var i = 0; i < sheet.ColumnWidths.Length; i++)
                    cols.Add(new XElement(Main + "col",
                        new XAttribute("min", i + 1),
                        new XAttribute("max", i + 1),
                        new XAttribute("width", sheet.ColumnWidths[i]),
                        new XAttribute("customWidth", 1)));
                ws.Add(cols);
            }

            var sheetData = new XElement(Main + "sheetData");
            for (var r = 0; r < sheet.Rows.Count; r++)
            {
                var rowEl = new XElement(Main + "row", new XAttribute("r", r + 1));
                var cells = sheet.Rows[r] ?? new string[0];
                var bold = r < sheet.HeaderRows;

                for (var c = 0; c < cells.Length; c++)
                {
                    var val = cells[c];
                    if (string.IsNullOrEmpty(val))
                        continue; // célula esparsa: omitir vazias é válido

                    var cell = new XElement(Main + "c",
                        new XAttribute("r", ColumnLetter(c) + (r + 1)),
                        new XAttribute("t", "inlineStr"));
                    if (bold)
                        cell.Add(new XAttribute("s", 1));
                    cell.Add(new XElement(Main + "is",
                        new XElement(Main + "t",
                            new XAttribute(XNamespace.Xml + "space", "preserve"), val)));
                    rowEl.Add(cell);
                }

                sheetData.Add(rowEl);
            }

            ws.Add(sheetData);
            return new XDocument(ws);
        }

        /// <summary>Índice 0-based → letra(s) de coluna do Excel (0→A, 26→AA).</summary>
        private static string ColumnLetter(int index)
        {
            index++;
            var sb = new StringBuilder();
            while (index > 0)
            {
                var rem = (index - 1) % 26;
                sb.Insert(0, (char)('A' + rem));
                index = (index - 1) / 26;
            }
            return sb.ToString();
        }

        // Excel: nome de aba até 31 chars e sem : \ / ? * [ ].
        private static string SafeSheetName(string name, int index)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "Sheet" + (index + 1);

            var cleaned = new string(name.Where(ch => ":\\/?*[]".IndexOf(ch) < 0).ToArray());
            if (cleaned.Length > 31)
                cleaned = cleaned.Substring(0, 31);
            return string.IsNullOrWhiteSpace(cleaned) ? "Sheet" + (index + 1) : cleaned;
        }
    }
}
