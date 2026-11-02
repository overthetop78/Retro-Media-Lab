using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMaster.Tube.Constants.TubeConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMaster.Tube.Services;

/// <summary>Carte 80186 à 10 MHz : RAM 512 Kio, BIOS 16 Kio, Tube en E/S et DMA du processeur.</summary>
internal sealed class Master512Board
{
    private const ushort InterruptMaskPort = 0xff28;
    private const ushort DmaBase = 0xffc0;
    private const ushort ExternalInterruptMask = 0x10;
    private ushort _interruptMask;
    private const int DmaRegisterBytes = 12;
    private const int DmaSource = 0;
    private const int DmaDestination = 4;
    private const int DmaCount = 8;
    private const int DmaControl = 10;
    private const byte DmaEnabled = 2;
    private const byte TubeInterruptVector = 12;
    private readonly TubeUla _tube;
    private readonly byte[] _ram = new byte[C.Master512RamBytes];
    private readonly byte[] _rom;
    private readonly byte[] _dma = new byte[DmaRegisterBytes];
    private readonly Cpu80186 _cpu;
    private long _target;
    internal Master512Board(TubeUla tube, ReadOnlySpan<byte> rom)
    {
        if (rom.Length != C.Master512RomBytes) throw new ArgumentException(nameof(rom));
        _tube = tube; _rom = rom.ToArray(); _cpu = new(ReadMemory, WriteMemory, ReadPort, WritePort) { InterruptVector = TubeInterruptVector };
        Reset();
    }
    internal void Reset() { _cpu.Reset(); _target = _cpu.Cycles; Array.Clear(_dma); _interruptMask = 0; }
    internal void RunHostCycle()
    {
        if (_tube.ResetHeld) { _target = _cpu.Cycles; return; }
        _target += C.Master512CyclesPerHostCycle;
        while (_cpu.Cycles < _target)
        {
            if (_tube.ParasiteNmi && (_dma[DmaControl] & DmaEnabled) != 0) TransferDma();
            _cpu.InterruptPending = _tube.ParasiteIrq && (_interruptMask & ExternalInterruptMask) == 0;
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
    private byte ReadPort(ushort port) => (port & ~15) == C.X86TubePort ? _tube.ParasiteRead(port >> 1)
        : port == InterruptMaskPort ? (byte)_interruptMask : port == InterruptMaskPort + 1 ? (byte)(_interruptMask >> 8) : byte.MaxValue;
    private void WritePort(ushort port, byte value)
    {
        if ((port & ~15) == C.X86TubePort) _tube.ParasiteWrite(port >> 1, value);
        else if (port >= DmaBase && port < DmaBase + DmaRegisterBytes) _dma[port - DmaBase] = value;
        else if (port == InterruptMaskPort) _interruptMask = (ushort)((_interruptMask & 0xff00) | value);
        else if (port == InterruptMaskPort + 1) _interruptMask = (ushort)((_interruptMask & 0xff) | value << 8);
    }
    private int DmaAddress(int index) => _dma[index] | _dma[index + 1] << 8 | (_dma[index + 2] & 15) << 16;
    private void SetDmaAddress(int index, int address)
    { _dma[index] = (byte)address; _dma[index + 1] = (byte)(address >> 8); _dma[index + 2] = (byte)(address >> 16); }
    private void TransferDma()
    {
        var source = DmaAddress(DmaSource); var destination = DmaAddress(DmaDestination);
        if (source < 256) { WriteMemory(destination, ReadPort((ushort)source)); SetDmaAddress(DmaDestination, destination + 1); }
        else { WritePort((ushort)destination, ReadMemory(source)); SetDmaAddress(DmaSource, source + 1); }
        var count = _dma[DmaCount] | _dma[DmaCount + 1] << 8;
        if (count != 0) { count--; _dma[DmaCount] = (byte)count; _dma[DmaCount + 1] = (byte)(count >> 8); }
    }
}
