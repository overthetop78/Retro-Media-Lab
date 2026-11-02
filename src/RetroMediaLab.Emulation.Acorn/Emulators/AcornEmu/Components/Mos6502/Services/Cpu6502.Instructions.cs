namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Mos6502.Services;

internal sealed partial class Cpu6502
{
    /// <summary>Instructions de base du chemin CMOS ; le NMOS utilise son séquenceur de bus.</summary>
    private int ExecuteCmosBase(byte opcode)
    {
        // Ces instructions occupent les positions irrégulières du groupe des décalages/chargements.
        switch (opcode)
        {
            case 0x8a: _a = Nz(_x); return 2;
            case 0x9a: _s = _x; return 2;
            case 0xaa: _x = Nz(_a); return 2;
            case 0xba: _x = Nz(_s); return 2;
            case 0xca: _x = Nz((byte)(_x - 1)); return 2;
            case 0xea: return 2;
        }
        if ((opcode & 3) == 1) return GroupOne(opcode);
        if ((opcode & 3) == 2) return GroupTwo(opcode);
        if ((opcode & 0x1f) == 0x10)
        {
            var test = (opcode >> 6) switch { 0 => Negative, 1 => Overflow, 2 => Carry, _ => Zero };
            return Branch(((_p & test) != 0) == ((opcode & 0x20) != 0));
        }
        switch (opcode)
        {
            case 0x00:
                Fetch(); Push((byte)(_pc >> 8)); Push((byte)_pc); Push((byte)(_p | Break | Unused)); _p |= InterruptDisable; if (cmos) _p &= unchecked((byte)~Decimal); _pc = ReadWord(InterruptVector(0xfffe)); return 7;
            case 0x08: Read(_pc); Push((byte)(_p | Break | Unused)); return 3;
            case 0x28: PreparePull(); _p = (byte)((Pop() & ~Break) | Unused); return 4;
            case 0x48: Read(_pc); Push(_a); return 3;
            case 0x68: PreparePull(); _a = Nz(Pop()); return 4;
            case 0x18: _p &= unchecked((byte)~Carry); return 2;
            case 0x38: _p |= Carry; return 2;
            case 0x58: _p &= unchecked((byte)~InterruptDisable); return 2;
            case 0x78: _p |= InterruptDisable; return 2;
            case 0xb8: _p &= unchecked((byte)~Overflow); return 2;
            case 0xd8: _p &= unchecked((byte)~Decimal); return 2;
            case 0xf8: _p |= Decimal; return 2;
            case 0x20:
                var targetLow = Fetch(); Read((ushort)(0x100 | _s)); Push((byte)(_pc >> 8)); Push((byte)_pc); _pc = (ushort)(targetLow | Fetch() << 8); return 6;
            case 0x40: PreparePull(); _p = (byte)((Pop() & ~Break) | Unused); var lo = Pop(); _pc = (ushort)(lo | Pop() << 8); return 6;
            case 0x60: PreparePull(); var low = Pop(); var last = (ushort)(low | Pop() << 8); Read(last); _pc = (ushort)(last + 1); return 6;
            case 0x4c: _pc = FetchWord(); return 3;
            case 0x6c:
                var indirect = FetchWord();
                var l = Read(indirect);
                if (cmos) Read((ushort)((indirect & 0xff00) | (byte)(indirect + 1)));
                _pc = (ushort)(l | Read(cmos ? (ushort)(indirect + 1) : (ushort)((indirect & 0xff00) | (byte)(indirect + 1))) << 8);
                return cmos ? 6 : 5;
            case 0x24: Bit(Read(Fetch())); return 3;
            case 0x2c: Bit(Read(FetchWord())); return 4;
            case 0x84: Write(Fetch(), _y); return 3;
            case 0x94: Write(IndexedZeroPage(_x), _y); return 4;
            case 0x8c: Write(FetchWord(), _y); return 4;
            case 0xa0: _y = Nz(Fetch()); return 2;
            case 0xa4: _y = Nz(Read(Fetch())); return 3;
            case 0xb4: _y = Nz(Read(IndexedZeroPage(_x))); return 4;
            case 0xac: _y = Nz(Read(FetchWord())); return 4;
            case 0xbc: var address = FetchWord(); _y = Nz(Read(IndexedAddress(address, _x, false))); return 4 + PageCross(address, _x);
            case 0xc0: Compare(_y, Fetch()); return 2;
            case 0xc4: Compare(_y, Read(Fetch())); return 3;
            case 0xcc: Compare(_y, Read(FetchWord())); return 4;
            case 0xe0: Compare(_x, Fetch()); return 2;
            case 0xe4: Compare(_x, Read(Fetch())); return 3;
            case 0xec: Compare(_x, Read(FetchWord())); return 4;
            case 0x88: _y = Nz((byte)(_y - 1)); return 2;
            case 0xc8: _y = Nz((byte)(_y + 1)); return 2;
            case 0xe8: _x = Nz((byte)(_x + 1)); return 2;
            case 0x98: _a = Nz(_y); return 2;
            case 0xa8: _y = Nz(_a); return 2;
            default: throw new InvalidOperationException($"6502: opcode {opcode:X2} non pris en charge à {(ushort)(PC - 1):X4}.");
        }
    }

