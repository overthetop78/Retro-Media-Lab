using RetroMediaLab.Chips.Controllers.Acorn.SystemClock.Services;
using RetroMediaLab.Chips.Controllers.Acorn.SystemCpuClock.Services;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Constants.SystemConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Services;

internal sealed partial class AcornSystemMachine
{
    private readonly SystemClockGenerator? _fastClock;
    private readonly SystemCpuClockControl? _fastCpuClock;
    private readonly SystemFastCpuBus? _fastCpuBus;
    // API batch size only. Physical division and phase remain in the three counters.
    private readonly long _fastAdvanceBatch;

    private void WireFastClock()
    {
        var cpu = _cpu6502!;
        var control = _fastCpuClock!;
        var bus = _fastCpuBus!;
        bus.ReadWriteChanged = control.SetCpuRead;
        control.SetCpuRead(bus.ReadHigh);
        control.ReadyChanged = high =>
        {
            bus.BuffersEnabled = high;
            cpu.SetReady(high);
        };
        bus.BuffersEnabled = control.BusBuffersEnabled;
        cpu.SetReady(control.Ready);
        cpu.SetReset(false);
        cpu.PhaseOutputsChanged = (_, high) =>
        {
            if (high) _via!.Phi2LeadingEdge();
            else _via!.Phi2TrailingEdge();
        };
        // Initialize the connected level explicitly, before the first real front.
        _via!.Phi2TrailingEdge();
        control.CpuClockChanged = cpu.SetPhaseZeroInput;
        cpu.SetPhaseZeroInput(control.CpuClockHigh);
        _fastClock!.Clock2MHzChanged = SetFastReferenceClock;
        _fastClock.Clock12MHzChanged = _video!.SetDotClock;
        _video.SetDotClock(_fastClock.Clock12MHz);
        _video.SetCharacterClock(_fastClock.Clock2MHz);
        control.SetSelectedClock(_fastClock.Clock2MHz);
    }

    private void SetFastReferenceClock(bool high)
    {
        // Reference time and the free-running cards reach this edge before the
        // CPU captures its transfer. WAIT only stretches PH0/PH2, not this clock.
        _video!.SetCharacterClock(high);
        if (!high) TickReferenceClock(advanceVia: false);
        _fastCpuClock!.SetSelectedClock(high);
    }

    private void ResetFastCpu()
    {
        _cpu6502!.SetReset(true);
        _fastClock!.AdvanceClocks(_fastAdvanceBatch * C.FastResetHoldPeriods);
        _cpu6502.SetReset(false);
        _fastClock.AdvanceClocks(_fastAdvanceBatch * C.FastResetSequencePeriods);
    }
}
