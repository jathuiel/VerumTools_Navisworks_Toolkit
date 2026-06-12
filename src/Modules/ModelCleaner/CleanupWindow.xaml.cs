using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;

using NavisworksToolkit.Core;
using NavisworksToolkit.Shared;

namespace NavisworksToolkit.Modules.ModelCleaner
{
    /// <summary>
    /// Ferramenta de Limpeza: lista os Search/Selection Sets e os Viewpoints do documento em
    /// duas listas com checkbox/busca, e remove os selecionados (ou TODOS via "Limpar tudo").
    /// A remoção é destrutiva, então cada ação pede confirmação; é, em geral, reversível com
    /// Ctrl+Z no Navisworks. Itens marcados permanecem marcados mesmo quando ocultos pelo
    /// filtro de busca (o filtro é só visual; a remoção opera sobre tudo o que está marcado).
    /// </summary>
    public partial class CleanupWindow : Window
    {
        private readonly NavisworksInterop _interop;
        private CleanupManager _manager;

        private readonly ObservableCollection<CleanupItemData> _sets = new ObservableCollection<CleanupItemData>();
        private readonly ObservableCollection<CleanupItemData> _vps = new ObservableCollection<CleanupItemData>();
        private ICollectionView _setsView;
        private ICollectionView _vpsView;
        private string _setsSearch = string.Empty;
        private string _vpsSearch = string.Empty;
        private bool _suppress;   // suprime o sync do "selecionar todos" durante operações em lote
        private bool _busy;

        public CleanupWindow(NavisworksInterop interop)
        {
            InitializeComponent();
            _interop = interop ?? throw new ArgumentNullException(nameof(interop));

            // Views filtráveis (a busca é só visual; não mexe nas coleções-fonte).
            _setsView = CollectionViewSource.GetDefaultView(_sets);
            _setsView.Filter = o => FilterItem(o, _setsSearch);
            SetsList.ItemsSource = _setsView;

            _vpsView = CollectionViewSource.GetDefaultView(_vps);
            _vpsView.Filter = o => FilterItem(o, _vpsSearch);
            VpsList.ItemsSource = _vpsView;

            UiCommon.ApplyBranding(this, PartnersLogo);
            try
            {
                _manager = new CleanupManager(_interop);
                LoadData();
            }
            catch (Exception ex)
            {
                UiCommon.ShowError("Erro de inicialização", $"Falha ao iniciar a Limpeza:\n{UiCommon.Flatten(ex)}");
            }
        }

        // ===================== Carregamento =====================

        private void LoadData()
        {
            UpdateStatus("Carregando...");

            foreach (var i in _sets) i.PropertyChanged -= Item_PropertyChanged;
            foreach (var i in _vps) i.PropertyChanged -= Item_PropertyChanged;
            _sets.Clear();
            _vps.Clear();

            foreach (var s in _manager.GetSearchSets())
            {
                s.PropertyChanged += Item_PropertyChanged;
                _sets.Add(s);
            }
            foreach (var v in _manager.GetViewpoints())
            {
                v.PropertyChanged += Item_PropertyChanged;
                _vps.Add(v);
            }

            UpdateCounts();
            SelectableListUi.SyncHeader(SetsSelectAll, _setsView);
            SelectableListUi.SyncHeader(VpsSelectAll, _vpsView);
            UpdateStatus($"{_sets.Count} conjunto(s) e {_vps.Count} viewpoint(s) carregados");
        }

        private void UpdateCounts()
        {
            SetsCountLabel.Text = $"{_sets.Count} conjunto(s)";
            VpsCountLabel.Text = $"{_vps.Count} viewpoint(s)";
        }

        // ===================== Busca / seleção =====================

        private static bool FilterItem(object obj, string term)
        {
            if (string.IsNullOrWhiteSpace(term)) return true;
            var it = obj as CleanupItemData;
            if (it == null) return false;

            var q = term.Trim();
            return SelectableListUi.Matches(it.DisplayName, q)
                || SelectableListUi.Matches(it.Path, q)
                || SelectableListUi.Matches(it.TypeLabel, q);
        }

        private void SetsSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            _setsSearch = SetsSearch.Text ?? string.Empty;
            SetsClearSearch.Visibility = _setsSearch.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
            _setsView?.Refresh();
            SelectableListUi.SyncHeader(SetsSelectAll, _setsView);
        }

