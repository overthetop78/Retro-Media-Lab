namespace RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Enums;

public enum Cpu6502AddressMode : byte
{
    Implied,
    Accumulator,
    Immediate,
    ZeroPage,
    ZeroPageX,
    ZeroPageY,
    Absolute,
    AbsoluteX,
    AbsoluteY,
    Indirect,
    IndexedIndirect,
    IndirectIndexed,
    Relative,
}
