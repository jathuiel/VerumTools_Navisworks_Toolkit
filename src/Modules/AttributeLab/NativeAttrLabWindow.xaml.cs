using Autodesk.Navisworks.Api;
using Autodesk.Navisworks.Api.Interop.ComApi;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NwApp = Autodesk.Navisworks.Api.Application;

using NavisworksToolkit.Core;
using NavisworksToolkit.Shared;

namespace NavisworksToolkit.Modules.AttributeLab
{
    public partial class NativeAttrLabWindow : Window
    {
        private readonly ObservableCollection<LabCatNode> _nativeCats = new ObservableCollection<LabCatNode>();
        private readonly ObservableCollection<AttributeEntry> _labEntries = new ObservableCollection<AttributeEntry>();
        private readonly ObservableCollection<SelectionSetItem> _detectedSets = new ObservableCollection<SelectionSetItem>();

        private List<ModelItem> _selectedItems = new List<ModelItem>();
        private Dictionary<string, Dictionary<string, string>> _nativeCatMap =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<ModelItem, List<SetAssignment>> _allSetsByItem =
            new Dictionary<ModelItem, List<SetAssignment>>();

        public NativeAttrLabWindow()
        {
            InitializeComponent();
            UiCommon.ApplyBranding(this, PartnersLogo);
            NativeTree.ItemsSource = _nativeCats;
            LabGrid.ItemsSource = _labEntries;
            SelectionSetsGrid.ItemsSource = _detectedSets;

            Loaded += async (s, e) =>
            {
                await ReloadNativeAsync();
                Activate();
                Topmost = true;
                Topmost = false;
            };
        }

        private async Task ReloadNativeAsync()
        {
            _nativeCats.Clear();
            _nativeCatMap.Clear();
            NativeCategoryCombo.Items.Clear();
            ResetDetectedSetsCollection();
            _allSetsByItem.Clear();
            _selectedItems.Clear();
            FooterStatus.Text = "Carregando...";

            var doc = NwApp.ActiveDocument;
            if (doc == null || doc.CurrentSelection.IsEmpty)
            {
                NativeSubtitle.Text = "(nenhum elemento selecionado)";
                FooterStatus.Text = "Nenhum elemento selecionado no modelo.";
                SetsSummaryText.Text = "0 set(s) detectado(s).";
                return;
            }

            var items = doc.CurrentSelection.SelectedItems.Cast<ModelItem>().ToList();
            _selectedItems = items;

            var catMap = await Task.Run(() => PropertyExtractionHelper.ExtractPropertiesFromItems(items));
            _nativeCatMap = catMap;

            foreach (var catKv in catMap.OrderBy(c => c.Key))
            {
                var node = new LabCatNode { Name = catKv.Key };
                foreach (var propKv in catKv.Value.OrderBy(p => p.Key))
                    node.Properties.Add(new LabPropNode { Name = propKv.Key, Value = propKv.Value });
                _nativeCats.Add(node);

                NativeCategoryCombo.Items.Add(catKv.Key);
            }

            if (string.IsNullOrWhiteSpace(CustomCategoryNameBox.Text))
                CustomCategoryNameBox.Text = VerumSchema.CategoriaPrincipal;

            RebuildSetsByItem(doc, items);
            RebuildDetectedSetsSummary();
            ApplySavedOrDefaultSetSelection();

            NativeSubtitle.Text = $"{catMap.Count} categorias • {items.Count} elemento(s)";
            UpdateSetsSummary();
            UpdateFooterStatus();
        }

