using Mode = RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Enums.Cpu6502AddressMode;
using Op = RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Enums.Cpu6502Operation;

namespace RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Services;

public partial class Cpu6502
{
    private readonly struct CmosInstruction(Op operation, Mode mode, byte repeatedCycles = 0)
    {
        public readonly Op Operation = operation;
        public readonly Mode Mode = mode;
        public readonly byte RepeatedCycles = repeatedCycles;
        public readonly bool Stores = operation is Op.Sta or Op.Stx or Op.Sty or Op.Stz;
        public readonly bool Modifies = operation is Op.Asl or Op.Lsr or Op.Rol or Op.Ror or Op.Inc or Op.Dec or Op.Tsb or Op.Trb;
    }

    private static class CmosDecode
    {
        internal static readonly CmosInstruction[] Instructions = CreateCmosInstructions();
    }

    private static CmosInstruction[] CreateCmosInstructions()
    {
        var instructions = new CmosInstruction[byte.MaxValue + 1];
        for (var opcode = 0; opcode < instructions.Length; opcode++)
        {
            var common = NmosInstructions[opcode];
            instructions[opcode] = new(common.Operation, common.Mode);
            // GTE's unused x3/x7/xB/xF encodings are single-cycle NOPs, not NMOS combinations.
            if ((opcode & 3) == 3) instructions[opcode] = new(Op.Nop, Mode.NopOneCycle);
        }
        foreach (var opcode in new byte[] { 0x02, 0x22, 0x42, 0x62, 0x82, 0xc2, 0xe2 })
            instructions[opcode] = new(Op.Nop, Mode.NopImmediate);
        instructions[0x44] = new(Op.Nop, Mode.NopRepeatedOperand, 1);
        foreach (var opcode in new byte[] { 0x54, 0xd4, 0xf4 })
            instructions[opcode] = new(Op.Nop, Mode.NopRepeatedOperand, 2);
        instructions[0x5c] = new(Op.Nop, Mode.NopRepeatedOperand, 5);
        instructions[0xdc] = instructions[0xfc] = new(Op.Nop, Mode.NopAbsolute);
        instructions[0x04] = new(Op.Tsb, Mode.ZeroPage);
        instructions[0x0c] = new(Op.Tsb, Mode.Absolute);
        instructions[0x14] = new(Op.Trb, Mode.ZeroPage);
        instructions[0x1c] = new(Op.Trb, Mode.Absolute);
        instructions[0x1a] = new(Op.Inc, Mode.Accumulator);
        instructions[0x3a] = new(Op.Dec, Mode.Accumulator);
        instructions[0x5a] = new(Op.Phy, Mode.Implied);
        instructions[0x7a] = new(Op.Ply, Mode.Implied);
        instructions[0xda] = new(Op.Phx, Mode.Implied);
        instructions[0xfa] = new(Op.Plx, Mode.Implied);
        instructions[0x64] = new(Op.Stz, Mode.ZeroPage);
        instructions[0x74] = new(Op.Stz, Mode.ZeroPageX);
        instructions[0x9c] = new(Op.Stz, Mode.Absolute);
        instructions[0x9e] = new(Op.Stz, Mode.AbsoluteX);
        instructions[0x80] = new(Op.Bra, Mode.Relative);
        instructions[0x89] = new(Op.BitImmediate, Mode.Immediate);
        instructions[0x34] = new(Op.Bit, Mode.ZeroPageX);
        instructions[0x3c] = new(Op.Bit, Mode.AbsoluteX);
        instructions[0x7c] = new(Op.Jmp, Mode.AbsoluteIndexedIndirect);
        instructions[0x12] = new(Op.Ora, Mode.ZeroPageIndirect);
        instructions[0x32] = new(Op.And, Mode.ZeroPageIndirect);
        instructions[0x52] = new(Op.Eor, Mode.ZeroPageIndirect);
        instructions[0x72] = new(Op.Adc, Mode.ZeroPageIndirect);
        instructions[0x92] = new(Op.Sta, Mode.ZeroPageIndirect);
        instructions[0xb2] = new(Op.Lda, Mode.ZeroPageIndirect);
        instructions[0xd2] = new(Op.Cmp, Mode.ZeroPageIndirect);
        instructions[0xf2] = new(Op.Sbc, Mode.ZeroPageIndirect);
        return instructions;
    }
}
