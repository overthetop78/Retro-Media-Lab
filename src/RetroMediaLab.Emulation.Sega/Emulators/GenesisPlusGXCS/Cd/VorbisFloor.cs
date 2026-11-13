using System.IO;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cd;

internal abstract class VorbisFloor
{
    public abstract int MemoLength { get; }
    public abstract bool Decode(VorbisBits bits, VorbisCodebook[] books, Span<int> memo);
    public abstract void Apply(Span<int> data, int count, Span<int> memo, bool present);
    public static VorbisFloor Unpack(VorbisBits bits, VorbisCodebook[] books, int[] blockSizes) => bits.Read(16) switch
    {
        0 => VorbisFloor0.Unpack(bits, books, blockSizes),
        1 => VorbisFloor1.Unpack(bits, books.Length),
        _ => throw new InvalidDataException("Unsupported Vorbis floor type.")
    };
}
