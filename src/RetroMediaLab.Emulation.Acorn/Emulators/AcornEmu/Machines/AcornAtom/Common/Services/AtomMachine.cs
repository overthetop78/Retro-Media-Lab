using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Enums;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Interfaces;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornAtom.Common.Constants.AtomConstants;
using V = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornAtom.Common.Constants.AtomVideoConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornAtom.Common.Services;

/// <summary>Carte Atom : 6502, 8255, 6522, MC6847, buzzer et extension Disc Pack.</summary>
internal sealed partial class AtomMachine : ICpuBus, IAcornMachine
{
    private readonly byte[] _ram = new byte[C.AddressBytes];
    private readonly byte[] _basicOs;
    private readonly byte[] _floatingPoint;
    private readonly byte[] _extension;
    private readonly byte[] _dos;
    private readonly int _textRamBytes;
    private readonly int _videoRamBytes;
    private readonly Cpu6502 _cpu;
    private readonly Via6522 _via = new();
    private readonly AtomPpi8255 _ppi = new();
    private readonly AtomKeyboard _keyboard = new();
    private readonly AtomVideo _video;
    private readonly AnalogCassette _cassette = new(C.CpuClock, CassetteSignalMode.DirectWaveform);
    private readonly Intel8271? _fdc;
    private readonly IEmulationDiskImage?[] _disks = new IEmulationDiskImage?[C.FloppyDrives];
    private readonly int[] _speeds = [C.DefaultSpeedPercent, C.DefaultSpeedPercent];
    private readonly List<short> _samples = [];
    private int _videoPhase;
    private int _audioPhase;
    private int _cassettePhase;
    private bool _cassetteClock;
    private byte _outputs;
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
        _ppi.PortAPinsChanged = value => { _keyboard.Select(value); _video.SetMode(value); };
        _ppi.ReadPortB = _keyboard.Read;
        _ppi.ReadPortC = ReadInputs;
        _ppi.PortCPinsChanged = value => { _outputs = value; _video.SetColorSet((value & V.ColorSetSelect) != 0); UpdateCassetteOutput(); };
        if (_dos.Length != 0) _fdc = new Intel8271(index => _disks[index], C.CpuClock);
        _cpu = new Cpu6502(this);
        Reset(true);
    }

    public bool Irq => _via.Irq;
    public bool Nmi => _fdc?.Nmi == true;
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
        _via.Tick();
        _fdc?.Tick();
        if (++_cassettePhase >= C.CassetteHalfPeriod)
        {
            _cassettePhase = 0;
            _cassetteClock = !_cassetteClock;
            UpdateCassetteOutput();
        }
        _cassette.Tick();
        _videoPhase += V.CharacterClock;
        while (_videoPhase >= C.CpuClock) { _videoPhase -= C.CpuClock; _video.Tick(); }
        _audioPhase += C.AudioSampleRate;
        if (_audioPhase < C.CpuClock) return;
        _audioPhase -= C.CpuClock;
        var speaker = (_outputs & C.SpeakerOutput) != 0 ? C.SpeakerAmplitude : 0;
        _speakerFiltered = speaker - _speakerPrevious + C.SpeakerDcRetention * _speakerFiltered;
        _speakerPrevious = speaker;
        _samples.Add((short)Math.Clamp(_speakerFiltered + _cassette.MonitorSample, short.MinValue, short.MaxValue));
    }

    private byte ReadInputs() => (byte)((_cassetteClock ? C.CassetteClockInput : 0)
        | (_cassette.InputHigh ? C.CassetteInput : 0)
        | (_keyboard.Repeat ? 0 : C.KeyboardRepeat)
        | (_video.FrameSync ? C.VideoFrameSync : 0));

    private void UpdateCassetteOutput()
    {
        var negative = (_outputs & C.CassetteOutputZero) != 0
            && !((_outputs & C.CassetteOutputOne) != 0 && !_cassetteClock);
        _cassette.SetOutputLevel(!negative);
    }

    public void SetInput(EmulationInputSnapshot snapshot)
    {
        _keyboard.SetInput(snapshot);
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
            _videoPhase = _audioPhase = _cassettePhase = 0;
            _cassetteClock = false;
        }
        _via.Reset();
        _cassette.ResetHardware();
        _ppi.Reset();
        _fdc?.Reset();
        _cpu.Reset();
    }

    public byte[]? ReadNonvolatileMemory() => null;
    public void WriteNonvolatileMemory(ReadOnlySpan<byte> bytes) { }
    public bool ReceiveNetworkFrame(ReadOnlySpan<byte> frame, bool validFcs = true) => false;
    public void SetNetworkLine(bool clockPresent, bool clearToSend = true, bool idle = true) { }
    public void SetOption(string key, string value) => throw new ArgumentOutOfRangeException(nameof(key));
}
