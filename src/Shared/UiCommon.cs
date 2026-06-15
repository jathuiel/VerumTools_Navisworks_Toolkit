using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

using NavisworksToolkit.Core;
using NavisworksToolkit.Shared;

namespace NavisworksToolkit.Shared
{
    /// <summary>
    /// Helpers de UI compartilhados pelas três janelas da suíte (branding, diálogos
    /// padronizados e formatação de exceções). Centraliza o que antes era triplicado
    /// em MainWindow, CleanupWindow e ExportWindow.
    /// </summary>
    internal static class UiCommon
    {
        // ===================== Branding (ícone + logo) =====================

        /// <summary>
        /// Aplica o ícone da janela e a logo do header a partir dos recursos embutidos na
        /// DLL (Build Action = Resource). Tudo em try/catch: um recurso ausente apenas não
        /// aparece — nunca impede a janela de abrir (essencial para um plugin hospedado).
        /// </summary>
        public static void ApplyBranding(Window window, Image partnersLogo)
        {
            const string asm = "NavisworksToolkit";
            if (window != null)
                TryLoadImage($"pack://application:,,,/{asm};component/assets/Icone.png",
                    bmp => window.Icon = bmp);
            if (partnersLogo != null)
                TryLoadImage($"pack://application:,,,/{asm};component/assets/logo_partners.png",
                    bmp => partnersLogo.Source = bmp);

            // Versão no rodapé: preenche qualquer janela que tenha um TextBlock x:Name="VersionText".
            if (window?.FindName("VersionText") is TextBlock versionText)
                versionText.Text = VersionLabel;
        }

        /// <summary>Rótulo de versão do plugin (ex.: "v1.0.0"), lido da assembly em runtime.</summary>
        public static string VersionLabel
        {
            get
            {
                try
                {
                    var v = typeof(UiCommon).Assembly.GetName().Version;
                    return v == null ? string.Empty : $"v{v.Major}.{v.Minor}.{v.Build}";
                }
                catch { return string.Empty; }
            }
        }

        private static void TryLoadImage(string packUri, Action<BitmapImage> apply)
        {
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(packUri, UriKind.Absolute);
                bmp.CacheOption = BitmapCacheOption.OnLoad;  // carrega já, não trava o recurso
                bmp.EndInit();
                bmp.Freeze();                                 // imutável => seguro entre threads
                apply(bmp);
            }
            catch
            {
                // Recurso de marca ausente/ilegível: segue sem ele (falha silenciosa).
            }
        }

        // ===================== Diálogos padronizados =====================

        public static void ShowInfo(string title, string message) =>
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);

        public static void ShowWarning(string title, string message) =>
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);

        public static void ShowError(string title, string message) =>
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);

        public static bool Confirm(string title, string message) =>
            MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning)
                == MessageBoxResult.Yes;

        // ===================== Progresso =====================

        /// <summary>
        /// Atualiza a barra (0–100, com clamp) e o percentual no rodapé. Se
        /// <paramref name="status"/> não for nulo, também atualiza a mensagem de status.
        /// </summary>
        public static void SetProgress(ProgressBar bar, TextBlock percentLabel,
                                       TextBlock statusLabel, double percent, string status)
        {
            var clamped = percent < 0 ? 0 : (percent > 100 ? 100 : percent);
            bar.Value = clamped;
            percentLabel.Text = $"{clamped:0}%";
            if (status != null)
                statusLabel.Text = status;
        }

        /// <summary>
        /// Mostra a barra zerada (início de operação) ou a esconde limpando o rótulo (fim) —
        /// a parte comum do SetBusy das três janelas.
        /// </summary>
        public static void SetProgressBarVisible(ProgressBar bar, TextBlock percentLabel, bool visible)
        {
            if (visible)
            {
                bar.Value = 0;
                percentLabel.Text = "0%";
                bar.Visibility = Visibility.Visible;
            }
            else
            {
                bar.Visibility = Visibility.Collapsed;
                percentLabel.Text = string.Empty;
            }
        }

        // ===================== Exceções =====================

        /// <summary>
        /// Achata a cadeia de exceções (mensagem + todas as InnerException) para que a
        /// causa-raiz apareça na UI. Sem isso, só o wrapper de topo era exibido,
        /// escondendo o erro real lançado pela Navisworks API.
        /// </summary>
        public static string Flatten(Exception ex)
        {
            var sb = new StringBuilder();
            var level = 0;
            for (var current = ex; current != null; current = current.InnerException)
            {
                sb.AppendLine($"{new string(' ', level * 2)}• [{current.GetType().Name}] {current.Message}");
                level++;
            }
            return sb.ToString().TrimEnd();
        }
    }
}
