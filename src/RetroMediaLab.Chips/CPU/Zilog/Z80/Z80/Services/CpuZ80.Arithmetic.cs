using F = RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Constants.Z80Flags;

namespace RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Services;

public sealed partial class CpuZ80
{
    // UM008011-0816 pp.145–172, 175–178, 194–201 ; Young v0.91 §§2.3,8.4–8.7.
    // Les helpers fournissent les six flags documentés. Le décodeur ajoute F3/F5
    // depuis leur cause : résultat, sauf CP qui les reçoit de l'opérande.
    private void ExecuteArithmetic(byte operand)
    {
        byte kind = (byte)((_opcode >> 3) & 7);
        bool carry = (_registers.F & F.Carry) != 0;
        byte result, flags;
        switch (kind)
        {
            case 0: result = Z80Arithmetic.Add8(_registers.A, operand, false, out flags); break;
            case 1: result = Z80Arithmetic.Add8(_registers.A, operand, carry, out flags); break;
            case 2: result = Z80Arithmetic.Subtract8(_registers.A, operand, false, out flags); break;
            case 3: result = Z80Arithmetic.Subtract8(_registers.A, operand, carry, out flags); break;
            case 4: result = Z80Arithmetic.And8(_registers.A, operand, out flags); break;
            case 5: result = Z80Arithmetic.Xor8(_registers.A, operand, out flags); break;
            case 6: result = Z80Arithmetic.Or8(_registers.A, operand, out flags); break;
            default: result = Z80Arithmetic.Subtract8(_registers.A, operand, false, out flags); break;
        }
        WriteFlags((byte)(flags | ((kind == 7 ? operand : result) & F.ResultBits)));
        if (kind != 7) _registers.A = result;
        RetireInstruction();
    }

    private void IncrementDecrementRegister(byte opcode)
    {
        byte code = (byte)((opcode >> 3) & 7);
        byte value = GetRegister(code);
        byte result, flags;
        if ((opcode & 1) == 0) result = Z80Arithmetic.Increment8(value, _registers.F, out flags);
        else result = Z80Arithmetic.Decrement8(value, _registers.F, out flags);
        SetRegister(code, result);
        WriteFlags((byte)(flags | (result & F.ResultBits)));
        RetireInstruction();
    }

    private void RotateAccumulator(byte opcode)
    {
        int value = _registers.A;
        int oldCarry = _registers.F & F.Carry;
        int carry, result;
        switch (opcode)
        {
            case 0x07: carry = value >> 7; result = (value << 1) | carry; break;
            case 0x0f: carry = value & 1; result = (value >> 1) | (carry << 7); break;
            case 0x17: carry = value >> 7; result = (value << 1) | oldCarry; break;
            default: carry = value & 1; result = (value >> 1) | (oldCarry << 7); break;
        }
        _registers.A = unchecked((byte)result);
        WriteFlags((byte)((_registers.F & (F.Sign | F.Zero | F.ParityOverflow))
            | (result & F.ResultBits) | carry));
        RetireInstruction();
    }

    private void ComplementAccumulator()
    {
        _registers.A = (byte)~_registers.A;
        WriteFlags((byte)((_registers.F & (F.Sign | F.Zero | F.ParityOverflow | F.Carry))
            | F.HalfCarry | F.Subtract | (_registers.A & F.ResultBits)));
        RetireInstruction();
    }

    private void NegateAccumulator()
    {
        _registers.A = Z80Arithmetic.Subtract8(0, _registers.A, false, out byte flags);
        WriteFlags((byte)(flags | (_registers.A & F.ResultBits)));
        RetireInstruction();
    }
}
