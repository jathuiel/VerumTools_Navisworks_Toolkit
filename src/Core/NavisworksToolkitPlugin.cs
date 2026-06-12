using Autodesk.Navisworks.Api.Plugins;
using NavisworksToolkit.Modules.AttributeLab;
using NavisworksToolkit.Modules.ImageExporter;
using NavisworksToolkit.Modules.ModelCleaner;
using NavisworksToolkit.Modules.SelectionInspector;
using NavisworksToolkit.Modules.SetColoring;
using NavisworksToolkit.Modules.ViewBuilder;

namespace NavisworksToolkit.Core
{
    /// <summary>
    /// Ponto de entrada unificado da plataforma Navisworks Toolkit.
    /// Um único <see cref="CommandHandlerPlugin"/> com ribbon tab personalizado expõe as
    /// seis ferramentas consolidadas dos projetos Auto_ViewTool (Verum Toolkit) e
    /// SetAtributesToolkit (Construct Sync Toolkit).
    /// </summary>
    [Plugin("NavisworksToolkit", "VRM",
        DisplayName = "Navisworks Toolkit",
        ToolTip = "Navisworks Toolkit — Suíte de automação e atributos para Navisworks")]
    [RibbonLayout("NavisworksToolkit.xaml")]
    [RibbonTab("NavisworksToolkitTab", DisplayName = "Navisworks Toolkit")]
    // Rótulos comerciais (UX Standards): nomes curtos, em inglês, orientados a resultado.
    // Os Ids dos comandos são contrato técnico estável — não acompanham renomeações.
    [Command("ViewBuilder",
        DisplayName = "Smart Views",
        ToolTip = "Gera automaticamente viewpoints isométricos a partir de Selection Sets usando um template Excel.")]
    [Command("ModelCleaner",
        DisplayName = "Model Cleanup",
        ToolTip = "Remove Search Sets e Viewpoints indesejados para manter o modelo organizado.")]
    [Command("ImageExporter",
        DisplayName = "Image Capture",
        ToolTip = "Exporta imagens de viewpoints com resolução, qualidade e markups configuráveis.")]
    [Command("SelectionInspector",
        DisplayName = "Selection Inspector",
        ToolTip = "Inspeciona categorias e propriedades dos elementos selecionados.")]
    [Command("NativeAttrLab",
        DisplayName = "Property Explorer",
        ToolTip = "Grava e remove atributos customizados nos elementos selecionados.")]
    [Command("SetColoringRules",
        DisplayName = "Visual Sets",
        ToolTip = "Aplica substituições de cor aos elementos dos Selection Sets selecionados.")]
    public class NavisworksToolkitPlugin : CommandHandlerPlugin
    {
        // Um launcher estático por ferramenta: mantém a janela modeless viva entre cliques
        // no ribbon e concentra o padrão reuse-or-create num único lugar.

        private static readonly ToolLauncher<MainWindow> ViewBuilder =
            new ToolLauncher<MainWindow>("Smart Views", interop => new MainWindow(interop));

        private static readonly ToolLauncher<CleanupWindow> ModelCleaner =
            new ToolLauncher<CleanupWindow>("Model Cleanup", interop => new CleanupWindow(interop));

        private static readonly ToolLauncher<ExportWindow> ImageExporter =
            new ToolLauncher<ExportWindow>("Image Capture", interop => new ExportWindow(interop));

        private static readonly ToolLauncher<SelectionInspectorWindow> SelectionInspector =
            new ToolLauncher<SelectionInspectorWindow>("Selection Inspector", () => new SelectionInspectorWindow());

        private static readonly ToolLauncher<NativeAttrLabWindow> NativeAttrLab =
            new ToolLauncher<NativeAttrLabWindow>("Property Explorer", () => new NativeAttrLabWindow());

        private static readonly ToolLauncher<SetColoringRulesWindow> SetColoringRules =
            new ToolLauncher<SetColoringRulesWindow>("Visual Sets", () => new SetColoringRulesWindow());

        /// <summary>Despacha o clique de um botão do ribbon para a ferramenta correspondente.</summary>
        public override int ExecuteCommand(string commandId, params string[] parameters)
        {
            switch (commandId)
            {
                case "ViewBuilder":        return ViewBuilder.Open();
                case "ModelCleaner":       return ModelCleaner.Open();
                case "ImageExporter":      return ImageExporter.Open();
                case "SelectionInspector": return SelectionInspector.Open();
                case "NativeAttrLab":      return NativeAttrLab.Open();
                case "SetColoringRules":   return SetColoringRules.Open();
                default:                   return 0;
            }
        }

        /// <summary>Todos os comandos ficam sempre habilitados; cada janela valida o documento internamente.</summary>
        public override CommandState CanExecuteCommand(string commandId)
        {
            return new CommandState { IsEnabled = true, IsVisible = true };
        }

        /// <summary>A tab Navisworks Toolkit é sempre visível.</summary>
        public override bool CanExecuteRibbonTab(string ribbonTabId)
        {
            return true;
        }
    }
}
