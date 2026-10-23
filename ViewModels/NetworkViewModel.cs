using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using GvzkHubOptimizer.Services;

namespace GvzkHubOptimizer.ViewModels
{
    public class NetworkViewModel : INotifyPropertyChanged
    {
        private readonly SpeedTestService _speedTestService = new();
        private readonly DnsService _dnsService = new();

        private bool _isTesting;
        private string _pingDisplay = "-- ms";
        private string _downloadDisplay = "-- Mbps";
        private string _uploadDisplay = "-- Mbps";
        private string _testStatus = "Pronto para iniciar o teste de velocidade.";

        private DnsPreset? _selectedDns;
        private string _dnsStatus = "Escolha um DNS para aplicar ou restaurar o automático.";

        public ObservableCollection<DnsPreset> DnsPresets { get; } = new();

        public bool IsTesting
        {
            get => _isTesting;
            set { _isTesting = value; OnPropertyChanged(); }
        }

        public string PingDisplay
        {
            get => _pingDisplay;
            set { _pingDisplay = value; OnPropertyChanged(); }
        }

        public string DownloadDisplay
        {
            get => _downloadDisplay;
            set { _downloadDisplay = value; OnPropertyChanged(); }
        }

        public string UploadDisplay
        {
            get => _uploadDisplay;
            set { _uploadDisplay = value; OnPropertyChanged(); }
        }

        public string TestStatus
        {
            get => _testStatus;
            set { _testStatus = value; OnPropertyChanged(); }
        }

        public DnsPreset? SelectedDns
        {
            get => _selectedDns;
            set { _selectedDns = value; OnPropertyChanged(); }
        }

        public string DnsStatus
        {
            get => _dnsStatus;
            set { _dnsStatus = value; OnPropertyChanged(); }
        }

        public void LoadDnsPresets()
        {
            DnsPresets.Clear();
            var presets = _dnsService.GetPresets();
            foreach (var p in presets)
            {
                DnsPresets.Add(p);
            }
            if (DnsPresets.Count > 0) SelectedDns = DnsPresets[0];
        }

        public async Task RunSpeedTestAsync()
        {
            IsTesting = true;
            TestStatus = "1/3 - Medindo latência (Ping)...";
            PingDisplay = "-- ms";
            DownloadDisplay = "-- Mbps";
            UploadDisplay = "-- Mbps";

            // 1. Ping
            double ping = await _speedTestService.TestPingAsync("1.1.1.1");
            PingDisplay = $"{ping} ms";

            // 2. Download
            TestStatus = "2/3 - Testando velocidade de Download...";
            double download = await _speedTestService.TestDownloadSpeedAsync(mbps =>
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    DownloadDisplay = $"{mbps} Mbps";
                });
            });
            DownloadDisplay = $"{download} Mbps";

            // 3. Upload
            TestStatus = "3/3 - Testando velocidade de Upload...";
            double upload = await _speedTestService.TestUploadSpeedAsync();
            UploadDisplay = $"{upload} Mbps";

            TestStatus = "Teste de conexão concluído com sucesso!";
            IsTesting = false;
        }

        public async Task ApplySelectedDnsAsync()
        {
            if (SelectedDns == null) return;

            DnsStatus = $"Aplicando DNS: {SelectedDns.Name}...";
            var result = await _dnsService.ApplyDnsAsync(SelectedDns);
            DnsStatus = result.Message;
        }

        public async Task FlushDnsAsync()
        {
            DnsStatus = "Limpando cache de DNS...";
            var result = await _dnsService.FlushDnsAsync();
            DnsStatus = result;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
}
