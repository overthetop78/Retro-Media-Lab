namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Mos6522.Enums;

internal enum ViaControlMode
{
    FallingInput = 0,
    IndependentFallingInput = 1,
    RisingInput = 2,
    IndependentRisingInput = 3,
    HandshakeOutput = 4,
    PulseOutput = 5,
    LowOutput = 6,
    HighOutput = 7
}
