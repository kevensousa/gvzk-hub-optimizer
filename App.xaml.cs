using System;
using System.Windows;

namespace GvzkHubOptimizer
{
    public partial class App : Application
    {
        public App()
        {
            DispatcherUnhandledException += (s, e) =>
            {
                MessageBox.Show($"Ocorreu um erro inesperado:\n\n{e.Exception.Message}\n\nStack:\n{e.Exception.StackTrace}", 
                                "gvzk HUB Optimizer - Erro", 
                                MessageBoxButton.OK, 
                                MessageBoxImage.Error);
                e.Handled = true;
            };

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                {
                    MessageBox.Show($"Erro fatal na aplicação:\n\n{ex.Message}\n\nStack:\n{ex.StackTrace}", 
                                    "gvzk HUB Optimizer - Erro Fatal", 
                                    MessageBoxButton.OK, 
                                    MessageBoxImage.Error);
                }
            };
        }
    }
}
