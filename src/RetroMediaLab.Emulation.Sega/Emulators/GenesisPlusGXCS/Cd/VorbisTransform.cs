using System.Runtime.CompilerServices;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Video;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cd;

internal sealed partial class VorbisTransform
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int MULT32(int x, int y) => unchecked((int)(((long)x * y) >> 32));
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int MULT31(int x, int y) => unchecked(MULT32(x, y) << 1);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int MULT31_SHIFT15(int x, int y) => unchecked((int)(((long)x * y) >> 15));
    public void Inverse(int size, Span<int> samples)
    {
        if (size is < 64 or > 8192 || (size & (size - 1)) != 0 || samples.Length < size) throw new ArgumentOutOfRangeException(nameof(size));
        mdct_backward(size, new(samples), new(samples));
    }
    private static ReadOnlySpan<int> Window(int size) => size switch
    {
        64 => vwin64, 128 => vwin128, 256 => vwin256, 512 => vwin512,
        1024 => vwin1024, 2048 => vwin2048, 4096 => vwin4096, 8192 => vwin8192,
        _ => throw new ArgumentOutOfRangeException(nameof(size))
    };
    public static void ApplyWindow(Span<int> data, int size, int leftSize, int rightSize)
    {
        if (data.Length < size) throw new ArgumentException("Vorbis window buffer is too short.", nameof(data));
        ReadOnlySpan<int> left = Window(leftSize), right = Window(rightSize);
        int leftBegin = size / 4 - leftSize / 4, leftEnd = leftBegin + leftSize / 2;
        int rightBegin = size / 2 + size / 4 - rightSize / 4, rightEnd = rightBegin + rightSize / 2;
        data[..leftBegin].Clear();
        for (int i = leftBegin, p = 0; i < leftEnd; i++, p++) data[i] = MULT31(data[i], left[p]);
        for (int i = rightBegin, p = rightSize / 2 - 1; i < rightEnd; i++, p--) data[i] = MULT31(data[i], right[p]);
        data.Slice(rightEnd, size - rightEnd).Clear();
    }
}
