using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using GvzkHubOptimizer.Models;

namespace GvzkHubOptimizer.Services
{
    public class CleanerService
    {
        [DllImport("Shell32.dll", CharSet = CharSet.Unicode)]
        private static extern uint SHEmptyRecycleBin(IntPtr hwnd, string? pszRootPath, uint dwFlags);

        [DllImport("Shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHQueryRecycleBin(string? pszRootPath, ref SHQUERYRBINFO pSHQueryRBInfo);

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct SHQUERYRBINFO
        {
            public int cbSize;
            public long i64Size;
            public long i64NumItems;
        }

        private const uint SHERB_NOCONFIRMATION = 0x00000001;
        private const uint SHERB_NOPROGRESSUI = 0x00000002;
        private const uint SHERB_NOSOUND = 0x00000004;

        public List<CleanerTarget> GetDefaultTargets()
        {
            return new List<CleanerTarget>
            {
                new CleanerTarget { Id = "user_temp", Title = "Arquivos Temporários do Usuário", Description = "Pastas %TEMP% e AppData/Local/Temp", SizeFormatted = "Calculando..." },
                new CleanerTarget { Id = "win_temp", Title = "Arquivos Temporários do Windows", Description = "Pasta C:\\Windows\\Temp", SizeFormatted = "Calculando..." },
                new CleanerTarget { Id = "prefetch", Title = "Cache Prefetch", Description = "Dados obsoletos de inicialização em C:\\Windows\\Prefetch", SizeFormatted = "Calculando..." },
                new CleanerTarget { Id = "wu_cache", Title = "Cache do Windows Update", Description = "Instaladores baixados em SoftwareDistribution\\Download", SizeFormatted = "Calculando..." },
                new CleanerTarget { Id = "crash_dumps", Title = "Relatórios de Falhas e Logs", Description = "Minidumps e arquivos WER de erros do sistema", SizeFormatted = "Calculando..." },
                new CleanerTarget { Id = "recycle_bin", Title = "Lixeira do Windows", Description = "Esvazia todos os itens na lixeira do sistema", SizeFormatted = "Calculando..." }
            };
        }

        public async Task<long> GetTargetSizeBytesAsync(string targetId)
        {
            return await Task.Run(() =>
            {
                try
                {
                    return targetId switch
                    {
                        "user_temp" => CalculateDirectorySize(Path.GetTempPath()),
                        "win_temp" => CalculateDirectorySize(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp")),
                        "prefetch" => CalculateDirectorySize(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch")),
                        "wu_cache" => CalculateDirectorySize(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "SoftwareDistribution", "Download")),
                        "crash_dumps" => CalculateDirectorySize(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CrashDumps")) +
                                         CalculateDirectorySize(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Minidump")),
                        "recycle_bin" => GetRecycleBinSize(),
                        _ => 0L
                    };
                }
                catch
                {
                    return 0L;
                }
            });
        }

        private long GetRecycleBinSize()
        {
            try
            {
                var info = new SHQUERYRBINFO { cbSize = Marshal.SizeOf(typeof(SHQUERYRBINFO)) };
                int hr = SHQueryRecycleBin(null, ref info);
                return hr == 0 ? info.i64Size : 0L;
            }
            catch
            {
                return 0L;
            }
        }

        private long CalculateDirectorySize(string path)
        {
            if (!Directory.Exists(path)) return 0;
            long size = 0;
            try
            {
                var dir = new DirectoryInfo(path);
                foreach (var file in dir.EnumerateFiles("*", SearchOption.AllDirectories))
                {
                    try { size += file.Length; } catch { }
                }
            }
            catch { }
            return size;
        }

        public async Task<long> CleanTargetAsync(string targetId)
        {
            return await Task.Run(() =>
            {
                long freedBytes = 0;

                try
                {
                    switch (targetId)
                    {
                        case "user_temp":
                            freedBytes += CleanDirectory(Path.GetTempPath());
                            break;
                        case "win_temp":
                            freedBytes += CleanDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp"));
                            break;
                        case "prefetch":
                            freedBytes += CleanDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch"));
                            break;
                        case "wu_cache":
                            freedBytes += CleanDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "SoftwareDistribution", "Download"));
                            break;
                        case "crash_dumps":
                            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                            freedBytes += CleanDirectory(Path.Combine(localAppData, "CrashDumps"));
                            freedBytes += CleanDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Minidump"));
                            break;
                        case "recycle_bin":
                            freedBytes += GetRecycleBinSize();
                            SHEmptyRecycleBin(IntPtr.Zero, null, SHERB_NOCONFIRMATION | SHERB_NOPROGRESSUI | SHERB_NOSOUND);
                            break;
                    }
                }
                catch { }

                return freedBytes;
            });
        }

        private long CleanDirectory(string path)
        {
            long freed = 0;
            if (!Directory.Exists(path)) return 0;

            var dir = new DirectoryInfo(path);

            foreach (var file in dir.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                try
                {
                    long length = file.Length;
                    file.Delete();
                    freed += length;
                }
                catch { }
            }

            foreach (var subDir in dir.EnumerateDirectories())
            {
                try
                {
                    subDir.Delete(true);
                }
                catch { }
            }

            return freed;
        }
    }
}
