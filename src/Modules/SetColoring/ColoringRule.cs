using System.ComponentModel;
using System.Windows.Media;

using NavisworksToolkit.Core;
using NavisworksToolkit.Shared;

namespace NavisworksToolkit.Modules.SetColoring
{
    public class ColoringRule : INotifyPropertyChanged
    {
        private bool _isEnabled = true;
        private string _selectionSetName = string.Empty;
        private string _hexColor = "#FF0000";
        private int _transparency = 0;
        private int _elementCount = 0;

        public bool IsEnabled
        {
            get => _isEnabled;
            set { if (_isEnabled == value) return; _isEnabled = value; OnPropertyChanged(nameof(IsEnabled)); }
        }

        public string SelectionSetName
        {
            get => _selectionSetName;
            set { if (_selectionSetName == value) return; _selectionSetName = value; OnPropertyChanged(nameof(SelectionSetName)); }
        }

        public string HexColor
        {
            get => _hexColor;
            set
            {
                if (_hexColor == value) return;
                _hexColor = value;
                OnPropertyChanged(nameof(HexColor));
                OnPropertyChanged(nameof(PreviewBrush));
            }
        }

        public int Transparency
        {
            get => _transparency;
            set
            {
                int clamped = value < 0 ? 0 : value > 100 ? 100 : value;
                if (_transparency == clamped) return;
                _transparency = clamped;
                OnPropertyChanged(nameof(Transparency));
            }
        }

        public int ElementCount
        {
            get => _elementCount;
            set { if (_elementCount == value) return; _elementCount = value; OnPropertyChanged(nameof(ElementCount)); }
        }

        public SolidColorBrush PreviewBrush
        {
            get
            {
                try
                {
                    var c = (Color)ColorConverter.ConvertFromString(HexColor);
                    return new SolidColorBrush(c);
                }
                catch
                {
                    return new SolidColorBrush(Colors.Red);
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
