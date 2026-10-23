using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Threading.Tasks;

namespace GvzkHubOptimizer.Services
{
    public class DnsPreset
    {
        public string Name { get; set; } = string.Empty;
        public string Primary { get; set; } = string.Empty;
        public string Secondary { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class DnsService
    {
        public List<DnsPreset> GetPresets()
        {
            return new List<DnsPreset>
            {
                new DnsPreset { Name = "Cloudflare (1.1.1.1)", Primary = "1.1.1.1", Secondary = "1.0.0.1", Description = "Ultra rápido e focado em privacidade" },
                new DnsPreset { Name = "Google Public DNS", Primary = "8.8.8.8", Secondary = "8.8.4.4", Description = "Alta confiabilidade e compatibilidade global" },
                new DnsPreset { Name = "Quad9 (Segurança)", Primary = "9.9.9.9", Secondary = "149.112.112.112", Description = "Bloqueio nativo de malwares e phishing" },
                new DnsPreset { Name = "AdGuard DNS", Primary = "94.140.14.14", Secondary = "94.140.15.15", Description = "Bloqueio integrado de anúncios e rastreadores" },
                new DnsPreset { Name = "Automático (DHCP)", Primary = "DHCP", Secondary = "", Description = "Restaura o DNS fornecido pelo seu roteador" }
            };
        }

        public async Task<(bool Success, string Message)> ApplyDnsAsync(DnsPreset preset)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (preset.Primary == "DHCP")
                    {
                        var psi = new ProcessStartInfo("netsh", "interface ipv4 set dns name=\"Wi-Fi\" source=dhcp") { CreateNoWindow = true, UseShellExecute = false };
                        Process.Start(psi)?.WaitForExit();

                        var psiEth = new ProcessStartInfo("netsh", "interface ipv4 set dns name=\"Ethernet\" source=dhcp") { CreateNoWindow = true, UseShellExecute = false };
                        Process.Start(psiEth)?.WaitForExit();

                        return (true, "DNS restaurado para automático (DHCP).");
                    }
                    else
                    {
                        // Apply for active interfaces
                        string cmd = $"interface ipv4 set dns name=\"Wi-Fi\" static {preset.Primary} & netsh interface ipv4 add dns name=\"Wi-Fi\" {preset.Secondary} index=2 & " +
                                     $"netsh interface ipv4 set dns name=\"Ethernet\" static {preset.Primary} & netsh interface ipv4 add dns name=\"Ethernet\" {preset.Secondary} index=2";

                        var psi = new ProcessStartInfo("cmd.exe", $"/c {cmd}") { CreateNoWindow = true, UseShellExecute = false };
                        Process.Start(psi)?.WaitForExit();

                        return (true, $"DNS {preset.Name} aplicado com sucesso!");
                    }
                }
                catch (Exception ex)
                {
                    return (false, $"Falha ao alterar DNS: {ex.Message}");
                }
            });
        }

        public async Task<string> FlushDnsAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo("ipconfig", "/flushdns")
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true
                    };
                    using var p = Process.Start(psi);
                    var output = p?.StandardOutput.ReadToEnd() ?? "";
                    p?.WaitForExit();
                    return "Cache do DNS limpo com sucesso!";
                }
                catch (Exception ex)
                {
                    return $"Erro ao limpar cache DNS: {ex.Message}";
                }
            });
        }
    }
}