        private void RebuildSetsByItem(Document doc, IEnumerable<ModelItem> selectedItems)
        {
            _allSetsByItem = new Dictionary<ModelItem, List<SetAssignment>>();

            foreach (var item in selectedItems)
                _allSetsByItem[item] = new List<SetAssignment>();

            foreach (var setInfo in SelectionSetCache.Collect(doc))
            {
                var savedName = string.IsNullOrWhiteSpace(setInfo.SaveName)
                    ? setInfo.Name
                    : setInfo.SaveName;

                foreach (ModelItem itemDoSet in setInfo.Items)
                {
                    if (_allSetsByItem.TryGetValue(itemDoSet, out var setsDoItem))
                        setsDoItem.Add(new SetAssignment(savedName));
                }
            }

            var keys = _allSetsByItem.Keys.ToList();
            foreach (var key in keys)
            {
                _allSetsByItem[key] = _allSetsByItem[key]
                    .Where(s => !string.IsNullOrWhiteSpace(s?.Nome))
                    .GroupBy(s => s.Nome.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Select(g => new SetAssignment(g.Key))
                    .ToList();
            }
        }

        private void RebuildDetectedSetsSummary()
        {
            ResetDetectedSetsCollection();

            var itensPorNome = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var setsDoItem in _allSetsByItem.Values)
            {
                foreach (var nome in setsDoItem
                    .Where(s => !string.IsNullOrWhiteSpace(s?.Nome))
                    .Select(s => s.Nome.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    if (itensPorNome.TryGetValue(nome, out var qtd))
                        itensPorNome[nome] = qtd + 1;
                    else
                        itensPorNome[nome] = 1;
                }
            }

            foreach (var kvp in itensPorNome.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
            {
                var item = new SelectionSetItem
                {
                    Name = kvp.Key,
                    ElementCount = kvp.Value,
                    IsSelected = false
                };
                item.PropertyChanged += OnDetectedSetPropertyChanged;
                _detectedSets.Add(item);
            }
        }

        private void ApplySavedOrDefaultSetSelection()
        {
            if (_selectedItems.Count == 0 || _detectedSets.Count == 0)
            {
                UpdateSetsSummary();
                return;
            }

            var nomesValidos = _detectedSets.Select(s => s.Name).ToList();
            var categoria = ResolveCategoryName();
            var setsSalvos = AtributoService.LerSetsSalvos(_selectedItems[0], nomesValidos, categoria);

            if (setsSalvos.Count > 0)
            {
                var salvos = new HashSet<string>(setsSalvos, StringComparer.OrdinalIgnoreCase);
                foreach (var s in _detectedSets)
                    s.IsSelected = salvos.Contains(s.Name);
            }
            else
            {
                _detectedSets[0].IsSelected = true;
            }

            UpdateSetsSummary();
        }

        private void ResetDetectedSetsCollection()
        {
            foreach (var item in _detectedSets)
                item.PropertyChanged -= OnDetectedSetPropertyChanged;
            _detectedSets.Clear();
        }

        private void OnDetectedSetPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectionSetItem.IsSelected))
            {
                UpdateSetsSummary();
                UpdateFooterStatus();
            }
        }

        private void UpdateSetsSummary()
        {
            var total = _detectedSets.Count;
            var selected = _detectedSets.Count(s => s.IsSelected);
            SetsSummaryText.Text = $"{selected}/{total} set(s) marcados para gravacao no campo fixo '{VerumSchema.PropriedadeSets}'.";
        }

        private void UpdateFooterStatus()
        {
            FooterStatus.Text =
                $"{_selectedItems.Count} elemento(s) selecionado(s) - {_nativeCatMap.Count} categoria(s) nativa(s) - {_detectedSets.Count} set(s) detectado(s) ({_detectedSets.Count(s => s.IsSelected)} marcados).";
        }

        private void BtnReloadNative_Click(object sender, RoutedEventArgs e)
            => _ = ReloadNativeAsync();

        private void BtnReloadSets_Click(object sender, RoutedEventArgs e)
        {
            var doc = NwApp.ActiveDocument;
            if (doc == null || doc.CurrentSelection.IsEmpty) return;

            var items = doc.CurrentSelection.SelectedItems.Cast<ModelItem>().ToList();
            _selectedItems = items;
            RebuildSetsByItem(doc, items);
            RebuildDetectedSetsSummary();
            ApplySavedOrDefaultSetSelection();
            UpdateFooterStatus();
        }

