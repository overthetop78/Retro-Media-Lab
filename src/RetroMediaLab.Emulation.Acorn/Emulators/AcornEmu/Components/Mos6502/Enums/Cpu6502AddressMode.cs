namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Mos6502.Enums;

internal enum Cpu6502AddressMode : byte
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
