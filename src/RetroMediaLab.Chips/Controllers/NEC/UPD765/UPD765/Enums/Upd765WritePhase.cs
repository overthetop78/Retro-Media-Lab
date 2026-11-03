namespace RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Enums;

internal enum Upd765WritePhase
{
    Idle,
    Sync,
    A1,
    Mark,
    Data,
    CrcHigh,
    CrcLow,
    Tail,
    FormatLead,
    FormatIndexSync,
    FormatIndexC2,
    FormatIndexMark,
    FormatGap1,
    FormatIdSync,
    FormatIdA1,
    FormatIdMark,
    FormatId,
    FormatIdCrcHigh,
    FormatIdCrcLow,
    FormatGap2,
    FormatDataSync,
    FormatDataA1,
    FormatDataMark,
    FormatFill,
    FormatDataCrcHigh,
    FormatDataCrcLow,
    FormatGap3,
    FormatTail
}
