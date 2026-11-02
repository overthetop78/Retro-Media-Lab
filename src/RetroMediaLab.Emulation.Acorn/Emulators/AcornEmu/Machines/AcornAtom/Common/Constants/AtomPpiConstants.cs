namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornAtom.Common.Constants;

internal static class AtomPpiConstants
{
    internal const int PortA = 0;
    internal const int PortB = 1;
    internal const int PortC = 2;
    internal const int Control = 3;
    internal const int RegisterMask = 3;
    internal const byte ModeSet = 0x80;
    internal const byte UnsupportedHandshakeModes = 0x64;
    internal const byte InputA = 0x10;
    internal const byte InputB = 0x02;
    internal const byte InputUpperC = 0x08;
    internal const byte InputLowerC = 0x01;
    internal const byte UpperC = 0xf0;
    internal const byte LowerC = 0x0f;
    internal const byte ResetControl = 0x9b;
    internal const int BitSelectionShift = 1;
    internal const int BitSelectionMask = 7;
    internal const int BitSet = 1;
}
