using System.Numerics;
using System.Runtime.InteropServices;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2;

internal sealed unsafe class PvrPixelBuffer<T> : IDisposable where T : unmanaged
{
    private T* _bufferStart;
    private T* _currentMipmap;
    private T* _currentLine;
    private T* _currentPixel;
    private uint _pixelsPerLine;

    ~PvrPixelBuffer() => Deinit();

    public void Dispose()
    {
        Deinit();
        GC.SuppressFinalize(this);
    }

    internal void Init(uint width, uint height, bool mipmapped)
    {
        Deinit();
        nuint size = (nuint)(width * height) * (uint)sizeof(T);
        if (mipmapped)
        {
            do
            {
                width /= 2;
                height /= 2;
                size += (nuint)(width * height) * (uint)sizeof(T);
            }
            while (width != 0 && height != 0);
        }

        _bufferStart = _currentLine = _currentPixel = _currentMipmap = (T*)NativeMemory.Alloc(size);
        _pixelsPerLine = 1;
    }

    internal void Init(uint width, uint height)
    {
        Deinit();
        _bufferStart = _currentLine = _currentPixel = _currentMipmap =
            (T*)NativeMemory.Alloc((nuint)(width * height) * (uint)sizeof(T));
        _pixelsPerLine = width;
    }

    internal void Deinit()
    {
        if (_bufferStart != null)
        {
            NativeMemory.Free(_bufferStart);
            _bufferStart = _currentMipmap = _currentLine = _currentPixel = null;
        }
    }

    internal void StealData(PvrPixelBuffer<T> buffer)
    {
        Deinit();
        _bufferStart = _currentMipmap = _currentLine = _currentPixel = buffer._bufferStart;
        _pixelsPerLine = buffer._pixelsPerLine;
        buffer._bufferStart = buffer._currentMipmap = buffer._currentLine = buffer._currentPixel = null;
    }

    internal void SetMipmap(int level)
    {
        uint offset = 0;
        for (int i = 0; i < level; i++)
            offset += 1u << (2 * i);
        _currentMipmap = _currentLine = _currentPixel = _bufferStart + offset;
        _pixelsPerLine = 1u << level;
    }

    internal T* Data(uint x = 0, uint y = 0) => _currentMipmap + _pixelsPerLine * y + x;

    internal void Prel(uint x, T value) => _currentPixel[x] = value;
    internal void Prel(uint x, uint y, T value) => _currentPixel[y * _pixelsPerLine + x] = value;

    internal void RmoveX(uint value) => _currentPixel += value;

    internal void RmoveY(uint value)
    {
        _currentLine += _pixelsPerLine * value;
        _currentPixel = _currentLine;
    }

    internal void Amove(uint x, uint y)
    {
        _currentLine = _currentMipmap + _pixelsPerLine * y;
        _currentPixel = _currentLine + x;
    }
}

internal interface IPvrColorPacker
{
    static abstract uint Pack(int r, int g, int b, int a);
}

internal readonly struct PvrRgbaPacker : IPvrColorPacker
{
    public static uint Pack(int r, int g, int b, int a) => unchecked((uint)(r | (g << 8) | (b << 16) | (a << 24)));
}

internal readonly struct PvrBgraPacker : IPvrColorPacker
{
    public static uint Pack(int r, int g, int b, int a) => unchecked((uint)(b | (g << 8) | (r << 16) | (a << 24)));
}

internal interface IPvrUnpacker<T> where T : unmanaged
{
    static abstract T Unpack(uint word);
}

internal readonly struct PvrNopUnpacker<T> : IPvrUnpacker<T> where T : unmanaged, IBinaryInteger<T>
{
    public static T Unpack(uint word) => T.CreateTruncating(word);
}

internal readonly struct PvrUnpacker1555 : IPvrUnpacker<ushort>
{
    public static ushort Unpack(uint word) => (ushort)(((word >> 15) & 1)
        | (((word >> 10) & 0x1F) << 11) | (((word >> 5) & 0x1F) << 6) | ((word & 0x1F) << 1));
}

internal readonly struct PvrUnpacker4444 : IPvrUnpacker<ushort>
{
    public static ushort Unpack(uint word) => (ushort)(((word & 0xF) << 4) | (((word >> 4) & 0xF) << 8)
        | (((word >> 8) & 0xF) << 12) | ((word >> 12) & 0xF));
}

internal readonly struct PvrUnpacker1555To32<TPacker> : IPvrUnpacker<uint> where TPacker : IPvrColorPacker
{
    public static uint Unpack(uint word) => TPacker.Pack(
        (int)((((word >> 10) & 0x1F) << 3) | ((word >> 12) & 7)),
        (int)((((word >> 5) & 0x1F) << 3) | ((word >> 7) & 7)),
        (int)(((word & 0x1F) << 3) | ((word >> 2) & 7)),
        (word & 0x8000) != 0 ? 0xFF : 0);
}

internal readonly struct PvrUnpacker565To32<TPacker> : IPvrUnpacker<uint> where TPacker : IPvrColorPacker
{
    public static uint Unpack(uint word) => TPacker.Pack(
        (int)((((word >> 11) & 0x1F) << 3) | ((word >> 13) & 7)),
        (int)((((word >> 5) & 0x3F) << 2) | ((word >> 9) & 3)),
        (int)(((word & 0x1F) << 3) | ((word >> 2) & 7)),
        0xFF);
}

internal readonly struct PvrUnpacker4444To32<TPacker> : IPvrUnpacker<uint> where TPacker : IPvrColorPacker
{
    public static uint Unpack(uint word) => TPacker.Pack(
        (int)((((word >> 8) & 0xF) << 4) | ((word >> 8) & 0xF)),
        (int)((((word >> 4) & 0xF) << 4) | ((word >> 4) & 0xF)),
        (int)(((word & 0xF) << 4) | (word & 0xF)),
        (int)((((word >> 12) & 0xF) << 4) | ((word >> 12) & 0xF)));
}

internal readonly struct PvrUnpacker8888<TPacker> : IPvrUnpacker<uint> where TPacker : IPvrColorPacker
{
    public static uint Unpack(uint word) =>
        TPacker.Pack((int)((word >> 16) & 0xFF), (int)((word >> 8) & 0xFF), (int)(word & 0xFF), (int)((word >> 24) & 0xFF));
}

internal enum PvrTextureType
{
    _565,
    _5551,
    _4444,
    _8888,
    _8
}

internal unsafe delegate void PvrTextureConverter<T>(PvrPixelBuffer<T> buffer, byte* input, uint width, uint height)
    where T : unmanaged;

internal readonly record struct PvrTextureInfo(
    string Name,
    int BitsPerPixel,
    PvrTextureType Type = default,
    PvrTextureConverter<ushort>? Tw = null,
    PvrTextureConverter<ushort>? Vq = null,
    PvrTextureConverter<uint>? Pl32 = null,
    PvrTextureConverter<uint>? Tw32 = null,
    PvrTextureConverter<uint>? Vq32 = null,
    PvrTextureConverter<uint>? PlVq32 = null,
    PvrTextureConverter<byte>? Tw8 = null);

internal static partial class PvrTextureConversion
{
    internal const int VqCodebookSize = 256 * 8;
}
