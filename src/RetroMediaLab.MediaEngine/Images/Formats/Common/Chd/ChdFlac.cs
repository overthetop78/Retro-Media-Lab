using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;

namespace RetroMediaLab.MediaEngine.Images.Formats.Common.Chd;

// The CHD FLAC codecs omit STREAMINFO: the wrapper supplies 44100 Hz,
// two channels and 16 bits. Frame headers can still carry explicit sizes.
internal sealed class ChdFlac
{
    private long[] _left = [], _right = [];
    private ref struct Bits
    {
        private readonly ReadOnlySpan<byte> _source;
        private ulong _buffer;
        private int _count, _offset;
        internal readonly int Offset => _offset - _count / 8;
        internal Bits(ReadOnlySpan<byte> source) { _source = source; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ulong Read(int width)
        {
            if ((uint)width > 33) throw new InvalidDataException("Invalid CHD FLAC bit width.");
            while (_count < width)
            {
                if (_offset >= _source.Length) throw new InvalidDataException("Truncated CHD FLAC stream.");
                _buffer = (_buffer << 8) | _source[_offset++]; _count += 8;
            }
            _count -= width;
            return (_buffer >> _count) & ((1UL << width) - 1);
        }
        internal long Signed(int width)
        {
            if (width == 0) return 0;
            ulong value = Read(width), sign = 1UL << (width - 1);
            return unchecked((long)((value ^ sign) - sign));
        }
        internal uint Unary()
        {
            uint count = 0;
            while (Read(1) == 0)
            {
                if (count == uint.MaxValue) throw new InvalidDataException("Invalid CHD FLAC unary code.");
                count++;
            }
            return count;
        }
        internal void Align() { Read(_count & 7); }
    }

    public int DecodeFrames(ReadOnlySpan<byte> source, Span<byte> destination, bool bigEndian, CancellationToken cancellation)
    {
        if ((destination.Length & 3) != 0) throw new InvalidDataException("Invalid CHD FLAC sample count.");
        Bits bits = new(source);
        int written = 0;
        while (written < destination.Length)
        {
            cancellation.ThrowIfCancellationRequested();
            int frameStart = bits.Offset;
            if (bits.Read(14) != 0x3ffe || bits.Read(1) != 0) throw new InvalidDataException("Invalid CHD FLAC frame sync.");
            bits.Read(1); // fixed/variable block numbering
            int blockCode = (int)bits.Read(4), rateCode = (int)bits.Read(4), channelCode = (int)bits.Read(4), sizeCode = (int)bits.Read(3);
            if (bits.Read(1) != 0 || rateCode == 15 || channelCode != 1 && channelCode is not (8 or 9 or 10))
                throw new InvalidDataException("Invalid CHD FLAC frame parameters.");
            int sampleBits = sizeCode switch { 0 => 16, 1 => 8, 2 => 12, 4 => 16, 5 => 20, 6 => 24, 7 => 32, _ => 0 };
            if (sampleBits == 0) throw new InvalidDataException("Reserved CHD FLAC sample size.");
            int first = (int)bits.Read(8), extra = 0;
            if ((first & 0x80) != 0)
            {
                int mask = 0x80;
                while ((first & mask) != 0 && mask != 0) { extra++; mask >>= 1; }
                if (extra is < 2 or > 7) throw new InvalidDataException("Invalid CHD FLAC frame number.");
                for (int i = 1; i < extra; i++) bits.Read(8);
            }
            int block = blockCode switch
            {
                1 => 192, >= 2 and <= 5 => 576 << (blockCode - 2),
                6 => (int)bits.Read(8) + 1, 7 => (int)bits.Read(16) + 1,
                >= 8 => 256 << (blockCode - 8), _ => 0
            };
            if (block == 0) throw new InvalidDataException("Reserved CHD FLAC block size.");
            if (rateCode == 12) bits.Read(8); else if (rateCode is 13 or 14) bits.Read(16);
            int headerEnd = bits.Offset;
            byte headerCrc = (byte)bits.Read(8);
            if (HeaderCrc(source.Slice(frameStart, headerEnd - frameStart)) != headerCrc) throw new InvalidDataException("CHD FLAC header CRC mismatch.");
            if (_left.Length < block) { _left = new long[block]; _right = new long[block]; }
            DecodeSubframe(ref bits, _left.AsSpan(0, block), sampleBits + (channelCode == 9 ? 1 : 0), cancellation);
            DecodeSubframe(ref bits, _right.AsSpan(0, block), sampleBits + (channelCode is 8 or 10 ? 1 : 0), cancellation);
            bits.Align();
            int frameEnd = bits.Offset;
            ushort crc = (ushort)bits.Read(16);
            if (FrameCrc(source.Slice(frameStart, frameEnd - frameStart)) != crc) throw new InvalidDataException("CHD FLAC frame CRC mismatch.");
            int frames = Math.Min(block, (destination.Length - written) / 4);
            for (int i = 0; i < frames; i++)
            {
                long left = _left[i], right = _right[i];
                if (channelCode == 8) right = left - right;
                else if (channelCode == 9) left += right;
                else if (channelCode == 10)
                {
                    long mid = (left << 1) | (right & 1);
                    left = (mid + right) >> 1; right = (mid - right) >> 1;
                }
                short l = unchecked((short)(sampleBits <= 16 ? left << (16 - sampleBits) : left >> (sampleBits - 16)));
                short r = unchecked((short)(sampleBits <= 16 ? right << (16 - sampleBits) : right >> (sampleBits - 16)));
                if (bigEndian)
                { BinaryPrimitives.WriteInt16BigEndian(destination[written..], l); BinaryPrimitives.WriteInt16BigEndian(destination[(written + 2)..], r); }
                else
                { BinaryPrimitives.WriteInt16LittleEndian(destination[written..], l); BinaryPrimitives.WriteInt16LittleEndian(destination[(written + 2)..], r); }
                written += 4;
            }
        }
        return bits.Offset;
    }

    private static void DecodeSubframe(ref Bits bits, Span<long> samples, int sampleBits, CancellationToken cancellation)
    {
        if (bits.Read(1) != 0) throw new InvalidDataException("Invalid CHD FLAC subframe padding.");
        int type = (int)bits.Read(6);
        int wasted = bits.Read(1) != 0 ? checked((int)bits.Unary() + 1) : 0;
        int width = sampleBits - wasted;
        if (width <= 0) throw new InvalidDataException("Invalid CHD FLAC wasted bits.");
        if (type == 0) samples.Fill(bits.Signed(width));
        else if (type == 1)
        {
            for (int i = 0; i < samples.Length; i++) samples[i] = bits.Signed(width);
        }
        else
        {
            bool fixedPredictor = type is >= 8 and <= 12;
            if (!fixedPredictor && type < 32) throw new InvalidDataException("Reserved CHD FLAC subframe type.");
            int order = fixedPredictor ? type - 8 : type - 31;
            if (order > samples.Length) throw new InvalidDataException("CHD FLAC predictor exceeds block size.");
            for (int i = 0; i < order; i++) samples[i] = bits.Signed(width);
            Span<int> coefficients = stackalloc int[32];
            int shift = 0;
            if (fixedPredictor)
            {
                ReadOnlySpan<int> fixedCoefficients = order switch
                {
                    0 => [], 1 => [1], 2 => [2, -1], 3 => [3, -3, 1], _ => [4, -6, 4, -1]
                };
                fixedCoefficients.CopyTo(coefficients);
            }
            else
            {
                int precision = (int)bits.Read(4) + 1;
                if (precision == 16) throw new InvalidDataException("Reserved CHD FLAC predictor precision.");
                shift = (int)bits.Signed(5);
                for (int i = 0; i < order; i++) coefficients[i] = (int)bits.Signed(precision);
            }
            int method = (int)bits.Read(2), partitions = (int)bits.Read(4);
            if (method > 1 || (samples.Length & ((1 << partitions) - 1)) != 0) throw new InvalidDataException("Invalid CHD FLAC residual coding.");
            int partitionSize = samples.Length >> partitions;
            if (partitionSize < order) throw new InvalidDataException("Invalid CHD FLAC residual partition.");
            int position = order, riceWidth = method == 0 ? 4 : 5;
            for (int partition = 0; partition < 1 << partitions; partition++)
            {
                cancellation.ThrowIfCancellationRequested();
                int rice = (int)bits.Read(riceWidth), rawWidth = rice == (1 << riceWidth) - 1 ? (int)bits.Read(5) : -1;
                int count = partitionSize - (partition == 0 ? order : 0);
                for (int i = 0; i < count; i++)
                {
                    long residual;
                    if (rawWidth >= 0) residual = bits.Signed(rawWidth);
                    else
                    {
                        uint unary = bits.Unary();
                        ulong folded = ((ulong)unary << rice) | bits.Read(rice);
                        residual = unchecked((long)(folded >> 1) ^ -((long)folded & 1));
                    }
                    long prediction = 0;
                    for (int j = 0; j < order; j++) prediction += coefficients[j] * samples[position - j - 1];
                    prediction = shift >= 0 ? prediction >> shift : prediction << -shift;
                    samples[position++] = unchecked(prediction + residual);
                }
            }
        }
        if (wasted != 0) for (int i = 0; i < samples.Length; i++) samples[i] <<= wasted;
    }
    private static byte HeaderCrc(ReadOnlySpan<byte> source)
    {
        int crc = 0;
        foreach (byte value in source)
        {
            crc ^= value;
            for (int i = 0; i < 8; i++) crc = ((crc << 1) ^ ((crc & 0x80) != 0 ? 7 : 0)) & 255;
        }
        return (byte)crc;
    }
    private static ushort FrameCrc(ReadOnlySpan<byte> source)
    {
        int crc = 0;
        foreach (byte value in source)
        {
            crc ^= value << 8;
            for (int i = 0; i < 8; i++) crc = ((crc << 1) ^ ((crc & 0x8000) != 0 ? 0x8005 : 0)) & 65535;
        }
        return (ushort)crc;
    }
}
