using RetroMediaLab.Chips.CPU.Common.Interfaces;
using RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Services;
using RetroMediaLab.Chips.CPU.Motorola.M6800.Chip6809.Services;
using RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Services;
using RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Services;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Interfaces;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Enums;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;
using KeyboardPins = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Constants.SystemKeyboardConstants;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Constants.SystemConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Services;

/// <summary>Bus Eurocard des Acorn System ; les profils déterminent les cartes réellement installées.</summary>
internal sealed partial class AcornSystemMachine : ICpuBus, IAcornMachine
{
    public long? ProcessorClockHz => _clock;
    private readonly SystemCpuCard _card;
    private readonly int _clock;
    private readonly byte[] _ram = new byte[C.AddressBytes];
    private readonly bool[] _ramMapped = new bool[C.AddressBytes];
    private readonly byte[] _rom;
    private readonly byte[] _basic;
    private readonly int _romStart;
    private readonly RamIo8154 _ramIo = new();
    private readonly Via6522 _via = new();
    private readonly SystemKeyboard _keyboard;
    private readonly AnalogTape _tape;
    private readonly SystemVideo? _video;
    private readonly SystemHexDisplay? _display;
    private readonly Cpu6502? _cpu6502;
    private readonly Cpu6809? _cpu6809;
    private readonly Intel8271? _fdc;
    private readonly Intel8271Drive? _fdcDrive;
    private readonly IEmulationDiskImage?[] _disks = new IEmulationDiskImage?[C.FloppyDrives];
    private readonly int[] _speeds = [C.DefaultSpeedPercent, C.DefaultSpeedPercent];
    private readonly List<short> _samples = [];
    private long _ticks;
    private long _nextFrame;
    private int _videoPhase;
    private int _viaPhase;
    private int _audioPhase;
    private bool _break;

    internal AcornSystemMachine(SystemCpuCard card, ReadOnlySpan<byte> rom, ReadOnlySpan<byte> characters,
        IReadOnlyList<(int Start, int Bytes)> ramCards, bool diskInterface, ReadOnlySpan<byte> basic = default)
    {
        _card = card;
        _clock = card == SystemCpuCard.Keypad6502 ? C.SystemOneClock : card == SystemCpuCard.Fast6502A ? C.FastClock : C.StandardClock;
        var maximumRom = card == SystemCpuCard.Keypad6502 ? C.SystemOneRomBytes : card == SystemCpuCard.Fast6502A ? C.MaximumRomBytes : C.FastRomBytes;
        if (rom.Length == 0 || rom.Length > maximumRom) throw new ArgumentException(nameof(rom));
        _rom = rom.ToArray();
        if (basic.Length != 0 && basic.Length != C.BasicRomBytes) throw new ArgumentException(nameof(basic));
        _basic = basic.ToArray();
        _romStart = card == SystemCpuCard.Keypad6502 ? C.SystemOneRomStart
            : card == SystemCpuCard.Motorola6809 && rom.Length == C.SmallRomBytes ? C.StandardRomStart : C.AddressBytes - rom.Length;
        Array.Fill(_ramMapped, true, 0, C.CpuCardRamBytes);
        foreach (var (start, bytes) in ramCards)
        {
            if (start < 0 || bytes <= 0 || start > C.AddressBytes - bytes) throw new ArgumentOutOfRangeException(nameof(ramCards));
            Array.Fill(_ramMapped, true, start, bytes);
        }
        _keyboard = new SystemKeyboard(_clock);
        _tape = new AnalogTape(_clock);
        if (card == SystemCpuCard.Keypad6502)
        {
            _display = new SystemHexDisplay();
            _ramIo.ReadPortA = () => _keyboard.ReadMatrix(_tape.InputBit);
            _ramIo.PortAPinsChanged = value => { _keyboard.Select(value); _tape.SetOutputBit(_keyboard.TapeOutput); };
            _ramIo.PortBPinsChanged = _keyboard.WriteSegments;
        }
        else
        {
            _video = new SystemVideo(card == SystemCpuCard.Fast6502A, characters);
            _ramIo.ReadPortB = () => _keyboard.DataWithStrobe;
            _ramIo.ReadPortA = () => _tape.InputBit ? byte.MaxValue : (byte)(byte.MaxValue ^ KeyboardPins.TapeInputBit);
            _ramIo.PortAPinsChanged = value => _tape.SetOutputBit((value & KeyboardPins.TapeOutputBit) != 0);
            if (card == SystemCpuCard.Motorola6809)
            {
                _via.ReadPortB = () => (byte)(_keyboard.Data | (_tape.InputBit ? KeyboardPins.TapeInputBit : 0));
                _keyboard.StrobeChanged = _via.SetCb1;
            }
            else if (card == SystemCpuCard.Fast6502A) _via.ReadPortA = () => _keyboard.DataWithStrobe;
        }
        if (diskInterface)
        {
            _fdc = new Intel8271(_clock);
            _fdcDrive = new(_fdc, index => _disks[index], _clock);
        }
        if (card == SystemCpuCard.Motorola6809) _cpu6809 = new Cpu6809(this);
        else _cpu6502 = new Cpu6502(this);
        Reset(true);
    }

