using NavisworksToolkit.Core;
using NavisworksToolkit.Shared;

namespace NavisworksToolkit.Modules.ViewBuilder
{
    /// <summary>
    /// Uma linha do template Excel: um viewpoint a criar a partir de um Selection Set.
    /// </summary>
    public class ViewpointTemplateRow
    {
        /// <summary>Número da linha no arquivo (1-based), para mensagens de erro.</summary>
        public int RowNumber { get; set; }

        /// <summary>Nome do Selection Set existente (coluna obrigatória).</summary>
        public string SelectionSetName { get; set; }

        /// <summary>Nome desejado do viewpoint (opcional; se vazio, gerado automaticamente).</summary>
        public string ViewpointName { get; set; }

        /// <summary>Descrição do viewpoint (opcional).</summary>
        public string Description { get; set; }
    }
}
