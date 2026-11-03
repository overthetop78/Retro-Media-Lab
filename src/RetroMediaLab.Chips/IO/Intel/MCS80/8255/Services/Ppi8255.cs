using RetroMediaLab.Chips.IO.Intel.MCS80.Chip8255.Enums;
using RetroMediaLab.Chips.IO.Intel.MCS80.Chip8255.Interfaces;
using C = RetroMediaLab.Chips.IO.Intel.MCS80.Chip8255.Constants.Ppi8255Constants;

namespace RetroMediaLab.Chips.IO.Intel.MCS80.Chip8255.Services;

/// <summary>8255 parallel interface, including independent strobed groups and bidirectional A.
/// This static device advances on CPU and peripheral strobes, without an invented oscillator.</summary>
public sealed partial class Ppi8255 : IPpi8255
{
    private byte _control = C.ResetControl;
    private byte _outputA;
    private byte _outputB;
    private byte _outputC;
    private byte _inputA;
    private byte _inputB;
    private byte _inputC = byte.MaxValue;
    private byte _freeC = byte.MaxValue;
    private byte _freeInputC = byte.MaxValue;
    private byte _statusOutputC;
    private bool _aInput = true;
    private bool _bInput = true;
    private bool _resetHigh;
    private PpiMode _modeA;
    private PpiMode _modeB;

    public Func<byte>? ReadPortA { get; set; }
    public Func<byte>? ReadPortB { get; set; }
    public Func<byte>? ReadPortC { get; set; }
    public Action<byte>? PortAPinsChanged { get; set; }
    public Action<byte>? PortBPinsChanged { get; set; }
    public Action<byte>? PortCPinsChanged { get; set; }
    public Action<byte, byte>? PortADriveChanged { get; set; }
    public Action<byte, byte>? PortBDriveChanged { get; set; }
    public Action<byte, byte>? PortCDriveChanged { get; set; }
    public Action<bool>? InterruptAChanged { get; set; }
    public Action<bool>? InterruptBChanged { get; set; }
    public byte ControlWord => _control;
    public PpiMode GroupAMode => _modeA;
    public PpiMode GroupBMode => _modeB;
    public bool InterruptA => _intrA;
    public bool InterruptB => _intrB;
    public byte PortADriveMask => _resetHigh || (_modeA == PpiMode.Bidirectional
        ? (_inputC & C.OutputAcknowledgeA) != 0 : _aInput) ? (byte)0 : byte.MaxValue;
    public byte PortBDriveMask => _resetHigh || _bInput ? (byte)0 : byte.MaxValue;
    public byte PortCDriveMask => _resetHigh ? (byte)0 : (byte)((_freeC & ~_freeInputC) | _statusOutputC);

    public void Reset()
    {
        _readActive = _writeActive = _readingA = _readingB = _writingA = _writingB = false;
        Configure(C.ResetControl);
    }

    public void SetReset(bool high)
    {
        if (_resetHigh == high) return;
        _resetHigh = high;
        if (high) Reset();
    }

    public byte Read(int register)
    {
        if (_resetHigh) return byte.MaxValue;
        register &= C.RegisterMask;
        if (register == C.PortC)
        {
            var pins = ReadC();
            return _resetHigh ? byte.MaxValue : pins;
        }
        if (register == C.Control) return byte.MaxValue;
        if (!IsInputHandshake(register))
        {
            var pins = ReadData(register);
            return _resetHigh ? byte.MaxValue : pins;
        }
        BeginRead(register);
        var data = ReadData(register);
        EndRead(register);
        return _resetHigh ? byte.MaxValue : data;
    }

    public void Write(int register, byte value)
    {
        if (_resetHigh) return;
        register &= C.RegisterMask;
        if (_modeA != PpiMode.Basic || _modeB != PpiMode.Basic) BeginWrite(register);
        EndWrite(register, value);
    }

