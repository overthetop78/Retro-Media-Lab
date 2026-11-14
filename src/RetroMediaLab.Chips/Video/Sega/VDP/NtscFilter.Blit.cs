using RetroMediaLab.Chips.Common.Memory;

using System.Runtime.CompilerServices;

namespace RetroMediaLab.Chips.Video.Sega.VDP;

public sealed partial class NtscFilter
{
    public int OutputWidth(int inputWidth) => _megaDrive ? (inputWidth - 3) / 4 * 8 + 8 : (inputWidth / 3 + 1) * 7;

    public int Blit(ReadOnlySpan<ushort> palette, ReadOnlySpan<byte> input, Span<ushort> output)
    {
        int expected = OutputWidth(input.Length);
        if (output.Length < expected) throw new ArgumentException("NTSC output buffer is too small.", nameof(output));
        Span<int> current = stackalloc int[4];
        Span<int> previous = stackalloc int[4];
        int border = KernelOffset(palette[0]);
        previous.Fill(border);
        current[0] = border;
        int inputPosition, outputPosition = 0;
        if (_megaDrive)
        {
            current[1] = KernelOffset(palette[input[0]]);
            current[2] = KernelOffset(palette[input[1]]);
            current[3] = KernelOffset(palette[input[2]]);
            inputPosition = 3;
            for (int n = input.Length / 4 - 1; n != 0; n--)
                for (int slot = 0; slot < 4; slot++)
                {
                    previous[slot] = current[slot];
                    current[slot] = KernelOffset(palette[input[inputPosition++]]);
                    output[outputPosition++] = OutputMd(slot * 2, current, previous);
                    output[outputPosition++] = OutputMd(slot * 2 + 1, current, previous);
                }
            for (int slot = 0; slot < 4; slot++)
            {
                previous[slot] = current[slot];
                current[slot] = slot == 0 ? KernelOffset(palette[input[inputPosition++]]) : border;
                output[outputPosition++] = OutputMd(slot * 2, current, previous);
                output[outputPosition++] = OutputMd(slot * 2 + 1, current, previous);
            }
        }
        else
        {
            int chunks = input.Length / 3;
            int extra = input.Length - chunks * 3;
            uint extra2 = unchecked((uint)-(extra >> 1 & 1));
            uint extra1 = unchecked((uint)-(extra & 1)) | extra2;
            current[1] = KernelOffset((ushort)(palette[input[0]] & extra2));
            current[2] = KernelOffset((ushort)(palette[input[(int)(extra2 & 1)]] & extra1));
            inputPosition = extra;
            for (int n = chunks; n != 0; n--)
            {
                for (int slot = 0; slot < 3; slot++)
                {
                    previous[slot] = current[slot];
                    current[slot] = KernelOffset(palette[input[inputPosition++]]);
                    output[outputPosition++] = OutputSms(slot * 2, current, previous);
                    output[outputPosition++] = OutputSms(slot * 2 + 1, current, previous);
                    if (slot == 2) output[outputPosition++] = OutputSms(6, current, previous);
                }
            }
            for (int slot = 0; slot < 3; slot++)
            {
                previous[slot] = current[slot];
                current[slot] = border;
                output[outputPosition++] = OutputSms(slot * 2, current, previous);
                output[outputPosition++] = OutputSms(slot * 2 + 1, current, previous);
                if (slot == 2) output[outputPosition++] = OutputSms(6, current, previous);
            }
        }
        return outputPosition;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int KernelOffset(ushort color)
    {
        int value = color;
        if (_megaDrive)
            return _rgb555
                ? ((value << 8 & 0x1c00) | (value & 0x0380) | (value >> 8 & 0x0070)) * 2
                : (value << 9 & 0x3800) | (value & 0x0700) | (value >> 8 & 0x00e0);
        return _rgb555
            ? ((value << 9 & 0x3c00) | (value & 0x03c0) | (value >> 9 & 0x003c)) * _entrySize / 4
            : ((value << 10 & 0x7800) | (value & 0x0780) | (value >> 9 & 0x0078)) * _entrySize / 8;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ushort OutputMd(int x, ReadOnlySpan<int> current, ReadOnlySpan<int> previous)
    {
        uint raw = unchecked(_table[current[0] + x] + _table[current[1] + (x + 6) % 8 + 16]
            + _table[current[2] + (x + 4) % 8] + _table[current[3] + (x + 2) % 8 + 16]
            + _table[previous[0] + x + 8] + _table[previous[1] + (x + 6) % 8 + 24]
            + _table[previous[2] + (x + 4) % 8 + 8] + _table[previous[3] + (x + 2) % 8 + 24]);
        return ToPixel(Clamp(raw));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ushort OutputSms(int x, ReadOnlySpan<int> current, ReadOnlySpan<int> previous)
    {
        uint raw = unchecked(_table[current[0] + x] + _table[current[1] + (x + 12) % 7 + 14]
            + _table[current[2] + (x + 10) % 7 + 28] + _table[previous[0] + (x + 7) % 14]
            + _table[previous[1] + (x + 5) % 7 + 21] + _table[previous[2] + (x + 3) % 7 + 35]);
        return ToPixel(Clamp(raw));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ushort ToPixel(uint raw) => (ushort)(_rgb555
        ? (raw >> 14 & 0x7c00) | (raw >> 9 & 0x03e0) | (raw >> 4 & 0x001f)
        : (raw >> 13 & 0xf800) | (raw >> 8 & 0x07e0) | (raw >> 4 & 0x001f));
}
