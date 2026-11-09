using RetroMediaLab.Chips.CPU.Common.Interfaces;
using RetroMediaLab.Chips.CPU.Common.Enums;
using RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Services;
using RetroMediaLab.Chips.Controllers.Acorn.SystemTape.Services;
using RetroMediaLab.Chips.CPU.Motorola.M6800.Chip6809.Services;
using RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Services;
using RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Services;
using RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Enums;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Interfaces;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Enums;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Enums;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;
using KeyboardPins = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Constants.SystemKeyboardConstants;
using VideoConstants = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Constants.SystemVideoConstants;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Constants.SystemConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Services;

/// <summary>Bus Eurocard des Acorn System ; les profils déterminent les cartes réellement installées.</summary>
internal sealed partial class AcornSystemMachine : ICpuBus, IAcornMachine
{
    public long? ProcessorClockHz => _clock;
    public int? VideoWidth => _video is null ? null : VideoConstants.Width;
    public int? VideoHeight => _video is null ? null : VideoConstants.Height;
    public EmulationVideoScanMode VideoScanMode => _video?.ScanMode ?? EmulationVideoScanMode.Progressive;
    public double? VideoFramesPerSecond => _video?.CompletedFieldClocks > 0
        ? (double)VideoClock / _video.CompletedFieldClocks : null;
    public double? VideoFrameDurationSeconds => (double)_lastFrameTicks / _clock;
    private int VideoClock => _card == SystemCpuCard.Fast6502A ? C.VideoCharacterClock : _clock;
    private readonly SystemCpuCard _card;
    private readonly int _clock;
    private readonly byte[] _ram = new byte[C.AddressBytes];
    private readonly bool[] _ramMapped = new bool[C.AddressBytes];
    private readonly byte[] _rom;
    private readonly byte[] _basic;
    private readonly int _romStart;
    private readonly RamIo8154? _ramIo;
    private readonly Via6522? _via;
    private readonly SystemKeyboard _keyboard;
    private readonly AnalogTape _tape;
    private bool _tapeClockRequired;
    private readonly SystemTapeInterface? _tapeInterface;
    private long _nextTapeClock = long.MaxValue;
    private long _nextKeyboardClock = long.MaxValue;
    private byte _tapeInputPins = byte.MaxValue;
    private bool _tapeOutputHigh;
    private bool _tapeCb2Driven;
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
    private long _lastFrameTicks;
    private int _viaPhase;
    private int _audioPhase;
    private bool _break;

