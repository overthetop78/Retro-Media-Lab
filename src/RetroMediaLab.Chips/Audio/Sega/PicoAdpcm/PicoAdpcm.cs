using RetroMediaLab.Chips.Audio.Common.Resampling;
namespace RetroMediaLab.Chips.Audio.Sega.PicoAdpcm;

/// <summary>Pico slave ADPCM ASIC with its FIFO and programmable output filter.</summary>
/// <remarks>Decoder and filter adapted from PicoDrive pico/pico/xpcm.c (notaz, irixxxx),
/// derived from the MAME uPD7759 driver. The caller owns the output buffer and clock.</remarks>
public sealed partial class PicoAdpcm
{
    public const int AdpcmClock = 320000;
    private const int Start = 1, Header = 2, CountHeader = 3;
    private readonly BandLimitedBuffer _output;
    private readonly Action<bool> _interrupt;
    private readonly byte[] _fifo = new byte[64];
    private readonly int[] _x = new int[3], _y = new int[3];
    private int _head, _count, _phase = Start, _samplePosition, _sample, _decoderState;
    private int _nibbles, _silence, _rate, _high = 1, _cache, _control;
    private int _lastOutput, _filterIndex, _gain = 32, _sampleRate = 44100, _stepSamples;
    private int _filterA0, _filterA1, _filterGain;
    private uint _masterClock;
    private long _nextSampleTick;
    private bool _irq;
    private static ReadOnlySpan<int> StateDeltas => [-1,-1,0,0,1,2,2,3,-1,-1,0,0,1,2,2,3];
    private static ReadOnlySpan<int> StepDeltas => [
        0, 0, 1, 2, 3, 5, 7, 10, 0, 0, -1, -2, -3, -5, -7, -10,
        0, 1, 2, 3, 4, 6, 8, 13, 0, -1, -2, -3, -4, -6, -8, -13,
        0, 1, 2, 4, 5, 7, 10, 15, 0, -1, -2, -4, -5, -7, -10, -15,
        0, 1, 3, 4, 6, 9, 13, 19, 0, -1, -3, -4, -6, -9, -13, -19,
        0, 2, 3, 5, 8, 11, 15, 23, 0, -2, -3, -5, -8, -11, -15, -23,
        0, 2, 4, 7, 10, 14, 19, 29, 0, -2, -4, -7, -10, -14, -19, -29,
        0, 3, 5, 8, 12, 16, 22, 33, 0, -3, -5, -8, -12, -16, -22, -33,
        1, 4, 7, 10, 15, 20, 29, 43, -1, -4, -7, -10, -15, -20, -29, -43,
        1, 4, 8, 13, 18, 25, 35, 53, -1, -4, -8, -13, -18, -25, -35, -53,
        1, 6, 10, 16, 22, 31, 43, 64, -1, -6, -10, -16, -22, -31, -43, -64,
        2, 7, 12, 19, 27, 37, 51, 76, -2, -7, -12, -19, -27, -37, -51, -76,
        2, 9, 16, 24, 34, 46, 64, 96, -2, -9, -16, -24, -34, -46, -64, -96,
        3, 11, 19, 29, 41, 57, 79, 117, -3, -11, -19, -29, -41, -57, -79, -117,
        4, 13, 24, 36, 50, 69, 96, 143, -4, -13, -24, -36, -50, -69, -96, -143,
        4, 16, 29, 44, 62, 85, 118, 175, -4, -16, -29, -44, -62, -85, -118, -175,
        6, 20, 36, 54, 76, 104, 144, 214, -6, -20, -36, -54, -76, -104, -144, -214,
    ];
    public int DecodedSample => _sample;
    public bool InterruptPending => _irq;
    public int FifoCount => _count;
    public int FreeBytes => Math.Max(0, 63 - _count);
    public ushort Status => (ushort)((_control & 0x7fff) | (_count == 0 && _phase <= Start ? 0x8000 : 0));

    public PicoAdpcm(BandLimitedBuffer output, Action<bool> interrupt)
    {
        _output = output ?? throw new ArgumentNullException(nameof(output));
        _interrupt = interrupt ?? throw new ArgumentNullException(nameof(interrupt));
    }

