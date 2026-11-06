using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Contracts;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Contracts;

internal sealed record DisksState(bool Enabled, bool Wd1770, byte Control, bool Nmi, int[] Speeds,
    Intel8271DriveState? IntelDrive, Wd1770DriveState? WdDrive);
