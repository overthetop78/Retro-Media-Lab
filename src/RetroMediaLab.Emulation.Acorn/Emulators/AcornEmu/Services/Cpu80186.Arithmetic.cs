namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;

internal sealed partial class Cpu80186
{
    private int SetResult(int value, bool word)
    {
        value &= word ? 0xffff : 0xff;
        Flag(Zero, value == 0); Flag(Sign, (value & (word ? 0x8000 : 0x80)) != 0);
        Flag(Parity, (System.Numerics.BitOperations.PopCount((uint)(value & 255)) & 1) == 0);
        return value;
    }
    private int Alu(int operation, int left, int right, bool word)
    {
        var carry = Has(Carry) ? 1 : 0;
        var sign = word ? 0x8000 : 0x80; var mask = word ? 0xffff : 255;
        int result;
        switch (operation)
        {
            case 0: case 2:
                result = left + right + (operation == 2 ? carry : 0);
                Flag(Carry, result > mask); Flag(Overflow, (~(left ^ right) & (left ^ result) & sign) != 0);
                Flag(Auxiliary, ((left ^ right ^ result) & 16) != 0); break;
            case 3: case 5: case 7:
                result = left - right - (operation == 3 ? carry : 0);
                Flag(Carry, result < 0); Flag(Overflow, ((left ^ right) & (left ^ result) & sign) != 0);
                Flag(Auxiliary, ((left ^ right ^ result) & 16) != 0); break;
            default:
                result = operation switch { 1 => left | right, 4 => left & right, _ => left ^ right };
                Flag(Carry, false); Flag(Overflow, false); Flag(Auxiliary, false); break;
        }
        return SetResult(result, word);
    }
    private int Increment(int value, bool decrement, bool word)
    {
        var carry = Has(Carry); var result = Alu(decrement ? 5 : 0, value, 1, word); Flag(Carry, carry); return result;
    }
    private bool Condition(int condition) => condition switch
    {
        0 => Has(Overflow), 1 => !Has(Overflow), 2 => Has(Carry), 3 => !Has(Carry),
        4 => Has(Zero), 5 => !Has(Zero), 6 => Has(Carry) || Has(Zero), 7 => !Has(Carry) && !Has(Zero),
        8 => Has(Sign), 9 => !Has(Sign), 10 => Has(Parity), 11 => !Has(Parity),
        12 => Has(Sign) != Has(Overflow), 13 => Has(Sign) == Has(Overflow),
        14 => Has(Zero) || Has(Sign) != Has(Overflow), _ => !Has(Zero) && Has(Sign) == Has(Overflow)
    };
    private int Shift(int operation, int value, int count, bool word)
    {
        count &= 31; if (count == 0) return value;
        var bits = word ? 16 : 8; var mask = word ? 0xffff : 255; var sign = 1 << (bits - 1);
        var old = value;
        for (var i = 0; i < count; i++)
        {
            var carry = Has(Carry);
            switch (operation)
            {
                case 0: Flag(Carry, (value & sign) != 0); value = ((value << 1) | (Has(Carry) ? 1 : 0)) & mask; break;
                case 1: Flag(Carry, (value & 1) != 0); value = (value >> 1) | (Has(Carry) ? sign : 0); break;
                case 2: Flag(Carry, (value & sign) != 0); value = ((value << 1) | (carry ? 1 : 0)) & mask; break;
                case 3: Flag(Carry, (value & 1) != 0); value = (value >> 1) | (carry ? sign : 0); break;
                case 4: case 6: Flag(Carry, (value & sign) != 0); value = (value << 1) & mask; break;
                case 5: Flag(Carry, (value & 1) != 0); value >>= 1; break;
                case 7: Flag(Carry, (value & 1) != 0); value = (value >> 1) | (value & sign); break;
            }
        }
        if (operation >= 4) SetResult(value, word);
        if (count == 1) Flag(Overflow, operation switch
        {
            0 or 2 or 4 or 6 => ((value & sign) != 0) != Has(Carry),
            1 or 3 => ((value & sign) != 0) != ((value & (sign >> 1)) != 0),
            5 => (old & sign) != 0, _ => false
        });
        return value;
    }
    private void MultiplyDivide(int operation, int operand, bool word)
    {
        if (operation is 4 or 5)
        {
            long product = operation == 4 ? (long)(word ? _registers[Ax] : ByteRegister(Ax)) * operand
                : (long)(word ? (short)_registers[Ax] : (sbyte)ByteRegister(Ax)) * (word ? (short)operand : (sbyte)operand);
            if (word) { _registers[Ax] = (ushort)product; _registers[Dx] = (ushort)(product >> 16); }
            else _registers[Ax] = (ushort)product;
            var truncated = word ? (short)(ushort)product : (sbyte)(byte)product;
            var overflow = operation == 4 ? product > (word ? 65535 : 255) : product != truncated;
            Flag(Carry, overflow); Flag(Overflow, overflow); return;
        }
        if (operand == 0) { Interrupt(0, true); return; }
        if (operation == 6)
        {
            var dividend = word ? ((ulong)_registers[Dx] << 16) | _registers[Ax] : _registers[Ax];
            var quotient = dividend / (uint)operand; var remainder = dividend % (uint)operand;
            if (quotient > (word ? 65535ul : 255ul)) { Interrupt(0, true); return; }
            if (word) { _registers[Ax] = (ushort)quotient; _registers[Dx] = (ushort)remainder; }
            else { SetByteRegister(Ax, (int)quotient); SetByteRegister(4, (int)remainder); }
        }
        else
        {
            long dividend = word ? (int)(((uint)_registers[Dx] << 16) | _registers[Ax]) : (short)_registers[Ax];
            long divisor = word ? (short)operand : (sbyte)operand;
            var quotient = dividend / divisor; var remainder = dividend % divisor;
            if (quotient < (word ? -32768 : -128) || quotient > (word ? 32767 : 127)) { Interrupt(0, true); return; }
            if (word) { _registers[Ax] = (ushort)quotient; _registers[Dx] = (ushort)remainder; }
            else { SetByteRegister(Ax, (int)quotient); SetByteRegister(4, (int)remainder); }
        }
    }
}
