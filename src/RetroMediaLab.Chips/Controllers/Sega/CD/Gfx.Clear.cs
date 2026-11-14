using RetroMediaLab.Chips.Common.Memory;

using System.Buffers.Binary;


namespace RetroMediaLab.Chips.Controllers.Sega.CD;

public sealed partial class Gfx
{
    private void ClearState()
    {
        gfx.cycles = default;
        gfx.cyclesPerLine = default;
        gfx.dotMask = default;
        gfx.tracePtrOffset = 0;
        gfx.mapPtrOffset = 0;
        gfx.stampShift = default;
        gfx.mapShift = default;
        gfx.bufferOffset = default;
        gfx.bufferStart = default;
        gfx.lut_offset.AsSpan().Clear();
        gfx.lut_prio.AsSpan().Clear();
        gfx.lut_pixel.AsSpan().Clear();
        gfx.lut_cell.AsSpan().Clear();
    }
}
