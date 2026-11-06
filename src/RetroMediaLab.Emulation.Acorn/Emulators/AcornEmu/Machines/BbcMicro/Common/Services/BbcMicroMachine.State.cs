using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Contracts;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services;

internal sealed partial class BbcMicroMachine
{
    // Les autres assemblages seront raccordés avec leurs données propres ; ne pas
    // proposer de sauvegarde partielle lorsqu'un de leurs composants est présent.
    public Type? StateType => _masterMemory is null && _tube is null
        && !_compact && !_winchester.Enabled ? typeof(BbcMicroState) : null;

    public object CaptureState()
    {
        if (StateType is null) throw new NotSupportedException();
        return new BbcMicroState
        {
            Ram = (byte[])_ram.Clone(),
            PlusMemory = _plusMemory?.CaptureState(),
            CyclesPerFrame = _cyclesPerFrame,
            Cpu = Cpu.CaptureState(),
            SystemVia = _via.CaptureState(),
            UserVia = _userVia?.CaptureState(),
            Adc = _adc?.CaptureState(),
            Keyboard = _keyboard.CaptureState(),
            SidewaysRam = _roms.CaptureState(),
            MemoryExtension = _memoryExtension.CaptureState(),
            Audio = _audio.CaptureState(),
            Video = Video.CaptureState(),
            Serial = _serial.CaptureState(),
            Cassette = _cassette.CaptureState(),
            Disks = _disks.CaptureState(),
            Econet = _econet.CaptureState(),
            MasterClockWakeTick = _masterClockWakeTick,
            WinchesterWakeTick = _winchesterWakeTick,
            AdcWakeTick = _adcWakeTick,
            FireInputs = _fireInputs,
            CassetteClockRequired = _cassetteClockRequired,
            Latch = _latch,
            SoundDataBus = _soundDataBus,
            KeyboardColumn = _keyboardColumn,
            Ticks = _ticks,
            NextFrame = _nextFrame,
            Break = _break,
            InterruptSources = _interruptSources,
            NmiSources = _nmiSources,
            ViaTrailingEdgePending = _viaTrailingEdgePending,
        };
    }

    public void RestoreState(object state)
    {
        if (StateType is null) throw new NotSupportedException();
        if (state is not BbcMicroState saved || saved.Ram?.Length != _ram.Length
            || saved.CyclesPerFrame != _cyclesPerFrame || (_userVia is null) != (saved.UserVia is null)
            || (_adc is null) != (saved.Adc is null) || (_plusMemory is null) != (saved.PlusMemory is null))
            throw new ArgumentException(nameof(state));
        saved.Ram.CopyTo(_ram, 0);
        if (_plusMemory is not null) _plusMemory.RestoreState(saved.PlusMemory!);
        _roms.RestoreState(saved.SidewaysRam);
        _memoryExtension.RestoreState(saved.MemoryExtension);
        _keyboard.RestoreState(saved.Keyboard);
        _via.RestoreState(saved.SystemVia);
        if (_userVia is not null) _userVia.RestoreState(saved.UserVia!);
        if (_adc is not null) _adc.RestoreState(saved.Adc!);
        _audio.RestoreState(saved.Audio);
        Video.RestoreState(saved.Video);
        _serial.RestoreState(saved.Serial);
        _cassette.RestoreState(saved.Cassette);
        _disks.RestoreState(saved.Disks);
        _econet.RestoreState(saved.Econet);
        _masterClockWakeTick = saved.MasterClockWakeTick;
        _winchesterWakeTick = saved.WinchesterWakeTick;
        _adcWakeTick = saved.AdcWakeTick;
        _fireInputs = saved.FireInputs;
        _cassetteClockRequired = saved.CassetteClockRequired;
        _latch = saved.Latch;
        _soundDataBus = saved.SoundDataBus;
        _keyboardColumn = saved.KeyboardColumn;
        _ticks = saved.Ticks;
        _nextFrame = saved.NextFrame;
        _break = saved.Break;
        _interruptSources = saved.InterruptSources;
        _nmiSources = saved.NmiSources;
        _viaTrailingEdgePending = saved.ViaTrailingEdgePending;
        Cpu.RestoreState(saved.Cpu);
    }
}