    public void Configure(uint masterClock, int sampleRate)
    {
        ArgumentOutOfRangeException.ThrowIfZero(masterClock);
        ArgumentOutOfRangeException.ThrowIfLessThan(sampleRate, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(sampleRate, 384000);
        if (_sampleRate != sampleRate)
        {
            _nextSampleTick = _nextSampleTick * sampleRate / _sampleRate;
            Array.Clear(_x); Array.Clear(_y);
        }
        _masterClock = masterClock; _sampleRate = sampleRate;
        _stepSamples = (int)(((long)sampleRate << 16) / AdpcmClock);
        ConfigureFilter();
    }

    public void Reset()
    {
        Array.Clear(_fifo); Array.Clear(_x); Array.Clear(_y);
        _head = _count = _samplePosition = _sample = _decoderState = 0;
        _nibbles = _silence = _rate = _cache = _control = _lastOutput = _filterIndex = 0;
        _phase = Start; _high = 1; _gain = 32; _nextSampleTick = 0;
        SetInterrupt(false); ConfigureFilter();
    }

    public void WriteData(ushort value, uint cycles)
    {
        Update(cycles);
        Push((byte)(value >> 8)); Push((byte)value);
    }

    public void WriteControl(ushort value, uint cycles)
    {
        Update(cycles);
        int oldFilter = _filterIndex;
        _control = value;
        _gain = (8 - (value & 7)) * 4;
        _filterIndex = (value >> 6) & 3;
        if (_filterIndex != oldFilter) { Array.Clear(_x); Array.Clear(_y); ConfigureFilter(); }
        if ((value & 0x4000) == 0) SetInterrupt(false);
        if ((value & 0x8000) != 0)
        {
            _phase = Start; _sample = _samplePosition = _decoderState = 0;
            _nibbles = _silence = _count = _head = 0;
            SetInterrupt(false);
        }
    }

    public void AcknowledgeInterrupt() => SetInterrupt(false);

    public void Update(uint cycles)
    {
        if (_masterClock == 0) throw new InvalidOperationException("Configure the audio clock first.");
        long target = (long)cycles * _sampleRate;
        while (_nextSampleTick < target)
        {
            int oldCount = _count;
            int value = RenderSample();
            if (value != _lastOutput)
            {
                _output.AddDeltaFast((uint)(_nextSampleTick / _sampleRate), value - _lastOutput, value - _lastOutput);
                _lastOutput = value;
            }
            _nextSampleTick += _masterClock;
            if (_count != oldCount && _count < 16 && (_control & 0x4000) != 0) SetInterrupt(true);
        }
    }

    public void EndFrame(uint cycles)
    {
        Update(cycles);
        _nextSampleTick -= (long)cycles * _sampleRate;
    }

    private void Push(byte value)
    {
        if (_count == _fifo.Length) return;
        _fifo[(_head + _count) & 63] = value; _count++;
    }

    private byte Pop()
    {
        byte value = _fifo[_head]; _head = (_head + 1) & 63; _count--; return value;
    }

    private void SetInterrupt(bool value)
    {
        if (_irq == value) return;
        _irq = value; _interrupt(value);
    }

    private int RenderSample()
    {
        // Slave underflow holds the previous sample. Stream parsing resumes when data arrives.
        while (_samplePosition <= 0 && _count > 0)
        {
            if (_silence > 0) { _silence--; _sample = 0; _samplePosition += _stepSamples * 256; }
            else if (_nibbles > 0)
            {
                _nibbles--;
                if (_high != 0) _cache = Pop(); else _cache <<= 4;
                _high ^= 1;
                int nibble = (_cache >> 4) & 15;
                _sample += StepDeltas[_decoderState * 16 + nibble];
                _decoderState = Math.Clamp(_decoderState + StateDeltas[nibble], 0, 15);
                _samplePosition += _stepSamples * _rate;
            }
            else if (_phase == Start)
            {
                if (_fifo[_head] == 0) Pop(); else _phase = Header;
            }
            else if (_phase == Header)
            {
                int value = Pop();
                _nibbles = _silence = _rate = 0; _high = 1;
                if (value == 0)
                {
                    // Preserve the upstream slave-stream odd-byte terminator handling.
                    if (_count == 1) Pop();
                    _phase = Start;
                }
                else switch (value >> 6)
                {
                    case 0: _silence = (value & 63) + 1; break;
                    case 1: _rate = (value & 63) + 1; _nibbles = 256; break;
                    case 2: _rate = (value & 63) + 1; _phase = CountHeader; break;
                }
            }
            else if (_phase == CountHeader) { _nibbles = Pop() + 1; _phase = Header; }
        }
        int sample = Math.Clamp(_gain * _sample, -16384, 16383);
        if (_samplePosition > 0) _samplePosition -= 1 << 16;
        if (_filterA0 == 0) return sample;
        unchecked
        {
            _x[0] = _x[1]; _x[1] = _x[2]; _x[2] = sample * _filterGain;
            _y[0] = _y[1]; _y[1] = _y[2];
            _y[2] = (_x[0] + 2 * _x[1] + _x[2] + _y[0] * _filterA1 + _y[1] * _filterA0) >> 16;
            return _y[2];
        }
    }

    private void ConfigureFilter()
    {
        int cutoff = _filterIndex switch { 1 => 3000, 2 => 4500, 3 => 7500, _ => 0 };
        if (cutoff == 0 || cutoff >= _sampleRate / 2) { _filterA0 = _filterA1 = _filterGain = 0; return; }
        double a = 1 / Math.Tan(Math.PI * cutoff / _sampleRate), square = a * a;
        double gain = 1 / (1 + Math.Sqrt(2) * a + square);
        _filterGain = (int)(gain * 65536);
        _filterA0 = (int)(2 * (square - 1) * gain * 65536);
        _filterA1 = (int)(-(1 - Math.Sqrt(2) * a + square) * gain * 65536);
    }
}
