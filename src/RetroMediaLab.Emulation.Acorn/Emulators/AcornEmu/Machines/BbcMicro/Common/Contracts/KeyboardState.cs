namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Contracts;

internal sealed record KeyboardState(ushort ActiveColumns, bool[] Matrix, bool BreakPressed);
