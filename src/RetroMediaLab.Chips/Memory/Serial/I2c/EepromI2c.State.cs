using System.Buffers.Binary;
using RetroMediaLab.Chips.Common.Memory;

namespace RetroMediaLab.Chips.Memory.Serial.I2c;

public sealed partial class EepromI2c
{
    private sealed class T_I2C_SPEC
    {
        public byte address_bits;
        public ushort size_mask;
        public ushort pagewrite_mask;
    }
    private sealed class I2cState
    {
        public byte sda;
        public byte scl;
        public byte old_sda;
        public byte old_scl;
        public byte cycles;
        public byte rw;
        public ushort device_address;
        public ushort word_address;
        public byte buffer;
        public int state;
        public readonly T_I2C_SPEC spec = new();
        public byte scl_in_bit;
        public byte sda_in_bit;
        public byte sda_out_bit;
    }
    private readonly I2cState eeprom_i2c = new();
}
