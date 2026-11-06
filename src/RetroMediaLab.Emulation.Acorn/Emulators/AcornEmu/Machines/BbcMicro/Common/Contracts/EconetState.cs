using RetroMediaLab.Chips.Network.Motorola.M6800.Chip6854.Contracts;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Contracts;

internal sealed record EconetState
{
    public required bool Enabled { get; init; }
    public required byte Station { get; init; }
    public required Mc6854State Controller { get; init; }
    public required bool NmiEnabled { get; init; }
    public required bool Driving { get; init; }
    public required bool ClockPresent { get; init; }
    public required bool ClearToSend { get; init; }
    public required bool Idle { get; init; }
    public required bool ControllerIrq { get; init; }
    public required bool Nmi { get; init; }
    public required bool Rts { get; init; }
    public required bool Transmitting { get; init; }
}