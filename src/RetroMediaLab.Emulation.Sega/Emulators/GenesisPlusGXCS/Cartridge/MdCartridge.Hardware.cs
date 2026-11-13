using System.Buffers.Binary;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Video;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cartridge;

internal sealed partial class MdCartridge
{
    private void md_cart_init()
    {
        unchecked
        {
            int i = default;
            uint size = unchecked((uint)(65536));
            while ((cart.romsize > size)) size <<= 1;
            if ((strstr(rominfo.international, "SONIC & KNUCKLES") != 0)) {
            size = unchecked((uint)(4194304));
            }
            if ((cart.romsize < size)) {
            if ((size < unchecked((uint)(10485760)))) {
            FillMemory((new CPointer<byte>(cart.rom) + unchecked((int)(cart.romsize))), 255, unchecked((int)((size - cart.romsize))));
            } else {
            FillMemory((new CPointer<byte>(cart.rom) + unchecked((int)(cart.romsize))), 255, unchecked((int)((unchecked((uint)(10485760)) - cart.romsize))));
            }
            }
            cart.mask = (size - unchecked((uint)(1)));
            cart.special = unchecked((byte)(0));
            for (i = 0; (i < 64); i++) {
            memory_map[(int)(i)].@base = (new CPointer<byte>(cart.rom) + unchecked((int)((unchecked((uint)((i << 16))) & cart.mask))));
            memory_map[(int)(i)].read8 = null;
            memory_map[(int)(i)].read16 = null;
            memory_map[(int)(i)].write8 = m68k_unused_8_w;
            memory_map[(int)(i)].write16 = m68k_unused_16_w;
            zbank_memory_map[(int)(i)].read = null;
            zbank_memory_map[(int)(i)].write = zbank_unused_w;
            }
            for (i = 64; (i < 128); i++) {
            memory_map[(int)(i)].@base = (new CPointer<byte>(cart.rom) + (i << 16));
            memory_map[(int)(i)].read8 = m68k_read_bus_8;
            memory_map[(int)(i)].read16 = m68k_read_bus_16;
            memory_map[(int)(i)].write8 = m68k_unused_8_w;
            memory_map[(int)(i)].write16 = m68k_unused_16_w;
            zbank_memory_map[(int)(i)].read = zbank_unused_r;
            zbank_memory_map[(int)(i)].write = zbank_unused_w;
            }
            if ((((strstr(rominfo.product, "00004054-01") != 0) && (cart.romsize == unchecked((uint)(524288)))) && (unchecked((int)(rominfo.checksum)) == 42163))) {
            for (i = 0; (i < 16); i++) {
            memory_map[(int)(i)].@base = (memory_map[(int)((i + 32))].@base = (new CPointer<byte>(cart.rom) + ((i & 3) << 16)));
            }
            for (i = 16; (i < 32); i++) {
            memory_map[(int)(i)].@base = (memory_map[(int)((i + 32))].@base = ((new CPointer<byte>(cart.rom) + 262144) + ((i & 3) << 16)));
            }
            }
            sram_init();
            eeprom_i2c_init();
            if (((sram.on != 0) && (!(sram.type != 0)))) {
            if ((sram.start >= cart.romsize)) {
            if ((strstr(rominfo.international, "SONIC THE HEDGEHOG 3") == 0)) {
            memory_map[(int)((sram.start >> 16))].@base = new CPointer<byte>(sram.sram);
            memory_map[(int)((sram.start >> 16))].read8 = sram_read_byte;
            memory_map[(int)((sram.start >> 16))].read16 = sram_read_word;
            memory_map[(int)((sram.start >> 16))].write8 = sram_write_byte;
            memory_map[(int)((sram.start >> 16))].write16 = sram_write_word;
            zbank_memory_map[(int)((sram.start >> 16))].read = sram_read_byte;
            zbank_memory_map[(int)((sram.start >> 16))].write = sram_write_byte;
            }
            if ((strstr(rominfo.product, "T-119186") != 0)) {
            for (i = 33; (i < 64); i++) {
            memory_map[(int)(i)].@base = new CPointer<byte>(sram.sram);
            memory_map[(int)(i)].read8 = sram_read_byte;
            memory_map[(int)(i)].read16 = sram_read_word;
            memory_map[(int)(i)].write8 = sram_write_byte;
            memory_map[(int)(i)].write16 = sram_write_word;
            zbank_memory_map[(int)(i)].read = sram_read_byte;
            zbank_memory_map[(int)(i)].write = sram_write_byte;
            }
            }
            } else if (((strstr(rominfo.product, "T-172026") != 0) || (strstr(rominfo.product, "T-172116") != 0))) {
            for (i = 32; (i < 48); i++) {
            memory_map[(int)(i)].@base = new CPointer<byte>(sram.sram);
            memory_map[(int)(i)].read8 = sram_read_byte;
            memory_map[(int)(i)].read16 = sram_read_word;
            memory_map[(int)(i)].write8 = sram_write_byte;
            memory_map[(int)(i)].write16 = sram_write_word;
            zbank_memory_map[(int)(i)].read = sram_read_byte;
            zbank_memory_map[(int)(i)].write = sram_write_byte;
            }
            if ((unchecked((int)(cart.rom[(int)((2097152 ^ 1))])) != 255)) {
            for (i = 48; (i < 64); i++) {
            memory_map[(int)(i)].@base = (new CPointer<byte>(cart.rom) + ((i - 16) << 16));
            }
            }
            }
            }
            svp = (0 != 0);
            if (((unchecked((int)(cart.rom[(int)((456 ^ 1))])) == (int)'S') && (unchecked((int)(cart.rom[(int)((457 ^ 1))])) == (int)'V'))) {
            svp_init();
            }
            ClearHardware();
            if (memory_map[(int)(32)].@base.Same(sram.sram)) {
            cart.hw.regs[(int)(0)] = unchecked((byte)(((unchecked((uint)(2097152)) & cart.mask) >> 16)));
            }
            for (i = 0; (i < 71); i++) {
            if (((unchecked((int)(rominfo.checksum)) == unchecked((int)(rom_database[(int)(i)].chk_1))) && (unchecked((int)(rominfo.realchecksum)) == unchecked((int)(rom_database[(int)(i)].chk_2))))) {
            int j = unchecked((int)(rom_database[(int)(i)].bank_start));
            CopyHardware(rom_database[(int)(i)].cart_hw);
            while ((j <= unchecked((int)(rom_database[(int)(i)].bank_end)))) {
            if ((cart.hw.regs_r != null)) {
            memory_map[(int)(j)].read8 = cart.hw.regs_r;
            memory_map[(int)(j)].read16 = cart.hw.regs_r;
            zbank_memory_map[(int)(j)].read = cart.hw.regs_r;
            }
            if ((cart.hw.regs_w != null)) {
            memory_map[(int)(j)].write8 = cart.hw.regs_w;
            memory_map[(int)(j)].write16 = cart.hw.regs_w;
            zbank_memory_map[(int)(j)].write = cart.hw.regs_w;
            }
            j++;
            }
            break;
            }
            }
            if ((cart.hw.realtec != 0)) {
            for (i = 0; (i < 8); i++) {
            CopyMemory(((new CPointer<byte>(cart.rom) + 4194304) + (i * 8192)), (new CPointer<byte>(cart.rom) + 516096), 8192);
            }
            memory_map[(int)(0)].read16 = mapper_realtec_r;
            } else if (((((strstr(rominfo.international, "DEMONS OF ASTEBORG") != 0) || (strstr(rominfo.international, "ASTEBROS") != 0)) || (strstr(rominfo.domestic, "DAEMON CLAW") != 0)) || (strstr(rominfo.domestic, "Real Bout Fatal Fury Genesis Lite") != 0))) {
            cart.hw.time_w = default_time_w;
            } else if ((strstr(rominfo.consoletype, "SEGA SSF") != 0)) {
            cart.hw.time_w = mapper_512k_w;
            cart.hw.bankshift = unchecked((ushort)(1));
            } else if (((strstr(rominfo.consoletype, "SEGA SSF2") != 0) && (cart.romsize <= unchecked((uint)(8388608))))) {
            cart.special |= unchecked((byte)(16));
            cart.hw.time_w = megasd_enhanced_ssf2_mapper_w;
            cart.hw.bankshift = unchecked((ushort)(1));
            } else if (((strstr(rominfo.consoletype, "SEGA MEGASD") != 0) && (cart.romsize <= unchecked((uint)(4194304))))) {
            cart.special |= unchecked((byte)(16));
            cart.hw.time_w = megasd_rom_mapper_w;
            } else if ((strstr(rominfo.domestic, "SUPER STREET FIGHTER2") != 0)) {
            cart.hw.time_w = mapper_ssf2_w;
            cart.hw.bankshift = unchecked((ushort)(1));
            } else if ((strstr(rominfo.product, "T-5740") != 0)) {
            cart.hw.bankshift = unchecked((ushort)(1));
            memory_map[(int)(1)].read8 = mapper_t5740_r;
            zbank_memory_map[(int)(1)].read = mapper_t5740_r;
            cart.hw.time_w = mapper_t5740_w;
            cart.hw.time_r = eeprom_spi_read;
            eeprom_spi_init();
            } else if (((strstr(rominfo.ROMType, "SF") != 0) && (strstr(rominfo.product, "001") != 0))) {
            memory_map[(int)(0)].write8 = mapper_sf001_w;
            memory_map[(int)(0)].write16 = mapper_sf001_w;
            zbank_memory_map[(int)(0)].write = mapper_sf001_w;
            cart.hw.time_w = m68k_unused_8_w;
            for (i = 60; (i < 64); i++) {
            memory_map[(int)(i)].@base = (new CPointer<byte>(cart.rom) + (i << 16));
            memory_map[(int)(i)].read8 = null;
            memory_map[(int)(i)].read16 = null;
            memory_map[(int)(i)].write8 = m68k_unused_8_w;
            memory_map[(int)(i)].write16 = m68k_unused_16_w;
            zbank_memory_map[(int)(i)].read = null;
            zbank_memory_map[(int)(i)].write = m68k_unused_8_w;
            }
            } else if (((strstr(rominfo.ROMType, "SF") != 0) && (strstr(rominfo.product, "002") != 0))) {
            memory_map[(int)(0)].write8 = mapper_sf002_w;
            memory_map[(int)(0)].write16 = mapper_sf002_w;
            zbank_memory_map[(int)(0)].write = mapper_sf002_w;
            cart.hw.time_w = m68k_unused_8_w;
            } else if (((strstr(rominfo.ROMType, "SF") != 0) && (strstr(rominfo.product, "004") != 0))) {
            memory_map[(int)(0)].write8 = mapper_sf004_w;
            memory_map[(int)(0)].write16 = mapper_sf004_w;
            zbank_memory_map[(int)(0)].write = mapper_sf004_w;
            cart.hw.time_r = mapper_sf004_r;
            cart.hw.time_w = m68k_unused_8_w;
            for (i = 0; (i < 32); i++) {
            memory_map[(int)(i)].@base = (new CPointer<byte>(cart.rom) + ((i & 3) << 16));
            }
            for (i = 32; (i < 48); i++) {
            memory_map[(int)(i)].@base = new CPointer<byte>(sram.sram);
            memory_map[(int)(i)].read8 = m68k_read_bus_8;
            memory_map[(int)(i)].read16 = m68k_read_bus_16;
            memory_map[(int)(i)].write8 = m68k_unused_8_w;
            memory_map[(int)(i)].write16 = m68k_unused_16_w;
            zbank_memory_map[(int)(i)].read = m68k_read_bus_8;
            zbank_memory_map[(int)(i)].write = zbank_unused_w;
            }
            for (i = 48; (i < 64); i++) {
            memory_map[(int)(i)].read8 = m68k_read_bus_8;
            memory_map[(int)(i)].read16 = m68k_read_bus_16;
            memory_map[(int)(i)].write8 = m68k_unused_8_w;
            memory_map[(int)(i)].write16 = m68k_unused_16_w;
            zbank_memory_map[(int)(i)].read = m68k_read_bus_8;
            zbank_memory_map[(int)(i)].write = zbank_unused_w;
            }
            } else if (((strstr(rominfo.ROMType, "GM") != 0) && (strstr(rominfo.product, "00000000-42") != 0))) {
            flash_cfi_init(0, default(CPointer<ushort>));
            memory_map[(int)(0)].read8 = mapper_flash_r8;
            memory_map[(int)(0)].read16 = mapper_flash_r16;
            memory_map[(int)(0)].write8 = mapper_flash_w8;
            memory_map[(int)(0)].write16 = mapper_flash_w16;
            zbank_memory_map[(int)(0)].read = mapper_flash_r8;
            zbank_memory_map[(int)(0)].write = mapper_flash_w8;
            } else if ((((strstr(rominfo.ROMType, "GM") != 0) && (strstr(rominfo.product, "00000000-00") != 0)) && (((unchecked((int)(rominfo.checksum)) == 52725) && (unchecked((int)(rominfo.realchecksum)) == 24634)) || ((unchecked((int)(rominfo.checksum)) == 27605) && (unchecked((int)(rominfo.realchecksum)) == 8170))))) {
            flash_cfi_init(1, default(CPointer<ushort>));
            memory_map[(int)(63)].@base = new CPointer<byte>(sram.sram);
            memory_map[(int)(0)].read8 = (memory_map[(int)(63)].read8 = mapper_flash_r8);
            memory_map[(int)(0)].read16 = (memory_map[(int)(63)].read16 = mapper_flash_r16);
            memory_map[(int)(0)].write8 = (memory_map[(int)(63)].write8 = mapper_flash_w8);
            memory_map[(int)(0)].write16 = (memory_map[(int)(63)].write16 = mapper_flash_w16);
            zbank_memory_map[(int)(0)].read = (zbank_memory_map[(int)(63)].read = mapper_flash_r8);
            zbank_memory_map[(int)(0)].write = (zbank_memory_map[(int)(63)].write = mapper_flash_w8);
            } else if ((((strstr(rominfo.ROMType, "GM") != 0) && (strstr(rominfo.product, "00000000-00") != 0)) && ((unchecked((int)(rominfo.checksum)) == 17857) && (unchecked((int)(rominfo.realchecksum)) == 50707)))) {
            cart.hw.time_w = mapper_kaiserwave_w;
            cart.special |= unchecked((byte)(32));
            EnsureYxAudio();
            yx5200_init(sample_rate);
            flash_cfi_init(1, default(CPointer<ushort>));
            memory_map[(int)(63)].@base = new CPointer<byte>(sram.sram);
            memory_map[(int)(0)].read8 = (memory_map[(int)(63)].read8 = mapper_flash_r8);
            memory_map[(int)(0)].read16 = (memory_map[(int)(63)].read16 = mapper_flash_r16);
            memory_map[(int)(0)].write8 = (memory_map[(int)(63)].write8 = mapper_flash_w8);
            memory_map[(int)(0)].write16 = (memory_map[(int)(63)].write16 = mapper_flash_w16);
            zbank_memory_map[(int)(0)].read = (zbank_memory_map[(int)(63)].read = mapper_flash_r8);
            zbank_memory_map[(int)(0)].write = (zbank_memory_map[(int)(63)].write = mapper_flash_w8);
            for (i = 64; (i < 128); i++) {
            memory_map[(int)(i)].read8 = null;
            memory_map[(int)(i)].read16 = null;
            memory_map[(int)(i)].write8 = m68k_unused_8_w;
            memory_map[(int)(i)].write16 = m68k_unused_16_w;
            zbank_memory_map[(int)(i)].read = null;
            zbank_memory_map[(int)(i)].write = zbank_unused_w;
            }
            } else if ((((strstr(rominfo.ROMType, "GM") != 0) && (strstr(rominfo.product, "00000000-00") != 0)) && ((unchecked((int)(rominfo.checksum)) == 8836) && (unchecked((int)(rominfo.realchecksum)) == 36303)))) {
            cart.hw.time_w = mapper_kaiserwave_w;
            cart.special |= unchecked((byte)(32));
            EnsureYxAudio();
            yx5200_init(sample_rate);
            for (i = 64; (i < 128); i++) {
            memory_map[(int)(i)].read8 = null;
            memory_map[(int)(i)].read16 = null;
            memory_map[(int)(i)].write8 = m68k_unused_8_w;
            memory_map[(int)(i)].write16 = m68k_unused_16_w;
            zbank_memory_map[(int)(i)].read = null;
            zbank_memory_map[(int)(i)].write = zbank_unused_w;
            }
            } else if ((strstr(rominfo.international, "COLOCODX") != 0)) {
            flash_cfi_init(1, new CPointer<ushort>(FlashOtp));
            memory_map[(int)(126)].@base = new CPointer<byte>(sram.sram);
            memory_map[(int)(0)].read8 = (memory_map[(int)(127)].read8 = (memory_map[(int)(126)].read8 = mapper_flash_r8));
            memory_map[(int)(0)].read16 = (memory_map[(int)(127)].read16 = (memory_map[(int)(126)].read16 = mapper_flash_r16));
            memory_map[(int)(0)].write8 = (memory_map[(int)(127)].write8 = (memory_map[(int)(126)].write8 = mapper_flash_w8));
            memory_map[(int)(0)].write16 = (memory_map[(int)(127)].write16 = (memory_map[(int)(126)].write16 = mapper_flash_w16));
            zbank_memory_map[(int)(0)].read = (zbank_memory_map[(int)(127)].read = (zbank_memory_map[(int)(126)].read = mapper_flash_r8));
            zbank_memory_map[(int)(0)].write = (zbank_memory_map[(int)(127)].write = (zbank_memory_map[(int)(126)].write = mapper_flash_w8));
            for (i = 64; (i < 126); i++) {
            memory_map[(int)(i)].read8 = null;
            memory_map[(int)(i)].read16 = null;
            memory_map[(int)(i)].write8 = m68k_unused_8_w;
            memory_map[(int)(i)].write16 = m68k_unused_16_w;
            zbank_memory_map[(int)(i)].read = null;
            zbank_memory_map[(int)(i)].write = zbank_unused_w;
            }
            } else if ((unchecked((int)(sram.type)) >= 4)) {
            int i__1 = default;
            int sram_start = unchecked((int)((sram.start >> 16)));
            int sram_end = unchecked((int)((sram.end >> 16)));
            flash_cfi_init((unchecked((int)(sram.type)) - 4), default(CPointer<ushort>));
            for (i__1 = 0; (i__1 < 128); i__1++) {
            if (((i__1 == 0) || ((i__1 >= sram_start) && (i__1 <= sram_end)))) {
            memory_map[(int)(i__1)].@base = ((i__1 != sram_start) ? (new CPointer<byte>(cart.rom) + (i__1 << 16)) : new CPointer<byte>(sram.sram));
            memory_map[(int)(i__1)].read8 = mapper_flash_r8;
            memory_map[(int)(i__1)].read16 = mapper_flash_r16;
            memory_map[(int)(i__1)].write8 = mapper_flash_w8;
            memory_map[(int)(i__1)].write16 = mapper_flash_w16;
            zbank_memory_map[(int)(i__1)].read = mapper_flash_r8;
            zbank_memory_map[(int)(i__1)].write = mapper_flash_w8;
            } else {
            memory_map[(int)(i__1)].@base = (new CPointer<byte>(cart.rom) + (i__1 << 16));
            memory_map[(int)(i__1)].read8 = null;
            memory_map[(int)(i__1)].read16 = null;
            memory_map[(int)(i__1)].write8 = m68k_unused_8_w;
            memory_map[(int)(i__1)].write16 = m68k_unused_16_w;
            zbank_memory_map[(int)(i__1)].read = null;
            zbank_memory_map[(int)(i__1)].write = zbank_unused_w;
            }
            }
            } else if (((((((cart.romsize == unchecked((uint)(4194304))) && (unchecked((int)(cart.rom[(int)((2097488 ^ 1))])) == (int)'C')) && (unchecked((int)(cart.rom[(int)((2097489 ^ 1))])) == (int)'A')) && (unchecked((int)(cart.rom[(int)((2097490 ^ 1))])) == (int)'N')) && (unchecked((int)(cart.rom[(int)((2097491 ^ 1))])) == (int)'O')) && (unchecked((int)(cart.rom[(int)((2097492 ^ 1))])) == (int)'N'))) {
            cart.hw.time_w = mapper_wd1601_w;
            cart.hw.bankshift = unchecked((ushort)(1));
            sram.on = unchecked((byte)(1));
            sram.start = unchecked((uint)(2097152));
            sram.end = unchecked((uint)(2105343));
            } else if ((((unchecked((int)((new CPointer<byte>(cart.rom) + 8).Cast<ushort>()[0])) == 24576) && (unchecked((int)((new CPointer<byte>(cart.rom) + 10).Cast<ushort>()[0])) == 502)) && (unchecked((int)(rominfo.realchecksum)) == 63636))) {
            for (i = 8; (i < 16); i++) {
            memory_map[(int)(i)].@base = (new CPointer<byte>(cart.rom) + ((i & 7) << 16));
            }
            for (i = 16; (i < 64); i++) {
            memory_map[(int)(i)].read8 = m68k_read_bus_8;
            memory_map[(int)(i)].read16 = m68k_read_bus_16;
            memory_map[(int)(i)].write8 = m68k_unused_8_w;
            memory_map[(int)(i)].write16 = m68k_unused_16_w;
            zbank_memory_map[(int)(i)].read = m68k_read_bus_8;
            zbank_memory_map[(int)(i)].write = zbank_unused_w;
            }
            for (i = 96; (i < 112); i++) {
            memory_map[(int)(i)].@base = (new CPointer<byte>(cart.rom) + 983040);
            memory_map[(int)(i)].read8 = (((i & 7) < 4) ? null : mapper_smw_64_r);
            memory_map[(int)(i)].read16 = (((i & 7) < 4) ? null : mapper_smw_64_r);
            memory_map[(int)(i)].write8 = mapper_smw_64_w;
            memory_map[(int)(i)].write16 = mapper_smw_64_w;
            zbank_memory_map[(int)(i)].read = (((i & 7) < 4) ? null : mapper_smw_64_r);
            zbank_memory_map[(int)(i)].write = mapper_smw_64_w;
            }
            } else if ((((unchecked((int)((new CPointer<byte>(cart.rom) + 4).Cast<ushort>()[0])) == 0) && (unchecked((int)((new CPointer<byte>(cart.rom) + 6).Cast<ushort>()[0])) == 260)) && (unchecked((int)(rominfo.checksum)) == 12796))) {
            memory_map[(int)(161)].write8 = mapper_sr16v1_w;
            } else if ((cart.romsize > unchecked((uint)(4194304)))) {
            for (i = 64; (i < 160); i++) {
            memory_map[(int)(i)].@base = (new CPointer<byte>(cart.rom) + (i << 16));
            memory_map[(int)(i)].read8 = null;
            memory_map[(int)(i)].read16 = null;
            zbank_memory_map[(int)(i)].read = null;
            }
            }
            if ((!(cart.hw.time_w != null))) {
            cart.hw.time_w = default_time_w;
            }
            ggenie_shutdown();
            areplay_shutdown();
            switch (config.lock_on) {
            case 1:
            {
            ggenie_init();
            break;
            }
            case 2:
            {
            areplay_init();
            break;
            }
            case 3:
            {
            if ((cart.romsize > unchecked((uint)(4194304)))) break;
            if ((load_archive(0, (new CPointer<byte>(cart.rom) + 4194304), 2097152, default(CPointer<byte>)) == 2097152)) {
            if ((!(memcmp(((new CPointer<byte>(cart.rom) + 4194304) + 288), "SONIC & KNUCKLES", 16) != 0))) {
            if ((load_archive(1, (new CPointer<byte>(cart.rom) + 6291456), 262144, default(CPointer<byte>)) == 262144)) {
            for (i = 0; (i < 32); i++) {
            memory_map[(int)(i)].@base = ((new CPointer<byte>(cart.rom) + 4194304) + (i << 16));
            }
            for (i = 0; (i < 2097152); i += 2) {
            byte temp = cart.rom[(int)((i + 4194304))];
            cart.rom[(int)((i + 4194304))] = cart.rom[(int)(((i + 4194304) + 1))];
            cart.rom[(int)(((i + 4194304) + 1))] = temp;
            }
            for (i = 0; (i < 262144); i += 2) {
            byte temp = cart.rom[(int)((i + 6291456))];
            cart.rom[(int)((i + 6291456))] = cart.rom[(int)(((i + 6291456) + 1))];
            cart.rom[(int)(((i + 6291456) + 1))] = temp;
            }
            cart.special |= unchecked((byte)(8));
            }
            }
            }
            break;
            }
            default:
            {
            break;
            }
            }
            if ((((cart.romsize <= unchecked((uint)(8388608))) && (unchecked((int)(system_hw)) == 128)) && ((unchecked((int)(config.add_on)) == 2) || ((unchecked((int)(config.add_on)) | disc_loaded) == 2)))) {
            cart.special |= unchecked((byte)(16));
            }
            if ((((unchecked((int)(cart.special)) & 16) != 0) && (!has_cd_audio))) {
            EnsureCdAudio();
            }
        }
    }
    private void md_cart_reset(int hard_reset)
    {
        unchecked
        {
            int i = default;
            if ((cart.hw.realtec != 0)) {
            for (i = 0; (i < 64); i++) {
            memory_map[(int)(i)].@base = (new CPointer<byte>(cart.rom) + 4194304);
            }
            cart.hw.regs[(int)(0)] = unchecked((byte)(0));
            cart.hw.regs[(int)(1)] = unchecked((byte)(0));
            cart.hw.regs[(int)(2)] = unchecked((byte)(0));
            } else if ((cart.hw.bankshift != 0)) {
            for (i = 0; (i < 64); i++) {
            memory_map[(int)(i)].@base = (new CPointer<byte>(cart.rom) + unchecked((int)((unchecked((uint)((i << 16))) & cart.mask))));
            }
            }
            if (((unchecked((int)(cart.special)) & 16) != 0)) {
            megasd_reset();
            }
            if (((unchecked((int)(cart.special)) & 32) != 0)) {
            yx5200_reset();
            }
            if (svp) {
            svp_reset();
            }
            switch (config.lock_on) {
            case 1:
            {
            ggenie_reset(hard_reset);
            break;
            }
            case 2:
            {
            areplay_reset(hard_reset);
            break;
            }
            case 3:
            {
            if (((unchecked((int)(cart.special)) & 8) != 0)) {
            for (i = 48; (i < 64); i++) {
            memory_map[(int)(i)].@base = (new CPointer<byte>(cart.rom) + unchecked((int)((unchecked((uint)((i << 16))) & cart.mask))));
            }
            }
            break;
            }
            default:
            {
            break;
            }
            }
        }
    }
    private int md_cart_context_save(CPointer<byte> state)
    {
        unchecked
        {
            int i = default;
            int bufferptr = 0;
            CPointer<byte> @base = default;
            for (i = 0; (i < 64); i++) {
            @base = memory_map[(int)(i)].@base;
            if (@base.Same(sram.sram)) {
            state[(int)((bufferptr++))] = unchecked((byte)(255));
            } else if (@base.Same(boot_rom)) {
            state[(int)((bufferptr++))] = unchecked((byte)(254));
            } else {
            state[(int)((bufferptr++))] = unchecked((byte)((((@base - new CPointer<byte>(cart.rom)) >> 16) & 255)));
            }
            }
            bufferptr += SaveChunk((state + bufferptr), new CPointer<byte>(cart.hw.regs), 4);
            if (svp) {
            bufferptr += SaveSvp((state + bufferptr));
            }
            if (((unchecked((int)(cart.special)) & 16) != 0)) {
            bufferptr += megasd_context_save((state + (int)(bufferptr)));
            }
            if ((memory_map[(int)(0)].read8 == mapper_flash_r8)) {
            bufferptr += flash_cfi_context_save((state + (int)(bufferptr)));
            }
            if (((unchecked((int)(cart.special)) & 32) != 0)) {
            bufferptr += yx5200_context_save((state + (int)(bufferptr)));
            }
            return bufferptr;
        }
    }
    private int md_cart_context_load(CPointer<byte> state)
    {
        unchecked
        {
            int i = default;
            int bufferptr = 0;
            byte offset = default;
            for (i = 0; (i < 64); i++) {
            offset = state[(int)((bufferptr++))];
            if ((unchecked((int)(offset)) == 255)) {
            memory_map[(int)(i)].@base = new CPointer<byte>(sram.sram);
            memory_map[(int)(i)].read8 = sram_read_byte;
            memory_map[(int)(i)].read16 = sram_read_word;
            memory_map[(int)(i)].write8 = sram_write_byte;
            memory_map[(int)(i)].write16 = sram_write_word;
            zbank_memory_map[(int)(i)].read = sram_read_byte;
            zbank_memory_map[(int)(i)].write = sram_write_byte;
            } else {
            if (memory_map[(int)(i)].@base.Same(sram.sram)) {
            memory_map[(int)(i)].read8 = null;
            memory_map[(int)(i)].read16 = null;
            memory_map[(int)(i)].write8 = m68k_unused_8_w;
            memory_map[(int)(i)].write16 = m68k_unused_16_w;
            zbank_memory_map[(int)(i)].read = null;
            zbank_memory_map[(int)(i)].write = zbank_unused_w;
            }
            memory_map[(int)(i)].@base = ((unchecked((int)(offset)) == 254) ? boot_rom : (new CPointer<byte>(cart.rom) + (unchecked((int)(offset)) << 16)));
            }
            }
            bufferptr += LoadChunk((state + bufferptr), new CPointer<byte>(cart.hw.regs), 4);
            if (svp) {
            bufferptr += LoadSvp((state + bufferptr));
            }
            if (((unchecked((int)(cart.special)) & 16) != 0)) {
            bufferptr += megasd_context_load((state + (int)(bufferptr)));
            }
            if ((memory_map[(int)(0)].read8 == mapper_flash_r8)) {
            bufferptr += flash_cfi_context_load((state + (int)(bufferptr)));
            }
            if (((unchecked((int)(cart.special)) & 32) != 0)) {
            bufferptr += yx5200_context_load((state + (int)(bufferptr)));
            }
            return bufferptr;
        }
    }
}
