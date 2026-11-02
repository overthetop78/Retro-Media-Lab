namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Constants;

/// <summary>Dimensions du tampon du rendu BBC courant ; elles ne définissent pas toutes les machines Acorn.</summary>
internal static class VideoConstants
{
    internal const int Width = 640;
    internal const int Height = 256;
    internal const int NtscHeight = 200;
    internal const int TeletextCellWidth = 16;
    internal const int BitmapCellWidth = 8;
    internal const int WideBitmapCellWidth = 16;
    internal const int PaletteEntries = 16;
    internal const int CursorPaletteOffset = PaletteEntries;
    internal const int BitmapByteValues = 256;
    internal const int BitmapDecodeModes = 4;
    private static readonly byte[] BitmapPixels = CreateBitmapPixels();
    private static readonly int[] TeletextMasks = [0x800, 0x800, 0x400, 0x200, 0x100, 0x100, 0x080, 0x040, 0x020, 0x020, 0x010, 0x008, 0x004, 0x004, 0x002, 0x001];
    private static readonly uint[] ColourPixels = [0xff000000, 0xffff0000, 0xff00ff00, 0xffffff00, 0xff0000ff, 0xffff00ff, 0xff00ffff, 0xffffffff];
    private static readonly uint[] MonochromePixels = [0xff000000, 0xff4c4c4c, 0xff959595, 0xffe1e1e1, 0xff1d1d1d, 0xff696969, 0xffb2b2b2, 0xffffffff];
    internal static ReadOnlySpan<int> TeletextPixelMasks => TeletextMasks;
    internal static ReadOnlySpan<uint> PackedColours => ColourPixels;
    internal static ReadOnlySpan<uint> PackedMonochrome => MonochromePixels;
    internal static ReadOnlySpan<byte> BitmapLogicalPixels => BitmapPixels;

    private static byte[] CreateBitmapPixels()
    {
        var result = new byte[BitmapDecodeModes * BitmapByteValues * BitmapCellWidth];
        for (var mode = 0; mode < BitmapDecodeModes; mode++)
        {
            var count = 1 << mode;
            for (var data = 0; data < BitmapByteValues; data++)
            for (var x = 0; x < BitmapCellWidth; x++)
            {
                var pixel = x * count / BitmapCellWidth;
                var logical = count == BitmapCellWidth ? ((data >> (7 - pixel)) & 1) * 15
                    : count == 4 ? (((data >> (7 - pixel)) & 1) * 5 | ((data >> (3 - pixel)) & 1) * 10)
                    : ((data >> (7 - pixel)) & 1) | (((data >> (5 - pixel)) & 1) << 1) | (((data >> (3 - pixel)) & 1) << 2) | (((data >> (1 - pixel)) & 1) << 3);
                result[(mode * BitmapByteValues + data) * BitmapCellWidth + x] = (byte)logical;
            }
        }
        return result;
    }
    /// <summary>Largeur du curseur ULA en octets vidéo, indexée par les trois bits supérieurs du contrôle.</summary>
    internal static IReadOnlyList<int> CursorWidths { get; } = Array.AsReadOnly<int>([0, 0, 1, 2, 1, 0, 2, 4]);
}
