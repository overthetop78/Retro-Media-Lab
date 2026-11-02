namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.Archimedes.Common.Enums;

internal enum ArchimedesKeyboardState
{
    AwaitReset,
    AwaitResetAckOne,
    AwaitResetAckTwo,
    AwaitFirstByteAck,
    AwaitFinalAck,
    Idle
}
