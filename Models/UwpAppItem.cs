using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GvzkHubOptimizer.Models
{
    public class UwpAppItem : INotifyPropertyChanged
    {
        private bool _isSelected;

        public string PackageName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Publisher { get; set; } = string.Empty;

        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
}
