using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using GvzkHubOptimizer.Models;
using GvzkHubOptimizer.Services;

namespace GvzkHubOptimizer.ViewModels
{
    public class CleanerViewModel : INotifyPropertyChanged
    {
        private readonly CleanerService _cleanerService = new();
        private bool _isCleaning;
        private string _statusMessage = "Pronto para analisar arquivos temporários.";
        private string _totalFreedFormatted = "0 MB";

        public ObservableCollection<CleanerTarget> Targets { get; } = new();

        public bool IsCleaning
        {
            get => _isCleaning;
            set { _isCleaning = value; OnPropertyChanged(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public string TotalFreedFormatted
        {
            get => _totalFreedFormatted;
            set { _totalFreedFormatted = value; OnPropertyChanged(); }
        }

        public async Task LoadTargetsAndCalculateAsync()
        {
            Targets.Clear();
            foreach (var t in _cleanerService.GetDefaultTargets())
            {
                Targets.Add(t);
            }

            StatusMessage = "Calculando tamanho dos arquivos no disco...";

            long totalFound = 0;
            foreach (var target in Targets)
            {
                long bytes = await _cleanerService.GetTargetSizeBytesAsync(target.Id);
                target.BytesFound = bytes;
                totalFound += bytes;

                if (bytes == 0)
                {
                    target.SizeFormatted = "0 MB (Vazio)";
                }
                else if (bytes < 1024 * 1024)
                {
                    target.SizeFormatted = $"{bytes / 1024} KB";
                }
                else if (bytes < 1024L * 1024L * 1024L)
                {
                    target.SizeFormatted = $"{bytes / (1024.0 * 1024.0):F1} MB";
                }
                else
                {
                    target.SizeFormatted = $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
                }
            }

            double totalMb = Math.Round((double)totalFound / (1024.0 * 1024.0), 1);
            StatusMessage = $"Análise concluída: {totalMb} MB de lixo detectados prontos para limpeza.";
        }

        public async Task CleanSelectedAsync()
        {
            IsCleaning = true;
            long totalFreedBytes = 0;

            foreach (var target in Targets.Where(t => t.IsSelected))
            {
                StatusMessage = $"Limpando: {target.Title}...";
                long freed = await _cleanerService.CleanTargetAsync(target.Id);
                totalFreedBytes += (freed > 0 ? freed : target.BytesFound);
                target.SizeFormatted = "0 MB (Limpo!)";
            }

            double freedMb = Math.Round((double)totalFreedBytes / (1024.0 * 1024.0), 1);
            TotalFreedFormatted = $"{freedMb} MB";
            StatusMessage = $"Limpeza concluída com sucesso! {freedMb} MB liberados.";
            IsCleaning = false;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
}
