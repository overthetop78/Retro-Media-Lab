namespace RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Enums;

internal enum Intel8271WritePhase
{
    Inactive,
    WaitIndex,
    Gap5,
    IndexSync,
    IndexMark,
    Gap1,
    IdSync,
    IdMark,
    Header,
    HeaderCrcHigh,
    HeaderCrcLow,
    Gap2,
    DataSync,
    DataMark,
    Payload,
    DataCrcHigh,
    DataCrcLow,
    Postamble,
    FinishSector,
    Gap3,
    Gap4Start,
    Gap4
}