        private void BtnSelectAllSets_Click(object sender, RoutedEventArgs e)
        {
            foreach (var s in _detectedSets) s.IsSelected = true;
            UpdateSetsSummary();
            UpdateFooterStatus();
        }

        private void BtnClearSets_Click(object sender, RoutedEventArgs e)
        {
            foreach (var s in _detectedSets) s.IsSelected = false;
            UpdateSetsSummary();
            UpdateFooterStatus();
        }

        private void BtnImportNative_Click(object sender, RoutedEventArgs e)
        {
            var selected = NativeCategoryCombo.SelectedItem as string;
            if (selected == null)
            {
                AppendLog("[AVISO] Selecione uma categoria nativa antes de importar.");
                return;
            }

            if (!_nativeCatMap.TryGetValue(selected, out var props))
            {
                AppendLog($"[AVISO] Categoria '{selected}' nao encontrada no mapa nativo.");
                return;
            }

            CustomCategoryNameBox.Text = selected;

            _labEntries.Clear();
            foreach (var kvp in props.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
            {
                _labEntries.Add(new AttributeEntry
                {
                    Name = kvp.Key,
                    Value = kvp.Value,
                    Type = "string"
                });
            }

            AppendLog($"[OK] {props.Count} propriedade(s) importada(s) da categoria nativa '{selected}'.");
        }

        private void LabGrid_PreparingCellForEdit(object sender, DataGridPreparingCellForEditEventArgs e)
        {
            if (e.EditingElement is TextBox tb)
            {
                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input, new Action(() =>
                {
                    tb.Focus();
                    Keyboard.Focus(tb);
                    tb.SelectAll();
                }));
            }
        }

