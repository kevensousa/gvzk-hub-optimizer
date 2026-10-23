using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using GvzkHubOptimizer.Models;
using GvzkHubOptimizer.Services;

namespace GvzkHubOptimizer.ViewModels
{
    public class HardwareViewModel : INotifyPropertyChanged
    {
        private readonly HardwareService _hardwareService = new();
        private HardwareSummary _summary = new();
        private bool _isLoading = true;
        private bool _isAdvancedOpen = false;

        public HardwareSummary Summary
        {
            get => _summary;
            set { _summary = value; OnPropertyChanged(); }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public bool IsAdvancedOpen
        {
            get => _isAdvancedOpen;
            set { _isAdvancedOpen = value; OnPropertyChanged(); }
        }

        public ObservableCollection<DiskInfo> Disks { get; } = new();

        public async Task LoadHardwareAsync()
        {
            IsLoading = true;
            Summary = await _hardwareService.GetHardwareSummaryAsync();

            Disks.Clear();
            foreach (var d in Summary.Disks)
            {
                Disks.Add(d);
            }

            IsLoading = false;
        }

        public void ToggleAdvancedSpecs()
        {
            IsAdvancedOpen = !IsAdvancedOpen;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
}
