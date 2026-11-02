using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants.Cpu6809Constants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;

internal sealed partial class Cpu6809
{
    private int Execute(byte opcode, int page)
    {
        if (opcode == 0x3f) return SoftwareInterrupt(page);
        if (page == 0x10 && opcode is >= 0x21 and <= 0x2f)
        {
            var displacement = (short)FetchWord();
            var taken = Condition(opcode & C.NibbleMask);
            if (taken) PC = (ushort)(PC + displacement);
            return taken ? 6 : 5;
        }
        if (opcode >= 0x80) return DataInstruction(opcode, page);
        if (page != 0) throw Illegal(opcode);
        if (opcode <= 0x0f || opcode is >= 0x40 and <= 0x7f) return UnaryInstruction(opcode);
        if (opcode is >= 0x20 and <= 0x2f)
        {
            var displacement = (sbyte)Fetch();
            if (Condition(opcode & C.NibbleMask)) PC = (ushort)(PC + displacement);
            return 3;
        }
        switch (opcode)
        {
            case 0x12: return 2;
            case 0x13: _sync = true; return 4;
            case 0x16:
                var jump = (short)FetchWord(); PC = (ushort)(PC + jump); return 5;
            case 0x17:
                var call = (short)FetchWord(); PushPc(); PC = (ushort)(PC + call); return 9;
            case 0x19: DecimalAdjust(); return 2;
            case 0x1a: CC |= Fetch(); return 3;
            case 0x1c: CC &= Fetch(); return 3;
            case 0x1d: A = (B & C.ByteSign) != 0 ? byte.MaxValue : (byte)0; Nz(D); return 2;
            case 0x1e: Transfer(true); return 8;
            case 0x1f: Transfer(false); return 6;
            case 0x30: X = Indexed(); Flag(C.Zero, X == 0); return 4;
            case 0x31: Y = Indexed(); Flag(C.Zero, Y == 0); return 4;
            case 0x32: S = Indexed(); _stackInitialized = true; return 4;
            case 0x33: U = Indexed(); return 4;
            case 0x34: return 5 + PushRegisters(Fetch(), false);
            case 0x35: return 5 + PullRegisters(Fetch(), false);
            case 0x36: return 5 + PushRegisters(Fetch(), true);
            case 0x37: return 5 + PullRegisters(Fetch(), true);
            case 0x39: PC = PullPc(); return 5;
            case 0x3a: X = (ushort)(X + B); return 3;
            case 0x3b: return ReturnInterrupt();
            case 0x3c:
                CC &= Fetch(); Flag(C.EntireState, true); PushRegisters(byte.MaxValue, false); _cwai = true; return 20;
            case 0x3d:
                D = (ushort)(A * B); Flag(C.Zero, D == 0); Flag(C.Carry, (B & C.ByteSign) != 0); return 11;
            default: throw Illegal(opcode);
        }
    }

    private int UnaryInstruction(byte opcode)
    {
        var operation = opcode & C.NibbleMask;
        var accumulator = opcode is >= 0x40 and <= 0x5f;
        var address = accumulator ? (ushort)0 : Address(opcode < 0x10 ? 1 : opcode < 0x70 ? 2 : 3);
        if (operation == 0x0e)
        {
            if (accumulator) throw Illegal(opcode);
            PC = address;
            return opcode >= 0x70 ? 4 : 3;
        }
        var value = accumulator ? opcode < 0x50 ? A : B : Read(address);
        byte result;
        switch (operation)
        {
            case 0x00: result = Subtract(value: 0, operand: value); break;
            case 0x03: result = Nz((byte)~value); Flag(C.Overflow, false); Flag(C.Carry, true); break;
            case 0x04: Flag(C.Carry, (value & 1) != 0); result = Nz((byte)(value >> 1)); break;
            case 0x06:
                var carry = IsSet(C.Carry) ? C.ByteSign : 0;
                Flag(C.Carry, (value & 1) != 0); result = Nz((byte)(value >> 1 | carry)); break;
            case 0x07: Flag(C.Carry, (value & 1) != 0); result = Nz((byte)((sbyte)value >> 1)); break;
            case 0x08:
                result = Nz((byte)(value << 1)); Flag(C.Carry, (value & C.ByteSign) != 0);
                Flag(C.Overflow, ((value ^ result) & C.ByteSign) != 0); break;
            case 0x09:
                var low = IsSet(C.Carry) ? 1 : 0;
                result = Nz((byte)(value << 1 | low)); Flag(C.Carry, (value & C.ByteSign) != 0);
                Flag(C.Overflow, ((value ^ result) & C.ByteSign) != 0); break;
            case 0x0a: result = Nz((byte)(value - 1)); Flag(C.Overflow, value == C.ByteSign); break;
            case 0x0c: result = Nz((byte)(value + 1)); Flag(C.Overflow, value == C.ByteSign - 1); break;
            case 0x0d: Load(value); return accumulator ? 2 : opcode >= 0x70 ? 7 : 6;
            case 0x0f: result = Load((byte)0); Flag(C.Carry, false); break;
            default: throw Illegal(opcode);
        }
        if (accumulator) { if (opcode < 0x50) A = result; else B = result; }
        else Write(address, result);
        return accumulator ? 2 : opcode >= 0x70 ? 7 : 6;
    }