        private void EditingTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb) tb.SelectAll();
        }

        private void BtnAddLabRow_Click(object sender, RoutedEventArgs e)
            => _labEntries.Add(new AttributeEntry { Name = "", Value = "", Type = "string" });

        private void BtnRemoveLabRow_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).Tag is AttributeEntry entry)
                _labEntries.Remove(entry);
        }

        private void BtnImportAttributes_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new System.Windows.Forms.OpenFileDialog
            {
                Title = "Importar Atributos",
                Filter = "CSV (*.csv)|*.csv|XML (*.xml)|*.xml"
            };

            if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

            try
            {
                var imported = ImportarArquivo(dlg.FileName, out var importedCategory);

                if (!string.IsNullOrWhiteSpace(importedCategory))
                    CustomCategoryNameBox.Text = importedCategory;

                AppendLog($"[OK] {imported} atributo(s) importado(s) de '{System.IO.Path.GetFileName(dlg.FileName)}'.");
            }
            catch (Exception ex)
            {
                AppendLog($"[ERRO] Falha ao importar: {ExceptionHelper.UnwrapMessage(ex)}");
            }
        }

        private void BtnExportAttributes_Click(object sender, RoutedEventArgs e)
        {
            if (_labEntries.Count == 0)
            {
                MessageBox.Show("Nao ha atributos para exportar.", "Atenção", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dlg = new System.Windows.Forms.SaveFileDialog
            {
                Title = "Exportar Atributos",
                Filter = "CSV (*.csv)|*.csv|XML (*.xml)|*.xml",
                DefaultExt = "csv",
                FileName = "atributos_custom"
            };

            if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

            try
            {
                var categoria = ResolveCategoryName();
                ExportarArquivo(dlg.FileName, categoria);
                AppendLog($"[OK] Atributos exportados para '{System.IO.Path.GetFileName(dlg.FileName)}'.");
            }
            catch (Exception ex)
            {
                AppendLog($"[ERRO] Falha ao exportar: {ExceptionHelper.UnwrapMessage(ex)}");
            }
        }

        private void BtnDeleteCategory_Click(object sender, RoutedEventArgs e)
        {
            var doc = NwApp.ActiveDocument;
            if (doc == null || doc.CurrentSelection.IsEmpty)
            {
                MessageBox.Show("Nenhum elemento selecionado no modelo.", "Atencao", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var categoria = ResolveCategoryName();
            var confirm = MessageBox.Show(
                $"Excluir a categoria '{categoria}' e legadas dos elementos selecionados?",
                "Confirmar exclusao",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            var colecao = new ModelItemCollection();
            foreach (var item in doc.CurrentSelection.SelectedItems.Cast<ModelItem>())
                colecao.Add(item);

            var resultado = AtributoService.ExcluirAtributos(colecao, categoria);
            AppendLog($"[INFO] {resultado.mensagem}");

            if (resultado.erros == 0)
                _ = ReloadNativeAsync();
        }

        private void BtnWriteAndVerify_Click(object sender, RoutedEventArgs e)
        {
            LabGrid.CommitEdit(DataGridEditingUnit.Row, true);

            var doc = NwApp.ActiveDocument;
            if (doc == null || doc.CurrentSelection.IsEmpty)
            {
                MessageBox.Show("Nenhum elemento selecionado no modelo.", "Atencao", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var categoria = ResolveCategoryName();
            var atributos = BuildAtributosCustom(categoria);
            var nomesSetsSelecionados = _detectedSets
                .Where(s => s.IsSelected)
                .Select(s => s.Name)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (atributos.Count == 0 && nomesSetsSelecionados.Count == 0)
            {
                MessageBox.Show("Adicione atributos ou marque pelo menos um set.", "Atencao", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selecao = doc.CurrentSelection.SelectedItems.Cast<ModelItem>().ToList();
            var colecao = new ModelItemCollection();
            foreach (var item in selecao) colecao.Add(item);

            var setsFiltradosPorItem = FiltrarSetsSelecionados(_allSetsByItem, nomesSetsSelecionados);

            AppendLog("---------------------------------------------");
            AppendLog($"[INFO] Gravando categoria '{categoria}' em {colecao.Count} elemento(s).");
            AppendLog($"[INFO] Atributos extras: {atributos.Count} | Sets selecionados: {nomesSetsSelecionados.Count}.");

            var resultado = AtributoService.GravarAtributos(colecao, atributos, categoria, setsFiltradosPorItem);
            AppendLog($"[INFO] {resultado.mensagem}");

            if (resultado.erros == 0)
                VerifyAfterWrite(categoria);
        }

        private List<AtributoCustom> BuildAtributosCustom(string categoria)
        {
            var lista = new List<AtributoCustom>();
            foreach (var entry in _labEntries)
            {
                var nome = entry.Name?.Trim();
                if (string.IsNullOrWhiteSpace(nome)) continue;

                var tipo = (entry.Type ?? "string").Trim().ToLowerInvariant();
                if (tipo == "boolean") tipo = "bool";

                lista.Add(new AtributoCustom(categoria, nome, entry.Value ?? string.Empty, tipo));
            }
            return lista;
        }

        private static Dictionary<ModelItem, List<SetAssignment>> FiltrarSetsSelecionados(
            Dictionary<ModelItem, List<SetAssignment>> setsPorItem,
            IEnumerable<string> nomesSelecionados)
        {
            if (setsPorItem == null)
                return null;

            var nomes = new HashSet<string>(
                (nomesSelecionados ?? Enumerable.Empty<string>())
                    .Where(nome => !string.IsNullOrWhiteSpace(nome))
                    .Select(nome => nome.Trim()),
                StringComparer.OrdinalIgnoreCase);

            var resultado = new Dictionary<ModelItem, List<SetAssignment>>();
            foreach (var kvp in setsPorItem)
            {
                resultado[kvp.Key] = (kvp.Value ?? new List<SetAssignment>())
                    .Where(setInfo => !string.IsNullOrWhiteSpace(setInfo?.Nome) && nomes.Contains(setInfo.Nome.Trim()))
                    .GroupBy(setInfo => setInfo.Nome.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Select(g => new SetAssignment(g.Key))
                    .ToList();
            }

            return resultado;
        }

        private void VerifyAfterWrite(string writtenCategoryName)
        {
            try
            {
                var doc = NwApp.ActiveDocument;
                if (doc == null || doc.CurrentSelection.IsEmpty) return;

                var firstItem = doc.CurrentSelection.SelectedItems.Cast<ModelItem>().First();

                var sb = new StringBuilder();
                sb.AppendLine("[INFO] Categorias apos gravacao no primeiro elemento:");

                foreach (PropertyCategory cat in firstItem.PropertyCategories)
                {
                    bool isTargetCat = string.Equals(cat.DisplayName, writtenCategoryName, StringComparison.OrdinalIgnoreCase);
                    string tag = isTargetCat ? " <- alvo" : string.Empty;
                    sb.AppendLine($"  [{cat.DisplayName}]{tag}");

                    if (!isTargetCat) continue;

                    foreach (DataProperty prop in cat.Properties)
                        sb.AppendLine($"      - {prop.DisplayName} = {PropertyExtractionHelper.SafeValue(prop.Value)}");
                }

                AppendLog(sb.ToString().TrimEnd());

                AppendLog("[INFO] Verificacao COM API (UserDefined):");
                var nwState = (InwOpState3)ComBridgeHelper.ObterEstado();
                var nwPath = (InwOaPath3)Autodesk.Navisworks.Api.ComApi.ComApiBridge.ToInwOaPath(firstItem);
                var propNode = (InwGUIPropertyNode2)nwState.GetGUIPropertyNode(nwPath, false);

                bool foundTarget = false;
                bool foundUserDefined = false;

                foreach (object obj in propNode.GUIAttributes())
                {
                    var a = obj as InwGUIAttribute2;
                    if (a == null) continue;

                    bool isTarget = string.Equals(a.name, writtenCategoryName, StringComparison.OrdinalIgnoreCase);
                    if (!isTarget) continue;

                    foundTarget = true;
                    if (a.UserDefined) foundUserDefined = true;

                    string tipo = a.UserDefined ? "[UserDefined]" : "[Nativo]";
                    AppendLog($"  {tipo} categoria: '{a.name}'");

                    foreach (object pObj in a.Properties())
                    {
                        var p = pObj as InwOaProperty;
                        if (p != null)
                            AppendLog($"      - {p.UserName} = {p.value}");
                    }
                }

                if (foundTarget && foundUserDefined)
                    AppendLog($"[OK] Categoria '{writtenCategoryName}' confirmada como UserDefined.");
                else if (foundTarget)
                    AppendLog($"[AVISO] Categoria '{writtenCategoryName}' encontrada, mas sem flag UserDefined.");
                else
                    AppendLog($"[AVISO] Categoria '{writtenCategoryName}' nao encontrada via COM apos gravacao.");

                _ = ReloadNativeAsync();
            }
            catch (Exception ex)
            {
                AppendLog($"[ERRO] Falha na verificacao: {ExceptionHelper.UnwrapMessage(ex)}");
            }
        }

        private void AppendLog(string line)
        {
            LogText.Text += line + "\n";
        }

        private string ResolveCategoryName()
        {
            var categoria = CustomCategoryNameBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(categoria))
            {
                categoria = VerumSchema.CategoriaPrincipal;
                CustomCategoryNameBox.Text = categoria;
            }
            return categoria;
        }

        private void BtnClearLog_Click(object sender, RoutedEventArgs e)
            => LogText.Text = string.Empty;

        private void ExportarArquivo(string path, string categoria)
        {
            if (path.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                AttributeTemplateSerializer.ExportXml(path, categoria, _labEntries);
            else
                AttributeTemplateSerializer.ExportCsv(path, categoria, _labEntries);
        }

        private int ImportarArquivo(string path, out string categoria)
        {
            (var entries, categoria) = path.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)
                ? AttributeTemplateSerializer.ImportXml(path)
                : AttributeTemplateSerializer.ImportCsv(path);

            _labEntries.Clear();
            foreach (var e in entries) _labEntries.Add(e);
            return entries.Count;
        }
    }
}
