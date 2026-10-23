using System.Windows;
using System.Windows.Controls;
using GvzkHubOptimizer.Models;
using GvzkHubOptimizer.ViewModels;

namespace GvzkHubOptimizer.Views
{
    public partial class TweaksPage : UserControl
    {
        public TweaksViewModel ViewModel { get; }

        public TweaksPage()
        {
            InitializeComponent();
            ViewModel = new TweaksViewModel();
            DataContext = ViewModel;

            Loaded += async (s, e) =>
            {
                if (ViewModel.AllTweaks.Count == 0)
                {
                    await ViewModel.LoadTweaksAsync();
                }
            };
        }

        private async void OnTweakToggleClick(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is TweakItem tweak)
            {
                await ViewModel.ToggleTweakAsync(tweak, tweak.IsActive);
            }
        }

        private async void OnCreateRestorePointClick(object sender, RoutedEventArgs e)
        {
            await ViewModel.CreateRestorePointAsync();
        }

        private async void OnFixClipboardClick(object sender, RoutedEventArgs e)
        {
            await ViewModel.FixClipboardWinVAsync();
        }

        private async void OnUltimatePowerClick(object sender, RoutedEventArgs e)
        {
            await ViewModel.ActivateUltimatePowerPlanAsync();
        }

        private async void OnRemoveTransparencyClick(object sender, RoutedEventArgs e)
        {
            await ViewModel.RemoveTransparencyAsync();
        }

        private async void OnOptimizedVisualsClick(object sender, RoutedEventArgs e)
        {
            await ViewModel.ApplyOptimizedVisualsAsync();
        }

        private async void OnRestartExplorerClick(object sender, RoutedEventArgs e)
        {
            await ViewModel.RestartExplorerAsync();
        }

        private void OnCategoryClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string category)
            {
                ViewModel.SelectedCategory = category;
            }
        }
    }
}
