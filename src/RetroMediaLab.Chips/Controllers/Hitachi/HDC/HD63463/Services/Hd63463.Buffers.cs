using System.Buffers.Binary;
using C = RetroMediaLab.Chips.Controllers.Hitachi.HDC.HD63463.Constants.Hd63463Constants;
using Command = RetroMediaLab.Chips.Controllers.Hitachi.HDC.HD63463.Enums.Hd63463Command;
using Phase = RetroMediaLab.Chips.Controllers.Hitachi.HDC.HD63463.Enums.Hd63463Phase;

namespace RetroMediaLab.Chips.Controllers.Hitachi.HDC.HD63463.Services;

public sealed partial class Hd63463
:RetroMediaLab.Chips.Controllers.Hitachi.HDC.HD63463.Interfaces.IHd63463{
    private bool DmaMode => (_specification[C.ModeParameter] & C.DmaTransfer) != 0;

    private void BeginBufferAccess(int length)
    {
        if (length != C.BufferParameterLength)
        {
            Finish(C.InvalidParameter);
            return;
        }
        var pointer = ParameterWord(0);
        if ((pointer & C.BufferReservedMask) != 0 || (pointer & 1) != 0)
        {
            Finish(C.InvalidParameter);
            return;
        }
        _buffer = (pointer & C.BufferSelect) == 0 ? 0 : 1;
        _bufferOffset = pointer & C.BufferOffsetMask;
        _phase = Phase.OpeningBuffer;
        _status |= C.Busy;
        var cycles = _command is Command.OpenBufferRead or Command.OpenBufferWrite
            ? C.OpenBufferCycles : C.BufferDmaCycles;
        _delay = ControllerCycles(cycles);
    }

    private void OpenBuffer()
    {
        _phase = _command switch
        {
            Command.OpenBufferRead => Phase.OpenReadBuffer,
            Command.OpenBufferWrite => Phase.OpenWriteBuffer,
            Command.BufferToMemory => Phase.ReadBuffer,
            _ => Phase.WriteBuffer
        };
        // PIO has completed its command, but DTR remains connected to the buffer
        // until Recall. DMA is completed only by the external DONE input.
        if (_phase is Phase.OpenReadBuffer or Phase.OpenWriteBuffer) _status = C.ParameterReject;
        else SetDrq(true);
        UpdateInterrupt();
    }

    private ushort ReadBufferWord()
    {
        if (_bufferOffset > C.SectorBytes - C.ParameterWordBytes) return ushort.MaxValue;
        var value = BinaryPrimitives.ReadUInt16LittleEndian(_buffers[_buffer].AsSpan(_bufferOffset));
        _bufferOffset += C.ParameterWordBytes;
        return value;
    }

    private void WriteBufferWord(ushort value)
    {
        if (_bufferOffset > C.SectorBytes - C.ParameterWordBytes) return;
        BinaryPrimitives.WriteUInt16LittleEndian(_buffers[_buffer].AsSpan(_bufferOffset), value);
        _bufferOffset += C.ParameterWordBytes;
    }

    public void DmaDone()
    {
        if (_phase == Phase.ReceivingFormatIdentifiers && _drq) { BeginFormatTrack(); return; }
        if (_drq && _phase is Phase.Reading or Phase.Writing or Phase.Comparing)
        {
            if (_phase == Phase.Reading) StopReadDma();
            else QueueWriteBuffer(done: true);
            return;
        }
        if (_phase == Phase.ReadIdentifierBuffer && _drq) { Finish(); return; }
        if (_phase is not (Phase.ReadBuffer or Phase.WriteBuffer) || !_drq) return;
        CompleteBufferDma();
    }

    private void CompleteBufferDma()
    {
        SetDrq(false);
        _phase = Phase.Idle;
        _status = C.ParameterReject | C.CommandEnd;
        _parameterRead = _parameterWrite = 0;
        UpdateInterrupt();
    }
}
