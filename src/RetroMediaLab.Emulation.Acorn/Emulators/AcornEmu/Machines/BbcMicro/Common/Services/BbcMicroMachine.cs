using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Enums;
using RetroMediaLab.Chips.CPU.Common.Enums;
using RetroMediaLab.Chips.CPU.Common.Interfaces;
using RetroMediaLab.Chips.IO.Motorola.M6800.Chip6850.Services;
using RetroMediaLab.Chips.Network.Motorola.M6800.Chip6854.Constants;
using RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Services;
using RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip65C12.Services;
using RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Services;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMaster.Tube.Services;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMaster.Tube.Enums;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMaster.Common.Services;
using Master = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMaster.Common.Constants.MasterConstants;
using CompactNonvolatile = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMaster.Common.Constants.CompactNonvolatileMemoryConstants;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.ModelBPlus.Services;
using Plus = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.ModelBPlus.Constants.ModelBPlusConstants;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Contracts;
using RetroMediaLab.Emulation.Acorn.Common.Machines.BbcMicro.Constants;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Interfaces;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants;
using Io = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Constants.BbcMicroIoConstants;
using TapeConstants = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Constants.TapeConstants;
using BbcTapeFunctions = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Functions.TapeFunctions;
using DiskConstants = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Constants.DiskConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services;

