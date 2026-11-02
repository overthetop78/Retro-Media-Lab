using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Interfaces;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMaster.Tube.Constants.TubeConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMaster.Tube.Services;

/// <summary>Carte interne ADC06 : 65C102 à 4 MHz, 64 Kio de RAM et ROM de démarrage déconnectable.</summary>
internal sealed class TurboBoard : ICpuBus
{
    private readonly TubeUla _tube;
    private readonly byte[] _ram = new byte[C.TurboRamBytes];
    private readonly byte[] _rom;
    private readonly Cpu6502 _cpu;
    private bool _romMapped;
    private long _target;
    public bool Irq => _tube.ParasiteIrq;
    public bool Nmi => _tube.ParasiteNmi;
    internal TurboBoard(TubeUla tube, ReadOnlySpan<byte> rom)
    {
        if (rom.Length != C.TurboRomBytes) throw new ArgumentException(nameof(rom));
        _tube = tube; _rom = rom.ToArray(); _cpu = new Cpu6502(this, cmos: true);
        Reset();
    }
    internal void Reset() { _romMapped = true; _cpu.Reset(); _target = _cpu.Cycles; }
    internal void RunHostCycle()
    {
        if (_tube.ResetHeld) { _target = _cpu.Cycles; return; }
        _target += C.TurboCyclesPerHostCycle;
        while (_cpu.Cycles < _target) _cpu.Step();
    }
    public void Tick() { }
    public byte Read(ushort address)
    {
        if ((address & ~C.RegisterMask) == C.TurboRegisters) { if ((address & C.RegisterMask) == 0) _romMapped = false; return _tube.ParasiteRead(address); }
        return _romMapped && address >= C.TurboRomStart ? _rom[address & (C.TurboRomBytes - 1)] : _ram[address];
    }
    public void Write(ushort address, byte value)
    {
        if ((address & ~C.RegisterMask) == C.TurboRegisters) _tube.ParasiteWrite(address, value);
        else _ram[address] = value;
    }
}
