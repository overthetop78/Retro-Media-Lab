using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.ModelBPlus.Constants.ModelBPlusConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.ModelBPlus.Services;

/// <summary>PAL de sélection mémoire du B+ ; la RAM principale est empruntée à la carte.</summary>
internal sealed partial class ModelBPlusMemory
{
    private readonly byte[] _shadow = new byte[C.ShadowBytes];
    private readonly byte[] _private = new byte[C.PrivateBytes];
    private bool _paged;
    internal bool ShadowEnabled { get; private set; }
    internal bool Select(ushort address, byte value)
    {
        if (address < C.ControlStart || address >= C.ControlEnd) return false;
        switch (address & C.ShadowRegister)
        {
            case C.RomRegister: _paged = (value & C.EnableBit) != 0; break;
            case C.ShadowRegister: ShadowEnabled = (value & C.EnableBit) != 0; break;
        }
        return true;
    }

    internal bool PrivateSelected(ushort address) => _paged && address >= C.PrivateStart && address < C.PrivateEnd;
    internal byte ReadPrivate(ushort address) => _private[address - C.PrivateStart];
    internal void WritePrivate(ushort address, byte value) => _private[address - C.PrivateStart] = value;
    internal bool UsesShadow(ushort address, ushort instruction) => ShadowEnabled && address >= C.ShadowStart && address < C.RamEnd
        && (instruction >= C.MosDriverStart && instruction < C.MosDriverEnd
            || _paged && instruction >= C.PrivateDriverStart && instruction < C.PrivateEnd);
    internal byte Read(ushort address, ushort instruction, byte[] main) => UsesShadow(address, instruction) ? _shadow[address - C.ShadowStart] : main[address];
    internal void Write(ushort address, ushort instruction, byte value, byte[] main)
    {
        if (UsesShadow(address, instruction)) _shadow[address - C.ShadowStart] = value;
        else main[address] = value;
    }
    internal byte ReadVideo(int address, byte[] main) => ShadowEnabled && address >= C.ShadowStart
        ? _shadow[address - C.ShadowStart] : main[address];
    internal void Reset(bool cold)
    {
        _paged = false;
        ShadowEnabled = false;
        if (!cold) return;
        Array.Clear(_shadow);
        Array.Clear(_private);
    }
}
