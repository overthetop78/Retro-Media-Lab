using System.Buffers.Binary;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Video;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cartridge;

internal sealed partial class EepromI2c
{
    private void Detect_START()
    {
        unchecked
        {
            if (((eeprom_i2c.old_scl != 0) && (eeprom_i2c.scl != 0))) {
            if (((eeprom_i2c.old_sda != 0) && (!(eeprom_i2c.sda != 0)))) {
            eeprom_i2c.cycles = unchecked((byte)(0));
            if ((unchecked((int)(eeprom_i2c.spec.address_bits)) == 7)) {
            eeprom_i2c.word_address = unchecked((ushort)(0));
            eeprom_i2c.state = 3;
            } else {
            eeprom_i2c.device_address = unchecked((ushort)(0));
            eeprom_i2c.state = 2;
            }
            }
            }
        }
    }
    private void Detect_STOP()
    {
        unchecked
        {
            if (((eeprom_i2c.old_scl != 0) && (eeprom_i2c.scl != 0))) {
            if (((!(eeprom_i2c.old_sda != 0)) && (eeprom_i2c.sda != 0))) {
            eeprom_i2c.state = 0;
            }
            }
        }
    }
    private void eeprom_i2c_update()
    {
        unchecked
        {
            switch (eeprom_i2c.state) {
            case 0:
            {
            Detect_START();
            break;
            }
            case 1:
            {
            Detect_STOP();
            break;
            }
            case 3:
            {
            Detect_START();
            Detect_STOP();
            if (((eeprom_i2c.old_scl != 0) && (!(eeprom_i2c.scl != 0)))) {
            if ((unchecked((int)(eeprom_i2c.cycles)) < 9)) {
            eeprom_i2c.cycles++;
            } else {
            eeprom_i2c.cycles = unchecked((byte)(1));
            eeprom_i2c.state = ((eeprom_i2c.rw != 0) ? 7 : 6);
            eeprom_i2c.buffer = unchecked((byte)(0));
            }
            } else if (((!(eeprom_i2c.old_scl != 0)) && (eeprom_i2c.scl != 0))) {
            if ((unchecked((int)(eeprom_i2c.cycles)) < 8)) {
            eeprom_i2c.word_address |= unchecked((ushort)((unchecked((int)(eeprom_i2c.sda)) << (7 - unchecked((int)(eeprom_i2c.cycles))))));
            } else if ((unchecked((int)(eeprom_i2c.cycles)) == 8)) {
            eeprom_i2c.rw = eeprom_i2c.sda;
            }
            }
            break;
            }
            case 2:
            {
            Detect_START();
            Detect_STOP();
            if (((eeprom_i2c.old_scl != 0) && (!(eeprom_i2c.scl != 0)))) {
            if ((unchecked((int)(eeprom_i2c.cycles)) < 9)) {
            eeprom_i2c.cycles++;
            } else {
            eeprom_i2c.device_address <<= unchecked((int)(eeprom_i2c.spec.address_bits));
            eeprom_i2c.cycles = unchecked((byte)(1));
            if ((eeprom_i2c.rw != 0)) {
            eeprom_i2c.state = 7;
            } else {
            eeprom_i2c.word_address = unchecked((ushort)(0));
            eeprom_i2c.state = ((unchecked((int)(eeprom_i2c.spec.address_bits)) == 16) ? 4 : 5);
            }
            }
            } else if (((!(eeprom_i2c.old_scl != 0)) && (eeprom_i2c.scl != 0))) {
            if (((unchecked((int)(eeprom_i2c.cycles)) > 4) && (unchecked((int)(eeprom_i2c.cycles)) < 8))) {
            eeprom_i2c.device_address |= unchecked((ushort)((unchecked((int)(eeprom_i2c.sda)) << (7 - unchecked((int)(eeprom_i2c.cycles))))));
            } else if ((unchecked((int)(eeprom_i2c.cycles)) == 8)) {
            eeprom_i2c.rw = eeprom_i2c.sda;
            }
            }
            break;
            }
            case 4:
            {
            Detect_START();
            Detect_STOP();
            if (((eeprom_i2c.old_scl != 0) && (!(eeprom_i2c.scl != 0)))) {
            if ((unchecked((int)(eeprom_i2c.cycles)) < 9)) {
            eeprom_i2c.cycles++;
            } else {
            eeprom_i2c.cycles = unchecked((byte)(1));
            eeprom_i2c.state = 5;
            }
            } else if (((!(eeprom_i2c.old_scl != 0)) && (eeprom_i2c.scl != 0))) {
            if ((unchecked((int)(eeprom_i2c.cycles)) < 9)) {
            if ((unchecked((int)(eeprom_i2c.spec.size_mask)) < (1 << (16 - unchecked((int)(eeprom_i2c.cycles)))))) {
            eeprom_i2c.device_address >>= 1;
            } else {
            eeprom_i2c.word_address |= unchecked((ushort)((unchecked((int)(eeprom_i2c.sda)) << (16 - unchecked((int)(eeprom_i2c.cycles))))));
            }
            }
            }
            break;
            }
            case 5:
            {
            Detect_START();
            Detect_STOP();
            if (((eeprom_i2c.old_scl != 0) && (!(eeprom_i2c.scl != 0)))) {
            if ((unchecked((int)(eeprom_i2c.cycles)) < 9)) {
            eeprom_i2c.cycles++;
            } else {
            eeprom_i2c.cycles = unchecked((byte)(1));
            eeprom_i2c.state = 6;
            eeprom_i2c.buffer = unchecked((byte)(0));
            }
            } else if (((!(eeprom_i2c.old_scl != 0)) && (eeprom_i2c.scl != 0))) {
            if ((unchecked((int)(eeprom_i2c.cycles)) < 9)) {
            if ((unchecked((int)(eeprom_i2c.spec.size_mask)) < (1 << (8 - unchecked((int)(eeprom_i2c.cycles)))))) {
            eeprom_i2c.device_address >>= 1;
            } else {
            eeprom_i2c.word_address |= unchecked((ushort)((unchecked((int)(eeprom_i2c.sda)) << (8 - unchecked((int)(eeprom_i2c.cycles))))));
            }
            }
            }
            break;
            }
            case 7:
            {
            Detect_START();
            Detect_STOP();
            if (((eeprom_i2c.old_scl != 0) && (!(eeprom_i2c.scl != 0)))) {
            if ((unchecked((int)(eeprom_i2c.cycles)) < 9)) {
            eeprom_i2c.cycles++;
            } else {
            eeprom_i2c.cycles = unchecked((byte)(1));
            }
            } else if (((!(eeprom_i2c.old_scl != 0)) && (eeprom_i2c.scl != 0))) {
            if ((unchecked((int)(eeprom_i2c.cycles)) == 9)) {
            if ((eeprom_i2c.sda != 0)) {
            eeprom_i2c.state = 1;
            } else {
            eeprom_i2c.word_address = unchecked((ushort)(((unchecked((int)(eeprom_i2c.word_address)) + 1) & unchecked((int)(eeprom_i2c.spec.size_mask)))));
            }
            }
            }
            break;
            }
            case 6:
            {
            Detect_START();
            Detect_STOP();
            if (((eeprom_i2c.old_scl != 0) && (!(eeprom_i2c.scl != 0)))) {
            if ((unchecked((int)(eeprom_i2c.cycles)) < 9)) {
            eeprom_i2c.cycles++;
            } else {
            eeprom_i2c.cycles = unchecked((byte)(1));
            }
            } else if (((!(eeprom_i2c.old_scl != 0)) && (eeprom_i2c.scl != 0))) {
            if ((unchecked((int)(eeprom_i2c.cycles)) < 9)) {
            eeprom_i2c.buffer |= unchecked((byte)((unchecked((int)(eeprom_i2c.sda)) << (8 - unchecked((int)(eeprom_i2c.cycles))))));
            } else {
            sram.sram[(int)(((unchecked((int)(eeprom_i2c.device_address)) | unchecked((int)(eeprom_i2c.word_address))) & 65535))] = eeprom_i2c.buffer;
            eeprom_i2c.buffer = unchecked((byte)(0));
            eeprom_i2c.word_address = unchecked((ushort)(((unchecked((int)(eeprom_i2c.word_address)) & (~unchecked((int)(eeprom_i2c.spec.pagewrite_mask)))) | ((unchecked((int)(eeprom_i2c.word_address)) + 1) & unchecked((int)(eeprom_i2c.spec.pagewrite_mask))))));
            }
            }
            break;
            }
            }
            eeprom_i2c.old_scl = eeprom_i2c.scl;
            eeprom_i2c.old_sda = eeprom_i2c.sda;
        }
    }
    private byte eeprom_i2c_out()
    {
        unchecked
        {
            if ((eeprom_i2c.state == 7)) {
            if ((unchecked((int)(eeprom_i2c.cycles)) < 9)) {
            return unchecked((byte)(((unchecked((int)(sram.sram[(int)(((unchecked((int)(eeprom_i2c.device_address)) | unchecked((int)(eeprom_i2c.word_address))) & 65535))])) >> (8 - unchecked((int)(eeprom_i2c.cycles)))) & 1)));
            }
            } else if ((unchecked((int)(eeprom_i2c.cycles)) == 9)) {
            return unchecked((byte)(0));
            }
            return eeprom_i2c.sda;
        }
    }
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
