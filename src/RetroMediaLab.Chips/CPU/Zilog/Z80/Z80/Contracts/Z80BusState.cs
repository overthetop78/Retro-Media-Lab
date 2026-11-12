using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Enums;
using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Models;

namespace RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Contracts;

/// <summary>État local du séquenceur de transactions, y compris les propagations non résolues.</summary>
public sealed record Z80BusState(Z80BusTiming Timing, long Time, long LastClockEdge,
    long LastRisingEdge, long RisingClocks, long CompletedCycles, bool ClockHigh,
    bool Active, bool Waiting, bool WaitAsserted, int Phase, Z80BusCycle Cycle,
    ushort Address, ushort RefreshAddress, byte WriteData, byte InputData,
    byte SampledData, bool HasSample, int[] Values, int[] Targets, long[] Deadlines,
    bool HoldWriteData = false);
