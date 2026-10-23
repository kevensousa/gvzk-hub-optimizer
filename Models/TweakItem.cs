using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GvzkHubOptimizer.Models
{
    public enum RestartType
    {
        None,
        Explorer,
        System
    }

    public class TweakItem : INotifyPropertyChanged
    {
        private bool _isActive;
        private bool _isBusy;

        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = "Sistema";
        public bool IsRecommended { get; set; } = true;

        public RestartType RestartRequirement { get; set; } = RestartType.None;

        public string RestartBadgeText => RestartRequirement switch
        {
            RestartType.Explorer => "🔄 Requer Reiniciar Explorer",
            RestartType.System => "💻 Requer Reiniciar PC",
            _ => "⚡ Imediato"
        };

        public string RestartBadgeColor => RestartRequirement switch
        {
            RestartType.Explorer => "#f39c12", // Laranja / Amber
            RestartType.System => "#9b5de5",   // Roxo / Azul
            _ => "#2da44e"                     // Verde
        };

        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive != value)
                {
                    _isActive = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    OnPropertyChanged();
                }
            }
        }

        public Action? ApplyAction { get; set; }
        public Action? RevertAction { get; set; }
        public Func<bool>? CheckAction { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
