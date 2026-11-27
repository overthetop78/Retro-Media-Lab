global using GdRomInterface = RetroMediaLab.Chips.Controllers.Sega.GdRom.GdRomInterface;
global using GdRomAudio = RetroMediaLab.Chips.Controllers.Sega.GdRom.GdRomInterface;

using System.IO;

namespace RetroMediaLab.Chips.Controllers.Sega.GdRom;

internal static unsafe partial class GdRomInterface
{
    internal static partial void RegInit();
    internal static partial void RegTerm();
    internal static partial void RegReset(bool hard);

    internal static partial uint ReadMem(uint address, uint size);
    internal static partial void WriteMem(uint address, uint data, uint size);
    internal static partial void ReadCddaSector(short* sector);
    internal static partial uint GetSubcode(uint format, uint fad, byte* subcode);
    internal static partial void SetDisc();

    internal static partial void Serialize(BinaryWriter writer);
    internal static partial void Deserialize(BinaryReader reader, int version);
}

internal enum GdRomDiscType
{
    CdDA = 0x00,
    CdRom = 0x10,
    CdRom_XA = 0x20,
    CdRom_Extra = 0x30,
    CdRom_CDI = 0x40,
    GdRom = 0x80,
    NoDisk = 0x1,
    Open = 0x2
}
