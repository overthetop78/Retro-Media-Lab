namespace RetroMediaLab.Emulation.Acorn.Common.Machines.Archimedes.Constants;

internal static class ArchimedesModelConstants
{
    internal const int Kibibyte = 1024;
    internal const int Mebibyte = Kibibyte * Kibibyte;
    internal const int SmallSystemRomBytes = 512 * Kibibyte;
    internal const int LargeSystemRomBytes = 2 * Mebibyte;
    internal const string IdentitySeparator = ":";
    internal const string ResourcePrefix = "Emulation.Acorn.Archimedes.";
    internal const string ModelResource = "Emulation.Acorn.Model.AcornArchimedes";
    internal const string A3000ModelResource = "Emulation.Acorn.Model.AcornA3000";
    internal const string OptionPrefix = "acornemu.archimedes.";
    internal const string CpuOption = OptionPrefix + "cpu";
    internal const string MemcOption = OptionPrefix + "memc";
    internal const string St506Option = OptionPrefix + "st506";
    internal const string SerialOption = OptionPrefix + "serial";
    internal const string MemcResource = ResourcePrefix + "Memc";
    internal const string St506Resource = ResourcePrefix + "St506";
    internal const string SerialResource = ResourcePrefix + "Serial";
    internal const string Group = "Archimedes";
    internal const string RomGroup = "Rom";
    internal const string BreakResource = "Emulation.Acorn.Key.Break";
    internal const float DisplayAspectRatio = 4f / 3;
    internal const string RiscOs3VersionPrefix = "3.";
    internal const string ArthurNamePrefix = "Arthur";
    internal const string CpuSummaryFormat = "CPU {0}";
    internal const string MemorySummaryFormat = "RAM {0} KiB";
    internal const string CoreSummaryFormat = "Core {0}";
    internal const string MemoryDisplayFormat = "{0} KiB";
    internal const string A3000ExpandedMemoryDisplay = "8192 KiB · IFEL";
    internal const int Arm2ClockHz = 8000000;
    internal const int Arm3ClockHz = 25000000;
    internal const int DefaultMemoryKib = 1024;
    internal const int MinimumMemoryKib = 512;
    internal const int MaximumSingleMemcMemoryKib = 4096;
    internal const int ExpandedMemoryKib = 8192;
    internal const int OnePort = 1;
    internal const int A3000FloppyDrives = 1;
    internal static IReadOnlyList<int> MemorySizesKib { get; } = Array.AsReadOnly<int>(
        [MinimumMemoryKib, DefaultMemoryKib, DefaultMemoryKib * 2, MaximumSingleMemcMemoryKib, ExpandedMemoryKib]);
}
