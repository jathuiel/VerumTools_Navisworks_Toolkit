using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;

using NavisworksToolkit.Core;
using NavisworksToolkit.Shared;

namespace NavisworksToolkit.Modules.ViewBuilder
{
    public partial class MainWindow : Window
    {
        private readonly NavisworksInterop _interop;
        private SelectionSetManager _selectionSetManager;
        private IsolationHandler _isolationHandler;
        private ViewpointManager _viewpointManager;
        private ObservableCollection<SelectionSetData> _selectionSets;
        private ICollectionView _setsView;            // view filtrável sobre _selectionSets (busca)
        private string _searchText = string.Empty;    // termo de busca atual
        private bool _suppressHeaderSync;
        private bool _busy;
        private bool _cancelRequested;

        // ----- Estimativa de tempo para criar viewpoints -----
        // Modelo linear: custo (quase) fixo por set + um pequeno custo por item.
        // Calibrado com lotes REAIS (06/2026): o tempo por set é dominado pelo isolamento
        // do "nível largo" do modelo (~20 s, praticamente constante e independente do
        // ItemCount); só sets muito grandes crescem um pouco. _timeCalibration ainda refina
        // esses valores em runtime (suavização exponencial) conforme a máquina/modelo.
        private const double PerSetMs = 21000.0;
        private const double PerItemMs = 0.3;
        // Custo aproximado por viewpoint capturado (além do isolamento, que é por set). Cada
        // vista marcada multiplica este custo; calibrado em runtime junto com os demais.
        private const double PerViewpointMs = 1500.0;
        private double _timeCalibration = 1.0;

        public MainWindow(NavisworksInterop interop)
        {
            InitializeComponent();

            _interop = interop ?? throw new ArgumentNullException(nameof(interop));
            _selectionSets = new ObservableCollection<SelectionSetData>();

            // View filtrável: a busca filtra apenas o que aparece, sem mexer na coleção-fonte
            // (itens marcados continuam marcados mesmo quando ocultos pelo filtro).
            _setsView = CollectionViewSource.GetDefaultView(_selectionSets);
            _setsView.Filter = FilterSet;
            SelectionSetsList.ItemsSource = _setsView;

            UiCommon.ApplyBranding(this, PartnersLogo);
            InitializeManagers();
            LoadSelectionSets();
        }

        private void InitializeManagers()
        {
            try
            {
                _selectionSetManager = new SelectionSetManager(_interop);
                _isolationHandler = new IsolationHandler(_interop);
                _viewpointManager = new ViewpointManager(_interop);

                UpdateStatistics();
            }
            catch (Exception ex)
            {
                UiCommon.ShowError("Erro de inicialização", $"Falha ao inicializar os gerenciadores:\n{ex.Message}");
            }
        }

        private void LoadSelectionSets()
        {
            try
            {
                UpdateStatus("Carregando Selection Sets...");

                // Desinscreve os antigos antes de limpar (evita vazamento de handlers).
                foreach (var existing in _selectionSets)
                    existing.PropertyChanged -= SelectionSet_PropertyChanged;
                _selectionSets.Clear();

                var sets = _selectionSetManager.GetAllSelectionSets();
                foreach (var set in sets)
                {
                    set.PropertyChanged += SelectionSet_PropertyChanged;
                    _selectionSets.Add(set);
                }

                SyncSelectAllState();
                UpdateStatus($"{sets.Count} Selection Sets carregados");
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                UiCommon.ShowError("Erro ao carregar", $"Falha ao carregar os Selection Sets:\n{UiCommon.Flatten(ex)}");
                UpdateStatus("Falha ao carregar os Selection Sets");
            }
        }

