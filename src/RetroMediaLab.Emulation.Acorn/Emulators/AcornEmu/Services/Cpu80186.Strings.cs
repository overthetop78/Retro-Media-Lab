namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;

internal sealed partial class Cpu80186
{
    /// <summary>Une itération REP par pas : une interruption peut reprendre la chaîne à son préfixe.</summary>
    private int StringInstruction(byte opcode)
    {
        if (_repeat != 0 && _registers[Cx] == 0) return 2;
        var word = (opcode & 1) != 0; var width = word ? 2 : 1;
        var source = _segmentOverride >= 0 ? _segmentOverride : Ds;
        var step = Has(Direction) ? -width : width;
        int value;
        switch (opcode & 0xfe)
        {
            case 0x6c:
                value = input(_registers[Dx]); if (word) value |= input((ushort)(_registers[Dx] + 1)) << 8;
                if (word) WriteWord(Es, _registers[Di], (ushort)value); else WriteByte(Es, _registers[Di], (byte)value);
                _registers[Di] = (ushort)(_registers[Di] + step); break;
            case 0x6e:
                value = word ? ReadWord(source, _registers[Si]) : ReadByte(source, _registers[Si]);
                output(_registers[Dx], (byte)value); if (word) output((ushort)(_registers[Dx] + 1), (byte)(value >> 8));
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
        if (_repeat != 0 && --_registers[Cx] != 0 && ((opcode & 0xfe) is not (0xa6 or 0xae) || Has(Zero) == (_repeat == 0xf3))) IP = _start;
        return 14;
    }
    private void DecimalAdjust(bool subtract)
    {
        var value = ByteRegister(Ax); var old = value; var carry = Has(Carry);
        if ((value & 15) > 9 || Has(Auxiliary)) { value += subtract ? -6 : 6; Flag(Auxiliary, true); } else Flag(Auxiliary, false);
        if (old > 0x99 || carry) { value += subtract ? -0x60 : 0x60; Flag(Carry, true); } else Flag(Carry, false);
        SetByteRegister(Ax, SetResult(value, false));
    }
    private void AsciiAdjust(bool subtract)
    {
        if ((ByteRegister(Ax) & 15) > 9 || Has(Auxiliary))
        {
            _registers[Ax] = (ushort)(_registers[Ax] + (subtract ? -0x106 : 0x106)); Flag(Auxiliary, true); Flag(Carry, true);
        }
        else { Flag(Auxiliary, false); Flag(Carry, false); }
        SetByteRegister(Ax, ByteRegister(Ax) & 15);
    }
}
