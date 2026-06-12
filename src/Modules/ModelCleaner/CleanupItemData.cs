using System.ComponentModel;
using Autodesk.Navisworks.Api;

using NavisworksToolkit.Core;
using NavisworksToolkit.Shared;

namespace NavisworksToolkit.Modules.ModelCleaner
{
    /// <summary>Origem do item de limpeza — define de qual coleção do documento removê-lo.</summary>
    public enum CleanupKind
    {
        SearchSet,
        Viewpoint
    }

    /// <summary>
    /// DTO de um item removível na ferramenta de Limpeza (um Search/Selection Set ou um
    /// Viewpoint). Guarda a referência ao <see cref="SavedItem"/> nativo para remoção direta.
    /// Apenas <see cref="IsSelected"/> muda em runtime (checkbox), por isso INotifyPropertyChanged.
    /// </summary>
    public class CleanupItemData : INotifyPropertyChanged, ISelectableItem
    {
        public string DisplayName { get; set; }
        public string TypeLabel { get; set; }   // "Busca" / "Explícito" / "Viewpoint"
        public string Path { get; set; }         // caminho na árvore (ex.: "Pasta A / Sub")
        public CleanupKind Kind { get; set; }
        public SavedItem Item { get; set; }      // referência nativa p/ Remove(SavedItem)

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value) return;
                _isSelected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public override string ToString() => $"{TypeLabel}: {DisplayName}";
    }
}
