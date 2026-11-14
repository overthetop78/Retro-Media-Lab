using System.Buffers.Binary;
using RetroMediaLab.Chips.Common.Memory;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Cartridge;

internal sealed partial class BackupRam
{
    private void sram_init()
    {
        unchecked
        {
            backup.detected = (backup.on = unchecked((byte)((backup.start = (backup.end = unchecked((uint)(0)))))));
            backup.type = unchecked((byte)(0));
            if ((strstr(rominfo.international, "Sonic 1 Remastered") != 0)) {
            FillRam(0);
            } else {
            FillRam(255);
            }
            backup.crc = crc32(unchecked((uint)(0)), new ArrayPointer<byte>(backup.sram), unchecked((uint)(65536)));
            if (((unchecked((int)(cart.rom[(int)((432 ^ 1))])) == 82) && (unchecked((int)(cart.rom[(int)((433 ^ 1))])) == 65))) {
            backup.detected = unchecked((byte)(1));
            backup.on = unchecked((byte)(1));
            backup.start = unchecked((uint)((((((unchecked((int)(cart.rom[(int)((436 + 1))])) << 24) | (unchecked((int)(cart.rom[(int)(436)])) << 16)) | (unchecked((int)(cart.rom[(int)((436 + 3))])) << 8)) | unchecked((int)(cart.rom[(int)((436 + 2))]))) & 16777215)));
            backup.end = unchecked((uint)((((((unchecked((int)(cart.rom[(int)((440 + 1))])) << 24) | (unchecked((int)(cart.rom[(int)(440)])) << 16)) | (unchecked((int)(cart.rom[(int)((440 + 3))])) << 8)) | unchecked((int)(cart.rom[(int)((440 + 2))]))) & 16777215)));
            if ((strstr(rominfo.product, "T-26013") != 0)) {
            backup.start = unchecked((uint)(2097153));
            backup.end = unchecked((uint)(2113535));
            } else if (((strstr(rominfo.product, "00000000-00") != 0) && ((((unchecked((int)(rominfo.checksum)) == 52725) && (unchecked((int)(rominfo.realchecksum)) == 24634)) || ((unchecked((int)(rominfo.checksum)) == 27605) && (unchecked((int)(rominfo.realchecksum)) == 8170))) || ((unchecked((int)(rominfo.checksum)) == 17857) && (unchecked((int)(rominfo.realchecksum)) == 50707))))) {
            backup.start = unchecked((uint)(4128768));
            backup.end = unchecked((uint)(4194303));
            backup.type = unchecked((byte)((4 + 1)));
            } else if ((backup.start == unchecked((uint)(16711680)))) {
            backup.on = unchecked((byte)(0));
            } else if ((backup.start >= unchecked((uint)(8388608)))) {
            backup.start = unchecked((uint)(2097152));
            backup.end = unchecked((uint)(2162687));
            } else if (((backup.start > backup.end) || ((backup.end - backup.start) >= unchecked((uint)(65536))))) {
            backup.end = (backup.start + unchecked((uint)(65535)));
            }
            } else if (((unchecked((int)(cart.rom[(int)((432 ^ 1))])) == 70) && (unchecked((int)(cart.rom[(int)((433 ^ 1))])) == 76))) {
            backup.type = unchecked((byte)((4 + 1)));
            backup.detected = unchecked((byte)(1));
            backup.on = unchecked((byte)(1));
            backup.start = unchecked((uint)((((((unchecked((int)(cart.rom[(int)((436 + 1))])) << 24) | (unchecked((int)(cart.rom[(int)(436)])) << 16)) | (unchecked((int)(cart.rom[(int)((436 + 3))])) << 8)) | unchecked((int)(cart.rom[(int)((436 + 2))]))) & 16777215)));
            backup.end = unchecked((uint)((((((unchecked((int)(cart.rom[(int)((440 + 1))])) << 24) | (unchecked((int)(cart.rom[(int)(440)])) << 16)) | (unchecked((int)(cart.rom[(int)((440 + 3))])) << 8)) | unchecked((int)(cart.rom[(int)((440 + 2))]))) & 16777215)));
            } else {
            if ((strstr(rominfo.product, "T-50086") != 0)) {
            backup.on = unchecked((byte)(1));
            backup.start = unchecked((uint)(2097153));
            backup.end = unchecked((uint)(2113535));
            } else if ((strstr(rominfo.product, "ACLD007") != 0)) {
            backup.on = unchecked((byte)(1));
            backup.start = unchecked((uint)(2097153));
            backup.end = unchecked((uint)(2101247));
            } else if ((strstr(rominfo.product, "T-50286") != 0)) {
            backup.on = unchecked((byte)(1));
            backup.start = unchecked((uint)(2097153));
            backup.end = unchecked((uint)(2113535));
            } else if ((((unchecked((int)(rominfo.realchecksum)) == 44714) || (unchecked((int)(rominfo.realchecksum)) == 36282)) && (unchecked((int)(rominfo.checksum)) == 33028))) {
            backup.on = unchecked((byte)(1));
            backup.start = unchecked((uint)(4194305));
            backup.end = unchecked((uint)(4259839));
            } else if (((((unchecked((int)(rominfo.checksum)) == 0) && (unchecked((int)(rominfo.realchecksum)) == 8063)) && (unchecked((int)((new ArrayPointer<byte>(cart.rom) + 524288)[(int)((432 ^ 1))])) == 82)) && (unchecked((int)((new ArrayPointer<byte>(cart.rom) + 524288)[(int)((433 ^ 1))])) == 65))) {
            backup.on = unchecked((byte)(1));
            backup.start = unchecked((uint)(2097153));
            backup.end = unchecked((uint)(2113535));
            } else if (((strstr(rominfo.ROMType, "SF") != 0) && (strstr(rominfo.product, "001") != 0))) {
            backup.on = unchecked((byte)(1));
            if ((unchecked((int)(rominfo.checksum)) == 15880)) {
            backup.start = unchecked((uint)(3932161));
            backup.end = unchecked((uint)(3997695));
            } else {
            backup.start = unchecked((uint)(4194305));
            backup.end = unchecked((uint)(4259839));
            }
            } else if (((strstr(rominfo.ROMType, "SF") != 0) && (strstr(rominfo.product, "004") != 0))) {
            backup.on = unchecked((byte)(1));
            backup.start = unchecked((uint)(2097153));
            backup.end = unchecked((uint)(2113535));
            } else if ((strstr(rominfo.international, "SONIC & KNUCKLES") != 0)) {
            if ((cart.romsize == unchecked((uint)(4194304)))) {
            backup.on = unchecked((byte)(1));
            backup.start = unchecked((uint)(2097153));
            backup.end = unchecked((uint)(2113535));
            }
            } else if ((strstr(rominfo.international, "COLOCODX") != 0)) {
            backup.on = unchecked((byte)(1));
            backup.start = unchecked((uint)(8257536));
            backup.end = unchecked((uint)(8388607));
            backup.type = unchecked((byte)((4 + 1)));
            } else if ((strstr(rominfo.product, "T-113016") != 0)) {
            backup.on = unchecked((byte)(0));
            } else if ((strstr(rominfo.international, "SONIC THE HEDGEHOG 2") != 0)) {
            backup.on = unchecked((byte)(0));
            } else if ((cart.romsize <= unchecked((uint)(2097152)))) {
            backup.start = unchecked((uint)(2097152));
            backup.end = unchecked((uint)(2162687));
            backup.on = unchecked((byte)(1));
            }
            }
        }
    }
    private uint sram_read_byte(uint address)
    {
        unchecked
        {
            return unchecked((uint)(backup.sram[(int)((address & unchecked((uint)(65535))))]));
        }
    }
    private uint sram_read_word(uint address)
    {
        unchecked
        {
            return unchecked((uint)(((unchecked((int)(backup.sram[(int)((address & unchecked((uint)(65534))))])) << 8) | unchecked((int)(backup.sram[(int)(((address & unchecked((uint)(65534))) + unchecked((uint)(1))))])))));
        }
    }
    private void sram_write_byte(uint address, uint data)
    {
        unchecked
        {
            backup.sram[(int)((address & unchecked((uint)(65535))))] = unchecked((byte)(data));
        }
    }
    private void sram_write_word(uint address, uint data)
    {
        unchecked
        {
            backup.sram[(int)((address & unchecked((uint)(65534))))] = unchecked((byte)(((data >> 8) & unchecked((uint)(255)))));
            backup.sram[(int)(((address & unchecked((uint)(65534))) + unchecked((uint)(1))))] = unchecked((byte)((data & unchecked((uint)(255)))));
        }
    }
}
