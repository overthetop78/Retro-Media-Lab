namespace RetroMediaLab.Chips.Memory.Serial.I2c;

public sealed partial class EepromI2c
{
    private static readonly (byte AddressBits, ushort SizeMask, ushort PageMask)[] Specifications =
    [
        (7, 127, 3),
        (8, 255, 3),
        (8, 127, 7),
        (8, 255, 7),
        (8, 511, 15),
        (8, 1023, 15),
        (8, 2047, 15),
        (16, 4095, 31),
        (16, 8191, 31),
        (16, 8191, 63),
        (16, 16383, 63),
        (16, 32767, 63),
        (16, 65535, 127),
    ];
}
