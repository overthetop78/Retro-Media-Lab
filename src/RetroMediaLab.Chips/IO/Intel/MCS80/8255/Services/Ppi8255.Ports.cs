using RetroMediaLab.Chips.IO.Intel.MCS80.Chip8255.Enums;
using C = RetroMediaLab.Chips.IO.Intel.MCS80.Chip8255.Constants.Ppi8255Constants;

namespace RetroMediaLab.Chips.IO.Intel.MCS80.Chip8255.Services;

public sealed partial class Ppi8255
{
    private byte _publishedA = byte.MaxValue;
    private byte _publishedB = byte.MaxValue;
    private byte _publishedC = byte.MaxValue;
    private byte _maskA;
    private byte _maskB;
    private byte _maskC;
    private bool _publishedInterruptA;
    private bool _publishedInterruptB;

    private byte StatusC(bool includeEnables)
    {
        var value = 0;
        if (_modeA != PpiMode.Basic)
        {
            if (_intrA) value |= C.InterruptA;
            if (IsInputHandshake(C.PortA))
            {
                if (_ibfA) value |= C.InputBufferA;
                if (includeEnables && _inteInputA) value |= C.InputStrobeA;
            }
            if (IsOutputHandshake(C.PortA))
            {
                if (_obfA) value |= C.OutputBufferA;
                if (includeEnables && _inteOutputA) value |= C.OutputAcknowledgeA;
            }
        }
        if (_modeB == PpiMode.Strobed)
        {
            if (_intrB) value |= C.InterruptB;
            if (_bInput ? _ibfB : _obfB) value |= C.BufferB;
            if (includeEnables && _inteB) value |= C.HandshakeB;
        }
        return (byte)value;
    }

    private byte ReadC() => (byte)((_outputC & _freeC & ~_freeInputC)
        | ((ReadPortC?.Invoke() ?? _inputC) & _freeInputC)
        | (_modeA == PpiMode.Basic && _modeB == PpiMode.Basic ? 0 : StatusC(true)));

    private void PublishPortA()
    {
        var mask = PortADriveMask;
        var value = (byte)(_outputA | ~mask);
        var changed = _publishedA != value || _maskA != mask;
        _publishedA = value;
        _maskA = mask;
        if (changed) PortADriveChanged?.Invoke(value, mask);
        PortAPinsChanged?.Invoke(value);
    }

    private void PublishPortB()
    {
        var mask = PortBDriveMask;
        var value = (byte)(_outputB | ~mask);
        var changed = _publishedB != value || _maskB != mask;
        _publishedB = value;
        _maskB = mask;
        if (changed) PortBDriveChanged?.Invoke(value, mask);
        PortBPinsChanged?.Invoke(value);
    }

    private void Publish(byte observe = 0)
    {
        var maskA = PortADriveMask;
        var maskB = PortBDriveMask;
        var maskC = PortCDriveMask;
        var valueA = (byte)(_outputA | ~maskA);
        var valueB = (byte)(_outputB | ~maskB);
        var valueC = (byte)((_outputC & _freeC & ~_freeInputC) | StatusC(false) | ~maskC);
        var changedA = valueA != _publishedA || maskA != _maskA;
        var changedB = valueB != _publishedB || maskB != _maskB;
        var changedC = valueC != _publishedC || maskC != _maskC;
        var interruptAChanged = _publishedInterruptA != _intrA;
        var interruptBChanged = _publishedInterruptB != _intrB;
        // Commit the complete pin state before any observer may read another port.
        _publishedA = valueA; _maskA = maskA;
        _publishedB = valueB; _maskB = maskB;
        _publishedC = valueC; _maskC = maskC;
        _publishedInterruptA = _intrA;
        _publishedInterruptB = _intrB;
        if (changedA) PortADriveChanged?.Invoke(valueA, maskA);
        if (changedB) PortBDriveChanged?.Invoke(valueB, maskB);
        if (changedC) PortCDriveChanged?.Invoke(valueC, maskC);
        if (changedA || (observe & C.ObserveA) != 0) PortAPinsChanged?.Invoke(valueA);
        if (changedB || (observe & C.ObserveB) != 0) PortBPinsChanged?.Invoke(valueB);
        if (changedC || (observe & C.ObserveC) != 0) PortCPinsChanged?.Invoke(valueC);
        if (interruptAChanged) InterruptAChanged?.Invoke(_intrA);
        if (interruptBChanged) InterruptBChanged?.Invoke(_intrB);
    }
}