    internal AcornSystemMachine(SystemCpuCard card, ReadOnlySpan<byte> rom, ReadOnlySpan<byte> characters,
        IReadOnlyList<(int Start, int Bytes)> ramCards, bool diskInterface, ReadOnlySpan<byte> basic = default)
    {
        _card = card;
        if (card is SystemCpuCard.Keypad6502 or SystemCpuCard.Standard6502) _ramIo = new RamIo8154();
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
        _keyboard = new SystemKeyboard(_clock, asciiKeyboard: card != SystemCpuCard.Keypad6502);
        if (card != SystemCpuCard.Keypad6502)
            _keyboard.NextClockEventChanged = next => _nextKeyboardClock = next ?? long.MaxValue;
        if (card is SystemCpuCard.Keypad6502 or SystemCpuCard.Standard6502 or SystemCpuCard.Motorola6809)
            _tapeInterface = new SystemTapeInterface(_clock);
        _tape = new AnalogTape(_clock, _tapeInterface is null ? TapeSignalMode.FrequencyShiftKeying : TapeSignalMode.DirectWaveform);
        _tape.ActivityChanged += () => _tapeClockRequired = _tape.TapeActive;
        if (_tapeInterface is not null)
        {
            _tapeInterface.TapeOutputChanged = _tape.SetOutputLevel;
            _tapeInterface.InputPinsChanged = pins => _tapeInputPins = (byte)(pins
                | (byte.MaxValue ^ (KeyboardPins.TapeInputBit | KeyboardPins.TapeCarrierBit)));
            _tapeInterface.NextClockEventChanged = clock => _nextTapeClock = clock;
            // IC5.3 inverts the LM358 comparator before clocking IC4 / resetting IC6.
            _tape.InputSignalChanged = level => _tapeInterface.SetComparatorInput(!level, _ticks);
        }
        if (card is SystemCpuCard.Fast6502A or SystemCpuCard.Motorola6809) _via = new Via6522();
        _via?.SetPb6(true);
        if (card == SystemCpuCard.Keypad6502)
        {
            var ramIo = _ramIo!;
            _display = new SystemHexDisplay();
            ramIo.ReadPortA = () => _keyboard.ReadMatrix((_tapeInputPins & KeyboardPins.TapeInputBit) != 0);
            ramIo.PortADriveChanged = (value, mask) =>
            {
                _keyboard.Select(value);
                SetTapeOutput(_keyboard.TapeOutput);
                _display.SelectDigit(value, (mask & KeyboardPins.DigitMask) == KeyboardPins.DigitMask, _ticks);
            };
            ramIo.PortBDriveChanged = (value, mask) => _display.SetSegments((byte)(value & mask), _ticks);
        }
        else
        {
            _video = new SystemVideo(card == SystemCpuCard.Fast6502A, characters);
            if (_ramIo is { } ramIo)
            {
                ramIo.ReadPortB = () => _keyboard.DataWithStrobe;
                // PB7 is also the 8154's asynchronous STB/ACK input.
                _keyboard.StrobeChanged = ramIo.SetStrobe;
                ramIo.SetStrobe(!_keyboard.Strobe);
                ramIo.ReadPortA = () => _tapeInputPins;
                ramIo.PortAPinsChanged = value => SetTapeOutput((value & KeyboardPins.TapeOutputBit) != 0);
            }
            if (card == SystemCpuCard.Motorola6809)
            {
                _via!.ReadPortB = () => (byte)(_keyboard.Data | (_tapeInputPins & KeyboardPins.TapeInputBit));
                _keyboard.DataChanged = data => _via.SetPb6((data & 0x40) != 0);
                _via.SetPb6((_keyboard.Data & 0x40) != 0);
                _keyboard.StrobeChanged = _via.SetCb1;
                _via.SetCb1(!_keyboard.Strobe);
                _via.Cb2PinsChanged = level => { if (_tapeCb2Driven) SetTapeOutput(level); };
                _via.ControlOutputsChanged = outputs =>
                {
                    var driven = (outputs & ViaControlOutputs.Cb2) != 0;
                    if (driven == _tapeCb2Driven) return;
                    _tapeCb2Driven = driven;
                    SetTapeOutput(driven ? _via.Cb2 : true);
                };
            }
            else if (card == SystemCpuCard.Fast6502A) _via!.ReadPortA = () => _keyboard.DataWithStrobe;
        }
        if (diskInterface)
        {
            _fdc = new Intel8271(_clock);
            _fdcDrive = new(_fdc, index => _disks[index], _clock);
        }
        if (card == SystemCpuCard.Motorola6809)
        {
            _cpu6809 = new Cpu6809(this);
            _cpu6809.SetIrq(_via?.Irq == true);
            _cpu6809.SetNmi(_fdc?.Nmi == true);
            _cpu6809.SetFirq(false);
            if (_via is not null) _via.IrqChanged = _cpu6809.SetIrq;
            if (_fdc is not null) _fdc.NmiChanged = _cpu6809.SetNmi;
        }
        else
        {
            _cpu6502 = new Cpu6502(this, _clock);
            // The CPU samples these retained input levels at its own phases.
            // Only mounted devices can publish a change; no board-wide polling is needed.
            _cpu6502.SetIrq(_via?.Irq == true);
            _cpu6502.SetNmi(_fdc?.Nmi == true);
            if (_via is not null) _via.IrqChanged = _cpu6502.SetIrq;
            if (_fdc is not null) _fdc.NmiChanged = _cpu6502.SetNmi;
        }
        Reset(true);
    }

    public CpuBusControlSignals ControlSignals => CpuBusControlSignals.None;
    public bool Irq => _via?.Irq == true;
    public bool Nmi => _fdc?.Nmi == true;
    public Action<ReadOnlyMemory<byte>>? NetworkFrameTransmitted { get; set; }
    public Action<int>? NetworkPoll { get; set; }

