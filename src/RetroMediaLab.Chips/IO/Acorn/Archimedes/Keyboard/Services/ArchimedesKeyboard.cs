using RetroMediaLab.Chips.IO.Acorn.Archimedes.Keyboard.Enums;
using C = RetroMediaLab.Chips.IO.Acorn.Archimedes.Keyboard.Constants.ArchimedesKeyboardConstants;

namespace RetroMediaLab.Chips.IO.Acorn.Archimedes.Keyboard.Services;

/// <summary>Protocole série du clavier et de la souris, sans modifier la mémoire RISC OS.</summary>
public sealed class ArchimedesKeyboard
:RetroMediaLab.Chips.IO.Acorn.Archimedes.Keyboard.Interfaces.IArchimedesKeyboard{
    private readonly bool[] _wanted = new bool[C.MatrixKeys];
    private readonly bool[] _reported = new bool[C.MatrixKeys];
    private readonly Queue<byte> _commandResponses = new();
    private ArchimedesKeyboardState _state;
    private byte? _outgoing;
    private byte _secondByte;
    private int _replyTicks;
    private int _pollTicks;
    private int _mouseX;
    private int _mouseY;
    private bool _mouseLocked;
    private bool _mouseRequested;
    public bool ScanEnabled { get; private set; }
    public bool MouseEnabled { get; private set; }
    public bool Break { get; private set; }
    public byte Leds { get; private set; }

    public ArchimedesKeyboard() => Reset();

    public void Reset()
    {
        Array.Clear(_reported);
        _commandResponses.Clear();
        _mouseX = _mouseY = 0;
        _mouseLocked = _mouseRequested = false;
        ScanEnabled = MouseEnabled = false;
        _state = ArchimedesKeyboardState.AwaitReset;
        _pollTicks = C.PollTicks;
        Send(C.HardReset);
    }

    public void SetInput(ReadOnlySpan<int> keys, bool breakPressed, bool left, bool middle, bool right, int deltaX, int deltaY)
    {
        Array.Clear(_wanted);
        foreach (var key in keys) _wanted[key] = true;
        Break = breakPressed;
        _wanted[C.MouseLeftKey] = left;
        _wanted[C.MouseMenuKey] = middle;
        _wanted[C.MouseRightKey] = right;
        if (_mouseLocked) return;
        var x = (long)_mouseX + deltaX;
        var y = (long)_mouseY - deltaY;
        _mouseX = (int)Math.Clamp(x, C.MouseMinimum, C.MouseMaximum);
        _mouseY = (int)Math.Clamp(y, C.MouseMinimum, C.MouseMaximum);
        _mouseLocked = x <= C.MouseMinimum || x >= C.MouseMaximum || y <= C.MouseMinimum || y >= C.MouseMaximum;
    }

    public void Write(byte command)
    {
        if (command == C.HardReset)
        {
            Reset();
            _state = ArchimedesKeyboardState.AwaitResetAckOne;
            return;
        }
        if (_state == ArchimedesKeyboardState.AwaitResetAckOne && command == C.ResetAckOne)
        {
            Send(C.ResetAckOne);
            _state = ArchimedesKeyboardState.AwaitResetAckTwo;
            return;
        }
        if (_state == ArchimedesKeyboardState.AwaitResetAckTwo && command == C.ResetAckTwo)
        {
            Send(C.ResetAckTwo);
            _state = ArchimedesKeyboardState.AwaitFinalAck;
            return;
        }
        if (_state is ArchimedesKeyboardState.AwaitReset or ArchimedesKeyboardState.AwaitResetAckOne or ArchimedesKeyboardState.AwaitResetAckTwo)
        {
            Reset();
            return;
        }
        // Les commandes peuvent être intercalées entre les acquittements d'un paquet.
        if (command <= C.LedMask) { Leds = command; return; }
        if (command == C.RequestReset) return;
        if (command == C.RequestMouse) { _mouseRequested = true; return; }
        if (command == C.RequestId)
        {
            _commandResponses.Enqueue(C.BritishKeyboardId);
            DispatchResponse();
            return;
        }
        if ((command & C.RequestDataMask) == C.RequestDataBase)
        {
            _commandResponses.Enqueue((byte)(C.DataResponseBase | (command & C.ColumnMask)));
            DispatchResponse();
            return;
        }
        if (_state == ArchimedesKeyboardState.AwaitFirstByteAck && command == C.ByteAck)
        {
            Send(_secondByte);
            _state = ArchimedesKeyboardState.AwaitFinalAck;
            return;
        }
        if (_state == ArchimedesKeyboardState.AwaitFinalAck && (command & C.ScanAckMask) == C.ScanAckBase)
        {
            ScanEnabled = (command & C.ScanEnable) != 0;
            MouseEnabled = (command & C.MouseEnable) != 0;
            _state = ArchimedesKeyboardState.Idle;
            DispatchResponse();
            return;
        }
        Reset();
    }

    private void DispatchResponse()
    {
        if (_state != ArchimedesKeyboardState.Idle || _outgoing.HasValue || _commandResponses.Count == 0) return;
        Send(_commandResponses.Dequeue());
        _state = ArchimedesKeyboardState.AwaitFinalAck;
    }

    private void Send(byte value)
    {
        _outgoing = value;
        _replyTicks = C.ReplyTicks;
    }

    private void SendPair(byte first, byte second)
    {
        Send(first);
        _secondByte = second;
        _state = ArchimedesKeyboardState.AwaitFirstByteAck;
    }

    public bool TryTakeTransmit(out byte value)
    {
        value = 0;
        if (!_outgoing.HasValue || _replyTicks > 0) return false;
        value = _outgoing.Value;
        _outgoing = null;
        return true;
    }

    public void Advance(int iocTicks)
    {
        if (iocTicks <= 0) return;
        _replyTicks = Math.Max(0, _replyTicks - iocTicks);
        _pollTicks -= iocTicks;
        if (_pollTicks > 0) return;
        _pollTicks = C.PollTicks - (-_pollTicks % C.PollTicks);
        if (_state != ArchimedesKeyboardState.Idle || _outgoing.HasValue) return;
        if (_mouseRequested || (_mouseLocked && MouseEnabled)) { SendMouse(); return; }
        if (ScanEnabled)
        {
            for (var matrix = 0; matrix < C.MatrixKeys; matrix++)
            {
                if (_wanted[matrix] == _reported[matrix]) continue;
                _reported[matrix] = _wanted[matrix];
                var prefix = _reported[matrix] ? C.KeyDownBase : C.KeyUpBase;
                SendPair((byte)(prefix | (matrix >> C.RowShift)), (byte)(prefix | (matrix & C.ColumnMask)));
                return;
            }
        }
        if (MouseEnabled && (_mouseX != 0 || _mouseY != 0)) SendMouse();
    }

    private void SendMouse()
    {
        SendPair((byte)(_mouseX & C.MouseMask), (byte)(_mouseY & C.MouseMask));
        _mouseX = _mouseY = 0;
        _mouseLocked = _mouseRequested = false;
    }
}
