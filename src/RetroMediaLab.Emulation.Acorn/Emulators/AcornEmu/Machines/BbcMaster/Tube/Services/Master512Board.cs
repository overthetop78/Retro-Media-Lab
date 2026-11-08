using RetroMediaLab.Chips.CPU.Intel.MCS80x86.Chip80186.Enums;
using RetroMediaLab.Chips.CPU.Intel.MCS80x86.Chip80186.Services;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;
using C = RetroMediaLab.Chips.IO.Acorn.Tube.ULA.Constants.TubeConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMaster.Tube.Services;

/// <summary>Carte 80186 à 10 MHz : RAM 512 Kio, BIOS 16 Kio, Tube en E/S et DMA du processeur.</summary>
internal sealed class Master512Board
{
    private const int TubeDmaChannel = 0;
    private const int TubeInterruptPin = 0;
    private const int HoldTimer = 0;
    private const int NmiTimer = 1;
    private readonly Master512Refresh _refresh = new();
    private readonly TubeUla _tube;
    private readonly byte[] _ram = new byte[C.Master512RamBytes];
    private readonly byte[] _rom;
    // IC8 : ARDY (50) et SRDY (49) sont tous deux reliés au +5 V sur le schéma 0143,200/C.
    // Les attentes de cette carte proviennent donc des registres chip-select du 80186.
    private readonly Cpu80186 _cpu;
    private long _target;
    internal Master512Board(TubeUla tube, ReadOnlySpan<byte> rom)
    {
        if (rom.Length != C.Master512RomBytes) throw new ArgumentException(nameof(rom));
        _tube = tube; _rom = rom.ToArray(); _cpu = new(ReadBusMemory, WriteBusMemory, ReadPort, WritePort);
        _cpu.TimerOutputChanged = TimerOutputChanged;
        _cpu.HoldAcknowledgeChanged = HoldAcknowledgeChanged;
        _cpu.BusClocksAdvanced = _refresh.AdvanceClocks;
        _cpu.ChipSelectChanged = selected => _refresh.UpperMemorySelected((selected & Cpu80186ChipSelect.Upper) != 0);
        _tube.ParasiteResetChanged = _cpu.SetReset;
        _tube.DmaRequestChanged = asserted => _cpu.SetDmaRequest(TubeDmaChannel, asserted);
        _tube.ParasiteIrqChanged = asserted => _cpu.SetExternalInterrupt(TubeInterruptPin, asserted);
        _cpu.SetReset(_tube.ResetHeld);
        Reset();
    }
    private void TimerOutputChanged(int timer, bool level)
    {
        if (timer == HoldTimer)
        {
            _refresh.TimerOutput(level);
            _cpu.HoldInput = _refresh.HoldRequested;
        }
        else if (timer == NmiTimer) _cpu.Nmi = !level;
    }
    private void HoldAcknowledgeChanged(bool asserted)
    {
        _refresh.HoldAcknowledged(asserted);
        _cpu.HoldInput = _refresh.HoldRequested;
    }
    internal void Reset()
    {
        _refresh.Reset();
        _cpu.HoldInput = false;
        _cpu.Reset();
        _cpu.SetTimerInput(HoldTimer, true);
        _cpu.SetTimerInput(NmiTimer, true);
        _cpu.SetDmaRequest(TubeDmaChannel, _tube.DmaRequest);
        _cpu.SetExternalInterrupt(TubeInterruptPin, _tube.ParasiteIrq);
        _target = _cpu.Cycles;
    }
    internal void RunHostCycle()
    {
        _target += C.Master512CyclesPerHostCycle;
        while (_cpu.Cycles < _target)
        {
            _cpu.Step();
        }
    }
    internal byte ReadMemory(int address)
    {
        address &= C.X86AddressMask;
        return address >= C.X86RomStart ? _rom[address & (C.Master512RomBytes - 1)]
            : _ram[address >= C.X86RamMirrorStart ? address - C.X86RamMirrorOffset : address];
    }
    internal void WriteMemory(int address, byte value)
    {
        address &= C.X86AddressMask;
        if (address < C.X86RomStart) _ram[address >= C.X86RamMirrorStart ? address - C.X86RamMirrorOffset : address] = value;
    }
    private bool TubeSelected => (_cpu.ActiveChipSelect & Cpu80186ChipSelect.Peripheral1) != 0;
    private byte ReadBusMemory(int address)
    {
        if (TubeSelected) return (address & 1) == 0 ? _tube.ParasiteRead((address >> 1) & C.RegisterMask) : byte.MaxValue;
        if ((_cpu.ActiveChipSelect & Cpu80186ChipSelect.Upper) != 0) return _rom[address & (C.Master512RomBytes - 1)];
        return address < C.X86RomStart ? ReadMemory(address) : byte.MaxValue;
    }
    private void WriteBusMemory(int address, byte value)
    {
        if (TubeSelected)
        {
            if ((address & 1) == 0) _tube.ParasiteWrite((address >> 1) & C.RegisterMask, value);
        }
        else if ((_cpu.ActiveChipSelect & Cpu80186ChipSelect.Upper) == 0) WriteMemory(address, value);
    }
    private byte ReadPort(ushort port) => TubeSelected && (port & 1) == 0
        ? _tube.ParasiteRead((port >> 1) & C.RegisterMask) : byte.MaxValue;
    private void WritePort(ushort port, byte value)
    {
        if (TubeSelected && (port & 1) == 0) _tube.ParasiteWrite((port >> 1) & C.RegisterMask, value);
    }
}
