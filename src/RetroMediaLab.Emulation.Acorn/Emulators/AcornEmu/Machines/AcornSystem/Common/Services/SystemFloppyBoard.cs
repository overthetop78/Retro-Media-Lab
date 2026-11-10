using RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Interfaces;
using System.Numerics;
using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS00.Models;
using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS00.Services;
using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN7438.Models;
using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN7438.Services;
using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS138.Models;
using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS138.Services;
using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS393.Models;
using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS393.Services;
using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS93.Models;
using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS93.Services;
using RetroMediaLab.Chips.Logic.TexasInstruments.CD4000.CD4020B.Models;
using RetroMediaLab.Chips.Logic.TexasInstruments.CD4000.CD4020B.Services;
using RetroMediaLab.Chips.Logic.TexasInstruments.CD4000.CD4013B.Models;
using RetroMediaLab.Chips.Logic.TexasInstruments.CD4000.CD4013B.Services;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Constants.SystemFloppyBoardConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Services;

/// <summary>Wires of Acorn 400,004 with external4 MHz (System5 LK2).
/// Components own all counters, phases and propagation. The controller is borrowed.</summary>
internal sealed partial class SystemFloppyBoard : IDisposable
{
    private readonly IIntel8271 _fdc;
    private readonly Sn74Ls00 _ic2 = new(Sn74Ls00Timing.Typical), _ic12 = new(Sn74Ls00Timing.Typical);
    private readonly Sn7438 _ic3 = new(Sn7438Timing.Typical), _ic5 = new(Sn7438Timing.Typical), _ic6 = new(Sn7438Timing.Typical);
    private readonly Sn74Ls138 _ic4 = new(Sn74Ls138Timing.Typical);
    private readonly Sn74Ls93 _ic7 = new(C.ControllerClock, C.ControllerClock / 2, 0, Sn74Ls93Timing.CalculatedTypical);
    private readonly Sn74Ls93 _ic14 = new(C.Clock4MHz, 0, 0, Sn74Ls93Timing.CalculatedTypical);
    private readonly Sn74Ls393 _ic8 = new(C.Clock4MHz, C.DividerSecondClock, 0, 0, Sn74Ls393Timing.CalculatedTypical);
    private readonly Cd4020B _ic9 = new(C.ReadyClock, 0, Cd4020BTiming.Typical5Volts);
    private readonly Cd4013B _ic10 = new(C.IndexClock, C.IndexClock, Cd4013BTiming.Typical5Volts),
        _ic11 = new(C.IndexClock, C.IndexClock, Cd4013BTiming.Typical5Volts);
    // Received deadlines of these physical packages and their drive connector. No polling of peers, input FIFO,
    // common emulator scheduler or additional oscillator is introduced here.
    private readonly long[] _deadlines = new long[C.LastPart + 1];
    private uint _activeParts;
    private SystemFloppyDrive? _driveTiming;
    private long _time, _next, _publishedNext;
    private int _nextPart;
    private bool _advancing, _disposed;

    internal SystemFloppyBoard(IIntel8271 controller)
    {
        ArgumentNullException.ThrowIfNull(controller);
        if (controller.InputClockFrequency != C.ControllerClock || controller.ControllerClockFrequency != C.ControllerClock)
            throw new ArgumentException("This board clocks its 8271 from the physical 2 MHz divider output.", nameof(controller));
        _fdc = controller;
        _ic2.NextEventNanosecondsChanged = next => RememberDeadline(C.Control, next);
        _ic3.NextEventNanosecondsChanged = next => RememberDeadline(C.Interrupt, next);
        _ic4.NextEventNanosecondsChanged = next => RememberDeadline(C.Decode, next);
        _ic5.NextEventNanosecondsChanged = next => RememberDeadline(C.Drive, next);
        _ic6.NextEventNanosecondsChanged = next => RememberDeadline(C.Write, next);
        _ic7.NextEventNanosecondsChanged = next => RememberDeadline(C.Separator, next);
        _ic8.NextOutputEventNanosecondsChanged = next => RememberDeadline(C.Divider, next);
        _ic9.NextEventNanosecondsChanged = next => RememberDeadline(C.Timeout, next);
        _ic10.NextEventNanosecondsChanged = next => RememberDeadline(C.Ready1, next);
        _ic11.NextEventNanosecondsChanged = next => RememberDeadline(C.Ready0, next);
        _ic12.NextEventNanosecondsChanged = next => RememberDeadline(C.Latch, next);
        _ic14.NextEventNanosecondsChanged = next => RememberDeadline(C.Predivider, next);
        WireBus(); WireReady(); WireSeparator(); WireDrive();
        _fdc.DriveSignalsChanged += ControllerDriveChanged;
        _fdc.WriteDataChanged += ControllerWriteDataChanged;
        _fdc.NmiChanged += ControllerInterruptChanged;
        try
        {
            InitializeBus(); InitializeReady(); InitializeSeparator();
            ControllerDriveChanged(); ControllerWriteDataChanged(_fdc.WriteData); ControllerInterruptChanged(_fdc.Nmi);
            _fdc.SetPhaseLockedSeparator(false);
            _fdc.SetCount(false); // PL1 COUNT/OP1 is grounded on this card.
            _fdc.SetWriteFault(0, false); _fdc.SetWriteFault(1, false); // Pin28 is pulled high by R8.
        }
        catch { Dispose(); throw; }
    }

