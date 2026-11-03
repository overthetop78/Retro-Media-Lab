namespace RetroMediaLab.Chips.Network.Motorola.M6800.Chip6854.Enums;

internal enum AdlcTransmitPhase
{
    Idle,
    OpeningFlag,
    Data,
    FrameCheck,
    ClosingFlag,
    Abort
}
