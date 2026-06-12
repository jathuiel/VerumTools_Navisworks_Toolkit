using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Forms.Integration;
using System.Windows.Interop;

namespace NavisworksToolkit.Core
{
    /// <summary>
    /// Abre (ou reativa) a janela modeless de UMA ferramenta dentro do host Win32 do
    /// Navisworks. Unifica os dois launchers dos projetos de origem:
    /// — Auto_ViewTool: factory que recebe um <see cref="NavisworksInterop"/> próprio,
    ///   descartado quando a janela fecha;
    /// — SetAtributesToolkit: janelas com construtor sem parâmetro, com inicialização
    ///   do esquema de pack URI antes da primeira janela WPF do processo.
    /// Cada instância mantém a janela viva entre cliques no ribbon (impede o GC de
    /// coletá-la) e garante keyboard interop + owner binding para foco e atalhos.
    /// </summary>
    internal sealed class ToolLauncher<TWindow> where TWindow : Window
    {
        private readonly string _toolName;
        private readonly Func<NavisworksInterop, TWindow> _interopFactory;
        private readonly Func<TWindow> _simpleFactory;

        private TWindow _window;
        private NavisworksInterop _interop;

        /// <summary>Ferramenta cuja janela recebe um interop próprio (padrão Auto_ViewTool).</summary>
        public ToolLauncher(string toolName, Func<NavisworksInterop, TWindow> factory)
        {
            _toolName = toolName;
            _interopFactory = factory;
        }

        /// <summary>Ferramenta cuja janela se auto-suficia via API estática (padrão SetAtributesToolkit).</summary>
        public ToolLauncher(string toolName, Func<TWindow> factory)
        {
            _toolName = toolName;
            _simpleFactory = factory;
        }

        public int Open()
        {
            try
            {
                if (_window != null && _window.IsLoaded)
                {
                    _window.Activate();
                    return 0;
                }

                // Dentro do host Win32 do Navisworks não existe Application WPF; sem isto,
                // o primeiro pack URI (temas/imagens embutidas) falharia.
                if (!UriParser.IsKnownScheme("pack"))
                    new Application();

                if (_interopFactory != null)
                {
                    _interop = new NavisworksInterop();
                    _window = _interopFactory(_interop);
                }
                else
                {
                    _window = _simpleFactory();
                }

                _window.Closed += (s, e) =>
                {
                    _interop?.Dispose();
                    _interop = null;
                    _window = null;
                };

                WireKeyboardInterop(_window);
                _window.Show();
                return 0;
            }
            catch (Exception ex)
            {
                return ShowLaunchError(_toolName, ex);
            }
        }

        /// <summary>
        /// Habilita o roteamento de teclado de uma janela WPF modeless hospedada no processo
        /// Win32 do Navisworks: sem estas duas chamadas, o mouse funciona mas os eventos de
        /// teclado não chegam aos controles WPF (caixas de texto etc.).
        /// </summary>
        private static void WireKeyboardInterop(Window window)
        {
            ElementHost.EnableModelessKeyboardInterop(window);
            var hostHandle = Process.GetCurrentProcess().MainWindowHandle;
            if (hostHandle != IntPtr.Zero)
                new WindowInteropHelper(window).Owner = hostHandle;
        }

        private static int ShowLaunchError(string toolName, Exception ex)
        {
            Debug.WriteLine(ex.ToString());
            MessageBox.Show(
                $"Falha ao abrir o {toolName}:\n{ex.Message}\n\n{ex.InnerException?.Message}",
                "Navisworks Toolkit",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return -1;
        }
    }
}
