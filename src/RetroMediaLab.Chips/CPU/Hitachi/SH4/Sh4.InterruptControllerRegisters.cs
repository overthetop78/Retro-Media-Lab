namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

internal sealed partial class Sh4InterruptControllerRegisters
{
    private const uint InterruptControlAddress = 0xFFD00000;
    private const uint PriorityAAddress = 0xFFD00004;
    private const uint PriorityBAddress = 0xFFD00008;
    private const uint PriorityCAddress = 0xFFD0000C;
    private const uint PriorityDAddress = 0xFFD00010;

    internal uint PriorityA => GetValue(PriorityAAddress);
    internal uint PriorityB => GetValue(PriorityBAddress);
    internal uint PriorityC => GetValue(PriorityCAddress);

    private void WritePriorityA(uint address, ushort value)
    {
        if (GetValue(PriorityAAddress) != value)
        {
            SetValue(PriorityAAddress, value);
            _interrupts.RebuildSortedInterruptTable();
        }
    }

    private void WritePriorityB(uint address, ushort value)
    {
        if (GetValue(PriorityBAddress) != value)
        {
            SetValue(PriorityBAddress, value);
            _interrupts.RebuildSortedInterruptTable();
        }
    }

    private void WritePriorityC(uint address, ushort value)
    {
        if (GetValue(PriorityCAddress) != value)
        {
            SetValue(PriorityCAddress, value);
            _interrupts.RebuildSortedInterruptTable();
        }
    }

    private static ushort ReadPriorityD(uint address) => 0;

    internal override void Initialize()
    {
        base.Initialize();
        SetReadWrite(InterruptControlAddress, 2, 0x4380);
        SetWriteHandler16(PriorityAAddress, WritePriorityA);
        SetWriteHandler16(PriorityBAddress, WritePriorityB);
        SetWriteHandler16(PriorityCAddress, WritePriorityC);
        SetReadOnly16(PriorityDAddress, ReadPriorityD);
        _interrupts.Initialize();
        Reset();
    }

    internal override void Reset()
    {
        base.Reset();
        _interrupts.Reset();
    }

    internal override void Terminate()
    {
        base.Terminate();
        _interrupts.Terminate();
    }
}
