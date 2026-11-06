using RetroMediaLab.Chips.IO.Acorn.SerialUla.BbcSerialUla.Contracts;
using RetroMediaLab.Chips.IO.Motorola.M6800.Chip6850.Contracts;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Contracts;

internal sealed record SerialState
{
    public required Acia6850State Acia { get; init; }
    public required BbcSerialUlaState Ula { get; init; }
    public required long OverridePhase { get; init; }
    public required int BaudRateOverride { get; init; }
    public required bool ModemPinsKnown { get; init; }
    public required bool CtsHigh { get; init; }
    public required bool DcdHigh { get; init; }
    public required bool ReceiveData { get; init; }
    public required bool Rs423Selected { get; init; }
    public required int ClockDivider { get; init; }
    public required long OverrideClockStep { get; init; }
}
