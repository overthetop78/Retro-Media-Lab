using RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Enums;
using C = RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Constants.Upd765Constants;

namespace RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Services;

public sealed partial class Upd765
{
    public Upd765Precompensation Precompensation => _writer.Precompensation;
    public bool Preshift0 => Precompensation == Upd765Precompensation.Early;
    public bool Preshift1 => Precompensation == Upd765Precompensation.Late;
    public Action<ulong, Upd765Precompensation>? PrecompensationChanged { get; set; }

    // No host acknowledge, request, CRC reset or sequencer mutation is permitted here.
    private (byte Value, Upd765WriteWordKind Kind)? PreviewNextWriteWord()
    {
        if (!Active || _resetAsserted || _closeAfterWord) return null;
        return _writePhase switch
        {
            Upd765WritePhase.Sync or Upd765WritePhase.FormatIndexSync or Upd765WritePhase.FormatIdSync
                or Upd765WritePhase.FormatDataSync => (0, Upd765WriteWordKind.Data),
            Upd765WritePhase.A1 or Upd765WritePhase.FormatIdA1 or Upd765WritePhase.FormatDataA1
                => (C.SyncA1, Upd765WriteWordKind.SyncA1),
            Upd765WritePhase.FormatIndexC2 => (C.SyncC2, Upd765WriteWordKind.SyncC2),
            Upd765WritePhase.Mark => Mark(_command == Upd765Command.WriteDeleted ? C.DeletedAddressMark : C.DataAddressMark),
            Upd765WritePhase.FormatIdMark => Mark(C.IdAddressMark),
            Upd765WritePhase.FormatDataMark => Mark(C.DataAddressMark),
            Upd765WritePhase.FormatIndexMark => (C.IndexAddressMark, Mfm ? Upd765WriteWordKind.Data : Upd765WriteWordKind.FmIndex),
            Upd765WritePhase.Data when _hostDataReady => (_hostData, Upd765WriteWordKind.Data),
            Upd765WritePhase.Data when _terminalCount || _offset >= _hostLength => (0, Upd765WriteWordKind.Data),
            Upd765WritePhase.FormatId when _hostDataReady => (_hostData, Upd765WriteWordKind.Data),
            Upd765WritePhase.FormatFill => (_parameters[C.FormatFillParameter], Upd765WriteWordKind.Data),
            Upd765WritePhase.CrcHigh or Upd765WritePhase.FormatIdCrcHigh or Upd765WritePhase.FormatDataCrcHigh
                => ((byte)(_writer.CrcAtWordEnd >> C.BitsPerByte), Upd765WriteWordKind.Data),
            Upd765WritePhase.CrcLow or Upd765WritePhase.FormatIdCrcLow or Upd765WritePhase.FormatDataCrcLow
                => ((byte)_writeCrc, Upd765WriteWordKind.Data),
            Upd765WritePhase.Tail or Upd765WritePhase.FormatLead or Upd765WritePhase.FormatGap1
                or Upd765WritePhase.FormatGap2 or Upd765WritePhase.FormatGap3 or Upd765WritePhase.FormatTail
                => (GapByte, Upd765WriteWordKind.Data),
            _ => null
        };
    }

    private (byte, Upd765WriteWordKind) Mark(byte value) =>
        (value, Mfm ? Upd765WriteWordKind.Data : Upd765WriteWordKind.FmAddress);
}
