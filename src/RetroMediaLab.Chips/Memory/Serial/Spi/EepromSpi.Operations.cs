using System.Buffers.Binary;
using RetroMediaLab.Chips.Common.Memory;

namespace RetroMediaLab.Chips.Memory.Serial.Spi;

public sealed partial class EepromSpi
{
    private void eeprom_spi_init()
    {
        unchecked
        {
            ClearState();
            spi_eeprom.sampleOutput = unchecked((byte)(1));
            spi_eeprom.state = 1;
        }
    }
    private void eeprom_spi_write(byte data)
    {
        unchecked
        {
            if (((unchecked((int)(data)) & (1 << 2)) != 0)) {
            if (((unchecked((int)(data)) & (1 << 3)) != 0)) {
            spi_eeprom.cycles = unchecked((uint)(0));
            spi_eeprom.sampleOutput = unchecked((byte)(1));
            spi_eeprom.opcode = unchecked((byte)(0));
            spi_eeprom.state = 1;
            } else {
            switch (spi_eeprom.state) {
            case 1:
            {
            if ((((unchecked((int)(data)) & (1 << 1)) != 0) && (!(spi_eeprom.clk != 0)))) {
            spi_eeprom.opcode |= unchecked((byte)(((unchecked((int)(data)) >> 0) & 1)));
            spi_eeprom.cycles++;
            if ((spi_eeprom.cycles == unchecked((uint)(8)))) {
            spi_eeprom.cycles = unchecked((uint)(0));
            switch (spi_eeprom.opcode) {
            case 1:
            {
            spi_eeprom.buffer = unchecked((byte)(0));
            spi_eeprom.state = 3;
            break;
            }
            case 2:
            {
            spi_eeprom.addr = unchecked((ushort)(0));
            spi_eeprom.state = 2;
            break;
            }
            case 3:
            {
            spi_eeprom.addr = unchecked((ushort)(0));
            spi_eeprom.state = 2;
            break;
            }
            case 4:
            {
            spi_eeprom.status &= unchecked((byte)((~2)));
            spi_eeprom.state = 0;
            break;
            }
            case 5:
            {
            spi_eeprom.buffer = spi_eeprom.status;
            spi_eeprom.state = 4;
            break;
            }
            case 6:
            {
            spi_eeprom.status |= unchecked((byte)(2));
            spi_eeprom.state = 0;
            break;
            }
            default:
            {
            spi_eeprom.state = 0;
            break;
            }
            }
            } else {
            spi_eeprom.opcode = unchecked((byte)((unchecked((int)(spi_eeprom.opcode)) << 1)));
            }
            }
            break;
            }
            case 2:
            {
            if ((((unchecked((int)(data)) & (1 << 1)) != 0) && (!(spi_eeprom.clk != 0)))) {
            spi_eeprom.addr |= unchecked((ushort)(((unchecked((int)(data)) >> 0) & 1)));
            spi_eeprom.cycles++;
            if ((spi_eeprom.cycles == unchecked((uint)(16)))) {
            spi_eeprom.cycles = unchecked((uint)(0));
            spi_eeprom.addr &= unchecked((ushort)(65535));
            if (((unchecked((int)(spi_eeprom.opcode)) & 1) != 0)) {
            spi_eeprom.buffer = _memory.Span[(int)(spi_eeprom.addr)];
            spi_eeprom.state = 4;
            } else {
            spi_eeprom.buffer = unchecked((byte)(0));
            spi_eeprom.state = 3;
            }
            } else {
            spi_eeprom.addr = unchecked((ushort)((unchecked((int)(spi_eeprom.addr)) << 1)));
            }
            }
            break;
            }
            case 3:
            {
            if ((((unchecked((int)(data)) & (1 << 1)) != 0) && (!(spi_eeprom.clk != 0)))) {
            spi_eeprom.buffer |= unchecked((byte)(((unchecked((int)(data)) >> 0) & 1)));
            spi_eeprom.cycles++;
            if ((spi_eeprom.cycles == unchecked((uint)(8)))) {
            spi_eeprom.cycles = unchecked((uint)(0));
            if (((unchecked((int)(spi_eeprom.opcode)) & 1) != 0)) {
            spi_eeprom.status = unchecked((byte)(((unchecked((int)(spi_eeprom.status)) & 2) | (unchecked((int)(spi_eeprom.buffer)) & 12))));
            spi_eeprom.state = 0;
            } else {
            if (((unchecked((int)(spi_eeprom.status)) & 2) != 0)) {
            switch (((unchecked((int)(spi_eeprom.status)) >> 2) & 3)) {
            case 1:
            {
            if ((unchecked((int)(spi_eeprom.addr)) < 49152)) {
            _memory.Span[(int)(spi_eeprom.addr)] = spi_eeprom.buffer;
            }
            break;
            }
            case 2:
            {
            if ((unchecked((int)(spi_eeprom.addr)) < 32768)) {
            _memory.Span[(int)(spi_eeprom.addr)] = spi_eeprom.buffer;
            }
            break;
            }
            case 3:
            {
            break;
            }
            default:
            {
            _memory.Span[(int)(spi_eeprom.addr)] = spi_eeprom.buffer;
            break;
            }
            }
            }
            spi_eeprom.buffer = unchecked((byte)(0));
            spi_eeprom.addr = unchecked((ushort)(((unchecked((int)(spi_eeprom.addr)) & (~127)) | ((unchecked((int)(spi_eeprom.addr)) + 1) & 127))));
            }
            } else {
            spi_eeprom.buffer = unchecked((byte)((unchecked((int)(spi_eeprom.buffer)) << 1)));
            }
            }
            break;
            }
            case 4:
            {
            if ((((unchecked((int)(data)) & (1 << 1)) != 0) && (!(spi_eeprom.clk != 0)))) {
            spi_eeprom.sampleOutput = unchecked((byte)(((unchecked((int)(spi_eeprom.buffer)) >> unchecked((int)((unchecked((uint)(7)) - spi_eeprom.cycles)))) & 1)));
            spi_eeprom.cycles++;
            if ((spi_eeprom.cycles == unchecked((uint)(8)))) {
            spi_eeprom.cycles = unchecked((uint)(0));
            if ((unchecked((int)(spi_eeprom.opcode)) == 3)) {
            spi_eeprom.addr = unchecked((ushort)(((unchecked((int)(spi_eeprom.addr)) + 1) & 65535)));
            spi_eeprom.buffer = _memory.Span[(int)(spi_eeprom.addr)];
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
            }
            }
            spi_eeprom.cs = unchecked((byte)(((unchecked((int)(data)) >> 3) & 1)));
            spi_eeprom.clk = unchecked((byte)(((unchecked((int)(data)) >> 1) & 1)));
        }
    }
    private uint eeprom_spi_read(uint address)
    {
        unchecked
        {
            return unchecked((uint)((unchecked((int)(spi_eeprom.sampleOutput)) << 0)));
        }
    }
}
