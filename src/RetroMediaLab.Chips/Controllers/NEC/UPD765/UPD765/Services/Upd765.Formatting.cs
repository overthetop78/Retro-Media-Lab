using RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Enums;
using C = RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Constants.Upd765Constants;

namespace RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Services;

public sealed partial class Upd765
{
    private void BeginFormat()
    {
        _serialPhase = Upd765SerialPhase.Format;
        _phase = Upd765Phase.Writing;
        _writer.DoubleDensity = Mfm;
        _writer.CrcEnabled = false;
        _writer.SetGate(true);
        _hostDataReady = _writtenDataWord = _closeAfterWord = false;
        _offset = _hostOffset = 0;
        SetWritePhase(Upd765WritePhase.FormatLead);
    }

    private void RequestFormatWord()
    {
        switch (_writePhase)
        {
            case Upd765WritePhase.FormatLead:
                RepeatWord(GapByte, Mfm ? C.LeadMfmBytes : C.LeadFmBytes, Upd765WritePhase.FormatIndexSync);
                break;
            case Upd765WritePhase.FormatIndexSync:
                RepeatWord(0, SyncBytes, Mfm ? Upd765WritePhase.FormatIndexC2 : Upd765WritePhase.FormatIndexMark);
                break;
            case Upd765WritePhase.FormatIndexC2:
                RepeatWord(C.SyncC2, C.MfmSyncWords, Upd765WritePhase.FormatIndexMark, Upd765WriteWordKind.SyncC2);
                break;
            case Upd765WritePhase.FormatIndexMark:
                QueueWord(C.IndexAddressMark, Mfm ? Upd765WriteWordKind.Data : Upd765WriteWordKind.FmIndex);
                SetWritePhase(Upd765WritePhase.FormatGap1);
                break;
            case Upd765WritePhase.FormatGap1:
                RepeatWord(GapByte, Mfm ? C.Gap1MfmBytes : C.Gap1FmBytes, Upd765WritePhase.FormatIdSync);
                break;
            case Upd765WritePhase.FormatIdSync:
                _writer.CrcEnabled = false;
                if (_writeCount == 0) { _offset = 0; SetRequest(true); if (!Active) return; }
                RepeatWord(0, SyncBytes, Mfm ? Upd765WritePhase.FormatIdA1 : Upd765WritePhase.FormatIdMark);
                break;
            case Upd765WritePhase.FormatIdA1:
                if (_writeCount == 0) StartWriteCrc();
                RepeatWord(C.SyncA1, C.MfmSyncWords, Upd765WritePhase.FormatIdMark, Upd765WriteWordKind.SyncA1);
                break;
            case Upd765WritePhase.FormatIdMark:
                if (!Mfm) StartWriteCrc();
                QueueWord(C.IdAddressMark, Mfm ? Upd765WriteWordKind.Data : Upd765WriteWordKind.FmAddress);
                SetWritePhase(Upd765WritePhase.FormatId);
                break;
            case Upd765WritePhase.FormatId:
                if (!_hostDataReady) { Overrun(); return; }
                _formatId[_offset++] = _hostData;
                QueueWord(_hostData);
                _hostDataReady = false;
                if (_offset == C.IdentifierBytes) SetWritePhase(Upd765WritePhase.FormatIdCrcHigh);
                else SetRequest(true);
                break;
            case Upd765WritePhase.FormatIdCrcHigh:
                _writeCrc = _writer.Crc;
                QueueWord((byte)(_writeCrc >> C.BitsPerByte));
                SetWritePhase(Upd765WritePhase.FormatIdCrcLow);
                break;
            case Upd765WritePhase.FormatIdCrcLow:
                QueueWord((byte)_writeCrc);
                SetWritePhase(Upd765WritePhase.FormatGap2);
                break;
            case Upd765WritePhase.FormatGap2:
                _writer.CrcEnabled = false;
                RepeatWord(GapByte, Gap2Bytes, Upd765WritePhase.FormatDataSync);
                break;
            case Upd765WritePhase.FormatDataSync:
                RepeatWord(0, SyncBytes, Mfm ? Upd765WritePhase.FormatDataA1 : Upd765WritePhase.FormatDataMark);
                break;
            case Upd765WritePhase.FormatDataA1:
                if (_writeCount == 0) StartWriteCrc();
                RepeatWord(C.SyncA1, C.MfmSyncWords, Upd765WritePhase.FormatDataMark, Upd765WriteWordKind.SyncA1);
                break;
            case Upd765WritePhase.FormatDataMark:
                if (!Mfm) StartWriteCrc();
                QueueWord(C.DataAddressMark, Mfm ? Upd765WriteWordKind.Data : Upd765WriteWordKind.FmAddress);
                SetWritePhase(Upd765WritePhase.FormatFill);
                break;
            case Upd765WritePhase.FormatFill:
                _writtenDataWord = true;
                RepeatWord(_parameters[C.FormatFillParameter], C.MinimumSectorBytes << _parameters[C.FormatSizeParameter],
                    Upd765WritePhase.FormatDataCrcHigh);
                break;
            case Upd765WritePhase.FormatDataCrcHigh:
                _writeCrc = _writer.Crc;
                QueueWord((byte)(_writeCrc >> C.BitsPerByte));
                SetWritePhase(Upd765WritePhase.FormatDataCrcLow);
                break;
            case Upd765WritePhase.FormatDataCrcLow:
                QueueWord((byte)_writeCrc);
                SetWritePhase(Upd765WritePhase.FormatGap3);
                break;
            case Upd765WritePhase.FormatGap3:
                if (_writeCount == 0)
                {
                    _cylinder = _formatId[C.IdentifierCylinderOffset];
                    _logicalHead = _formatId[C.IdentifierHeadOffset];
                    _record = _formatId[C.IdentifierRecordOffset];
                    _size = _formatId[C.IdentifierSizeOffset];
                    _trackFields++;
                }
                _writer.CrcEnabled = false;
                RepeatWord(GapByte, _parameters[C.FormatGapParameter],
                    _trackFields == _parameters[C.FormatCountParameter] ? Upd765WritePhase.FormatTail : Upd765WritePhase.FormatIdSync);
                break;
            case Upd765WritePhase.FormatTail:
                QueueWord(GapByte);
                break;
        }
    }
}