    private int DataInstruction(byte opcode, int page)
    {
        var mode = opcode >> 4 & C.RegisterMask;
        var operation = opcode & C.NibbleMask;
        var second = opcode >= 0xc0;
        if (page != 0) return PrefixedData(opcode, page, mode, operation, second);
        if (!second && operation == 0x0d)
        {
            if (mode == 0)
            {
                var displacement = (sbyte)Fetch(); PushPc(); PC = (ushort)(PC + displacement); return 7;
            }
            var target = Address(mode); PushPc(); PC = target; return mode == 3 ? 8 : 7;
        }
        var word = operation == 3 || operation >= 0x0c;
        var store = operation == 7 || operation == 0x0f || second && operation == 0x0d;
        if (mode == 0 && store) throw Illegal(opcode);
        var address = mode == 0 ? (ushort)0 : Address(mode);
        var cycles = mode == 0 ? 2 : mode == 3 ? 5 : 4;
        if (word)
        {
            if (store)
            {
                var stored = operation == 0x0d ? D : second ? U : X;
                WriteWord(address, Load(stored));
                return cycles + 1;
            }
            var operand = mode == 0 ? FetchWord() : ReadWord(address);
            switch (operation)
            {
                case 3:
                    D = second ? AddWord(D, operand) : SubtractWord(D, operand); return cycles + 2;
                case 0x0c:
                    if (second) { D = Load(operand); return cycles + 1; }
                    SubtractWord(X, operand); return cycles + 2;
                case 0x0e:
                    if (second) U = Load(operand); else X = Load(operand); return cycles + 1;
                default: throw Illegal(opcode);
            }
        }
        var value = second ? B : A;
        if (store) { Write(address, Load(value)); return cycles; }
        var data = mode == 0 ? Fetch() : Read(address);
        byte result;
        switch (operation)
        {
            case 0: result = Subtract(value, data); break;
            case 1: Subtract(value, data); return cycles;
            case 2: result = Subtract(value, data, IsSet(C.Carry) ? 1 : 0); break;
            case 4: result = Load((byte)(value & data)); break;
            case 5: Load((byte)(value & data)); return cycles;
            case 6: result = Load(data); break;
            case 8: result = Load((byte)(value ^ data)); break;
            case 9: result = Add(value, data, IsSet(C.Carry) ? 1 : 0); break;
            case 0x0a: result = Load((byte)(value | data)); break;
            case 0x0b: result = Add(value, data); break;
            default: throw Illegal(opcode);
        }
        if (second) B = result; else A = result;
        return cycles;
    }

    private int PrefixedData(byte opcode, int page, int mode, int operation, bool second)
    {
        var compare = !second && (operation == 3 || operation == 0x0c);
        var load = page == 0x10 && operation == 0x0e;
        var store = page == 0x10 && operation == 0x0f && mode != 0;
        if (!compare && !load && !store) throw Illegal(opcode);
        var address = mode == 0 ? (ushort)0 : Address(mode);
        var cycles = mode == 0 ? 4 : mode == 3 ? 7 : 6;
        if (store) { WriteWord(address, Load(second ? S : Y)); return cycles; }
        var operand = mode == 0 ? FetchWord() : ReadWord(address);
        if (compare)
        {
            var register = page == 0x10 ? operation == 3 ? D : Y : operation == 3 ? U : S;
            SubtractWord(register, operand);
            return cycles + 1;
        }
        if (second) { S = Load(operand); _stackInitialized = true; }
        else Y = Load(operand);
        return cycles;
    }

