using RetroMediaLab.Chips.CPU.Common.Interfaces;
using RetroMediaLab.Chips.CPU.Common.Enums;
using RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Services;
using RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Services;
using RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Services;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Enums;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Interfaces;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornAtom.Common.Constants.AtomConstants;
using V = RetroMediaLab.Chips.Video.Motorola.M6847.Chip6847.Constants.AtomVideoConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornAtom.Common.Services;

/// <summary>Carte Atom : 6502, 8255, 6522, MC6847, buzzer et extension Disc Pack.</summary>
internal sealed partial class AtomMachine : ICpuBus, IAcornMachine
{
    public long? ProcessorClockHz => C.CpuClock;
    private readonly byte[] _ram = new byte[C.AddressBytes];
    private readonly byte[] _basicOs;
    private readonly byte[] _floatingPoint;
    private readonly byte[] _extension;
    private readonly byte[] _dos;
    private readonly int _textRamBytes;
    private readonly int _videoRamBytes;
    private readonly Cpu6502 _cpu;
    private readonly Via6522 _via = new();
    private readonly Ppi8255 _ppi = new();
    private readonly AtomKeyboard _keyboard = new();
    private readonly AtomVideo _video;
    private readonly AnalogTape _tape = new(C.CpuClock, TapeSignalMode.DirectWaveform);
    private bool _tapeClockRequired;
    private readonly Intel8271? _fdc;
    private readonly Intel8271Drive? _fdcDrive;
    private readonly IEmulationDiskImage?[] _disks = new IEmulationDiskImage?[C.FloppyDrives];
    private readonly int[] _speeds = [C.DefaultSpeedPercent, C.DefaultSpeedPercent];
    private readonly List<short> _samples = [];
    private int _videoPhase;
    private int _audioPhase;
    private int _tapePhase;
    private bool _tapeClock;
    private byte _outputs;
    private byte _inputPins;
    private bool _break;
    private double _speakerPrevious;
    private double _speakerFiltered;

    internal AtomMachine(ReadOnlySpan<byte> basicOs, ReadOnlySpan<byte> characters,
        int textRamBytes, int videoRamBytes, ReadOnlySpan<byte> floatingPoint = default,
        ReadOnlySpan<byte> extension = default, ReadOnlySpan<byte> dos = default)
    {
        if (basicOs.Length != C.CombinedBasicOsBytes) throw new ArgumentException(nameof(basicOs));
        if (floatingPoint.Length != 0 && floatingPoint.Length != C.FloatingPointRomBytes) throw new ArgumentException(nameof(floatingPoint));
        if (extension.Length != 0 && extension.Length != C.ExtensionRomBytes) throw new ArgumentException(nameof(extension));
        if (dos.Length != 0 && dos.Length != C.DiscRomBytes) throw new ArgumentException(nameof(dos));
        if (textRamBytes < 0 || textRamBytes > C.TextRamMaximumBytes || textRamBytes % C.RamBlockBytes != 0)
            throw new ArgumentOutOfRangeException(nameof(textRamBytes));
        if (videoRamBytes < C.RamBlockBytes || videoRamBytes > C.VideoRamMaximumBytes || videoRamBytes % C.RamBlockBytes != 0)
            throw new ArgumentOutOfRangeException(nameof(videoRamBytes));
        _basicOs = basicOs.ToArray();
        _floatingPoint = floatingPoint.ToArray();
        _extension = extension.ToArray();
        _dos = dos.ToArray();
        _textRamBytes = textRamBytes;
        _videoRamBytes = videoRamBytes;
        _video = new AtomVideo(address => address < _videoRamBytes ? _ram[C.VideoRamStart + address] : (byte)0, characters);
        _ppi.PortADriveChanged = (value, _) => SetPpiPortA(value);
        _ppi.ReadPortB = _keyboard.Read;
        _video.FrameSyncChanged = level => SetPpiInput(C.VideoFrameSync, level);
        _tape.InputSignalChanged = level => SetPpiInput(C.TapeInput, level);
        _tape.ActivityChanged += () => _tapeClockRequired = _tape.TapeActive;
        _ppi.PortCDriveChanged = (value, _) => SetPpiPortC(value);
        if (_dos.Length != 0)
        {
            _fdc = new Intel8271(C.CpuClock);
            _fdcDrive = new(_fdc, index => _disks[index], C.CpuClock);
        }
        _cpu = new Cpu6502(this, C.CpuClock);
        WireCpuClock();
        _cpu.SetIrq(_via.Irq);
        _via.SetPb6(true);
        _cpu.SetNmi(_fdc?.Nmi == true);
        _via.IrqChanged = _cpu.SetIrq;
        if (_fdc is not null) _fdc.NmiChanged = _cpu.SetNmi;
        Reset(true);
    }

