using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using WinForms = System.Windows.Forms;

using NavisworksToolkit.Core;
using NavisworksToolkit.Shared;

namespace NavisworksToolkit.Modules.ImageExporter
{
    /// <summary>
    /// Ferramenta de Exportação: lista os Viewpoints (checkbox + busca + selecionar-todos,
    /// duplicatas sinalizadas) e exporta imagens JPG dos selecionados, com controle de
    /// resolução, qualidade, prefixo/sufixo e inclusão de markups. A renderização roda na
    /// thread STA da UI; o loop cede o thread (Dispatcher.Yield) entre as imagens para a barra
    /// de progresso responder.
    /// </summary>
    public partial class ExportWindow : Window
    {
        private readonly NavisworksInterop _interop;
        private ExportManager _manager;

        private readonly ObservableCollection<ExportItemData> _vps = new ObservableCollection<ExportItemData>();
        private ICollectionView _vpsView;
        private string _vpsSearch = string.Empty;
        private bool _suppress;
        private bool _busy;
        private string _lastFolder;

        public ExportWindow(NavisworksInterop interop)
        {
            InitializeComponent();
            _interop = interop ?? throw new ArgumentNullException(nameof(interop));

            _vpsView = CollectionViewSource.GetDefaultView(_vps);
            _vpsView.Filter = o => FilterItem(o, _vpsSearch);
            VpsList.ItemsSource = _vpsView;

            UiCommon.ApplyBranding(this, PartnersLogo);
            try
            {
                _manager = new ExportManager(_interop);
                LoadData();
            }
            catch (Exception ex)
            {
                UiCommon.ShowError("Erro de inicialização", $"Falha ao iniciar a Exportação:\n{UiCommon.Flatten(ex)}");
            }
        }

        // ===================== Carregamento =====================

        private void LoadData()
        {
            UpdateStatus("Carregando viewpoints...");

            foreach (var i in _vps) i.PropertyChanged -= Item_PropertyChanged;
            _vps.Clear();

            foreach (var v in _manager.GetViewpoints())
            {
                v.PropertyChanged += Item_PropertyChanged;
                _vps.Add(v);
            }

            VpsCountLabel.Text = $"{_vps.Count} viewpoint(s)";
            SyncSelectAll();
            UpdateSelInfo();
            UpdateStatus($"{_vps.Count} viewpoint(s) carregados");
        }

        // ===================== Busca / seleção =====================

        private static bool FilterItem(object obj, string term)
        {
            if (string.IsNullOrWhiteSpace(term)) return true;
            var it = obj as ExportItemData;
            if (it == null) return false;
            var q = term.Trim();
            return SelectableListUi.Matches(it.DisplayName, q)
                || SelectableListUi.Matches(it.Path, q);
        }

        private void VpsSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            _vpsSearch = VpsSearch.Text ?? string.Empty;
            VpsClearSearch.Visibility = _vpsSearch.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
            _vpsView?.Refresh();
            SyncSelectAll();
        }

        private void VpsClearSearch_Click(object sender, RoutedEventArgs e) { VpsSearch.Clear(); VpsSearch.Focus(); }

        private void VpsSelectAll_Click(object sender, RoutedEventArgs e)
        {
            SelectableListUi.ToggleAll(VpsSelectAll, _vpsView, ref _suppress);
            UpdateSelInfo();
        }

