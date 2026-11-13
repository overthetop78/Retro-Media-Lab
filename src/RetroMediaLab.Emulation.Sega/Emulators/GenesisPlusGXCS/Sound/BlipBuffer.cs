using System.Runtime.CompilerServices;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Sound;

internal sealed class BlipBuffer
{
    private const int TimeBits = 52;
    private const ulong TimeUnit = 1UL << TimeBits;
    private const int BufferExtra = 18;
    private const int DeltaBits = 15;
    private const int DeltaUnit = 1 << DeltaBits;
    private readonly int[] _left;
    private readonly int[] _right;
    private ulong _factor = TimeUnit / (1 << 20);
    private ulong _offset;
    private int _integratorLeft;
    private int _integratorRight;

    public BlipBuffer(int sampleCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(sampleCount);
        _left = new int[checked(sampleCount + BufferExtra)];
        _right = new int[_left.Length];
        Clear();
    }

    public int SamplesAvailable => (int)(_offset >> TimeBits);

    public void SetRates(double clockRate, double sampleRate)
    {
        double factor = TimeUnit * sampleRate / clockRate;
        _factor = (ulong)factor;
        if (_factor < factor) _factor++;
    }

    public void Clear()
    {
        _offset = _factor / 2;
        _integratorLeft = _integratorRight = 0;
        Array.Clear(_left);
        Array.Clear(_right);
    }

    public int ClocksNeeded(int samples)
    {
        ulong needed = (ulong)samples * TimeUnit;
        return needed < _offset ? 0 : (int)((needed - _offset + _factor - 1) / _factor);
    }

    public void EndFrame(uint clocks) => _offset = unchecked(_offset + clocks * _factor);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddDelta(uint time, int left, int right)
    {
        if ((left | right) == 0) return;
        uint position = unchecked((uint)((time * _factor + _offset) >> 32));
        int phase = (int)(position >> 15 & 31);
        int interpolation = (int)(position & (DeltaUnit - 1));
        int index = (int)(position >> 20);
        int forward = phase * 8;
        int reverse = (32 - phase) * 8;
        int leftNext = unchecked(left * interpolation) >> DeltaBits;
        int rightNext = unchecked(right * interpolation) >> DeltaBits;
        left -= leftNext;
        right -= rightNext;
        var steps = Steps;
        for (int i = 0; i < 8; i++)
        {
            int a = steps[forward + i];
            int b = steps[forward + 8 + i];
            int c = steps[reverse + 7 - i];
            int d = steps[reverse - 1 - i];
            unchecked
            {
                _left[index + i] += a * left + b * leftNext;
                _right[index + i] += a * right + b * rightNext;
                _left[index + 8 + i] += c * left + d * leftNext;
                _right[index + 8 + i] += c * right + d * rightNext;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddDeltaFast(uint time, int left, int right)
    {
        if ((left | right) == 0) return;
        uint position = unchecked((uint)((time * _factor + _offset) >> 32));
        int interpolation = (int)(position >> 5 & (DeltaUnit - 1));
        int index = (int)(position >> 20);
        unchecked
        {
            int next = left * interpolation;
            _left[index + 7] += left * DeltaUnit - next;
            _left[index + 8] += next;
            next = right * interpolation;
            _right[index + 7] += right * DeltaUnit - next;
            _right[index + 8] += next;
        }
    }

    public int ReadSamples(Span<short> output, int count) => MixSamples([], output, count);

    public int MixSamples(ReadOnlySpan<BlipBuffer> others, Span<short> output, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (count > SamplesAvailable || count > output.Length / 2)
            throw new ArgumentOutOfRangeException(nameof(count));
        foreach (var other in others)
            if (count > other.SamplesAvailable) throw new ArgumentOutOfRangeException(nameof(count));
        int left = _integratorLeft;
        int right = _integratorRight;
        for (int i = 0; i < count; i++)
        {
            int sampleLeft = left >> DeltaBits;
            int sampleRight = right >> DeltaBits;
            unchecked
            {
                left += _left[i];
                right += _right[i];
                foreach (var other in others)
                {
                    left += other._left[i];
                    right += other._right[i];
                }
                sampleLeft = Math.Clamp(sampleLeft, short.MinValue, short.MaxValue);
                sampleRight = Math.Clamp(sampleRight, short.MinValue, short.MaxValue);
                output[i * 2] = (short)sampleLeft;
                output[i * 2 + 1] = (short)sampleRight;
                left -= sampleLeft << 6;
                right -= sampleRight << 6;
            }
        }
        _integratorLeft = left;
        _integratorRight = right;
        RemoveSamples(count);
        foreach (var other in others) other.RemoveSamples(count);
        return count;
    }

    private void RemoveSamples(int count)
    {
        int remaining = SamplesAvailable + BufferExtra - count;
        _offset -= (ulong)count * TimeUnit;
        _left.AsSpan(count, remaining).CopyTo(_left);
        _right.AsSpan(count, remaining).CopyTo(_right);
        _left.AsSpan(remaining, count).Clear();
        _right.AsSpan(remaining, count).Clear();
    }

    private static ReadOnlySpan<short> Steps =>
    [
           43, -115, 350, -488, 1136, -914, 5861, 21022,
           44, -118, 348, -473, 1076, -799, 5274, 21001,
           45, -121, 344, -454, 1011, -677, 4706, 20936,
           46, -122, 336, -431, 942, -549, 4156, 20829,
           47, -123, 327, -404, 868, -418, 3629, 20679,
           47, -122, 316, -375, 792, -285, 3124, 20488,
           47, -120, 303, -344, 714, -151, 2644, 20256,
           46, -117, 289, -310, 634, -17, 2188, 19985,
           46, -114, 273, -275, 553, 117, 1758, 19675,
           44, -108, 255, -237, 471, 247, 1356, 19327,
           43, -103, 237, -199, 390, 373, 981, 18944,
           42, -98, 218, -160, 310, 495, 633, 18527,
           40, -91, 198, -121, 231, 611, 314, 18078,
           38, -84, 178, -81, 153, 722, 22, 17599,
           36, -76, 157, -43, 80, 824, -241, 17092,
           34, -68, 135, -3, 8, 919, -476, 16558,
           32, -61, 115, 34, -60, 1006, -683, 16001,
           29, -52, 94, 70, -123, 1083, -862, 15422,
           27, -44, 73, 106, -184, 1152, -1015, 14824,
           25, -36, 53, 139, -239, 1211, -1142, 14210,
           22, -27, 34, 170, -290, 1261, -1244, 13582,
           20, -20, 16, 199, -335, 1301, -1322, 12942,
           18, -12, -3, 226, -375, 1331, -1376, 12293,
           15, -4, -19, 250, -410, 1351, -1408, 11638,
           13, 3, -35, 272, -439, 1361, -1419, 10979,
           11, 9, -49, 292, -464, 1362, -1410, 10319,
           9, 16, -63, 309, -483, 1354, -1383, 9660,
           7, 22, -75, 322, -496, 1337, -1339, 9005,
           6, 26, -85, 333, -504, 1312, -1280, 8355,
           4, 31, -94, 341, -507, 1278, -1205, 7713,
           3, 35, -102, 347, -506, 1238, -1119, 7082,
           1, 40, -110, 350, -499, 1190, -1021, 6464,
           0, 43, -115, 350, -488, 1136, -914, 5861
    ];
}
