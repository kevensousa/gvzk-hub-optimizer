using System.Windows;
using System.Windows.Controls;
using GvzkHubOptimizer.ViewModels;

namespace GvzkHubOptimizer.Views
{
    public partial class UwpPage : UserControl
    {
        public UwpViewModel ViewModel { get; }

        public UwpPage()
        {
            InitializeComponent();
            ViewModel = new UwpViewModel();
            DataContext = ViewModel;

            Loaded += async (s, e) =>
            {
                if (ViewModel.Apps.Count == 0)
                {
                    await ViewModel.LoadAppsAsync();
                }
            };
        }

        private async void OnScanClick(object sender, RoutedEventArgs e)
        {
            await ViewModel.LoadAppsAsync();
        }

        private async void OnUninstallClick(object sender, RoutedEventArgs e)
        {
            await ViewModel.UninstallSelectedAsync();
        }
    }
}
