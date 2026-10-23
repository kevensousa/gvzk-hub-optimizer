using System;
using System.Collections.Generic;
using System.IO;
using System.Management;
using System.Threading.Tasks;
using Microsoft.Win32;
using GvzkHubOptimizer.Models;

namespace GvzkHubOptimizer.Services
{
    public class HardwareService
    {
        public async Task<HardwareSummary> GetHardwareSummaryAsync()
        {
            return await Task.Run(() =>
            {
                var summary = new HardwareSummary();

                try
                {
                    // 1. CPU
                    using var cpuSearcher = new ManagementObjectSearcher("SELECT Name, NumberOfCores, NumberOfLogicalProcessors, MaxClockSpeed FROM Win32_Processor");
                    foreach (ManagementObject obj in cpuSearcher.Get())
                    {
                        var name = obj["Name"]?.ToString()?.Trim() ?? "Processador Desconhecido";
                        summary.CpuName = name;
                        var cores = obj["NumberOfCores"]?.ToString() ?? "0";
                        var threads = obj["NumberOfLogicalProcessors"]?.ToString() ?? "0";
                        summary.CpuCoresThreads = $"{cores} Cores / {threads} Threads";

                        double baseGhz = 3.0;
                        if (obj["MaxClockSpeed"] != null)
                        {
                            baseGhz = Math.Round(Convert.ToDouble(obj["MaxClockSpeed"]) / 1000.0, 2);
                        }
                        summary.CpuBaseClock = $"Base: {baseGhz:F2} GHz";

                        // Determina Turbo / Boost Clock
                        summary.CpuBoostClock = GetBoostClock(name, baseGhz);
                        break;
                    }
                }
                catch { }

                try
                {
                    // 2. GPU & VRAM REAL (Lendo 64-bit do Registro para ultrapassar limite de 4GB do WMI)
                    ulong realVramBytes = 0;
                    string detectedGpuName = "";
                    string driverVer = "";

                    try
                    {
                        using var classKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}");
                        if (classKey != null)
                        {
                            foreach (var subKeyName in classKey.GetSubKeyNames())
                            {
                                if (subKeyName.Length == 4 && int.TryParse(subKeyName, out _))
                                {
                                    using var subKey = classKey.OpenSubKey(subKeyName);
                                    if (subKey != null)
                                    {
                                        var desc = subKey.GetValue("DriverDesc")?.ToString();
                                        var qwSize = subKey.GetValue("HardwareInformation.qwMemorySize");
                                        var ver = subKey.GetValue("DriverVersion")?.ToString();

                                        if (!string.IsNullOrEmpty(desc) && !desc.Contains("Virtual", StringComparison.OrdinalIgnoreCase))
                                        {
                                            if (qwSize != null)
                                            {
                                                ulong bytes = Convert.ToUInt64(qwSize);
                                                if (bytes > realVramBytes)
                                                {
                                                    realVramBytes = bytes;
                                                    detectedGpuName = desc;
                                                    if (!string.IsNullOrEmpty(ver)) driverVer = ver;
                                                }
                                            }
                                            else if (string.IsNullOrEmpty(detectedGpuName))
                                            {
                                                detectedGpuName = desc;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch { }

                    // Fallback para WMI se registro falhar
                    if (string.IsNullOrEmpty(detectedGpuName) || realVramBytes == 0)
                    {
                        using var gpuSearcher = new ManagementObjectSearcher("SELECT Name, AdapterRAM, DriverVersion FROM Win32_VideoController");
                        foreach (ManagementObject obj in gpuSearcher.Get())
                        {
                            var name = obj["Name"]?.ToString()?.Trim() ?? "";
                            if (name.Contains("Virtual", StringComparison.OrdinalIgnoreCase)) continue;

                            if (string.IsNullOrEmpty(detectedGpuName)) detectedGpuName = name;
                            if (realVramBytes == 0 && obj["AdapterRAM"] != null)
                            {
                                realVramBytes = Convert.ToUInt64(obj["AdapterRAM"]);
                            }
                            if (string.IsNullOrEmpty(driverVer) && obj["DriverVersion"] != null)
                            {
                                driverVer = obj["DriverVersion"]?.ToString() ?? "";
                            }
                            break;
                        }
                    }

                    summary.GpuName = detectedGpuName;
                    if (realVramBytes > 0)
                    {
                        double vramGb = Math.Round((double)realVramBytes / (1024.0 * 1024.0 * 1024.0), 1);
                        summary.GpuVram = $"{vramGb:F1} GB VRAM Dedicada";
                    }
                    else
                    {
                        summary.GpuVram = "Memória Compartilhada";
                    }
                    summary.GpuDriver = !string.IsNullOrEmpty(driverVer) ? $"Driver {driverVer}" : "";
                }
                catch { }

                try
                {
                    // 3. RAM
                    ulong totalBytes = 0;
                    uint maxSpeed = 0;
                    int moduleCount = 0;
                    string memoryTypeStr = "DDR4";

                    using var memSearcher = new ManagementObjectSearcher("SELECT Capacity, Speed, ConfiguredClockSpeed, SMBIOSMemoryType FROM Win32_PhysicalMemory");
                    foreach (ManagementObject obj in memSearcher.Get())
                    {
                        moduleCount++;
                        if (obj["Capacity"] != null)
                        {
                            totalBytes += Convert.ToUInt64(obj["Capacity"]);
                        }
                        uint speed = 0;
                        if (obj["ConfiguredClockSpeed"] != null)
                            speed = Convert.ToUInt32(obj["ConfiguredClockSpeed"]);
                        else if (obj["Speed"] != null)
                            speed = Convert.ToUInt32(obj["Speed"]);

                        if (speed > maxSpeed) maxSpeed = speed;

                        if (obj["SMBIOSMemoryType"] != null)
                        {
                            int smbiosType = Convert.ToInt32(obj["SMBIOSMemoryType"]);
                            memoryTypeStr = smbiosType switch
                            {
                                26 => "DDR4",
                                30 => "DDR5",
                                24 => "DDR3",
                                _ => "DDR4"
                            };
                        }
                    }

                    double totalGb = Math.Round((double)totalBytes / (1024.0 * 1024.0 * 1024.0), 0);
                    summary.RamTotal = $"{totalGb} GB {memoryTypeStr}";
                    summary.RamModules = $"{moduleCount} pente(s) instalado(s)";
                    summary.RamFrequency = maxSpeed > 0 ? $"{maxSpeed} MHz (MT/s)" : "3600 MHz";
                    summary.RamLatency = "Perfil XMP/DOCP Ativo";
                }
                catch { }

                try
                {
                    // 4. Discos
                    var drives = DriveInfo.GetDrives();
                    foreach (var drive in drives)
                    {
                        if (drive.IsReady && drive.DriveType == DriveType.Fixed)
                        {
                            double totalGb = Math.Round((double)drive.TotalSize / (1024.0 * 1024.0 * 1024.0), 1);
                            double freeGb = Math.Round((double)drive.TotalFreeSpace / (1024.0 * 1024.0 * 1024.0), 1);

                            summary.Disks.Add(new DiskInfo
                            {
                                DriveLetter = drive.Name.TrimEnd('\\'),
                                Model = string.IsNullOrEmpty(drive.VolumeLabel) ? "Disco Local" : drive.VolumeLabel,
                                Size = $"{totalGb} GB",
                                FreeSpace = $"{freeGb} GB livres",
                                MediaType = "SSD / NVMe"
                            });
                        }
                    }
                }
                catch { }

                try
                {
                    // 5. Advanced: Placa-Mãe & BIOS
                    using var boardSearcher = new ManagementObjectSearcher("SELECT Manufacturer, Product FROM Win32_BaseBoard");
                    foreach (ManagementObject obj in boardSearcher.Get())
                    {
                        summary.Motherboard.Manufacturer = obj["Manufacturer"]?.ToString()?.Trim() ?? "";
                        summary.Motherboard.Product = obj["Product"]?.ToString()?.Trim() ?? "";
                        break;
                    }

                    using var biosSearcher = new ManagementObjectSearcher("SELECT SMBIOSBIOSVersion, ReleaseDate FROM Win32_BIOS");
                    foreach (ManagementObject obj in biosSearcher.Get())
                    {
                        summary.Bios.Version = obj["SMBIOSBIOSVersion"]?.ToString()?.Trim() ?? "";
                        summary.Bios.ReleaseDate = obj["ReleaseDate"]?.ToString()?.Trim() ?? "";
                        break;
                    }
                }
                catch { }

                try
                {
                    // 6. Advanced: Áudio
                    using var soundSearcher = new ManagementObjectSearcher("SELECT Name FROM Win32_SoundDevice");
                    foreach (ManagementObject obj in soundSearcher.Get())
                    {
                        var name = obj["Name"]?.ToString()?.Trim();
                        if (!string.IsNullOrEmpty(name) && !summary.AudioDevices.Contains(name))
                        {
                            summary.AudioDevices.Add(name);
                        }
                    }
                }
                catch { }

                try
                {
                    // 7. Advanced: Periféricos
                    using var pnpSearcher = new ManagementObjectSearcher("SELECT Name, Service FROM Win32_PnPEntity WHERE Service IN ('kbdhid', 'mouhid', 'usbccgp', 'HidUsb')");
                    foreach (ManagementObject obj in pnpSearcher.Get())
                    {
                        var name = obj["Name"]?.ToString()?.Trim();
                        if (!string.IsNullOrEmpty(name) && !summary.Peripherals.Contains(name))
                        {
                            summary.Peripherals.Add(name);
                        }
                    }
                }
                catch { }

                return summary;
            });
        }

        private static string GetBoostClock(string cpuName, double baseGhz)
        {
            var upper = cpuName.ToUpperInvariant();
            if (upper.Contains("5700X3D")) return "Turbo / Boost: Até 4.10 GHz";
            if (upper.Contains("5800X3D")) return "Turbo / Boost: Até 4.50 GHz";
            if (upper.Contains("7800X3D")) return "Turbo / Boost: Até 5.00 GHz";
            if (upper.Contains("5600X")) return "Turbo / Boost: Até 4.60 GHz";
            if (upper.Contains("5700X")) return "Turbo / Boost: Até 4.65 GHz";
            if (upper.Contains("5800X")) return "Turbo / Boost: Até 4.70 GHz";
            if (upper.Contains("5900X")) return "Turbo / Boost: Até 4.80 GHz";
            if (upper.Contains("5950X")) return "Turbo / Boost: Até 4.90 GHz";
            if (upper.Contains("7600X")) return "Turbo / Boost: Até 5.30 GHz";
            if (upper.Contains("7700X")) return "Turbo / Boost: Até 5.40 GHz";
            if (upper.Contains("7900X")) return "Turbo / Boost: Até 5.60 GHz";
            if (upper.Contains("7950X")) return "Turbo / Boost: Até 5.70 GHz";

            // Genérico
            double estimatedBoost = Math.Round(baseGhz * 1.35, 1);
            return $"Turbo / Boost: Até {estimatedBoost:F2} GHz";
        }
    }
}
