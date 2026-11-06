using RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Enums;

namespace RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Contracts;

/// <summary>État du VIA, sans callbacks ni référence au propriétaire des ports.</summary>
public sealed record Via6522State
{
    public required int FormatVersion { get; init; }
    public required byte[] Registers { get; init; }
    public required bool Phi2High { get; init; }
    public required int Timer1 { get; init; }
    public required int Timer2 { get; init; }
    public required int Latch1 { get; init; }
    public required bool Timer1Running { get; init; }
    public required bool Timer2Running { get; init; }
    public required bool Timer2Armed { get; init; }
    public required byte InterruptFlags { get; init; }
    public required byte InterruptEnabled { get; init; }
    public required bool Irq { get; init; }
    public required bool Ca1 { get; init; }
    public required bool Ca2 { get; init; }
    public required bool Cb1 { get; init; }
    public required bool Cb2 { get; init; }
    public required bool Ca2Output { get; init; }
    public required bool Cb2Output { get; init; }
    public required int Ca2Pulse { get; init; }
    public required int Cb2Pulse { get; init; }
    public required byte InputLatchA { get; init; }
    public required byte InputLatchB { get; init; }
    public required bool? Pb6Input { get; init; }
    public required bool LastPb6 { get; init; }
    public required ViaControlMode ControlModeA { get; init; }
    public required ViaControlMode ControlModeB { get; init; }
    public required bool ShiftActive { get; init; }
    public required bool ShiftArmed { get; init; }
    public required bool ShiftClock { get; init; }
    public required bool ShiftClockPin { get; init; }
    public required bool ShiftOutputComplete { get; init; }
    public required bool ShiftFinishOutputPending { get; init; }
    public required int ShiftBits { get; init; }
    public required int ShiftClockDelay { get; init; }
    public required bool ExternalShiftPending { get; init; }
    public required bool InternalShiftPending { get; init; }
    public required bool InternalShiftValue { get; init; }
    public required bool ShiftInterruptPending { get; init; }
    public required bool ShiftLeadingPending { get; init; }
    public required ViaShiftMode ShiftMode { get; init; }
    public required bool ShiftOutput { get; init; }
    public required bool ShiftUsesTimer { get; init; }
    public required bool ShiftInternalClock { get; init; }
    public required bool ShiftSystemClock { get; init; }
    public required bool Timer1Armed { get; init; }
    public required bool Timer1Reload { get; init; }
    public required int Timer1LoadDelay { get; init; }
    public required int Timer2LoadDelay { get; init; }
    public required bool Timer2Reload { get; init; }
    public required bool TimerPb7 { get; init; }
    public required bool Timer1JustFired { get; init; }
    public required bool Timer2JustFired { get; init; }
    public required byte TimerInterruptsPending { get; init; }
    public required bool Timer2PulseSampled { get; init; }
    public required bool Timer1Continuous { get; init; }
    public required bool Timer1Output { get; init; }
    public required bool Timer2PulseMode { get; init; }
}
