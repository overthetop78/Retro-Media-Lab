using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Sound;

internal enum PsgType { Discrete, Integrated }

internal sealed class Psg
{
    private const int ClockRatio = 15 * 16;
    internal const int ContextSize = 31 * sizeof(int);
    private readonly BlipBuffer _output;
    private readonly int[] _registers = new int[8];
    private readonly int[] _frequencyIncrement = new int[4];
    private readonly int[] _frequencyCounter = new int[4];
    private readonly int[] _polarity = new int[4];
    private readonly int[] _channelDelta = new int[8];
    private readonly int[] _channelOutput = new int[8];
    private readonly int[] _channelAmplification = new int[8];
    private int _clocks;
    private int _latch;
    private int _zeroFrequencyIncrement;
    private int _noiseShiftValue;
    private int _noiseShiftWidth;
    private int _noiseBitMask;

    public bool HighQuality { get; set; }
    private readonly int[] _channelVolumes = [100, 100, 100, 100];
    /// <summary>Host mixing levels in percent; these do not alter emulated registers.</summary>
    public Span<int> ChannelVolumes => _channelVolumes;

    public Psg(BlipBuffer output, PsgType type)
    {
        _output = output;
        Initialize(type);
    }

    public void Initialize(PsgType type)
    {
        Array.Fill(_channelAmplification, 100);
        _zeroFrequencyIncrement = (type == PsgType.Discrete ? 0x400 : 1) * ClockRatio;
        _noiseShiftWidth = type == PsgType.Discrete ? 14 : 15;
        _noiseBitMask = type == PsgType.Discrete ? 6 : 9;
    }

    public void Reset()
    {
        Array.Clear(_registers);
        Array.Clear(_frequencyCounter);
        Array.Clear(_channelDelta);
        Array.Clear(_channelOutput);
        for (int i = 0; i < 4; i++)
        {
            _frequencyIncrement[i] = i < 3 ? _zeroFrequencyIncrement : 16 * ClockRatio;
            _polarity[i] = -1;
        }
        _latch = 3;
        _noiseShiftValue = 1 << _noiseShiftWidth;
        _clocks = 0;
    }

    public int SaveContext(Span<byte> state)
    {
        if (state.Length < ContextSize) throw new ArgumentException("PSG context buffer is too small.", nameof(state));
        int offset = 0;
        WriteInt(state, ref offset, _clocks);
        WriteInt(state, ref offset, _latch);
        WriteInt(state, ref offset, _noiseShiftValue);
        WriteArray(state, ref offset, _registers);
        WriteArray(state, ref offset, _frequencyIncrement);
        WriteArray(state, ref offset, _frequencyCounter);
        WriteArray(state, ref offset, _polarity);
        WriteArray(state, ref offset, _channelOutput);
        return offset;
    }

    public int LoadContext(ReadOnlySpan<byte> state)
    {
        if (state.Length < ContextSize) throw new ArgumentException("PSG context buffer is too small.", nameof(state));
        int left = 0, right = 0;
        if ((_noiseShiftValue & 1) != 0)
        {
            left = -_channelOutput[6];
            right = -_channelOutput[7];
        }
        for (int i = 0; i < 3; i++)
            if (_polarity[i] > 0)
            {
                left -= _channelOutput[i * 2];
                right -= _channelOutput[i * 2 + 1];
            }
        int offset = 0;
        _clocks = ReadInt(state, ref offset);
        _latch = ReadInt(state, ref offset) & 7;
        _noiseShiftValue = ReadInt(state, ref offset);
        ReadArray(state, ref offset, _registers);
        ReadArray(state, ref offset, _frequencyIncrement);
        ReadArray(state, ref offset, _frequencyCounter);
        ReadArray(state, ref offset, _polarity);
        ReadArray(state, ref offset, _channelOutput);
        if ((_noiseShiftValue & 1) != 0)
        {
            left += _channelOutput[6];
            right += _channelOutput[7];
        }
        for (int i = 0; i < 3; i++)
            if (_polarity[i] > 0)
            {
                left += _channelOutput[i * 2];
                right += _channelOutput[i * 2 + 1];
            }
        AddDelta(_clocks, left, right);
        return offset;
    }

    public void Write(uint clocks, uint data)
    {
        Synchronize(clocks);
        int index = (data & 0x80) != 0 ? _latch = (int)(data >> 4) & 7 : _latch;
        switch (index)
        {
            case 0:
            case 2:
            case 4:
                data = (data & 0x80) != 0
                    ? (uint)(_registers[index] & 0x3f0) | (data & 15)
                    : (uint)(_registers[index] & 15) | ((data & 0x3f) << 4);
                _frequencyIncrement[index >> 1] = data != 0 ? (int)data * ClockRatio : _zeroFrequencyIncrement;
                if (index == 4 && (_registers[6] & 3) == 3)
                    _frequencyIncrement[3] = _frequencyIncrement[2];
                break;
            case 6:
                int frequency = (int)data & 3;
                if (frequency == 3)
                {
                    _frequencyIncrement[3] = _frequencyIncrement[2];
                    _frequencyCounter[3] = _frequencyCounter[2];
                }
                else _frequencyIncrement[3] = (0x10 << frequency) * ClockRatio;
                if ((_noiseShiftValue & 1) != 0)
                {
                    _channelDelta[6] -= _channelOutput[6];
                    _channelDelta[7] -= _channelOutput[7];
                }
                _noiseShiftValue = 1 << _noiseShiftWidth;
                break;
            default:
                int channel = index >> 1;
                data = (uint)Volumes[(int)data & 15];
                SetVolume(channel, (int)data, channel == 3 ? (_noiseShiftValue & 1) != 0 : _polarity[channel] > 0);
                break;
        }
        _registers[index] = (int)data;
    }

