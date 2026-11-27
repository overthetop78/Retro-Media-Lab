using System;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2;

internal static unsafe partial class PvrOsd
{
    internal static readonly uint[,] VmuLcdData = new uint[8, 48 * 32];
    internal static readonly bool[] VmuLcdStatus = new bool[8];
    internal static readonly ulong[] VmuLastChanged = new ulong[8];

    internal static void PushVmuScreen(int busId, int busPort, byte* buffer)
    {
        int vmuId = busId * 2 + busPort;
        if (vmuId < 0 || vmuId >= VmuLcdData.GetLength(0))
            return;
        for (int i = 0; i < VmuLcdData.GetLength(1); i++, buffer++)
            VmuLcdData[vmuId, i] = *buffer != 0 ? 0xFFFFFFFFu : 0xFF000000u;
        VmuLcdStatus[vmuId] = true;
        VmuLastChanged[vmuId] = PvrRuntime.GetTimeMs();
    }

    private static readonly uint[] LightgunCrosshairData =
    [
        0, 0, 0, 0, 0, 0, 0, uint.MaxValue, uint.MaxValue, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, uint.MaxValue, uint.MaxValue, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, uint.MaxValue, uint.MaxValue, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, uint.MaxValue, uint.MaxValue, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, uint.MaxValue, uint.MaxValue, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, uint.MaxValue, uint.MaxValue, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        uint.MaxValue, uint.MaxValue, uint.MaxValue, uint.MaxValue, uint.MaxValue, uint.MaxValue, 0, 0, 0, 0, uint.MaxValue, uint.MaxValue, uint.MaxValue, uint.MaxValue, uint.MaxValue, uint.MaxValue,
        uint.MaxValue, uint.MaxValue, uint.MaxValue, uint.MaxValue, uint.MaxValue, uint.MaxValue, 0, 0, 0, 0, uint.MaxValue, uint.MaxValue, uint.MaxValue, uint.MaxValue, uint.MaxValue, uint.MaxValue,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, uint.MaxValue, uint.MaxValue, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, uint.MaxValue, uint.MaxValue, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, uint.MaxValue, uint.MaxValue, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, uint.MaxValue, uint.MaxValue, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, uint.MaxValue, uint.MaxValue, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, uint.MaxValue, uint.MaxValue, 0, 0, 0, 0, 0, 0, 0,
    ];

    internal static ReadOnlySpan<uint> GetCrosshairTextureData() => LightgunCrosshairData;

    internal static (float X, float Y) GetCrosshairPosition(int playerNum)
    {
        float x = PvrRuntime.MouseXAbsolute[playerNum];
        float y = PvrRuntime.MouseYAbsolute[playerNum];
        float width = 640f;
        float height = 480f;
        if (PvrConfiguration.Rotate90)
        {
            float previousY = y;
            y = 639f - x;
            x = previousY;
            (width, height) = (height, width);
        }
        float scale = height / PvrRuntime.DisplayHeight;
        y /= scale;
        scale /= PvrConfiguration.ScreenStretching / 100f;
        x = x / scale + (PvrRuntime.DisplayWidth - width / scale) / 2f;
        return (x, y);
    }
}
