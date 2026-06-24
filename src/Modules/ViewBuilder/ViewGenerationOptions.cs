using System.Collections.Generic;

namespace NavisworksToolkit.Modules.ViewBuilder
{
    /// <summary>
    /// Nível de isolamento aplicado antes de capturar os viewpoints de um Selection Set.
    /// </summary>
    public enum IsolationMode
    {
        /// <summary>
        /// Oculta TUDO que não pertence ao set (e ao caminho de ancestrais necessário para
        /// mantê-lo visível). Mostra apenas a geometria selecionada, sem contexto.
        /// </summary>
        SetItemsOnly,

        /// <summary>
        /// Mantém o contexto do nível: ghosting cinza nos elementos do MESMO Source File dos
        /// itens do set e oculta os de outros Source Files. É o comportamento padrão/histórico.
        /// </summary>
        SourceFileLevel
    }

    /// <summary>Tipo de projeção da câmera do viewpoint.</summary>
    public enum CameraProjectionMode
    {
        Perspective,
        Orthographic
    }

    /// <summary>
    /// Posicionamento/orientação da câmera. Cada orientação marcada gera um viewpoint
    /// independente para o mesmo Selection Set.
    /// </summary>
    public enum ViewOrientation
    {
        Isometric,
        Top,
        Front,
        Back,
        Left,
        Right,

        // Isométricas superiores (canto de cima — 3 eixos: cima + frente/trás + direita/esquerda)
        TopFrontRight,
        TopFrontLeft,
        TopBackRight,
        TopBackLeft,

        // Isométricas intermediárias (vista de cima ao longo de uma direção — 2 eixos: cima + horizontal)
        TopFront,
        TopBack,
        TopRight,
        TopLeft
    }

    /// <summary>
    /// Configuração escolhida na UI para a geração em lote de viewpoints: como isolar o
    /// modelo, qual projeção usar e quais vistas produzir para cada Selection Set.
    /// </summary>
    public class ViewGenerationOptions
    {
        public IsolationMode Isolation { get; set; } = IsolationMode.SourceFileLevel;

        public CameraProjectionMode Projection { get; set; } = CameraProjectionMode.Orthographic;

        /// <summary>Orientações marcadas — uma por viewpoint. Ordem = ordem de geração.</summary>
        public List<ViewOrientation> Orientations { get; set; } = new List<ViewOrientation>();

        /// <summary>Rótulo curto em pt-BR de uma orientação, usado em nomes/sufixos de viewpoint.</summary>
        public static string LabelOf(ViewOrientation orientation)
        {
            switch (orientation)
            {
                case ViewOrientation.Top:   return "Superior";
                case ViewOrientation.Front: return "Frontal";
                case ViewOrientation.Back:  return "Traseira";
                case ViewOrientation.Left:  return "Lateral Esquerda";
                case ViewOrientation.Right: return "Lateral Direita";

                // Isométricas superiores
                case ViewOrientation.TopFrontRight: return "Top Front Right";
                case ViewOrientation.TopFrontLeft:  return "Top Front Left";
                case ViewOrientation.TopBackRight:  return "Top Back Right";
                case ViewOrientation.TopBackLeft:   return "Top Back Left";

                // Isométricas intermediárias
                case ViewOrientation.TopFront: return "Top Front";
                case ViewOrientation.TopBack:  return "Top Back";
                case ViewOrientation.TopRight: return "Top Right";
                case ViewOrientation.TopLeft:  return "Top Left";

                case ViewOrientation.Isometric:
                default:                    return "Isométrica";
            }
        }
    }
}
