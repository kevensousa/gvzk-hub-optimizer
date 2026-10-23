using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using GvzkHubOptimizer.Models;

namespace GvzkHubOptimizer.Services
{
    public class UwpService
    {
        public async Task<List<UwpAppItem>> GetInstalledBloatwareAsync()
        {
            return await Task.Run(() =>
            {
                var list = new List<UwpAppItem>();

                // Dicionário completo de Bloatwares conhecidos do Windows 10/11
                var friendlyNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "Microsoft.BingNews", "Microsoft Notícias" },
                    { "Microsoft.BingWeather", "Microsoft Clima" },
                    { "Microsoft.BingSearch", "Barra de Pesquisa Bing" },
                    { "Microsoft.GetHelp", "Obter Ajuda" },
                    { "Microsoft.Getstarted", "Dicas do Windows" },
                    { "Microsoft.MicrosoftSolitaireCollection", "Coleção Solitário" },
                    { "Microsoft.People", "Contatos (People)" },
                    { "Microsoft.WindowsFeedbackHub", "Hub de Comentários" },
                    { "Microsoft.YourPhone", "Vincular ao Celular" },
                    { "Microsoft.ZuneMusic", "Reprodutor de Mídia / Groove" },
                    { "Microsoft.ZuneVideo", "Filmes e TV" },
                    { "Clipchamp.Clipchamp", "Editor de Vídeo Clipchamp" },
                    { "Microsoft.Todos", "Microsoft To Do" },
                    { "Microsoft.Microsoft365", "Microsoft 365 (Web)" },
                    { "Microsoft.MicrosoftOfficeHub", "Microsoft 365 / Office Hub" },
                    { "Microsoft.M365Companions", "M365 Companions" },
                    { "Microsoft.OutlookForWindows", "Novo Outlook para Windows" },
                    { "Microsoft.PowerAutomateDesktop", "Power Automate Desktop" },
                    { "Microsoft.Windows.DevHome", "Dev Home" },
                    { "Microsoft.GamingApp", "Aplicativo Xbox" },
                    { "Microsoft.XboxGamingOverlay", "Xbox Game Bar Overlay" },
                    { "Microsoft.XboxIdentityProvider", "Xbox Identity Provider" },
                    { "Microsoft.XboxSpeechToTextOverlay", "Xbox Speech To Text" },
                    { "Microsoft.Xbox.TCUI", "Xbox TCUI" },
                    { "Microsoft.WindowsAlarms", "Alarmes e Relógio" },
                    { "Microsoft.WindowsCamera", "Câmera do Windows" },
                    { "Microsoft.WindowsSoundRecorder", "Gravador de Som" },
                    { "Microsoft.MicrosoftStickyNotes", "Notas Autoadesivas (Sticky Notes)" },
                    { "Microsoft.MinecraftUWP", "Minecraft" },
                    { "SpotifyAB.SpotifyMusic", "Spotify Music" },
                    { "AD2F1837.myHP", "HP Bloatware (myHP)" }
                };

                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = "-NoProfile -Command \"Get-AppxPackage | Select-Object -ExpandProperty Name\"",
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true
                    };

                    using var p = Process.Start(psi);
                    if (p != null)
                    {
                        var output = p.StandardOutput.ReadToEnd();
                        p.WaitForExit();

                        var installed = new HashSet<string>(output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries), StringComparer.OrdinalIgnoreCase);

                        foreach (var pkg in installed)
                        {
                            if (friendlyNames.TryGetValue(pkg, out var displayName))
                            {
                                list.Add(new UwpAppItem
                                {
                                    PackageName = pkg,
                                    DisplayName = displayName,
                                    Publisher = "Remoção Segura Recomendada"
                                });
                            }
                        }
                    }
                }
                catch { }

                return list;
            });
        }

        public async Task<bool> UninstallAppAsync(string packageName)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -Command \"Get-AppxPackage -Name '{packageName}' | Remove-AppxPackage\"",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    using var p = Process.Start(psi);
                    p?.WaitForExit();
                    return p?.ExitCode == 0;
                }
                catch
                {
                    return false;
                }
            });
        }
    }
}
