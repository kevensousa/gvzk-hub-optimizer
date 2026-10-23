using System.Windows;
using System.Windows.Controls;
using GvzkHubOptimizer.Views;
using Wpf.Ui.Controls;

namespace GvzkHubOptimizer
{
    public partial class MainWindow : FluentWindow
    {
        private readonly HardwarePage _hardwarePage = new();
        private readonly TweaksPage _tweaksPage = new();
        private readonly CleanerPage _cleanerPage = new();
        private readonly UwpPage _uwpPage = new();
        private readonly NetworkPage _networkPage = new();

        public MainWindow()
        {
            InitializeComponent();
            // Inicia na aba de Hardware como solicitado
            MainContent.Content = _hardwarePage;
        }

        private void OnNavChanged(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.Tag is string tag)
            {
                switch (tag)
                {
                    case "Hardware":
                        MainContent.Content = _hardwarePage;
                        break;
                    case "Tweaks":
                        MainContent.Content = _tweaksPage;
                        break;
                    case "Cleaner":
                        MainContent.Content = _cleanerPage;
                        break;
                    case "Uwp":
                        MainContent.Content = _uwpPage;
                        break;
                    case "Network":
                        MainContent.Content = _networkPage;
                        break;
                }
            }
        }
    }
}