/// <summary>Carte partagée BBC Micro : RAM, ROM, VIA système et périphériques communs aux profils A et B.</summary>
/// <remarks>Les composants ne connaissent pas les services de l'application. Cette carte possède leur câblage et leur horloge.
/// À 16 Kio, les adresses 4000–7FFF sont des alias de la RAM basse ; l'extension 32 Kio les rend indépendantes.
/// Le bus CPU et la vidéo partagent la même mémoire physique.</remarks>
internal sealed partial class BbcMicroMachine : ICpuBus, IAcornMachine
{
    private readonly byte[] _ram;
    private readonly ModelBPlusMemory? _plusMemory;
    private readonly MasterMemory? _masterMemory;
    private readonly MasterClock? _masterClock;
    private long _masterClockWakeTick = long.MaxValue;
    private readonly bool _compact;
    private readonly CompactEeprom? _compactEeprom;
    private readonly CompactJoystick? _compactJoystick;
    private readonly TubeUla? _tube;
    private readonly TurboBoard? _turbo;
    private readonly Master512Board? _master512;
    private readonly MemoryExtension _memoryExtension;
    private readonly byte[] _mos;
    private readonly SidewaysRoms _roms;
    private readonly Disks _disks;
    private readonly Winchester _winchester;
    private long _winchesterWakeTick = long.MaxValue;
    private readonly Econet _econet;
    private readonly Via6522 _via = new();
    private readonly Via6522? _userVia;
    private readonly UserPortPointer? _pointer;
    private long _pointerWakeTick = long.MaxValue;
    private readonly Adc7002? _adc;
    private long _adcWakeTick = long.MaxValue;
    private byte _fireInputs = Io.OpenInputs;
    private readonly Keyboard _keyboard = new();
    private readonly Audio _audio = new();
    private readonly Acia6850 _acia = new(TapeConstants.CpuClock, TapeConstants.ExternalAciaClock);
    private readonly Serial _serial;
    private readonly Tape _tape;
    private bool _tapeClockRequired;
    private byte _latch = 255;
    private byte _soundDataBus = byte.MaxValue;
    private int _keyboardColumn;
    private long _ticks, _nextFrame;
    private readonly int _cyclesPerFrame;
    private bool _break;
    internal Cpu6502 Cpu { get; }
    // Ces lignes conservent les niveaux par défaut du bus BBC : RDY/DBE hauts,
    // SO/RESET inactifs ; SYNC n'a pas de récepteur sur cet assemblage.
    public CpuBusControlSignals ControlSignals =>
        CpuBusControlSignals.None;
    public long? ProcessorClockHz => BbcMicroModelConstants.CpuFrequencyHz;
    internal Video Video { get; }
    public int? VideoHeight => Video.OutputHeight;
    public EmulationVideoScanMode VideoScanMode => Video.ScanMode;
    public bool SupportsPointerCapture => _pointer is not null;
    public bool Irq => _interruptSources != BbcInterruptSources.None;
    public bool Nmi => _nmiSources != BbcNmiSources.None;
    public Action<ReadOnlyMemory<byte>>? NetworkFrameTransmitted { get => _econet.FrameTransmitted; set => _econet.FrameTransmitted = value; }
    public Action<int>? NetworkPoll { get; set; }
    public bool ReceiveNetworkFrame(ReadOnlySpan<byte> frame, bool validFcs = true) => _econet.Receive(frame, validFcs);
    public void SetNetworkLine(bool clockPresent, bool clearToSend = true, bool idle = true) => _econet.SetLine(clockPresent, clearToSend, idle);
    public int FloppyDriveCount => StorageConstants.DriveCount;
    public int HardDiskDriveCount => StorageConstants.HardDiskCount;
    public void AttachFloppy(int index, IEmulationDiskImage? image) => _disks.Attach(index, image);
    public bool FloppyActive(int index) => _disks.Active(index);
    public bool FloppyMotor(int index) => _disks.Motor(index);
    public bool FloppyReading(int index) => _disks.Reading(index);
    public bool FloppyWriting(int index) => _disks.Writing(index);
    public long FloppySeekSteps(int index) => _disks.SeekSteps(index);
    public long FloppyReadBytes(int index) => _disks.ReadBytes(index);
    public long FloppyWrittenBytes(int index) => _disks.WrittenBytes(index);
    public void ConfigureFloppySpeed(int index, int percent) => _disks.SetSpeed(index, percent);
    public void AttachHardDisk(int unit, IEmulationDiskImage? image) => _winchester.Attach(unit, image);
    public bool HardDiskActive(int unit) => _winchester.Active(unit);
    public bool HardDiskMotor(int unit) => _winchester.Motor(unit);
    public bool HardDiskWriting(int unit) => _winchester.Writing(unit);
    public long HardDiskSeekCount(int unit) => _winchester.SeekCount(unit);
    public long HardDiskReadBlocks(int unit) => _winchester.ReadBlocks(unit);
    public long HardDiskWrittenBlocks(int unit) => _winchester.WrittenBlocks(unit);
    public void ConfigureHardDisk(int unit, int latencyMilliseconds, int speedPercent, int? writeLatencyMilliseconds = null) => _winchester.Configure(unit, latencyMilliseconds, speedPercent, writeLatencyMilliseconds);
    public IEmulatorTapeTransport TapeTransport => _tape;
    public void InsertTape(EmulationTapeSignal signal, bool writable = false) => _tape.Insert(signal, writable);
    public bool TapeHasChanges => _tape.HasChanges;
    public EmulationTapeSignal RecordedTape => _tape.RecordedSignal;
    public void MarkTapeSaved() => _tape.MarkSaved();
    public void RefreshTapeIndex(EmulationTapeSignal signal) => _tape.RefreshIndex(signal);
    public void ConfigureTape(EmulationTapeSettings settings) => _tape.Configure(settings);
    public void EjectTape() => _tape.Eject();