    public void Configure(uint clocks, uint preamp, uint panning)
    {
        Synchronize(clocks);
        for (int i = 0; i < 4; i++)
        {
            int level = (int)preamp * _channelVolumes[i] / 100;
            _channelAmplification[i * 2] = level * (int)(panning >> (i + 4) & 1);
            _channelAmplification[i * 2 + 1] = level * (int)(panning >> i & 1);
            SetVolume(i, _registers[i * 2 + 1], i < 3 ? _polarity[i] > 0 : (_noiseShiftValue & 1) != 0);
        }
    }

    public void EndFrame(uint clocks)
    {
        Synchronize(clocks);
        _clocks = unchecked(_clocks - (int)clocks);
        for (int i = 0; i < 4; i++) _frequencyCounter[i] = unchecked(_frequencyCounter[i] - (int)clocks);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Synchronize(uint clocks)
    {
        if (clocks <= (uint)_clocks) return;
        Update(clocks);
        _clocks = unchecked(_clocks + (int)((clocks - (uint)_clocks + ClockRatio - 1) / ClockRatio) * ClockRatio);
    }

    private void SetVolume(int channel, int volume, bool high)
    {
        int offset = channel * 2;
        int left = volume * _channelAmplification[offset] / 100;
        int right = volume * _channelAmplification[offset + 1] / 100;
        if (high)
        {
            _channelDelta[offset] += left - _channelOutput[offset];
            _channelDelta[offset + 1] += right - _channelOutput[offset + 1];
        }
        _channelOutput[offset] = left;
        _channelOutput[offset + 1] = right;
    }

    private void Update(uint clocks)
    {
        for (int i = 0; i < 4; i++)
        {
            int offset = i * 2;
            if ((_channelDelta[offset] | _channelDelta[offset + 1]) != 0)
            {
                AddDelta(_clocks, _channelDelta[offset], _channelDelta[offset + 1]);
                _channelDelta[offset] = _channelDelta[offset + 1] = 0;
            }
            int timestamp = _frequencyCounter[i];
            int polarity = _polarity[i];
            if (i < 3)
            {
                while ((uint)timestamp < clocks)
                {
                    polarity = -polarity;
                    AddDelta(timestamp, polarity * _channelOutput[offset], polarity * _channelOutput[offset + 1]);
                    timestamp += _frequencyIncrement[i];
                }
            }
            else
            {
                int shift = _noiseShiftValue;
                while ((uint)timestamp < clocks)
                {
                    polarity = -polarity;
                    if (polarity > 0)
                    {
                        int output = shift & 1;
                        shift = (shift >> 1) | (((_registers[6] & 4) != 0 ? NoiseFeedback[shift & _noiseBitMask] : output) << _noiseShiftWidth);
                        output = (shift & 1) - output;
                        AddDelta(timestamp, output * _channelOutput[6], output * _channelOutput[7]);
                    }
                    timestamp += _frequencyIncrement[3];
                }
                _noiseShiftValue = shift;
            }
            _frequencyCounter[i] = timestamp;
            _polarity[i] = polarity;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AddDelta(int timestamp, int left, int right)
    {
        if (HighQuality) _output.AddDelta((uint)timestamp, left, right);
        else _output.AddDeltaFast((uint)timestamp, left, right);
    }

    private static void WriteInt(Span<byte> target, ref int offset, int value)
    {
        BinaryPrimitives.WriteInt32LittleEndian(target[offset..], value);
        offset += 4;
    }

    private static int ReadInt(ReadOnlySpan<byte> source, ref int offset)
    {
        int value = BinaryPrimitives.ReadInt32LittleEndian(source[offset..]);
        offset += 4;
        return value;
    }

    private static void WriteArray(Span<byte> target, ref int offset, int[] values)
    {
        foreach (int value in values) WriteInt(target, ref offset, value);
    }

    private static void ReadArray(ReadOnlySpan<byte> source, ref int offset, int[] values)
    {
        for (int i = 0; i < values.Length; i++) values[i] = ReadInt(source, ref offset);
    }

    private static ReadOnlySpan<byte> NoiseFeedback => [0, 1, 1, 0, 1, 0, 0, 1, 1, 0];
    private static ReadOnlySpan<int> Volumes =>
    [
        2800, (int)(2800 * 0.794328234), (int)(2800 * 0.630957344), (int)(2800 * 0.501187233),
        (int)(2800 * 0.398107170), (int)(2800 * 0.316227766), (int)(2800 * 0.251188643), (int)(2800 * 0.199526231),
        (int)(2800 * 0.158489319), (int)(2800 * 0.125892541), (int)(2800 * 0.1), (int)(2800 * 0.079432823),
        (int)(2800 * 0.063095734), (int)(2800 * 0.050118723), (int)(2800 * 0.039810717), 0
    ];
}
