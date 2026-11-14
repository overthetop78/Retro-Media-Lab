using System.Buffers.Binary;
using RetroMediaLab.Chips.Common.Memory;

namespace RetroMediaLab.Chips.Memory.Serial.Spi;

public sealed partial class EepromSpi
{
    private void ClearState()
    {
        spi_eeprom.cs = 0;
        spi_eeprom.clk = 0;
        spi_eeprom.sampleOutput = 0;
        spi_eeprom.status = 0;
        spi_eeprom.opcode = 0;
        spi_eeprom.buffer = 0;
        spi_eeprom.addr = 0;
        spi_eeprom.cycles = 0;
        spi_eeprom.state = 0;
    }
}