    internal BbcMicroMachine(ReadOnlySpan<byte> mos, ReadOnlySpan<byte> basic, int memoryBytes = (int)BbcMicroModelConstants.SixteenKibibytes, ReadOnlySpan<byte> characters = default, bool diskInterface = false, IReadOnlyDictionary<int, byte[]>? optionRoms = null, bool wd1770 = false, bool winchesterInterface = false, bool econetInterface = false, int econetStation = 1, int econetCyclesPerByte = Mc6854Constants.DefaultCyclesPerByte, MemoryExtensionSettings? memoryExtension = null, SidewaysExpansionSettings? sidewaysExpansion = null, bool expandedIo = false, EmulationVideoStandard videoStandard = EmulationVideoStandard.Pal, int modelBPlusMemory = 0, bool master = false, bool compact = false, MasterCoprocessor coprocessor = MasterCoprocessor.None, ReadOnlySpan<byte> coprocessorRom = default, bool userPortPointer = false, bool? analogInterface = null)
    {
        if (mos.Length != BbcMicroFirmwareConstants.BankSize || basic.Length != BbcMicroFirmwareConstants.BankSize) throw new ArgumentException("MOS et BASIC doivent chacun contenir 16 384 octets.");
        if (memoryBytes != BbcMicroModelConstants.SixteenKibibytes && memoryBytes != BbcMicroModelConstants.ThirtyTwoKibibytes) throw new ArgumentOutOfRangeException(nameof(memoryBytes));
        memoryExtension ??= new(MemoryExtensionConstants.None);
        if (memoryExtension.Enabled && memoryBytes != BbcMicroModelConstants.ThirtyTwoKibibytes) throw new ArgumentOutOfRangeException(nameof(memoryBytes));
        if (modelBPlusMemory is not (0 or Plus.BaseBytes or Plus.ExpandedBytes)) throw new ArgumentOutOfRangeException(nameof(modelBPlusMemory));
        _plusMemory = modelBPlusMemory == 0 ? null : new ModelBPlusMemory();
        _masterMemory = master ? new MasterMemory() : null;
        _masterClock = master && !compact ? new MasterClock((byte)econetStation, diskInterface ? (byte)9 : (byte)8, () => _ticks) : null;
        _compact = compact;
        _compactEeprom = compact ? new CompactEeprom() : null;
        if (_compactEeprom is not null)
        {
            var initial = _compactEeprom.Export();
            CompactNonvolatile.InitialConfiguration.CopyTo(initial);
            _compactEeprom.Import(initial);
        }
        _compactJoystick = compact ? new CompactJoystick() : null;
        if (coprocessor != MasterCoprocessor.None)
        {
            if (!master || compact) throw new ArgumentException(nameof(coprocessor));
            _tube = new TubeUla();
            if (coprocessor == MasterCoprocessor.Turbo) { _turbo = new TurboBoard(_tube, coprocessorRom); _tube.ResetParasite = _turbo.Reset; }
            else { _master512 = new Master512Board(_tube, coprocessorRom); _tube.ResetParasite = _master512.Reset; }
        }
        _ram = new byte[memoryBytes];
        _memoryExtension = new(memoryExtension);
        _mos = mos.ToArray();
        _roms = new SidewaysRoms(basic, optionRoms, sidewaysExpansion, modelBPlusMemory, master, compact ? Master.CompactBasicBank : Master.BasicBank);
        _disks = new Disks(diskInterface, wd1770);
        _winchester = new Winchester(winchesterInterface, () => _ticks)
        {
            NextClockEventChanged = next => _winchesterWakeTick = next ?? long.MaxValue
        };
        _econet = new Econet(econetInterface, econetStation, econetCyclesPerByte);
        if (videoStandard is not (EmulationVideoStandard.Pal or EmulationVideoStandard.Ntsc)) throw new ArgumentOutOfRangeException(nameof(videoStandard));
        _cyclesPerFrame = videoStandard == EmulationVideoStandard.Ntsc ? BbcMicroModelConstants.NtscCyclesPerFrame : BbcMicroModelConstants.CyclesPerFrame;
        _nextFrame = _cyclesPerFrame;
        Video = new Video(_ram, characters, videoStandard == EmulationVideoStandard.Ntsc ? Constants.VideoConstants.NtscHeight : Constants.VideoConstants.Height, teletextHighBankOnly: _masterMemory is not null || _plusMemory is not null);
        Video.VSyncChanged = _via.SetCa1;
        if (_masterMemory is not null) Video.ReadMemory = address => _masterMemory.ReadVideo(address, _ram);
        else if (_plusMemory is not null) Video.ReadMemory = address => _plusMemory.ReadVideo(address, _ram);
        _serial = new Serial(_acia);
        _tape = new Tape(_serial)
        {
            ClockRequiredChanged = required => _tapeClockRequired = required
        };
        _via.ReadPortA = () => (_latch & 8) == 0 ? (byte)(_keyboard.Read(_via.OutputA) & (_masterClock?.ReadBus() ?? byte.MaxValue)) : _masterClock?.ReadBus() ?? byte.MaxValue;
        _userVia = expandedIo ? new Via6522() : null;
        _userVia?.SetPb6(true);
        if (userPortPointer)
        {
            if (_userVia is null) throw new ArgumentException(nameof(userPortPointer));
            // La souris Acorn du 512 et le connecteur Compact emploient PB3/PB4,
            // contrairement à l'AMX sur PB0/PB2. Le VIA reçoit les mêmes fronts physiques.
            _pointer = new UserPortPointer(compact || coprocessor == MasterCoprocessor.Master512, _userVia.SetCb1, _userVia.SetCb2)
            {
                NextSignalChanged = next => _pointerWakeTick = next ?? long.MaxValue
            };
            _userVia.ReadPortB = () => _pointer.PortB;
            _pointer.Pb6Changed = _userVia.SetPb6;
            _userVia.SetPb6((_pointer.PortB & 0x40) != 0);
        }
        else if (_compactJoystick is not null) _userVia!.ReadPortB = () => _compactJoystick.Port;
        _adc = (analogInterface ?? expandedIo) && !compact ? new Adc7002(_via.SetCb1) : null;
        if (_adc is not null) _adc.NextClockEventChanged = next => _adcWakeTick = next ?? long.MaxValue;
        if (_masterClock is not null)
        {
            _masterClock.NextClockEventChanged = next => _masterClockWakeTick = next ?? long.MaxValue;
            _masterClockWakeTick = _masterClock.NextClockEvent ?? long.MaxValue;
        }
        UpdateAnalogInputs(EmulationInputSnapshot.Empty.Controllers);
        _via.ReadPortB = () => _compactEeprom?.ReadPort ?? (_adc is null ? null : (byte?)_fireInputs) ?? Io.OpenInputs;
        // Les fournisseurs raccordés ci-dessus maintiennent PB6 haut : EEPROM sur PB4/5,
        // boutons analogiques sur PB4/5, sinon entrées ouvertes.
        _via.SetPb6(true);
        _via.WritePortB = WriteLatch;
        if (_compactEeprom is not null) _via.PortBPinsChanged = _compactEeprom.WritePort;
        _via.WritePortA = value => _masterClock?.Update(_latch, value);
        _via.PortAPinsChanged = value =>
        {
            _soundDataBus = value;
            _audio.SetBus(value, (_latch & Io.SoundWriteEnableMask) == 0);
        };
        Cpu = master ? new Cpu65C12(this, BbcMicroModelConstants.CpuFrequencyHz) : new Cpu6502(this);
        ConnectInterrupts();
        Reset(true);
    }
    public void Reset(bool cold)
    {
        CompleteViaBusCycle();
        if (cold)
        {
            Array.Clear(_ram);
            _via.Reset();
            _userVia?.Reset();
            _pointer?.SynchronizePins();
            _adc?.AdvanceTo(_ticks);
            _adc?.Reset();
            Video.Reset();
            _via.SetCa1(Video.VSync);
            _audio.Reset();
            _tape.ResetHardware();
            _roms.Reset();
            _latch = byte.MaxValue;
            _soundDataBus = byte.MaxValue;
            _audio.SetBus(_soundDataBus, writeEnabled: false);
        }
        _memoryExtension.Reset(cold);
        _plusMemory?.Reset(cold);
        _masterMemory?.Reset(cold);
        _tube?.Reset(); _turbo?.Reset(); _master512?.Reset();
        _disks.Reset();
        _winchester.Reset();
        _econet.Reset();
        Cpu.Reset();
    }
    public void SetInput(EmulationInputSnapshot snapshot)
    {
        _keyboard.SetInput(snapshot.Keys, snapshot.HostCharacters);
        UpdateAnalogInputs(snapshot.Controllers);
        _compactJoystick?.SetInput(snapshot.Controllers);
        _pointer?.SetInput(snapshot.Pointer, _ticks);
        if (_keyboard.BreakPressed && !_break) Reset(false);
        _break = _keyboard.BreakPressed;
    }
    /// <summary>Change le raccordement vidéo sans réinitialiser les registres, la RAM ou le programme exécuté.</summary>
    public void SetOption(string key, string value)
    {
        if (key != EmulatorConstants.VideoOutput || !EmulatorConstants.VideoOutputs.Contains(value, StringComparer.Ordinal)) throw new ArgumentOutOfRangeException(nameof(key));
        Video.Monochrome = value == EmulatorConstants.MonochromeOutput;
    }
    public byte[] RunFrame()
    {
        // Les commandes hôtes sont sérialisées entre les frames : le mode de chargement
        // ne change pas dans cette boucle. La lecture matérielle n'a aucun appel MOS à intercepter.
        if (_tape.Settings.Mode == EmulationTapePlaybackMode.Direct)
        {
            while (_ticks < _nextFrame) { if (!BbcTapeFunctions.TryDirectLoad(Cpu, this, _tape)) Cpu.Step(); }
        }
        else while (_ticks < _nextFrame) Cpu.Step();
        CompleteViaBusCycle();
        _nextFrame += _cyclesPerFrame;
        return Video.Render();
    }
    /// <summary>La carte remet ses échantillons au cœur ; le Common possède la sortie audio physique.</summary>
    public short[] DrainAudio() => _audio.DrainSamples();
    public void Tick()
    {
        CompleteViaBusCycle();
        _ticks++;
        _turbo?.RunHostCycle();
        _master512?.RunHostCycle();
        if ((_ticks & 127) == 0) NetworkPoll?.Invoke(128);
        _audio.Tick();
        if (_tapeClockRequired) _tape.Tick();
        else _serial.Tick();
        if (_ticks >= _adcWakeTick) _adc!.AdvanceTo(_ticks);
        if (_ticks >= _masterClockWakeTick) _masterClock!.AdvanceTo(_ticks);
        if (_ticks >= _pointerWakeTick) _pointer!.AdvanceTo(_ticks);
        _disks.Tick();
        if (_ticks >= _winchesterWakeTick) _winchesterWakeTick = _winchester.AdvanceTo(_ticks);
        _econet.Tick();
        Video.Tick();
        ClockVia();
        if ((_ticks & Io.ViaPhaseMask) == Io.ViaTrailingPhase)
        {
            if ((_latch & 8) != 0) _keyboardColumn = (_keyboardColumn + 1) & 15;
            else _keyboardColumn = _via.OutputA & 15;
            _via.SetKeyboardLine(_keyboard.ColumnPressed(_keyboardColumn));
        }
    }
    private byte ReadBus(ushort address)
    {
        if (_masterMemory is not null)
        {
            if (address < Master.RamEnd || _masterMemory.Private(address)) return _masterMemory.Read(address, Cpu.InstructionAddress, _ram);
            if (_masterMemory.TestRom(address)) return _mos[address & (Master.BankBytes - 1)];
            if (TryReadMasterBus(address, out var masterValue)) return masterValue;
        }
        if (address < MemoryExtensionConstants.RamEnd) return _plusMemory?.Read(address, Cpu.InstructionAddress, _ram) ?? _memoryExtension.Read(address, _ram);
        if (_plusMemory?.PrivateSelected(address) == true) return _plusMemory.ReadPrivate(address);
        if (address < 0xc000) return _roms.Read(address);
        if (address < 0xfc00 || address >= 0xff00) return _mos[address & 0x3fff];
        if (address is >= WinchesterDriveConstants.RegisterStart and < WinchesterDriveConstants.RegisterEnd) return _winchester.Read(address);
        if (address < 0xfe00) return 255;
        if (address < 0xfe08) return (address & 1) != 0 ? Video.ReadRegister() : (byte)0;
        if (address < 0xfe10) return _acia.Read((address & 1) != 0);
        if (address < 0xfe18) return _tape.ReadUla();
        if (address < 0xfe20) return _econet.ReadStation();
        if (address < 0xfe28) { _econet.EnableNmi(); return 0xfe; }
        if (address is >= 0xfe40 and < 0xfe60) return _via.Read(address & 15);
        if (address is >= Io.UserViaStart and < Io.UserViaEnd) return _userVia?.Read(address & Io.UserViaRegisterMask) ?? Io.UnconnectedBus;
        if (address is >= Io.AdcStart and < Io.AdcEnd)
        {
            _adc?.AdvanceTo(_ticks);
            return _adc?.Read(address & Io.RegisterMask) ?? Io.UnconnectedBus;
        }
        if (address is >= DiskConstants.ControllerStart and < DiskConstants.ControllerEnd) return _disks.Read(address);
        if (address is >= 0xfea0 and < 0xfec0) return _econet.Read(address);
        return 0xfe;
    }
    private void WriteBus(ushort address, byte value)
    {
        if (_masterMemory is not null)
        {
            if (address < Master.RamEnd || _masterMemory.Private(address)) { _masterMemory.Write(address, Cpu.InstructionAddress, value, _ram); return; }
            if (TryWriteMasterBus(address, value)) return;
        }
        if (address < MemoryExtensionConstants.RamEnd)
        {
            if (_plusMemory is not null) _plusMemory.Write(address, Cpu.InstructionAddress, value, _ram);
            else _memoryExtension.Write(address, value, _ram);
            return;
        }
        if (_plusMemory?.PrivateSelected(address) == true) { _plusMemory.WritePrivate(address, value); return; }
        if (address < MemoryExtensionConstants.RamEnd + BbcMicroFirmwareConstants.BankSize) { _roms.Write(address, value); return; }
        if (_memoryExtension.Control(address)) return;
        if (address is >= WinchesterDriveConstants.RegisterStart and < WinchesterDriveConstants.RegisterEnd) { _winchester.Write(address, value); return; }
        if (address is >= 0xfe00 and < 0xfe08)
        {
            if ((address & 1) == 0) Video.Select(value); else Video.WriteRegister(value);
        }
        else if (address is >= 0xfe08 and < 0xfe10) _acia.Write((address & 1) != 0, value);
        else if (address is >= 0xfe10 and < 0xfe18) _tape.WriteUla(value);
        else if (address is >= 0xfe18 and < 0xfe20) _econet.DisableNmi();
        else if (address is >= 0xfe20 and < 0xfe30) Video.WriteUla(address, value);
        else if (address is >= DiskConstants.RomSelectStart and < DiskConstants.RomSelectEnd)
        {
            if (_plusMemory is null || address < Plus.ControlEnd && (address & Plus.ShadowRegister) == Plus.RomRegister) _roms.Select(value);
            _plusMemory?.Select(address, value);
        }
        else if (address is >= 0xfe40 and < 0xfe60) _via.Write(address & 15, value);
        else if (address is >= Io.UserViaStart and < Io.UserViaEnd) _userVia?.Write(address & Io.UserViaRegisterMask, value);
        else if (address is >= Io.AdcStart and < Io.AdcEnd)
        {
            _adc?.AdvanceTo(_ticks);
            _adc?.Write(address & Io.RegisterMask, value);
        }
        else if (address is >= DiskConstants.ControllerStart and < DiskConstants.ControllerEnd) _disks.Write(address, value);
        else if (address is >= 0xfea0 and < 0xfec0) _econet.Write(address, value);
    }
    /// <summary>PB0–PB2 désignent la sortie du verrou ; PB3 fournit le niveau à mémoriser.</summary>
    private void WriteLatch(byte value)
    {
        _masterClock?.Address(value, _via.OutputA);
        var mask = 1 << (value & 7);
        _latch = (value & 8) != 0 ? (byte)(_latch | mask) : (byte)(_latch & ~mask);
        // IC32 sortie 0 : validation active basse du PSG sur le bus lent PA0–PA7.
        _audio.SetBus(_soundDataBus, (_latch & Io.SoundWriteEnableMask) == 0);
        _masterClock?.Update(_latch, _via.OutputA);
        Video.ScreenWrap = ((_latch >> 4) & 3) switch { 0 => 0x4000, 1 => 0x6000, 2 => 0x3000, _ => 0x5800 };
    }
    private void UpdateAnalogInputs(IReadOnlyList<EmulationControllerState> controllers)
    {
        _fireInputs = Io.OpenInputs;
        if (_adc is null) return;
        _adc.AdvanceTo(_ticks);
        var fireMask = 1u << EmulationSdk.InputProcessing.ControllerButtonIndex(EmulationControllerCommandIds.A);
        for (var index = 0; index < Io.ControllerCount; index++)
        {
            var controller = index < controllers.Count ? controllers[index] : EmulationControllerState.Empty;
            _adc.SetChannel(index * Io.AxesPerController, (ushort)(short.MaxValue - controller.LeftX));
            _adc.SetChannel(index * Io.AxesPerController + 1, (ushort)(short.MaxValue - controller.LeftY));
            if ((controller.Buttons & fireMask) != 0) _fireInputs &= (byte)~(Io.FirstFireLine << index);
        }
    }
}
