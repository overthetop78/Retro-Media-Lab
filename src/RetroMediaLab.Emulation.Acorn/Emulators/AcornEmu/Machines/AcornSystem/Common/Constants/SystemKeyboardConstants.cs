namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Constants;

internal static class SystemKeyboardConstants
{
    internal const byte DataMask = 0x7f;
    internal const byte StrobeMask = 0x80;
    internal const int RepeatDelayMilliseconds = 500;
    internal const int RepeatToggleMilliseconds = 50;
    internal const int MillisecondsPerSecond = 1000;
    internal const byte ControlMask = 0x1f;
    internal const byte Escape = 27;
    internal const byte Delete = 127;
    internal const byte Backspace = 8;
    internal const byte Tab = 9;
    internal const byte Return = 13;
    internal const byte LineFeed = 10;
    internal const int MatrixRows = 8;
    internal const int MatrixColumns = 3;
    internal const int DigitMask = 7;
    internal const int OutputMask = 0x47;
    internal const int MatrixMask = 0x38;
    internal const int MatrixFirstBit = 3;
    internal const int TapeOutputBit = 0x40;
    internal const int TapeInputBit = 0x80;
    internal const int TapeCarrierBit = 0x20;
}
