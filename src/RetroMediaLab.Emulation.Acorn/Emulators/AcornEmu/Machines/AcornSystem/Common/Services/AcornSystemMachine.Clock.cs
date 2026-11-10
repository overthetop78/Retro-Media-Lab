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
    private long _floppySourceCount = -1;

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
            bus.SetPhase(high);
            if (high) _via!.Phi2LeadingEdge();
            else _via!.Phi2TrailingEdge();
        };
        // Initialize the connected level explicitly, before the first real front.
        _via!.Phi2TrailingEdge();
        control.CpuClockChanged = cpu.SetPhaseZeroInput;
        cpu.SetPhaseZeroInput(control.CpuClockHigh);
        _fastClock!.Clock2MHzChanged = SetFastReferenceClock;
        if (_floppyBoard is not null)
        {
            _fastClock.Clock4MHzChanged = high =>
            {
                AdvanceFloppyToSource();
                _floppyBoard.SetClock4MHz(high);
            };
            // The disk's only CPU path is latched NMI. CPU transfers and VDU
            // character inputs still wait for the2 MHz edge below. No4 MHz
            // recipient reads or modifies the independent dot-clock receiver.
            _fastClock.Clock4MHzCanChangeClock12Inputs = false;
            _floppyBoard.SetClock4MHz(_fastClock.Clock4MHz);
            _fastClock.Clock4MHzPeriodicAvailable = (nextFall, period, high) =>
            {
                // The source owns qualification/phase; each receiver owns its
                // clock processing. This card's real signal has a250 ns period.
                AdvanceFloppyToSource();
                _floppyBoard.ConnectClock4MHz((nextFall - 1) * 125 / 3);
                _fastClock.Clock4MHzChanged = null;
            };
        }
        _fastClock.Clock12MHzReceiver = _video!;
        _video!.StableClockCandidateChanged = high => _fastClock.Clock12MHzCanGroup = high;
        _fastClock.Clock12MHzCanGroup = _video.StableClockCandidate;
        _video.SetDotClock(_fastClock.Clock12MHz);
        _video.SetCharacterClock(_fastClock.Clock2MHz);
        control.SetSelectedClock(_fastClock.Clock2MHz);
    }

    private void SetFastReferenceClock(bool high)
    {
        // Reference time and the free-running cards reach this edge before the
        // CPU captures its transfer. WAIT only stretches PH0/PH2, not this clock.
        AdvanceFloppyToSource();
        _video!.SetCharacterClock(high);
        if (!high) TickReferenceClock(advanceVia: false);
        _fastCpuClock!.SetSelectedClock(high);
    }

    private void ResetFastCpu()
    {
        _cpu6502!.SetReset(true);
        _fastClock!.AdvanceClocks(_fastAdvanceBatch * C.FastResetHoldPeriods);
        _floppyBoard?.SetResetLine(true);
        _cpu6502.SetReset(false);
        _fastClock.AdvanceClocks(_fastAdvanceBatch * C.FastResetSequencePeriods);
    }

    private void AdvanceFloppyToSource()
    {
        if (_floppyBoard is null) return;
        // Rising source period n is at (n-1)/24 MHz. Keep the rational source
        // position; do not accumulate rounded 41 ns periods into a second clock.
        var count = _fastClock!.InputClockCount;
        if (_floppySourceCount == count) return;
        _floppySourceCount = count;
        var target = count == 0 ? 0 : (count - 1) * 125 / 3;
        _floppyBoard.AdvanceTo(target);
    }

    internal void PrepareFastBus(ushort address, byte value = 0)
    {
        if (_floppyBoard is null) return;
        AdvanceFloppyToSource();
        _floppyBoard.SetBusStrobes(true, true);
        _floppyBoard.SetBusAddress(address, value);
    }

    internal byte? FastDiskBusData => _floppyBoard?.BusData;

    internal void SetFastBusPhase(bool high, bool read)
    {
        if (_floppyBoard is null) return;
        AdvanceFloppyToSource();
        _floppyBoard.SetBusStrobes(!high || !read, !high || read);
    }
}
