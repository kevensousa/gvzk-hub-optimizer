using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using GvzkHubOptimizer.Models;
using GvzkHubOptimizer.Services;

namespace GvzkHubOptimizer.ViewModels
{
    public class UwpViewModel : INotifyPropertyChanged
    {
        private readonly UwpService _uwpService = new();
        private bool _isLoading;
        private bool _isUninstalling;
        private string _statusMessage = "Pronto para buscar bloatwares instalados.";

        public ObservableCollection<UwpAppItem> Apps { get; } = new();

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public bool IsUninstalling
        {
            get => _isUninstalling;
            set { _isUninstalling = value; OnPropertyChanged(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public async Task LoadAppsAsync()
        {
            IsLoading = true;
            StatusMessage = "Escaneando aplicativos UWP instalados...";

            Apps.Clear();
            var detected = await _uwpService.GetInstalledBloatwareAsync();
            foreach (var app in detected)
            {
                Apps.Add(app);
            }

            StatusMessage = Apps.Count > 0 ? $"{Apps.Count} aplicativos encontrados para remoção segura." : "Nenhum bloatware padrão encontrado!";
            IsLoading = false;
        }

        public async Task UninstallSelectedAsync()
        {
            var selected = Apps.Where(a => a.IsSelected).ToList();
            if (selected.Count == 0)
            {
                StatusMessage = "Selecione pelo menos um aplicativo para desinstalar.";
                return;
            }

            IsUninstalling = true;
            int count = 0;

            foreach (var app in selected)
            {
                StatusMessage = $"Removendo {app.DisplayName}...";
                bool removed = await _uwpService.UninstallAppAsync(app.PackageName);
                if (removed)
                {
                    Apps.Remove(app);
                    count++;
                }
            }

            StatusMessage = $"{count} aplicativo(s) removido(s) com sucesso!";
            IsUninstalling = false;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
}