        private async void CreateViewpointButton_Click(object sender, RoutedEventArgs e)
        {
            if (_busy)
                return;

            // Sets marcados via checkbox; fallback para o item destacado na lista.
            var targets = GetTargetSets();

            if (targets.Count == 0)
            {
                UiCommon.ShowWarning("Seleção necessária",
                    "Marque um ou mais Selection Sets na coluna de checkbox (ou selecione um na lista).");
                return;
            }

            // Opções de geração (isolamento, projeção e vistas) escolhidas no painel.
            var options = BuildOptions();
            if (options.Orientations.Count == 0)
            {
                UiCommon.ShowWarning("Vista necessária",
                    "Marque ao menos uma vista (Isométrica, Superior, Frontal, Traseira ou Lateral) " +
                    "na seção VISTAS para gerar viewpoints.");
                return;
            }

            var failures = new List<string>();
            var created = 0;
            var cancelled = false;
            string lastName = null;
            var total = targets.Count;
            var orientCount = options.Orientations.Count;
            var totalVps = total * orientCount;   // 1 viewpoint por (set × vista)
            var vpsDone = 0;                       // viewpoints concluídos (p/ a barra de progresso)

            // Instrumentação temporária: zera o log e cronometra o lote para localizar
            // o gargalo de performance (lido depois em %TEMP%\AutoViewTool_perf.log).
            PerfLog.Reset();
            PerfLog.Info($"=== Batch start: {total} set(s) × {orientCount} vista(s) = {totalVps} viewpoint(s) " +
                         $"[isolamento={options.Isolation}, projeção={options.Projection}] ===");
            var batchSw = System.Diagnostics.Stopwatch.StartNew();

            // A API Navisworks é STA/single-thread: o trabalho roda no próprio thread da UI.
            // Para a barra não congelar, cedemos o thread (Dispatcher.Yield) ANTES de cada
            // chamada bloqueante — a UI redesenha o progresso e então a operação executa.
            SetBusy(true);
            try
            {
                for (var i = 0; i < total && !cancelled; i++)
                {
                    var set = targets[i];
                    long isolateMs = 0;

                    // Fase 1: isolar UMA vez por set (o passo caro). Em seguida capturamos
                    // todas as vistas marcadas reaproveitando o mesmo estado de visibilidade.
                    SetProgress(100.0 * (i * orientCount) / totalVps, $"Isolando '{set.Name}' ({i + 1}/{total})...");
                    await Dispatcher.Yield(DispatcherPriority.Background);
                    if (_cancelRequested) { cancelled = true; break; }

                    try
                    {
                        var isoSw = System.Diagnostics.Stopwatch.StartNew();
                        _isolationHandler.IsolateSelectionSet(set, options.Isolation);
                        isolateMs = isoSw.ElapsedMilliseconds;
                    }
                    catch (Exception exIso)
                    {
                        // Falha de isolamento invalida TODAS as vistas deste set: registra e segue.
                        failures.Add($"• {set.Name}: isolamento falhou — {exIso.Message}");
                        vpsDone += orientCount;
                        SetProgress(100.0 * vpsDone / totalVps, null);
                        PerfLog.Info($"SET items={set.ItemCount,7}  isolate=FALHOU  '{set.Name}'");
                        continue;
                    }

                    // Fase 2: um viewpoint por vista marcada (mesmo isolamento, câmera diferente).
                    long viewpointMsSum = 0;
                    for (var v = 0; v < orientCount && !cancelled; v++)
                    {
                        var orientation = options.Orientations[v];
                        var label = ViewGenerationOptions.LabelOf(orientation);

                        SetProgress(100.0 * vpsDone / totalVps,
                            $"Criando '{set.Name}' · {label} ({vpsDone + 1}/{totalVps})...");
                        await Dispatcher.Yield(DispatcherPriority.Background);
                        if (_cancelRequested) { cancelled = true; break; }

                        try
                        {
                            var vpSw = System.Diagnostics.Stopwatch.StartNew();
                            var viewpoint = _viewpointManager.CreateViewpointFromSelection(
                                set, orientation, options.Projection);
                            _viewpointManager.SaveViewpoint(viewpoint);
                            viewpointMsSum += vpSw.ElapsedMilliseconds;

                            lastName = viewpoint.Name;
                            created++;
                        }
                        catch (Exception exItem)
                        {
                            // Uma vista com falha não aborta as demais do set nem o lote.
                            failures.Add($"• {set.Name} ({label}): {exItem.Message}");
                        }
                        finally
                        {
                            vpsDone++;
                            SetProgress(100.0 * vpsDone / totalVps, null);
                        }
                    }

                    // Linha-resumo para calibração: itens, tempo de isolar e soma dos viewpoints.
                    PerfLog.Info($"SET items={set.ItemCount,7}  isolate={isolateMs,6} ms  " +
                                 $"viewpoints={viewpointMsSum,6} ms ({orientCount} vista(s))  '{set.Name}'");
                }

                // Mantém 100% visível por um instante (feedback de conclusão) antes de esconder.
                SetProgress(100, null);
                await Task.Delay(400);
            }
            finally
            {
                SetBusy(false);
                batchSw.Stop();
                PerfLog.Info($"=== Batch total: {batchSw.ElapsedMilliseconds} ms ({created} created, {failures.Count} failed) ===");
                PerfLog.Info($"Log: {PerfLog.LogPath}");
            }

            if (!cancelled)
                CalibrateTimeEstimate(targets, orientCount, batchSw.ElapsedMilliseconds);
            UpdateStatistics();

            if (cancelled)
            {
                UpdateStatus($"Cancelado — {created} de {totalVps} criado(s)");
                if (failures.Count > 0)
                    UiCommon.ShowWarning("Criação interrompida",
                        $"Cancelado pelo usuário.\n{created} criados, {failures.Count} com falha:\n\n{string.Join("\n", failures)}");
            }
            else if (failures.Count == 0)
            {
                UpdateStatus($"{created} viewpoint(s) criado(s)");
                UiCommon.ShowInfo("Sucesso", totalVps == 1
                    ? $"Viewpoint criado com sucesso:\n{lastName}"
                    : $"{created} viewpoints criados com sucesso " +
                      $"({total} set(s) × {orientCount} vista(s)).");
            }
            else
            {
                UpdateStatus($"{created} criados, {failures.Count} com falha");
                UiCommon.ShowError("Criação de Viewpoints",
                    $"{created} criados, {failures.Count} com falha:\n\n{string.Join("\n", failures)}");
            }
        }

