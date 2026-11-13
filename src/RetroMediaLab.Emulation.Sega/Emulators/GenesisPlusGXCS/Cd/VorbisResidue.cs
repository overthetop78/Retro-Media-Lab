using System.IO;
using System.Numerics;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cd;

internal sealed class VorbisResidue
{
    private readonly int _type, _begin, _end, _grouping, _stages, _dimension, _partValues, _maxWords;
    private readonly int[] _cascades, _decodeMap, _classWords;
    private readonly VorbisCodebook _phrase;
    private readonly VorbisCodebook?[][] _stageBooks;
    private VorbisResidue(VorbisBits bits, VorbisCodebook[] books, int channels, int maxBlock)
    {
        _type = bits.Read(16);
        if (_type is < 0 or > 2) throw new InvalidDataException("Invalid Vorbis residue type.");
        _begin = bits.Read(24); _end = bits.Read(24); _grouping = bits.Read(24) + 1;
        int parts = bits.Read(6) + 1, groupBook = bits.Read(8);
        if (_begin < 0 || _end < 0 || _grouping < 1 || parts < 1 || (uint)groupBook >= books.Length)
            throw new InvalidDataException("Invalid Vorbis residue setup.");
        _phrase = books[groupBook]; _dimension = _phrase.Dimension;
        _cascades = new int[parts];
        int bookCount = 0;
        for (int i = 0; i < parts; i++)
        {
            int cascade = bits.Read(3), high = bits.Read(1);
            if (high != 0) cascade |= bits.Read(5) << 3;
            if (cascade < 0 || high < 0) throw new InvalidDataException("Truncated Vorbis residue cascade.");
            _cascades[i] = cascade; bookCount += BitOperations.PopCount((uint)cascade);
        }
        int[] bookList = new int[bookCount];
        for (int i = 0; i < bookCount; i++)
        {
            int book = bookList[i] = bits.Read(8);
            if ((uint)book >= books.Length) throw new InvalidDataException("Invalid Vorbis residue stage book.");
        }
        _stageBooks = new VorbisCodebook?[parts][];
        for (int i = 0, position = 0; i < parts; i++)
        {
            int stages = VorbisCodebook.ILog((uint)_cascades[i]); _stages = Math.Max(_stages, stages);
            _stageBooks[i] = new VorbisCodebook?[stages];
            for (int stage = 0; stage < stages; stage++)
                if ((_cascades[i] & (1 << stage)) != 0) _stageBooks[i][stage] = books[bookList[position++]];
        }
        int partValues = parts;
        for (int i = 1; i < _dimension; i++) partValues = checked(partValues * parts);
        _partValues = partValues; _decodeMap = new int[checked(partValues * _dimension)];
        for (int i = 0; i < partValues; i++)
        {
            int value = i, multiplier = partValues / parts;
            for (int k = 0; k < _dimension; k++)
            {
                int decoded = value / multiplier; value -= decoded * multiplier; multiplier /= parts;
                _decodeMap[i * _dimension + k] = decoded;
            }
        }
        int max = _type == 2 ? maxBlock * channels / 2 : maxBlock / 2;
        int partitions = Math.Max(0, Math.Min(_end, max) - _begin) / _grouping;
        _maxWords = (partitions + _dimension - 1) / _dimension;
        _classWords = new int[checked(_maxWords * (_type == 2 ? 1 : channels))];
    }
    public static VorbisResidue Unpack(VorbisBits bits, VorbisCodebook[] books, int channels, int maxBlock) => new(bits, books, channels, maxBlock);
    public void Decode(VorbisBits bits, int[][] input, ReadOnlySpan<bool> nonzero, int channels, int blockSize)
    {
        if (_type == 2) { DecodeInterleaved(bits, input, nonzero, channels, blockSize); return; }
        int used = 0;
        for (int i = 0; i < channels; i++) if (nonzero[i]) input[used++] = input[i];
        if (used == 0) return;
        int count = Math.Min(_end, blockSize / 2) - _begin;
        if (count <= 0) return;
        int partitions = count / _grouping;
        for (int stage = 0; stage < _stages; stage++)
        {
            for (int partition = 0, word = 0; partition < partitions; word++)
            {
                if (stage == 0)
                {
                    for (int channel = 0; channel < used; channel++)
                    {
                        int entry = _phrase.Decode(bits);
                        if ((uint)entry >= _partValues) return;
                        _classWords[channel * _maxWords + word] = entry * _dimension;
                    }
                }
                for (int k = 0; k < _dimension && partition < partitions; k++, partition++)
                {
                    int offset = _begin + partition * _grouping;
                    for (int channel = 0; channel < used; channel++)
                    {
                        int classId = _decodeMap[_classWords[channel * _maxWords + word] + k];
                        if ((_cascades[classId] & (1 << stage)) == 0) continue;
                        VorbisCodebook? book = _stageBooks[classId][stage];
                        if (book is null) continue;
                        bool complete = _type == 0
                            ? book.DecodeTransposedAdd(bits, input[channel].AsSpan(offset), _grouping, -8)
                            : book.DecodeVector(bits, input[channel].AsSpan(offset), _grouping, -8, add: true);
                        if (!complete) return;
                    }
                }
            }
        }
    }
    private void DecodeInterleaved(VorbisBits bits, int[][] input, ReadOnlySpan<bool> nonzero, int channels, int blockSize)
    {
        int count = Math.Min(_end, blockSize * channels / 2) - _begin;
        if (count <= 0) return;
        bool any = false;
        for (int i = 0; i < channels; i++) any |= nonzero[i];
        if (!any) return;
        int partitions = count / _grouping, samplesPerPartition = _grouping / channels, beginOffset = _begin / channels;
        for (int stage = 0; stage < _stages; stage++)
        {
            for (int partition = 0, word = 0; partition < partitions; word++)
            {
                if (stage == 0)
                {
                    int entry = _phrase.Decode(bits);
                    if ((uint)entry >= _partValues) return;
                    _classWords[word] = entry * _dimension;
                }
                for (int k = 0; k < _dimension && partition < partitions; k++, partition++)
                {
                    int classId = _decodeMap[_classWords[word] + k];
                    if ((_cascades[classId] & (1 << stage)) == 0) continue;
                    VorbisCodebook? book = _stageBooks[classId][stage];
                    if (book is not null && !book.DecodeInterleavedAdd(bits, input, partition * samplesPerPartition + beginOffset, channels, samplesPerPartition, -8)) return;
                }
            }
        }
    }
}
