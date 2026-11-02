namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Hd63463.Enums;

internal enum Hd63463Phase
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
