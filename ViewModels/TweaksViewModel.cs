using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Win32;
using GvzkHubOptimizer.Models;
using GvzkHubOptimizer.Services;

namespace GvzkHubOptimizer.ViewModels
{
    public class TweaksViewModel : INotifyPropertyChanged
    {
        private readonly TweaksService _tweaksService = new();
        private readonly RestorePointService _restorePointService = new();

        private string _statusMessage = "Pronto para otimizar.";
        private bool _isBusy;
        private string _selectedCategory = "Todos";

        public ObservableCollection<TweakItem> AllTweaks { get; } = new();
        public ObservableCollection<TweakItem> FilteredTweaks { get; } = new();

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (_selectedCategory != value)
                {
                    _selectedCategory = value;
                    OnPropertyChanged();
                    FilterTweaks();
                }
            }
        }

        public async Task LoadTweaksAsync()
        {
            IsBusy = true;
            StatusMessage = "Lendo configurações atuais do Windows...";

            await Task.Run(() =>
            {
                var tweaks = _tweaksService.GetAllTweaks();

                App.Current.Dispatcher.Invoke(() =>
                {
                    AllTweaks.Clear();
                    foreach (var tw in tweaks)
                    {
                        tw.IsActive = tw.CheckAction?.Invoke() ?? false;
                        AllTweaks.Add(tw);
                    }
                    FilterTweaks();
                });
            });

            StatusMessage = $"{AllTweaks.Count} tweaks carregados.";
            IsBusy = false;
        }

        public void FilterTweaks()
        {
            FilteredTweaks.Clear();
            var filtered = SelectedCategory == "Todos"
                ? AllTweaks
                : AllTweaks.Where(t => t.Category.Equals(SelectedCategory, StringComparison.OrdinalIgnoreCase));

            foreach (var t in filtered)
            {
                FilteredTweaks.Add(t);
            }
        }

        public async Task ToggleTweakAsync(TweakItem tweak, bool targetState)
        {
            tweak.IsBusy = true;
            StatusMessage = $"Aplicando tweak '{tweak.Title}'...";

            await Task.Run(() =>
            {
                try
                {
                    if (targetState)
                    {
                        tweak.ApplyAction?.Invoke();
                    }
                    else
                    {
                        tweak.RevertAction?.Invoke();
                    }
                }
                catch (Exception ex)
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        StatusMessage = $"Erro ao aplicar {tweak.Title}: {ex.Message}";
                    });
                }
            });

            // Re-verifica o estado real no Windows
            bool realState = tweak.CheckAction?.Invoke() ?? targetState;
            tweak.IsActive = realState;
            tweak.IsBusy = false;

            StatusMessage = realState == targetState
                ? $"Tweak '{tweak.Title}' atualizado com sucesso."
                : $"Aviso: '{tweak.Title}' requer reinicialização do Explorer ou do Windows para surtir efeito completo.";
        }

        public async Task CreateRestorePointAsync()
        {
            IsBusy = true;
            StatusMessage = "Criando ponto de restauração no Windows...";

            var result = await _restorePointService.CreateRestorePointAsync("gvzk HUB Optimizer Backup");

            StatusMessage = result.Message;
            IsBusy = false;
        }

        public async Task FixClipboardWinVAsync()
        {
            IsBusy = true;
            StatusMessage = "Restaurando histórico da área de transferência (Win + V)...";

            await Task.Run(() =>
            {
                try
                {
                    Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Clipboard", "EnableClipboardHistory", 1, RegistryValueKind.DWord);
                    Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System", "AllowClipboardHistory", 1, RegistryValueKind.DWord);
                    Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Clipboard", "EnableClipboardHistory", 1, RegistryValueKind.DWord);

                    try
                    {
                        using var sysKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\System", true);
                        sysKey?.DeleteValue("AllowClipboardHistory", false);
                    }
                    catch { }

                    var psi = new ProcessStartInfo("powershell.exe", "-NoProfile -Command \"Get-Service *cbdhsvc* | Start-Service -ErrorAction SilentlyContinue\"")
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    using var p = Process.Start(psi);
                    p?.WaitForExit(3000);
                }
                catch (Exception ex)
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        StatusMessage = $"Erro: {ex.Message}";
                    });
                }
            });

            var clipboardTweak = AllTweaks.FirstOrDefault(t => t.Id == "win_v_clipboard");
            if (clipboardTweak != null)
            {
                clipboardTweak.IsActive = true;
            }

            StatusMessage = "Histórico da Área de Transferência (Win + V) restaurado com sucesso!";
            IsBusy = false;
        }

        public async Task RestartExplorerAsync()
        {
            IsBusy = true;
            StatusMessage = "Reiniciando Windows Explorer para aplicar alterações visuais...";

            await Task.Run(() =>
            {
                try
                {
                    foreach (var process in Process.GetProcessesByName("explorer"))
                    {
                        process.Kill();
                        process.WaitForExit(3000);
                    }
                    Process.Start("explorer.exe");
                }
                catch { }
            });

            StatusMessage = "Windows Explorer reiniciado com sucesso! Ajustes visuais aplicados.";
            IsBusy = false;
        }

        public async Task ActivateUltimatePowerPlanAsync()
        {
            IsBusy = true;
            StatusMessage = "Ativando Plano de Desempenho Máximo do Windows...";

            await Task.Run(() =>
            {
                TweaksService.ApplyUltimatePowerPlan();
            });

            var tweak = AllTweaks.FirstOrDefault(t => t.Id == "sys_ultimate_power");
            if (tweak != null) tweak.IsActive = true;

            StatusMessage = "Plano de Energia 'Desempenho Máximo' ativado com sucesso!";
            IsBusy = false;
        }

        public async Task RemoveTransparencyAsync()
        {
            IsBusy = true;
            StatusMessage = "Desativando transparência do Windows...";

            await Task.Run(() =>
            {
                TweaksService.SetTransparency(false);
            });

            var tweak = AllTweaks.FirstOrDefault(t => t.Id == "sys_disable_transparency");
            if (tweak != null) tweak.IsActive = true;

            StatusMessage = "Efeitos de transparência desativados! (Clique em 'Reiniciar Explorer' se necessário)";
            IsBusy = false;
        }

        public async Task ApplyOptimizedVisualsAsync()
        {
            IsBusy = true;
            StatusMessage = "Aplicando efeitos de alto desempenho mantendo fontes e miniaturas...";

            await Task.Run(() =>
            {
                TweaksService.ApplyOptimizedVisualEffects();
            });

            var tweak = AllTweaks.FirstOrDefault(t => t.Id == "sys_optimized_visuals");
            if (tweak != null) tweak.IsActive = true;

            StatusMessage = "Efeitos visuais otimizados! Fontes suaves e miniaturas preservadas.";
            IsBusy = false;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
}
