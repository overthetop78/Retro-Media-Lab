using System.Buffers.Binary;
using B = RetroMediaLab.Chips.Custom.Acorn.Archimedes.Hardware.Constants.ArchimedesHardwareConstants;
using C = RetroMediaLab.Chips.Memory.Acorn.Archimedes.MEMC.Constants.ArchimedesMemcConstants;

namespace RetroMediaLab.Chips.Memory.Acorn.Archimedes.MEMC.Interfaces;
/// <summary>Contrat matériel du composant ArchimedesMemc ; aucune ressource hôte n'est possédée par ce contrat.</summary>
public interface IArchimedesMemc
{
    uint Control { get; }

    bool BootRomMapped { get; }

    int RamBytes { get; }

    int PageShift { get; }

    bool VideoDmaEnabled { get; }

    bool SoundDmaEnabled { get; }

    bool CursorEnabled { get; }

    bool SoundInterrupt { get; }

    uint VideoPointer { get; }

    uint SoundPointer { get; }

    Action<bool>? SoundInterruptChanged { get; set; }

    void Reset(bool cold);
    void ObserveCpuAddress(uint address);
    bool TryReadLogicalWord(uint address, bool user, out uint value);
    bool TryReadLogicalByte(uint address, bool user, out byte value);
    bool TryWriteLogicalWord(uint address, uint value, bool user);
    bool TryWriteLogicalByte(uint address, byte value, bool user);
    uint ReadPhysicalWord(uint physical);
    byte ReadPhysicalByte(uint physical);
    void WritePhysicalWord(uint physical, uint value);
    void WritePhysicalByte(uint physical, byte value);
    uint ReadRomWord(uint address);
    byte ReadRomByte(uint address);
    int RomCycles(uint address, bool sequential);
    void WriteCam(uint address);
    void WriteRegister(uint address);
    void BeginVideoFrame();
    void BeginCursorFrame();
    void ReadVideoBurst(Span<uint> words);
    void ReadCursorBurst(Span<uint> words);
    void ReadSoundBurst(Span<uint> words);
}
