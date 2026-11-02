using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Mos6502.Constants.Cpu6502Constants;
using Op = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Mos6502.Enums.Cpu6502Operation;
using Mode = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Mos6502.Enums.Cpu6502AddressMode;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Mos6502.Services;

internal sealed partial class Cpu6502
{
    private void ApplyNmosRead(byte value)
    {
        switch (_nmosInstruction.Operation)
        {
            case Op.Ora: A = Nz((byte)(A | value)); break;
            case Op.And: A = Nz((byte)(A & value)); break;
            case Op.Eor: A = Nz((byte)(A ^ value)); break;
            case Op.Adc: Add(value); break;
            case Op.Sbc: Subtract(value); break;
            case Op.Lda: A = Nz(value); break;
            case Op.Ldx: X = Nz(value); break;
            case Op.Ldy: Y = Nz(value); break;
            case Op.Cmp: Compare(A, value); break;
            case Op.Cpx: Compare(X, value); break;
            case Op.Cpy: Compare(Y, value); break;
            case Op.Bit: Bit(value); break;
            case Op.Lax: A = X = Nz(value); break;
            case Op.Las: A = X = S = Nz((byte)(value & S)); break;
            case Op.Anc: A = Nz((byte)(A & value)); Flag(Carry, (A & Negative) != 0); break;
            case Op.Alr:
                var masked = A & value;
                Flag(Carry, (masked & Carry) != 0);
                A = Nz((byte)(masked >> 1));
                break;
            case Op.Arr: Arr(value); break;
            case Op.Xaa: A = Nz((byte)((A | C.UnstableBusMask) & X & value)); break;
            case Op.Lxa: A = X = Nz((byte)((A | C.UnstableBusMask) & value)); break;
            case Op.Axs:
                var difference = (A & X) - value;
                Flag(Carry, difference >= 0);
                X = Nz((byte)difference);
                break;
            case Op.Nop: break;
            default: throw new InvalidOperationException(nameof(_nmosInstruction.Operation));
        }
    }

    private byte ModifyNmosValue(byte value)
    {
        var carry = P & Carry;
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
            case Op.Slo: A = Nz((byte)(A | value)); break;
            case Op.Rla: A = Nz((byte)(A & value)); break;
            case Op.Sre: A = Nz((byte)(A ^ value)); break;
            case Op.Rra: Add(value); break;
            case Op.Dcp: Compare(A, value); break;
            case Op.Isc: Subtract(value); break;
            default: Nz(value); break;
        }
    }

    private void ApplyNmosImplied()
    {
        if (_nmosInstruction.Mode == Mode.Accumulator)
        {
            A = Nz(ModifyNmosValue(A));
            return;
        }
        switch (_nmosInstruction.Operation)
        {
            case Op.Tax: X = Nz(A); break;
            case Op.Tay: Y = Nz(A); break;
            case Op.Txa: A = Nz(X); break;
            case Op.Tya: A = Nz(Y); break;
            case Op.Tsx: X = Nz(S); break;
            case Op.Txs: S = X; break;
            case Op.Dex: X = Nz((byte)(X - 1)); break;
            case Op.Dey: Y = Nz((byte)(Y - 1)); break;
            case Op.Inx: X = Nz((byte)(X + 1)); break;
            case Op.Iny: Y = Nz((byte)(Y + 1)); break;
            case Op.Clc: P &= unchecked((byte)~Carry); break;
            case Op.Sec: P |= Carry; break;
            case Op.Cli: P &= unchecked((byte)~InterruptDisable); break;
            case Op.Sei: P |= InterruptDisable; break;
            case Op.Clv: P &= unchecked((byte)~Overflow); break;
            case Op.Cld: P &= unchecked((byte)~Decimal); break;
            case Op.Sed: P |= Decimal; break;
            case Op.Nop: break;
            default: throw new InvalidOperationException(nameof(_nmosInstruction.Operation));
        }
    }

    private bool NmosBranchTaken => _nmosInstruction.Operation switch
    {
        Op.Bpl => (P & Negative) == 0,
        Op.Bmi => (P & Negative) != 0,
        Op.Bvc => (P & Overflow) == 0,
        Op.Bvs => (P & Overflow) != 0,
        Op.Bcc => (P & Carry) == 0,
        Op.Bcs => (P & Carry) != 0,
        Op.Bne => (P & Zero) == 0,
        Op.Beq => (P & Zero) != 0,
        _ => throw new InvalidOperationException(nameof(_nmosInstruction.Operation))
    };

    private void Arr(byte operand)
    {
        var masked = (byte)(A & operand);
        var rotated = Nz((byte)((masked >> 1) | (P & Carry) << 7));
        Flag(Overflow, ((rotated ^ (rotated << 1)) & 0x40) != 0);
        if ((P & Decimal) == 0)
        {
            Flag(Carry, (rotated & 0x40) != 0);
            A = rotated;
            return;
        }
        A = rotated;
        if ((masked & 15) + (masked & 1) > 5) A = (byte)((A & 0xf0) | ((A + 6) & 15));
        var carry = (masked & 0xf0) + (masked & 0x10) > 0x50;
        if (carry) A = (byte)(A + 0x60);
        Flag(Carry, carry);
    }
}
