using System.Buffers.Binary;
using RetroMediaLab.Chips.Common.Memory;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Cartridge;

internal sealed partial class MdCartridge
{
    private void mapper_sega_w(uint data)
    {
        unchecked
        {
            int i = default;
            if (((data & unchecked((uint)(1))) != 0)) {
            if ((sram.on != 0)) {
            if ((!memory_map[(int)(32)].@base.Same(sram.sram))) {
            cart.hw.regs[(int)(0)] = unchecked((byte)((((memory_map[(int)(32)].@base - new ArrayPointer<byte>(cart.rom)) >> 16) & 255)));
            memory_map[(int)(32)].@base = new ArrayPointer<byte>(sram.sram);
            memory_map[(int)(32)].read8 = sram_read_byte;
            memory_map[(int)(32)].read16 = sram_read_word;
            zbank_memory_map[(int)(32)].read = sram_read_byte;
            }
            if (((data & unchecked((uint)(2))) != 0)) {
            memory_map[(int)(32)].write8 = m68k_unused_8_w;
            memory_map[(int)(32)].write16 = m68k_unused_16_w;
            zbank_memory_map[(int)(32)].write = zbank_unused_w;
            } else {
            memory_map[(int)(32)].write8 = sram_write_byte;
            memory_map[(int)(32)].write16 = sram_write_word;
            zbank_memory_map[(int)(32)].write = sram_write_byte;
            }
            }
            if (((unchecked((int)(cart.special)) & 8) != 0)) {
            for (i = 48; (i < 64); i++) {
            memory_map[(int)(i)].@base = ((new ArrayPointer<byte>(cart.rom) + 6291456) + ((i & 3) << 16));
            }
            }
            } else {
            if (memory_map[(int)(32)].@base.Same(sram.sram)) {
            memory_map[(int)(32)].@base = (new ArrayPointer<byte>(cart.rom) + (unchecked((int)(cart.hw.regs[(int)(0)])) << 16));
            memory_map[(int)(32)].read8 = null;
            memory_map[(int)(32)].read16 = null;
            zbank_memory_map[(int)(32)].read = null;
            memory_map[(int)(32)].write8 = m68k_unused_8_w;
            memory_map[(int)(32)].write16 = m68k_unused_16_w;
            zbank_memory_map[(int)(32)].write = zbank_unused_w;
            }
            if (((unchecked((int)(cart.special)) & 8) != 0)) {
            for (i = 48; (i < 64); i++) {
            memory_map[(int)(i)].@base = (new ArrayPointer<byte>(cart.rom) + unchecked((int)((unchecked((uint)((i << 16))) & cart.mask))));
            }
            }
            }
        }
    }
    private void mapper_512k_w(uint address, uint data)
    {
        unchecked
        {
            uint i = default;
            ArrayPointer<byte> src = (new ArrayPointer<byte>(cart.rom) + unchecked((int)(((data << 19) & cart.mask))));
            address = ((address << 2) & unchecked((uint)(56)));
            for (i = unchecked((uint)(0)); (i < unchecked((uint)(8))); i++) {
            memory_map[(int)((address++))].@base = (src + unchecked((int)((i << 16))));
            }
        }
    }
    private void mapper_ssf2_w(uint address, uint data)
    {
        unchecked
        {
            if (((address & unchecked((uint)(14))) != 0)) {
            mapper_512k_w(address, data);
            }
        }
    }
    private void mapper_sf001_w(uint address, uint data)
    {
        unchecked
        {
            switch (((address >> 8) & unchecked((uint)(15)))) {
            case 14:
            {
            int i = default;
            if (((data & unchecked((uint)(64))) != 0)) {
            for (i = 0; (i < 64); i++) {
            memory_map[(int)(i)].@base = (new ArrayPointer<byte>(cart.rom) + (i << 16));
            memory_map[(int)(i)].read8 = m68k_read_bus_8;
            memory_map[(int)(i)].read16 = m68k_read_bus_16;
            memory_map[(int)(i)].write8 = ((i > 0) ? m68k_unused_8_w : mapper_sf001_w);
            memory_map[(int)(i)].write16 = ((i > 0) ? m68k_unused_16_w : mapper_sf001_w);
            zbank_memory_map[(int)(i)].read = zbank_unused_r;
            zbank_memory_map[(int)(i)].write = ((i > 0) ? m68k_unused_8_w : mapper_sf001_w);
            }
            } else if (((data & unchecked((uint)(128))) != 0)) {
            for (i = 0; (i < 4); i++) {
            memory_map[(int)(i)].@base = (new ArrayPointer<byte>(cart.rom) + ((56 + i) << 16));
            memory_map[(int)(i)].read8 = null;
            memory_map[(int)(i)].read16 = null;
            zbank_memory_map[(int)(i)].read = null;
            }
            for (i = 4; (unchecked((uint)(i)) < (sram.start >> 16)); i++) {
            memory_map[(int)(i)].@base = (new ArrayPointer<byte>(cart.rom) + (i << 16));
            memory_map[(int)(i)].read8 = null;
            memory_map[(int)(i)].read16 = null;
            zbank_memory_map[(int)(i)].read = null;
            }
            while ((i < 64)) {
            memory_map[(int)(i)].@base = new ArrayPointer<byte>(sram.sram);
            memory_map[(int)(i)].read8 = sram_read_byte;
            memory_map[(int)(i)].read16 = sram_read_word;
            memory_map[(int)(i)].write8 = sram_write_byte;
            memory_map[(int)(i)].write16 = sram_write_word;
            zbank_memory_map[(int)(i)].read = sram_read_byte;
            zbank_memory_map[(int)(i)].write = sram_write_byte;
            i++;
            }
            } else {
            for (i = 0; (i < 64); i++) {
            memory_map[(int)(i)].@base = (new ArrayPointer<byte>(cart.rom) + (i << 16));
            memory_map[(int)(i)].read8 = null;
            memory_map[(int)(i)].read16 = null;
            memory_map[(int)(i)].write8 = ((i > 0) ? m68k_unused_8_w : mapper_sf001_w);
            memory_map[(int)(i)].write16 = ((i > 0) ? m68k_unused_16_w : mapper_sf001_w);
            zbank_memory_map[(int)(i)].read = null;
            zbank_memory_map[(int)(i)].write = ((i > 0) ? m68k_unused_8_w : mapper_sf001_w);
            }
            }
            if (((data & unchecked((uint)(32))) != 0)) {
            memory_map[(int)(0)].write8 = m68k_unused_8_w;
            memory_map[(int)(0)].write16 = m68k_unused_16_w;
            zbank_memory_map[(int)(0)].write = m68k_unused_8_w;
            }
            return;
            }
            default:
            {
            m68k_unused_8_w(address, data);
            return;
            }
            }
        }
    }
    private void mapper_sf002_w(uint address, uint data)
    {
        unchecked
        {
            int i = default;
            if (((data & unchecked((uint)(128))) != 0)) {
            for (i = 32; (i < 60); i++) {
            memory_map[(int)(i)].@base = (new ArrayPointer<byte>(cart.rom) + ((i & 31) << 16));
            }
            } else {
            for (i = 32; (i < 60); i++) {
            memory_map[(int)(i)].@base = (new ArrayPointer<byte>(cart.rom) + (i << 16));
            }
            }
        }
    }
    private void mapper_sf004_w(uint address, uint data)
    {
        unchecked
        {
            int i = default;
            switch (((address >> 8) & unchecked((uint)(15)))) {
            case 13:
            {
            if (((data & unchecked((uint)(128))) != 0)) {
            for (i = 32; (i < 48); i++) {
            memory_map[(int)(i)].read8 = sram_read_byte;
            memory_map[(int)(i)].read16 = sram_read_word;
            memory_map[(int)(i)].write8 = sram_write_byte;
            memory_map[(int)(i)].write16 = sram_write_word;
            zbank_memory_map[(int)(i)].read = sram_read_byte;
            zbank_memory_map[(int)(i)].write = sram_write_byte;
            }
            } else {
            for (i = 32; (i < 48); i++) {
            memory_map[(int)(i)].read8 = m68k_read_bus_8;
            memory_map[(int)(i)].read16 = m68k_read_bus_16;
            memory_map[(int)(i)].write8 = m68k_unused_8_w;
            memory_map[(int)(i)].write16 = m68k_unused_16_w;
            zbank_memory_map[(int)(i)].read = m68k_read_bus_8;
            zbank_memory_map[(int)(i)].write = m68k_unused_8_w;
            }
            }
            return;
            }
            case 14:
            {
            if (((data & unchecked((uint)(32))) != 0)) {
            for (i = 0; (i < 32); i++) {
            memory_map[(int)(i)].read8 = m68k_read_bus_8;
            memory_map[(int)(i)].read16 = m68k_read_bus_16;
            zbank_memory_map[(int)(i)].read = m68k_read_bus_8;
            }
            } else if (((data & unchecked((uint)(64))) != 0)) {
            byte @base = unchecked((byte)(((memory_map[(int)(0)].@base - new ArrayPointer<byte>(cart.rom)) >> 16)));
            for (i = 0; (i < 20); i++) {
            memory_map[(int)(i)].@base = (new ArrayPointer<byte>(cart.rom) + (((unchecked((int)(@base)) + i) & 31) << 16));
            memory_map[(int)(i)].read8 = null;
            memory_map[(int)(i)].read16 = null;
            zbank_memory_map[(int)(i)].read = null;
            }
            for (i = 20; (i < 32); i++) {
            memory_map[(int)(i)].read8 = m68k_read_bus_8;
            memory_map[(int)(i)].read16 = m68k_read_bus_16;
            zbank_memory_map[(int)(i)].read = m68k_read_bus_8;
            }
            } else {
            for (i = 0; (i < 32); i++) {
            memory_map[(int)(i)].@base = (memory_map[(int)(0)].@base + ((i & 3) << 16));
            memory_map[(int)(i)].read8 = null;
            memory_map[(int)(i)].read16 = null;
            zbank_memory_map[(int)(i)].read = null;
            }
            }
            if ((!((data & unchecked((uint)(128))) != 0))) {
            memory_map[(int)(0)].write8 = m68k_unused_8_w;
            memory_map[(int)(0)].write16 = m68k_unused_16_w;
            zbank_memory_map[(int)(0)].write = m68k_unused_8_w;
            }
            return;
            }
            case 15:
            {
            byte @base = unchecked((byte)((((data >> 4) & unchecked((uint)(7))) << 2)));
            if (memory_map[(int)(0)].@base.Same(memory_map[(int)(4)].@base)) {
            for (i = 0; (i < 32); i++) {
            memory_map[(int)(i)].@base = (new ArrayPointer<byte>(cart.rom) + ((unchecked((int)(@base)) + (i & 3)) << 16));
            }
            } else {
            for (i = 0; (i < 20); i++) {
            memory_map[(int)(i)].@base = (new ArrayPointer<byte>(cart.rom) + (((unchecked((int)(@base)) + i) & 31) << 16));
            }
            }
            return;
            }
            default:
            {
            m68k_unused_8_w(address, data);
            return;
            }
            }
        }
    }
    private uint mapper_sf004_r(uint address)
    {
        unchecked
        {
            return unchecked((uint)((((memory_map[(int)(0)].@base - new ArrayPointer<byte>(cart.rom)) >> 18) << 4)));
        }
    }
    private void mapper_t5740_w(uint address, uint data)
    {
        unchecked
        {
            int i = default;
            ArrayPointer<byte> @base = default;
            switch ((address & unchecked((uint)(255)))) {
            case 1:
            {
            return;
            }
            case 3:
            {
            @base = (new ArrayPointer<byte>(cart.rom) + unchecked((int)(((data & unchecked((uint)(15))) << 19))));
            for (i = 40; (i < 48); i++) {
            memory_map[(int)(i)].@base = (@base + ((i & 7) << 16));
            }
            return;
            }
            case 5:
            {
            @base = (new ArrayPointer<byte>(cart.rom) + unchecked((int)(((data & unchecked((uint)(15))) << 19))));
            for (i = 48; (i < 56); i++) {
            memory_map[(int)(i)].@base = (@base + ((i & 7) << 16));
            }
            return;
            }
            case 7:
            {
            @base = (new ArrayPointer<byte>(cart.rom) + unchecked((int)(((data & unchecked((uint)(15))) << 19))));
            for (i = 56; (i < 64); i++) {
            memory_map[(int)(i)].@base = (@base + ((i & 7) << 16));
            }
            return;
            }
            case 9:
            {
            eeprom_spi_write(unchecked((byte)(data)));
            return;
            }
            default:
            {
            m68k_unused_8_w(address, data);
            return;
            }
            }
        }
    }
    private uint mapper_t5740_r(uint address)
    {
        unchecked
        {
            if (((address & unchecked((uint)(65280))) == unchecked((uint)(33024)))) {
            return unchecked((uint)(cart.rom[(int)(((address & unchecked((uint)(32767))) ^ unchecked((uint)(1))))]));
            }
            return unchecked((uint)(cart.rom[(int)((address ^ unchecked((uint)(1))))]));
        }
    }
    private void mapper_flash_w8(uint address, uint data)
    {
        unchecked
        {
            if (((address & unchecked((uint)(1))) != 0)) {
            flash_cfi_write((address >> 1), ((data << 8) | (data & unchecked((uint)(255)))));
            } else {
            m68k_unused_8_w(address, data);
            }
        }
    }
    private void mapper_flash_w16(uint address, uint data)
    {
        unchecked
        {
            flash_cfi_write((address >> 1), data);
        }
    }
    private uint mapper_flash_r8(uint address)
    {
        unchecked
        {
            uint data = flash_cfi_read((address >> 1));
            return (((address & unchecked((uint)(1))) != 0) ? (data & unchecked((uint)(255))) : (data >> 8));
            ;
        }
    }
    private uint mapper_flash_r16(uint address)
    {
        unchecked
        {
            return flash_cfi_read((address >> 1));
            ;
        }
    }
    private void mapper_kaiserwave_w(uint address, uint data)
    {
        unchecked
        {
            if ((!((address & unchecked((uint)(1))) != 0))) {
            yx5200_write((data & unchecked((uint)(1))));
            } else {
            m68k_unused_8_w(address, data);
            }
        }
    }
    private void mapper_smw_64_w(uint address, uint data)
    {
        unchecked
        {
            switch (((address >> 16) & unchecked((uint)(7)))) {
            case 0:
            {
            if (((address & unchecked((uint)(2))) != 0)) {
            switch ((unchecked((int)(sram.sram[(int)(0)])) & 7)) {
            case 0:
            {
            sram.sram[(int)(6)] = unchecked((byte)(((unchecked((uint)((unchecked((int)(sram.sram[(int)(6)])) ^ unchecked((int)(sram.sram[(int)(1)]))))) ^ data) & unchecked((uint)(254)))));
            break;
            }
            case 1:
            {
            sram.sram[(int)(7)] = unchecked((byte)((data & unchecked((uint)(254)))));
            break;
            }
            case 7:
            {
            memory_map[(int)(97)].@base = (memory_map[(int)(105)].@base = ((new ArrayPointer<byte>(cart.rom) + 524288) + unchecked((int)(((data & unchecked((uint)(28))) << 14)))));
            break;
            }
            default:
            {
            break;
            }
            }
            sram.sram[(int)(1)] = unchecked((byte)(data));
            } else {
            sram.sram[(int)(0)] = unchecked((byte)(data));
            }
            return;
            }
            case 1:
            {
            if (((address & unchecked((uint)(2))) != 0)) {
            sram.sram[(int)(2)] = unchecked((byte)(data));
            }
            return;
            }
            case 4:
            {
            if (((address & unchecked((uint)(2))) != 0)) {
            sram.sram[(int)(4)] = unchecked((byte)(data));
            } else {
            sram.sram[(int)(3)] = unchecked((byte)(data));
            }
            return;
            }
            case 6:
            {
            return;
            }
            case 7:
            {
            if ((!((address & unchecked((uint)(2))) != 0))) {
            sram.sram[(int)(5)] = unchecked((byte)(data));
            if (((unchecked((int)(sram.sram[(int)(2)])) & 128) != 0)) {
            memory_map[(int)(96)].@base = (memory_map[(int)(104)].@base = ((new ArrayPointer<byte>(cart.rom) + 524288) + unchecked((int)(((data & unchecked((uint)(28))) << 14)))));
            }
            }
            return;
            }
            default:
            {
            m68k_unused_8_w(address, data);
            return;
            }
            }
        }
    }
    private uint mapper_smw_64_r(uint address)
    {
        unchecked
        {
            switch (((address >> 16) & unchecked((uint)(3)))) {
            case 2:
            {
            switch (((address >> 1) & unchecked((uint)(7)))) {
            case 0:
            return unchecked((uint)(sram.sram[(int)(6)]));
            case 1:
            return unchecked((uint)((unchecked((int)(sram.sram[(int)(6)])) + 1)));
            case 2:
            return unchecked((uint)(sram.sram[(int)(7)]));
            case 3:
            return unchecked((uint)((unchecked((int)(sram.sram[(int)(7)])) + 1)));
            case 4:
            return unchecked((uint)(sram.sram[(int)(8)]));
            case 5:
            return unchecked((uint)((unchecked((int)(sram.sram[(int)(8)])) + 1)));
            case 6:
            return unchecked((uint)((unchecked((int)(sram.sram[(int)(8)])) + 2)));
            case 7:
            return unchecked((uint)((unchecked((int)(sram.sram[(int)(8)])) + 3)));
            }
            goto case 3;
            }
            case 3:
            {
            byte data = unchecked((byte)((((unchecked((int)(sram.sram[(int)(2)])) & 128) != 0) ? (((unchecked((int)(sram.sram[(int)(5)])) & 64) != 0) ? (unchecked((int)(sram.sram[(int)(3)])) & unchecked((int)(sram.sram[(int)(4)]))) : (unchecked((int)(sram.sram[(int)(3)])) ^ 255)) : 0)));
            if (((address & unchecked((uint)(2))) != 0)) {
            data &= unchecked((byte)(127));
            } else {
            if (((unchecked((int)(sram.sram[(int)(5)])) & 128) != 0)) {
            if (((unchecked((int)(sram.sram[(int)(5)])) & 32) != 0)) {
            sram.sram[(int)(8)] = unchecked((byte)(((unchecked((int)(sram.sram[(int)(4)])) << 2) & 252)));
            } else {
            sram.sram[(int)(6)] = unchecked((byte)(((unchecked((int)(sram.sram[(int)(1)])) ^ (unchecked((int)(sram.sram[(int)(3)])) << 1)) & 254)));
            }
            }
            }
            return unchecked((uint)(data));
            }
            default:
            {
            return unchecked((uint)(0));
            }
            }
        }
    }
    private void mapper_realtec_w(uint address, uint data)
    {
        unchecked
        {
            switch (address) {
            case 4202496:
            {
            cart.hw.regs[(int)(1)] = unchecked((byte)(((data & unchecked((uint)(3))) << 1)));
            return;
            }
            case 4210688:
            {
            cart.hw.regs[(int)(2)] = unchecked((byte)(((data & unchecked((uint)(3))) << 1)));
            return;
            }
            case 4194304:
            {
            if (((data & unchecked((uint)(1))) != 0)) {
            if ((!(cart.hw.regs[(int)(0)] != 0))) {
            int i = default;
            for (i = 0; (i < 64); i++) {
            uint @base = unchecked((uint)((i & 7)));
            @base = ((@base & unchecked((uint)((~unchecked((int)(cart.hw.regs[(int)(1)])))))) | unchecked((uint)((unchecked((int)(cart.hw.regs[(int)(2)])) & unchecked((int)(cart.hw.regs[(int)(1)]))))));
            memory_map[(int)(i)].@base = new ArrayPointer<byte>(cart.rom, (int)((@base << 16)));
            }
            cart.hw.regs[(int)(0)] = unchecked((byte)(data));
            }
            }
            return;
            }
            }
        }
    }
    private uint mapper_realtec_r(uint address)
    {
        unchecked
        {
            if (((address == unchecked((uint)(256))) && (!(memory_map[(int)(0)].@base = cart.@base).IsNull))) {
            m68k_pulse_reset();
            }
            return unchecked((uint)((memory_map[(int)(0)].@base + unchecked((int)((address & unchecked((uint)(65534)))))).Cast<ushort>()[0]));
        }
    }
    private void mapper_seganet_w(uint address, uint data)
    {
        unchecked
        {
            if (((address & unchecked((uint)(255))) == unchecked((uint)(241)))) {
            int i = default;
            if (((data & unchecked((uint)(1))) != 0)) {
            for (i = 0; (i < 64); i++) {
            memory_map[(int)(i)].write8 = m68k_unused_8_w;
            memory_map[(int)(i)].write16 = m68k_unused_16_w;
            zbank_memory_map[(int)(i)].write = zbank_unused_w;
            }
            } else {
            for (i = 0; (i < 64); i++) {
            memory_map[(int)(i)].write8 = null;
            memory_map[(int)(i)].write16 = null;
            zbank_memory_map[(int)(i)].write = null;
            }
            }
            }
        }
    }
    private void mapper_32k_w(uint data)
    {
        unchecked
        {
            int i = default;
            if ((data != 0)) {
            for (i = 0; (i < 16); i++) {
            memory_map[(int)(i)].@base = new ArrayPointer<byte>(cart.rom, (int)((4194304 + (i << 16))));
            CopyMemory(memory_map[(int)(i)].@base, (new ArrayPointer<byte>(cart.rom) + unchecked((int)((unchecked((uint)((i << 16))) | ((data & unchecked((uint)(63))) << 15))))), 32768);
            CopyMemory((memory_map[(int)(i)].@base + 32768), (new ArrayPointer<byte>(cart.rom) + unchecked((int)((unchecked((uint)((i << 16))) | (((data | unchecked((uint)(1))) & unchecked((uint)(63))) << 15))))), 32768);
            }
            } else {
            for (i = 0; (i < 16); i++) {
            memory_map[(int)(i)].@base = new ArrayPointer<byte>(cart.rom, (int)((i << 16)));
            }
            }
        }
    }
    private void mapper_64k_w(uint data)
    {
        unchecked
        {
            int i = default;
            if ((data != 0)) {
            for (i = 0; (i < 16); i++) {
            memory_map[(int)(i)].@base = new ArrayPointer<byte>(cart.rom, (int)(((data & unchecked((uint)(15))) << 16)));
            }
            } else {
            for (i = 0; (i < 16); i++) {
            memory_map[(int)(i)].@base = new ArrayPointer<byte>(cart.rom, (int)(((i & 15) << 16)));
            }
            }
        }
    }
    private void mapper_64k_multi_w(uint address)
    {
        unchecked
        {
            int i = default;
            for (i = 0; (i < 64); i++) {
            memory_map[(int)(i)].@base = new ArrayPointer<byte>(cart.rom, (int)((((address + unchecked((uint)(i))) & unchecked((uint)(63))) << 16)));
            }
        }
    }
    private uint mapper_128k_multi_r(uint address)
    {
        unchecked
        {
            int i = default;
            int bank = unchecked((int)((((((address & unchecked((uint)(2))) << 2) | (address & unchecked((uint)(4)))) | ((address & unchecked((uint)(16))) >> 3)) | ((address & unchecked((uint)(32))) >> 5))));
            address = unchecked((uint)((bank << 1)));
            for (i = 0; (i < 64); i++) {
            memory_map[(int)(i)].@base = new ArrayPointer<byte>(cart.rom, (int)((((address + unchecked((uint)(i))) & unchecked((uint)(63))) << 16)));
            }
            return unchecked((uint)(3));
        }
    }
    private void mapper_256k_multi_w(uint address, uint data)
    {
        unchecked
        {
            int i = default;
            int bank = unchecked((int)((((data & unchecked((uint)(1))) << 2) | ((data & unchecked((uint)(192))) >> 6))));
            address = unchecked((uint)((bank << 2)));
            for (i = 0; (i < 64); i++) {
            memory_map[(int)(i)].@base = new ArrayPointer<byte>(cart.rom, (int)((((address + unchecked((uint)(i))) & unchecked((uint)(63))) << 16)));
            }
        }
    }
    private void mapper_wd1601_w(uint address, uint data)
    {
        unchecked
        {
            int i = default;
            if (((address & unchecked((uint)(254))) == unchecked((uint)(2)))) {
            for (i = 0; (i < 32); i++) {
            memory_map[(int)(i)].@base = new ArrayPointer<byte>(cart.rom, (int)(((32 + i) << 16)));
            }
            for (i = 32; (i < 64); i++) {
            memory_map[(int)(i)].@base = new ArrayPointer<byte>(sram.sram);
            memory_map[(int)(i)].read8 = sram_read_byte;
            memory_map[(int)(i)].read16 = sram_read_word;
            memory_map[(int)(i)].write8 = sram_write_byte;
            memory_map[(int)(i)].write16 = sram_write_word;
            zbank_memory_map[(int)(i)].read = sram_read_byte;
            zbank_memory_map[(int)(i)].write = sram_write_byte;
            }
            }
        }
    }
    private uint mapper_64k_radica_r(uint address)
    {
        unchecked
        {
            int i = 0;
            int index = unchecked((int)(((address >> 1) & unchecked((uint)(63)))));
            for (i = 0; (i < 64); i++) {
            memory_map[(int)(i)].@base = new ArrayPointer<byte>(cart.rom, (int)(((index | i) << 16)));
            }
            return unchecked((uint)(65535));
        }
    }
    private uint mapper_128k_radica_r(uint address)
    {
        unchecked
        {
            int i = 0;
            int index = unchecked((int)(((address >> 1) & unchecked((uint)(62)))));
            for (i = 0; (i < 32); i++) {
            memory_map[(int)(i)].@base = new ArrayPointer<byte>(cart.rom, (int)(((index | i) << 16)));
            }
            for (i = 32; (i < 64); i++) {
            memory_map[(int)(i)].@base = new ArrayPointer<byte>(sram.sram);
            memory_map[(int)(i)].read8 = sram_read_byte;
            memory_map[(int)(i)].read16 = sram_read_word;
            memory_map[(int)(i)].write8 = sram_write_byte;
            memory_map[(int)(i)].write16 = sram_write_word;
            zbank_memory_map[(int)(i)].read = sram_read_byte;
            zbank_memory_map[(int)(i)].write = sram_write_byte;
            }
            return unchecked((uint)(65535));
        }
    }
    private void mapper_sr16v1_w(uint address, uint data)
    {
        unchecked
        {
            ctrl_io_write_byte(address, data);
            if (((address & unchecked((uint)(65281))) == unchecked((uint)(16641)))) {
            if (((data & unchecked((uint)(1))) != 0)) {
            m68k_pulse_reset();
            }
            }
        }
    }
    private void default_time_w(uint address, uint data)
    {
        unchecked
        {
            if ((address < unchecked((uint)(10563680)))) {
            mapper_64k_multi_w(address);
            cart.hw.bankshift = unchecked((ushort)(1));
            return;
            }
            if ((address > unchecked((uint)(10563825)))) {
            mapper_512k_w(address, data);
            } else {
            mapper_sega_w(data);
            }
        }
    }
    private uint default_regs_r(uint address)
    {
        unchecked
        {
            int i = default;
            for (i = 0; (i < 4); i++) {
            if (((address & cart.hw.mask[(int)(i)]) == cart.hw.addr[(int)(i)])) {
            return unchecked((uint)(cart.hw.regs[(int)(i)]));
            }
            }
            return m68k_read_bus_8(address);
        }
    }
    private uint default_regs_r_16(uint address)
    {
        unchecked
        {
            int i = default;
            for (i = 0; (i < 4); i++) {
            if (((address & cart.hw.mask[(int)(i)]) == cart.hw.addr[(int)(i)])) {
            return unchecked((uint)((unchecked((int)(cart.hw.regs[(int)(i)])) << 8)));
            }
            }
            return m68k_read_bus_16(address);
        }
    }
    private void default_regs_w(uint address, uint data)
    {
        unchecked
        {
            int i = default;
            for (i = 0; (i < 4); i++) {
            if (((address & cart.hw.mask[(int)(i)]) == cart.hw.addr[(int)(i)])) {
            cart.hw.regs[(int)(i)] = unchecked((byte)(data));
            return;
            }
            }
            m68k_unused_8_w(address, data);
        }
    }
    private uint custom_regs_r(uint address)
    {
        unchecked
        {
            int i = default;
            for (i = 0; (i < 4); i++) {
            if (((address & cart.hw.mask[(int)(i)]) == cart.hw.addr[(int)(i)])) {
            return unchecked((uint)((unchecked((int)(cart.hw.regs[(int)(i)])) >> 1)));
            }
            }
            return m68k_read_bus_8(address);
        }
    }
    private void custom_regs_w(uint address, uint data)
    {
        unchecked
        {
            byte temp = default;
            if (((address >> 16) > unchecked((uint)(111)))) {
            mapper_32k_w(data);
            return;
            }
            default_regs_w(address, data);
            temp = cart.hw.regs[(int)(0)];
            switch ((unchecked((int)(cart.hw.regs[(int)(1)])) & 3)) {
            case 0:
            cart.hw.regs[(int)(2)] = unchecked((byte)((unchecked((int)(temp)) << 1)));
            break;
            case 1:
            cart.hw.regs[(int)(2)] = unchecked((byte)((unchecked((int)(temp)) >> 1)));
            return;
            case 2:
            cart.hw.regs[(int)(2)] = unchecked((byte)(((unchecked((int)(temp)) >> 4) | ((unchecked((int)(temp)) & 15) << 4))));
            return;
            default:
            cart.hw.regs[(int)(2)] = unchecked((byte)((((((((((unchecked((int)(temp)) >> 7) & 1) | ((unchecked((int)(temp)) >> 5) & 2)) | ((unchecked((int)(temp)) >> 3) & 4)) | ((unchecked((int)(temp)) >> 1) & 8)) | ((unchecked((int)(temp)) << 1) & 16)) | ((unchecked((int)(temp)) << 3) & 32)) | ((unchecked((int)(temp)) << 5) & 64)) | ((unchecked((int)(temp)) << 7) & 128))));
            return;
            }
        }
    }
    private void custom_alt_regs_w(uint address, uint data)
    {
        unchecked
        {
            if (((address >> 16) > unchecked((uint)(95)))) {
            mapper_64k_w(data);
            return;
            }
            default_regs_w(address, data);
        }
    }
    private uint tekken_regs_r(uint address)
    {
        unchecked
        {
            if (((address & unchecked((uint)(14))) == unchecked((uint)(2)))) {
            return unchecked((uint)((unchecked((int)(cart.hw.regs[(int)(0)])) - 1)));
            }
            return m68k_read_bus_16(address);
        }
    }
    private void tekken_regs_w(uint address, uint data)
    {
        unchecked
        {
            switch ((address & unchecked((uint)(14)))) {
            case 0:
            {
            cart.hw.regs[(int)(0)] = unchecked((byte)(0));
            break;
            }
            case 2:
            {
            break;
            }
            case 12:
            {
            break;
            }
            case 14:
            {
            break;
            }
            default:
            {
            if (((data & unchecked((uint)(1))) != 0)) {
            cart.hw.regs[(int)(0)] |= unchecked((byte)((1 << unchecked((int)((((address - unchecked((uint)(4))) >> 1) & unchecked((uint)(3))))))));
            }
            break;
            }
            }
        }
    }
    private uint topshooter_r(uint address)
    {
        unchecked
        {
            if ((address < unchecked((uint)(2105344)))) {
            byte temp = unchecked((byte)(255));
            switch ((address & unchecked((uint)(255)))) {
            case 67:
            {
            if (((unchecked((int)(digital_inputs[(int)(0)])) & 64) != 0)) temp &= unchecked((byte)((~128)));
            if (((unchecked((int)(digital_inputs[(int)(0)])) & 16) != 0)) temp &= unchecked((byte)((~16)));
            if (((unchecked((int)(digital_inputs[(int)(0)])) & 128) != 0)) temp &= unchecked((byte)((~32)));
            break;
            }
            case 69:
            {
            if (((unchecked((int)(digital_inputs[(int)(0)])) & 1) != 0)) temp &= unchecked((byte)((~8)));
            if (((unchecked((int)(digital_inputs[(int)(0)])) & 2) != 0)) temp &= unchecked((byte)((~16)));
            break;
            }
            case 71:
            {
            if (((unchecked((int)(digital_inputs[(int)(0)])) & 8) != 0)) temp &= unchecked((byte)((~3)));
            break;
            }
            case 73:
            {
            if (((unchecked((int)(digital_inputs[(int)(0)])) & 4) != 0)) temp &= unchecked((byte)((~3)));
            if (((unchecked((int)(digital_inputs[(int)(0)])) & 32) != 0)) temp &= unchecked((byte)((~1)));
            break;
            }
            case 81:
            {
            temp = unchecked((byte)(165));
            break;
            }
            default:
            {
            temp = unchecked((byte)(m68k_read_bus_8(address)));
            break;
            }
            }
            return unchecked((uint)(temp));
            }
            return unchecked((uint)(sram.sram[(int)(((address & unchecked((uint)(65535))) ^ unchecked((uint)(1))))]));
        }
    }
    private void topshooter_w(uint address, uint data)
    {
        unchecked
        {
            if ((address >= unchecked((uint)(2105344)))) {
            sram.sram[(int)(((address & unchecked((uint)(65535))) ^ unchecked((uint)(1))))] = unchecked((byte)((data & unchecked((uint)(255)))));
            return;
            }
            m68k_unused_8_w(address, data);
        }
    }
}
