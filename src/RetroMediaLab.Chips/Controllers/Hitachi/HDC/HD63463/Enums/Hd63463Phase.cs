namespace RetroMediaLab.Chips.Controllers.Hitachi.HDC.HD63463.Enums;

public enum Hd63463Phase
{
    Idle,
    Resetting,
    Executing,
    Recalling,
    Specifying,
    AbortingOutputs,
    Aborting,
    EnteringTest,
    OpeningBuffer,
    OpenReadBuffer,
    OpenWriteBuffer,
    ReadBuffer,
    WriteBuffer,
    ReadingIdentifiers,
    WaitingIdentifierIndex,
    ReadIdentifierBuffer,
    Reading,
    Writing,
    Comparing,
    Formatting,
    ReceivingFormatIdentifiers,
    WaitingFormatIndex,
    FinishingFormatTrack,
    Checking,
    Polling,
    Positioning
}
