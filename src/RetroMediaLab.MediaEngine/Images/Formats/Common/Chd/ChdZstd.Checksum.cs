using System.IO;
using System.Buffers.Binary;
using System.Numerics;

namespace RetroMediaLab.MediaEngine.Images.Formats.Common.Chd;

internal sealed partial class ChdZstd
{
    private const ulong Prime1 = 0x9e3779b185ebca87UL, Prime2 = 0xc2b2ae3d27d4eb4fUL;
    private const ulong Prime3 = 0x165667b19e3779f9UL, Prime4 = 0x85ebca77c2b2ae63UL, Prime5 = 0x27d4eb2f165667c5UL;
    private static ulong HashRound(ulong value, ulong input) => unchecked(BitOperations.RotateLeft(value + input * Prime2, 31) * Prime1);
    private static ulong HashMerge(ulong hash, ulong value) => unchecked((hash ^ HashRound(0, value)) * Prime1 + Prime4);
    private static ulong XxHash64(ReadOnlySpan<byte> input)
    {
        unchecked
        {
            int length = input.Length;
            ulong hash;
            if (input.Length >= 32)
            {
                ulong a = Prime1 + Prime2, b = Prime2, c = 0, d = 0UL - Prime1;
                do
                {
                    a = HashRound(a, BinaryPrimitives.ReadUInt64LittleEndian(input));
                    b = HashRound(b, BinaryPrimitives.ReadUInt64LittleEndian(input[8..]));
                    c = HashRound(c, BinaryPrimitives.ReadUInt64LittleEndian(input[16..]));
                    d = HashRound(d, BinaryPrimitives.ReadUInt64LittleEndian(input[24..]));
                    input = input[32..];
                } while (input.Length >= 32);
                hash = BitOperations.RotateLeft(a, 1) + BitOperations.RotateLeft(b, 7) + BitOperations.RotateLeft(c, 12) + BitOperations.RotateLeft(d, 18);
                hash = HashMerge(hash, a); hash = HashMerge(hash, b); hash = HashMerge(hash, c); hash = HashMerge(hash, d);
            }
            else hash = Prime5;
            hash += (uint)length;
            while (input.Length >= 8)
            {
                hash ^= HashRound(0, BinaryPrimitives.ReadUInt64LittleEndian(input));
                hash = BitOperations.RotateLeft(hash, 27) * Prime1 + Prime4; input = input[8..];
            }
            if (input.Length >= 4)
            {
                hash ^= BinaryPrimitives.ReadUInt32LittleEndian(input) * Prime1;
                hash = BitOperations.RotateLeft(hash, 23) * Prime2 + Prime3; input = input[4..];
            }
            foreach (byte value in input) { hash ^= value * Prime5; hash = BitOperations.RotateLeft(hash, 11) * Prime1; }
            hash ^= hash >> 33; hash *= Prime2; hash ^= hash >> 29; hash *= Prime3; hash ^= hash >> 32;
            return hash;
        }
    }
}
