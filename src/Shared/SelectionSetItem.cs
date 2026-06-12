using System.Collections.Generic;
using System.ComponentModel;

using NavisworksToolkit.Core;
using NavisworksToolkit.Shared;

namespace NavisworksToolkit.Shared
{
    public class SelectionSetItem : INotifyPropertyChanged
    {
        private bool _isSelected;

        public string Name { get; set; }
        public int ElementCount { get; set; }
        public IList<Autodesk.Navisworks.Api.ModelItem> Items { get; set; } = new List<Autodesk.Navisworks.Api.ModelItem>();

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

        public string CountText => ElementCount == 1 ? "1 elemento" : $"{ElementCount} elementos";

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