    public void Reset(bool cold)
    {
        if (cold)
        {
            Array.Clear(_ram);
            if (_display is null) _keyboard.AdvanceTo(_ticks);
            _keyboard.Reset();
            _video?.Reset();
            _samples.Clear();
            _tapeOutputHigh = false;
            if (_tapeInterface is not null)
            {
                _tapeInterface.Reset(_ticks);
                _tapeInputPins = (byte)(_tapeInterface.InputPins
                    | (byte.MaxValue ^ (KeyboardPins.TapeInputBit | KeyboardPins.TapeCarrierBit)));
                _nextTapeClock = _tapeInterface.NextClockEvent;
            }
        }
        _via?.Reset();
        if (_card == SystemCpuCard.Motorola6809 && !_tapeCb2Driven) SetTapeOutput(true);
        _display?.Reset(_ticks);
        _ramIo?.Reset();
        if (cold || _tapeInterface is null) _tape.ResetHardware();
        if (_tapeInterface is not null) _tape.SetOutputLevel(_tapeInterface.TapeOutput);
        _fdc?.Reset();
        _fdcDrive?.Reset();
        if (_cpu6809 is not null) _cpu6809.Reset(); else _cpu6502!.Reset();
        _nextFrame = _ticks + _clock / C.PresentationRate;
    }

    public byte[] RunFrame()
    {
        var start = _ticks;
        var field = _video?.Frame;
        // The display oscillator determines field completion. The keypad and an
        // unprogrammed CRTC retain their software presentation interval.
        while (_video?.IsProgrammed == true ? _video.Frame == field : _ticks < _nextFrame)
        {
            if (_cpu6809 is not null) _cpu6809.Step(); else _cpu6502!.Step();
        }
        _lastFrameTicks = _ticks - start;
        _nextFrame = _video is null ? _nextFrame + _clock / C.PresentationRate : _ticks + _clock / C.PresentationRate;
        return _video?.CompletedFrame ?? _display!.Render(_ticks);
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
        // The System 1 has a passive scanned keypad, not the ASCII keyboard's repeater.
        if (_ticks >= _nextKeyboardClock) _keyboard.AdvanceTo(_ticks);
        if (_ticks >= _nextTapeClock) _tapeInterface!.AdvanceTo(_ticks);
        if (_tapeClockRequired) _tape.Tick();
        _fdcDrive?.Tick();
        if (_via is not null)
        {
            _viaPhase += C.ViaClock;
            while (_viaPhase >= _clock) { _viaPhase -= _clock; _via.Tick(); }
        }
        // VDU40 CLK is PH2 (1 MHz); VDU80 receives the fast card's 2 MHz.
        // Each configured card supplies one character clock per CPU bus period.
        _video?.TickCharacter();
        _audioPhase += C.AudioSampleRate;
        if (_audioPhase >= _clock)
        {
            _audioPhase -= _clock;
            _samples.Add((short)(_tape.MonitorSample / C.TapeMonitorDivisor));
        }
    }

    private void SetTapeOutput(bool high)
    {
        if (_tapeOutputHigh == high) return;
        _tapeOutputHigh = high;
        if (_tapeInterface is null) _tape.SetOutputBit(high);
        else _tapeInterface.SetOutputBit(high, _ticks);
    }

    public void SetInput(EmulationInputSnapshot snapshot)
    {
        if (_display is null) _keyboard.AdvanceTo(_ticks);
        _keyboard.SetInput(snapshot);
        if (_card == SystemCpuCard.Fast6502A) _via!.SetCa1(!_keyboard.Blank);
        if (_keyboard.Break && !_break) Reset(false);
        _break = _keyboard.Break;
    }

    public byte[]? ReadNonvolatileMemory() => null;
    public void WriteNonvolatileMemory(ReadOnlySpan<byte> bytes) { }
    public bool ReceiveNetworkFrame(ReadOnlySpan<byte> frame, bool validFcs = true) => false;
    public void SetNetworkLine(bool clockPresent, bool clearToSend = true, bool idle = true) { }
    public void SetOption(string key, string value) => throw new ArgumentOutOfRangeException(nameof(key));
}
