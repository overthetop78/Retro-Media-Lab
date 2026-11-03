using RetroMediaLab.Chips.IO.NationalSemiconductor.INS815x.Chip8154.Enums;
using C = RetroMediaLab.Chips.IO.NationalSemiconductor.INS815x.Chip8154.Constants.RamIoConstants;

namespace RetroMediaLab.Chips.IO.NationalSemiconductor.INS815x.Chip8154.Services;

public sealed partial class RamIo8154
{
    private byte _inputA;
    private bool _strobeHigh = true;
    private bool _request;
    private bool _irq;
    private byte _publishedA = byte.MaxValue;
    private byte _publishedB = byte.MaxValue;
    private byte _publishedMaskA;
    private byte _publishedMaskB;

    public bool Irq => _irq;
    public bool StrobeHigh => _strobeHigh;
    public byte PortADriveMask => !_resetHigh || _operatingMode == RamIoMode.StrobedOutputThreeState && _strobeHigh
        ? (byte)0 : _directionA;
    public byte PortBDriveMask => _resetHigh ? _directionB : (byte)0;
    private bool IsStrobedOutput => _operatingMode is RamIoMode.StrobedOutput or RamIoMode.StrobedOutputThreeState;

    /// <summary>PB7 STB/ACK input level. It acts independently of CPU chip selection.
    /// A ReadPortB provider must describe the same physical PB7 level.</summary>
    public void SetStrobe(bool high)
    {
        if (_strobeHigh == high) return;
        _strobeHigh = high;
        if (!_resetHigh || _operatingMode == RamIoMode.Basic) return;
        if (!high) _outputB |= C.BufferFlag;
        else
        {
            if (_operatingMode == RamIoMode.StrobedInput) _inputA = PinsA();
            _request = true;
        }
        PublishOutputs();
    }

    // Pads are read, not output latches. External low levels can load a high output;
    // an unconnected callback models released/high inputs, not another push-pull driver.
    private byte PinsA() => (byte)((_outputA | ~(!_resetHigh
        || _operatingMode == RamIoMode.StrobedOutputThreeState && _strobeHigh ? 0 : _directionA))
        & (ReadPortA?.Invoke() ?? byte.MaxValue));
    private byte PinsB() => (byte)((_outputB | ~(_resetHigh ? _directionB : 0))
        & (ReadPortB?.Invoke() ?? (_strobeHigh ? byte.MaxValue : C.StrobeLowInputs)));
    private byte ReadA() => _operatingMode == RamIoMode.StrobedInput && _strobeHigh ? _inputA : PinsA();
    private byte ReadB()
    {
        var pins = PinsB();
        return _operatingMode == RamIoMode.Basic ? pins
            : (byte)((pins & ~C.InterruptEnable) | (_irq ? C.InterruptEnable : 0));
    }

    private void PublishOutputs(bool observeA = false, bool observeB = false)
    {
        var previousIrq = _irq;
        _irq = _resetHigh && _operatingMode != RamIoMode.Basic && _request && (_outputB & C.InterruptEnable) != 0;
        PublishPortA(observeA);
        PublishPortB(observeB);
        if (previousIrq != _irq) InterruptChanged?.Invoke(_irq);
    }

    private void PublishPortA(bool observeA)
    {
        var maskA = PortADriveMask;
        var valueA = (byte)(_outputA | ~maskA);
        var changedA = _publishedA != valueA || _publishedMaskA != maskA;
        if (changedA)
        {
            _publishedA = valueA;
            _publishedMaskA = maskA;
            PortADriveChanged?.Invoke(valueA, maskA);
        }
        if (changedA || observeA) PortAPinsChanged?.Invoke(valueA);
    }

    private void PublishPortB(bool observeB)
    {
        var maskB = PortBDriveMask;
        var valueB = (byte)(_outputB | ~maskB);
        var changedB = _publishedB != valueB || _publishedMaskB != maskB;
        if (changedB)
        {
            _publishedB = valueB;
            _publishedMaskB = maskB;
            PortBDriveChanged?.Invoke(valueB, maskB);
        }
        if (changedB || observeB) PortBPinsChanged?.Invoke(valueB);
    }
}