        private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_suppress || e.PropertyName != nameof(ExportItemData.IsSelected)) return;
            SyncSelectAll();
            UpdateSelInfo();
        }

        private void SyncSelectAll()
            => SelectableListUi.SyncHeader(VpsSelectAll, _vpsView);

        private void UpdateSelInfo()
        {
            if (SelInfoLabel == null) return;
            var n = _vps.Count(i => i.IsSelected);
            SelInfoLabel.Text = $"{n} selecionado(s)";
        }

        // ===================== Ações =====================

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            using (var dlg = new WinForms.FolderBrowserDialog())
            {
                dlg.Description = "Escolha a pasta de destino das imagens";
                if (!string.IsNullOrWhiteSpace(OutputFolderBox.Text))
                    dlg.SelectedPath = OutputFolderBox.Text;
                if (dlg.ShowDialog() == WinForms.DialogResult.OK)
                    OutputFolderBox.Text = dlg.SelectedPath;
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (_busy) return;
            try { LoadData(); }
            catch (Exception ex) { UiCommon.ShowError("Atualizar", UiCommon.Flatten(ex)); UpdateStatus("Falha ao atualizar"); }
        }

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (_busy) return;

            var selected = _vps.Where(i => i.IsSelected).ToList();
            if (selected.Count == 0)
            {
                UiCommon.ShowWarning("Nada selecionado", "Marque ao menos um viewpoint para exportar.");
                return;
            }

            var options = BuildOptions();
            if (options == null) return; // validação já avisou o usuário

            var total = selected.Count;
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int ok = 0, fail = 0;

            SetBusy(true);
            try
            {
                for (var i = 0; i < total; i++)
                {
                    var it = selected[i];
                    SetProgress(100.0 * i / total, $"Exportando '{it.DisplayName}' ({i + 1}/{total})...");
                    await Dispatcher.Yield(DispatcherPriority.Background);
                    try { _manager.ExportOne(it, options, used); ok++; }
                    catch { fail++; }
                }
                SetProgress(100, null);
                await Task.Delay(250);
            }
            finally
            {
                SetBusy(false);
            }

            _lastFolder = options.OutputFolder;
            UpdateStatus($"{ok} imagem(ns) exportada(s)" + (fail > 0 ? $", {fail} com falha" : string.Empty));

            var msg = $"{ok} imagem(ns) exportada(s)" + (fail > 0 ? $"\n{fail} com falha." : ".") +
                      $"\n\nPasta:\n{options.OutputFolder}\n\nAbrir a pasta agora?";
            if (MessageBox.Show(msg, "Exportação concluída", MessageBoxButton.YesNo,
                    fail > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information) == MessageBoxResult.Yes)
                OpenFolder(options.OutputFolder);
        }

        /// <summary>Lê e valida os campos de opção; devolve null (avisando) se algo estiver inválido.</summary>
        private ExportOptions BuildOptions()
        {
            var folder = (OutputFolderBox.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(folder))
            {
                UiCommon.ShowWarning("Pasta de destino", "Escolha a pasta de destino das imagens.");
                return null;
            }

            if (!int.TryParse((WidthBox.Text ?? "").Trim(), out var w) || w < 16 || w > 16384)
            {
                UiCommon.ShowWarning("Resolução", "Largura inválida (use um inteiro entre 16 e 16384).");
                return null;
            }
            if (!int.TryParse((HeightBox.Text ?? "").Trim(), out var h) || h < 16 || h > 16384)
            {
                UiCommon.ShowWarning("Resolução", "Altura inválida (use um inteiro entre 16 e 16384).");
                return null;
            }

            long quality = 90;
            if (!string.IsNullOrWhiteSpace(QualityBox.Text) && long.TryParse(QualityBox.Text.Trim(), out var q))
                quality = Math.Max(0, Math.Min(100, q));

            return new ExportOptions
            {
                OutputFolder = folder,
                Width = w,
                Height = h,
                JpegQuality = quality,
                Prefix = (PrefixBox.Text ?? string.Empty).Trim(),
                Suffix = (SuffixBox.Text ?? string.Empty).Trim(),
                IncludeMarkups = MarkupsCheck.IsChecked == true
            };
        }

        private static void OpenFolder(string folder)
        {
            try { Process.Start(new ProcessStartInfo { FileName = folder, UseShellExecute = true }); }
            catch { /* não foi possível abrir o Explorer — ignora */ }
        }

        // ===================== Estado / progresso =====================

        private void SetBusy(bool busy)
        {
            _busy = busy;
            RefreshButton.IsEnabled = !busy;
            ExportButton.IsEnabled = !busy;
            BrowseButton.IsEnabled = !busy;

            UiCommon.SetProgressBarVisible(OperationProgress, ProgressLabel, busy);
        }

        private void SetProgress(double percent, string status)
            => UiCommon.SetProgress(OperationProgress, ProgressLabel, StatusLabel, percent, status);

        // ===================== Helpers =====================

        private void UpdateStatus(string message) => StatusLabel.Text = message;
    }
}
