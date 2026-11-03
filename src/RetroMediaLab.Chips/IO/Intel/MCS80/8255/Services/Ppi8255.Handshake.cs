using RetroMediaLab.Chips.IO.Intel.MCS80.Chip8255.Enums;
using C = RetroMediaLab.Chips.IO.Intel.MCS80.Chip8255.Constants.Ppi8255Constants;

namespace RetroMediaLab.Chips.IO.Intel.MCS80.Chip8255.Services;

public sealed partial class Ppi8255
{
    private bool _ibfA;
    private bool _ibfB;
    private bool _obfA = true;
    private bool _obfB = true;
    private bool _inteInputA;
    private bool _inteOutputA;
    private bool _inteB;
    private bool _intrA;
    private bool _intrB;
    private bool _readingA;
    private bool _readingB;
    private bool _writingA;
    private bool _writingB;

    public void SetPortCInput(byte levels)
    {
        var changed = (byte)(_inputC ^ levels);
        _inputC = levels;
        if (_resetHigh || changed == 0) return;
        byte groups = 0;
        if (IsInputHandshake(C.PortA) && (changed & C.InputStrobeA) != 0)
        {
            groups |= C.ObserveA;
            if ((levels & C.InputStrobeA) == 0) _ibfA = true;
            else _inputA = ReadPortA?.Invoke() ?? byte.MaxValue;
        }
        if (IsInputHandshake(C.PortB) && (changed & C.HandshakeB) != 0)
        {
            groups |= C.ObserveB;
            if ((levels & C.HandshakeB) == 0) _ibfB = true;
            else _inputB = ReadPortB?.Invoke() ?? byte.MaxValue;
        }
        if (IsOutputHandshake(C.PortA) && (changed & C.OutputAcknowledgeA) != 0)
        {
            groups |= C.ObserveA;
            if ((levels & C.OutputAcknowledgeA) == 0) _obfA = true;
        }
        if (IsOutputHandshake(C.PortB) && (changed & C.HandshakeB) != 0)
        {
            groups |= C.ObserveB;
            if ((levels & C.HandshakeB) == 0) _obfB = true;
        }
        if (groups == 0) return;
        UpdateInterrupts(groups);
        Publish();
    }

    private void UpdateInterrupts(byte groups = C.ObserveA | C.ObserveB)
    {
        if ((groups & C.ObserveA) != 0)
        {
            var input = _ibfA && _inteInputA && (_inputC & C.InputStrobeA) != 0 && !_readingA;
            var output = _obfA && _inteOutputA && (_inputC & C.OutputAcknowledgeA) != 0 && !_writingA;
            _intrA = !_resetHigh && (_modeA == PpiMode.Bidirectional ? input || output
                : _modeA == PpiMode.Strobed && (_aInput ? input : output));
        }
        if ((groups & C.ObserveB) != 0)
            _intrB = !_resetHigh && _modeB == PpiMode.Strobed && _inteB
                && (_inputC & C.HandshakeB) != 0 && (_bInput ? _ibfB && !_readingB : _obfB && !_writingB);
    }

    private void BeginRead(int register)
    {
        if (!IsInputHandshake(register)) return;
        if (register == C.PortA) _readingA = true;
        else _readingB = true;
        UpdateInterrupts(register == C.PortA ? C.ObserveA : C.ObserveB);
        Publish();
    }

    private void EndRead(int register)
    {
        if (!IsInputHandshake(register)) return;
        if (register == C.PortA) { _readingA = false; _ibfA = false; }
        else { _readingB = false; _ibfB = false; }
        UpdateInterrupts(register == C.PortA ? C.ObserveA : C.ObserveB);
        Publish();
    }

    private void BeginWrite(int register)
    {
        if (!IsOutputHandshake(register)) return;
        if (register == C.PortA) _writingA = true;
        else _writingB = true;
        UpdateInterrupts(register == C.PortA ? C.ObserveA : C.ObserveB);
        Publish();
    }

    private void SetResetBit(byte command)
    {
        var bit = (byte)(1 << ((command >> C.BitSelectionShift) & C.BitSelectionMask));
        var set = (command & C.BitSet) != 0;
        _outputC = set ? (byte)(_outputC | bit) : (byte)(_outputC & ~bit);
        byte groups = 0;
        if (IsInputHandshake(C.PortA))
        {
            if (bit == C.InputStrobeA) { _inteInputA = set; groups |= C.ObserveA; }
            if (bit == C.InputBufferA) { _ibfA = set; groups |= C.ObserveA; }
        }
        if (IsOutputHandshake(C.PortA))
        {
            if (bit == C.OutputAcknowledgeA) { _inteOutputA = set; groups |= C.ObserveA; }
            if (bit == C.OutputBufferA) { _obfA = set; groups |= C.ObserveA; }
        }
        if (_modeB == PpiMode.Strobed)
        {
            if (bit == C.HandshakeB) { _inteB = set; groups |= C.ObserveB; }
            if (bit == C.BufferB)
            {
                if (_bInput) _ibfB = set; else _obfB = set;
                groups |= C.ObserveB;
            }
        }
        UpdateInterrupts(groups);
        if (_modeA != PpiMode.Basic && bit == C.InterruptA) _intrA = set;
        if (_modeB == PpiMode.Strobed && bit == C.InterruptB) _intrB = set;
        Publish(C.ObserveC);
    }
}
