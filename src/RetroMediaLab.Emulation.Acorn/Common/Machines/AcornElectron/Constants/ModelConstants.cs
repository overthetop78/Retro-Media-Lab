namespace RetroMediaLab.Emulation.Acorn.Common.Machines.AcornElectron.Constants;

internal static class ElectronModelConstants
{
    internal const string ResourcePrefix = "Emulation.Acorn.Model.";
    internal const string OptionPrefix = "acornemu.electron.";
    internal const string Group = "Electron";
    internal const string RomGroup = "Rom";
    internal const string SystemRomLabel = "Emulation.Acorn.Firmware.Mos";
    internal const string BasicRomLabel = "Emulation.Firmware.Rom.Basic";
    internal const string BreakLabel = "Emulation.Acorn.Input.Break";
    internal const string CpuSummaryFormat = "CPU {0}";
    internal const string MemorySummaryFormat = "RAM {0} KiB";
    internal const string CoreSummaryFormat = "Core {0}";
    internal const string IdentitySeparator = ":";
    internal const int CpuClock = 2000000;
    internal const int MemoryBytes = 32768;
    internal const int MemoryKib = 32;
    internal const int RomBytes = 16384;
    internal const int CombinedRomBytes = RomBytes * 2;
    internal const int CassetteBaud = 1200;
    internal const int KeyboardPorts = 1;
}
