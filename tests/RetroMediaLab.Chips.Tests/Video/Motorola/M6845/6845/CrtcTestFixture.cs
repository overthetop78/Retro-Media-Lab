using RetroMediaLab.Chips.Video.Motorola.M6845.Chip6845.Enums;
using RetroMediaLab.Chips.Video.Motorola.M6845.Chip6845.Services;

namespace RetroMediaLab.Chips.Tests.Video.Motorola.M6845.Chip6845;

internal static class CrtcTestFixture
{
    internal static Crtc6845 Create(Crtc6845Model model = Crtc6845Model.Hd6845S,
        byte total = 15, byte displayed = 8, byte rows = 5, byte rasters = 2, byte adjust = 0, byte mode = 0)
    {
        var crtc = new Crtc6845(model);
        Write(crtc, 0, total); Write(crtc, 1, displayed); Write(crtc, 2, 10); Write(crtc, 3, 0x23);
        Write(crtc, 4, rows); Write(crtc, 5, adjust); Write(crtc, 6, 2); Write(crtc, 7, 3);
        Write(crtc, 8, mode); Write(crtc, 9, rasters);
        Write(crtc, 10, 0x20); Write(crtc, 12, 0x12); Write(crtc, 13, 0x34);
        return crtc;
    }

    internal static void Write(Crtc6845 crtc, byte register, byte value)
    {
        crtc.Select(register); crtc.Write(value);
    }

    internal static int NextFrame(Crtc6845 crtc)
    {
        for (var clocks = 1; clocks <= 200_000; clocks++) if (crtc.Tick()) return clocks;
        throw new Xunit.Sdk.XunitException("No frame in bounded CRTC trace");
    }

    internal static void Clocks(Crtc6845 crtc, int count)
    {
        for (var tick = 0; tick < count; tick++) crtc.Tick();
    }
}
