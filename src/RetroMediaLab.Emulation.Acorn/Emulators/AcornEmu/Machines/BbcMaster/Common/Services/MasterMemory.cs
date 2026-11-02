using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMaster.Common.Constants.MasterConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMaster.Common.Services;

/// <summary>LYNNE (20 Kio), ANDY (4 Kio) et HAZEL (8 Kio) ; le MOS sélectionne les accès par ACCCON et ROMSEL.</summary>
internal sealed class MasterMemory
{
    private readonly byte[] _shadow = new byte[C.ShadowBytes];
    private readonly byte[] _andy = new byte[C.AndyBytes];
    private readonly byte[] _hazel = new byte[C.HazelBytes];
    internal byte RomSelect { get; private set; }
    internal byte AccessControl { get; private set; }
    internal bool Irq => (AccessControl & C.InterruptRequest) != 0;
    internal bool TestRom(ushort address) => address is >= C.IoStart and < C.IoEnd && (AccessControl & C.TestRom) != 0;
    internal void Select(ushort address, byte value)
    {
        if ((address & ~3) == C.RomSelect) RomSelect = (byte)(value & C.RomSelectMask);
        else if ((address & ~3) == C.AccessControl) AccessControl = value;
    }
    private bool Shadow(ushort address, ushort instruction) => address is >= C.ShadowStart and < C.RamEnd
        && ((instruction is >= C.HazelStart and < C.HazelEnd && (AccessControl & C.HazelSelected) == 0)
            ? (AccessControl & C.MosShadow) != 0 : (AccessControl & C.CpuShadow) != 0);
    internal bool Private(ushort address) => address is >= C.RamEnd and < C.AndyEnd && (RomSelect & C.AndySelected) != 0
        || address is >= C.HazelStart and < C.HazelEnd && (AccessControl & C.HazelSelected) != 0;
    internal byte Read(ushort address, ushort instruction, byte[] main) => Private(address)
        ? address < C.AndyEnd ? _andy[address - C.RamEnd] : _hazel[address - C.HazelStart]
        : Shadow(address, instruction) ? _shadow[address - C.ShadowStart] : main[address];
    internal void Write(ushort address, ushort instruction, byte value, byte[] main)
    {
        if (Private(address)) { if (address < C.AndyEnd) _andy[address - C.RamEnd] = value; else _hazel[address - C.HazelStart] = value; }
        else if (Shadow(address, instruction)) _shadow[address - C.ShadowStart] = value;
        else main[address] = value;
    }
    internal byte ReadVideo(int address, byte[] main) => address is >= C.ShadowStart and < C.RamEnd && (AccessControl & C.DisplayShadow) != 0 ? _shadow[address - C.ShadowStart] : main[address & (C.MainBytes - 1)];
    internal void Reset(bool cold)
    {
        RomSelect = 0; AccessControl = 0;
        if (cold) { Array.Clear(_shadow); Array.Clear(_andy); Array.Clear(_hazel); }
    }
}
