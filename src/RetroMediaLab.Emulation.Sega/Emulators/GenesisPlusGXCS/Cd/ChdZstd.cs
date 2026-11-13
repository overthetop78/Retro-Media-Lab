using System.Buffers.Binary;
using System.IO;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cd;

internal sealed partial class ChdZstd
{
    private readonly FseTable _ll = new(9), _of = new(8), _ml = new(9);
    private readonly uint[] _repeat = new uint[3];
    private static int Read24(ReadOnlySpan<byte> data) => data[0] | data[1] << 8 | data[2] << 16;
    private static ReadOnlySpan<byte> Take(ref ReadOnlySpan<byte> source, int count)
    {
        if ((uint)count > (uint)source.Length) throw new InvalidDataException("Truncated CHD Zstandard frame.");
        ReadOnlySpan<byte> result = source[..count]; source = source[count..]; return result;
    }
    public void Decompress(ReadOnlySpan<byte> source, Span<byte> destination, CancellationToken cancellation)
    {
        int position = 0;
        while (!source.IsEmpty)
        {
            cancellation.ThrowIfCancellationRequested();
            uint magic = BinaryPrimitives.ReadUInt32LittleEndian(Take(ref source, 4));
            if ((magic & 0xfffffff0) == 0x184d2a50)
            {
                uint skip = BinaryPrimitives.ReadUInt32LittleEndian(Take(ref source, 4));
                if (skip > source.Length) throw new InvalidDataException("Truncated CHD Zstandard skippable frame.");
                source = source[(int)skip..]; continue;
            }
            if (magic != 0xfd2fb528) throw new InvalidDataException("Invalid CHD Zstandard frame signature.");
            int descriptor = Take(ref source, 1)[0], sizeCode = descriptor >> 6;
            if ((descriptor & 8) != 0) throw new InvalidDataException("Reserved CHD Zstandard frame flag.");
            bool single = (descriptor & 32) != 0, checksum = (descriptor & 4) != 0;
            ulong window = 0;
            if (!single)
            {
                int windowDescriptor = Take(ref source, 1)[0], log = (windowDescriptor >> 3) + 10;
                if (log > 31) throw new InvalidDataException("Excess CHD Zstandard window size.");
                window = 1UL << log; window += (window >> 3) * (uint)(windowDescriptor & 7);
            }
            int dictionaryBytes = (descriptor & 3) switch { 0 => 0, 1 => 1, 2 => 2, _ => 4 };
            ReadOnlySpan<byte> dictionary = Take(ref source, dictionaryBytes);
            if (dictionary.IndexOfAnyExcept((byte)0) >= 0) throw new InvalidDataException("CHD Zstandard frame requires a dictionary.");
            int sizeBytes = sizeCode switch { 0 => single ? 1 : 0, 1 => 2, 2 => 4, _ => 8 };
            ReadOnlySpan<byte> sizeData = Take(ref source, sizeBytes);
            ulong size = 0;
            for (int i = 0; i < sizeBytes; i++) size |= (ulong)sizeData[i] << (i * 8);
            if (sizeCode == 1) size += 256;
            if (single) window = size;
            int maxBlock = (int)Math.Min(window, 128 * 1024UL);
            int frameStart = position;
            _huffmanLog = 0; _ll.Valid = _of.Valid = _ml.Valid = false;
            _repeat[0] = 1; _repeat[1] = 4; _repeat[2] = 8;
            bool final;
            do
            {
                cancellation.ThrowIfCancellationRequested();
                int block = Read24(Take(ref source, 3)), type = (block >> 1) & 3, length = block >> 3;
                final = (block & 1) != 0;
                if (length > maxBlock || type == 3) throw new InvalidDataException("Invalid CHD Zstandard block header.");
                ReadOnlySpan<byte> data = Take(ref source, type == 1 ? 1 : length);
                if (type is 0 or 1)
                {
                    if (length > destination.Length - position) throw new InvalidDataException("Excess CHD Zstandard output.");
                    if (type == 0) data.CopyTo(destination[position..]); else destination.Slice(position, length).Fill(data[0]);
                    position += length;
                }
                else
                {
                    int before = position;
                    int literals = ReadLiterals(data);
                    DecodeSequences(data[literals..], destination, ref position, frameStart, cancellation);
                    if (position - before > maxBlock) throw new InvalidDataException("Excess CHD Zstandard decompressed block size.");
                }
            } while (!final);
            if (sizeBytes != 0 && (ulong)(position - frameStart) != size) throw new InvalidDataException("CHD Zstandard frame content size mismatch.");
            if (checksum)
            {
                uint expected = BinaryPrimitives.ReadUInt32LittleEndian(Take(ref source, 4));
                if ((uint)XxHash64(destination.Slice(frameStart, position - frameStart)) != expected) throw new InvalidDataException("CHD Zstandard frame checksum mismatch.");
            }
        }
        if (position != destination.Length) throw new InvalidDataException("CHD Zstandard output size mismatch.");
    }
    private static int ReadSequenceTable(FseTable table, int mode, ReadOnlySpan<byte> source, int maxSymbol, int maxLog, ReadOnlySpan<short> defaults, int defaultLog)
    {
        switch (mode)
        {
            case 0: table.Build(defaults, defaultLog); return 0;
            case 1:
                if (source.IsEmpty || source[0] > maxSymbol) throw new InvalidDataException("Invalid CHD Zstandard RLE sequence table.");
                table.Rle(source[0]); return 1;
            case 2: return table.Read(source, maxSymbol, maxLog);
            case 3:
                if (!table.Valid) throw new InvalidDataException("Missing CHD Zstandard repeated FSE table.");
                return 0;
            default: throw new InvalidDataException("Invalid CHD Zstandard sequence mode.");
        }
    }
    private void DecodeSequences(ReadOnlySpan<byte> source, Span<byte> output, ref int position, int frameStart, CancellationToken cancellation)
    {
        int count = Take(ref source, 1)[0];
        if (count == 255) count = BinaryPrimitives.ReadUInt16LittleEndian(Take(ref source, 2)) + 0x7f00;
        else if (count > 127) count = ((count - 128) << 8) + Take(ref source, 1)[0];
        int literalPosition = 0;
        if (count == 0)
        {
            if (!source.IsEmpty) throw new InvalidDataException("Excess CHD Zstandard empty sequence data.");
        }
        else
        {
            int modes = Take(ref source, 1)[0];
            if ((modes & 3) != 0) throw new InvalidDataException("Reserved CHD Zstandard sequence flags.");
            int read = ReadSequenceTable(_ll, modes >> 6, source, 35, 9, LL_defaultNorm, 6); source = source[read..];
            read = ReadSequenceTable(_of, (modes >> 4) & 3, source, 31, 8, OF_defaultNorm, 5); source = source[read..];
            read = ReadSequenceTable(_ml, (modes >> 2) & 3, source, 52, 9, ML_defaultNorm, 6); source = source[read..];
            ReverseBits bits = new(source);
            int llState = (int)bits.Read(_ll.Log), ofState = (int)bits.Read(_of.Log), mlState = (int)bits.Read(_ml.Log);
            for (int i = 0; i < count; i++)
            {
                if ((i & 1023) == 0) cancellation.ThrowIfCancellationRequested();
                FseEntry ll = _ll.Entries[llState], of = _of.Entries[ofState], ml = _ml.Entries[mlState];
                uint literalLength = LL_base[ll.Symbol], matchLength = ML_base[ml.Symbol], offset;
                int ofBits = of.Symbol;
                if (ofBits > 1)
                {
                    offset = checked(OF_base[of.Symbol] + bits.Read(ofBits));
                    _repeat[2] = _repeat[1]; _repeat[1] = _repeat[0]; _repeat[0] = offset;
                }
                else
                {
                    int zero = literalLength == 0 ? 1 : 0;
                    if (ofBits == 0)
                    {
                        offset = _repeat[zero]; _repeat[1] = _repeat[1 - zero]; _repeat[0] = offset;
                    }
                    else
                    {
                        int code = 1 + zero + (int)bits.Read(1);
                        offset = code == 3 ? _repeat[0] - 1 : _repeat[code];
                        if (offset == 0) throw new InvalidDataException("Invalid CHD Zstandard repeated offset.");
                        if (code != 1) _repeat[2] = _repeat[1];
                        _repeat[1] = _repeat[0]; _repeat[0] = offset;
                    }
                }
                matchLength += bits.Read(ML_bits[ml.Symbol]); literalLength += bits.Read(LL_bits[ll.Symbol]);
                if (literalLength > _literalCount - literalPosition || literalLength > output.Length - position) throw new InvalidDataException("Invalid CHD Zstandard literal sequence length.");
                _literals.AsSpan(literalPosition, (int)literalLength).CopyTo(output[position..]);
                literalPosition += (int)literalLength; position += (int)literalLength;
                if (offset == 0 || offset > position - frameStart || matchLength > output.Length - position) throw new InvalidDataException("Invalid CHD Zstandard match.");
                int from = position - (int)offset;
                if (offset >= matchLength) output.Slice(from, (int)matchLength).CopyTo(output[position..]);
                else for (int j = 0; j < matchLength; j++) output[position + j] = output[from + j];
                position += (int)matchLength;
                if (i + 1 < count)
                {
                    llState = ll.Next + (int)bits.Read(ll.Bits); mlState = ml.Next + (int)bits.Read(ml.Bits);
                    ofState = of.Next + (int)bits.Read(of.Bits);
                }
            }
            if (bits.Remaining != 0) throw new InvalidDataException("CHD Zstandard sequence stream length mismatch.");
        }
        int rest = _literalCount - literalPosition;
        if (rest > output.Length - position) throw new InvalidDataException("Excess CHD Zstandard final literals.");
        _literals.AsSpan(literalPosition, rest).CopyTo(output[position..]); position += rest;
    }
}
