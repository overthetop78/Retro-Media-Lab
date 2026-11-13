using System.Buffers.Binary;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Video;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cartridge;

internal sealed partial class Eeprom93c
{
    private void ClearState()
    {
        eeprom_93c.enabled = 0;
        eeprom_93c.cs = 0;
        eeprom_93c.clk = 0;
        eeprom_93c.data = 0;
        eeprom_93c.cycles = 0;
        eeprom_93c.we = 0;
        eeprom_93c.opcode = 0;
        eeprom_93c.buffer = 0;
        eeprom_93c.state = 0;
    }
}
