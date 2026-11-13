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
    private Mb8118ReadPort? _dramReadPort;
    private ushort _pressed;
    private byte _muxContactLow = 255, _muxContactHigh = 255;
    private bool _disposed, _pause, _masterHigh;
    private long _nextMasterIndex = 1;
    private bool _vdpReleased, _cpuReleased;
    public long TimeNanoseconds { get; private set; }
    public CpuZ80 Cpu => _cpu;
    public Tms9918A Vdp => _vdp;
    public Sn76489 Psg => _psg;
    public bool DataBusKnown { get; private set; }

    public Sg1000Board(ReadOnlySpan<byte> cartridge, bool powerUp = true,
        Sg1000RomConfiguration? romConfiguration = null,ReadOnlySpan<byte> internalGame = default)
    {
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
            _dramReadPort = new(_dram);
            Connect();
            _cpu.SetReset(true);
            _vdp.SetReset(0, true);
            _cpu.SetWait(!_psg.Ready);
            _cpu.SetInt(!_vdp.CpuOutputs.IntHigh);
            _audioLevel = _psg.Output;
            ConnectRangeBus();
            InitializeBusPins();
            ConnectClockDomains();
            if (powerUp) AdvanceTo(C.CpuReleaseNanoseconds);
            initialized = true;
        }
        finally { if (!initialized) Dispose(); }
    }

    private void Connect()
    {
        _psg.NextEventNanosecondsChanged = next => _psgReadyDue = next ?? long.MaxValue;
        _vdp.CpuOutputsChanged = output =>
        {
            _cpu.SetInt(!output.IntHigh);
            var driver = new DataWire(output.Data, output.DataKnown ? (byte)255 : (byte)0, output.DataDriven ? (byte)255 : (byte)0);
            if (driver != _busVideoDriver) { _busVideoDriver = driver; _cpuBusDirty = true; }
            ResolveRangeCpuData();
        };
        _vdp.UnrepresentedVramPinsChanged = OnVramPins;
        _vdp.VramReadCycleProduced = ReceiveVramReadCycle;
        _vdp.VramReadCycleCancelled = CancelVramReadCycle;
        _vdp.VideoSignalProduced = OnVideo;
        _vdp.FrameStarted = _ => FramesCompleted = _vdp.RasterState.Frame;
        _psg.ReadyChanged = ready => _cpu.SetWait(!ready);
        _psg.OutputChanged = level => { AdvanceAudio(_psg.TimeNanoseconds); _audioLevel = level; };
        for (int index = 0; index < 8; index++)
        {
            int chip = index;
            CacheDramOutput(index, _dram[index].Output);
            _dram[index].NextUnrepresentedEventNanosecondsChanged = next => ReceiveVideoMemoryDeadline(chip, next);
            _dram[index].UnrepresentedOutputChanged = value => UpdateVramReadData(_dram[chip].TimeNanoseconds, chip, value);
            _dram[index].ReadOutputProgramWithdrawalRequested = WithdrawVramOutputPrograms;
        }
        RefreshVideoMemoryBoundaries();
    }

    public void SetController(int port, byte pressedButtons)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)port, 2U);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pressedButtons, (byte)63);
        ObjectDisposedException.ThrowIf(_disposed, this);
        int shift = port * 6;
        ushort next = (ushort)((_pressed & ~(63 << shift)) | (pressedButtons << shift));
        if (next == _pressed) return;
        _pressed = next;
        RefreshControllerContacts();
        DeliverExternalBusInputs();
    }

    private void RefreshControllerContacts()
    {
        byte p0 = (byte)(~_pressed & 63), p1 = (byte)((~_pressed >> 6) & 63);
        byte even = (byte)(p0 | ((p1 & 3) << 6)), odd = (byte)((p1 >> 2) | 0xf0);
        _muxContactLow = (byte)((even & 15) | ((odd & 15) << 4));
        _muxContactHigh = (byte)((even >> 4) | (odd & 240));
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
            _vdp.VramReadCycleProduced = null; _vdp.VramReadCycleCancelled = null;
            _vdp.UnrepresentedVramPinsChanged = null;
            _vdp.ClockInputBoundaryReached = null;
            _psg.OutputChanged = null; _psg.ReadyChanged = null; _psg.NextEventNanosecondsChanged = null;
            _decoder.OutputChanged = null; _decoder.NextEventNanosecondsChanged = null;
            _gates.OutputChanged = null; _gates.NextEventNanosecondsChanged = null;
            _ram.OutputChanged = null;
            foreach (var chip in _joy) { chip.OutputChanged = null; chip.NextEventNanosecondsChanged = null; }
            foreach (var chip in _dram)
            {
                chip.OutputChanged = null; chip.NextEventNanosecondsChanged = null;
                chip.UnrepresentedOutputChanged = null; chip.NextUnrepresentedEventNanosecondsChanged = null;
                chip.ReadOutputProgramWithdrawalRequested = null;
            }
        }
        finally
        {
            try { _dramReadPort?.Dispose(); }
            finally { _disposed = true; }
        }
    }
}
