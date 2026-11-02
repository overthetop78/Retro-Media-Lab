using RetroMediaLab.Chips.CPU.ARM.ARMv2.ARM2.Enums;
using RetroMediaLab.Chips.CPU.ARM.ARMv2.ARM2.Interfaces;
using RetroMediaLab.Chips.CPU.ARM.ARMv2.ARM2.Services;
using RetroMediaLab.Chips.Controllers.Hitachi.HDC.HD63463.Services;
using RetroMediaLab.Chips.IO.Acorn.Archimedes.IOC.Services;
using RetroMediaLab.Chips.Memory.Acorn.Archimedes.MEMC.Enums;
using RetroMediaLab.Chips.Memory.Acorn.Archimedes.MEMC.Services;
using RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6551.Services;
using RetroMediaLab.Chips.Timers.Philips.PCF85xx.PCF8583.Services;
using RetroMediaLab.Chips.Video.Acorn.Archimedes.VIDC.Services;
using RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Enums;
using RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Services;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Enums;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.Archimedes.Common.Enums;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Interfaces;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.Archimedes.Common.Constants.ArchimedesBoardConstants;
using Ioc = RetroMediaLab.Chips.IO.Acorn.Archimedes.IOC.Constants.ArchimedesIocConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.Archimedes.Common.Services;

/// <summary>Carte ARM/MEMC/VIDC/IOC des premiers Archimedes. Les images restent empruntées au cœur.
/// Aucun accès aux appels système de la ROM et aucune ressource native possédée.</summary>
internal sealed partial class ArchimedesMachine : IArmBus, IAcornMachine
{
    private readonly ArchimedesMemc _memc;
    private readonly ArchimedesIoc _ioc = new();
    private readonly ArchimedesVidc _vidc = new();
    private readonly ArchimedesKeyboard _keyboard = new();
    private readonly Pcf8583 _cmos;
    private readonly CpuArm26 _cpu;
    private readonly Wd1770 _floppy;
    private readonly Mos6551? _serial;
    private readonly int _floppyDriveCount;
    private readonly Hd63463? _hardDisk;
    private readonly int _cpuClockHz;
    private readonly bool _stretchInternalCycles;
    private int _lastBusMasterTicks;
    private bool _mergeInternalCyclePending;
    private readonly int _memoryMasterTicks;
    private readonly int _iocMasterTicks = C.MasterClockHz / C.IocClockHz;
    private readonly int _floppyMasterTicks = C.MasterClockHz / C.ReferenceClockHz;
    private long _masterTicks;
    private long _cpuPhase;
    private int _dmaTicks;
    private int _refreshTicks;
    private bool _busLocked;
    private bool _cpuBusBusy;
    private bool _break;
    private double _lastFrameSeconds;

