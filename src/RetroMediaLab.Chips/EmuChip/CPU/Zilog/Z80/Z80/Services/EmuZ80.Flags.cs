using System.Numerics;
using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Enums;
using F = RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Constants.Z80Flags;

namespace RetroMediaLab.Chips.EmuChip.CPU.Zilog.Z80.Z80.Services;

public sealed partial class EmuZ80
{
    private void WriteFlags(byte flags)
    {
        _registers.F = flags;
        _instructionFlagsModified = true;
    }

    private static byte Parity(byte value) => (BitOperations.PopCount((uint)value) & 1) == 0 ? F.ParityOverflow : (byte)0;

    private void TestBit(byte value, byte xy)
    {
        int mask = 1 << ((_opcode >> 3) & 7);
        int tested = value & mask;
        WriteFlags((byte)((_registers.F & F.Carry) | F.HalfCarry | (xy & F.ResultBits)
            | (tested == 0 ? F.Zero | F.ParityOverflow : tested & F.Sign)));
    }

    private void DecimalAdjust()
    {
        byte before = _registers.A;
        bool subtract = (_registers.F & F.Subtract) != 0;
        bool carry = (_registers.F & F.Carry) != 0;
        int correction = ((_registers.F & F.HalfCarry) != 0 || (before & 0x0f) > 9) ? 6 : 0;
        if (carry || before > 0x99) { correction |= 0x60; carry = true; }
        _registers.A = unchecked((byte)(subtract ? before - correction : before + correction));
        byte result = _registers.A;
        WriteFlags((byte)((result & (F.Sign | F.ResultBits)) | (result == 0 ? F.Zero : 0)
            | Parity(result) | ((before ^ result) & F.HalfCarry)
            | (subtract ? F.Subtract : 0) | (carry ? F.Carry : 0)));
        RetireInstruction();
    }

    private void SetComplementCarry(bool complement)
    {
        byte old = _registers.F;
        int xy = _registers.A;
        if (_profile.CarryFlagSource == Z80CarryFlagSource.AccumulatorWithQHistory) xy |= old & ~_registers.Q;
        int carry = complement ? (old ^ F.Carry) & F.Carry : F.Carry;
        WriteFlags((byte)((old & (F.Sign | F.Zero | F.ParityOverflow)) | (xy & F.ResultBits)
            | (complement && (old & F.Carry) != 0 ? F.HalfCarry : 0) | carry));
        RetireInstruction();
    }
}
