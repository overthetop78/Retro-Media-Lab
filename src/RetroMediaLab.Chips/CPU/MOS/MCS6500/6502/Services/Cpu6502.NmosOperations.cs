using C = RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Constants.Cpu6502Constants;
using Op = RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Enums.Cpu6502Operation;
using Mode = RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Enums.Cpu6502AddressMode;

namespace RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Services;

public sealed partial class Cpu6502
{
    private void ApplyNmosRead(byte value)
    {
        switch (_nmosInstruction.Operation)
        {
            case Op.Ora: _a = Nz((byte)(_a | value)); break;
            case Op.And: _a = Nz((byte)(_a & value)); break;
            case Op.Eor: _a = Nz((byte)(_a ^ value)); break;
            case Op.Adc: Add(value); break;
            case Op.Sbc: Subtract(value); break;
            case Op.Lda: _a = Nz(value); break;
            case Op.Ldx: _x = Nz(value); break;
            case Op.Ldy: _y = Nz(value); break;
            case Op.Cmp: Compare(_a, value); break;
            case Op.Cpx: Compare(_x, value); break;
            case Op.Cpy: Compare(_y, value); break;
            case Op.Bit: Bit(value); break;
            case Op.Lax: _a = _x = Nz(value); break;
            case Op.Las: _a = _x = _s = Nz((byte)(value & _s)); break;
            case Op.Anc: _a = Nz((byte)(_a & value)); Flag(Carry, (_a & Negative) != 0); break;
            case Op.Alr:
                var masked = _a & value;
                Flag(Carry, (masked & Carry) != 0);
                _a = Nz((byte)(masked >> 1));
                break;
            case Op.Arr: Arr(value); break;
            case Op.Xaa: _a = Nz((byte)((_a | C.UnstableBusMask) & _x & value)); break;
            case Op.Lxa: _a = _x = Nz((byte)((_a | C.UnstableBusMask) & value)); break;
            case Op.Axs:
                var difference = (_a & _x) - value;
                Flag(Carry, difference >= 0);
                _x = Nz((byte)difference);
                break;
            case Op.Nop: break;
            default: throw new InvalidOperationException(nameof(_nmosInstruction.Operation));
        }
    }

    private byte ModifyNmosValue(byte value)
    {
        var carry = _p & Carry;
        switch (_nmosInstruction.Operation)
        {
            case Op.Asl or Op.Slo:
                Flag(Carry, (value & Negative) != 0);
                return (byte)(value << 1);
            case Op.Rol or Op.Rla:
                Flag(Carry, (value & Negative) != 0);
                return (byte)((value << 1) | carry);
            case Op.Lsr or Op.Sre:
                Flag(Carry, (value & Carry) != 0);
                return (byte)(value >> 1);
            case Op.Ror or Op.Rra:
                Flag(Carry, (value & Carry) != 0);
                return (byte)((value >> 1) | carry << 7);
            case Op.Dec or Op.Dcp: return (byte)(value - 1);
            case Op.Inc or Op.Isc: return (byte)(value + 1);
            default: throw new InvalidOperationException(nameof(_nmosInstruction.Operation));
        }
    }

    private void ApplyNmosModified(byte value)
    {
        switch (_nmosInstruction.Operation)
        {
            case Op.Slo: _a = Nz((byte)(_a | value)); break;
            case Op.Rla: _a = Nz((byte)(_a & value)); break;
            case Op.Sre: _a = Nz((byte)(_a ^ value)); break;
            case Op.Rra: Add(value); break;
            case Op.Dcp: Compare(_a, value); break;
            case Op.Isc: Subtract(value); break;
            default: Nz(value); break;
        }
    }

    private void ApplyNmosImplied()
    {
        if (_nmosInstruction.Mode == Mode.Accumulator)
        {
            _a = Nz(ModifyNmosValue(_a));
            return;
        }
        switch (_nmosInstruction.Operation)
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
            default: throw new InvalidOperationException(nameof(_nmosInstruction.Operation));
        }
    }

    private bool NmosBranchTaken => _nmosInstruction.Operation switch
    {
        Op.Bpl => (_p & Negative) == 0,
        Op.Bmi => (_p & Negative) != 0,
        Op.Bvc => (_p & Overflow) == 0,
        Op.Bvs => (_p & Overflow) != 0,
        Op.Bcc => (_p & Carry) == 0,
        Op.Bcs => (_p & Carry) != 0,
        Op.Bne => (_p & Zero) == 0,
        Op.Beq => (_p & Zero) != 0,
        _ => throw new InvalidOperationException(nameof(_nmosInstruction.Operation))
    };

    private void Arr(byte operand)
    {
        var masked = (byte)(_a & operand);
        var rotated = Nz((byte)((masked >> 1) | (_p & Carry) << 7));
        Flag(Overflow, ((rotated ^ (rotated << 1)) & 0x40) != 0);
        if ((_p & Decimal) == 0)
        {
            Flag(Carry, (rotated & 0x40) != 0);
            _a = rotated;
            return;
        }
        _a = rotated;
        if ((masked & 15) + (masked & 1) > 5) _a = (byte)((_a & 0xf0) | ((_a + 6) & 15));
        var carry = (masked & 0xf0) + (masked & 0x10) > 0x50;
        if (carry) _a = (byte)(_a + 0x60);
        Flag(Carry, carry);
    }
}
