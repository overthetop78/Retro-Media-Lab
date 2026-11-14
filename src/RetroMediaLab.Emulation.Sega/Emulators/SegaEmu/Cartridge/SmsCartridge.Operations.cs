using System.Buffers.Binary;
using RetroMediaLab.Chips.Common.Memory;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Cartridge;

internal sealed partial class SmsCartridge
{
    private void sms_cart_init()
    {
        unchecked
        {
            int i = (302 - 1);
            uint crc = crc32(unchecked((uint)(0)), new ArrayPointer<byte>(cart.rom), cart.romsize);
            FillMemory((new ArrayPointer<byte>(cart.rom) + 5308416), 255, 2048);
            cart_rom.mapper = unchecked((byte)(((cart.romsize > unchecked((uint)(49152))) ? 16 : 0)));
            cart.special = unchecked((byte)(0));
            if (((config.ym2413 & 2) != 0)) {
            if ((((unchecked((int)(system_hw)) & 32) != 0) && (unchecked((int)(region_code)) == 0))) {
            config.ym2413 = 3;
            } else {
            config.ym2413 = 2;
            }
            }
            do {
            if ((crc == game_list[(int)(i)].crc)) {
            cart_rom.mapper = game_list[(int)(i)].mapper;
            if ((game_list[(int)(i)].peripheral != 0)) {
            if ((old_system[(int)(0)] == (-1))) {
            old_system[(int)(0)] = unchecked((int)(port_systems[(int)(0)]));
            }
            port_systems[(int)(0)] = game_list[(int)(i)].peripheral;
            }
            cart.special = game_list[(int)(i)].g_3d;
            if (((!(config.system != 0)) || ((unchecked((int)(config.system)) == 64) && (unchecked((int)(game_list[(int)(i)].system)) == 65)))) {
            system_hw = game_list[(int)(i)].system;
            }
            if (((config.ym2413 & 2) != 0)) {
            config.ym2413 |= unchecked((int)(game_list[(int)(i)].fm));
            }
            break;
            }
            } while (((i--) != 0));
            if ((unchecked((int)(cart_rom.mapper)) < 16)) {
            cart_rom.pages = unchecked((ushort)((((cart.romsize + unchecked((uint)((1 << 10)))) - unchecked((uint)(1))) >> 10)));
            } else if (((unchecked((int)(cart_rom.mapper)) & 32) != 0)) {
            cart_rom.pages = unchecked((ushort)((((cart.romsize + unchecked((uint)((1 << 13)))) - unchecked((uint)(1))) >> 13)));
            } else if (((unchecked((int)(cart_rom.mapper)) & 64) != 0)) {
            cart_rom.pages = unchecked((ushort)((((cart.romsize + unchecked((uint)((1 << 15)))) - unchecked((uint)(1))) >> 15)));
            } else {
            cart_rom.pages = unchecked((ushort)((((cart.romsize + unchecked((uint)((1 << 14)))) - unchecked((uint)(1))) >> 14)));
            }
            if ((unchecked((int)(cart_rom.mapper)) == 18)) {
            eeprom_93c_init();
            } else if ((unchecked((int)(cart_rom.mapper)) == 1)) {
            cart.special |= unchecked((byte)(2));
            }
            sram_init();
            sram.on = unchecked((byte)(1));
            gun_offset_x = 20;
            gun_offset_y = 0;
            if (((crc == unchecked((uint)(1398371885))) || (crc == unchecked((uint)(1606896938))))) {
            gun_offset_x = 16;
            }
            if (((unchecked((int)(config.bios)) & 1) != 0)) {
            int bios_size = load_bios(unchecked((int)(system_hw)));
            if ((bios_size > 49152)) {
            bios_rom.mapper = unchecked((byte)(16));
            bios_rom.pages = unchecked((ushort)((bios_size >> 14)));
            } else if ((bios_size >= 0)) {
            bios_rom.mapper = unchecked((byte)(0));
            bios_rom.pages = unchecked((ushort)((bios_size >> 10)));
            }
            if (((!((unchecked((int)(config.bios)) & 2) != 0)) && (bios_rom.pages != 0))) {
            cart_rom.pages = unchecked((ushort)(0));
            }
            } else {
            system_bios &= unchecked((byte)((~(32 | 64))));
            bios_rom.pages = unchecked((ushort)(0));
            }
        }
    }
    private void sms_cart_reset()
    {
        unchecked
        {
            bios_rom.fcr[(int)(0)] = unchecked((byte)(0));
            bios_rom.fcr[(int)(1)] = unchecked((byte)(0));
            bios_rom.fcr[(int)(2)] = unchecked((byte)(1));
            bios_rom.fcr[(int)(3)] = unchecked((byte)(2));
            switch (cart_rom.mapper) {
            case 16:
            case 17:
            cart_rom.fcr[(int)(0)] = unchecked((byte)(0));
            cart_rom.fcr[(int)(1)] = unchecked((byte)(0));
            cart_rom.fcr[(int)(2)] = unchecked((byte)(1));
            cart_rom.fcr[(int)(3)] = unchecked((byte)(2));
            break;
            case 27:
            cart_rom.fcr[(int)(0)] = unchecked((byte)(0));
            cart_rom.fcr[(int)(1)] = unchecked((byte)(0));
            cart_rom.fcr[(int)(2)] = unchecked((byte)(1));
            cart_rom.fcr[(int)(3)] = unchecked((byte)(1));
            break;
            case 37:
            cart_rom.fcr[(int)(0)] = unchecked((byte)(3));
            cart_rom.fcr[(int)(1)] = unchecked((byte)(2));
            cart_rom.fcr[(int)(2)] = unchecked((byte)(1));
            cart_rom.fcr[(int)(3)] = unchecked((byte)(0));
            break;
            case 32:
            case 33:
            case 34:
            case 29:
            case 36:
            case 35:
            cart_rom.fcr[(int)(0)] = unchecked((byte)(0));
            cart_rom.fcr[(int)(1)] = unchecked((byte)(0));
            cart_rom.fcr[(int)(2)] = unchecked((byte)(0));
            cart_rom.fcr[(int)(3)] = unchecked((byte)(0));
            break;
            default:
            cart_rom.fcr[(int)(0)] = unchecked((byte)(0));
            cart_rom.fcr[(int)(1)] = unchecked((byte)(0));
            cart_rom.fcr[(int)(2)] = unchecked((byte)(1));
            cart_rom.fcr[(int)(3)] = unchecked((byte)(0));
            break;
            }
            if ((unchecked((int)(bios_rom.pages)) > 1)) {
            slot.rom = (new ArrayPointer<byte>(cart.rom) + 4194304);
            slot.fcr = new ArrayPointer<byte>(bios_rom.fcr);
            slot.mapper = bios_rom.mapper;
            slot.pages = bios_rom.pages;
            } else {
            slot.rom = new ArrayPointer<byte>(cart.rom);
            slot.fcr = new ArrayPointer<byte>(cart_rom.fcr);
            slot.mapper = cart_rom.mapper;
            slot.pages = cart_rom.pages;
            if (((unchecked((int)(system_hw)) & 32) != 0)) {
            work_ram[(int)(0)] = unchecked((byte)(168));
            }
            }
            mapper_reset();
            if ((unchecked((int)(bios_rom.pages)) == 1)) {
            z80_readmap[(int)(0)] = (new ArrayPointer<byte>(cart.rom) + 4194304);
            }
        }
    }
    private void sms_cart_switch(byte mode)
    {
        unchecked
        {
            slot.pages = unchecked((ushort)(0));
            if (((unchecked((int)(mode)) & 64) != 0)) {
            if ((cart_rom.pages != 0)) {
            slot.rom = new ArrayPointer<byte>(cart.rom);
            slot.fcr = new ArrayPointer<byte>(cart_rom.fcr);
            slot.mapper = cart_rom.mapper;
            slot.pages = cart_rom.pages;
            }
            } else {
            if (((unchecked((int)(mode)) & 8) != 0)) {
            if ((unchecked((int)(bios_rom.pages)) > 1)) {
            slot.rom = (new ArrayPointer<byte>(cart.rom) + 4194304);
            slot.fcr = new ArrayPointer<byte>(bios_rom.fcr);
            slot.mapper = bios_rom.mapper;
            slot.pages = bios_rom.pages;
            } else {
            slot.rom = new ArrayPointer<byte>(cart.rom);
            slot.fcr = new ArrayPointer<byte>(cart_rom.fcr);
            slot.mapper = cart_rom.mapper;
            slot.pages = cart_rom.pages;
            }
            }
            if ((!(bios_rom.pages != 0))) {
            if ((cart.romsize <= unchecked((uint)(1048576)))) {
            CopyMemory((new ArrayPointer<byte>(cart.rom) + 4194304), new ArrayPointer<byte>(cart.rom), cart.romsize);
            CopyMemory(new ArrayPointer<byte>(bios_rom.fcr), new ArrayPointer<byte>(cart_rom.fcr), unchecked((uint)(4)));
            bios_rom.mapper = cart_rom.mapper;
            bios_rom.pages = cart_rom.pages;
            cart_rom.pages = unchecked((ushort)(0));
            }
            }
            }
            mapper_reset();
            if (((unchecked((int)(bios_rom.pages)) == 1) && ((unchecked((int)(mode)) & 72) == 8))) {
            z80_readmap[(int)(0)] = (new ArrayPointer<byte>(cart.rom) + 4194304);
            }
        }
    }
    private int sms_cart_ram_size()
    {
        unchecked
        {
            if (((unchecked((int)(cart_rom.mapper)) == 3) || (unchecked((int)(cart_rom.mapper)) == 4))) {
            return 8192;
            }
            if ((unchecked((int)(cart_rom.mapper)) == 2)) {
            return 2048;
            }
            return 0;
        }
    }
    private int sms_cart_region_detect()
    {
        unchecked
        {
            int i = (302 - 1);
            uint crc = crc32(unchecked((uint)(0)), new ArrayPointer<byte>(cart.rom), cart.romsize);
            if (((config.ym2413 != 0) && ((crc == unchecked((uint)(583838139))) || (crc == unchecked((uint)(1738413686)))))) {
            return 0;
            }
            do {
            if ((crc == game_list[(int)(i)].crc)) {
            return unchecked((int)(game_list[(int)(i)].region));
            }
            } while (((i--) != 0));
            if ((unchecked((int)(config.system)) == 16)) {
            region_code = unchecked((byte)(0));
            }
            if ((unchecked((int)(system_hw)) >= 32)) {
            if (HasJapaneseHeader()) {
            return 0;
            }
            }
            return 128;
        }
    }
    private int sms_cart_context_save(ArrayPointer<byte> state)
    {
        unchecked
        {
            int bufferptr = 0;
            if (((unchecked((int)(io_reg[(int)(14)])) & 64) != 0)) {
            bufferptr += SaveChunk((state + bufferptr), new ArrayPointer<byte>(bios_rom.fcr), 4);
            } else {
            bufferptr += SaveChunk((state + bufferptr), new ArrayPointer<byte>(cart_rom.fcr), 4);
            }
            if (((unchecked((int)(cart_rom.mapper)) == 3) || (unchecked((int)(cart_rom.mapper)) == 4))) {
            bufferptr += SaveChunk((state + bufferptr), (new ArrayPointer<byte>(work_ram) + 8192), 8192);
            } else if ((unchecked((int)(cart_rom.mapper)) == 2)) {
            bufferptr += SaveChunk((state + bufferptr), (new ArrayPointer<byte>(work_ram) + 8192), 2048);
            }
            return bufferptr;
        }
    }
    private int sms_cart_context_load(ArrayPointer<byte> state)
    {
        unchecked
        {
            int bufferptr = 0;
            if (((unchecked((int)(io_reg[(int)(14)])) & 64) != 0)) {
            bufferptr += LoadChunk((state + bufferptr), new ArrayPointer<byte>(bios_rom.fcr), 4);
            switch (cart_rom.mapper) {
            case 16:
            case 17:
            cart_rom.fcr[(int)(0)] = unchecked((byte)(0));
            cart_rom.fcr[(int)(1)] = unchecked((byte)(0));
            cart_rom.fcr[(int)(2)] = unchecked((byte)(1));
            cart_rom.fcr[(int)(3)] = unchecked((byte)(2));
            break;
            case 27:
            cart_rom.fcr[(int)(0)] = unchecked((byte)(0));
            cart_rom.fcr[(int)(1)] = unchecked((byte)(0));
            cart_rom.fcr[(int)(2)] = unchecked((byte)(1));
            cart_rom.fcr[(int)(3)] = unchecked((byte)(1));
            break;
            case 37:
            cart_rom.fcr[(int)(0)] = unchecked((byte)(3));
            cart_rom.fcr[(int)(1)] = unchecked((byte)(2));
            cart_rom.fcr[(int)(2)] = unchecked((byte)(1));
            cart_rom.fcr[(int)(3)] = unchecked((byte)(0));
            break;
            case 32:
            case 33:
            case 34:
            case 29:
            case 36:
            case 35:
            cart_rom.fcr[(int)(0)] = unchecked((byte)(0));
            cart_rom.fcr[(int)(1)] = unchecked((byte)(0));
            cart_rom.fcr[(int)(2)] = unchecked((byte)(0));
            cart_rom.fcr[(int)(3)] = unchecked((byte)(0));
            break;
            default:
            cart_rom.fcr[(int)(0)] = unchecked((byte)(0));
            cart_rom.fcr[(int)(1)] = unchecked((byte)(0));
            cart_rom.fcr[(int)(2)] = unchecked((byte)(1));
            cart_rom.fcr[(int)(3)] = unchecked((byte)(0));
            break;
            }
            } else {
            bufferptr += LoadChunk((state + bufferptr), new ArrayPointer<byte>(cart_rom.fcr), 4);
            bios_rom.fcr[(int)(0)] = unchecked((byte)(0));
            bios_rom.fcr[(int)(1)] = unchecked((byte)(0));
            bios_rom.fcr[(int)(2)] = unchecked((byte)(1));
            bios_rom.fcr[(int)(3)] = unchecked((byte)(2));
            }
            if (((unchecked((int)(cart_rom.mapper)) == 3) || (unchecked((int)(cart_rom.mapper)) == 4))) {
            bufferptr += LoadChunk((state + bufferptr), (new ArrayPointer<byte>(work_ram) + 8192), 8192);
            } else if ((unchecked((int)(cart_rom.mapper)) == 2)) {
            bufferptr += LoadChunk((state + bufferptr), (new ArrayPointer<byte>(work_ram) + 8192), 2048);
            }
            return bufferptr;
        }
    }
    private void mapper_reset()
    {
        unchecked
        {
            int i = default;
            if ((unchecked((int)(system_hw)) == 1)) {
            for (i = 48; (i < 64); i++) {
            z80_readmap[(int)(i)] = (z80_writemap[(int)(i)] = new ArrayPointer<byte>(work_ram, (int)(0)));
            }
            } else if ((unchecked((int)(system_hw)) == 2)) {
            for (i = 48; (i < 64); i++) {
            z80_readmap[(int)(i)] = (z80_writemap[(int)(i)] = new ArrayPointer<byte>(work_ram, (int)(((i & 1) << 10))));
            }
            } else {
            for (i = 48; (i < 64); i++) {
            z80_readmap[(int)(i)] = (z80_writemap[(int)(i)] = new ArrayPointer<byte>(work_ram, (int)(((i & 7) << 10))));
            }
            }
            if ((!(slot.pages != 0))) {
            for (i = 0; (i < 48); i++) {
            z80_writemap[(int)(i)] = (new ArrayPointer<byte>(cart.rom) + 5308416);
            z80_readmap[(int)(i)] = (new ArrayPointer<byte>(cart.rom) + 5309440);
            }
            z80_readmem = read_mapper_none;
            z80_writemem = write_mapper_none;
            return;
            }
            if ((unchecked((int)(slot.mapper)) < 16)) {
            for (i = 0; (i < 32); i++) {
            z80_readmap[(int)(i)] = (slot.rom + (int)(((i % unchecked((int)(slot.pages))) << 10)));
            z80_writemap[(int)(i)] = (new ArrayPointer<byte>(cart.rom) + 5308416);
            }
            if ((unchecked((int)(slot.mapper)) == 4)) {
            for (i = 8; (i < 16); i++) {
            z80_readmap[(int)(i)] = (z80_writemap[(int)(i)] = new ArrayPointer<byte>(work_ram, (int)((8192 + ((i & 7) << 10)))));
            }
            }
            if ((unchecked((int)(slot.mapper)) == 3)) {
            for (i = 32; (i < 48); i++) {
            z80_readmap[(int)(i)] = (z80_writemap[(int)(i)] = new ArrayPointer<byte>(work_ram, (int)((8192 + ((i & 7) << 10)))));
            }
            } else if ((unchecked((int)(slot.mapper)) == 2)) {
            for (i = 32; (i < 48); i++) {
            z80_readmap[(int)(i)] = (z80_writemap[(int)(i)] = new ArrayPointer<byte>(work_ram, (int)((8192 + ((i & 1) << 10)))));
            }
            } else if ((unchecked((int)(slot.pages)) <= 32)) {
            for (i = 32; (i < 48); i++) {
            z80_writemap[(int)(i)] = (new ArrayPointer<byte>(cart.rom) + 5308416);
            z80_readmap[(int)(i)] = (new ArrayPointer<byte>(cart.rom) + 5309440);
            }
            } else {
            for (i = 32; (i < 48); i++) {
            z80_readmap[(int)(i)] = (slot.rom + (int)(((32 + (i % (unchecked((int)(slot.pages)) - 32))) << 10)));
            z80_writemap[(int)(i)] = (new ArrayPointer<byte>(cart.rom) + 5308416);
            }
            }
            } else {
            for (i = 0; (i < 48); i++) {
            z80_readmap[(int)(i)] = (slot.rom + (int)((i << 10)));
            z80_writemap[(int)(i)] = (new ArrayPointer<byte>(cart.rom) + 5308416);
            }
            if (((unchecked((int)(slot.mapper)) & 32) != 0)) {
            mapper_8k_w(0, slot.fcr[(int)(0)]);
            mapper_8k_w(1, slot.fcr[(int)(1)]);
            mapper_8k_w(2, slot.fcr[(int)(2)]);
            mapper_8k_w(3, slot.fcr[(int)(3)]);
            if ((unchecked((int)(slot.mapper)) == 34)) {
            for (i = 0; (i < 8); i++) {
            z80_readmap[(int)(i)] = (slot.rom + (int)(((15 << 13) | ((i & 7) << 10))));
            }
            }
            } else if (((unchecked((int)(slot.mapper)) & 64) != 0)) {
            mapper_32k_w(slot.fcr[(int)(0)]);
            } else {
            if (((unchecked((int)(slot.mapper)) == 23) || (unchecked((int)(slot.mapper)) == 24))) {
            mapper_16k_w(1, slot.fcr[(int)(1)]);
            mapper_16k_w(2, slot.fcr[(int)(2)]);
            } else if (((unchecked((int)(slot.mapper)) == 25) || (unchecked((int)(slot.mapper)) == 26))) {
            mapper_16k_w(1, slot.fcr[(int)(1)]);
            mapper_16k_w(2, slot.fcr[(int)(2)]);
            mapper_16k_w(3, slot.fcr[(int)(3)]);
            } else {
            mapper_16k_w(0, slot.fcr[(int)(0)]);
            mapper_16k_w(1, slot.fcr[(int)(1)]);
            mapper_16k_w(2, slot.fcr[(int)(2)]);
            mapper_16k_w(3, slot.fcr[(int)(3)]);
            }
            }
            }
            switch (slot.mapper) {
            case 16:
            case 17:
            z80_readmem = read_mapper_default;
            z80_writemem = write_mapper_sega;
            break;
            case 19:
            z80_readmem = read_mapper_default;
            z80_writemem = write_mapper_codies;
            break;
            case 32:
            z80_readmem = read_mapper_korea_8k;
            z80_writemem = write_mapper_korea_8k;
            break;
            case 21:
            z80_readmem = read_mapper_default;
            z80_writemem = write_mapper_korea_16k_v1;
            break;
            case 22:
            z80_readmem = read_mapper_default;
            z80_writemem = write_mapper_korea_16k_v2;
            break;
            case 33:
            case 34:
            z80_readmem = read_mapper_default;
            z80_writemem = write_mapper_msx_8k;
            break;
            case 29:
            z80_readmem = read_mapper_default;
            z80_writemem = write_mapper_msx_16k;
            break;
            case 20:
            z80_readmem = read_mapper_default;
            z80_writemem = write_mapper_multi_16k;
            break;
            case 23:
            z80_readmem = read_mapper_default;
            z80_writemem = write_mapper_multi_2x16k_v1;
            break;
            case 24:
            z80_readmem = read_mapper_default;
            z80_writemem = write_mapper_multi_2x16k_v2;
            break;
            case 25:
            z80_readmem = read_mapper_default;
            z80_writemem = write_mapper_multi_16k_32k_v1;
            break;
            case 26:
            z80_readmem = read_mapper_default;
            z80_writemem = write_mapper_multi_16k_32k_v2;
            break;
            case 64:
            z80_readmem = read_mapper_default;
            z80_writemem = write_mapper_multi_32k;
            break;
            case 65:
            z80_readmem = read_mapper_default;
            z80_writemem = write_mapper_multi_32k_16k;
            break;
            case 66:
            z80_readmem = read_mapper_default;
            z80_writemem = write_mapper_hicom;
            break;
            case 35:
            z80_readmem = read_mapper_default;
            z80_writemem = write_mapper_multi_8k;
            break;
            case 36:
            z80_readmem = read_mapper_default;
            z80_writemem = write_mapper_multi_4x8k;
            break;
            case 37:
            z80_readmem = read_mapper_default;
            z80_writemem = write_mapper_zemina_4x8k;
            break;
            case 27:
            z80_readmem = read_mapper_default;
            z80_writemem = write_mapper_zemina_16k_32k;
            break;
            case 28:
            z80_readmem = read_mapper_default;
            z80_writemem = write_mapper_hwasung;
            break;
            case 18:
            z80_readmem = read_mapper_93c46;
            z80_writemem = write_mapper_93c46;
            break;
            case 1:
            z80_readmem = read_mapper_terebi;
            z80_writemem = write_mapper_terebi;
            break;
            default:
            z80_readmem = read_mapper_default;
            z80_writemem = write_mapper_none;
            break;
            }
        }
    }
    private void mapper_8k_w(int offset, byte data)
    {
        unchecked
        {
            int i = default;
            ArrayPointer<byte> page = (slot.rom + (int)(((unchecked((int)(data)) % unchecked((int)(slot.pages))) << 13)));
            slot.fcr[(int)(offset)] = data;
            switch ((offset & 3)) {
            case 0:
            {
            for (i = 32; (i < 40); i++) {
            z80_readmap[(int)(i)] = (page + (int)(((i & 7) << 10)));
            }
            if ((unchecked((int)(slot.mapper)) == 37)) {
            if (((unchecked((int)(data)) & 128) != 0)) {
            for (i = 0; (i < 8); i++) {
            z80_readmap[(int)(i)] = z80_readmap[(int)((32 + i))];
            }
            } else {
            for (i = 0; (i < 8); i++) {
            z80_readmap[(int)(i)] = ((slot.rom + (int)(((60 % unchecked((int)(slot.pages))) << 13))) + ((i & 7) << 10));
            }
            }
            }
            break;
            }
            case 1:
            {
            for (i = 40; (i < 48); i++) {
            z80_readmap[(int)(i)] = (page + (int)(((i & 7) << 10)));
            }
            if ((unchecked((int)(slot.mapper)) == 35)) {
            for (i = 8; (i < 16); i++) {
            z80_readmap[(int)(i)] = z80_readmap[(int)((32 + i))];
            }
            } else if ((unchecked((int)(slot.mapper)) == 37)) {
            if (((unchecked((int)(data)) & 128) != 0)) {
            for (i = 8; (i < 16); i++) {
            z80_readmap[(int)(i)] = z80_readmap[(int)((32 + i))];
            }
            } else {
            for (i = 8; (i < 16); i++) {
            z80_readmap[(int)(i)] = ((slot.rom + (int)(((60 % unchecked((int)(slot.pages))) << 13))) + ((i & 7) << 10));
            }
            }
            }
            break;
            }
            case 2:
            {
            for (i = 16; (i < 24); i++) {
            z80_readmap[(int)(i)] = (page + (int)(((i & 7) << 10)));
            }
            break;
            }
            case 3:
            {
            for (i = 24; (i < 32); i++) {
            z80_readmap[(int)(i)] = (page + (int)(((i & 7) << 10)));
            }
            break;
            }
            }
        }
    }
    private void mapper_16k_w(int offset, byte data)
    {
        unchecked
        {
            int i = default;
            byte page = unchecked((byte)((unchecked((int)(data)) % unchecked((int)(slot.pages)))));
            if (((unchecked((int)(slot.mapper)) == 16) && ((unchecked((int)(slot.fcr[(int)(0)])) & 3) != 0))) {
            page = unchecked((byte)(((unchecked((int)(page)) + ((4 - (unchecked((int)(slot.fcr[(int)(0)])) & 3)) << 3)) % unchecked((int)(slot.pages)))));
            }
            slot.fcr[(int)(offset)] = data;
            switch (offset) {
            case 0:
            {
            if (((unchecked((int)(data)) & 8) != 0)) {
            for (i = 32; (i < 48); i++) {
            z80_readmap[(int)(i)] = (z80_writemap[(int)(i)] = new ArrayPointer<byte>(sram.sram, (int)((((unchecked((int)(data)) & 4) << 12) + ((i & 15) << 10)))));
            }
            } else {
            page = unchecked((byte)((unchecked((int)(slot.fcr[(int)(3)])) % unchecked((int)(slot.pages)))));
            if ((((unchecked((int)(data)) & 3) != 0) && (unchecked((int)(slot.mapper)) == 16))) {
            page = unchecked((byte)(((unchecked((int)(page)) + ((4 - (unchecked((int)(data)) & 3)) << 3)) % unchecked((int)(slot.pages)))));
            }
            for (i = 32; (i < 48); i++) {
            z80_readmap[(int)(i)] = (slot.rom + (int)(((unchecked((int)(page)) << 14) | ((i & 15) << 10))));
            z80_writemap[(int)(i)] = (new ArrayPointer<byte>(cart.rom) + 5308416);
            }
            }
            if (((unchecked((int)(data)) & 16) != 0)) {
            for (i = 48; (i < 64); i++) {
            z80_readmap[(int)(i)] = (z80_writemap[(int)(i)] = new ArrayPointer<byte>(sram.sram, (int)(((i & 15) << 10))));
            }
            } else {
            for (i = 48; (i < 64); i++) {
            z80_readmap[(int)(i)] = (z80_writemap[(int)(i)] = new ArrayPointer<byte>(work_ram, (int)(((i & 7) << 10))));
            }
            }
            break;
            }
            case 1:
            {
            if ((unchecked((int)(slot.mapper)) >= 19)) {
            z80_readmap[(int)(0)] = (slot.rom + (int)((unchecked((int)(page)) << 14)));
            }
            for (i = 1; (i < 16); i++) {
            z80_readmap[(int)(i)] = (slot.rom + (int)(((unchecked((int)(page)) << 14) | ((i & 15) << 10))));
            }
            break;
            }
            case 2:
            {
            for (i = 16; (i < 32); i++) {
            z80_readmap[(int)(i)] = (slot.rom + (int)(((unchecked((int)(page)) << 14) | ((i & 15) << 10))));
            }
            if ((unchecked((int)(slot.mapper)) == 19)) {
            if (((unchecked((int)(data)) & 128) != 0)) {
            for (i = 40; (i < 48); i++) {
            z80_readmap[(int)(i)] = (z80_writemap[(int)(i)] = new ArrayPointer<byte>(sram.sram, (int)(((i & 15) << 10))));
            }
            } else {
            page = unchecked((byte)((unchecked((int)(slot.fcr[(int)(3)])) % unchecked((int)(slot.pages)))));
            for (i = 40; (i < 48); i++) {
            z80_readmap[(int)(i)] = (slot.rom + (int)(((unchecked((int)(page)) << 14) | ((i & 15) << 10))));
            z80_writemap[(int)(i)] = (new ArrayPointer<byte>(cart.rom) + 5308416);
            }
            }
            } else if ((unchecked((int)(slot.mapper)) == 23)) {
            if ((unchecked((int)(slot.fcr[(int)(0)])) != 1)) {
            for (i = 32; (i < 48); i++) {
            z80_readmap[(int)(i)] = (new ArrayPointer<byte>(cart.rom) + 5309440);
            }
            } else {
            for (i = 32; (i < 40); i++) {
            z80_readmap[(int)(i)] = z80_readmap[(int)((i - 8))];
            }
            for (i = 40; (i < 48); i++) {
            z80_readmap[(int)(i)] = z80_readmap[(int)((i - 24))];
            }
            }
            } else if ((unchecked((int)(slot.mapper)) == 24)) {
            if ((unchecked((int)(slot.fcr[(int)(0)])) != 3)) {
            for (i = 32; (i < 48); i++) {
            z80_readmap[(int)(i)] = (new ArrayPointer<byte>(cart.rom) + 5309440);
            }
            } else {
            for (i = 32; (i < 40); i++) {
            z80_readmap[(int)(i)] = z80_readmap[(int)((i - 8))];
            }
            for (i = 40; (i < 48); i++) {
            z80_readmap[(int)(i)] = z80_readmap[(int)((i - 24))];
            }
            }
            }
            break;
            }
            case 3:
            {
            if ((((unchecked((int)(slot.mapper)) == 16) || (unchecked((int)(slot.mapper)) == 17)) && ((unchecked((int)(slot.fcr[(int)(0)])) & 8) != 0))) break;
            for (i = 32; (i < 40); i++) {
            z80_readmap[(int)(i)] = (slot.rom + (int)(((unchecked((int)(page)) << 14) | ((i & 15) << 10))));
            }
            if (((unchecked((int)(slot.mapper)) == 19) && ((unchecked((int)(slot.fcr[(int)(2)])) & 128) != 0))) break;
            for (i = 40; (i < 48); i++) {
            z80_readmap[(int)(i)] = (slot.rom + (int)(((unchecked((int)(page)) << 14) | ((i & 15) << 10))));
            }
            break;
            }
            }
        }
    }
    private void mapper_32k_w(byte data)
    {
        unchecked
        {
            int i = default;
            ArrayPointer<byte> page = (slot.rom + (int)(((unchecked((int)(data)) % unchecked((int)(slot.pages))) << 15)));
            slot.fcr[(int)(0)] = data;
            if ((unchecked((int)(slot.mapper)) == 65)) {
            switch ((unchecked((int)(slot.fcr[(int)(1)])) & 15)) {
            case 0:
            {
            for (i = 0; (i < 32); i++) {
            z80_readmap[(int)(i)] = (page + (int)(((i & 15) << 10)));
            }
            for (i = 32; (i < 48); i++) {
            z80_readmap[(int)(i)] = (slot.rom + (int)((2088960 + ((i & 7) << 10))));
            }
            break;
            }
            case 1:
            {
            for (i = 0; (i < 32); i++) {
            z80_readmap[(int)(i)] = (page + (int)(((16 + (i & 15)) << 10)));
            }
            for (i = 32; (i < 48); i++) {
            z80_readmap[(int)(i)] = (slot.rom + (int)((2088960 + ((i & 7) << 10))));
            }
            break;
            }
            case 2:
            case 3:
            {
            for (i = 0; (i < 32); i++) {
            z80_readmap[(int)(i)] = (page + (int)((i << 10)));
            }
            for (i = 32; (i < 48); i++) {
            z80_readmap[(int)(i)] = (slot.rom + (int)((2088960 + ((i & 7) << 10))));
            }
            break;
            }
            case 4:
            {
            for (i = 0; (i < 16); i++) {
            z80_readmap[(int)(i)] = (slot.rom + (int)((1048576 + ((i & 15) << 10))));
            }
            for (i = 16; (i < 32); i++) {
            z80_readmap[(int)(i)] = (page + (int)(((i & 15) << 10)));
            }
            for (i = 32; (i < 48); i++) {
            z80_readmap[(int)(i)] = (slot.rom + (int)((2088960 + ((i & 7) << 10))));
            }
            break;
            }
            case 5:
            {
            for (i = 0; (i < 16); i++) {
            z80_readmap[(int)(i)] = (slot.rom + (int)((1048576 + ((i & 15) << 10))));
            }
            for (i = 16; (i < 32); i++) {
            z80_readmap[(int)(i)] = (page + (int)(((16 + (i & 15)) << 10)));
            }
            for (i = 32; (i < 48); i++) {
            z80_readmap[(int)(i)] = (slot.rom + (int)((2088960 + ((i & 7) << 10))));
            }
            break;
            }
            case 6:
            {
            for (i = 0; (i < 16); i++) {
            z80_readmap[(int)(i)] = (slot.rom + (int)((1048576 + ((i & 15) << 10))));
            }
            for (i = 16; (i < 32); i++) {
            z80_readmap[(int)(i)] = (slot.rom + (int)((2080768 + ((i & 15) << 10))));
            }
            for (i = 32; (i < 48); i++) {
            z80_readmap[(int)(i)] = (page + (int)(((i & 15) << 10)));
            }
            break;
            }
            case 7:
            {
            for (i = 0; (i < 16); i++) {
            z80_readmap[(int)(i)] = (slot.rom + (int)((1048576 + ((i & 15) << 10))));
            }
            for (i = 16; (i < 32); i++) {
            z80_readmap[(int)(i)] = (slot.rom + (int)((2080768 + ((i & 15) << 10))));
            }
            for (i = 32; (i < 48); i++) {
            z80_readmap[(int)(i)] = (page + (int)(((16 + (i & 15)) << 10)));
            }
            break;
            }
            case 8:
            {
            for (i = 0; (i < 16); i++) {
            z80_readmap[(int)(i)] = (slot.rom + (int)((1048576 + ((i & 15) << 10))));
            }
            for (i = 16; (i < 32); i++) {
            z80_readmap[(int)(i)] = (page + (int)(((i & 15) << 10)));
            }
            for (i = 32; (i < 48); i++) {
            z80_readmap[(int)(i)] = z80_readmap[(int)(((i ^ 8) - 16))];
            }
            break;
            }
            case 9:
            {
            for (i = 0; (i < 16); i++) {
            z80_readmap[(int)(i)] = (slot.rom + (int)((1048576 + ((i & 15) << 10))));
            }
            for (i = 16; (i < 32); i++) {
            z80_readmap[(int)(i)] = (page + (int)(((16 + (i & 15)) << 10)));
            }
            for (i = 32; (i < 48); i++) {
            z80_readmap[(int)(i)] = z80_readmap[(int)(((i ^ 8) - 16))];
            }
            break;
            }
            case 10:
            {
            for (i = 0; (i < 16); i++) {
            z80_readmap[(int)(i)] = (slot.rom + (int)((1048576 + ((i & 15) << 10))));
            }
            for (i = 16; (i < 32); i++) {
            z80_readmap[(int)(i)] = (page + (int)(((i & 15) << 10)));
            }
            for (i = 32; (i < 48); i++) {
            z80_readmap[(int)(i)] = (page + (int)(((16 + (i & 15)) << 10)));
            }
            break;
            }
            case 11:
            {
            for (i = 0; (i < 16); i++) {
            z80_readmap[(int)(i)] = (slot.rom + (int)((1048576 + ((i & 15) << 10))));
            }
            for (i = 16; (i < 32); i++) {
            z80_readmap[(int)(i)] = (page + (int)(((16 + (i & 15)) << 10)));
            }
            for (i = 32; (i < 48); i++) {
            z80_readmap[(int)(i)] = (page + (int)(((i & 15) << 10)));
            }
            break;
            }
            default:
            {
            for (i = 0; (i < 48); i++) {
            z80_readmap[(int)(i)] = (slot.rom + (int)((2088960 + ((i & 7) << 10))));
            }
            break;
            }
            }
            } else {
            for (i = 0; (i < 32); i++) {
            z80_readmap[(int)(i)] = (page + (int)((i << 10)));
            }
            for (i = 32; (i < 48); i++) {
            z80_readmap[(int)(i)] = z80_readmap[(int)((i & 15))];
            }
            }
        }
    }
    private void write_mapper_none(uint address, byte data)
    {
        unchecked
        {
            z80_writemap[(int)((address >> 10))][(int)((address & unchecked((uint)(1023))))] = data;
        }
    }
    private void write_mapper_sega(uint address, byte data)
    {
        unchecked
        {
            if ((address >= unchecked((uint)(65532)))) {
            mapper_16k_w(unchecked((int)((address & unchecked((uint)(3))))), data);
            }
            z80_writemap[(int)((address >> 10))][(int)((address & unchecked((uint)(1023))))] = data;
        }
    }
    private void write_mapper_codies(uint address, byte data)
    {
        unchecked
        {
            if ((address == unchecked((uint)(0)))) {
            mapper_16k_w(1, data);
            return;
            }
            if ((address == unchecked((uint)(16384)))) {
            mapper_16k_w(2, data);
            return;
            }
            if ((address == unchecked((uint)(32768)))) {
            mapper_16k_w(3, data);
            return;
            }
            z80_writemap[(int)((address >> 10))][(int)((address & unchecked((uint)(1023))))] = data;
        }
    }
    private void write_mapper_multi_16k(uint address, byte data)
    {
        unchecked
        {
            if ((address == unchecked((uint)(16382)))) {
            mapper_16k_w(1, data);
            return;
            }
            if ((address == unchecked((uint)(32767)))) {
            mapper_16k_w(2, data);
            return;
            }
            if ((address == unchecked((uint)(49151)))) {
            mapper_16k_w(3, unchecked((byte)(((unchecked((int)(slot.fcr[(int)(1)])) & 48) + unchecked((int)(data))))));
            return;
            }
            z80_writemap[(int)((address >> 10))][(int)((address & unchecked((uint)(1023))))] = data;
        }
    }
    private void write_mapper_multi_2x16k_v1(uint address, byte data)
    {
        unchecked
        {
            if ((address == unchecked((uint)(65534)))) {
            slot.fcr[(int)(0)] = unchecked((byte)(((unchecked((int)(data)) >> 5) & 3)));
            if (((unchecked((int)(slot.fcr[(int)(0)])) & 2) != 0)) {
            data &= unchecked((byte)(30));
            mapper_16k_w(1, data);
            mapper_16k_w(2, unchecked((byte)((unchecked((int)(data)) + 1))));
            } else {
            data &= unchecked((byte)(31));
            mapper_16k_w(1, unchecked((byte)(0)));
            mapper_16k_w(2, data);
            }
            }
            z80_writemap[(int)((address >> 10))][(int)((address & unchecked((uint)(1023))))] = data;
        }
    }
    private void write_mapper_multi_2x16k_v2(uint address, byte data)
    {
        unchecked
        {
            if ((address == unchecked((uint)(49148)))) {
            slot.fcr[(int)(0)] = unchecked((byte)(((unchecked((int)(data)) >> 6) & 3)));
            switch (slot.fcr[(int)(0)]) {
            case 0:
            {
            data &= unchecked((byte)(62));
            mapper_16k_w(1, data);
            mapper_16k_w(2, unchecked((byte)((unchecked((int)(data)) + 1))));
            return;
            }
            case 1:
            {
            data &= unchecked((byte)(63));
            mapper_16k_w(1, data);
            mapper_16k_w(2, data);
            return;
            }
            default:
            {
            data &= unchecked((byte)(63));
            mapper_16k_w(1, unchecked((byte)(32)));
            mapper_16k_w(2, data);
            return;
            }
            }
            }
            z80_writemap[(int)((address >> 10))][(int)((address & unchecked((uint)(1023))))] = data;
        }
    }
    private void write_mapper_multi_16k_32k_v1(uint address, byte data)
    {
        unchecked
        {
            z80_writemap[(int)((address >> 10))][(int)((address & unchecked((uint)(1023))))] = data;
            address &= unchecked((uint)(49135));
            if ((address == unchecked((uint)(49125)))) {
            slot.fcr[(int)(0)] = unchecked((byte)((unchecked((int)(data)) << 1)));
            mapper_16k_w(1, slot.fcr[(int)(0)]);
            mapper_16k_w(2, unchecked((byte)((unchecked((int)(slot.fcr[(int)(0)])) + 1))));
            mapper_16k_w(3, unchecked((byte)((unchecked((int)(slot.fcr[(int)(0)])) + 1))));
            } else if ((address == unchecked((uint)(49134)))) {
            data &= unchecked((byte)(((unchecked((int)(slot.fcr[(int)(0)])) >= 32) ? 31 : 15)));
            mapper_16k_w(2, unchecked((byte)((unchecked((int)(slot.fcr[(int)(0)])) + unchecked((int)(data))))));
            } else if ((address == unchecked((uint)(49135)))) {
            data &= unchecked((byte)(((unchecked((int)(slot.fcr[(int)(0)])) >= 32) ? 31 : 15)));
            mapper_16k_w(3, unchecked((byte)((unchecked((int)(slot.fcr[(int)(0)])) + unchecked((int)(data))))));
            }
        }
    }
    private void write_mapper_multi_16k_32k_v2(uint address, byte data)
    {
        unchecked
        {
            z80_writemap[(int)((address >> 10))][(int)((address & unchecked((uint)(1023))))] = data;
            if ((address == unchecked((uint)(65520)))) {
            slot.fcr[(int)(0)] = unchecked((byte)((unchecked((int)(data)) << 1)));
            mapper_16k_w(1, slot.fcr[(int)(0)]);
            mapper_16k_w(2, unchecked((byte)((unchecked((int)(slot.fcr[(int)(0)])) + 1))));
            mapper_16k_w(3, slot.fcr[(int)(0)]);
            } else if ((address == unchecked((uint)(65534)))) {
            data &= unchecked((byte)(15));
            mapper_16k_w(2, unchecked((byte)((unchecked((int)(slot.fcr[(int)(0)])) + unchecked((int)(data))))));
            } else if ((address == unchecked((uint)(65535)))) {
            data &= unchecked((byte)(15));
            mapper_16k_w(3, unchecked((byte)((unchecked((int)(slot.fcr[(int)(0)])) + unchecked((int)(data))))));
            }
        }
    }
    private void write_mapper_multi_32k(uint address, byte data)
    {
        unchecked
        {
            if ((address == unchecked((uint)(8192)))) {
            mapper_32k_w(data);
            return;
            }
            z80_writemap[(int)((address >> 10))][(int)((address & unchecked((uint)(1023))))] = data;
        }
    }
    private void write_mapper_multi_32k_16k(uint address, byte data)
    {
        unchecked
        {
            if ((address == unchecked((uint)(65523)))) {
            slot.fcr[(int)(1)] = unchecked((byte)(((unchecked((int)(slot.fcr[(int)(1)])) & 14) | (unchecked((int)(data)) & 1))));
            data = unchecked((byte)(((unchecked((int)(slot.fcr[(int)(0)])) & 32) | ((unchecked((int)(data)) & 62) >> 1))));
            mapper_32k_w(data);
            } else if ((address == unchecked((uint)(65532)))) {
            slot.fcr[(int)(1)] = unchecked((byte)((((unchecked((int)(data)) & 224) >> 4) | (unchecked((int)(slot.fcr[(int)(1)])) & 1))));
            data = unchecked((byte)((((unchecked((int)(data)) & 16) << 1) | (unchecked((int)(slot.fcr[(int)(0)])) & 31))));
            mapper_32k_w(data);
            }
            z80_writemap[(int)((address >> 10))][(int)((address & unchecked((uint)(1023))))] = data;
        }
    }
    private void write_mapper_hicom(uint address, byte data)
    {
        unchecked
        {
            if ((address == unchecked((uint)(65535)))) {
            mapper_32k_w(data);
            }
            z80_writemap[(int)((address >> 10))][(int)((address & unchecked((uint)(1023))))] = data;
        }
    }
    private void write_mapper_multi_8k(uint address, byte data)
    {
        unchecked
        {
            z80_writemap[(int)((address >> 10))][(int)((address & unchecked((uint)(1023))))] = data;
            address &= unchecked((uint)(65280));
            if ((address == unchecked((uint)(0)))) {
            mapper_8k_w(0, data);
            return;
            }
            if ((address == unchecked((uint)(256)))) {
            mapper_8k_w(2, data);
            return;
            }
            if ((address == unchecked((uint)(512)))) {
            mapper_8k_w(1, data);
            return;
            }
            if ((address == unchecked((uint)(768)))) {
            mapper_8k_w(3, data);
            return;
            }
        }
    }
    private void write_mapper_multi_4x8k(uint address, byte data)
    {
        unchecked
        {
            if ((address == unchecked((uint)(8192)))) {
            mapper_8k_w(2, unchecked((byte)((unchecked((int)(data)) ^ 31))));
            mapper_8k_w(3, unchecked((byte)((unchecked((int)(data)) ^ 30))));
            mapper_8k_w(0, unchecked((byte)((unchecked((int)(data)) ^ 29))));
            mapper_8k_w(1, unchecked((byte)((unchecked((int)(data)) ^ 28))));
            return;
            }
            z80_writemap[(int)((address >> 10))][(int)((address & unchecked((uint)(1023))))] = data;
        }
    }
    private void write_mapper_zemina_4x8k(uint address, byte data)
    {
        unchecked
        {
            if ((address == unchecked((uint)(32768)))) {
            if ((unchecked((int)(slot.fcr[(int)(3)])) == 255)) data ^= unchecked((byte)(34));
            mapper_8k_w(2, unchecked((byte)((unchecked((int)(data)) ^ 1))));
            mapper_8k_w(3, data);
            mapper_8k_w(0, unchecked((byte)((unchecked((int)(data)) ^ 3))));
            mapper_8k_w(1, unchecked((byte)((unchecked((int)(data)) ^ 2))));
            return;
            }
            z80_writemap[(int)((address >> 10))][(int)((address & unchecked((uint)(1023))))] = data;
        }
    }
    private void write_mapper_zemina_16k_32k(uint address, byte data)
    {
        unchecked
        {
            if ((address == unchecked((uint)(0)))) {
            data = unchecked((byte)((((unchecked((int)(data)) ^ 240) & 240) >> 3)));
            if ((unchecked((int)(data)) == 0)) {
            mapper_16k_w(2, unchecked((byte)(1)));
            mapper_16k_w(3, unchecked((byte)(1)));
            } else {
            mapper_16k_w(2, data);
            mapper_16k_w(3, unchecked((byte)((unchecked((int)(data)) + 1))));
            }
            return;
            }
            z80_writemap[(int)((address >> 10))][(int)((address & unchecked((uint)(1023))))] = data;
        }
    }
    private void write_mapper_hwasung(uint address, byte data)
    {
        unchecked
        {
            if ((address == unchecked((uint)(8192)))) {
            if (((unchecked((int)(data)) & 1) != 0)) {
            mapper_16k_w(2, unchecked((byte)(3)));
            mapper_16k_w(3, unchecked((byte)(4)));
            } else {
            mapper_16k_w(2, unchecked((byte)(1)));
            mapper_16k_w(3, unchecked((byte)(2)));
            }
            return;
            }
            z80_writemap[(int)((address >> 10))][(int)((address & unchecked((uint)(1023))))] = data;
        }
    }
    private void write_mapper_msx_8k(uint address, byte data)
    {
        unchecked
        {
            if ((address <= unchecked((uint)(3)))) {
            mapper_8k_w(unchecked((int)(address)), data);
            return;
            }
            z80_writemap[(int)((address >> 10))][(int)((address & unchecked((uint)(1023))))] = data;
        }
    }
    private void write_mapper_msx_16k(uint address, byte data)
    {
        unchecked
        {
            if (((address & unchecked((uint)(49152))) == unchecked((uint)(32768)))) {
            mapper_16k_w(3, data);
            return;
            }
            z80_writemap[(int)((address >> 10))][(int)((address & unchecked((uint)(1023))))] = data;
        }
    }
    private void write_mapper_korea_8k(uint address, byte data)
    {
        unchecked
        {
            if ((address == unchecked((uint)(16384)))) {
            mapper_8k_w(2, data);
            return;
            }
            if ((address == unchecked((uint)(24576)))) {
            mapper_8k_w(3, data);
            return;
            }
            if ((address == unchecked((uint)(32768)))) {
            mapper_8k_w(0, data);
            return;
            }
            if ((address == unchecked((uint)(40960)))) {
            mapper_8k_w(1, data);
            return;
            }
            if ((address == unchecked((uint)(65534)))) {
            mapper_8k_w(2, unchecked((byte)(((unchecked((int)(data)) << 1) & 255))));
            mapper_8k_w(3, unchecked((byte)(((1 + (unchecked((int)(data)) << 1)) & 255))));
            } else if ((address == unchecked((uint)(65535)))) {
            mapper_8k_w(0, unchecked((byte)(((unchecked((int)(data)) << 1) & 255))));
            mapper_8k_w(1, unchecked((byte)(((1 + (unchecked((int)(data)) << 1)) & 255))));
            }
            z80_writemap[(int)((address >> 10))][(int)((address & unchecked((uint)(1023))))] = data;
        }
    }
    private void write_mapper_korea_16k_v1(uint address, byte data)
    {
        unchecked
        {
            if ((address == unchecked((uint)(40960)))) {
            mapper_16k_w(3, data);
            return;
            }
            z80_writemap[(int)((address >> 10))][(int)((address & unchecked((uint)(1023))))] = data;
        }
    }
    private void write_mapper_korea_16k_v2(uint address, byte data)
    {
        unchecked
        {
            if ((address == unchecked((uint)(16384)))) {
            mapper_16k_w(2, data);
            return;
            }
            if ((address == unchecked((uint)(32768)))) {
            mapper_16k_w(3, data);
            return;
            }
            if ((address >= unchecked((uint)(65532)))) {
            mapper_16k_w(unchecked((int)((address & unchecked((uint)(3))))), data);
            }
            z80_writemap[(int)((address >> 10))][(int)((address & unchecked((uint)(1023))))] = data;
        }
    }
    private void write_mapper_93c46(uint address, byte data)
    {
        unchecked
        {
            if (((address == unchecked((uint)(32768))) && (eeprom_enabled != 0))) {
            eeprom_93c_write(data);
            return;
            }
            if ((address == unchecked((uint)(65532)))) {
            eeprom_enabled = unchecked((byte)((unchecked((int)(data)) & 8)));
            if (((unchecked((int)(data)) & 128) != 0)) {
            eeprom_93c_init();
            }
            }
            if ((address > unchecked((uint)(65532)))) {
            mapper_16k_w(unchecked((int)((address & unchecked((uint)(3))))), data);
            }
            z80_writemap[(int)((address >> 10))][(int)((address & unchecked((uint)(1023))))] = data;
        }
    }
    private void write_mapper_terebi(uint address, byte data)
    {
        unchecked
        {
            if ((address == unchecked((uint)(24576)))) {
            terebi_oekaki_write(data);
            return;
            }
            z80_writemap[(int)((address >> 10))][(int)((address & unchecked((uint)(1023))))] = data;
        }
    }
    private byte read_mapper_93c46(uint address)
    {
        unchecked
        {
            if (((address == unchecked((uint)(32768))) && (eeprom_enabled != 0))) {
            return eeprom_93c_read();
            }
            return z80_readmap[(int)((address >> 10))][(int)((address & unchecked((uint)(1023))))];
        }
    }
    private byte read_mapper_terebi(uint address)
    {
        unchecked
        {
            if ((address == unchecked((uint)(32768)))) {
            return unchecked((byte)((unchecked((int)(terebi_oekaki_read())) >> 8)));
            }
            if ((address == unchecked((uint)(40960)))) {
            return unchecked((byte)((unchecked((int)(terebi_oekaki_read())) & 255)));
            }
            return z80_readmap[(int)((address >> 10))][(int)((address & unchecked((uint)(1023))))];
        }
    }
    private byte read_mapper_korea_8k(uint address)
    {
        unchecked
        {
            byte data = z80_readmap[(int)((address >> 10))][(int)((address & unchecked((uint)(1023))))];
            byte page = unchecked((byte)((address >> 14)));
            if ((((unchecked((int)(page)) == 1) && ((unchecked((int)(slot.fcr[(int)(2)])) & 128) != 0)) || ((unchecked((int)(page)) == 2) && ((unchecked((int)(slot.fcr[(int)(0)])) & 128) != 0)))) {
            data = unchecked((byte)((((((((((unchecked((int)(data)) >> 7) & 1) | ((unchecked((int)(data)) >> 5) & 2)) | ((unchecked((int)(data)) >> 3) & 4)) | ((unchecked((int)(data)) >> 1) & 8)) | ((unchecked((int)(data)) << 1) & 16)) | ((unchecked((int)(data)) << 3) & 32)) | ((unchecked((int)(data)) << 5) & 64)) | ((unchecked((int)(data)) << 7) & 128))));
            }
            return data;
        }
    }
    private byte read_mapper_default(uint address)
    {
        unchecked
        {
            return z80_readmap[(int)((address >> 10))][(int)((address & unchecked((uint)(1023))))];
        }
    }
    private byte read_mapper_none(uint address)
    {
        unchecked
        {
            if ((address >= unchecked((uint)(49152)))) {
            return z80_readmap[(int)((address >> 10))][(int)((address & unchecked((uint)(1023))))];
            }
            return z80_last_fetch;
        }
    }
}
