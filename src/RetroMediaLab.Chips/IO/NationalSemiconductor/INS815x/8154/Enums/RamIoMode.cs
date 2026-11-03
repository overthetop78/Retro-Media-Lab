namespace RetroMediaLab.Chips.IO.NationalSemiconductor.INS815x.Chip8154.Enums;

public enum RamIoMode : byte
{
    Basic = 0x00,
    StrobedInput = 0x20,
    StrobedOutput = 0x60,
    StrobedOutputThreeState = 0xe0
}
