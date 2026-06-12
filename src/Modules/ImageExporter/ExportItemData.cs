using System.ComponentModel;
using Autodesk.Navisworks.Api;

using NavisworksToolkit.Core;
using NavisworksToolkit.Shared;

namespace NavisworksToolkit.Modules.ImageExporter
{
    /// <summary>
    /// DTO de um Viewpoint exportável. Guarda a referência ao <see cref="SavedViewpoint"/>
    /// nativo (aplicado via <c>CurrentSavedViewpoint</c> antes de renderizar a imagem).
    /// <see cref="IsSelected"/> e <see cref="IsDuplicate"/> mudam em runtime (UI).
    /// </summary>
    public class ExportItemData : INotifyPropertyChanged, ISelectableItem
    {
        public string DisplayName { get; set; }
        public string Path { get; set; }                 // caminho na árvore de viewpoints
        public SavedViewpoint Viewpoint { get; set; }    // referência nativa p/ render

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { if (_isSelected == value) return; _isSelected = value; OnChanged(nameof(IsSelected)); }
        }

        private bool _isDuplicate;
        /// <summary>True se há outro viewpoint com o mesmo nome (sinalização de duplicata).</summary>
        public bool IsDuplicate
        {
            get => _isDuplicate;
            set { if (_isDuplicate == value) return; _isDuplicate = value; OnChanged(nameof(IsDuplicate)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public override string ToString() => DisplayName;
    }
}
