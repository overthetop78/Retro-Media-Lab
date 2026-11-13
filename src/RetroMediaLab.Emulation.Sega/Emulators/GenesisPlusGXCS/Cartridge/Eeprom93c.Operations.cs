using System.Buffers.Binary;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Video;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cartridge;

internal sealed partial class Eeprom93c
{
    private void eeprom_93c_init()
    {
        unchecked
        {
            ClearState();
            eeprom_93c.data = unchecked((byte)(1));
            eeprom_93c.state = 1;
            sram.type = unchecked((byte)(3));
        }
    }
    private void eeprom_93c_write(byte data)
    {
        unchecked
        {
            if (((unchecked((int)(data)) & (1 << 2)) != 0)) {
            if ((((unchecked((int)(data)) & (1 << 1)) != 0) && (!(eeprom_93c.clk != 0)))) {
            switch (eeprom_93c.state) {
            case 1:
            {
            if (((unchecked((int)(data)) & (1 << 0)) != 0)) {
            eeprom_93c.opcode = unchecked((byte)(0));
            eeprom_93c.cycles = unchecked((byte)(0));
            eeprom_93c.state = 2;
            }
            break;
            }
            case 2:
            {
            eeprom_93c.opcode |= unchecked((byte)((((unchecked((int)(data)) >> 0) & 1) << (7 - unchecked((int)(eeprom_93c.cycles))))));
            eeprom_93c.cycles++;
            if ((unchecked((int)(eeprom_93c.cycles)) == 8)) {
            switch (((unchecked((int)(eeprom_93c.opcode)) >> 6) & 3)) {
            case 1:
            {
            eeprom_93c.buffer = unchecked((ushort)(0));
            eeprom_93c.cycles = unchecked((byte)(0));
            eeprom_93c.state = 3;
            break;
            }
            case 2:
            {
            eeprom_93c.buffer = (new CPointer<byte>(sram.sram) + ((unchecked((int)(eeprom_93c.opcode)) & 63) << 1)).Cast<ushort>()[0];
            eeprom_93c.cycles = unchecked((byte)(0));
            eeprom_93c.state = 4;
            eeprom_93c.data = unchecked((byte)(0));
            break;
            }
            case 3:
            {
            if ((eeprom_93c.we != 0)) {
            (new CPointer<byte>(sram.sram) + ((unchecked((int)(eeprom_93c.opcode)) & 63) << 1)).Cast<ushort>()[0] = unchecked((ushort)(65535));
            }
            eeprom_93c.state = 0;
            break;
            }
            default:
            {
            switch (((unchecked((int)(eeprom_93c.opcode)) >> 4) & 3)) {
            case 1:
            {
            eeprom_93c.buffer = unchecked((ushort)(0));
            eeprom_93c.cycles = unchecked((byte)(0));
            eeprom_93c.state = 3;
            break;
            }
            case 2:
            {
            if ((eeprom_93c.we != 0)) {
            EraseAll();
            }
            eeprom_93c.state = 0;
            break;
            }
            default:
            {
            eeprom_93c.we = unchecked((byte)(((unchecked((int)(eeprom_93c.opcode)) >> 4) & 1)));
            eeprom_93c.state = 0;
            break;
            }
            }
            break;
            }
            }
            }
            break;
            }
            case 3:
            {
            eeprom_93c.buffer |= unchecked((ushort)((((unchecked((int)(data)) >> 0) & 1) << (15 - unchecked((int)(eeprom_93c.cycles))))));
            eeprom_93c.cycles++;
            if ((unchecked((int)(eeprom_93c.cycles)) == 16)) {
            if ((eeprom_93c.we != 0)) {
            if (((unchecked((int)(eeprom_93c.opcode)) & 64) != 0)) {
            (new CPointer<byte>(sram.sram) + ((unchecked((int)(eeprom_93c.opcode)) & 63) << 1)).Cast<ushort>()[0] = eeprom_93c.buffer;
            } else {
            int i = default;
            for (i = 0; (i < 64); i++) {
            (new CPointer<byte>(sram.sram) + (i << 1)).Cast<ushort>()[0] = eeprom_93c.buffer;
            }
            }
            }
            eeprom_93c.state = 0;
            }
            break;
            }
            case 4:
            {
            eeprom_93c.data = unchecked((byte)(((unchecked((int)(eeprom_93c.buffer)) >> (15 - unchecked((int)(eeprom_93c.cycles)))) & 1)));
            eeprom_93c.cycles++;
            if ((unchecked((int)(eeprom_93c.cycles)) == 16)) {
            eeprom_93c.opcode++;
            eeprom_93c.cycles = unchecked((byte)(0));
            eeprom_93c.buffer = (new CPointer<byte>(sram.sram) + ((unchecked((int)(eeprom_93c.opcode)) & 63) << 1)).Cast<ushort>()[0];
            }
            break;
            }
            default:
            {
            break;
            }
            }
            }
            } else {
            if ((eeprom_93c.cs != 0)) {
            eeprom_93c.data = unchecked((byte)(1));
            eeprom_93c.state = 1;
            }
            }
            eeprom_93c.cs = unchecked((byte)(((unchecked((int)(data)) >> 2) & 1)));
            eeprom_93c.clk = unchecked((byte)(((unchecked((int)(data)) >> 1) & 1)));
        }
    }
    private byte eeprom_93c_read()
    {
        unchecked
        {
            return unchecked((byte)((((unchecked((int)(eeprom_93c.cs)) << 2) | (unchecked((int)(eeprom_93c.data)) << 0)) | (1 << 1))));
        }
    }
}