    internal ArchimedesMachine(ReadOnlyMemory<byte> rom, int ramBytes,
        Arm26Model processor = Arm26Model.Arm2, int cpuClockHz = C.DefaultCpuClockHz,
        int memoryClockHz = C.ReferenceClockHz, DateTime? initialTime = null, ReadOnlySpan<byte> initialCmosRam = default,
        bool st506 = false, ArchimedesMemcRevision memoryRevision = ArchimedesMemcRevision.Memc1A,
        int floppyDrives = C.FloppyDrives, bool serialPort = true)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cpuClockHz);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(memoryClockHz);
        if (!Enum.IsDefined(memoryRevision)) throw new ArgumentOutOfRangeException(nameof(memoryRevision));
        if (C.MasterClockHz % memoryClockHz != 0) throw new ArgumentOutOfRangeException(nameof(memoryClockHz));
        ArgumentOutOfRangeException.ThrowIfLessThan(floppyDrives, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(floppyDrives, C.FloppyDrives);
        _floppyDriveCount = floppyDrives;
        _cpuClockHz = cpuClockHz;
        _memoryMasterTicks = C.MasterClockHz / memoryClockHz;
        _stretchInternalCycles = memoryRevision == ArchimedesMemcRevision.Memc1 && processor == Arm26Model.Arm2;
        _memc = new ArchimedesMemc(ramBytes, rom);
        _cmos = new Pcf8583(C.IocClockHz, initialTime, initialRam: initialCmosRam);
        _floppy = new Wd1770(index => _disks[index], C.ReferenceClockHz, Wd1770Model.Wd1772, C.FloppyDrives);
        _memc.SoundInterruptChanged = active => _ioc.SetIrqB(Ioc.SoundInterrupt, active);
        _vidc.FlybackChanged = _ioc.SetFlyback;
        _vidc.VideoFrameStarted = _memc.BeginVideoFrame;
        _vidc.CursorFrameStarted = _memc.BeginCursorFrame;
        _ioc.KeyboardTransmitted = _keyboard.Write;
        _ioc.ControlWritten = WriteControlPins;
        if (serialPort)
        {
            _serial = new Mos6551(C.IocClockHz);
            _ioc.BaudTransitions = _serial.AdvanceReceiveClock;
            _serial.InterruptChanged = active => _ioc.SetIrqB(C.SerialInterrupt, active);
        }
        if (st506)
        {
            _hardDisk = new Hd63463(unit => _hardDisks[unit], C.IocClockHz);
            _hardDisk.InterruptChanged = _ => RefreshHardDiskInterrupt();
            _hardDisk.DataRequestChanged = _ => RefreshHardDiskInterrupt();
        }
        _cpu = new CpuArm26(this, processor);
        Reset(true);
    }

    public bool Irq => _ioc.Irq;
    public bool Fiq => _ioc.Fiq;
    internal int Width => _vidc.Width;
    internal int Height => _vidc.Height;
    internal long MasterTicks => _masterTicks;
    internal long Instructions => _cpu.Instructions;
    internal uint ProgramCounter => _cpu.PC;
    internal void Step() => _cpu.Step();

    public byte[] RunFrame()
    {
        var frame = _vidc.Frame;
        var start = _masterTicks;
        var limit = start + Math.Max(C.MasterClockHz / C.InitialFrameRate, _vidc.FrameMasterClocks ?? 0);
        while (_vidc.Frame == frame && _masterTicks < limit) _cpu.Step();
        _lastFrameSeconds = (double)(_masterTicks - start) / C.MasterClockHz;
        return _vidc.CompletedFrame;
    }

    public void SetInput(EmulationInputSnapshot snapshot)
    {
        _keyboard.SetInput(snapshot);
        if (_keyboard.Break && !_break) Reset(false);
        _break = _keyboard.Break;
    }

    public byte[] ReadNonvolatileMemory() => _cmos.ReadNonvolatileMemory();
    public void WriteNonvolatileMemory(ReadOnlySpan<byte> bytes) => _cmos.WriteNonvolatileMemory(bytes);

    public void Reset(bool cold)
    {
        _ioc.Reset(cold);
        _memc.Reset(cold);
        _floppy.Reset();
        _serial?.Reset();
        _hardDisk?.Reset();
        _cmos.ResetBus();
        if (cold)
        {
            _vidc.Reset();
            _keyboard.Reset();
            _audio.Clear();
            _audioPhase = 0;
            _speakerLeft = _speakerRight = 0;
            _masterTicks = _cpuPhase = 0;
            _lastFrameSeconds = 0;
        }
        _dmaTicks = 0;
        _refreshTicks = C.RefreshPeriodMemoryCycles * _memoryMasterTicks;
        _busLocked = _cpuBusBusy = false;
        _lastBusMasterTicks = _memoryMasterTicks;
        _mergeInternalCyclePending = false;
        WriteFloppyLatchA(C.InitialFloppyLatchA);
        WriteFloppyLatchB(0);
        RefreshControlInputs();
        _cpu.Reset();
    }

    private void WriteControlPins(byte pins)
    {
        _cmos.WriteLines((pins & C.CmosClockPin) != 0, (pins & C.CmosDataPin) != 0);
        RefreshControlInputs();
    }

    private void RefreshHardDiskInterrupt() =>
        _ioc.SetIrqB(C.HardDiskInterrupt, _hardDisk is { } controller && (controller.Irq || controller.Drq));

    private void RefreshControlInputs()
    {
        var pins = Ioc.ControlPins;
        if (!_cmos.Sda) pins &= unchecked((byte)~C.CmosDataPin);
        if (!_floppy.Selected || _disks[_floppy.Drive] is null) pins &= unchecked((byte)~C.FloppyReadyPin);
        _ioc.SetControlInputs(pins);
        _ioc.SetFiq(C.FloppyDrqFiq, _floppy.Drq);
        _ioc.SetFiq(C.FloppyIrqFiq, _floppy.Irq);
    }
}
