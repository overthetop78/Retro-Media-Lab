using RetroMediaLab.Chips.CPU.Common.Enums;
using RetroMediaLab.Chips.CPU.Common.Interfaces;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Services;

/// <summary>CPU-facing wires of the 400,005 buffers; the board owns the external Acorn bus.</summary>
internal sealed class SystemFastCpuBus(AcornSystemMachine board) : ICpuBus, ICpuBusCyclePins
{
    internal bool ReadHigh { get; private set; } = true;
    internal bool BuffersEnabled { get; set; } = true;
    internal Action<bool>? ReadWriteChanged { get; set; }
    public CpuBusControlSignals ControlSignals => CpuBusControlSignals.None;
    public bool Irq => false;
    public bool Nmi => false;

    public byte Read(ushort address) => BuffersEnabled ? board.Read(address) : byte.MaxValue;
    public void Write(ushort address, byte value)
    {
        if (BuffersEnabled) board.Write(address, value);
    }

    // The 24 MHz network drives the board independently of CPU transactions.
    public void Tick() { }
    public void PrepareRead(ushort address) => SetReadWrite(true);
    public void PrepareWrite(ushort address, byte data) => SetReadWrite(false);

    private void SetReadWrite(bool read)
    {
        if (ReadHigh == read) return;
        ReadHigh = read;
        ReadWriteChanged?.Invoke(read);
    }
}
