using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace GvzkHubOptimizer.Services
{
    public class RestorePointService
    {
        public async Task<(bool Success, string Message)> CreateRestorePointAsync(string description = "gvzk HUB Optimizer Backup")
        {
            return await Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"Checkpoint-Computer -Description '{description}' -RestorePointType 'MODIFY_SETTINGS'\"",
                        Verb = "runas",
                        UseShellExecute = true,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };

                    using var process = Process.Start(psi);
                    if (process == null)
                    {
                        return (false, "Não foi possível iniciar o processo de ponto de restauração.");
                    }

                    process.WaitForExit(60000); // 60 segundos max

                    if (process.ExitCode == 0)
                    {
                        return (true, "Ponto de restauração criado com sucesso no Windows!");
                    }
                    else
                    {
                        return (false, $"O Windows retornou o código de erro {process.ExitCode}. Verifique se a Proteção do Sistema está ativada no disco C:.");
                    }
                }
                catch (Exception ex)
                {
                    return (false, $"Erro ao criar ponto de restauração: {ex.Message}");
                }
            });
        }
    }
}