    public bool Irq => _via.Irq;
    public bool Nmi => _fdc?.Nmi == true;
    public CpuBusControlSignals ControlSignals => CpuBusControlSignals.None;
    public Action<ReadOnlyMemory<byte>>? NetworkFrameTransmitted { get; set; }
    public Action<int>? NetworkPoll { get; set; }

    public byte[] RunFrame()
    {
        var frame = _video.Frame;
        while (_video.Frame == frame) _cpu.Step();
        return _video.CompletedFrame;
    }

    public short[] DrainAudio()
    {
        var samples = _samples.ToArray();
        _samples.Clear();
        return samples;
    }

    public void Tick()
    {
        _fdcDrive?.Tick();
        if (++_tapePhase >= C.TapeHalfPeriod)
        {
            _tapePhase = 0;
            _tapeClock = !_tapeClock;
            SetPpiInput(C.TapeClockInput, _tapeClock);
            UpdateTapeOutput();
        }
        if (_tapeClockRequired) _tape.Tick();
        ClockVideo();
        _audioPhase += C.AudioSampleRate;
        if (_audioPhase < C.CpuClock) return;
        _audioPhase -= C.CpuClock;
        var speaker = (_outputs & C.SpeakerOutput) != 0 ? C.SpeakerAmplitude : 0;
        _speakerFiltered = speaker - _speakerPrevious + C.SpeakerDcRetention * _speakerFiltered;
        _speakerPrevious = speaker;
        _samples.Add((short)Math.Clamp(_speakerFiltered + _tape.MonitorSample, short.MinValue, short.MaxValue));
    }

    /// <summary>Les entrées du PPI conservent les niveaux reçus de chaque fil câblé.
    /// Le 8255 résout lui-même leur effet selon son mode ; aucun Tick ne sonde les émetteurs.</summary>
    private void SetPpiInput(byte mask, bool high)
    {
        var pins = high ? (byte)(_inputPins | mask) : (byte)(_inputPins & ~mask);
        if (pins == _inputPins) return;
        _inputPins = pins;
        _ppi.SetPortCInput(pins);
    }

    private void SetPpiPortA(byte value)
    {
        _keyboard.Select(value);
        _video.SetMode(value);
    }

    private void SetPpiPortC(byte value)
    {
        _outputs = value;
        _video.SetColorSet((value & V.ColorSetSelect) != 0);
        UpdateTapeOutput();
    }

    private void UpdateTapeOutput()
    {
        var negative = (_outputs & C.TapeOutputZero) != 0
            && !((_outputs & C.TapeOutputOne) != 0 && !_tapeClock);
        _tape.SetOutputLevel(!negative);
    }

    public void SetInput(EmulationInputSnapshot snapshot)
    {
        _keyboard.SetInput(snapshot);
        SetPpiInput(C.KeyboardRepeat, !_keyboard.Repeat);
        if (_keyboard.Break && !_break) Reset(false);
        _break = _keyboard.Break;
    }

    public void Reset(bool cold)
    {
        if (cold)
        {
            Array.Clear(_ram);
            _keyboard.Reset();
            _video.Reset();
            _samples.Clear();
            _speakerPrevious = _speakerFiltered = 0;
            _videoPhase = _audioPhase = _tapePhase = 0;
            _tapeClock = false;
        }
        _via.Reset();
        _tape.ResetHardware();
        _ppi.Reset();
        // RESET libère tous les pilotes : les résistances du montage résolvent
        // les niveaux hauts, même si aucun changement de masque n'est publié.
        SetPpiPortA(byte.MaxValue);
        SetPpiPortC(byte.MaxValue);
        _inputPins = (byte)((_tapeClock ? C.TapeClockInput : 0)
            | (_tape.InputHigh ? C.TapeInput : 0)
            | (_keyboard.Repeat ? 0 : C.KeyboardRepeat)
            | (_video.FrameSync ? C.VideoFrameSync : 0));
        _ppi.SetPortCInput(_inputPins);
        _fdc?.Reset();
        _fdcDrive?.Reset();
        _cpu.Reset();
    }

    public byte[]? ReadNonvolatileMemory() => null;
    public void WriteNonvolatileMemory(ReadOnlySpan<byte> bytes) { }
    public bool ReceiveNetworkFrame(ReadOnlySpan<byte> frame, bool validFcs = true) => false;
    public void SetNetworkLine(bool clockPresent, bool clearToSend = true, bool idle = true) { }
    public void SetOption(string key, string value) => throw new ArgumentOutOfRangeException(nameof(key));
}