    private byte ReadData(int register)
    {
        if (register == C.PortA)
        {
            if (_modeA == PpiMode.Bidirectional || _modeA == PpiMode.Strobed && _aInput)
                return (_inputC & C.InputStrobeA) == 0 ? ReadPortA?.Invoke() ?? byte.MaxValue : _inputA;
            return _aInput ? ReadPortA?.Invoke() ?? byte.MaxValue : _outputA;
        }
        if (_modeB == PpiMode.Strobed && _bInput)
            return (_inputC & C.HandshakeB) == 0 ? ReadPortB?.Invoke() ?? byte.MaxValue : _inputB;
        return _bInput ? ReadPortB?.Invoke() ?? byte.MaxValue : _outputB;
    }

    private bool IsInputHandshake(int register) => register == C.PortA
        ? _modeA == PpiMode.Bidirectional || _modeA == PpiMode.Strobed && _aInput
        : register == C.PortB && _modeB == PpiMode.Strobed && _bInput;

    private bool IsOutputHandshake(int register) => register == C.PortA
        ? _modeA == PpiMode.Bidirectional || _modeA == PpiMode.Strobed && !_aInput
        : register == C.PortB && _modeB == PpiMode.Strobed && !_bInput;

    private void Configure(byte control)
    {
        _control = control;
        _modeA = (control & C.GroupAModeTwo) != 0 ? PpiMode.Bidirectional
            : (control & C.GroupAModeOne) != 0 ? PpiMode.Strobed : PpiMode.Basic;
        _modeB = (control & C.GroupBModeOne) != 0 ? PpiMode.Strobed : PpiMode.Basic;
        _aInput = (control & C.InputA) != 0;
        _bInput = (control & C.InputB) != 0;
        _outputA = _outputB = _outputC = _inputA = _inputB = 0;
        _ibfA = _ibfB = _inteInputA = _inteOutputA = _inteB = _intrA = _intrB = false;
        _obfA = _obfB = true;
        byte reserved = 0;
        _statusOutputC = 0;
        if (_modeA != PpiMode.Basic)
        {
            reserved = (byte)(_modeA == PpiMode.Bidirectional ? C.UpperC | C.InterruptA
                : _aInput ? C.InputStrobeA | C.InputBufferA | C.InterruptA
                : C.OutputAcknowledgeA | C.OutputBufferA | C.InterruptA);
            _statusOutputC = (byte)(C.InterruptA | (_modeA == PpiMode.Bidirectional
                ? C.InputBufferA | C.OutputBufferA : _aInput ? C.InputBufferA : C.OutputBufferA));
        }
        if (_modeB == PpiMode.Strobed)
        {
            reserved |= C.InterruptB | C.BufferB | C.HandshakeB;
            _statusOutputC |= C.InterruptB | C.BufferB;
        }
        _freeC = (byte)~reserved;
        _freeInputC = (byte)(_freeC & (((control & C.InputUpperC) != 0 ? C.UpperC : 0)
            | ((control & C.InputLowerC) != 0 ? C.LowerC : 0)));
        Publish(C.ObserveAll);
    }

    private void EndWrite(int register, byte value)
    {
        if (_resetHigh) return;
        switch (register)
        {
            case C.PortA:
                if (!_aInput || _modeA == PpiMode.Bidirectional) _outputA = value;
                if (IsOutputHandshake(register))
                {
                    _obfA = false;
                    _writingA = false;
                    UpdateInterrupts(C.ObserveA);
                    Publish(C.ObserveA);
                }
                else PublishPortA();
                return;
            case C.PortB:
                if (!_bInput) _outputB = value;
                if (IsOutputHandshake(register))
                {
                    _obfB = false;
                    _writingB = false;
                    UpdateInterrupts(C.ObserveB);
                    Publish(C.ObserveB);
                }
                else PublishPortB();
                return;
            case C.PortC:
                var writable = (byte)(_freeC & ~_freeInputC);
                if (_modeA != PpiMode.Basic) writable &= C.LowerC;
                _outputC = (byte)((_outputC & ~writable) | (value & writable));
                Publish(C.ObserveC);
                return;
            case C.Control:
                if ((value & C.ModeSet) != 0) Configure(value);
                else SetResetBit(value);
                return;
        }
    }
}
