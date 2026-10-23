using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using GvzkHubOptimizer.Models;

namespace GvzkHubOptimizer.Services
{
    public enum ServiceMode
    {
        Automatic,
        Manual,
        Disabled
    }

    public class TweaksService
    {
        public List<TweakItem> GetAllTweaks()
        {
            var list = new List<TweakItem>();

            // ==================== ÁREA DE TRANSFERÊNCIA / PRODUTIVIDADE (WINDOWS + V) ====================
            list.Add(new TweakItem
            {
                Id = "win_v_clipboard",
                Category = "Produtividade",
                Title = "Manter Histórico da Área de Transferência (Windows + V)",
                Description = "Garante que o atalho Win + V e o histórico de cópias funcionem sem serem bloqueados por políticas de telemetria.",
                IsRecommended = true,
                RestartRequirement = RestartType.None,
                CheckAction = () =>
                {
                    try
                    {
                        var userVal = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Clipboard", "EnableClipboardHistory", 1);
                        var policyVal = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System", "AllowClipboardHistory", 1);
                        var lmVal = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Clipboard", "EnableClipboardHistory", 1);

                        return Convert.ToInt32(userVal ?? 1) == 1 &&
                               Convert.ToInt32(policyVal ?? 1) != 0 &&
                               Convert.ToInt32(lmVal ?? 1) != 0;
                    }
                    catch { return true; }
                },
                ApplyAction = () =>
                {
                    // Ativa Win + V em todas as chaves (Usuário e Máquina)
                    SetRegistryValue(Registry.CurrentUser, @"Software\Microsoft\Clipboard", "EnableClipboardHistory", 1, RegistryValueKind.DWord);
                    SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\System", "AllowClipboardHistory", 1, RegistryValueKind.DWord);
                    SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Microsoft\Clipboard", "EnableClipboardHistory", 1, RegistryValueKind.DWord);

                    // Tenta remover a restrição de política se existir
                    try
                    {
                        using var sysKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\System", true);
                        sysKey?.DeleteValue("AllowClipboardHistory", false);
                    }
                    catch { }

                    // Reinicia o serviço de Clipboard do usuário
                    RunProcessHidden("powershell.exe", "-NoProfile -Command \"Get-Service *cbdhsvc* | Start-Service -ErrorAction SilentlyContinue\"");
                },
                RevertAction = () =>
                {
                    SetRegistryValue(Registry.CurrentUser, @"Software\Microsoft\Clipboard", "EnableClipboardHistory", 0, RegistryValueKind.DWord);
                }
            });

            // ==================== SISTEMA ====================
            list.Add(new TweakItem
            {
                Id = "sys_ultimate_power",
                Category = "Sistema",
                Title = "Ativar Plano de Energia de Desempenho Máximo",
                Description = "Ativa o plano de alto rendimento oculto do Windows (Ultimate Performance), eliminando latências e mantendo a CPU na frequência máxima.",
                IsRecommended = true,
                RestartRequirement = RestartType.None,
                CheckAction = () => CheckUltimatePowerPlanActive(),
                ApplyAction = () => ApplyUltimatePowerPlan(),
                RevertAction = () => RunProcessHidden("powercfg", "/setactive 381b4222-f694-41f0-9685-ff5bb260df2e") // Volta para equilibrado
            });

            list.Add(new TweakItem
            {
                Id = "sys_disable_transparency",
                Category = "Sistema",
                Title = "Retirar Transparência do Windows",
                Description = "Desativa efeitos de transparência e desfoque acrílico das janelas e da barra de tarefas para economizar uso de GPU e VRAM.",
                IsRecommended = true,
                RestartRequirement = RestartType.Explorer,
                CheckAction = () => CheckTransparencyDisabled(),
                ApplyAction = () => SetTransparency(false),
                RevertAction = () => SetTransparency(true)
            });

            list.Add(new TweakItem
            {
                Id = "sys_optimized_visuals",
                Category = "Sistema",
                Title = "Ajustar Efeitos Visuais para Desempenho (Fontes e Miniaturas Preservadas)",
                Description = "Define os efeitos visuais para melhor desempenho, desativando animações e sombras, mas mantendo as fontes com cantos arredondados e exibindo miniaturas em vez de ícones.",
                IsRecommended = true,
                RestartRequirement = RestartType.Explorer,
                CheckAction = () => CheckOptimizedVisualEffectsApplied(),
                ApplyAction = () => ApplyOptimizedVisualEffects(),
                RevertAction = () => SetRegistryValue(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", "VisualFXSetting", 0, RegistryValueKind.DWord)
            });

            list.Add(new TweakItem
            {
                Id = "sys_performance_tweaks",
                Category = "Sistema",
                Title = "Habilitar ajustes de desempenho",
                Description = "Ajusta as configurações visuais do Windows para priorizar resposta rápida do sistema.",
                IsRecommended = true,
                RestartRequirement = RestartType.Explorer,
                CheckAction = () => CheckRegistryDWord(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", "VisualFXSetting", 2),
                ApplyAction = () => SetRegistryValue(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", "VisualFXSetting", 2, RegistryValueKind.DWord),
                RevertAction = () => SetRegistryValue(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", "VisualFXSetting", 0, RegistryValueKind.DWord)
            });

            list.Add(new TweakItem
            {
                Id = "sys_menu_delay",
                Category = "Sistema",
                Title = "Remover atraso de menus",
                Description = "Reduz o tempo de resposta e abertura de menus de contexto do sistema operacional para 0ms.",
                IsRecommended = true,
                RestartRequirement = RestartType.Explorer,
                CheckAction = () => CheckRegistryString(Registry.CurrentUser, @"Control Panel\Desktop", "MenuShowDelay", "0"),
                ApplyAction = () => SetRegistryValue(Registry.CurrentUser, @"Control Panel\Desktop", "MenuShowDelay", "0", RegistryValueKind.String),
                RevertAction = () => SetRegistryValue(Registry.CurrentUser, @"Control Panel\Desktop", "MenuShowDelay", "400", RegistryValueKind.String)
            });

            list.Add(new TweakItem
            {
                Id = "sys_network_throttling",
                Category = "Sistema",
                Title = "Desativar limitações de rede",
                Description = "Desativa o limitador de rede padrão do Windows para multimídia e downloads (NetworkThrottlingIndex).",
                IsRecommended = true,
                RestartRequirement = RestartType.System,
                CheckAction = () => CheckRegistryDWord(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "NetworkThrottlingIndex", unchecked((int)0xFFFFFFFF)),
                ApplyAction = () =>
                {
                    SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "NetworkThrottlingIndex", unchecked((int)0xFFFFFFFF), RegistryValueKind.DWord);
                    SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "SystemResponsiveness", 0, RegistryValueKind.DWord);
                },
                RevertAction = () =>
                {
                    SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "NetworkThrottlingIndex", 10, RegistryValueKind.DWord);
                    SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "SystemResponsiveness", 20, RegistryValueKind.DWord);
                }
            });

            list.Add(new TweakItem
            {
                Id = "sys_error_reporting",
                Category = "Sistema",
                Title = "Desativar Serviço de Relatório de Erros",
                Description = "Impede que o Windows gaste recursos em segundo plano coletando e enviando logs de falhas.",
                IsRecommended = true,
                RestartRequirement = RestartType.System,
                CheckAction = () => CheckRegistryDWord(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\Windows Error Reporting", "Disabled", 1),
                ApplyAction = () =>
                {
                    SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\Windows Error Reporting", "Disabled", 1, RegistryValueKind.DWord);
                    SetServiceStartup("WerSvc", ServiceMode.Disabled);
                },
                RevertAction = () =>
                {
                    SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\Windows Error Reporting", "Disabled", 0, RegistryValueKind.DWord);
                    SetServiceStartup("WerSvc", ServiceMode.Manual);
                }
            });

            list.Add(new TweakItem
            {
                Id = "sys_pca",
                Category = "Sistema",
                Title = "Desativar o Assistente de Compatibilidade",
                Description = "Desativa alertas e verificações desnecessárias de programas legados ao abrir instaladores.",
                IsRecommended = true,
                RestartRequirement = RestartType.System,
                CheckAction = () => CheckServiceDisabled("PcaSvc"),
                ApplyAction = () => SetServiceStartup("PcaSvc", ServiceMode.Disabled),
                RevertAction = () => SetServiceStartup("PcaSvc", ServiceMode.Manual)
            });

            list.Add(new TweakItem
            {
                Id = "sys_fax",
                Category = "Sistema",
                Title = "Desabilitar Serviço de Fax",
                Description = "Desativa serviço inútil de Fax que consome recursos de sistema.",
                IsRecommended = true,
                RestartRequirement = RestartType.System,
                CheckAction = () => CheckServiceDisabled("Fax"),
                ApplyAction = () => SetServiceStartup("Fax", ServiceMode.Disabled),
                RevertAction = () => SetServiceStartup("Fax", ServiceMode.Manual)
            });

            list.Add(new TweakItem
            {
                Id = "sys_sticky_keys",
                Category = "Sistema",
                Title = "Desabilitar as Teclas de Aderência",
                Description = "Evita o popup irritante de teclas de aderência ao pressionar Shift 5 vezes repetidas.",
                IsRecommended = true,
                RestartRequirement = RestartType.None,
                CheckAction = () => CheckRegistryString(Registry.CurrentUser, @"Control Panel\Accessibility\StickyKeys", "Flags", "506"),
                ApplyAction = () => SetRegistryValue(Registry.CurrentUser, @"Control Panel\Accessibility\StickyKeys", "Flags", "506", RegistryValueKind.String),
                RevertAction = () => SetRegistryValue(Registry.CurrentUser, @"Control Panel\Accessibility\StickyKeys", "Flags", "510", RegistryValueKind.String)
            });

            list.Add(new TweakItem
            {
                Id = "sys_smartscreen",
                Category = "Sistema",
                Title = "Desabilitar o SmartScreen",
                Description = "Desativa verificações de segurança em nuvem da Microsoft para arquivos baixados.",
                IsRecommended = false,
                RestartRequirement = RestartType.System,
                CheckAction = () => CheckRegistryString(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer", "SmartScreenEnabled", "Off"),
                ApplyAction = () => SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer", "SmartScreenEnabled", "Off", RegistryValueKind.String),
                RevertAction = () => SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer", "SmartScreenEnabled", "RequireAdmin", RegistryValueKind.String)
            });

            list.Add(new TweakItem
            {
                Id = "sys_long_paths",
                Category = "Sistema",
                Title = "Habilitar Caminhos Longos",
                Description = "Remove o limite clássico de 260 caracteres em caminhos de arquivos do Windows.",
                IsRecommended = true,
                RestartRequirement = RestartType.System,
                CheckAction = () => CheckRegistryDWord(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\FileSystem", "LongPathsEnabled", 1),
                ApplyAction = () => SetRegistryValue(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\FileSystem", "LongPathsEnabled", 1, RegistryValueKind.DWord),
                RevertAction = () => SetRegistryValue(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\FileSystem", "LongPathsEnabled", 0, RegistryValueKind.DWord)
            });

            list.Add(new TweakItem
            {
                Id = "sys_classic_context",
                Category = "Extras",
                Title = "Habilitar Menu Clássico do Botão Direito",
                Description = "Restaura o menu de contexto completo clássico no Windows 11 sem precisar clicar em 'Mostrar mais opções'.",
                IsRecommended = true,
                RestartRequirement = RestartType.Explorer,
                CheckAction = () => Registry.CurrentUser.OpenSubKey(@"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32") != null,
                ApplyAction = () =>
                {
                    using var k = Registry.CurrentUser.CreateSubKey(@"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32");
                    k?.SetValue("", "");
                },
                RevertAction = () =>
                {
                    try { Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}"); } catch { }
                }
            });

            list.Add(new TweakItem
            {
                Id = "sys_copilot",
                Category = "Extras",
                Title = "Desativar CoPilot AI",
                Description = "Desativa a integração nativa e atalhos do Windows CoPilot.",
                IsRecommended = true,
                RestartRequirement = RestartType.Explorer,
                CheckAction = () => CheckRegistryDWord(Registry.CurrentUser, @"Software\Policies\Microsoft\Windows\WindowsCopilot", "TurnOffWindowsCopilot", 1),
                ApplyAction = () => SetRegistryValue(Registry.CurrentUser, @"Software\Policies\Microsoft\Windows\WindowsCopilot", "TurnOffWindowsCopilot", 1, RegistryValueKind.DWord),
                RevertAction = () => SetRegistryValue(Registry.CurrentUser, @"Software\Policies\Microsoft\Windows\WindowsCopilot", "TurnOffWindowsCopilot", 0, RegistryValueKind.DWord)
            });

            // ==================== PRIVACIDADE & TELEMETRIA ====================
            list.Add(new TweakItem
            {
                Id = "priv_telemetry_svc",
                Category = "Privacidade",
                Title = "Desativar Serviços de Telemetria",
                Description = "Desativa serviços de rastreamento e telemetria (DiagTrack e dmwappushservice).",
                IsRecommended = true,
                CheckAction = () => CheckServiceDisabled("DiagTrack"),
                ApplyAction = () =>
                {
                    SetServiceStartup("DiagTrack", ServiceMode.Disabled);
                    SetServiceStartup("dmwappushservice", ServiceMode.Disabled);
                },
                RevertAction = () =>
                {
                    SetServiceStartup("DiagTrack", ServiceMode.Automatic);
                    SetServiceStartup("dmwappushservice", ServiceMode.Manual);
                }
            });

            list.Add(new TweakItem
            {
                Id = "priv_cortana",
                Category = "Privacidade",
                Title = "Desabilitar a Assistência da Cortana",
                Description = "Desativa recursos de voz e coleta em segundo plano da assistente Cortana.",
                IsRecommended = true,
                CheckAction = () => CheckRegistryDWord(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowCortana", 0),
                ApplyAction = () => SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowCortana", 0, RegistryValueKind.DWord),
                RevertAction = () => SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowCortana", 1, RegistryValueKind.DWord)
            });

            list.Add(new TweakItem
            {
                Id = "priv_ads_start",
                Category = "Privacidade",
                Title = "Desativar anúncios do Menu Iniciar",
                Description = "Bloqueia sugestões de apps e anúncios promovidos pela Microsoft no Menu Iniciar.",
                IsRecommended = true,
                CheckAction = () => CheckRegistryDWord(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SystemPaneSuggestionsEnabled", 0),
                ApplyAction = () => SetRegistryValue(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SystemPaneSuggestionsEnabled", 0, RegistryValueKind.DWord),
                RevertAction = () => SetRegistryValue(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SystemPaneSuggestionsEnabled", 1, RegistryValueKind.DWord)
            });

            list.Add(new TweakItem
            {
                Id = "priv_edge_telemetry",
                Category = "Privacidade",
                Title = "Desativar telemetria de borda (Edge)",
                Description = "Bloqueia o envio de métricas de navegação e diagnóstico do Microsoft Edge.",
                IsRecommended = true,
                CheckAction = () => CheckRegistryDWord(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Edge", "MetricsReportingEnabled", 0),
                ApplyAction = () => SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Edge", "MetricsReportingEnabled", 0, RegistryValueKind.DWord),
                RevertAction = () => SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Edge", "MetricsReportingEnabled", 1, RegistryValueKind.DWord)
            });

            // ==================== APLICATIVOS ====================
            list.Add(new TweakItem
            {
                Id = "app_office_telemetry",
                Category = "Aplicativos",
                Title = "Desabilitar Telemetria para o Office",
                Description = "Bloqueia diagnóstico e relatórios de uso em pacotes Microsoft Office / 365.",
                IsRecommended = true,
                CheckAction = () => CheckRegistryDWord(Registry.CurrentUser, @"Software\Policies\Microsoft\Office\Common\Privacy", "DisconnectedState", 2),
                ApplyAction = () => SetRegistryValue(Registry.CurrentUser, @"Software\Policies\Microsoft\Office\Common\Privacy", "DisconnectedState", 2, RegistryValueKind.DWord),
                RevertAction = () => SetRegistryValue(Registry.CurrentUser, @"Software\Policies\Microsoft\Office\Common\Privacy", "DisconnectedState", 1, RegistryValueKind.DWord)
            });

            list.Add(new TweakItem
            {
                Id = "app_chrome_telemetry",
                Category = "Aplicativos",
                Title = "Desativar a Telemetria do Chrome",
                Description = "Desativa o envio automático de relatórios de métricas e falhas do Google Chrome.",
                IsRecommended = true,
                CheckAction = () => CheckRegistryDWord(Registry.LocalMachine, @"SOFTWARE\Policies\Google\Chrome", "MetricsReportingEnabled", 0),
                ApplyAction = () => SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Google\Chrome", "MetricsReportingEnabled", 0, RegistryValueKind.DWord),
                RevertAction = () => SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Google\Chrome", "MetricsReportingEnabled", 1, RegistryValueKind.DWord)
            });

            list.Add(new TweakItem
            {
                Id = "app_nvidia_telemetry",
                Category = "Aplicativos",
                Title = "Desativar a telemetria NVIDIA",
                Description = "Desativa tarefas em segundo plano de coleta do driver GeForce.",
                IsRecommended = true,
                CheckAction = () =>
                {
                    try
                    {
                        var psi = new ProcessStartInfo("schtasks", "/query /tn NvTmMon") { CreateNoWindow = true, UseShellExecute = false, RedirectStandardOutput = true };
                        using var p = Process.Start(psi);
                        var output = p?.StandardOutput.ReadToEnd() ?? "";
                        return output.Contains("Disabled") || output.Contains("Desabilitada");
                    }
                    catch { return false; }
                },
                ApplyAction = () =>
                {
                    RunProcessHidden("schtasks", "/change /tn NvTmMon /disable");
                    RunProcessHidden("schtasks", "/change /tn NvTmRep /disable");
                },
                RevertAction = () =>
                {
                    RunProcessHidden("schtasks", "/change /tn NvTmMon /enable");
                    RunProcessHidden("schtasks", "/change /tn NvTmRep /enable");
                }
            });

            // ==================== JOGOS ====================
            list.Add(new TweakItem
            {
                Id = "game_mode",
                Category = "Jogos",
                Title = "Ativar o modo de jogo",
                Description = "Garante que o Windows Game Mode priorize recursos da CPU e GPU para os seus jogos.",
                IsRecommended = true,
                CheckAction = () => CheckRegistryDWord(Registry.CurrentUser, @"Software\Microsoft\GameBar", "AllowAutoGameMode", 1),
                ApplyAction = () => SetRegistryValue(Registry.CurrentUser, @"Software\Microsoft\GameBar", "AllowAutoGameMode", 1, RegistryValueKind.DWord),
                RevertAction = () => SetRegistryValue(Registry.CurrentUser, @"Software\Microsoft\GameBar", "AllowAutoGameMode", 0, RegistryValueKind.DWord)
            });

            list.Add(new TweakItem
            {
                Id = "game_dvr_bar",
                Category = "Jogos",
                Title = "Desabilitar Xbox Game Bar",
                Description = "Desativa a sobreposição da Game Bar (Win+G) para reduzir input lag e consumo de RAM.",
                IsRecommended = false,
                CheckAction = () => CheckRegistryDWord(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\GameDVR", "AppCaptureEnabled", 0),
                ApplyAction = () => SetRegistryValue(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\GameDVR", "AppCaptureEnabled", 0, RegistryValueKind.DWord),
                RevertAction = () => SetRegistryValue(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\GameDVR", "AppCaptureEnabled", 1, RegistryValueKind.DWord)
            });

            // ==================== WINDOWS UPDATE (AS 3 PRIMEIRAS) ====================
            list.Add(new TweakItem
            {
                Id = "wu_auto_updates",
                Category = "Windows Update",
                Title = "Desativar Atualizações Automáticas",
                Description = "Impede que o Windows baixe e reinicie o computador inesperadamente durante o uso.",
                IsRecommended = false,
                CheckAction = () => CheckRegistryDWord(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "NoAutoUpdate", 1),
                ApplyAction = () => SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "NoAutoUpdate", 1, RegistryValueKind.DWord),
                RevertAction = () => SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "NoAutoUpdate", 0, RegistryValueKind.DWord)
            });

            list.Add(new TweakItem
            {
                Id = "wu_store_updates",
                Category = "Windows Update",
                Title = "Desativar atualizações da Microsoft Store",
                Description = "Impede que a Microsoft Store atualize aplicativos pré-instalados em segundo plano.",
                IsRecommended = false,
                CheckAction = () => CheckRegistryDWord(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\WindowsStore", "AutoDownload", 2),
                ApplyAction = () => SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\WindowsStore", "AutoDownload", 2, RegistryValueKind.DWord),
                RevertAction = () => SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\WindowsStore", "AutoDownload", 4, RegistryValueKind.DWord)
            });

            list.Add(new TweakItem
            {
                Id = "wu_insider_svc",
                Category = "Windows Update",
                Title = "Desabilitar Windows Insider Service",
                Description = "Desativa o serviço de testes de versões preliminares da Microsoft.",
                IsRecommended = true,
                CheckAction = () => CheckServiceDisabled("wisvc"),
                ApplyAction = () => SetServiceStartup("wisvc", ServiceMode.Disabled),
                RevertAction = () => SetServiceStartup("wisvc", ServiceMode.Manual)
            });

            return list;
        }

        #region Helpers de Registro, Energia e Efeitos

        public static void ApplyUltimatePowerPlan()
        {
            try
            {
                var psi = new ProcessStartInfo("cmd.exe", "/c \"for /f \"tokens=4\" %a in ('powercfg /l ^| findstr /i \"Desempenho M Ultimate\"') do (powercfg /setactive %a & exit)\"")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                using var p = Process.Start(psi);
                p?.WaitForExit(3000);

                var psiDup = new ProcessStartInfo("cmd.exe", "/c \"powercfg -duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61 & for /f \"tokens=4\" %a in ('powercfg /l ^| findstr /i \"Desempenho M Ultimate\"') do (powercfg /setactive %a & exit)\"")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                using var p2 = Process.Start(psiDup);
                p2?.WaitForExit(3000);
            }
            catch { }
        }

        public static bool CheckUltimatePowerPlanActive()
        {
            try
            {
                var psi = new ProcessStartInfo("powercfg", "/getactivescheme")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };
                using var p = Process.Start(psi);
                var output = p?.StandardOutput.ReadToEnd() ?? "";
                p?.WaitForExit(2000);
                return output.Contains("Desempenho M", StringComparison.OrdinalIgnoreCase) ||
                       output.Contains("Ultimate", StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }

        public static void SetTransparency(bool enabled)
        {
            SetRegistryValue(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "EnableTransparency", enabled ? 1 : 0, RegistryValueKind.DWord);
        }

        public static bool CheckTransparencyDisabled()
        {
            return CheckRegistryDWord(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "EnableTransparency", 0);
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, IntPtr pvParam, uint fWinIni);

        [StructLayout(LayoutKind.Sequential)]
        private struct ANIMATIONINFO
        {
            public uint cbSize;
            public int iMinAnimate;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref ANIMATIONINFO pvParam, uint fWinIni);

        public static void ApplyOptimizedVisualEffects()
        {
            try
            {
                const uint SPIF_FLAGS = 0x01 | 0x02; // SPIF_UPDATEINIFILE | SPIF_SENDCHANGE

                // 1. Chamadas nativas ao Win32 SystemParametersInfo (atualiza em memória imediatamente)
                // Animar janelas ao minimizar/maximizar -> OFF
                var ai = new ANIMATIONINFO { cbSize = (uint)Marshal.SizeOf(typeof(ANIMATIONINFO)), iMinAnimate = 0 };
                SystemParametersInfo(73, 0, ref ai, SPIF_FLAGS);

                // Efeitos de animação e desfoque -> OFF
                SystemParametersInfo(4101, 0, IntPtr.Zero, SPIF_FLAGS); // ComboBoxAnimation
                SystemParametersInfo(4163, 0, IntPtr.Zero, SPIF_FLAGS); // ControlAnimations
                SystemParametersInfo(4123, 0, IntPtr.Zero, SPIF_FLAGS); // CursorShadow
                SystemParametersInfo(37, 0, IntPtr.Zero, SPIF_FLAGS);   // DragFullWindows
                SystemParametersInfo(4133, 0, IntPtr.Zero, SPIF_FLAGS); // DropShadow
                SystemParametersInfo(4103, 0, IntPtr.Zero, SPIF_FLAGS); // ListBoxSmoothScrolling
                SystemParametersInfo(4099, 0, IntPtr.Zero, SPIF_FLAGS); // MenuAnimation
                SystemParametersInfo(4117, 0, IntPtr.Zero, SPIF_FLAGS); // SelectionFade
                SystemParametersInfo(4119, 0, IntPtr.Zero, SPIF_FLAGS); // TooltipAnimation

                // FontSmoothing (Usar fontes de tela com cantos arredondados) -> ON (Preservado!)
                SystemParametersInfo(75, 1, IntPtr.Zero, SPIF_FLAGS);

                // 2. Chaves do Windows Explorer & DWM
                SetRegistryValue(Registry.CurrentUser, @"Software\Microsoft\Windows\DWM", "EnableAeroPeek", 0, RegistryValueKind.DWord);
                SetRegistryValue(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "IconsOnly", 0, RegistryValueKind.DWord); // Mostrar miniaturas!
                SetRegistryValue(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ListviewAlphaSelect", 0, RegistryValueKind.DWord);
                SetRegistryValue(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ListviewShadow", 0, RegistryValueKind.DWord);
                SetRegistryValue(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarAnimations", 0, RegistryValueKind.DWord);

                // 3. Desktop & Fontes
                SetRegistryValue(Registry.CurrentUser, @"Control Panel\Desktop", "FontSmoothing", "2", RegistryValueKind.String);
                SetRegistryValue(Registry.CurrentUser, @"Control Panel\Desktop", "FontSmoothingType", 2, RegistryValueKind.DWord);
                SetRegistryValue(Registry.CurrentUser, @"Control Panel\Desktop", "DragFullWindows", "0", RegistryValueKind.String);
                SetRegistryValue(Registry.CurrentUser, @"Control Panel\Desktop\WindowMetrics", "MinAnimate", "0", RegistryValueKind.String);

                // UserPreferencesMask no modo desempenho
                byte[] perfMask = new byte[] { 0x90, 0x12, 0x01, 0x80, 0x10, 0x00, 0x00, 0x00 };
                using (var k = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true))
                {
                    k?.SetValue("UserPreferencesMask", perfMask, RegistryValueKind.Binary);
                }

                // 4. Subchaves individuais lidas pela interface 'Propriedades do Sistema' (sysdm.cpl)
                var visualFxRoot = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects");
                visualFxRoot?.SetValue("VisualFXSetting", 3, RegistryValueKind.DWord); // Modo Personalizar

                string[] subkeysToDisable = new[]
                {
                    "AnimateMinMax", "ComboBoxAnimation", "ControlAnimations", "CursorShadow",
                    "DragFullWindows", "DropShadow", "DWMAeroPeekEnabled", "ListBoxSmoothScrolling",
                    "ListviewAlphaSelect", "ListviewShadow", "MenuAnimation", "SelectionFade",
                    "TaskbarAnimations", "TooltipAnimation"
                };

                foreach (var sk in subkeysToDisable)
                {
                    using var sub = Registry.CurrentUser.CreateSubKey($@"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects\{sk}");
                    sub?.SetValue("DefaultValue", 0, RegistryValueKind.DWord);
                    sub?.SetValue("DefaultApplied", 0, RegistryValueKind.DWord);
                }

                // Subchaves a MANTER ATIVAS
                string[] subkeysToEnable = new[] { "FontSmoothing", "ThumbnailsOrIcon" };
                foreach (var sk in subkeysToEnable)
                {
                    using var sub = Registry.CurrentUser.CreateSubKey($@"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects\{sk}");
                    sub?.SetValue("DefaultValue", 1, RegistryValueKind.DWord);
                    sub?.SetValue("DefaultApplied", 1, RegistryValueKind.DWord);
                }
            }
            catch { }
        }

        public static bool CheckOptimizedVisualEffectsApplied()
        {
            bool font = CheckRegistryString(Registry.CurrentUser, @"Control Panel\Desktop", "FontSmoothing", "2");
            bool thumbs = CheckRegistryDWord(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "IconsOnly", 0);
            bool noAnim = CheckRegistryString(Registry.CurrentUser, @"Control Panel\Desktop\WindowMetrics", "MinAnimate", "0");
            return font && thumbs && noAnim;
        }

        #endregion

        #region Helpers de Registro e Serviços

        private static bool CheckRegistryDWord(RegistryKey root, string keyPath, string valName, int expected)
        {
            try
            {
                using var k = root.OpenSubKey(keyPath);
                if (k == null) return false;
                var val = k.GetValue(valName);
                return val != null && Convert.ToInt32(val) == expected;
            }
            catch { return false; }
        }

        private static bool CheckRegistryString(RegistryKey root, string keyPath, string valName, string expected)
        {
            try
            {
                using var k = root.OpenSubKey(keyPath);
                if (k == null) return false;
                var val = k.GetValue(valName)?.ToString();
                return string.Equals(val, expected, StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }

        private static void SetRegistryValue(RegistryKey root, string keyPath, string valName, object val, RegistryValueKind kind)
        {
            try
            {
                using var k = root.CreateSubKey(keyPath, true);
                k?.SetValue(valName, val, kind);
            }
            catch { }
        }

        private static bool CheckServiceDisabled(string svcName)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher($"SELECT StartMode FROM Win32_Service WHERE Name = '{svcName}'");
                foreach (ManagementObject obj in searcher.Get())
                {
                    return string.Equals(obj["StartMode"]?.ToString(), "Disabled", StringComparison.OrdinalIgnoreCase);
                }
            }
            catch { }
            return false;
        }

        private static void SetServiceStartup(string svcName, ServiceMode mode)
        {
            try
            {
                var modeStr = mode switch
                {
                    ServiceMode.Disabled => "disabled",
                    ServiceMode.Manual => "demand",
                    ServiceMode.Automatic => "auto",
                    _ => "demand"
                };

                RunProcessHidden("sc.exe", $"config {svcName} start= {modeStr}");
                if (mode == ServiceMode.Disabled)
                {
                    RunProcessHidden("sc.exe", $"stop {svcName}");
                }
            }
            catch { }
        }

        private static void RunProcessHidden(string file, string args)
        {
            try
            {
                var psi = new ProcessStartInfo(file, args)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                using var p = Process.Start(psi);
                p?.WaitForExit(5000);
            }
            catch { }
        }

        #endregion
    }
}
