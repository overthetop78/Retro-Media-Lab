namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Contracts;

internal sealed record SidewaysRamState(int Selected, byte[]?[] RamBanks, bool[] Protected);
