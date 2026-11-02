using static RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Intel80186.Constants.Cpu80186Constants;
using T = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Intel80186.Constants.Cpu80186Timings;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Intel80186.Services;

internal sealed partial class Cpu80186
{
    /// <summary>Une itération REP par pas : une interruption peut reprendre la chaîne à son préfixe.</summary>
    private int StringInstruction(byte opcode)
    {
        var operation = opcode & 0xfe;
        var setupCycles = operation switch
        {
            0xa6 or 0xae => T.RepeatCompareStart,
            0xaa or 0xac => T.RepeatLoadStoreStart,
            _ => T.RepeatMoveStart
        };
        if (_repeat != 0 && _registers[Cx] == 0) return setupCycles;
        var word = (opcode & 1) != 0; var width = word ? 2 : 1;
        var source = _segmentOverride >= 0 ? _segmentOverride : Ds;
        var step = Has(Direction) ? -width : width;
        int value;
        switch (operation)
        {
            case 0x6c:
                value = word ? ReadIoWord(_registers[Dx]) : ReadIoByte(_registers[Dx]);
                if (word) WriteWord(Es, _registers[Di], (ushort)value); else WriteByte(Es, _registers[Di], (byte)value);
                _registers[Di] = (ushort)(_registers[Di] + step); break;
            case 0x6e:
                value = word ? ReadWord(source, _registers[Si]) : ReadByte(source, _registers[Si]);
                if (word) WriteIoWord(_registers[Dx], (ushort)value); else WriteIoByte(_registers[Dx], (byte)value);
                _registers[Si] = (ushort)(_registers[Si] + step); break;
            case 0xa4:
                value = word ? ReadWord(source, _registers[Si]) : ReadByte(source, _registers[Si]);
                if (word) WriteWord(Es, _registers[Di], (ushort)value); else WriteByte(Es, _registers[Di], (byte)value);
                _registers[Si] = (ushort)(_registers[Si] + step); _registers[Di] = (ushort)(_registers[Di] + step); break;
            case 0xa6:
                Alu(7, word ? ReadWord(source, _registers[Si]) : ReadByte(source, _registers[Si]), word ? ReadWord(Es, _registers[Di]) : ReadByte(Es, _registers[Di]), word);
                _registers[Si] = (ushort)(_registers[Si] + step); _registers[Di] = (ushort)(_registers[Di] + step); break;
            case 0xaa:
                if (word) WriteWord(Es, _registers[Di], _registers[Ax]); else WriteByte(Es, _registers[Di], (byte)_registers[Ax]);
                _registers[Di] = (ushort)(_registers[Di] + step); break;
            case 0xac:
                value = word ? ReadWord(source, _registers[Si]) : ReadByte(source, _registers[Si]);
                if (word) _registers[Ax] = (ushort)value; else SetByteRegister(Ax, value);
                _registers[Si] = (ushort)(_registers[Si] + step); break;
            case 0xae:
                Alu(7, word ? _registers[Ax] : ByteRegister(Ax), word ? ReadWord(Es, _registers[Di]) : ReadByte(Es, _registers[Di]), word);
                _registers[Di] = (ushort)(_registers[Di] + step); break;
        }
        if (_repeat != 0 && --_registers[Cx] != 0 && (operation is not (0xa6 or 0xae) || Has(Zero) == (_repeat == 0xf3)))
        {
            IP = _start;
            _repeatContinuation = true;
            _repeatBusLock = _lockPrefix;
            _repeatStart = _start;
            _repeatSegment = _segments[Cs];
        }
        var iterationCycles = operation switch
        {
            0xa6 => T.StringCompare,
            0xae => T.StringScan,
            0xac => _repeat == 0 ? T.StringLoad : T.RepeatLoadIteration,
            0xaa => _repeat == 0 ? T.StringStore : T.RepeatStoreIteration,
            _ => _repeat == 0 ? T.StringMove : T.RepeatMoveIteration
        };
        return iterationCycles + (_repeat != 0 && !_continuingRepeat ? setupCycles : 0);
    }
    private void DecimalAdjust(bool subtract)
    {
        var value = ByteRegister(Ax); var old = value; var carry = Has(Carry);
        if ((value & DecimalDigitMask) > DecimalDigitMaximum || Has(Auxiliary)) { value += subtract ? -DecimalAdjustment : DecimalAdjustment; Flag(Auxiliary, true); } else Flag(Auxiliary, false);
        if (old > PackedDecimalMaximum || carry) { value += subtract ? -DecimalHighDigitAdjustment : DecimalHighDigitAdjustment; Flag(Carry, true); } else Flag(Carry, false);
        SetByteRegister(Ax, SetResult(value, false));
    }
    private void AsciiAdjust(bool subtract)
    {
        var low = ByteRegister(Ax);
        var adjustment = (low & DecimalDigitMask) > DecimalDigitMaximum || Has(Auxiliary);
        if (adjustment)
        {
            // Le 80186 ajuste les octets séparément ; une retenue AL ne modifie pas AH.
            low += subtract ? -DecimalAdjustment : DecimalAdjustment;
            var high = ByteRegister(HighAccumulatorByte) + (subtract ? -1 : 1);
            SetByteRegister(HighAccumulatorByte, high);
        }
        SetByteRegister(Ax, low & DecimalDigitMask);
        Flag(Auxiliary, adjustment);
        Flag(Carry, adjustment);
    }
}
