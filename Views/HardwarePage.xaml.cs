using System.Windows;
using System.Windows.Controls;
using GvzkHubOptimizer.ViewModels;

namespace GvzkHubOptimizer.Views
{
    public partial class HardwarePage : UserControl
    {
        public HardwareViewModel ViewModel { get; }

        public HardwarePage()
        {
            InitializeComponent();
            ViewModel = new HardwareViewModel();
            DataContext = ViewModel;

            Loaded += async (s, e) =>
            {
                await ViewModel.LoadHardwareAsync();
            };
        }

        private void OnToggleAdvancedClick(object sender, RoutedEventArgs e)
        {
            ViewModel.ToggleAdvancedSpecs();
        }
    }
}
