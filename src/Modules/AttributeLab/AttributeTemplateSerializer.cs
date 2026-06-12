using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;

using NavisworksToolkit.Core;
using NavisworksToolkit.Shared;

namespace NavisworksToolkit.Modules.AttributeLab
{
    // Lê e grava templates de atributos em CSV e XML.
    // Formato CSV: Categoria,Nome,Valor,Tipo (linha de cabeçalho opcional)
    // Formato XML: DataSet com schema — gerado via DataSet.WriteXml
    internal static class AttributeTemplateSerializer
    {
        public static (List<AttributeEntry> entries, string categoria) ImportCsv(string path)
        {
            var entries = new List<AttributeEntry>();
            string categoria = null;

            var lines = File.ReadAllLines(path, Encoding.UTF8);
            if (lines.Length == 0) return (entries, categoria);

            int start = lines[0].StartsWith("Categoria", System.StringComparison.OrdinalIgnoreCase) ? 1 : 0;

            for (int i = start; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                var parts = ParseCsvLine(lines[i]);
                if (parts.Count < 4) continue;

                if (string.IsNullOrWhiteSpace(categoria) && !string.IsNullOrWhiteSpace(parts[0]))
                    categoria = parts[0].Trim();

                entries.Add(new AttributeEntry
                {
                    Name = parts[1],
                    Value = parts[2],
                    Type = string.IsNullOrWhiteSpace(parts[3]) ? "string" : parts[3]
                });
            }

            return (entries, categoria);
        }

        public static (List<AttributeEntry> entries, string categoria) ImportXml(string path)
        {
            var entries = new List<AttributeEntry>();
            string categoria = null;

            var ds = new DataSet();
            ds.ReadXml(path);
            if (ds.Tables.Count == 0) return (entries, categoria);

            var dt = ds.Tables[0];
            foreach (DataRow row in dt.Rows)
            {
                var nome = dt.Columns.Contains("Nome") ? row["Nome"]?.ToString() : string.Empty;
                if (string.IsNullOrWhiteSpace(nome)) continue;

                if (string.IsNullOrWhiteSpace(categoria) && dt.Columns.Contains("Categoria"))
                    categoria = row["Categoria"]?.ToString();

                entries.Add(new AttributeEntry
                {
                    Name = nome,
                    Value = dt.Columns.Contains("Valor") ? row["Valor"]?.ToString() : string.Empty,
                    Type = dt.Columns.Contains("Tipo") ? (row["Tipo"]?.ToString() ?? "string") : "string"
                });
            }

            return (entries, categoria);
        }

        public static void ExportCsv(string path, string categoria, IEnumerable<AttributeEntry> entries)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Categoria,Nome,Valor,Tipo");

            foreach (var entry in entries)
            {
                var nome = entry.Name?.Trim();
                if (string.IsNullOrWhiteSpace(nome)) continue;

                sb.AppendLine(string.Join(",",
                    EscapeCsv(categoria),
                    EscapeCsv(nome),
                    EscapeCsv(entry.Value ?? string.Empty),
                    EscapeCsv(entry.Type ?? "string")));
            }

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }

        public static void ExportXml(string path, string categoria, IEnumerable<AttributeEntry> entries)
        {
            var ds = new DataSet("VerumAtributos");
            var dt = new DataTable("Atributo");
            dt.Columns.Add("Categoria", typeof(string));
            dt.Columns.Add("Nome", typeof(string));
            dt.Columns.Add("Valor", typeof(string));
            dt.Columns.Add("Tipo", typeof(string));

            foreach (var entry in entries)
            {
                var nome = entry.Name?.Trim();
                if (string.IsNullOrWhiteSpace(nome)) continue;

                var row = dt.NewRow();
                row["Categoria"] = categoria;
                row["Nome"] = nome;
                row["Valor"] = entry.Value ?? string.Empty;
                row["Tipo"] = entry.Type ?? "string";
                dt.Rows.Add(row);
            }

            ds.Tables.Add(dt);
            ds.WriteXml(path, XmlWriteMode.WriteSchema);
        }

        private static string EscapeCsv(string value)
        {
            value = value ?? string.Empty;
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            return value;
        }

        private static List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            bool inQuotes = false;
            var current = new StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (inQuotes)
                {
                    if (c == '"' && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else if (c == '"')
                    {
                        inQuotes = false;
                    }
                    else
                    {
                        current.Append(c);
                    }
                }
                else
                {
                    if (c == '"')
                        inQuotes = true;
                    else if (c == ',')
                    {
                        result.Add(current.ToString());
                        current.Clear();
                    }
                    else
                        current.Append(c);
                }
            }

            result.Add(current.ToString());
            return result;
        }
    }
}
