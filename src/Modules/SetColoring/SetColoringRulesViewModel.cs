using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

using NavisworksToolkit.Core;
using NavisworksToolkit.Shared;

namespace NavisworksToolkit.Modules.SetColoring
{
    public class SetColoringRulesViewModel : INotifyPropertyChanged
    {
        private readonly SetColoringService _service = new SetColoringService();
        private readonly Random _random = new Random();
        private readonly StringBuilder _logBuilder = new StringBuilder();
        private bool _isBusy;
        private string _log = string.Empty;
        private string _statusText = "Pronto";

        public ObservableCollection<ColoringRule> ColoringRules { get; }
            = new ObservableCollection<ColoringRule>();

        public ICommand LoadSetsCommand { get; }
        public ICommand ImportXmlCommand { get; }
        public ICommand ExportXmlCommand { get; }
        public ICommand ApplyColoringCommand { get; }
        public ICommand RemoveColoringCommand { get; }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (_isBusy == value) return;
                _isBusy = value;
                OnPropertyChanged(nameof(IsBusy));
                RaiseAll();
            }
        }

        public string Log
        {
            get => _log;
            private set { if (_log == value) return; _log = value; OnPropertyChanged(nameof(Log)); }
        }

        public string StatusText
        {
            get => _statusText;
            private set { if (_statusText == value) return; _statusText = value; OnPropertyChanged(nameof(StatusText)); }
        }

        public SetColoringRulesViewModel()
        {
            LoadSetsCommand = new AsyncRelayCommand(LoadSetsAsync, () => !IsBusy);
            ImportXmlCommand = new AsyncRelayCommand(ImportXmlAsync, () => !IsBusy);
            ExportXmlCommand = new AsyncRelayCommand(ExportXmlAsync, () => !IsBusy && ColoringRules.Any());
            ApplyColoringCommand = new AsyncRelayCommand(ApplyColoringAsync, () => !IsBusy && ColoringRules.Any());
            RemoveColoringCommand = new AsyncRelayCommand(RemoveColoringAsync, () => !IsBusy);
        }

        public async Task LoadSetsAsync()
        {
            IsBusy = true;
            StatusText = "Carregando Selection Sets...";
            try
            {
                await Task.Yield();
                var sets = _service.LoadSelectionSets();
                var anyPreselected = sets.Any(s => s.IsSelected);
                var preselectedCount = sets.Count(s => s.IsSelected);

                ColoringRules.Clear();
                foreach (var s in sets)
                {
                    ColoringRules.Add(new ColoringRule
                    {
                        SelectionSetName = s.Name,
                        ElementCount = s.ElementCount,
                        IsEnabled = anyPreselected ? s.IsSelected : false
                    });
                }

                AppendLog($"[INFO] {ColoringRules.Count} set(s) carregado(s).");
                if (anyPreselected)
                    AppendLog($"[INFO] {preselectedCount} set(s) auto-marcado(s) por sobreposicao com a selecao atual.");
                StatusText = $"{ColoringRules.Count} set(s) disponiveis.";
            }
            catch (Exception ex)
            {
                AppendLog($"[ERRO] {ExceptionHelper.UnwrapMessage(ex)}");
                StatusText = "Erro ao carregar sets.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ImportXmlAsync()
        {
            var dlg = new System.Windows.Forms.OpenFileDialog
            {
                Title = "Importar Regras de Coloracao",
                Filter = "XML (*.xml)|*.xml",
                DefaultExt = "xml"
            };
            if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

            IsBusy = true;
            StatusText = "Importando XML...";
            try
            {
                var path = dlg.FileName;
                var rules = await Task.Run(() => _service.LoadFromXml(path));

                foreach (var r in rules)
                {
                    var existing = ColoringRules.FirstOrDefault(x =>
                        string.Equals(x.SelectionSetName, r.SelectionSetName, StringComparison.OrdinalIgnoreCase));
                    if (existing != null)
                    {
                        existing.HexColor = r.HexColor;
                        existing.Transparency = r.Transparency;
                        existing.IsEnabled = r.IsEnabled;
                    }
                    else
                    {
                        ColoringRules.Add(r);
                    }
                }

                AppendLog($"[INFO] {rules.Count} regra(s) importada(s) de '{System.IO.Path.GetFileName(dlg.FileName)}'.");
                StatusText = "XML importado com sucesso.";
            }
            catch (Exception ex)
            {
                AppendLog($"[ERRO] Falha ao importar XML: {ExceptionHelper.UnwrapMessage(ex)}");
                StatusText = "Erro ao importar.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ExportXmlAsync()
        {
            var dlg = new System.Windows.Forms.SaveFileDialog
            {
                Title = "Exportar Regras de Coloracao",
                Filter = "XML (*.xml)|*.xml",
                DefaultExt = "xml",
                FileName = "coloring_rules"
            };
            if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

            IsBusy = true;
            StatusText = "Exportando XML...";
            try
            {
                var snapshot = ColoringRules.ToList();
                var path = dlg.FileName;
                await Task.Run(() => _service.SaveToXml(snapshot, path));
                AppendLog($"[INFO] {snapshot.Count} regra(s) exportada(s) para '{System.IO.Path.GetFileName(dlg.FileName)}'.");
                StatusText = "XML exportado com sucesso.";
            }
            catch (Exception ex)
            {
                AppendLog($"[ERRO] Falha ao exportar XML: {ExceptionHelper.UnwrapMessage(ex)}");
                StatusText = "Erro ao exportar.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ApplyColoringAsync()
        {
            IsBusy = true;
            StatusText = "Aplicando cores...";
            AppendLog("[INFO] Iniciando aplicacao de cores...");
            try
            {
                var (applied, skipped, errors) = await _service.ApplyColoringAsync(ColoringRules, AppendLog);
                StatusText = $"Aplicados: {applied}  Pulados: {skipped}  Erros: {errors}";
                AppendLog($"[INFO] Concluido. Aplicados: {applied}, Pulados: {skipped}, Erros: {errors}.");
            }
            catch (Exception ex)
            {
                AppendLog($"[ERRO] {ExceptionHelper.UnwrapMessage(ex)}");
                StatusText = "Erro ao aplicar cores.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task RemoveColoringAsync()
        {
            IsBusy = true;
            StatusText = "Removendo cores...";
            try
            {
                var msg = await _service.RemoveColoringAsync(AppendLog);
                StatusText = msg;
            }
            catch (Exception ex)
            {
                AppendLog($"[ERRO] {ExceptionHelper.UnwrapMessage(ex)}");
                StatusText = "Erro ao remover cores.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void AppendLog(string line)
        {
            _logBuilder.AppendLine(line);
            Log = _logBuilder.ToString();
        }

        public void SetAllEnabled(bool enabled)
        {
            foreach (var rule in ColoringRules)
                rule.IsEnabled = enabled;
            AppendLog(enabled ? "[INFO] Todos os sets foram marcados." : "[INFO] Marcacoes limpas.");
        }

        public void RandomizeColorsForEnabled()
        {
            var target = ColoringRules.Where(r => r.IsEnabled).ToList();
            if (target.Count == 0)
            {
                target = ColoringRules.ToList();
                foreach (var rule in target) rule.IsEnabled = true;
                AppendLog("[AVISO] Nenhum set marcado. Aplicando randomizacao em todos.");
            }

            var colors = GenerateDistinctColors(target.Count);
            for (int i = 0; i < target.Count; i++)
            {
                var (r, g, b) = colors[i];
                target[i].HexColor = $"#{r:X2}{g:X2}{b:X2}";
            }

            AppendLog($"[INFO] Cores randomicas aplicadas em {target.Count} set(s).");
        }

        private List<(int r, int g, int b)> GenerateDistinctColors(int quantidade)
        {
            var cores = new List<(int r, int g, int b)>();
            double goldenRatio = 0.618033988749895;
            double hue = _random.NextDouble();

            for (int i = 0; i < quantidade; i++)
            {
                hue += goldenRatio;
                hue %= 1.0;
                double sat = 0.7 + (_random.NextDouble() * 0.2);
                double lum = 0.45 + (_random.NextDouble() * 0.15);

                double q = lum < 0.5 ? lum * (1 + sat) : lum + sat - lum * sat;
                double p = 2 * lum - q;
                double rd = HueToRgb(p, q, hue + (1.0 / 3.0));
                double gd = HueToRgb(p, q, hue);
                double bd = HueToRgb(p, q, hue - (1.0 / 3.0));
                cores.Add(((int)(rd * 255), (int)(gd * 255), (int)(bd * 255)));
            }

            return cores;
        }

        private static double HueToRgb(double p, double q, double t)
        {
            if (t < 0) t += 1;
            if (t > 1) t -= 1;
            if (t < 1.0 / 6.0) return p + ((q - p) * 6 * t);
            if (t < 1.0 / 2.0) return q;
            if (t < 2.0 / 3.0) return p + ((q - p) * (2.0 / 3.0 - t) * 6);
            return p;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private void RaiseAll()
        {
            ((AsyncRelayCommand)LoadSetsCommand).RaiseCanExecuteChanged();
            ((AsyncRelayCommand)ImportXmlCommand).RaiseCanExecuteChanged();
            ((AsyncRelayCommand)ExportXmlCommand).RaiseCanExecuteChanged();
            ((AsyncRelayCommand)ApplyColoringCommand).RaiseCanExecuteChanged();
            ((AsyncRelayCommand)RemoveColoringCommand).RaiseCanExecuteChanged();
        }
    }
}
