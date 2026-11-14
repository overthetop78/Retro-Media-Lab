using System.Buffers.Binary;
using RetroMediaLab.Chips.Common.Memory;

namespace RetroMediaLab.Chips.Memory.Serial.I2c;

public sealed partial class EepromI2c
{
    private void Detect_START()
    {
        unchecked
        {
            if (((eeprom_i2c.old_scl != 0) && (eeprom_i2c.scl != 0))) {
            if (((eeprom_i2c.old_sda != 0) && (!(eeprom_i2c.sda != 0)))) {
            eeprom_i2c.cycles = unchecked((byte)(0));
            if ((unchecked((int)(eeprom_i2c.spec.address_bits)) == 7)) {
            eeprom_i2c.word_address = unchecked((ushort)(0));
            eeprom_i2c.state = 3;
            } else {
            eeprom_i2c.device_address = unchecked((ushort)(0));
            eeprom_i2c.state = 2;
            }
            }
            }
        }
    }
    private void Detect_STOP()
    {
        unchecked
        {
            if (((eeprom_i2c.old_scl != 0) && (eeprom_i2c.scl != 0))) {
            if (((!(eeprom_i2c.old_sda != 0)) && (eeprom_i2c.sda != 0))) {
            eeprom_i2c.state = 0;
            }
            }
        }
    }
    private void eeprom_i2c_update()
    {
        unchecked
        {
            switch (eeprom_i2c.state) {
            case 0:
            {
            Detect_START();
            break;
            }
            case 1:
            {
            Detect_STOP();
            break;
            }
            case 3:
            {
            Detect_START();
            Detect_STOP();
            if (((eeprom_i2c.old_scl != 0) && (!(eeprom_i2c.scl != 0)))) {
            if ((unchecked((int)(eeprom_i2c.cycles)) < 9)) {
            eeprom_i2c.cycles++;
            } else {
            eeprom_i2c.cycles = unchecked((byte)(1));
            eeprom_i2c.state = ((eeprom_i2c.rw != 0) ? 7 : 6);
            eeprom_i2c.buffer = unchecked((byte)(0));
            }
            } else if (((!(eeprom_i2c.old_scl != 0)) && (eeprom_i2c.scl != 0))) {
            if ((unchecked((int)(eeprom_i2c.cycles)) < 8)) {
            eeprom_i2c.word_address |= unchecked((ushort)((unchecked((int)(eeprom_i2c.sda)) << (7 - unchecked((int)(eeprom_i2c.cycles))))));
            } else if ((unchecked((int)(eeprom_i2c.cycles)) == 8)) {
            eeprom_i2c.rw = eeprom_i2c.sda;
            }
            }
            break;
            }
            case 2:
            {
            Detect_START();
            Detect_STOP();
            if (((eeprom_i2c.old_scl != 0) && (!(eeprom_i2c.scl != 0)))) {
            if ((unchecked((int)(eeprom_i2c.cycles)) < 9)) {
            eeprom_i2c.cycles++;
            } else {
            eeprom_i2c.device_address <<= unchecked((int)(eeprom_i2c.spec.address_bits));
            eeprom_i2c.cycles = unchecked((byte)(1));
            if ((eeprom_i2c.rw != 0)) {
            eeprom_i2c.state = 7;
            } else {
            eeprom_i2c.word_address = unchecked((ushort)(0));
            eeprom_i2c.state = ((unchecked((int)(eeprom_i2c.spec.address_bits)) == 16) ? 4 : 5);
            }
            }
            } else if (((!(eeprom_i2c.old_scl != 0)) && (eeprom_i2c.scl != 0))) {
            if (((unchecked((int)(eeprom_i2c.cycles)) > 4) && (unchecked((int)(eeprom_i2c.cycles)) < 8))) {
            eeprom_i2c.device_address |= unchecked((ushort)((unchecked((int)(eeprom_i2c.sda)) << (7 - unchecked((int)(eeprom_i2c.cycles))))));
            } else if ((unchecked((int)(eeprom_i2c.cycles)) == 8)) {
            eeprom_i2c.rw = eeprom_i2c.sda;
            }
            }
            break;
            }
            case 4:
            {
            Detect_START();
            Detect_STOP();
            if (((eeprom_i2c.old_scl != 0) && (!(eeprom_i2c.scl != 0)))) {
            if ((unchecked((int)(eeprom_i2c.cycles)) < 9)) {
            eeprom_i2c.cycles++;
            } else {
            eeprom_i2c.cycles = unchecked((byte)(1));
            eeprom_i2c.state = 5;
            }
            } else if (((!(eeprom_i2c.old_scl != 0)) && (eeprom_i2c.scl != 0))) {
            if ((unchecked((int)(eeprom_i2c.cycles)) < 9)) {
            if ((unchecked((int)(eeprom_i2c.spec.size_mask)) < (1 << (16 - unchecked((int)(eeprom_i2c.cycles)))))) {
            eeprom_i2c.device_address >>= 1;
            } else {
            eeprom_i2c.word_address |= unchecked((ushort)((unchecked((int)(eeprom_i2c.sda)) << (16 - unchecked((int)(eeprom_i2c.cycles))))));
            }
            }
            }
            break;
            }
            case 5:
            {
            Detect_START();
            Detect_STOP();
            if (((eeprom_i2c.old_scl != 0) && (!(eeprom_i2c.scl != 0)))) {
            if ((unchecked((int)(eeprom_i2c.cycles)) < 9)) {
            eeprom_i2c.cycles++;
            } else {
            eeprom_i2c.cycles = unchecked((byte)(1));
            eeprom_i2c.state = 6;
            eeprom_i2c.buffer = unchecked((byte)(0));
            }
            } else if (((!(eeprom_i2c.old_scl != 0)) && (eeprom_i2c.scl != 0))) {
            if ((unchecked((int)(eeprom_i2c.cycles)) < 9)) {
            if ((unchecked((int)(eeprom_i2c.spec.size_mask)) < (1 << (8 - unchecked((int)(eeprom_i2c.cycles)))))) {
            eeprom_i2c.device_address >>= 1;
            } else {
            eeprom_i2c.word_address |= unchecked((ushort)((unchecked((int)(eeprom_i2c.sda)) << (8 - unchecked((int)(eeprom_i2c.cycles))))));
            }
            }
            }
            break;
            }
            case 7:
            {
            Detect_START();
            Detect_STOP();
            if (((eeprom_i2c.old_scl != 0) && (!(eeprom_i2c.scl != 0)))) {
            if ((unchecked((int)(eeprom_i2c.cycles)) < 9)) {
            eeprom_i2c.cycles++;
            } else {
            eeprom_i2c.cycles = unchecked((byte)(1));
            }
            } else if (((!(eeprom_i2c.old_scl != 0)) && (eeprom_i2c.scl != 0))) {
            if ((unchecked((int)(eeprom_i2c.cycles)) == 9)) {
            if ((eeprom_i2c.sda != 0)) {
            eeprom_i2c.state = 1;
            } else {
            eeprom_i2c.word_address = unchecked((ushort)(((unchecked((int)(eeprom_i2c.word_address)) + 1) & unchecked((int)(eeprom_i2c.spec.size_mask)))));
            }
            }
            }
            break;
            }
            case 6:
            {
            Detect_START();
            Detect_STOP();
            if (((eeprom_i2c.old_scl != 0) && (!(eeprom_i2c.scl != 0)))) {
            if ((unchecked((int)(eeprom_i2c.cycles)) < 9)) {
            eeprom_i2c.cycles++;
            } else {
            eeprom_i2c.cycles = unchecked((byte)(1));
            }
            } else if (((!(eeprom_i2c.old_scl != 0)) && (eeprom_i2c.scl != 0))) {
            if ((unchecked((int)(eeprom_i2c.cycles)) < 9)) {
            eeprom_i2c.buffer |= unchecked((byte)((unchecked((int)(eeprom_i2c.sda)) << (8 - unchecked((int)(eeprom_i2c.cycles))))));
            } else {
            _memory.Span[(int)(((unchecked((int)(eeprom_i2c.device_address)) | unchecked((int)(eeprom_i2c.word_address))) & 65535))] = eeprom_i2c.buffer;
            eeprom_i2c.buffer = unchecked((byte)(0));
            eeprom_i2c.word_address = unchecked((ushort)(((unchecked((int)(eeprom_i2c.word_address)) & (~unchecked((int)(eeprom_i2c.spec.pagewrite_mask)))) | ((unchecked((int)(eeprom_i2c.word_address)) + 1) & unchecked((int)(eeprom_i2c.spec.pagewrite_mask))))));
            }
            }
            break;
            }
            }
            eeprom_i2c.old_scl = eeprom_i2c.scl;
            eeprom_i2c.old_sda = eeprom_i2c.sda;
        }
    }
    private byte eeprom_i2c_out()
    {
        unchecked
        {
            if ((eeprom_i2c.state == 7)) {
            if ((unchecked((int)(eeprom_i2c.cycles)) < 9)) {
            return unchecked((byte)(((unchecked((int)(_memory.Span[(int)(((unchecked((int)(eeprom_i2c.device_address)) | unchecked((int)(eeprom_i2c.word_address))) & 65535))])) >> (8 - unchecked((int)(eeprom_i2c.cycles)))) & 1)));
            }
            } else if ((unchecked((int)(eeprom_i2c.cycles)) == 9)) {
            return unchecked((byte)(0));
            }
            return eeprom_i2c.sda;
        }
    }
}
