namespace RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Enums;

internal enum Upd765SerialPhase
{
    Idle,
    AwaitIndex,
    SearchId,
    Id,
    SearchData,
    Data,
    Crc,
    Write,
    Format
}
