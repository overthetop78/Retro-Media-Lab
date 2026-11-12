using RetroMediaLab.Chips.Audio.TexasInstruments.SN764xx.SN76489.Services;
using RetroMediaLab.Chips.CPU.NEC.UPD780C.UPD780C1.Models;
using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Services;
using RetroMediaLab.Chips.Logic.Common.Enums;
using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS139.Models;
using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS139.Services;
using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS257.Models;
using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS257.Services;
using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS32.Models;
using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS32.Services;
using RetroMediaLab.Chips.Memory.Fujitsu.MB81xx.MB8118.Models;
using RetroMediaLab.Chips.Memory.Fujitsu.MB81xx.MB8118.Services;
using RetroMediaLab.Chips.Memory.Toshiba.TMM20xx.TMM2009.Services;
using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;
using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Models;
using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Services;
using C = RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.SG1000.Constants.Sg1000Constants;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.SG1000.Contracts;
using RetroMediaLab.Chips.Memory.Toshiba.TMM23xx.TMM23256.Services;
using System.Security.Cryptography;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.SG1000.Services;

/// <summary>Discrete NTSC SG-1000 wiring. Owns no thread, host window or native resource.</summary>
internal sealed partial class Sg1000Board : IDisposable
{
    private byte[] _rom;
    private readonly CpuZ80 _cpu = new(Upd780C1Profile.Hardware, Upd780C1Profile.Timing,
        new() { SP = 0xffff }, default, 0xff);
    private readonly Tms9918A _vdp = new(new(default, 0, default, new(null, 0,
        RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Enums.Tms9918CpuVramOperation.None, false),
        new(0, true, true, true, true)), Tms9918CpuTiming.SpecifiedMaximum);
    private readonly Sn76489 _psg = new(150);
    private readonly Sn74Ls139 _decoder = new(Sn74Ls139Timing.CharacterizedMaximum);
    private readonly Sn74Ls32 _gates = new(Sn74Ls32Timing.CharacterizedMaximum);
    private readonly Sn74Ls257[] _joy = [new(Sn74Ls257Timing.CharacterizedMaximum), new(Sn74Ls257Timing.CharacterizedMaximum)];
    private readonly Tmm2009 _ram = new(false);
    private readonly Mb8118[] _dram = Enumerable.Range(0, 8).Select(_ => new Mb8118(Mb8118Timing.Speed12)).ToArray();
    private readonly long[] _due = Enumerable.Repeat(long.MaxValue, 19).ToArray();
    private readonly LogicPinState[] _decodePins = new LogicPinState[8], _gatePins = new LogicPinState[4];
    private readonly LogicPinState[] _joyPins = new LogicPinState[8], _dramPins = new LogicPinState[8];
    private ushort _pressed;
    private bool _disposed, _pause, _masterHigh;
    private long _nextMasterIndex = 1;
    private long _nextMasterTime = C.NanosecondsPerSecond / (C.CrystalHertz * 2);
    private long _masterFraction = C.NanosecondsPerSecond % (C.CrystalHertz * 2);
    private bool _vdpReleased, _cpuReleased;
    private readonly bool _useClockDomains;
    public long TimeNanoseconds { get; private set; }
    public CpuZ80 Cpu => _cpu;
    public Tms9918A Vdp => _vdp;
    public Sn76489 Psg => _psg;
    public bool DataBusKnown { get; private set; }

    public Sg1000Board(ReadOnlySpan<byte> cartridge, bool powerUp = true,
        Sg1000RomConfiguration? romConfiguration = null,ReadOnlySpan<byte> internalGame = default,
        bool useClockDomains = true)
    {
        _useClockDomains = useClockDomains;
        _romConfiguration = romConfiguration ?? new();
        if (_romConfiguration.InternalGameEnabled && internalGame.Length != 16384)
            throw new ArgumentException("The selected internal game requires a complete16KiB ROM image.",nameof(internalGame));
        _rom = cartridge.ToArray();
        _cartridgeHash = SHA256.HashData(_rom);
        _internalGame = _romConfiguration.InternalGameEnabled ? internalGame.ToArray() : [];
        _internalGameHash = SHA256.HashData(_internalGame);
        bool initialized = false;
        try
        {
            (_cartridgeLow,_cartridgeHigh,_cartridgeLowMask,_cartridgeHighMask,_cartridgeRam) = CreateCartridge(_rom,_romConfiguration);
            if (_romConfiguration.InternalGameEnabled)
                _internalRom = CreateRom(_internalGame,_romConfiguration.InternalGameProfile,true);
            Connect();
            _cpu.SetReset(true);
            _vdp.SetReset(0, true);
            OnCpuPins(_cpu.Pins);
            _cpu.SetWait(!_psg.Ready);
            _cpu.SetInt(!_vdp.CpuOutputs.IntHigh);
            _audioLevel = _psg.Output;
            if (_useClockDomains) ConnectClockDomains();
            if (powerUp) AdvanceTo(C.CpuReleaseNanoseconds);
            initialized = true;
        }
        finally { if (!initialized) Dispose(); }
    }

