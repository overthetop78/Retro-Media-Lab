using BlipBuffer = RetroMediaLab.Chips.Audio.Common.Resampling.BandLimitedBuffer;
using Equalizer = RetroMediaLab.Chips.Audio.Common.Resampling.ThreeBandEqualizer;
using Psg = RetroMediaLab.Chips.Audio.TexasInstruments.SN76489.Instruction.Sn76489;
using PsgType = RetroMediaLab.Chips.Audio.TexasInstruments.SN76489.Instruction.Sn76489Type;
using Ym2413 = RetroMediaLab.Chips.Audio.Yamaha.YM2413.Instruction.Ym2413;
using Ym2612 = RetroMediaLab.Chips.Audio.Yamaha.YM2612.Instruction.Ym2612;
using Ym3438 = RetroMediaLab.Chips.Audio.Yamaha.YM3438.Instruction.Ym3438;
using Opll = RetroMediaLab.Chips.Audio.Yamaha.YM2413.Instruction.CycleOpll;
namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Common.Audio;

internal sealed class SoundConfiguration
{
    public int Ym2612Type = Ym2612.Discrete;
    public bool UseYm3438;
    public uint Ym3438Type = Ym3438.ReadMode;
    public bool UseOpll;
    public int Ym2413Mode = 2;
    public bool HighQualityFm = true;
    public bool HighQualityPsg = true;
    public int FmPreamp = 100;
    public uint PsgPreamp = 100;
    public int Filter;
    public uint LowPassRange = 0x9999;
    public int LowFrequency = 200;
    public int HighFrequency = 8000;
    public int LowGain = 100, MidGain = 100, HighGain = 100;
    public bool Mono;
    public readonly int[] PsgChannelVolumes = [100, 100, 100, 100];
    public readonly int[] MdChannelVolumes = [100, 100, 100, 100, 100, 100];
    public readonly int[] SmsChannelVolumes = [100, 100, 100, 100, 100, 100, 100, 100, 100];
}

internal sealed partial class SoundUnit
{
    private const int Ym2612ClockRatio = 7 * 6;
    private readonly SoundConfiguration _configuration;
    private readonly BlipBuffer _output;
    private readonly bool _isMd;
    private readonly bool _fmEnabled;
    private readonly int[] _fmBuffer = new int[1080 * 2 * 24];
    private readonly int[] _fmLast = new int[2];
    private int _fmPosition, _fmCyclesRatio, _fmCyclesStart, _fmCyclesCount, _fmCyclesBusy;
    private Ym2612? _ym2612;
    private Ym2413? _ym2413;
    private Ym3438? _ym3438;
    private Opll? _opll;
    private readonly short[] _ym3438Accumulator = new short[24 * 2];
    private readonly int[] _ym3438Sample = new int[2];
    private int _ym3438Cycles;
    private readonly int[] _opllAccumulator = new int[18 * 2];
    private int _opllSample, _opllCycles, _opllStatus;
    public Psg Psg { get; }
    public BlipBuffer Output => _output;
    public void ConfigureChannels()
    {
        _configuration.PsgChannelVolumes.AsSpan().CopyTo(Psg.ChannelVolumes);
        if (_ym2612 is not null) _configuration.MdChannelVolumes.AsSpan().CopyTo(_ym2612.ChannelVolumes);
        if (_ym3438 is not null) _configuration.MdChannelVolumes.AsSpan().CopyTo(_ym3438.ChannelVolumes);
        if (_ym2413 is not null) _configuration.SmsChannelVolumes.AsSpan().CopyTo(_ym2413.ChannelVolumes);
        if (_opll is not null) _configuration.SmsChannelVolumes.AsSpan().CopyTo(_opll.ChannelVolumes);
    }

    public SoundUnit(byte hardware, SoundConfiguration configuration, BlipBuffer output)
    {
        _configuration = configuration;
        _output = output;
        _isMd = (hardware & 0x81) == 0x80;
        _fmEnabled = _isMd || (configuration.Ym2413Mode & 1) != 0;
        if (_isMd)
        {
            if (configuration.UseYm3438)
            {
                _ym3438 = new();
                _ym3438.Configure(configuration.Ym3438Type);
                _fmCyclesRatio = Ym2612ClockRatio;
            }
            else
            {
                _ym2612 = new();
                _ym2612.Configure(configuration.Ym2612Type);
                _fmCyclesRatio = Ym2612ClockRatio * 24;
            }
        }
        else if (configuration.UseOpll)
        {
            _opll = new();
            _fmCyclesRatio = 4 * 15;
        }
        else
        {
            _ym2413 = new();
            _fmCyclesRatio = 72 * 15;
        }
        Psg = new(output, hardware == 1 ? PsgType.Discrete : PsgType.Integrated)
        {
            HighQuality = configuration.HighQualityPsg
        };
        ConfigureChannels();
    }

    public void Reset()
    {
        ResetFm(0);
        Psg.Reset();
        Psg.Configure(0, _configuration.PsgPreamp, 0xff);
        _fmLast.AsSpan().Clear();
        _fmPosition = 0;
        _fmCyclesStart = _fmCyclesCount = 0;
    }

