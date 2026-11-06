using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants.WinchesterDriveConstants;
using H = RetroMediaLab.Chips.Controllers.Acorn.Winchester.Scsi.Constants.ScsiHostAdapterConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services;

/// <summary>Décode l'interface hôte Acorn ; le cœur conserve la possession des images attachées.</summary>
internal sealed class Winchester(bool enabled, Func<long>? clock = null)
{
    private readonly IEmulationDiskImage?[] _images = new IEmulationDiskImage?[C.DriveCount];
    private readonly WinchesterStorage?[] _storage = new WinchesterStorage?[C.DriveCount];
    private WinchesterController? _controller;
    private long? _nextClockEvent;
    private WinchesterController Controller => _controller ??= CreateController();
    internal Action<bool>? IrqChanged { get; set; }
    internal Action<long?>? NextClockEventChanged { get; set; }

    private void SynchronizeClock()
    {
        if (clock is not null) Controller.AdvanceTo(clock());
    }

    private void PublishClockEvent()
    {
        var next = Controller.NextClockEvent;
        if (_nextClockEvent == next) return;
        _nextClockEvent = next;
        NextClockEventChanged?.Invoke(next);
    }

    internal long AdvanceTo(long ticks)
    {
        if (!enabled) return long.MaxValue;
        Controller.AdvanceTo(ticks);
        _nextClockEvent = Controller.NextClockEvent;
        return _nextClockEvent ?? long.MaxValue;
    }

    private WinchesterController CreateController()
    {
        var controller = new WinchesterController(unit => _storage[unit]);
        controller.IrqChanged = asserted => IrqChanged?.Invoke(asserted);
        return controller;
    }
    internal bool Irq => enabled && Controller.Irq;
    internal byte Read(ushort address)
    {
        if (!enabled) return H.OpenBus;
        SynchronizeClock();
        var value = Controller.Read(address);
        PublishClockEvent();
        return value;
    }
    internal void Write(ushort address, byte value)
    {
        if (!enabled) return;
        SynchronizeClock();
        Controller.Write(address, value);
        PublishClockEvent();
    }
    internal void Tick()
    {
        if (!enabled) return;
        Controller.Tick();
        PublishClockEvent();
    }
    internal void Reset()
    {
        if (!enabled) return;
        SynchronizeClock();
        Controller.Reset();
        PublishClockEvent();
    }
    internal bool Active(int unit) => enabled && IsPort(unit) && _images[unit] is not null && Controller.Active(unit);
    internal bool Writing(int unit) => Active(unit) && Controller.Writing(unit);
    internal bool Motor(int unit) => enabled && IsPort(unit) && _images[unit] is not null;
    internal long SeekCount(int unit) => enabled && IsPort(unit) ? Controller.SeekCount(unit) : 0;
    internal long ReadBlocks(int unit) => enabled && IsPort(unit) ? Controller.ReadBlocks(unit) : 0;
    internal long WrittenBlocks(int unit) => enabled && IsPort(unit) ? Controller.WrittenBlocks(unit) : 0;
    internal void Configure(int unit, int latencyMilliseconds, int speedPercent, int? writeLatencyMilliseconds = null)
    {
        // Legacy logical latencies no longer replace physical seek and rotational timing.
        if (enabled)
        {
            SynchronizeClock();
            Controller.ConfigureSpeed(unit, speedPercent);
            PublishClockEvent();
        }
    }
    internal void Attach(int unit, IEmulationDiskImage? image)
    {
        if (!IsPort(unit)) throw new ArgumentOutOfRangeException(nameof(unit));
        if (!enabled && image is not null) throw new InvalidOperationException("The Winchester interface is not installed.");
        if (enabled) SynchronizeClock();
        // Publish the old image's pending window before changing its borrowed storage.
        if (enabled && Controller.Busy) Controller.Reset();
        _images[unit] = image;
        _storage[unit] = image is null ? null : new WinchesterStorage(image);
        if (enabled)
        {
            Controller.RefreshDrives();
            PublishClockEvent();
        }
    }

    private static bool IsPort(int unit) => unit is >= 0 and < C.DriveCount;
}
