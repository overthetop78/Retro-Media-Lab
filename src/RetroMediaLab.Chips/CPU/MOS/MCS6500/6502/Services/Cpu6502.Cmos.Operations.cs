using Mode = RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Enums.Cpu6502AddressMode;
using Op = RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Enums.Cpu6502Operation;

namespace RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Services;

public partial class Cpu6502
{
    private void ApplyCmosRead(byte value)
    {
        switch (_cmosInstruction.Operation)
        {
            case Op.Ora: _a = Nz((byte)(_a | value)); break;
            case Op.And: _a = Nz((byte)(_a & value)); break;
            case Op.Eor: _a = Nz((byte)(_a ^ value)); break;
            case Op.Adc: CmosAdd(value); break;
            case Op.Sbc: CmosSubtract(value); break;
            case Op.Lda: _a = Nz(value); break;
            case Op.Ldx: _x = Nz(value); break;
            case Op.Ldy: _y = Nz(value); break;
            case Op.Cmp: Compare(_a, value); break;
            case Op.Cpx: Compare(_x, value); break;
            case Op.Cpy: Compare(_y, value); break;
            case Op.Bit: Bit(value); break;
            case Op.BitImmediate: Flag(Zero, (_a & value) == 0); break;
            case Op.Nop: break;
            default: throw new InvalidOperationException(nameof(_cmosInstruction.Operation));
        }
    }

    private void CmosAdd(byte value)
    {
        var binary = _a + value + (_p & Carry);
        var result = binary;
        if ((_p & Decimal) != 0)
        {
            var low = (_a & 15) + (value & 15) + (_p & Carry);
            if (low > 9) low += 6;
            result = (_a & 0xf0) + (value & 0xf0) + (low & 15) + (low > 15 ? 16 : 0);
        }
        Flag(Overflow, (~(_a ^ value) & (_a ^ result) & Negative) != 0);
        if ((_p & Decimal) != 0 && result >= 0xa0) result += 0x60;
        Flag(Carry, result > byte.MaxValue);
        _a = Nz((byte)result);
    }

    private void CmosSubtract(byte value)
    {
        var borrow = (_p & Carry) == 0 ? 1 : 0;
        var result = _a - value - borrow;
        Flag(Overflow, ((_a ^ value) & (_a ^ result) & Negative) != 0);
        Flag(Carry, result >= 0);
        if ((_p & Decimal) != 0)
        {
            if (result < 0) result -= 0x60;
            if ((_a & 15) - (value & 15) - borrow < 0) result -= 6;
        }
        _a = Nz((byte)result);
    }

    private byte ModifyCmosValue(byte value)
    {
        var carry = _p & Carry;
        switch (_cmosInstruction.Operation)
        {
            case Op.Asl: Flag(Carry, (value & Negative) != 0); return Nz((byte)(value << 1));
            case Op.Lsr: Flag(Carry, (value & Carry) != 0); return Nz((byte)(value >> 1));
            case Op.Rol: Flag(Carry, (value & Negative) != 0); return Nz((byte)(value << 1 | carry));
            case Op.Ror: Flag(Carry, (value & Carry) != 0); return Nz((byte)(value >> 1 | carry << 7));
            case Op.Inc: return Nz((byte)(value + 1));
            case Op.Dec: return Nz((byte)(value - 1));
            case Op.Tsb: Flag(Zero, (_a & value) == 0); return (byte)(value | _a);
            case Op.Trb: Flag(Zero, (_a & value) == 0); return (byte)(value & ~_a);
            default: throw new InvalidOperationException(nameof(_cmosInstruction.Operation));
        }
    }

    private void ApplyCmosImplied()
    {
        if (_cmosInstruction.Mode == Mode.Accumulator) { _a = ModifyCmosValue(_a); return; }
        switch (_cmosInstruction.Operation)
        {
            case Op.Tax: _x = Nz(_a); break;
            case Op.Tay: _y = Nz(_a); break;
            case Op.Txa: _a = Nz(_x); break;
            case Op.Tya: _a = Nz(_y); break;
            case Op.Tsx: _x = Nz(_s); break;
            case Op.Txs: _s = _x; break;
            case Op.Dex: _x = Nz((byte)(_x - 1)); break;
            case Op.Dey: _y = Nz((byte)(_y - 1)); break;
            case Op.Inx: _x = Nz((byte)(_x + 1)); break;
            case Op.Iny: _y = Nz((byte)(_y + 1)); break;
            case Op.Clc: _p &= unchecked((byte)~Carry); break;
            case Op.Sec: _p |= Carry; break;
            case Op.Cli: _p &= unchecked((byte)~InterruptDisable); break;
            case Op.Sei: _p |= InterruptDisable; break;
            case Op.Clv: _p &= unchecked((byte)~Overflow); break;
            case Op.Cld: _p &= unchecked((byte)~Decimal); break;
            case Op.Sed: _p |= Decimal; break;
            case Op.Nop: break;
            default: throw new InvalidOperationException(nameof(_cmosInstruction.Operation));
        }
    }

    private bool CmosBranchTaken => _cmosInstruction.Operation switch
    {
        Op.Bra => true,
        Op.Bpl => (_p & Negative) == 0,
        Op.Bmi => (_p & Negative) != 0,
        Op.Bvc => (_p & Overflow) == 0,
        Op.Bvs => (_p & Overflow) != 0,
        Op.Bcc => (_p & Carry) == 0,
        Op.Bcs => (_p & Carry) != 0,
        Op.Bne => (_p & Zero) == 0,
        Op.Beq => (_p & Zero) != 0,
        _ => throw new InvalidOperationException(nameof(_cmosInstruction.Operation))
    };
}
