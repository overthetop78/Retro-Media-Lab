namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Enums;

internal enum Acb4000Stage
{
    Idle,
    Command,
    DataIn,
    DataOut,
    Verify,
    Read,
    SearchData,
    WriteSearch,
    Write,
    Format,
    Positioning,
    Parameters,
    Status,
    Message
}
