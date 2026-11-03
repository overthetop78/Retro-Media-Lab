using RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Enums;
using C = RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Constants.Upd765Constants;

namespace RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Services;

public sealed partial class Upd765
{
    private Upd765WritePhase _writePhase;
    private int _writeGapCells;
    private int _writeCount;
    private ushort _writeCrc;
    private bool _writeClockPin;
    private bool _writtenDataWord;
    private bool _closeAfterWord;
    private byte GapByte => Mfm ? C.MfmGap : C.FmGap;
    private int SyncBytes => Mfm ? C.SyncMfmBytes : C.SyncFmBytes;
    private int Gap2Bytes => Mfm ? C.Gap2MfmBytes : C.Gap2FmBytes;

    private void BeginWriteField()
    {
        _phase = Upd765Phase.Writing;
        _serialPhase = Upd765SerialPhase.Write;
        _writer.DoubleDensity = Mfm;
        _writer.SetGate(false);
        _writePhase = Upd765WritePhase.Sync;
        _writeCount = _offset = _hostOffset = 0;
        _hostDataReady = _closeAfterWord = _writtenDataWord = false;
        _length = C.MinimumSectorBytes << _size;
        _hostLength = _size == 0 && _parameters[C.LengthParameter] != 0
            ? Math.Min((int)_parameters[C.LengthParameter], C.MinimumSectorBytes) : _length;
        _writeGapCells = Gap2Bytes * C.EncodedCellsPerByte;
        SetRequest(true);
    }

    private void SetWritePhase(Upd765WritePhase phase)
    {
        _writePhase = phase;
        _writeCount = 0;
    }

    private void QueueWord(byte value, Upd765WriteWordKind kind = Upd765WriteWordKind.Data) => _writer.TryQueue(value, kind);

    private void RepeatWord(byte value, int count, Upd765WritePhase next,
        Upd765WriteWordKind kind = Upd765WriteWordKind.Data)
    {
        if (count == 0) { SetWritePhase(next); RequestWriteWord(); return; }
        QueueWord(value, kind);
        if (++_writeCount == count) SetWritePhase(next);
    }

    private void StartWriteCrc()
    {
        _writer.ResetCrc();
        _writer.CrcEnabled = true;
    }

    private void RequestWriteWord()
    {
        if (!Active || _resetAsserted) return;
        if (Formatting) { RequestFormatWord(); return; }
        switch (_writePhase)
        {
            case Upd765WritePhase.Sync:
                _writer.CrcEnabled = false;
                RepeatWord(0, SyncBytes, Mfm ? Upd765WritePhase.A1 : Upd765WritePhase.Mark);
                break;
            case Upd765WritePhase.A1:
                if (_writeCount == 0) StartWriteCrc();
                RepeatWord(C.SyncA1, C.MfmSyncWords, Upd765WritePhase.Mark, Upd765WriteWordKind.SyncA1);
                break;
            case Upd765WritePhase.Mark:
                if (!Mfm) StartWriteCrc();
                QueueWord(_command == Upd765Command.WriteDeleted ? C.DeletedAddressMark : C.DataAddressMark,
                    Mfm ? Upd765WriteWordKind.Data : Upd765WriteWordKind.FmAddress);
                SetWritePhase(Upd765WritePhase.Data);
                break;
            case Upd765WritePhase.Data:
                if (!_terminalCount && _offset < _hostLength && !_hostDataReady) { Overrun(); return; }
                QueueWord(_hostDataReady ? _hostData : (byte)0);
                _hostDataReady = false;
                _offset++;
                _writtenDataWord = true;
                if (_offset == _length) SetWritePhase(Upd765WritePhase.CrcHigh);
                else if (!_terminalCount && _offset < _hostLength) SetRequest(true);
                break;
            case Upd765WritePhase.CrcHigh:
                _writeCrc = _writer.Crc;
                QueueWord((byte)(_writeCrc >> C.BitsPerByte));
                SetWritePhase(Upd765WritePhase.CrcLow);
                break;
            case Upd765WritePhase.CrcLow:
                QueueWord((byte)_writeCrc);
                SetWritePhase(Upd765WritePhase.Tail);
                break;
            case Upd765WritePhase.Tail:
                _writer.CrcEnabled = false;
                QueueWord(GapByte);
                _closeAfterWord = true;
                break;
        }
    }

    private void CompleteWriteWord()
    {
        if (_writtenDataWord) { _writtenBytes[_unit]++; _writtenDataWord = false; }
        if (!_closeAfterWord || !Active) return;
        _closeAfterWord = false;
        var generation = _generation;
        _writer.SetGate(false);
        if (generation != _generation || _resetAsserted) return;
        _writePhase = Upd765WritePhase.Idle;
        FinishSector();
    }
}
