using System.Buffers.Binary;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Video;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cd;

internal sealed partial class CdCartridge
{
    private uint cart_ram_read_byte(uint address)
    {
        unchecked
        {
            if (((address & unchecked((uint)(1))) != 0)) {
            return unchecked((uint)(area[(int)(((address >> 1) & mask))]));
            }
            return unchecked((uint)(255));
        }
    }
    private uint cart_ram_read_word(uint address)
    {
        unchecked
        {
            return unchecked((uint)((unchecked((int)(area[(int)(((address >> 1) & mask))])) | 65280)));
        }
    }
    private void cart_ram_write_byte(uint address, uint data)
    {
        unchecked
        {
            if (((address & unchecked((uint)(1))) != 0)) {
            area[(int)(((address >> 1) & mask))] = unchecked((byte)(data));
            }
        }
    }
    private void cart_ram_write_word(uint address, uint data)
    {
        unchecked
        {
            area[(int)(((address >> 1) & mask))] = unchecked((byte)((data & unchecked((uint)(255)))));
        }
    }
    private uint cart_id_read_byte(uint address)
    {
        unchecked
        {
            if (((address & unchecked((uint)(1))) != 0)) {
            return unchecked((uint)(id));
            }
            return unchecked((uint)(255));
        }
    }
    private uint cart_id_read_word(uint address)
    {
        unchecked
        {
            return unchecked((uint)((unchecked((int)(id)) | 65280)));
        }
    }
    private uint cart_prot_read_byte(uint address)
    {
        unchecked
        {
            if (((address & unchecked((uint)(1))) != 0)) {
            return unchecked((uint)(prot));
            }
            return unchecked((uint)(255));
        }
    }
    private uint cart_prot_read_word(uint address)
    {
        unchecked
        {
            return unchecked((uint)((unchecked((int)(prot)) | 65280)));
        }
    }
    private void cart_prot_write_byte(uint address, uint data)
    {
        unchecked
        {
            if (((address & unchecked((uint)(1))) != 0)) {
            int i = default;
            if (((data & unchecked((uint)(1))) != 0)) {
            for (i = 96; (i < 112); i++) {
            memory_map[(int)(i)].write8 = cart_ram_write_byte;
            memory_map[(int)(i)].write16 = cart_ram_write_word;
            zbank_memory_map[(int)(i)].write = cart_ram_write_byte;
            }
            } else {
            for (i = 96; (i < 112); i++) {
            memory_map[(int)(i)].write8 = m68k_unused_8_w;
            memory_map[(int)(i)].write16 = m68k_unused_16_w;
            zbank_memory_map[(int)(i)].write = zbank_unused_w;
            }
            }
            prot = unchecked((byte)(data));
            }
        }
    }
    private void cart_prot_write_word(uint address, uint data)
    {
        unchecked
        {
            int i = default;
            if (((data & unchecked((uint)(1))) != 0)) {
            for (i = 96; (i < 112); i++) {
            memory_map[(int)(i)].write8 = cart_ram_write_byte;
            memory_map[(int)(i)].write16 = cart_ram_write_word;
            zbank_memory_map[(int)(i)].write = cart_ram_write_byte;
            }
            } else {
            for (i = 96; (i < 112); i++) {
            memory_map[(int)(i)].write8 = m68k_unused_8_w;
            memory_map[(int)(i)].write16 = m68k_unused_16_w;
            zbank_memory_map[(int)(i)].write = zbank_unused_w;
            }
            }
            prot = unchecked((byte)((data & unchecked((uint)(255)))));
        }
    }
    private void cd_cart_init()
    {
        unchecked
        {
            int i = default;
            if ((boot != 0)) {
            id = unchecked((byte)(0));
            } else {
            id = ConfiguredRamId;
            }
            if ((id != 0)) {
            ClearBackupMemory();
            ClearArea();
            mask = unchecked((uint)(((1 << (unchecked((int)(id)) + 13)) - 1)));
            prot = unchecked((byte)(1));
            for (i = 64; (i < 96); i++) {
            memory_map[(int)(i)].@base = default(CPointer<byte>);
            memory_map[(int)(i)].read8 = cart_id_read_byte;
            memory_map[(int)(i)].read16 = cart_id_read_word;
            memory_map[(int)(i)].write8 = m68k_unused_8_w;
            memory_map[(int)(i)].write16 = m68k_unused_16_w;
            zbank_memory_map[(int)(i)].read = cart_id_read_byte;
            zbank_memory_map[(int)(i)].write = zbank_unused_w;
            }
            for (i = 96; (i < 112); i++) {
            memory_map[(int)(i)].@base = default(CPointer<byte>);
            memory_map[(int)(i)].read8 = cart_ram_read_byte;
            memory_map[(int)(i)].read16 = cart_ram_read_word;
            memory_map[(int)(i)].write8 = cart_ram_write_byte;
            memory_map[(int)(i)].write16 = cart_ram_write_word;
            zbank_memory_map[(int)(i)].read = cart_ram_read_byte;
            zbank_memory_map[(int)(i)].write = cart_ram_write_byte;
            }
            for (i = 112; (i < 128); i++) {
            memory_map[(int)(i)].@base = default(CPointer<byte>);
            memory_map[(int)(i)].read8 = cart_prot_read_byte;
            memory_map[(int)(i)].read16 = cart_prot_read_word;
            memory_map[(int)(i)].write8 = cart_prot_write_byte;
            memory_map[(int)(i)].write16 = cart_prot_write_word;
            zbank_memory_map[(int)(i)].read = cart_prot_read_byte;
            zbank_memory_map[(int)(i)].write = cart_prot_write_byte;
            }
            } else {
            md_cart_init();
            if ((!(boot != 0))) {
            for (i = 0; (i < 64); i++) {
            memory_map[(int)((i + 64))].@base = memory_map[(int)(i)].@base;
            memory_map[(int)((i + 64))].read8 = memory_map[(int)(i)].read8;
            memory_map[(int)((i + 64))].read16 = memory_map[(int)(i)].read16;
            memory_map[(int)((i + 64))].write8 = memory_map[(int)(i)].write8;
            memory_map[(int)((i + 64))].write16 = memory_map[(int)(i)].write16;
            zbank_memory_map[(int)((i + 64))].read = zbank_memory_map[(int)(i)].read;
            zbank_memory_map[(int)((i + 64))].write = zbank_memory_map[(int)(i)].write;
            }
            }
            }
        }
    }
}
