using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Contracts;
using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Models;
using RetroMediaLab.Chips.Logic.Common.Contracts;

namespace RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Interfaces;

/// <summary>Contrat du cœur Z80 en cours de réalisation. Les fonctions de contrôle encore
/// absentes sont suivies dans la fiche Z80 ; ce contrat ne déclare pas la puce terminée.</summary>
public interface ICpuZ80
{
    Z80HardwareProfile HardwareProfile { get; }
    Z80Registers Registers { get; }
    Z80InterruptState InterruptState { get; }
    Z80BusPins Pins { get; }
    long TimeNanoseconds { get; }
    long InputClockCount { get; }
    long InstructionsCompleted { get; }
    ushort InstructionAddress { get; }
    bool AtInstructionBoundary { get; }
    bool IsFaulted { get; }
    long? NextEventNanoseconds { get; }
    Action<Z80BusPins>? PinsChanged { get; set; }
    Action<long?>? NextEventNanosecondsChanged { get; set; }
    void SetData(byte data);
    void SetWait(bool asserted);
    bool IsHalted { get; }
    bool IsBusGranted { get; }
    void SetInt(bool asserted);
    void SetNmi(bool asserted);
    void SetReset(bool asserted);
    void SetBusRequest(bool asserted);
    void SetClock(bool high);
    void ReceiveClockEdges(long firstEdgeNanoseconds, long halfPeriodNanoseconds, int edgeCount);
    void ReceiveClockSource(RationalClockSource source);
    int AdvanceClockSourceUntil(long boundaryExclusive);
    void LimitClockSourceBefore(long boundaryExclusive);
    void AdvanceTo(long nanoseconds);
    CpuZ80State CaptureState();
    void RestoreState(CpuZ80State state);
}
