using System.Windows;
using System.Windows.Controls;
using GvzkHubOptimizer.ViewModels;

namespace GvzkHubOptimizer.Views
{
    public partial class CleanerPage : UserControl
    {
        public CleanerViewModel ViewModel { get; }

        public CleanerPage()
        {
            InitializeComponent();
            ViewModel = new CleanerViewModel();
            DataContext = ViewModel;

            Loaded += async (s, e) =>
            {
                if (ViewModel.Targets.Count == 0)
                {
                    await ViewModel.LoadTargetsAndCalculateAsync();
                }
            };
        }

        private async void OnCleanNowClick(object sender, RoutedEventArgs e)
        {
            await ViewModel.CleanSelectedAsync();
        }
    }
}
