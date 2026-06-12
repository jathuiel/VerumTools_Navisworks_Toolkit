using System.Collections.ObjectModel;
using System.ComponentModel;

using NavisworksToolkit.Core;
using NavisworksToolkit.Shared;

namespace NavisworksToolkit.Modules.AttributeLab
{
    public class LabCatNode : INotifyPropertyChanged
    {
        private bool _isExpanded = true;

        public string Name { get; set; }

        public bool IsExpanded
        {
            get => _isExpanded;
            set { _isExpanded = value; OnPropertyChanged(nameof(IsExpanded)); }
        }

        public ObservableCollection<LabPropNode> Properties { get; } = new ObservableCollection<LabPropNode>();

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
