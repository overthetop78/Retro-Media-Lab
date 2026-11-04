using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using C = RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Constants.ElectronVideoConstants;

namespace RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Services;

public sealed partial class ElectronVideo
{
    private static readonly byte[] DecodedPens = CreateDecodedPens();
    private int _pixelColor;

    public int PixelColor
    {
        get
        {
            var color = BitConverter.IsLittleEndian ? _pixelColor : BinaryPrimitives.ReverseEndianness(_pixelColor);
            return ((color & C.ByteMask) << C.BlueShift) | (color & (C.ByteMask << C.GreenShift))
                | ((color >> C.BlueShift) & C.ByteMask);
        }
    }

    private static byte[] CreateDecodedPens()
    {
        var result = new byte[C.ByteMask + 1];
        for (var value = 0; value < result.Length; value++)
            result[value] = (byte)(((value >> C.FirstPlaneBit) & 1) << C.FirstPenBit
                | ((value >> C.SecondPlaneBit) & 1) << C.SecondPenBit
                | ((value >> C.ThirdPlaneBit) & 1) << C.ThirdPenBit
                | ((value >> C.FourthPlaneBit) & 1));
        return result;
    }

    public void WritePalette(int register, byte value)
    {
        _registers[register] = (byte)(value ^ C.ByteMask);
        var pair = register & C.PalettePairMask;
        var even = _registers[pair];
        var odd = _registers[pair + 1];
        var first = C.PaletteBases[pair >> C.PalettePairShift];
        for (var entry = 0; entry < 4; entry++)
        {
            var red = (odd >> entry) & 1;
            var green = entry < 2 ? (odd >> (entry + 4)) & 1 : (even >> entry) & 1;
            var blue = (even >> (entry + 4)) & 1;
            var logical = first + (entry & 1) * 2 + (entry >> 1) * 8;
            var packed = blue * C.ByteMask | (green * C.ByteMask << C.GreenShift) | (red * C.ByteMask << C.BlueShift);
            _packedPalette[logical] = BitConverter.IsLittleEndian ? packed : BinaryPrimitives.ReverseEndianness(packed);
        }
    }

    private void RenderPoint(int dot)
    {
        if (dot >= C.Width)
        {
            _pixelColor = 0;
            return;
        }
        var byteMask = _bytePoints - 1;
        if (_line >= C.Height)
        {
            _pixelColor = 0;
            if ((dot & byteMask) == byteMask) _byteAddress = Wrap(_byteAddress + C.ActiveRasters);
            return;
        }
        var fetching = _line < _modeHeight && _raster < C.ActiveRasters;
        if (fetching && (dot & byteMask) == 0)
        {
            VideoAddress = _byteAddress | (_raster & C.RasterAddressMask);
            _data = _nextVideoData;
        }

        var packedColor = 0;
        if (fetching)
        {
            var shifted = (byte)(_data << ((dot & byteMask) >> _pixelShift));
            var pen = DecodedPens[shifted] & _penMask;
            packedColor = _packedPalette[pen];
        }
        var offset = (_line * C.Width + dot) * C.PixelBytes;
        // The early exits guarantee a visible four-byte pixel; its unused byte remains zero.
        Unsafe.WriteUnaligned(ref _drawing[offset], packedColor);
        _pixelColor = packedColor;

        // The address counter keeps running through the two hidden text rasters.
        if ((dot & byteMask) == byteMask)
            _byteAddress = Wrap(_byteAddress + C.ActiveRasters);
    }
}
