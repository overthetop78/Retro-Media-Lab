using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Interfaces;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornElectron.Common.Constants.ElectronConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornElectron.Common.Services;

/// <summary>Carte Electron 32K : 6502, RAM partagée avec la vidéo et ULA de la machine.</summary>
internal sealed partial class ElectronMachine : ICpuBus, IAcornMachine
{
    private readonly byte[] _ram = new byte[C.RamBytes];
    private readonly byte[] _basic;
    private readonly byte[] _mos;
    private readonly Cpu6502 _cpu;
    private readonly ElectronKeyboard _keyboard = new();
    private readonly AnalogCassette _cassette = new(C.CpuClock);
    private readonly ElectronVideo _video;
    private readonly ElectronUla _ula;
    private readonly List<short> _audio = [];
    private int _audioPhase;
    private long _ticks;
    private bool _break;

    internal ElectronMachine(ReadOnlySpan<byte> mos, ReadOnlySpan<byte> basic)
    {
        if (mos.Length != C.RomBytes) throw new ArgumentException(nameof(mos));
        if (basic.Length != C.RomBytes) throw new ArgumentException(nameof(basic));
        _mos = mos.ToArray();
        _basic = basic.ToArray();
        _video = new ElectronVideo(address => _ram[address]);
        _ula = new ElectronUla(_video, _cassette);
        _cpu = new Cpu6502(this);
        Reset(true);
    }

    public bool Irq => _ula.Irq;
    public bool Nmi => false;
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
        var result = _audio.ToArray();
        _audio.Clear();
        return result;
    }

    public void Tick()
    {
        _ticks++;
        _ula.Tick();
        _audioPhase += C.AudioSampleRate;
        if (_audioPhase < C.CpuClock) return;
        _audioPhase -= C.CpuClock;
        _audio.Add((short)Math.Clamp(_ula.SoundSample + _cassette.MonitorSample, short.MinValue, short.MaxValue));
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
            _video.Reset();
            _keyboard.Reset();
            _audio.Clear();
            _audioPhase = 0;
            _ticks = 0;
        }
        _cassette.ResetHardware();
        _ula.Reset(cold);
        _cpu.Reset();
    }

    public byte[]? ReadNonvolatileMemory() => null;
    public void WriteNonvolatileMemory(ReadOnlySpan<byte> bytes) { }
    public bool ReceiveNetworkFrame(ReadOnlySpan<byte> frame, bool validFcs = true) => false;
    public void SetNetworkLine(bool clockPresent, bool clearToSend = true, bool idle = true) { }
    public void SetOption(string key, string value) => throw new ArgumentOutOfRangeException(nameof(key));
}