    private static int PageCross(ushort address, byte index) => (address & 0xff00) == ((address + index) & 0xff00) ? 0 : 1;
    private void Bit(byte value) { Flag(Zero, (_a & value) == 0); _p = (byte)((_p & 0x3f) | (value & 0xc0)); }

    /// <summary>ORA, AND, EOR, ADC, STA, LDA, CMP et SBC partagent huit formes d'adressage.</summary>
    private int GroupOne(byte opcode)
    {
        var operation = opcode >> 5;
        var mode = (opcode >> 2) & 7;
        ushort address;
        var extra = 0;
        var cycles = 0;
        switch (mode)
        {
            case 0: address = IndexedIndirect(); cycles = 6; break;
            case 1: address = Fetch(); cycles = 3; break;
            case 2:
                if (operation == 4) throw new InvalidOperationException("6502: STA immédiat invalide.");
                address = _pc++; cycles = 2; break;
            case 3: address = FetchWord(); cycles = 4; break;
            case 4: var pointer = ZeroPageWord(Fetch()); extra = PageCross(pointer, _y); address = IndexedAddress(pointer, _y, operation == 4, indirect: true); cycles = operation == 4 ? 6 : 5; break;
            case 5: address = IndexedZeroPage(_x); cycles = 4; break;
            case 6: var ay = FetchWord(); extra = PageCross(ay, _y); address = IndexedAddress(ay, _y, operation == 4); cycles = operation == 4 ? 5 : 4; break;
            default: var ax = FetchWord(); extra = PageCross(ax, _x); address = IndexedAddress(ax, _x, operation == 4); cycles = operation == 4 ? 5 : 4; break;
        }
        if (operation == 4) { Write(address, _a); return cycles; }
        var value = Read(address);
        switch (operation)
        {
            case 0: _a = Nz((byte)(_a | value)); break;
            case 1: _a = Nz((byte)(_a & value)); break;
            case 2: _a = Nz((byte)(_a ^ value)); break;
            case 3: Add(value); break;
            case 5: _a = Nz(value); break;
            case 6: Compare(_a, value); break;
            case 7: Subtract(value); break;
        }
        return cycles + extra;
    }

    /// <summary>Décalages, rotations, STX/LDX et décrémentations/incrémentations mémoire.</summary>
    private int GroupTwo(byte opcode)
    {
        var operation = opcode >> 5;
        var mode = (opcode >> 2) & 7;
        if (mode == 0 && operation == 5) { _x = Nz(Fetch()); return 2; }
        var accumulator = mode == 2 && operation < 4;
        ushort address = 0;
        var cycles = 2;
        var extra = 0;
        if (!accumulator)
        {
            switch (mode)
            {
                case 1: address = Fetch(); cycles = operation is 4 or 5 ? 3 : 5; break;
                case 3: address = FetchWord(); cycles = operation is 4 or 5 ? 4 : 6; break;
                case 5: address = IndexedZeroPage(operation is 4 or 5 ? _y : _x); cycles = operation is 4 or 5 ? 4 : 6; break;
                case 7 when operation != 4:
                    var a = FetchWord();
                    var index = operation == 5 ? _y : _x;
                    var cmosRotate = cmos && operation < 4;
                    address = IndexedAddress(a, index, operation != 5 && !cmosRotate);
                    extra = operation == 5 || cmosRotate ? PageCross(a, index) : 0;
                    cycles = operation == 5 ? 4 : cmosRotate ? 6 : 7;
                    break;
                default: throw new InvalidOperationException($"6502: opcode {opcode:X2} non pris en charge.");
            }
        }
        if (operation == 4) { Write(address, _x); return cycles; }
        var old = accumulator ? _a : Read(address);
        if (operation == 5) { _x = Nz(old); return cycles + extra; }
        var c = _p & Carry;
        byte result;
        switch (operation)
        {
            case 0: Flag(Carry, (old & 128) != 0); result = (byte)(old << 1); break;
            case 1: Flag(Carry, (old & 128) != 0); result = (byte)((old << 1) | c); break;
            case 2: Flag(Carry, (old & 1) != 0); result = (byte)(old >> 1); break;
            case 3: Flag(Carry, (old & 1) != 0); result = (byte)((old >> 1) | (c << 7)); break;
            case 6: result = (byte)(old - 1); break;
            default: result = (byte)(old + 1); break;
        }
        result = Nz(result);
        if (accumulator) _a = result;
        else { if (cmos) _ = Read(address); else Write(address, old); Write(address, result); }
        return cycles + extra;
    }
}
