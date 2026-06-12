using Autodesk.Navisworks.Api;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;
using WpfColor = System.Windows.Media.Color;
using WpfColorConverter = System.Windows.Media.ColorConverter;
using WpfColors = System.Windows.Media.Colors;
using NwApp = Autodesk.Navisworks.Api.Application;
using NwColor = Autodesk.Navisworks.Api.Color;

using NavisworksToolkit.Core;
using NavisworksToolkit.Shared;

namespace NavisworksToolkit.Modules.SetColoring
{
    public class SetColoringService
    {
        // A API de override de aparência vive em DocumentModels (doc.Models) e é
        // descoberta em runtime porque a superfície varia entre versões/SKUs.
        // Cor e transparência são métodos separados (não há objeto "appearance");
        // os candidatos "Temporary" são fallback caso o override permanente não exista.
        private static readonly string[] ColorMethodNames =
            { "OverridePermanentColor", "OverrideTemporaryColor" };

        private static readonly string[] TransparencyMethodNames =
            { "OverridePermanentTransparency", "OverrideTemporaryTransparency" };

        private static readonly string[] ResetMethodNames =
            { "ResetAllPermanentMaterials", "ResetAllTemporaryMaterials" };

        public void SaveToXml(IEnumerable<ColoringRule> rules, string filePath)
        {
            var doc = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement("ColoringRules",
                    new XAttribute("version", "1.0"),
                    rules.Select(r => new XElement("Rule",
                        new XAttribute("SelectionSet", r.SelectionSetName),
                        new XAttribute("Color", r.HexColor),
                        new XAttribute("Transparency", r.Transparency),
                        new XAttribute("Enabled", r.IsEnabled.ToString().ToLowerInvariant())
                    ))
                )
            );
            doc.Save(filePath);
        }

        public List<ColoringRule> LoadFromXml(string filePath)
        {
            var result = new List<ColoringRule>();
            var xdoc = XDocument.Load(filePath);
            var root = xdoc.Root;
            if (root == null || root.Name.LocalName != "ColoringRules")
                throw new InvalidOperationException("Arquivo XML invalido: elemento raiz 'ColoringRules' nao encontrado.");

            foreach (var el in root.Elements("Rule"))
            {
                var rule = new ColoringRule
                {
                    SelectionSetName = (string)el.Attribute("SelectionSet") ?? string.Empty,
                    HexColor = (string)el.Attribute("Color") ?? "#FF0000",
                };

                if (int.TryParse((string)el.Attribute("Transparency"), out int t))
                    rule.Transparency = t;

                if (bool.TryParse((string)el.Attribute("Enabled"), out bool en))
                    rule.IsEnabled = en;

                result.Add(rule);
            }

            return result;
        }

        public Task<(int applied, int skipped, int errors)> ApplyColoringAsync(
            IEnumerable<ColoringRule> rules, Action<string> log)
        {
            var ruleList = rules?.ToList() ?? new List<ColoringRule>();

            var doc = NwApp.ActiveDocument;
            if (doc == null)
            {
                log?.Invoke("[ERRO] Nenhum documento ativo.");
                return Task.FromResult((0, 0, 1));
            }

            // A API do Navisworks não é thread-safe e deve ser usada na thread
            // principal — todo o trabalho aqui é chamada de API, então roda síncrono.
            int applied = 0, skipped = 0, errors = 0;

            var setMap = BuildSelectionSetMap(doc);
            var modelsObj = doc.Models;
            var modelsType = modelsObj.GetType();

            var colorMethod = FindFirstMethod(modelsType, ColorMethodNames);
            var transparencyMethod = FindFirstMethod(modelsType, TransparencyMethodNames);

            if (colorMethod == null)
            {
                log?.Invoke("[ERRO] API de substituicao de cor nao encontrada nesta versao do Navisworks.");
                log?.Invoke($"[DICA] Metodos procurados em {modelsType.FullName}: {string.Join(", ", ColorMethodNames)}");
                return Task.FromResult((0, 0, 1));
            }

            foreach (var rule in ruleList.Where(r => r.IsEnabled))
            {
                if (!setMap.TryGetValue(rule.SelectionSetName, out var items) || items == null || items.IsEmpty)
                {
                    log?.Invoke($"[AVISO] Set '{rule.SelectionSetName}' nao encontrado ou vazio. Pulando.");
                    skipped++;
                    continue;
                }

                try
                {
                    WpfColor wpfColor = ParseHexColor(rule.HexColor);
                    var nwColor = new NwColor(wpfColor.R / 255.0, wpfColor.G / 255.0, wpfColor.B / 255.0);

                    colorMethod.Invoke(modelsObj, new object[] { items, nwColor });
                    // 0.0 = opaco, 1.0 = invisível; aplicar 0 também remove transparência anterior.
                    transparencyMethod?.Invoke(modelsObj, new object[] { items, rule.Transparency / 100.0 });

                    log?.Invoke($"[OK] '{rule.SelectionSetName}' -> {rule.HexColor} transp={rule.Transparency}% ({items.Count} elemento(s))");
                    applied++;
                }
                catch (Exception ex)
                {
                    log?.Invoke($"[ERRO] '{rule.SelectionSetName}': {ExceptionHelper.UnwrapMessage(ex)}");
                    errors++;
                }
            }

            return Task.FromResult((applied, skipped, errors));
        }

