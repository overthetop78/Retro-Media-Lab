namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Intel80186.Services;

internal sealed partial class Cpu80186
{
    private int Execute(byte opcode)
    {
        if (opcode < 0x40 && (opcode & 7) < 6) return ArithmeticInstruction(opcode);
        if (opcode is >= 0x40 and <= 0x4f) { var registerIndex = opcode & 7; _registers[registerIndex] = (ushort)Increment(_registers[registerIndex], (opcode & 8) != 0, true); return 3; }
        if (opcode is >= 0x50 and <= 0x57) { Push((opcode & 7) == Sp ? (ushort)(_registers[Sp] - 2) : _registers[opcode & 7]); return 10; }
        if (opcode is >= 0x58 and <= 0x5f) { _registers[opcode & 7] = Pop(); return 8; }
        if (opcode is >= 0x70 and <= 0x7f) { var displacement = (sbyte)Fetch(); if (Condition(opcode & 15)) { IP = (ushort)(IP + displacement); return 13; } return 4; }
        if (opcode is >= 0x90 and <= 0x97) { var registerIndex = opcode & 7; (_registers[Ax], _registers[registerIndex]) = (_registers[registerIndex], _registers[Ax]); return 3; }
        if (opcode is >= 0xb0 and <= 0xb7) { SetByteRegister(opcode & 7, Fetch()); return 4; }
        if (opcode is >= 0xb8 and <= 0xbf) { _registers[opcode & 7] = FetchWord(); return 4; }
        byte modrm; Operand operand; ushort offset; int value; int source; int index; bool word;
        switch (opcode)
        {
            case 0x06: case 0x0e: case 0x16: case 0x1e: Push(_segments[(opcode >> 3) & 3]); return 9;
            case 0x07: case 0x17: case 0x1f: index = (opcode >> 3) & 3; _segments[index] = Pop(); if (index == Ss) _interruptDelay = 1; return 8;
            case 0x27: case 0x2f: DecimalAdjust(opcode == 0x2f); return 4;
            case 0x37: case 0x3f: AsciiAdjust(opcode == 0x3f); return 8;
            case 0x60:
                var stack = _registers[Sp];
                for (var i = 0; i < 8; i++) Push(i == Sp ? stack : _registers[i]); return 19;
            case 0x61:
                for (var i = 7; i >= 0; i--) { var popped = Pop(); if (i != Sp) _registers[i] = popped; } return 19;
            case 0x62:
                modrm = Fetch(); operand = Decode(modrm);
                if (operand.Register) { Interrupt(6, true); return 4; }
                var bound = (short)_registers[(modrm >> 3) & 7];
                if (bound < (short)ReadWord(operand.Segment, operand.Offset) || bound > (short)ReadWord(operand.Segment, (ushort)(operand.Offset + 2))) Interrupt(5, true);
                return 13;
            case 0x68: Push(FetchWord()); return 10;
            case 0x6a: Push((ushort)(short)(sbyte)Fetch()); return 10;
            case 0x69: case 0x6b:
                modrm = Fetch(); operand = Decode(modrm); value = (short)Load(operand, true); source = opcode == 0x69 ? (short)FetchWord() : (sbyte)Fetch();
                var product = value * source; _registers[(modrm >> 3) & 7] = (ushort)product;
                Flag(Carry, product != (short)product); Flag(Overflow, Has(Carry)); return 24;
            case 0x6c: case 0x6d: case 0x6e: case 0x6f:
            case 0xa4: case 0xa5: case 0xa6: case 0xa7: case 0xaa: case 0xab: case 0xac: case 0xad: case 0xae: case 0xaf:
                return StringInstruction(opcode);
            case 0x80: case 0x81: case 0x82: case 0x83:
                word = (opcode & 1) != 0; modrm = Fetch(); operand = Decode(modrm);
                source = opcode == 0x83 ? (ushort)(short)(sbyte)Fetch() : word ? FetchWord() : Fetch();
                value = Alu((modrm >> 3) & 7, Load(operand, word), source, word);
                if (((modrm >> 3) & 7) != 7) Store(operand, word, value); return operand.Register ? 4 : 16;
            case 0x84: case 0x85:
                word = (opcode & 1) != 0; modrm = Fetch(); operand = Decode(modrm); index = (modrm >> 3) & 7;
                Alu(4, Load(operand, word), word ? _registers[index] : ByteRegister(index), word); return 4;
            case 0x86: case 0x87:
                word = (opcode & 1) != 0; modrm = Fetch(); operand = Decode(modrm); index = (modrm >> 3) & 7;
                value = Load(operand, word); Store(operand, word, word ? _registers[index] : ByteRegister(index));
                if (word) _registers[index] = (ushort)value; else SetByteRegister(index, value); return 17;
            case 0x88: case 0x89: case 0x8a: case 0x8b:
                word = (opcode & 1) != 0; modrm = Fetch(); operand = Decode(modrm); index = (modrm >> 3) & 7;
                if ((opcode & 2) == 0) Store(operand, word, word ? _registers[index] : ByteRegister(index));
                else { value = Load(operand, word); if (word) _registers[index] = (ushort)value; else SetByteRegister(index, value); } return operand.Register ? 2 : 9;
            case 0x8c: case 0x8e:
                modrm = Fetch(); operand = Decode(modrm); index = (modrm >> 3) & 7;
                if (index >= _segments.Length || opcode == 0x8e && index == Cs) { Interrupt(6, true); return 4; }
                if (opcode == 0x8c) Store(operand, true, _segments[index]);
                else { _segments[index] = (ushort)Load(operand, true); if (index == Ss) _interruptDelay = 1; } return 8;
            case 0x8d:
                modrm = Fetch(); operand = Decode(modrm);
                if (operand.Register) Interrupt(6, true); else _registers[(modrm >> 3) & 7] = operand.Offset; return 6;
            case 0x8f: modrm = Fetch(); operand = Decode(modrm); Store(operand, true, Pop()); return 17;
            case 0x98: _registers[Ax] = (ushort)(short)(sbyte)ByteRegister(Ax); return 2;
            case 0x99: _registers[Dx] = (short)_registers[Ax] < 0 ? ushort.MaxValue : (ushort)0; return 4;
            case 0x9a:
                offset = FetchWord(); var segment = FetchWord(); Push(_segments[Cs]); Push(IP); _segments[Cs] = segment; IP = offset; return 28;
            case 0x9b: return 3;
            case 0x9c: Push((ushort)(_flags | ReservedHigh)); return 9;
            case 0x9d: _flags = (ushort)((Pop() & 0x0fd5) | Reserved); return 8;
            case 0x9e: _flags = (ushort)((_flags & 0xff00) | (ByteRegister(4) & 0xd5) | Reserved); return 3;
            case 0x9f: SetByteRegister(4, _flags); return 2;
            case 0xa0: case 0xa1: case 0xa2: case 0xa3:
                word = (opcode & 1) != 0; offset = FetchWord(); operand = new(false, 0, _segmentOverride >= 0 ? _segmentOverride : Ds, offset);
                if ((opcode & 2) != 0) Store(operand, word, word ? _registers[Ax] : ByteRegister(Ax));
                else { value = Load(operand, word); if (word) _registers[Ax] = (ushort)value; else SetByteRegister(Ax, value); } return 10;
            case 0xa8: case 0xa9: word = (opcode & 1) != 0; Alu(4, word ? _registers[Ax] : ByteRegister(Ax), word ? FetchWord() : Fetch(), word); return 4;
            case 0xc0: case 0xc1: case 0xd0: case 0xd1: case 0xd2: case 0xd3:
                word = (opcode & 1) != 0; modrm = Fetch(); operand = Decode(modrm);
                var count = opcode < 0xd0 ? Fetch() : opcode < 0xd2 ? 1 : ByteRegister(Cx);
                Store(operand, word, Shift((modrm >> 3) & 7, Load(operand, word), count, word)); return 5 + (count & 31);
            case 0xc2: offset = FetchWord(); IP = Pop(); _registers[Sp] += offset; return 18;
            case 0xc3: IP = Pop(); return 16;
            case 0xc4: case 0xc5:
                modrm = Fetch(); operand = Decode(modrm);
                if (operand.Register) { Interrupt(6, true); return 4; }
                _registers[(modrm >> 3) & 7] = ReadWord(operand.Segment, operand.Offset);
                _segments[opcode == 0xc4 ? Es : Ds] = ReadWord(operand.Segment, (ushort)(operand.Offset + 2)); return 16;
            case 0xc6: case 0xc7: word = (opcode & 1) != 0; modrm = Fetch(); operand = Decode(modrm); Store(operand, word, word ? FetchWord() : Fetch()); return 10;
            case 0xc8:
                var allocation = FetchWord(); var nesting = Fetch() & 31;
                Push(_registers[Bp]); var frame = _registers[Sp];
                if (nesting > 0) { for (var i = 1; i < nesting; i++) { _registers[Bp] -= 2; Push(ReadWord(Ss, _registers[Bp])); } Push(frame); }
                _registers[Bp] = frame; _registers[Sp] -= allocation; return 15 + nesting * 4;
            case 0xc9: _registers[Sp] = _registers[Bp]; _registers[Bp] = Pop(); return 8;
            case 0xca: offset = FetchWord(); IP = Pop(); _segments[Cs] = Pop(); _registers[Sp] += offset; return 25;
            case 0xcb: IP = Pop(); _segments[Cs] = Pop(); return 22;
            case 0xcc: Interrupt(3); return 45;
            case 0xcd: Interrupt(Fetch()); return 45;
            case 0xce: if (Has(Overflow)) Interrupt(4); return 4;
            case 0xcf: IP = Pop(); _segments[Cs] = Pop(); _flags = (ushort)((Pop() & 0x0fd5) | Reserved); return 28;
            case 0xd4:
                var radix = Fetch(); if (radix == 0) { Interrupt(0, true); return 4; }
                value = ByteRegister(Ax); SetByteRegister(4, value / radix); SetByteRegister(Ax, SetResult(value % radix, false)); return 19;
            case 0xd5: value = ByteRegister(Ax) + ByteRegister(4) * Fetch(); _registers[Ax] = (ushort)SetResult(value, false); return 15;
            case 0xd6: SetByteRegister(Ax, Has(Carry) ? 255 : 0); return 3;
            case 0xd7: SetByteRegister(Ax, ReadByte(_segmentOverride >= 0 ? _segmentOverride : Ds, (ushort)(_registers[Bx] + ByteRegister(Ax)))); return 11;
            case >= 0xd8 and <= 0xdf: Decode(Fetch()); return 2;
            case 0xe0: case 0xe1: case 0xe2: case 0xe3:
                var displacement = (sbyte)Fetch();
                var taken = opcode == 0xe3 ? _registers[Cx] == 0 : --_registers[Cx] != 0 && (opcode == 0xe2 || Has(Zero) == (opcode == 0xe1));
                if (taken) IP = (ushort)(IP + displacement); return taken ? 13 : 5;
            case 0xe4: case 0xe5: case 0xec: case 0xed:
                var port = opcode < 0xec ? Fetch() : _registers[Dx]; value = input(port);
                if ((opcode & 1) != 0) _registers[Ax] = (ushort)(value | input((ushort)(port + 1)) << 8); else SetByteRegister(Ax, value); return 10;
            case 0xe6: case 0xe7: case 0xee: case 0xef:
                var outPort = opcode < 0xee ? Fetch() : _registers[Dx]; output(outPort, (byte)_registers[Ax]);
                if ((opcode & 1) != 0) output((ushort)(outPort + 1), (byte)(_registers[Ax] >> 8)); return 10;
            case 0xe8: offset = FetchWord(); var next = IP; Push(next); IP = (ushort)(next + (short)offset); return 19;
            case 0xe9: offset = FetchWord(); IP = (ushort)(IP + (short)offset); return 15;
            case 0xea: offset = FetchWord(); _segments[Cs] = FetchWord(); IP = offset; return 15;
            case 0xeb: var relative = (sbyte)Fetch(); IP = (ushort)(IP + relative); return 15;
            case 0xf4: _halted = true; return 2;
            case 0xf5: Flag(Carry, !Has(Carry)); return 2;
            case 0xf6: case 0xf7:
                word = (opcode & 1) != 0; modrm = Fetch(); operand = Decode(modrm); value = Load(operand, word); index = (modrm >> 3) & 7;
                if (index <= 1) Alu(4, value, word ? FetchWord() : Fetch(), word);
                else if (index == 2) Store(operand, word, ~value);
                else if (index == 3) Store(operand, word, Alu(5, 0, value, word));
                else MultiplyDivide(index, value, word); return index >= 4 ? 30 : 8;
            case 0xf8: Flag(Carry, false); return 2;
            case 0xf9: Flag(Carry, true); return 2;
            case 0xfa: Flag(InterruptEnable, false); return 2;
            case 0xfb: Flag(InterruptEnable, true); _interruptDelay = 1; return 2;
            case 0xfc: Flag(Direction, false); return 2;
            case 0xfd: Flag(Direction, true); return 2;
            case 0xfe: case 0xff: return GroupFive(opcode);
            default: Interrupt(6, true); return 4;
        }
    }
    private int ArithmeticInstruction(byte opcode)
    {
        var operation = opcode >> 3; var form = opcode & 7; var word = (form & 1) != 0;
        if (form >= 4)
        {
            var result = Alu(operation, word ? _registers[Ax] : ByteRegister(Ax), word ? FetchWord() : Fetch(), word);
            if (operation != 7) { if (word) _registers[Ax] = (ushort)result; else SetByteRegister(Ax, result); }
            return 4;
        }
        var modrm = Fetch(); var operand = Decode(modrm); var register = (modrm >> 3) & 7;
        var left = Load(operand, word); var right = word ? _registers[register] : ByteRegister(register);
        var toRegister = (form & 2) != 0;
        var value = toRegister ? Alu(operation, right, left, word) : Alu(operation, left, right, word);
        if (operation != 7)
        {
            if (!toRegister) Store(operand, word, value);
            else if (word) _registers[register] = (ushort)value; else SetByteRegister(register, value);
        }
        return operand.Register ? 3 : 16;
    }
    private int GroupFive(byte opcode)
    {
        var word = opcode == 0xff; var modrm = Fetch(); var operand = Decode(modrm); var operation = (modrm >> 3) & 7;
        var value = Load(operand, word);
        if (operation <= 1) Store(operand, word, Increment(value, operation == 1, word));
        else if (!word || operation == 7) Interrupt(6, true);
        else if (operation == 2) { Push(IP); IP = (ushort)value; }
        else if (operation is 3 or 5)
        {
            if (operand.Register) { Interrupt(6, true); return 4; }
            var segment = ReadWord(operand.Segment, (ushort)(operand.Offset + 2));
            if (operation == 3) { Push(_segments[Cs]); Push(IP); }
            IP = (ushort)value; _segments[Cs] = segment;
        }
        else if (operation == 4) IP = (ushort)value;
        else if (operation == 6) Push(operand.Register && operand.Index == Sp ? (ushort)(value - 2) : (ushort)value);
        return 16;
    }
}
