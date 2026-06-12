using System.Windows;

using NavisworksToolkit.Core;
using NavisworksToolkit.Shared;

namespace NavisworksToolkit.Modules.SetColoring
{
    public partial class SetColoringRulesWindow : Window
    {
        private readonly SetColoringRulesViewModel _viewModel;

        public SetColoringRulesWindow()
        {
            InitializeComponent();
            _viewModel = new SetColoringRulesViewModel();
            DataContext = _viewModel;

            Loaded += async (s, e) =>
            {
                Activate();
                Topmost = true;
                Topmost = false;
                await _viewModel.LoadSetsAsync();
            };

            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(SetColoringRulesViewModel.Log))
                    LogScrollViewer.ScrollToBottom();
            };
        }

        private void ColorCell_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is System.Windows.Controls.Button btn)) return;
            if (!(btn.Tag is ColoringRule rule)) return;

            using (var dlg = new System.Windows.Forms.ColorDialog())
            {
                try
                {
                    var c = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(rule.HexColor);
                    dlg.Color = System.Drawing.Color.FromArgb(c.R, c.G, c.B);
                }
                catch { }

                dlg.FullOpen = true;
                dlg.AllowFullOpen = true;

                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var picked = dlg.Color;
                    rule.HexColor = $"#{picked.R:X2}{picked.G:X2}{picked.B:X2}";
                }
            }
        }

        private void BtnRandomColors_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.RandomizeColorsForEnabled();
        }

        private void BtnEnableAll_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.SetAllEnabled(true);
        }

        private void BtnDisableAll_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.SetAllEnabled(false);
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}