    public bool Irq => _via.Irq;
    public bool Nmi => _fdc?.Nmi == true;
    public Action<ReadOnlyMemory<byte>>? NetworkFrameTransmitted { get; set; }
    public Action<int>? NetworkPoll { get; set; }

    public void Reset(bool cold)
    {
        if (cold)
        {
            Array.Clear(_ram);
            _keyboard.Reset();
            _video?.Reset();
            _samples.Clear();
        }
        _via.Reset();
        _ramIo.Reset();
        _tape.ResetHardware();
        _fdc?.Reset();
        _fdcDrive?.Reset();
        if (_cpu6809 is not null) _cpu6809.Reset(); else _cpu6502!.Reset();
        _nextFrame = _ticks + _clock / C.PresentationRate;
    }

    public byte[] RunFrame()
    {
        if (_display is not null) _keyboard.BeginDisplayFrame();
        while (_ticks < _nextFrame)
        {
            if (_cpu6809 is not null) _cpu6809.Step(); else _cpu6502!.Step();
        }
        _nextFrame += _clock / C.PresentationRate;
        return _video?.CompletedFrame ?? _display!.Render(_keyboard.DisplaySegments);
    }

    public short[] DrainAudio()
    {
        var samples = _samples.ToArray();
        _samples.Clear();
        return samples;
    }

    public void Tick()
    {
        _ticks++;
        _keyboard.Tick();
        _tape.Tick();
        _fdcDrive?.Tick();
        _viaPhase += C.ViaClock;
        while (_viaPhase >= _clock) { _viaPhase -= _clock; _via.Tick(); }
        if (_video is not null)
        {
            _videoPhase += C.VideoCharacterClock;
            while (_videoPhase >= _clock) { _videoPhase -= _clock; _video.TickCharacter(); }
        }
        _audioPhase += C.AudioSampleRate;
        if (_audioPhase >= _clock)
        {
            _audioPhase -= _clock;
            _samples.Add((short)(_tape.MonitorSample / C.TapeMonitorDivisor));
        }
    }

    public void SetInput(EmulationInputSnapshot snapshot)
    {
        _keyboard.SetInput(snapshot);
        if (_card == SystemCpuCard.Fast6502A) _via.SetCa1(!_keyboard.Blank);
        if (_keyboard.Break && !_break) Reset(false);
        _break = _keyboard.Break;
    }

    public byte[]? ReadNonvolatileMemory() => null;
    public void WriteNonvolatileMemory(ReadOnlySpan<byte> bytes) { }
    public bool ReceiveNetworkFrame(ReadOnlySpan<byte> frame, bool validFcs = true) => false;
    public void SetNetworkLine(bool clockPresent, bool clearToSend = true, bool idle = true) { }
    public void SetOption(string key, string value) => throw new ArgumentOutOfRangeException(nameof(key));
}
