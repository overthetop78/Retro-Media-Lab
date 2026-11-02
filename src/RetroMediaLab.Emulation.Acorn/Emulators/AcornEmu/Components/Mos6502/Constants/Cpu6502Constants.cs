namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Mos6502.Constants;

internal static class Cpu6502Constants
{
    internal const ushort StackBase = 0x0100;
    internal const ushort PageMask = 0xff00;
    internal const ushort ResetVector = 0xfffc;
    internal const ushort NmiVector = 0xfffa;
    internal const ushort IrqVector = 0xfffe;
    internal const int HighByteShift = 8;
    internal const byte UnstableBusMask = 0xee;
    internal const ushort JamBusAddress = 0xffff;
}
