namespace RetroMediaLab.Emulation.Acorn.Common.Machines.AcornAtom.Constants;

internal static class AtomModelConstants
{
    internal const string ResourcePrefix = "Emulation.Acorn.Model.";
    internal const string OptionPrefix = "acornemu.atom.";
    internal const string CassetteMachineId = "acorn-atom";
    internal const string TextRamOption = OptionPrefix + "textRam";
    internal const string VideoRamOption = OptionPrefix + "videoRam";
    internal const string DiscPackOption = OptionPrefix + "discPack";
    internal const string FloatingPointFirmware = OptionPrefix + "floatingPoint";
    internal const string UtilityFirmware = OptionPrefix + "utility";
    internal const string Group = "Atom";
    internal const string IdentitySeparator = ":";
    internal const string RomGroup = "Rom";
    internal const string MemoryLabel = "Emulation.Acorn.System.Memory";
    internal const string TextRamLabel = "Emulation.Acorn.Atom.TextRam";
    internal const string VideoRamLabel = "Emulation.Acorn.Atom.VideoRam";
    internal const string DiscPackGroupLabel = "Emulation.Acorn.Atom.DiscPack";
    internal const string DiscPackLabel = "Emulation.Acorn.Atom.InstallDiscPack";
    internal const string SystemRomLabel = "Emulation.Acorn.Atom.BasicOsRom";
    internal const string FloatingPointLabel = "Emulation.Acorn.Atom.FloatingPointRom";
    internal const string UtilityLabel = "Emulation.Acorn.Atom.UtilityRom";
    internal const string DosLabel = "Emulation.Acorn.Atom.DosRom";
    internal const string CharacterLabel = "Emulation.Acorn.Firmware.CharacterGenerator";
    internal const string BreakLabel = "Emulation.Acorn.Input.Break";
    internal const string CpuSummaryFormat = "CPU {0}";
    internal const string MemorySummaryFormat = "RAM {0} KiB";
    internal const string CoreSummaryFormat = "Core {0}";
    internal const string MemorySuffix = " KiB";
    internal const int CpuClock = 1000000;
    internal const int CpuRamBytes = 1024;
    internal const int RamBlockBytes = 1024;
    internal const int MaximumTextRamBlocks = 5;
    internal const int MinimumVideoRamBlocks = 1;
    internal const int MaximumVideoRamBlocks = 6;
    internal const int DefaultMemoryBytes = CpuRamBytes + (MaximumTextRamBlocks + MaximumVideoRamBlocks) * RamBlockBytes;
    internal const int DiscPackRamBytes = 3072;
    internal const int KeyboardPorts = 1;
    internal const int CassetteBaud = 300;
    internal const int SystemRomBytes = 8192;
    internal const int OptionRomBytes = 4096;
    internal const int CharacterRomBytes = 280;
}
