namespace RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Enums;

public enum Intel8271SerialPhase
{
    Inactive,
    WaitIndex,
    SearchId,
    Header,
    SearchData,
    Data,
    DataCrc,
    WaitFormatIndex,
    WriteGap,
    Writing
}
