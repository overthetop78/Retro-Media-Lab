using RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Enums;

namespace RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Services;

public partial class Cpu6502
{
    private bool _busPinsEnabled = true;
    private bool _memoryLockAsserted;
    private bool _directDataBusEnable = true;
    private bool _hasDirectDataBusEnable;
    private bool _busCyclePresented;
    private bool _busCycleWriting;
    private ushort _busCycleAddress;
    private byte _busCycleData;
    private bool _resetPinConnected;
    private bool _resetInput;
    private bool _readyPinConnected;
    private bool _readyInput = true;
    private bool _overflowPinConnected;
    private bool _overflowInputEdge;

    private bool ResetInputActive => _resetPinConnected ? _resetInput : bus.ResetRequested;
    private bool ReadyInputHigh => _readyPinConnected ? _readyInput : bus.Ready;

    public void SetReset(bool asserted)
    {
        _resetPinConnected = _samplesReset = true;
        _resetInput = asserted;
    }

    public void SetReady(bool high)
    {
        if (Variant == Cpu6502Variant.Gte65Sc12) throw new NotSupportedException(nameof(SetReady));
        _readyPinConnected = _samplesReady = true;
        _readyInput = high;
    }

    public void SetOverflowInput(bool asserted)
    {
        _overflowPinConnected = true;
        _samplesOverflow = true;
        if (_overflowLevel == asserted) return;
        _overflowLevel = asserted;
        if (asserted) _overflowInputEdge = true;
    }

    private void ResetOverflowInput()
    {
        if (!_overflowPinConnected) _overflowLevel = _samplesOverflow && bus.SetOverflow;
    }

    protected bool BusPinsEnabled => _busPinsEnabled;
    protected bool MemoryLockAsserted => _memoryLockAsserted;
    protected bool DirectDataBusEnabled => _hasDirectDataBusEnable
        ? _directDataBusEnable : !_samplesDataEnable || bus.DataBusEnabled;
    protected Action<bool>? BusPinsEnableChanged { get; set; }
    protected Action<bool>? MemoryLockOutputChanged { get; set; }

    protected void SetDirectDataBusEnable(bool high)
    {
        _hasDirectDataBusEnable = true;
        _directDataBusEnable = high;
    }

    protected void SetBusPinsEnable(bool high)
    {
        if (_busPinsEnabled == high) return;
        _busPinsEnabled = high;
        if (!high) _cyclePins?.ReleaseBus();
        else if (_busCyclePresented)
        {
            if (_busCycleWriting) _cyclePins?.PrepareWrite(_busCycleAddress, _busCycleData);
            else _cyclePins?.PrepareRead(_busCycleAddress);
        }
        BusPinsEnableChanged?.Invoke(high);
    }

    private void SetMemoryLock(bool asserted)
    {
        if (Variant != Cpu6502Variant.Gte65Sc102 || _memoryLockAsserted == asserted) return;
        _memoryLockAsserted = asserted;
        MemoryLockOutputChanged?.Invoke(asserted);
    }

    private void PresentCmosRead(ushort address)
    {
        _busCyclePresented = true;
        _busCycleWriting = false;
        _busCycleAddress = address;
        if (_busPinsEnabled) _cyclePins?.PrepareRead(address);
    }

    private void PresentCmosWrite(ushort address, byte value)
    {
        _busCyclePresented = _busCycleWriting = true;
        _busCycleAddress = address;
        _busCycleData = value;
        if (_busPinsEnabled) _cyclePins?.PrepareWrite(address, value);
    }
}