        public Task<string> RemoveColoringAsync(Action<string> log)
        {
            var doc = NwApp.ActiveDocument;
            if (doc == null) return Task.FromResult("Nenhum documento ativo.");

            var modelsObj = doc.Models;
            var modelsType = modelsObj.GetType();

            var resetMethod = FindFirstMethod(modelsType, ResetMethodNames);
            if (resetMethod == null)
            {
                var msg = $"API de remocao de cor nao encontrada. Procurados: {string.Join(", ", ResetMethodNames)}";
                log?.Invoke($"[ERRO] {msg}");
                return Task.FromResult(msg);
            }

            try
            {
                resetMethod.Invoke(modelsObj, null);
                log?.Invoke("[OK] Todas as substituicoes de cor foram removidas.");
                return Task.FromResult("Cores removidas com sucesso.");
            }
            catch (Exception ex)
            {
                var message = ExceptionHelper.UnwrapMessage(ex);
                log?.Invoke($"[ERRO] {message}");
                return Task.FromResult($"Erro ao remover cores: {message}");
            }
        }

        private static MethodInfo FindFirstMethod(Type type, string[] candidateNames)
        {
            foreach (var name in candidateNames)
            {
                var method = type.GetMethod(name);
                if (method != null) return method;
            }
            return null;
        }

        public ObservableCollection<SelectionSetItem> LoadSelectionSets()
        {
            var doc = NwApp.ActiveDocument;
            if (doc == null) return new ObservableCollection<SelectionSetItem>();

            var sets = PropertyExtractionHelper.LoadSelectionSetsViaReflection(doc);
            if (doc.CurrentSelection == null || doc.CurrentSelection.IsEmpty)
                return sets;

            var selectedItems = new HashSet<ModelItem>(doc.CurrentSelection.SelectedItems.Cast<ModelItem>());
            foreach (var set in sets)
            {
                if (set?.Items == null) continue;
                set.IsSelected = set.Items.Any(i => i != null && selectedItems.Contains(i));
            }

            return sets;
        }

        private Dictionary<string, ModelItemCollection> BuildSelectionSetMap(Document doc)
        {
            var map = new Dictionary<string, ModelItemCollection>(StringComparer.OrdinalIgnoreCase);
            var sets = PropertyExtractionHelper.LoadSelectionSetsViaReflection(doc);
            foreach (var s in sets)
            {
                var col = new ModelItemCollection();
                foreach (var item in s.Items)
                    col.Add(item);
                map[s.Name] = col;
            }
            return map;
        }

        private static WpfColor ParseHexColor(string hex)
        {
            try
            {
                return (WpfColor)WpfColorConverter.ConvertFromString(hex);
            }
            catch
            {
                return WpfColors.Red;
            }
        }

    }
}
