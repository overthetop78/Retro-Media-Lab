using RetroMediaLab.Chips.CPU.Common.Enums;
using RetroMediaLab.Chips.CPU.Common.Interfaces;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Services;

/// <summary>CPU-facing wires of the 400,005 buffers; the board owns the external Acorn bus.</summary>
internal sealed class SystemFastCpuBus(AcornSystemMachine board) : ICpuBus, ICpuBusCyclePins
{
    internal bool ReadHigh { get; private set; } = true;
    private bool _buffersEnabled = true, _phaseHigh;
    private byte? _latchedRead;
    internal bool BuffersEnabled
    {
        get => _buffersEnabled;
        set
        {
            if (_buffersEnabled == value) return;
            _buffersEnabled = value;
            board.SetFastBusPhase(_phaseHigh && value, ReadHigh);
        }
    }
    internal Action<bool>? ReadWriteChanged { get; set; }
    public CpuBusControlSignals ControlSignals => CpuBusControlSignals.None;
    public bool Irq => false;
    public bool Nmi => false;

    public byte Read(ushort address)
    {
        try { return BuffersEnabled ? _latchedRead ?? board.Read(address) : byte.MaxValue; }
        finally { board.SetFastBusPhase(false, ReadHigh); }
    }
    public void Write(ushort address, byte value)
    {
        try { if (BuffersEnabled) board.Write(address, value); }
        finally { board.SetFastBusPhase(false, ReadHigh); }
    }

    // The 24 MHz network drives the board independently of CPU transactions.
    public void Tick() { }
    public void PrepareRead(ushort address)
    {
        _latchedRead = null;
        if (BuffersEnabled) board.PrepareFastBus(address);
        SetReadWrite(true);
    }
    public void PrepareWrite(ushort address, byte data)
    {
        _latchedRead = null;
        if (BuffersEnabled) board.PrepareFastBus(address, data);
        SetReadWrite(false);
    }

    internal void SetPhase(bool high)
    {
        if (!high && _phaseHigh && BuffersEnabled && ReadHigh) _latchedRead = board.FastDiskBusData;
        _phaseHigh = high;
        board.SetFastBusPhase(high && BuffersEnabled, ReadHigh);
    }

    private void SetReadWrite(bool read)
    {
        if (ReadHigh == read) return;
        ReadHigh = read;
        ReadWriteChanged?.Invoke(read);
    }
}