    private void Connect()
    {
        _cpu.PinsChanged = OnCpuPins;
        _cpu.NextEventNanosecondsChanged = next => _due[0] = next ?? long.MaxValue;
        _vdp.NextEventNanosecondsChanged = next => _due[1] = next ?? long.MaxValue;
        _psg.NextEventNanosecondsChanged = next => ReceiveDomainDeadline(2, next);
        _decoder.NextEventNanosecondsChanged = next => ReceiveDomainDeadline(3, next);
        _gates.NextEventNanosecondsChanged = next => ReceiveDomainDeadline(4, next);
        _decoder.OutputChanged = (pin, value) =>
        {
            _decodePins[pin] = value;
            if (pin == 6) UpdateGateInputs(_decoder.TimeNanoseconds, 6);
            else if (pin == 7) UpdateGateInputs(_decoder.TimeNanoseconds, 8);
            if (pin == 3) UpdateRamBus(_decoder.TimeNanoseconds);
            if (pin == 2) UpdateRomBus(_decoder.TimeNanoseconds, 10);
            if (pin == 5) UpdatePsgBus(_decoder.TimeNanoseconds);
        };
        _gates.OutputChanged = (pin, value) =>
        {
            _gatePins[pin] = value;
            long time = _gates.TimeNanoseconds;
            if (pin == 0) UpdateRomBus(time, 1);
            else if (pin is 1 or 2) UpdateVdpBus(time);
            else UpdateJoyBus(time);
        };
        _ram.OutputChanged = _ => ResolveCpuData();
        _vdp.CpuOutputsChanged = output => { _cpu.SetInt(!output.IntHigh); ResolveCpuData(); };
        if (!_useClockDomains) _vdp.CpuClockChanged = output =>
        {
            _cpu.AdvanceTo(output.TimeNanoseconds);
            _psg.ReceiveClock(output.TimeNanoseconds, output.High);
            _cpu.SetClock(output.High);
        };
        _vdp.VramPinsChanged = OnVramPins;
        _vdp.VideoSignalProduced = OnVideo;
        _vdp.FrameStarted = _ => FramesCompleted = _vdp.RasterState.Frame;
        _psg.ReadyChanged = ready => _cpu.SetWait(!ready);
        _psg.OutputChanged = level => { AdvanceAudio(_psg.TimeNanoseconds); _audioLevel = level; };
        for (int index = 0; index < 2; index++)
        {
            int chip = index;
            _joy[index].NextEventNanosecondsChanged = next => ReceiveDomainDeadline(5 + chip, next);
            _joy[index].OutputChanged = (pin, value) => { _joyPins[chip * 4 + pin] = value; ResolveCpuData(); };
        }
        for (int index = 0; index < 8; index++)
        {
            int chip = index;
            _dramPins[index] = _dram[index].Output;
            CacheDramOutput(index, _dramPins[index]);
            _dram[index].NextEventNanosecondsChanged = next => ReceiveDomainDeadline(7 + chip, next);
            _dram[index].OutputChanged = value => { _dramPins[chip] = value; UpdateVramReadData(_dram[chip].TimeNanoseconds, chip, value); };
        }
        ConnectCartridge();
        if (_internalRom is { } internalRom)
        {
            internalRom.OutputChanged = _ => ResolveCpuData();
            internalRom.NextEventNanosecondsChanged = next => ReceiveDomainDeadline(17, next);
        }
        RefreshDeadlines();
    }

    public void SetController(int port, byte pressedButtons)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)port, 2U);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pressedButtons, (byte)63);
        ObjectDisposedException.ThrowIf(_disposed, this);
        int shift = port * 6;
        _pressed = (ushort)((_pressed & ~(63 << shift)) | (pressedButtons << shift));
        UpdateJoyBus(TimeNanoseconds);
    }

    public void SetPause(bool pressed)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_pause == pressed) return;
        _pause = pressed;
        _cpu.SetNmi(pressed);
    }

    public void Dispose()
    {
        if (_disposed) return;
        try
        {
            DisconnectCartridge();
            if (_internalRom is { } internalRom) { internalRom.OutputChanged = null; internalRom.NextEventNanosecondsChanged = null; }
            _cpu.PinsChanged = null; _cpu.NextEventNanosecondsChanged = null;
            _vdp.CpuClockChanged = null; _vdp.CpuOutputsChanged = null; _vdp.VramPinsChanged = null;
            _vdp.VideoSignalProduced = null; _vdp.FrameStarted = null; _vdp.NextEventNanosecondsChanged = null;
            _vdp.CpuClockSourceChanged = null; _vdp.NextCpuEventNanosecondsChanged = null;
            _vdp.ClockInputBoundaryReached = null;
            _psg.OutputChanged = null; _psg.ReadyChanged = null; _psg.NextEventNanosecondsChanged = null;
            _decoder.OutputChanged = null; _decoder.NextEventNanosecondsChanged = null;
            _gates.OutputChanged = null; _gates.NextEventNanosecondsChanged = null;
            _ram.OutputChanged = null;
            foreach (var chip in _joy) { chip.OutputChanged = null; chip.NextEventNanosecondsChanged = null; }
            foreach (var chip in _dram) { chip.OutputChanged = null; chip.NextEventNanosecondsChanged = null; }
        }
        finally { _disposed = true; }
    }
}
