namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.ModelBPlus.Constants;

internal static class ModelBPlusConstants
{
    internal const string Memory64 = "64";
    internal const string Memory128 = "128";
    internal const string Memory64Display = "64 KiB";
    internal const string Memory128Display = "128 KiB";
    internal const string ChipPrefix = "acornemu.bplus.rom.";
    internal const string MemoryGroup = "Emulation.Acorn.BPlus.Memory";
    internal const string MemoryDescription = "Emulation.Acorn.BPlus.Memory.Description";
    internal const string SystemRomLabel = "Emulation.Acorn.BPlus.SystemRom";
    internal const string RomGroup = "Emulation.Acorn.Memory.Sideways.Sockets";
    internal const string RomSizeError = "Emulation.Acorn.BPlus.InvalidRomSize";
    internal const int MainBytes = 32 * 1024;
    internal const int ShadowBytes = 20 * 1024;
    internal const int PrivateBytes = 12 * 1024;
    internal const int BaseBytes = MainBytes + ShadowBytes + PrivateBytes;
    internal const int ExpandedBytes = BaseBytes + 64 * 1024;
    internal const int BankBytes = 16 * 1024;
    internal const int ChipBytes = 2 * BankBytes;
    internal const int SmallChipBytes = BankBytes / 2;
    internal const ushort ShadowStart = 0x3000;
    internal const ushort RamEnd = 0x8000;
    internal const ushort PrivateStart = RamEnd;
    internal const ushort PrivateEnd = 0xb000;
    internal const ushort PrivateDriverStart = 0xa000;
    internal const ushort MosDriverStart = 0xc000;
    internal const ushort MosDriverEnd = 0xe000;
    internal const ushort ControlStart = 0xfe30;
    internal const ushort ControlEnd = 0xfe38;
    internal const int RegisterMask = 7;
    internal const int RomRegister = 0;
    internal const int ShadowRegister = 4;
    internal const byte EnableBit = 0x80;
    internal const int DfsBank = 7;
    internal const int NetworkBank = 11;
    internal static IReadOnlyList<int> RamBanks { get; } = Array.AsReadOnly<int>([0, 1, 12, 13]);
    internal static IReadOnlyList<int> SocketBanks { get; } = Array.AsReadOnly<int>([2, 4, 6, 8, 10]);
    internal static IReadOnlyList<string> SocketNames { get; } = Array.AsReadOnly<string>(["IC35", "IC44", "IC57", "IC62", "IC68"]);
}
