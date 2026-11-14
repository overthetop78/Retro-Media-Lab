using RetroMediaLab.Chips.Common.Memory;
namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Cartridge;
internal sealed partial class EepromI2c
{
    private uint mapper_i2c_generic_read8(uint address)
    {
        unchecked
        {
            if (((address & unchecked((uint)(1))) != 0)) {
            return unchecked((uint)((unchecked((int)(eeprom_i2c_out())) << unchecked((int)(eeprom_i2c.sda_out_bit)))));
            }
            return m68k_read_bus_8(address);
        }
    }
    private uint mapper_i2c_generic_read16(uint address)
    {
        unchecked
        {
            return unchecked((uint)((unchecked((int)(eeprom_i2c_out())) << unchecked((int)(eeprom_i2c.sda_out_bit)))));
        }
    }
    private void mapper_i2c_generic_write8(uint address, uint data)
    {
        unchecked
        {
            if (((address & unchecked((uint)(1))) != 0)) {
            eeprom_i2c.sda = unchecked((byte)(((data >> unchecked((int)(eeprom_i2c.sda_in_bit))) & unchecked((uint)(1)))));
            eeprom_i2c.scl = unchecked((byte)(((data >> unchecked((int)(eeprom_i2c.scl_in_bit))) & unchecked((uint)(1)))));
            eeprom_i2c_update();
            } else {
            m68k_unused_8_w(address, data);
            }
        }
    }
    private void mapper_i2c_generic_write16(uint address, uint data)
    {
        unchecked
        {
            eeprom_i2c.sda = unchecked((byte)(((data >> unchecked((int)(eeprom_i2c.sda_in_bit))) & unchecked((uint)(1)))));
            eeprom_i2c.scl = unchecked((byte)(((data >> unchecked((int)(eeprom_i2c.scl_in_bit))) & unchecked((uint)(1)))));
            eeprom_i2c_update();
        }
    }
    private void mapper_i2c_ea_init()
    {
        unchecked
        {
            int i = default;
            for (i = 32; (i < 64); i++) {
            memory_map[(int)(i)].read8 = mapper_i2c_generic_read8;
            memory_map[(int)(i)].read16 = mapper_i2c_generic_read16;
            memory_map[(int)(i)].write8 = mapper_i2c_generic_write8;
            memory_map[(int)(i)].write16 = mapper_i2c_generic_write16;
            zbank_memory_map[(int)(i)].read = mapper_i2c_generic_read8;
            zbank_memory_map[(int)(i)].write = mapper_i2c_generic_write8;
            }
            eeprom_i2c.scl_in_bit = unchecked((byte)(6));
            eeprom_i2c.sda_in_bit = unchecked((byte)(7));
            eeprom_i2c.sda_out_bit = unchecked((byte)(7));
        }
    }
    private void mapper_i2c_sega_init()
    {
        unchecked
        {
            int i = default;
            for (i = 32; (i < 64); i++) {
            memory_map[(int)(i)].read8 = mapper_i2c_generic_read8;
            memory_map[(int)(i)].read16 = mapper_i2c_generic_read16;
            memory_map[(int)(i)].write8 = mapper_i2c_generic_write8;
            memory_map[(int)(i)].write16 = mapper_i2c_generic_write16;
            zbank_memory_map[(int)(i)].read = mapper_i2c_generic_read8;
            zbank_memory_map[(int)(i)].write = mapper_i2c_generic_write8;
            }
            eeprom_i2c.scl_in_bit = unchecked((byte)(1));
            eeprom_i2c.sda_in_bit = unchecked((byte)(0));
            eeprom_i2c.sda_out_bit = unchecked((byte)(0));
        }
    }
    private void mapper_i2c_acclaim_16M_init()
    {
        unchecked
        {
            int i = default;
            for (i = 32; (i < 64); i++) {
            memory_map[(int)(i)].read8 = mapper_i2c_generic_read8;
            memory_map[(int)(i)].read16 = mapper_i2c_generic_read16;
            memory_map[(int)(i)].write8 = mapper_i2c_generic_write16;
            memory_map[(int)(i)].write16 = mapper_i2c_generic_write16;
            zbank_memory_map[(int)(i)].read = mapper_i2c_generic_read8;
            zbank_memory_map[(int)(i)].write = mapper_i2c_generic_write16;
            }
            eeprom_i2c.scl_in_bit = unchecked((byte)(1));
            eeprom_i2c.sda_in_bit = unchecked((byte)(0));
            eeprom_i2c.sda_out_bit = unchecked((byte)(1));
        }
    }
    private void mapper_acclaim_32M_write8(uint address, uint data)
    {
        unchecked
        {
            if (((address & unchecked((uint)(1))) != 0)) {
            eeprom_i2c.sda = unchecked((byte)((data & unchecked((uint)(1)))));
            } else {
            eeprom_i2c.scl = unchecked((byte)((data & unchecked((uint)(1)))));
            }
            eeprom_i2c_update();
        }
    }
    private void mapper_acclaim_32M_write16(uint address, uint data)
    {
        unchecked
        {
            int i = default;
            if (((data & unchecked((uint)(1))) != 0)) {
            for (i = 32; (i < 48); i++) {
            memory_map[(int)(i)].read8 = null;
            memory_map[(int)(i)].read16 = null;
            zbank_memory_map[(int)(i)].read = null;
            }
            } else {
            for (i = 32; (i < 48); i++) {
            memory_map[(int)(i)].read8 = mapper_i2c_generic_read8;
            memory_map[(int)(i)].read16 = mapper_i2c_generic_read16;
            zbank_memory_map[(int)(i)].read = mapper_i2c_generic_read8;
            }
            }
        }
    }
    private void mapper_i2c_acclaim_32M_init()
    {
        unchecked
        {
            int i = default;
            for (i = 32; (i < 48); i++) {
            memory_map[(int)(i)].write8 = mapper_acclaim_32M_write8;
            memory_map[(int)(i)].write16 = mapper_acclaim_32M_write16;
            zbank_memory_map[(int)(i)].write = mapper_acclaim_32M_write8;
            }
            eeprom_i2c.scl_in_bit = unchecked((byte)(0));
            eeprom_i2c.sda_in_bit = unchecked((byte)(0));
            eeprom_i2c.sda_out_bit = unchecked((byte)(0));
            bankshift = unchecked((ushort)(1));
        }
    }
    private uint mapper_i2c_jcart_read8(uint address)
    {
        unchecked
        {
            if (((address & unchecked((uint)(1))) != 0)) {
            return (unchecked((uint)((unchecked((int)(eeprom_i2c_out())) << 7))) | (jcart_read(address) & unchecked((uint)(127))));
            }
            return (jcart_read(address) >> 8);
        }
    }
    private uint mapper_i2c_jcart_read16(uint address)
    {
        unchecked
        {
            return (unchecked((uint)((unchecked((int)(eeprom_i2c_out())) << 7))) | jcart_read(address));
        }
    }
    private void mapper_i2c_jcart_init()
    {
        unchecked
        {
            int i = default;
            if ((unchecked((int)(sram.type)) == 1)) {
            for (i = 48; (i < 56); i++) {
            memory_map[(int)(i)].write8 = mapper_i2c_generic_write16;
            memory_map[(int)(i)].write16 = mapper_i2c_generic_write16;
            zbank_memory_map[(int)(i)].write = mapper_i2c_generic_write16;
            }
            }
            if (IsCricketProduct()) {
            for (i = 56; (i < 64); i++) {
            memory_map[(int)(i)].read8 = mapper_i2c_generic_read8;
            memory_map[(int)(i)].read16 = mapper_i2c_generic_read16;
            memory_map[(int)(i)].write8 = m68k_unused_8_w;
            memory_map[(int)(i)].write16 = m68k_unused_16_w;
            zbank_memory_map[(int)(i)].read = mapper_i2c_generic_read8;
            zbank_memory_map[(int)(i)].write = m68k_unused_8_w;
            }
            } else {
            cart_special |= unchecked((byte)(4));
            for (i = 56; (i < 64); i++) {
            memory_map[(int)(i)].read8 = mapper_i2c_jcart_read8;
            memory_map[(int)(i)].read16 = mapper_i2c_jcart_read16;
            memory_map[(int)(i)].write8 = jcart_write;
            memory_map[(int)(i)].write16 = jcart_write;
            zbank_memory_map[(int)(i)].read = mapper_i2c_jcart_read8;
            zbank_memory_map[(int)(i)].write = jcart_write;
            }
            }
            eeprom_i2c.scl_in_bit = unchecked((byte)(1));
            eeprom_i2c.sda_in_bit = unchecked((byte)(0));
            eeprom_i2c.sda_out_bit = unchecked((byte)(7));
        }
    }
}
