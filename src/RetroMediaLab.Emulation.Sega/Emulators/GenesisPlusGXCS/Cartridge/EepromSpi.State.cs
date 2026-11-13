using System.Buffers.Binary;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Video;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cartridge;

internal sealed partial class EepromSpi
{
    private sealed class T_EEPROM_SPI
    {
        public byte cs;
        public byte clk;
        public byte sampleOutput;
        public byte status;
        public byte opcode;
        public byte buffer;
        public ushort addr;
        public uint cycles;
        public int state;
    }
    private readonly T_EEPROM_SPI spi_eeprom = new();
}
