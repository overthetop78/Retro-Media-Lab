using RetroMediaLab.Chips.Common.Memory;

using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace RetroMediaLab.Chips.Video.Sega.VDP;

public sealed partial class Vdp
{
    private readonly ushort[] _palette16 = new ushort[256];
    private NtscFilter? _mdNtsc, _smsNtsc;
    private bool _ntscRgb555;
    private int _paletteDepth;
    public int EffectivePixelDepth => config.ntsc != 0 ? (config.PixelDepth == 15 ? 15 : 16) : config.PixelDepth;
    public int OutputWidth
    {
        get
        {
            int width = bitmap.viewport.w + 2 * bitmap.viewport.x;
            if (config.ntsc == 0) return width;
            return (reg[12] & 1) != 0 ? (width - 3) / 4 * 8 + 8 : (width / 3 + 1) * 7;
        }
    }

    public void RefreshVideoConfiguration()
    {
        _paletteDepth = EffectivePixelDepth;
        InitializePalette();
        if (config.ntsc != 0) InitializeNtsc();
        if ((reg[1] & 4) != 0)
        {
            color_update_m5(0, BinaryPrimitives.ReadUInt16LittleEndian(cram.AsSpan(border << 1, 2)));
            for (int i = 1; i < 64; i++) color_update_m5(i, BinaryPrimitives.ReadUInt16LittleEndian(cram.AsSpan(i << 1, 2)));
        }
        else
        {
            for (int i = 0; i < 32; i++) color_update_m4(i, BinaryPrimitives.ReadUInt16LittleEndian(cram.AsSpan(i << 1, 2)));
            color_update_m4(0x40, BinaryPrimitives.ReadUInt16LittleEndian(cram.AsSpan((0x10 | (border & 15)) << 1, 2)));
        }
    }

    private void InitializeNtsc()
    {
        bool rgb555 = EffectivePixelDepth == 15;
        if (_mdNtsc is null || _smsNtsc is null || _ntscRgb555 != rgb555)
        {
            _mdNtsc = new NtscFilter(true, rgb555, config.MdNtscSetup);
            _smsNtsc = new NtscFilter(false, rgb555, config.SmsNtscSetup);
            _ntscRgb555 = rgb555;
        }
        else
        {
            _mdNtsc.Initialize(config.MdNtscSetup);
            _smsNtsc.Initialize(config.SmsNtscSetup);
        }
    }

    private void remap_line(int line)
    {
        int depth = EffectivePixelDepth;
        if (_paletteDepth != depth || config.ntsc != 0 && (_mdNtsc is null || _smsNtsc is null)) RefreshVideoConfiguration();
        if (depth == 32) { remap_line_32(line); return; }
        int width = bitmap.viewport.w + 2 * bitmap.viewport.x;
        ReadOnlySpan<byte> source = linebuf.AsSpan(0x20 - bitmap.viewport.x, width);
        line = (line + bitmap.viewport.y) % lines_per_frame;
        if (line < 0) return;
        if (interlaced != 0 && config.render != 0) line = line * 2 + odd_frame;
        Span<byte> row = bitmap.data.AsSpan(line * bitmap.pitch, bitmap.pitch);
        if (config.ntsc != 0)
        {
            var filter = (reg[12] & 1) != 0 ? _mdNtsc! : _smsNtsc!;
            filter.Blit(_palette16, source, MemoryMarshal.Cast<byte, ushort>(row));
            return;
        }
        for (int x = 0; x < width; x++)
        {
            uint value = pixel[source[x]];
            if (config.lcd != 0)
            {
                uint old = depth == 8 ? row[x] : BinaryPrimitives.ReadUInt16LittleEndian(row.Slice(x * 2, 2));
                int rShift = depth == 8 ? 5 : depth == 15 ? 10 : 11;
                int gShift = depth == 8 ? 2 : 5;
                int rMask = depth == 8 ? 7 : 31;
                int gMask = depth == 8 ? 7 : depth == 15 ? 31 : 63;
                int bMask = depth == 8 ? 3 : 31;
                int red = (int)(value >> rShift) & rMask;
                int green = (int)(value >> gShift) & gMask;
                int blue = (int)value & bMask;
                int redDecay = ((int)(old >> rShift) & rMask) - red;
                int greenDecay = ((int)(old >> gShift) & gMask) - green;
                int blueDecay = ((int)old & bMask) - blue;
                if (redDecay > 0) red += config.lcd * redDecay >> 8;
                if (greenDecay > 0) green += config.lcd * greenDecay >> 8;
                if (blueDecay > 0) blue += config.lcd * blueDecay >> 8;
                value = (uint)(red << rShift | green << gShift | blue | (depth == 15 ? 0x8000 : 0));
            }
            if (depth == 8) row[x] = (byte)value;
            else BinaryPrimitives.WriteUInt16LittleEndian(row.Slice(x * 2, 2), (ushort)value);
        }
    }
}