    private byte Add(byte value, byte operand, int carry = 0)
    {
        var sum = value + operand + carry;
        Flag(C.HalfCarry, ((value & C.NibbleMask) + (operand & C.NibbleMask) + carry) > C.NibbleMask);
        Flag(C.Carry, sum > byte.MaxValue);
        Flag(C.Overflow, (~(value ^ operand) & (value ^ sum) & C.ByteSign) != 0);
        return Nz((byte)sum);
    }

    private byte Subtract(byte value, byte operand, int carry = 0)
    {
        var difference = value - operand - carry;
        Flag(C.Carry, difference < 0);
        Flag(C.Overflow, ((value ^ operand) & (value ^ difference) & C.ByteSign) != 0);
        return Nz((byte)difference);
    }

    private ushort AddWord(ushort value, ushort operand)
    {
        var sum = value + operand;
        Flag(C.Carry, sum > ushort.MaxValue);
        Flag(C.Overflow, (~(value ^ operand) & (value ^ sum) & C.WordSign) != 0);
        return Nz((ushort)sum);
    }

    private ushort SubtractWord(ushort value, ushort operand)
    {
        var difference = value - operand;
        Flag(C.Carry, difference < 0);
        Flag(C.Overflow, ((value ^ operand) & (value ^ difference) & C.WordSign) != 0);
        return Nz((ushort)difference);
    }

    private bool Condition(int condition)
    {
        var signed = IsSet(C.Negative) != IsSet(C.Overflow);
        return condition switch
        {
            0 => true, 1 => false, 2 => !IsSet(C.Carry) && !IsSet(C.Zero), 3 => IsSet(C.Carry) || IsSet(C.Zero),
            4 => !IsSet(C.Carry), 5 => IsSet(C.Carry), 6 => !IsSet(C.Zero), 7 => IsSet(C.Zero),
            8 => !IsSet(C.Overflow), 9 => IsSet(C.Overflow), 10 => !IsSet(C.Negative), 11 => IsSet(C.Negative),
            12 => !signed, 13 => signed, 14 => !signed && !IsSet(C.Zero), _ => signed || IsSet(C.Zero)
        };
    }

    private void PushPc() { var stack = S; PushWord(ref stack, PC); S = stack; }
    private ushort PullPc() { var stack = S; var value = PullWord(ref stack); S = stack; return value; }

    private void DecimalAdjust()
    {
        var correction = 0;
        if ((A & C.NibbleMask) > 9 || IsSet(C.HalfCarry)) correction |= 0x06;
        if (A > 0x99 || IsSet(C.Carry)) correction |= 0x60;
        var sum = A + correction;
        Flag(C.Carry, IsSet(C.Carry) || sum > byte.MaxValue);
        A = Nz((byte)sum);
    }

    private ushort ReadTransferRegister(int register) => register switch
    {
        0 => D, 1 => X, 2 => Y, 3 => U, 4 => S, 5 => PC,
        8 => (ushort)(C.WordHighByte | A), 9 => (ushort)(C.WordHighByte | B),
        10 => (ushort)(C.WordHighByte | CC), 11 => (ushort)(C.WordHighByte | DP),
        _ => ushort.MaxValue
    };

    private void WriteTransferRegister(int register, ushort value)
    {
        switch (register)
        {
            case 0: D = value; break;
            case 1: X = value; break;
            case 2: Y = value; break;
            case 3: U = value; break;
            case 4: S = value; _stackInitialized = true; break;
            case 5: PC = value; break;
            case 8: A = (byte)value; break;
            case 9: B = (byte)value; break;
            case 10: CC = (byte)value; break;
            case 11: DP = (byte)value; break;
        }
    }

    private void Transfer(bool exchange)
    {
        var post = Fetch();
        var source = post >> 4;
        var destination = post & C.NibbleMask;
        var first = ReadTransferRegister(source);
        var second = ReadTransferRegister(destination);
        WriteTransferRegister(destination, first);
        if (exchange) WriteTransferRegister(source, second);
    }
}