    internal long TimeNanoseconds => _time;
    internal long? NextEventNanoseconds => _next == 0 ? null : _next;
    internal Action<long?>? NextEventNanosecondsChanged { get; set; }

    internal void ConnectDriveTiming(SystemFloppyDrive drives)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(drives);
        if (ReferenceEquals(_driveTiming, drives)) return;
        if (_driveTiming is not null) throw new InvalidOperationException("The physical drive connector is already connected.");
        _driveTiming = drives;
        drives.NextEventChanged = DriveEventChanged;
        DriveEventChanged(drives.NextEventNanoseconds);
    }

    internal void DisconnectDriveTiming(SystemFloppyDrive drives)
    {
        if (!ReferenceEquals(_driveTiming, drives)) return;
        drives.NextEventChanged = null;
        _driveTiming = null;
        if (!_disposed) RememberDeadline(C.DriveConnector, null);
    }

    private void DriveEventChanged(long next) => RememberDeadline(C.DriveConnector, next == long.MaxValue ? null : next);

    internal void SetClock4MHz(bool high)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _ic8.AdvanceTo(_time); _ic8.SetClock(0, high);
        _ic14.AdvanceTo(_time); _ic14.SetClock(0, high);
    }

    /// <summary>Connect the established external4 MHz waveform to its two real
    /// input pins. Both chips retain their own phases, counters and responses.</summary>
    internal void ConnectClock4MHz(long nextFallingNanoseconds)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _ic8.AdvanceTo(_time); _ic14.AdvanceTo(_time);
        _ic8.ConnectPeriodicClock(0, nextFallingNanoseconds, 250, 125);
        _ic14.ConnectPeriodicClock(0, nextFallingNanoseconds, 250, 125);
    }

    internal void AdvanceTo(long nanoseconds)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentOutOfRangeException.ThrowIfLessThan(nanoseconds, _time);
        if (_advancing)
        {
            if (nanoseconds != _time) throw new InvalidOperationException("Cannot advance the board from a wire notification.");
            return;
        }
        _advancing = true;
        try
        {
            while (_next != 0 && _next <= nanoseconds)
            {
                _time = _next;
                // Visit only the package which announced this transition. A recipient
                // catches up to this same instant before receiving the changed input.
                AdvancePart(_nextPart, _time);
            }
            _time = nanoseconds;
            // All connected responses through this instant were handled above.
            // Settle the remaining internal divider stages, then account for its
            // actual QA clocks; idle/delay cycles stay inside the 8271 fast path.
            SynchronizeController(includeCurrentTime: true);
        }
        finally { _advancing = false; PublishDeadline(); }
    }

    private void RememberDeadline(int part, long? next)
    {
        var previous = _deadlines[part];
        var oldNext = _next;
        var value = next ?? 0;
        _deadlines[part] = value;
        if (value == 0) _activeParts &= ~(1u << part);
        else _activeParts |= 1u << part;
        if (value != 0 && (_next == 0 || value < _next)) { _next = value; _nextPart = part; }
        if (previous != 0 && previous == oldNext)
        {
            _next = 0; _nextPart = 0;
            var active = _activeParts;
            while (active != 0)
            {
                var index = BitOperations.TrailingZeroCount(active);
                active &= active - 1;
                if (_next == 0 || _deadlines[index] < _next)
                { _next = _deadlines[index]; _nextPart = index; }
            }
        }
        PublishDeadline();
    }

    private void AdvancePart(int part, long target)
    {
        switch (part)
        {
            case C.Control: _ic2.AdvanceTo(target); break;
            case C.Interrupt: _ic3.AdvanceTo(target); break;
            case C.Decode: _ic4.AdvanceTo(target); break;
            case C.Drive: _ic5.AdvanceTo(target); break;
            case C.Write: _ic6.AdvanceTo(target); break;
            case C.Separator: _ic7.AdvanceTo(target); break;
            case C.Divider: _ic8.AdvanceTo(target); break;
            case C.Timeout: _ic9.AdvanceTo(target); break;
            case C.Ready1: _ic10.AdvanceTo(target); break;
            case C.Ready0: _ic11.AdvanceTo(target); break;
            case C.Latch: _ic12.AdvanceTo(target); break;
            case C.Predivider: _ic14.AdvanceTo(target); break;
            case C.DriveConnector: _driveTiming!.AdvanceTo(target); break;
            default: throw new InvalidOperationException("No physical package or connector for the received deadline.");
        }
    }

    private void PublishDeadline()
    {
        if (_advancing || _publishedNext == _next) return;
        _publishedNext = _next;
        NextEventNanosecondsChanged?.Invoke(_next == 0 ? null : _next);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_driveTiming is not null) DisconnectDriveTiming(_driveTiming);
        _fdc.DriveSignalsChanged -= ControllerDriveChanged;
        _fdc.WriteDataChanged -= ControllerWriteDataChanged;
        _fdc.NmiChanged -= ControllerInterruptChanged;
        _fdc.NextClockEventChanged -= ControllerClockEventChanged;
        _ic8.NextOutputEventNanosecondsChanged = null;
        _ic8.FirstQaClockReached = null;
        NextEventNanosecondsChanged = null;
        DriveLinesChanged = null; NmiChanged = null;
        // The borrowed controller, media and machine are never reset or destroyed here.
    }
}