        private void VpsSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            _vpsSearch = VpsSearch.Text ?? string.Empty;
            VpsClearSearch.Visibility = _vpsSearch.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
            _vpsView?.Refresh();
            SelectableListUi.SyncHeader(VpsSelectAll, _vpsView);
        }

        private void SetsClearSearch_Click(object sender, RoutedEventArgs e) { SetsSearch.Clear(); SetsSearch.Focus(); }
        private void VpsClearSearch_Click(object sender, RoutedEventArgs e) { VpsSearch.Clear(); VpsSearch.Focus(); }

        private void SetsSelectAll_Click(object sender, RoutedEventArgs e)
            => SelectableListUi.ToggleAll(SetsSelectAll, _setsView, ref _suppress);
        private void VpsSelectAll_Click(object sender, RoutedEventArgs e)
            => SelectableListUi.ToggleAll(VpsSelectAll, _vpsView, ref _suppress);

        private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_suppress || e.PropertyName != nameof(CleanupItemData.IsSelected)) return;
            var it = sender as CleanupItemData;
            if (it == null) return;
            if (it.Kind == CleanupKind.SearchSet) SelectableListUi.SyncHeader(SetsSelectAll, _setsView);
            else SelectableListUi.SyncHeader(VpsSelectAll, _vpsView);
        }

        // ===================== Ações =====================

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (_busy) return;
            try { LoadData(); }
            catch (Exception ex) { UiCommon.ShowError("Atualizar", UiCommon.Flatten(ex)); UpdateStatus("Falha ao atualizar"); }
        }

        private async void RemoveSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            if (_busy) return;

            // Tudo o que está marcado nas duas listas (mesmo itens ocultos pelo filtro).
            var selected = _sets.Where(i => i.IsSelected)
                                 .Concat(_vps.Where(i => i.IsSelected))
                                 .ToList();
            if (selected.Count == 0)
            {
                UiCommon.ShowWarning("Nada selecionado", "Marque ao menos um item para remover.");
                return;
            }

            var setCount = selected.Count(i => i.Kind == CleanupKind.SearchSet);
            var vpCount = selected.Count - setCount;
            if (!UiCommon.Confirm("Remover selecionados",
                    $"Remover {selected.Count} item(ns)?\n\n" +
                    $"• {setCount} search set(s)\n• {vpCount} viewpoint(s)\n\n" +
                    "Geralmente reversível com Ctrl+Z no Navisworks."))
                return;

            var result = default(CleanupResult);
            SetBusy(true);
            try
            {
                SetProgress(30, "Removendo selecionados...");
                await Dispatcher.Yield(DispatcherPriority.Background);
                result = _manager.RemoveItems(selected);

                SetProgress(80, "Atualizando listas...");
                await Dispatcher.Yield(DispatcherPriority.Background);
                LoadData();

                SetProgress(100, null);
                await Task.Delay(200);
            }
            catch (Exception ex)
            {
                SetBusy(false);
                UiCommon.ShowError("Remover selecionados", UiCommon.Flatten(ex));
                UpdateStatus("Falha ao remover");
                return;
            }
            SetBusy(false);

            UpdateStatus($"{result.Removed} removido(s)" + (result.Failed > 0 ? $", {result.Failed} com falha" : string.Empty));
            if (result.Failed > 0)
                UiCommon.ShowWarning("Remoção concluída",
                    $"{result.Removed} item(ns) removido(s).\n{result.Failed} não puderam ser removidos (talvez já tivessem sido removidos).");
            else
                UiCommon.ShowInfo("Remoção concluída", $"{result.Removed} item(ns) removido(s) com sucesso.");
        }

        private async void ClearAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (_busy) return;

            var totalSets = _sets.Count;
            var totalVps = _vps.Count;
            if (totalSets + totalVps == 0)
            {
                UiCommon.ShowInfo("Nada a limpar", "Não há Search Sets nem Viewpoints no modelo.");
                return;
            }

            if (!UiCommon.Confirm("Limpar tudo",
                    $"Remover TODOS os itens do modelo?\n\n" +
                    $"• {totalSets} search set(s)\n• {totalVps} viewpoint(s)\n\n" +
                    "Esta ação remove tudo de uma vez (geralmente reversível com Ctrl+Z)."))
                return;

            SetBusy(true);
            try
            {
                SetProgress(30, "Limpando tudo...");
                await Dispatcher.Yield(DispatcherPriority.Background);
                _manager.ClearAllSearchSets();
                _manager.ClearAllViewpoints();

                SetProgress(80, "Atualizando listas...");
                await Dispatcher.Yield(DispatcherPriority.Background);
                LoadData();

                SetProgress(100, null);
                await Task.Delay(200);
            }
            catch (Exception ex)
            {
                SetBusy(false);
                UiCommon.ShowError("Limpar tudo", UiCommon.Flatten(ex));
                UpdateStatus("Falha ao limpar");
                return;
            }
            SetBusy(false);

            UpdateStatus($"Limpeza concluída: {totalSets + totalVps} item(ns) removido(s)");
            UiCommon.ShowInfo("Limpeza concluída", $"Removidos {totalSets} search set(s) e {totalVps} viewpoint(s).");
        }

        // ===================== Estado / progresso =====================

        private void SetBusy(bool busy)
        {
            _busy = busy;
            RefreshButton.IsEnabled = !busy;
            RemoveSelectedButton.IsEnabled = !busy;
            ClearAllButton.IsEnabled = !busy;

            UiCommon.SetProgressBarVisible(OperationProgress, ProgressLabel, busy);
        }

        private void SetProgress(double percent, string status)
            => UiCommon.SetProgress(OperationProgress, ProgressLabel, StatusLabel, percent, status);

        // ===================== Helpers =====================

        private void UpdateStatus(string message) => StatusLabel.Text = message;
    }
}
