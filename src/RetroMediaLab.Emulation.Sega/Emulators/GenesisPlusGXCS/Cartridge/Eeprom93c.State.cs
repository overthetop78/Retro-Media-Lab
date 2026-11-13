using System.Buffers.Binary;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Video;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cartridge;

internal sealed partial class Eeprom93c
{
    private sealed class T_EEPROM_93C
    {
        public byte enabled;
        public byte cs;
        public byte clk;
        public byte data;
        public byte cycles;
        public byte we;
        public byte opcode;
        public ushort buffer;
        public int state;
    }
    private readonly T_EEPROM_93C eeprom_93c = new();
}
