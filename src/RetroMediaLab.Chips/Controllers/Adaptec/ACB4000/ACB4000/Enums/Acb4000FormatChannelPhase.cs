namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Enums;

internal enum Acb4000FormatChannelPhase
{
    Idle,
    WaitForIndex,
    Gap,
    IdPreamble,
    Id,
    DataPreamble,
    Data,
    PostData,
    Tail,
    Complete
}
