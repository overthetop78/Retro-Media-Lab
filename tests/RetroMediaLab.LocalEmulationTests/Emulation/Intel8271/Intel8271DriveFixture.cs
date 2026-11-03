using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;
using RetroMediaLab.Emulation.Interfaces;

namespace RetroMediaLab.Tests.Emulation.Intel8271;

/// <summary>Montage de test du module ; le contrôleur ne reçoit que les broches du lecteur.</summary>
internal sealed class Intel8271DriveFixture : IDisposable
{
    private const int OwnerClock = 2_000_000;
    private const int MiniClock = 2_000_000;
    private const byte ReadDriveStatus = 0x6c;
    private const byte ReadSecondDriveStatus = 0xac;
    private readonly Intel8271Drive _drive;
    internal RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Services.Intel8271 Chip { get; }

    internal Intel8271DriveFixture(IEmulationDiskImage disk, int chipClock = MiniClock)
        : this(index => index == 0 ? disk : null, chipClock) { }

    internal Intel8271DriveFixture(Func<int, IEmulationDiskImage?> images, int chipClock = MiniClock,
        Func<byte, int>? headSelect = null)
    {
        Chip = new(OwnerClock, chipClock);
        _drive = new(Chip, images, OwnerClock, chipClock, headSelect);
        _drive.MediaChanged(0);
        _drive.MediaChanged(1);
        Chip.Write(0, ReadSecondDriveStatus);
        _ = Chip.Read(1);
        Chip.Write(0, ReadDriveStatus);
        _ = Chip.Read(1);
    }

    internal bool Drq => Chip.Drq;
    internal bool Nmi => Chip.Nmi;
    internal bool Motor => Chip.Motor;
    internal byte DriveControl => Chip.DriveControl;
    internal int MechanicalSpeedPercent { get => Chip.MechanicalSpeedPercent; set => Chip.MechanicalSpeedPercent = value; }
    internal long SeekSteps(int drive) => Chip.SeekSteps(drive);
    internal long ReadBytes(int drive) => Chip.ReadBytes(drive);
    internal long WrittenBytes(int drive) => Chip.WrittenBytes(drive);
    internal byte Read(int register) => Chip.Read(register);
    internal void MediaChanged() => _drive.MediaChanged(0);
    internal long ElapsedNanoseconds { get; private set; }
    internal void Tick()
    {
        _drive.Tick();
        ElapsedNanoseconds += 1_000_000_000 / OwnerClock;
    }
    internal void Write(int register, byte value)
    {
        Chip.Write(register, value);
        _drive.Synchronize();
    }

    internal void Reset()
    {
        try { Chip.Reset(); }
        finally { _drive.Reset(); }
    }

    internal void SetReset(bool asserted)
    {
        Chip.SetReset(asserted);
        _drive.Synchronize();
    }

    public void Dispose()
    {
        try { Chip.Reset(); }
        finally { _drive.Reset(); }
    }
}
