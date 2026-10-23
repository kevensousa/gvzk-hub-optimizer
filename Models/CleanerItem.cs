using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GvzkHubOptimizer.Models
{
    public class CleanerTarget : INotifyPropertyChanged
    {
        private bool _isSelected = true;
        private string _sizeFormatted = "Calculando...";

        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public long BytesFound { get; set; } = 0;

        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        public string SizeFormatted
        {
            get => _sizeFormatted;
            set { _sizeFormatted = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
}
