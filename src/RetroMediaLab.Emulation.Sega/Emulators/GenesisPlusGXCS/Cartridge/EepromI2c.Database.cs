using System.Buffers.Binary;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Video;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cartridge;

internal sealed partial class EepromI2c
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
    private DatabaseEntry[] CreateDatabase() =>
    [
        new("T-50176", 0u, 0, mapper_i2c_ea_init, 0),
        new("T-50396", 0u, 0, mapper_i2c_ea_init, 0),
        new("T-50446", 0u, 0, mapper_i2c_ea_init, 0),
        new("T-50516", 0u, 0, mapper_i2c_ea_init, 0),
        new("T-50606", 0u, 0, mapper_i2c_ea_init, 0),
        new(" T-12046", 0u, 0, mapper_i2c_sega_init, 0),
        new(" T-12053", 0u, 0, mapper_i2c_sega_init, 0),
        new("MK-1215", 0u, 0, mapper_i2c_sega_init, 0),
        new("MK-1228", 0u, 0, mapper_i2c_sega_init, 0),
        new("G-5538", 0u, 0, mapper_i2c_sega_init, 0),
        new("PR-1993", 0u, 0, mapper_i2c_sega_init, 0),
        new(" G-4060", 0u, 0, mapper_i2c_sega_init, 0),
        new("00001211", 0u, 0, mapper_i2c_sega_init, 0),
        new("00004076", 0u, 0, mapper_i2c_sega_init, 0),
        new("G-4524", 0u, 0, mapper_i2c_sega_init, 0),
        new("00054503", 0u, 0, mapper_i2c_sega_init, 0),
        new("T-81033", 0u, 0, mapper_i2c_acclaim_16M_init, 1),
        new("T-081326", 0u, 0, mapper_i2c_acclaim_16M_init, 1),
        new("T-081276", 0u, 0, mapper_i2c_acclaim_32M_init, 3),
        new("T-81406", 0u, 0, mapper_i2c_acclaim_32M_init, 4),
        new("T-081586", 0u, 0, mapper_i2c_acclaim_32M_init, 6),
        new("T-81476", 0u, 0, mapper_i2c_acclaim_32M_init, 9),
        new("T-81576", 0u, 0, mapper_i2c_acclaim_32M_init, 9),
        new("T-120106", 0u, 0, mapper_i2c_jcart_init, 5),
        new("00000000", 1145982020u, 5771, mapper_i2c_jcart_init, 5),
        new("00000000", 1145982020u, 5726, mapper_i2c_jcart_init, 6),
        new("T-120096", 0u, 0, mapper_i2c_jcart_init, 6),
        new("T-120146", 0u, 0, mapper_i2c_jcart_init, 9),
        new("00000000", 4294967292u, 5771, mapper_i2c_jcart_init, -1),
        new("00000000", 4294967292u, 5726, mapper_i2c_jcart_init, -1),
        new("T-120066", 0u, 0, mapper_i2c_jcart_init, -1),
        new("T-123456", 0u, 0, mapper_i2c_jcart_init, -1),
        new("XXXXXXXX", 0u, 57145, mapper_i2c_jcart_init, -1),
    ];
}
