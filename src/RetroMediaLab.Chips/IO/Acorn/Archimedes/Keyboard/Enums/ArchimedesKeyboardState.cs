namespace RetroMediaLab.Chips.IO.Acorn.Archimedes.Keyboard.Enums;

public enum ArchimedesKeyboardState
{
    AwaitReset,
    AwaitResetAckOne,
    AwaitResetAckTwo,
    AwaitFirstByteAck,
    AwaitFinalAck,
    Idle
}