    private void Synchronize(int cycles)
    {
        if (cycles <= _fmCyclesCount) return;
        int samples = (cycles - _fmCyclesCount + _fmCyclesRatio - 1) / _fmCyclesRatio;
        Span<int> buffer = _fmBuffer.AsSpan(_fmPosition, samples * 2);
        if (_isMd)
        {
            if (_configuration.UseYm3438) UpdateYm3438(buffer, samples);
            else _ym2612!.Update(buffer, samples);
        }
        else if (_configuration.UseOpll) UpdateOpll(buffer, samples);
        else _ym2413!.Update(buffer, samples);
        _fmPosition += samples * 2;
        _fmCyclesCount += samples * _fmCyclesRatio;
    }

    private void UpdateYm3438(Span<int> buffer, int length)
    {
        for (int i = 0; i < length; i++)
        {
            _ym3438!.Clock(_ym3438Accumulator.AsSpan(_ym3438Cycles * 2, 2));
            _ym3438Cycles = (_ym3438Cycles + 1) % 24;
            if (_ym3438Cycles == 0)
            {
                _ym3438Sample.AsSpan().Clear();
                for (int j = 0; j < 24; j++)
                {
                    _ym3438Sample[0] += _ym3438Accumulator[j * 2];
                    _ym3438Sample[1] += _ym3438Accumulator[j * 2 + 1];
                }
            }
            buffer[i * 2] = _ym3438Sample[0] * 11;
            buffer[i * 2 + 1] = _ym3438Sample[1] * 11;
        }
    }

    private void UpdateOpll(Span<int> buffer, int length)
    {
        for (int i = 0; i < length; i++)
        {
            _opll!.Clock(_opllAccumulator.AsSpan(_opllCycles * 2, 2));
            _opllCycles = (_opllCycles + 1) % 18;
            if (_opllCycles == 0)
            {
                _opllSample = 0;
                for (int j = 0; j < 18; j++) _opllSample += _opllAccumulator[j * 2] + _opllAccumulator[j * 2 + 1];
            }
            buffer[i * 2] = buffer[i * 2 + 1] = _opllSample * 16 * _opllStatus;
        }
    }

    public void ResetFm(uint cycles)
    {
        Synchronize(unchecked((int)cycles));
        if (_isMd)
        {
            if (_configuration.UseYm3438) _ym3438!.Reset();
            else { _ym2612!.Reset(); _fmCyclesBusy = 0; }
        }
        else if (_configuration.UseOpll) _opll!.Reset(Opll.Ym2413);
        else _ym2413!.Reset();
    }

    public void WriteFm(uint cycles, uint address, uint data)
    {
        if (_isMd)
        {
            if (_configuration.UseYm3438)
            {
                Synchronize(unchecked((int)cycles));
                _ym3438!.Write(address, unchecked((byte)data));
            }
            else
            {
                if ((address & 1) != 0)
                {
                    Synchronize(unchecked((int)cycles));
                    if (_configuration.Ym2612Type < Ym2612.Enhanced)
                        _fmCyclesBusy = unchecked((int)(((cycles + Ym2612ClockRatio - 1) / Ym2612ClockRatio + 32) * Ym2612ClockRatio));
                }
                _ym2612!.Write(address, data);
            }
        }
        else if (_configuration.UseOpll)
        {
            if ((address & 2) == 0)
            {
                Synchronize(unchecked((int)cycles));
                _opll!.Write(address, unchecked((byte)data));
            }
            else _opllStatus = (int)data & 1;
        }
        else
        {
            if ((address & 1) != 0) Synchronize(unchecked((int)cycles));
            _ym2413!.Write(address, data);
        }
    }

    public uint ReadFm(uint cycles, uint address)
    {
        if (!_isMd) return _configuration.UseOpll ? (uint)(0xf8 | _opllStatus) : _ym2413!.Read();
        if (_configuration.UseYm3438)
        {
            Synchronize(unchecked((int)cycles));
            return _ym3438!.Read(address);
        }
        if (address != 0 && _configuration.Ym2612Type == Ym2612.Discrete) return 0;
        Synchronize(unchecked((int)cycles));
        return _ym2612!.Read() | (cycles >= (uint)_fmCyclesBusy ? 0u : 0x80u);
    }

    public int EndFrame(uint cycles)
    {
        Psg.EndFrame(cycles);
        if (_fmEnabled)
        {
            Synchronize(unchecked((int)cycles));
            int time = _fmCyclesStart;
            int previousLeft = _fmLast[0], previousRight = _fmLast[1];
            int position = 0;
            do
            {
                int left = unchecked(_fmBuffer[position++] * _configuration.FmPreamp) / 100;
                int right = unchecked(_fmBuffer[position++] * _configuration.FmPreamp) / 100;
                if (_configuration.HighQualityFm) _output.AddDelta((uint)time, left - previousLeft, right - previousRight);
                else _output.AddDeltaFast((uint)time, left - previousLeft, right - previousRight);
                previousLeft = left; previousRight = right;
                time += _fmCyclesRatio;
            } while ((uint)time < cycles);
            _fmPosition = 0;
            _fmLast[0] = previousLeft; _fmLast[1] = previousRight;
            _fmCyclesCount = _fmCyclesStart = time - unchecked((int)cycles);
            _fmCyclesBusy = (uint)_fmCyclesBusy > cycles ? _fmCyclesBusy - unchecked((int)cycles) : 0;
        }
        _output.EndFrame(cycles);
        return _output.SamplesAvailable;
    }
}
