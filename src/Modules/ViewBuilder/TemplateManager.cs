using System;
using System.Collections.Generic;
using System.Linq;

using NavisworksToolkit.Core;
using NavisworksToolkit.Shared;

namespace NavisworksToolkit.Modules.ViewBuilder
{
    /// <summary>
    /// Gera o template Excel (modelo amigável) e importa as linhas de volta. Cada linha
    /// representa um viewpoint a criar a partir de um Selection Set existente.
    /// </summary>
    public static class TemplateManager
    {
        /// <summary>Aba de dados que o usuário preenche / que importamos.</summary>
        public const string DataSheetName = "Viewpoints";

        /// <summary>Cabeçalhos das colunas (ordem fixa: Set, Nome do VP, Descrição).</summary>
        public static readonly string[] Headers = { "SelectionSet", "NomeDoViewpoint", "Descricao" };

        private const string HelpSheetName = "Instruções";

        /// <summary>
        /// Resultado de uma importação: as linhas válidas e eventuais avisos (arquivo vazio,
        /// sem dados, etc.). Não inclui o casamento com os sets do modelo — isso é feito na UI.
        /// </summary>
        public class ImportResult
        {
            public List<ViewpointTemplateRow> Rows { get; } = new List<ViewpointTemplateRow>();
            public List<string> Warnings { get; } = new List<string>();
        }

        /// <summary>
        /// Escreve um template .xlsx amigável: aba "Viewpoints" (cabeçalho em negrito +
        /// exemplos usando nomes REAIS de sets existentes, quando disponíveis) e uma aba
        /// "Instruções" com o passo a passo.
        /// </summary>
        public static void GenerateTemplate(string path, IEnumerable<SelectionSetData> existingSets)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Caminho do arquivo vazio.", nameof(path));

            var data = new XlsxFile.Sheet
            {
                Name = DataSheetName,
                HeaderRows = 1,
                ColumnWidths = new double[] { 40, 34, 50 }
            };
            data.Rows.Add(Headers);

            // Exemplos com nomes reais (até 3) para o usuário ver o formato esperado.
            var examples = (existingSets ?? Enumerable.Empty<SelectionSetData>())
                .Where(s => s != null && !string.IsNullOrWhiteSpace(s.Name))
                .Select(s => s.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(3)
                .ToList();

            if (examples.Count == 0)
                examples = new List<string> { "NOME-DO-SELECTION-SET-1", "NOME-DO-SELECTION-SET-2" };

            foreach (var name in examples)
                data.Rows.Add(new[] { name, "", "" });

            var help = new XlsxFile.Sheet
            {
                Name = HelpSheetName,
                HeaderRows = 1,
                ColumnWidths = new double[] { 95 }
            };
            help.Rows.Add(new[] { "Como usar este template — ViewBuilder" });
            help.Rows.Add(new[] { "1) Na aba 'Viewpoints', preencha UMA LINHA por viewpoint que deseja criar." });
            help.Rows.Add(new[] { "2) Coluna 'SelectionSet' (OBRIGATÓRIA): o nome EXATO de um Selection Set existente no modelo." });
            help.Rows.Add(new[] { "3) Coluna 'NomeDoViewpoint' (opcional): se deixar em branco, o nome é gerado automaticamente." });
            help.Rows.Add(new[] { "4) Coluna 'Descricao' (opcional): texto livre associado ao viewpoint." });
            help.Rows.Add(new[] { "5) Salve o arquivo e use o botão 'Importar template (.xlsx)' no ViewBuilder." });
            help.Rows.Add(new[] { "6) Os sets que casarem pelo nome serão marcados na lista; depois clique em 'Criar Viewpoints'." });
            help.Rows.Add(new[] { "" });
            help.Rows.Add(new[] { "Observações: maiúsculas/minúsculas e espaços nas pontas são ignorados ao casar os nomes." });
            help.Rows.Add(new[] { "Se o mesmo set aparecer em mais de uma linha, é criado apenas 1 viewpoint para ele." });
            help.Rows.Add(new[] { "Não renomeie nem reordene as colunas da aba 'Viewpoints'." });

            XlsxFile.Write(path, new[] { data, help });
        }

        /// <summary>
        /// Lê o template e extrai as linhas válidas (com SelectionSet preenchido). Pula a
        /// linha de cabeçalho e linhas em branco. Não falha se houver colunas a mais.
        /// </summary>
        public static ImportResult Import(string path)
        {
            var result = new ImportResult();
            var rows = XlsxFile.ReadSheet(path, DataSheetName);

            if (rows.Count == 0)
            {
                result.Warnings.Add("O arquivo está vazio ou não tem a aba de dados.");
                return result;
            }

            // Pula o cabeçalho se a 1ª célula da 1ª linha for o cabeçalho conhecido.
            var start = 0;
            if (rows[0].Length > 0 &&
                Normalize(rows[0][0]).Equals("selectionset", StringComparison.OrdinalIgnoreCase))
                start = 1;

            for (var i = start; i < rows.Count; i++)
            {
                var r = rows[i];
                var set = Cell(r, 0);
                if (string.IsNullOrWhiteSpace(set))
                    continue; // linha em branco

                result.Rows.Add(new ViewpointTemplateRow
                {
                    RowNumber = i + 1,
                    SelectionSetName = set.Trim(),
                    ViewpointName = NullIfBlank(Cell(r, 1)),
                    Description = NullIfBlank(Cell(r, 2))
                });
            }

            if (result.Rows.Count == 0)
                result.Warnings.Add("Nenhuma linha de dados encontrada — preencha a coluna 'SelectionSet'.");

            return result;
        }

        private static string Cell(string[] row, int index) =>
            row != null && index < row.Length ? row[index] : null;

        private static string NullIfBlank(string s) =>
            string.IsNullOrWhiteSpace(s) ? null : s.Trim();

        private static string Normalize(string s) =>
            (s ?? string.Empty).Trim().Replace(" ", string.Empty).Replace("_", string.Empty);
    }
}
