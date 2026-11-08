using RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Services;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Constants.DiskConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services;

/// <summary>Câblage des cartes BBC 8271 ou Acorn WD1770 ; les images sont empruntées au propriétaire du cœur.</summary>
internal sealed partial class Disks
{
    private readonly bool _enabled;
    private readonly bool _wd1770;
    private readonly IEmulationDiskImage?[] _images = new IEmulationDiskImage?[C.DriveCount];
    private readonly int[] _speeds = [100, 100];
    private readonly Intel8271? _controller;
    private readonly Intel8271Drive? _intelDrive;
    private readonly Wd1770Drive? _wd;
    private byte _control;
    private bool _nmi;
    internal Action<bool>? NmiChanged { get; set; }

    internal Disks(bool enabled, bool wd1770 = false)
    {
        _enabled = enabled;
        _wd1770 = wd1770;
        if (!enabled) return;
        if (wd1770)
        {
            _wd = new Wd1770Drive(index => _images[index]) { Selected = false };
            _wd.Chip.NmiChanged = SetNmi;
            SetNmi(_wd.Nmi);
        }
        else
        {
            _controller = new(C.IntelOwnerClock, C.IntelChipClock);
            _intelDrive = new(_controller, index => _images[index], C.IntelOwnerClock, C.IntelChipClock,
                static output => (output & C.IntelSideSelect) != 0 ? 1 : 0);
            _controller.NmiChanged = SetNmi;
            SetNmi(_controller.Nmi);
        }
    }
    internal bool Nmi => _nmi;
    private void SetNmi(bool asserted)
    {
        if (_nmi == asserted) return;
        _nmi = asserted;
        NmiChanged?.Invoke(asserted);
    }
    // /MR resets the controller; it does not gate the board's 8 MHz clock.
    internal void Tick() { if (_enabled) { if (_wd1770) _wd!.Tick(); else _intelDrive!.Tick(); } }
    internal void Reset() { if (_enabled) { if (_wd1770) { _control = 0; _wd!.SetReset(true); _wd.Selected = false; } else { _controller!.Reset(); _intelDrive!.Reset(); } } }
    internal byte Read(ushort address)
    {
        if (!_enabled) return 0xfe;
        if (_wd1770) return (address & 4) != 0 ? _wd!.Read(address & 3) : (byte)0xfe;
        _intelDrive!.SynchronizeClock();
        return _controller!.Read(address & 7);
    }
    internal void Write(ushort address, byte value)
    {
        if (!_enabled) return;
        if (_wd1770)
        {
            // Le bus CPU reste accessible au reset : la DFS teste le registre de piste avant de libérer /MR.
            if ((address & 4) != 0)
            {
                if ((address & 3) != 0 || (_control & 0x20) != 0) _wd!.Write(address & 3, value);
            }
            else
            {
                _wd!.SetReset((value & 0x20) == 0);
                _control = value; _wd.Drive = (value & 2) != 0 ? 1 : 0; _wd.Side = (value & 4) != 0 ? 1 : 0;
                _wd.Selected = (value & 3) != 0; _wd.DoubleDensity = (value & 8) == 0;
                _wd.MechanicalSpeedPercent = _speeds[_wd.Drive];
            }
            return;
        }
        _intelDrive!.SynchronizeClock();
        if ((address & 7) == 0) _controller!.MechanicalSpeedPercent = _speeds[(value & 0x80) == 0 ? 0 : 1];
        _controller!.Write(address & 7, value);
        _intelDrive!.Synchronize();
    }
    internal bool Active(int drive) => _enabled && _images[drive] is not null && (_wd1770 ? _wd!.Selected && _wd.Motor && _wd.Drive == drive : _controller!.Motor && _controller.ActiveDrive == drive);
    internal bool Motor(int drive) => _enabled && (_wd1770 ? _wd!.Selected && _wd.Motor && _wd.Drive == drive : _controller!.Motor && _controller.ActiveDrive == drive);
    internal bool Reading(int drive) => _enabled && (_wd1770 ? _wd!.Selected && _wd.Drive == drive && _wd.Reading : _controller!.ActiveDrive == drive && _controller.Reading);
    internal long SeekSteps(int drive) => !_enabled ? 0 : _wd1770 ? _wd!.SeekSteps(drive) : _controller!.SeekSteps(drive);
    internal long ReadBytes(int drive) => !_enabled ? 0 : _wd1770 ? _wd!.ReadBytes(drive) : _controller!.ReadBytes(drive);
    internal long WrittenBytes(int drive) => !_enabled ? 0 : _wd1770 ? _wd!.WrittenBytes(drive) : _controller!.WrittenBytes(drive);
    internal void SetSpeed(int drive, int percent)
    {
        if (drive is < 0 or >= C.DriveCount || percent < 0) throw new ArgumentOutOfRangeException(nameof(drive));
        _speeds[drive] = percent;
    }
    internal bool Writing(int drive) => _enabled && (_wd1770 ? _wd!.Selected && _wd.Drive == drive && _wd.Writing : _controller!.ActiveDrive == drive && _controller.Writing);
    internal void Attach(int drive, IEmulationDiskImage? image)
    {
        if (drive < 0 || drive >= C.DriveCount) throw new ArgumentOutOfRangeException(nameof(drive));
        if (!_enabled && image is not null) throw new InvalidOperationException("The disk interface is not installed.");
        if (_enabled)
        {
            if (_wd1770) { if (_wd!.Active && _wd.Drive == drive) _wd.Reset(); }
            else if (_controller!.Active && _controller.ActiveDrive == drive) { _controller.Reset(); _intelDrive!.Reset(); }
        }
        _images[drive] = image;
        if (_enabled) { if (_wd1770) _wd!.MediaChanged(drive); else _intelDrive!.MediaChanged(drive); }
    }
}