        private void SelectAllCheckBox_Click(object sender, RoutedEventArgs e)
        {
            // Opera sobre os itens VISÍVEIS (respeita o filtro de busca): se todos os visíveis
            // já estão marcados, desmarca-os; senão, marca todos os visíveis.
            SelectableListUi.ToggleAll(SelectAllCheckBox, _setsView, ref _suppressHeaderSync);
            UpdateEstimatedTime();
        }

        private void SelectionSet_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectionSetData.IsSelected) && !_suppressHeaderSync)
            {
                SyncSelectAllState();
                UpdateEstimatedTime();
            }
        }

        /// <summary>
        /// Atualiza a estimativa quando o item destacado muda — relevante apenas como
        /// fallback (nenhum checkbox marcado), espelhando a lógica de Criar Viewpoints.
        /// </summary>
        private void SelectionSetsList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            UpdateEstimatedTime();
        }

        /// <summary>
        /// Mantém o checkbox do cabeçalho coerente com as linhas: marcado (todos),
        /// desmarcado (nenhum) ou indeterminado (alguns).
        /// </summary>
        private void SyncSelectAllState()
            => SelectableListUi.SyncHeader(SelectAllCheckBox, _setsView);

        // ===================== Busca / filtro =====================

        /// <summary>
        /// Predicado do <see cref="ICollectionView"/>: mantém o set quando o termo de busca
        /// está vazio, ou quando ele casa (case-insensitive) com o Nome ou a Descrição.
        /// </summary>
        private bool FilterSet(object obj)
        {
            if (string.IsNullOrWhiteSpace(_searchText))
                return true;

            var set = obj as SelectionSetData;
            if (set == null)
                return false;

            var q = _searchText.Trim();
            return SelectableListUi.Matches(set.Name, q)
                || SelectableListUi.Matches(set.Description, q);
        }

        private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            _searchText = SearchBox.Text ?? string.Empty;
            ClearSearchButton.Visibility = _searchText.Length > 0
                ? Visibility.Visible : Visibility.Collapsed;

            _setsView?.Refresh();   // reaplica o filtro à lista
            SyncSelectAllState();   // "selecionar todos" passa a refletir os itens visíveis
        }

        private void ClearSearchButton_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Clear();      // dispara SearchBox_TextChanged (limpa filtro e o botão)
            SearchBox.Focus();
        }

        private async void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await RunWithProgressAsync(
                    "Restaurando visibilidade...",
                    "Visibilidade restaurada ao padrão",
                    () => _isolationHandler.ResetIsolation());
            }
            catch (Exception ex)
            {
                UiCommon.ShowError("Erro ao restaurar", $"Falha ao restaurar a visibilidade:\n{UiCommon.Flatten(ex)}");
                UpdateStatus("Falha ao restaurar");
            }
        }

        // ===================== Template Excel =====================

        private void GenerateTemplateButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Salvar modelo de template",
                FileName = "ViewBuilder_Template.xlsx",
                Filter = "Planilha Excel (*.xlsx)|*.xlsx",
                DefaultExt = ".xlsx",
                AddExtension = true
            };
            if (dlg.ShowDialog(this) != true)
                return;

            try
            {
                // Usa os sets atuais como exemplos reais no modelo (mais amigável).
                TemplateManager.GenerateTemplate(dlg.FileName, _selectionSets);
                UpdateStatus($"Modelo salvo: {System.IO.Path.GetFileName(dlg.FileName)}");
                UiCommon.ShowInfo("Template gerado",
                    $"Modelo criado em:\n{dlg.FileName}\n\n" +
                    "Preencha a aba 'Viewpoints' e use 'Importar template'.");
            }
            catch (Exception ex)
            {
                UiCommon.ShowError("Gerar modelo", $"Falha ao gerar o modelo:\n{UiCommon.Flatten(ex)}");
                UpdateStatus("Falha ao gerar o modelo");
            }
        }

        private void ImportTemplateButton_Click(object sender, RoutedEventArgs e)
        {
            if (_busy)
                return;

            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Importar template",
                Filter = "Planilha Excel (*.xlsx)|*.xlsx|Todos os arquivos (*.*)|*.*",
                DefaultExt = ".xlsx",
                CheckFileExists = true
            };
            if (dlg.ShowDialog(this) != true)
                return;

            try
            {
                var result = TemplateManager.Import(dlg.FileName);
                ApplyTemplate(result);
            }
            catch (Exception ex)
            {
                UiCommon.ShowError("Importar template", $"Falha ao importar:\n{UiCommon.Flatten(ex)}");
                UpdateStatus("Falha ao importar template");
            }
        }

        /// <summary>
        /// Casa as linhas importadas com os Selection Sets carregados (por nome,
        /// case/trim-insensitive), marca os correspondentes e aplica nome/descrição do
        /// template. Mostra um resumo com casados, repetidos e não encontrados.
        /// </summary>
        private void ApplyTemplate(TemplateManager.ImportResult result)
        {
            // Índice por nome (1º vencedor em caso de nomes duplicados no modelo).
            var byName = new Dictionary<string, SelectionSetData>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in _selectionSets)
            {
                var key = s?.Name?.Trim();
                if (!string.IsNullOrEmpty(key) && !byName.ContainsKey(key))
                    byName[key] = s;
            }

            // Limpa seleção/customização anterior antes de aplicar o template.
            _suppressHeaderSync = true;
            foreach (var s in _selectionSets)
            {
                s.IsSelected = false;
                s.TemplateViewpointName = null;
                s.TemplateDescription = null;
            }

            var matched = 0;
            var notFound = new List<string>();
            var duplicates = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var row in result.Rows)
            {
                if (byName.TryGetValue(row.SelectionSetName, out var set))
                {
                    if (!seen.Add(row.SelectionSetName))
                    {
                        // Set repetido no template: este fluxo cria 1 viewpoint por set.
                        duplicates.Add(row.SelectionSetName);
                        continue;
                    }

                    set.IsSelected = true;
                    set.TemplateViewpointName = row.ViewpointName;
                    set.TemplateDescription = row.Description;
                    matched++;
                }
                else
                {
                    notFound.Add(row.SelectionSetName);
                }
            }

            _suppressHeaderSync = false;
            SyncSelectAllState();
            UpdateEstimatedTime();

            var sb = new StringBuilder();
            sb.AppendLine($"Linhas no template: {result.Rows.Count}");
            sb.AppendLine($"Sets casados e marcados: {matched}");
            if (duplicates.Count > 0)
                sb.AppendLine($"Sets repetidos (1 viewpoint cada): {duplicates.Distinct(StringComparer.OrdinalIgnoreCase).Count()}");
            if (notFound.Count > 0)
            {
                sb.AppendLine($"Não encontrados no modelo: {notFound.Count}");
                sb.AppendLine("  " + string.Join(", ", notFound.Take(15)) +
                              (notFound.Count > 15 ? " …" : string.Empty));
            }
            foreach (var w in result.Warnings)
                sb.AppendLine("⚠ " + w);

            if (matched > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Revise a lista e clique em 'Criar Viewpoints'.");
            }

            UpdateStatus($"Template: {matched} marcados, {notFound.Count} não encontrados");

            if (matched == 0 && notFound.Count > 0)
                UiCommon.ShowWarning("Importação concluída", sb.ToString().TrimEnd());
            else
                UiCommon.ShowInfo("Importação concluída", sb.ToString().TrimEnd());
        }

        // ===================== Estado ocupado / barra de progresso =====================

        /// <summary>
        /// Liga/desliga o estado "ocupado": desabilita os botões de ação (a API Navisworks
        /// é single-thread, nenhuma operação pode rodar em paralelo) e mostra/esconde a
        /// barra de progresso, zerando-a ao iniciar.
        /// </summary>
        private void SetBusy(bool busy)
        {
            _busy = busy;
            if (busy) _cancelRequested = false;

            ImportTemplateButton.IsEnabled = !busy;
            GenerateTemplateButton.IsEnabled = !busy;
            CreateViewpointButton.IsEnabled = !busy;
            ResetButton.IsEnabled = !busy;
            CancelButton.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
            CancelButton.IsEnabled = true;

            UiCommon.SetProgressBarVisible(OperationProgress, ProgressLabel, busy);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _cancelRequested = true;
            CancelButton.IsEnabled = false;
            UpdateStatus("Cancelando — aguardando o set atual concluir...");
        }

        /// <summary>
        /// Atualiza a barra (0–100, com clamp) e o percentual no rodapé. Se
        /// <paramref name="status"/> não for nulo, também atualiza a mensagem de status.
        /// </summary>
        private void SetProgress(double percent, string status)
            => UiCommon.SetProgress(OperationProgress, ProgressLabel, StatusLabel, percent, status);

        /// <summary>
        /// Executa uma operação síncrona de passo único exibindo a barra como indicador de
        /// progresso: pinta o estado inicial, cede o thread para a UI redesenhar (a API
        /// Navisworks é STA, então o trabalho roda no próprio thread da UI) e mostra 100%
        /// ao concluir. Reabilita os botões/esconde a barra no finally, mesmo em exceção,
        /// e ignora reentrância enquanto outra operação estiver em curso.
        /// </summary>
        private async Task RunWithProgressAsync(string startStatus, string doneStatus, Action work)
        {
            if (work == null)
                throw new ArgumentNullException(nameof(work));
            if (_busy)
                return;

            SetBusy(true);
            try
            {
                SetProgress(12, startStatus);
                await Dispatcher.Yield(DispatcherPriority.Background);

                work();

                SetProgress(100, doneStatus);
                await Task.Delay(250);
            }
            finally
            {
                SetBusy(false);
            }

            UpdateStatistics();
        }

        private void UpdateStatistics()
        {
            try
            {
                CountLabel.Text = $"Selection Sets: {_selectionSets.Count}";
                ViewpointsLabel.Text = $"Viewpoints: {_viewpointManager?.GetViewpointCount() ?? 0}";
                UpdateEstimatedTime();
            }
            catch
            {
                // Falha silenciosa ao atualizar as estatísticas
            }
        }

        /// <summary>
        /// Sets que serão processados pelo Criar Viewpoints: os marcados via checkbox e,
        /// se nenhum estiver marcado, o item destacado na lista (fluxo de set único).
        /// </summary>
        private List<SelectionSetData> GetTargetSets()
        {
            var targets = _selectionSets.Where(s => s.IsSelected).ToList();
            if (targets.Count == 0 && SelectionSetsList.SelectedItem is SelectionSetData highlighted)
                targets.Add(highlighted);
            return targets;
        }

        /// <summary>
        /// Lê o painel de configuração (isolamento, projeção e vistas marcadas) num
        /// <see cref="ViewGenerationOptions"/>. A ordem das vistas segue a do painel.
        /// </summary>
        private ViewGenerationOptions BuildOptions()
        {
            var options = new ViewGenerationOptions
            {
                Isolation = IsoSetOnlyRadio.IsChecked == true
                    ? IsolationMode.SetItemsOnly
                    : IsolationMode.SourceFileLevel,
                Projection = CamPerspRadio.IsChecked == true
                    ? CameraProjectionMode.Perspective
                    : CameraProjectionMode.Orthographic
            };

            if (ViewIsoCheck.IsChecked == true)   options.Orientations.Add(ViewOrientation.Isometric);
            if (ViewTopCheck.IsChecked == true)   options.Orientations.Add(ViewOrientation.Top);
            if (ViewFrontCheck.IsChecked == true) options.Orientations.Add(ViewOrientation.Front);
            if (ViewBackCheck.IsChecked == true)  options.Orientations.Add(ViewOrientation.Back);
            if (ViewLeftCheck.IsChecked == true)  options.Orientations.Add(ViewOrientation.Left);
            if (ViewRightCheck.IsChecked == true) options.Orientations.Add(ViewOrientation.Right);

            // Isométricas superiores
            if (ViewTopFrontRightCheck.IsChecked == true) options.Orientations.Add(ViewOrientation.TopFrontRight);
            if (ViewTopFrontLeftCheck.IsChecked == true)  options.Orientations.Add(ViewOrientation.TopFrontLeft);
            if (ViewTopBackRightCheck.IsChecked == true)  options.Orientations.Add(ViewOrientation.TopBackRight);
            if (ViewTopBackLeftCheck.IsChecked == true)   options.Orientations.Add(ViewOrientation.TopBackLeft);

            // Isométricas intermediárias
            if (ViewTopFrontCheck.IsChecked == true) options.Orientations.Add(ViewOrientation.TopFront);
            if (ViewTopBackCheck.IsChecked == true)  options.Orientations.Add(ViewOrientation.TopBack);
            if (ViewTopRightCheck.IsChecked == true) options.Orientations.Add(ViewOrientation.TopRight);
            if (ViewTopLeftCheck.IsChecked == true)  options.Orientations.Add(ViewOrientation.TopLeft);

            return options;
        }

        /// <summary>Quantas vistas estão marcadas (≥0). Usado pela estimativa de tempo.</summary>
        private int GetSelectedViewCount()
        {
            var n = 0;
            if (ViewIsoCheck?.IsChecked == true)   n++;
            if (ViewTopCheck?.IsChecked == true)   n++;
            if (ViewFrontCheck?.IsChecked == true) n++;
            if (ViewBackCheck?.IsChecked == true)  n++;
            if (ViewLeftCheck?.IsChecked == true)  n++;
            if (ViewRightCheck?.IsChecked == true) n++;
            // Isométricas superiores
            if (ViewTopFrontRightCheck?.IsChecked == true) n++;
            if (ViewTopFrontLeftCheck?.IsChecked == true)  n++;
            if (ViewTopBackRightCheck?.IsChecked == true)  n++;
            if (ViewTopBackLeftCheck?.IsChecked == true)   n++;
            // Isométricas intermediárias
            if (ViewTopFrontCheck?.IsChecked == true) n++;
            if (ViewTopBackCheck?.IsChecked == true)  n++;
            if (ViewTopRightCheck?.IsChecked == true) n++;
            if (ViewTopLeftCheck?.IsChecked == true)  n++;
            return n;
        }

        /// <summary>Recalcula a estimativa quando o usuário marca/desmarca uma vista.</summary>
        private void ViewOption_Changed(object sender, RoutedEventArgs e) => UpdateEstimatedTime();

        /// <summary>
        /// Custo bruto (sem calibração) do lote: isolamento por set + um custo por viewpoint
        /// (set × vista) + um pequeno custo por item. Compartilhado por estimativa e calibração.
        /// </summary>
        private static double EstimateBaseMs(IReadOnlyList<SelectionSetData> targets, int viewCount)
        {
            long items = 0;
            foreach (var t in targets)
                items += Math.Max(0, t.ItemCount);

            var vpCount = targets.Count * Math.Max(1, viewCount);
            return targets.Count * PerSetMs + vpCount * PerViewpointMs + items * PerItemMs;
        }

        /// <summary>
        /// Atualiza o rótulo "Tempo est." com o tempo estimado para criar os viewpoints dos
        /// sets atualmente selecionados (custo por set + custo por item × calibração).
        /// </summary>
        private void UpdateEstimatedTime()
        {
            if (EstTimeLabel == null)
                return;

            var targets = GetTargetSets();
            var views = GetSelectedViewCount();
            if (targets.Count == 0 || views == 0)
            {
                EstTimeLabel.Text = "Tempo est.: —";
                return;
            }

            var ms = EstimateBaseMs(targets, views) * _timeCalibration;
            var totalVps = targets.Count * views;
            EstTimeLabel.Text =
                $"Tempo est.: {FormatDuration(ms)} ({totalVps} viewpoint{(totalVps == 1 ? "" : "s")})";
        }

        /// <summary>
        /// Refina <see cref="_timeCalibration"/> com o tempo real do último lote (suavização
        /// exponencial). Desconta o atraso fixo de conclusão (~400 ms do feedback de 100%)
        /// e limita o fator a [0.2, 5] para não reagir a outliers.
        /// </summary>
        private void CalibrateTimeEstimate(List<SelectionSetData> processed, int viewCount, long measuredMs)
        {
            if (processed == null || processed.Count == 0)
                return;

            var baseMs = EstimateBaseMs(processed, viewCount);
            var effective = measuredMs - 400; // remove o Task.Delay de feedback de conclusão
            if (baseMs <= 0 || effective <= 0)
                return;

            var ratio = effective / baseMs;
            ratio = Math.Max(0.2, Math.Min(5.0, ratio));
            _timeCalibration = 0.5 * _timeCalibration + 0.5 * ratio;
        }

        /// <summary>Formata uma duração em ms como texto curto: "&lt;1s", "~12s", "~2m 05s".</summary>
        private static string FormatDuration(double ms)
        {
            if (ms < 500)
                return "<1s";

            var seconds = ms / 1000.0;
            if (seconds < 60)
                return $"~{Math.Round(seconds)}s";

            var mins = (int)(seconds / 60);
            var secs = (int)Math.Round(seconds - mins * 60);
            if (secs == 60) { mins++; secs = 0; }
            return $"~{mins}m {secs:00}s";
        }

        private void UpdateStatus(string message)
        {
            StatusLabel.Text = message;
        }

    }
}
