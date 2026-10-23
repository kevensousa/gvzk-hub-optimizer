using System.Collections.Generic;

namespace GvzkHubOptimizer.Models
{
    public class HardwareSummary
    {
        // CPU
        public string CpuName { get; set; } = "Carregando...";
        public string CpuCoresThreads { get; set; } = "";
        public string CpuBaseClock { get; set; } = "";
        public string CpuBoostClock { get; set; } = "";

        // GPU
        public string GpuName { get; set; } = "Carregando...";
        public string GpuVram { get; set; } = "";
        public string GpuDriver { get; set; } = "";

        // RAM
        public string RamTotal { get; set; } = "Carregando...";
        public string RamFrequency { get; set; } = "";
        public string RamLatency { get; set; } = "CL (Automático / SMBIOS)";
        public string RamModules { get; set; } = "";

        // Disks
        public List<DiskInfo> Disks { get; set; } = new();

        // Advanced Specs
        public MotherboardInfo Motherboard { get; set; } = new();
        public BiosInfo Bios { get; set; } = new();
        public List<string> AudioDevices { get; set; } = new();
        public List<string> Peripherals { get; set; } = new();
        public List<string> NetworkAdapters { get; set; } = new();
    }

    public class DiskInfo
    {
        public string Model { get; set; } = "";
        public string Size { get; set; } = "";
        public string FreeSpace { get; set; } = "";
        public string DriveLetter { get; set; } = "";
        public string MediaType { get; set; } = "SSD/HDD";
    }

    public class MotherboardInfo
    {
        public string Manufacturer { get; set; } = "";
        public string Product { get; set; } = "";
    }

    public class BiosInfo
    {
        public string Version { get; set; } = "";
        public string ReleaseDate { get; set; } = "";
    }
}
