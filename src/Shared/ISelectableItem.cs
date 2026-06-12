using NavisworksToolkit.Core;
using NavisworksToolkit.Shared;

namespace NavisworksToolkit.Shared
{
    /// <summary>
    /// Contrato mínimo dos DTOs listáveis com checkbox (seleção em massa nas janelas).
    /// Permite que os helpers genéricos de lista (<c>SelectableListUi</c>) operem sobre
    /// SelectionSetData, CleanupItemData e ExportItemData sem duplicar a lógica de
    /// "selecionar todos" / sincronização do cabeçalho.
    /// </summary>
    internal interface ISelectableItem
    {
        bool IsSelected { get; set; }
    }
}
