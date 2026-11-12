namespace RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Contracts;

/// <summary>Received stability of WAIT and control inputs, exclusive of their
/// next possible response date. Cycle bits use the ordinals of Z80BusCycle.
/// The wiring must include every CPU cycle capable of changing these inputs;
/// such cycles invalidate the promise through their last output propagation.
/// DATA is never covered. This is transport metadata, not saved CPU state.</summary>
public readonly record struct Z80BusInputWindow(long WaitBefore, long ControlBefore,
    uint WaitDependentCycles, uint ControlDependentCycles);

public readonly record struct Z80BusRangeStatistics(long HeldWaitSamples, long HeldControlSamples);
