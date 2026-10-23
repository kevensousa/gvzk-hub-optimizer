using System.Windows;
using System.Windows.Controls;
using GvzkHubOptimizer.ViewModels;

namespace GvzkHubOptimizer.Views
{
    public partial class NetworkPage : UserControl
    {
        public NetworkViewModel ViewModel { get; }

        public NetworkPage()
        {
            InitializeComponent();
            ViewModel = new NetworkViewModel();
            DataContext = ViewModel;

            Loaded += (s, e) =>
            {
                if (ViewModel.DnsPresets.Count == 0)
                {
                    ViewModel.LoadDnsPresets();
                }
            };
        }

        private async void OnStartSpeedTestClick(object sender, RoutedEventArgs e)
        {
            await ViewModel.RunSpeedTestAsync();
        }

        private async void OnApplyDnsClick(object sender, RoutedEventArgs e)
        {
            await ViewModel.ApplySelectedDnsAsync();
        }

        private async void OnFlushDnsClick(object sender, RoutedEventArgs e)
        {
            await ViewModel.FlushDnsAsync();
        }
    }
}
