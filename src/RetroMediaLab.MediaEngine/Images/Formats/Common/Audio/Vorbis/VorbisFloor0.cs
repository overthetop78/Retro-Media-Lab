using System.IO;


namespace RetroMediaLab.MediaEngine.Images.Formats.Common.Audio.Vorbis;

internal sealed partial class VorbisFloor0 : VorbisFloor
{
    private readonly int _order, _rate, _barkSize, _ampBits, _ampDb, _shortCount, _memoLength;
    private readonly int[] _books, _shortMap, _longMap, _lspLook;
    public override int MemoLength => _memoLength;
    private VorbisFloor0(VorbisBits bits, VorbisCodebook[] books, int[] blockSizes)
    {
        _order = bits.Read(8); _rate = bits.Read(16); _barkSize = bits.Read(16);
        _ampBits = bits.Read(6); _ampDb = bits.Read(8);
        int count = bits.Read(4) + 1;
        if (_order < 1 || _rate < 1 || _barkSize < 1 || count < 1 || _ampBits is < 0 or > 32 || _ampDb < 0)
            throw new InvalidDataException("Invalid Vorbis floor-0 setup.");
        _books = new int[count];
        int maximumDimension = 0;
        for (int i = 0; i < count; i++)
        {
            int book = _books[i] = bits.Read(8);
            if ((uint)book >= books.Length) throw new InvalidDataException("Invalid Vorbis floor-0 codebook.");
            maximumDimension = Math.Max(maximumDimension, books[book].Dimension);
        }
        _memoLength = checked(_order + maximumDimension + 1);
        _shortCount = blockSizes[0] / 2;
        _shortMap = BuildMap(_shortCount);
        _longMap = blockSizes[0] == blockSizes[1] ? _shortMap : BuildMap(blockSizes[1] / 2);
        _lspLook = new int[_barkSize];
        unchecked { for (int i = 0; i < _barkSize; i++) _lspLook[i] = vorbis_coslook2_i(0x10000 * i / _barkSize); }
    }
    public static VorbisFloor0 Unpack(VorbisBits bits, VorbisCodebook[] books, int[] blockSizes) => new(bits, books, blockSizes);
    private int[] BuildMap(int count)
    {
        int[] result = new int[count + 1];
        unchecked
        {
            int bark = toBARK(_rate / 2);
            for (int i = 0; i < count; i++)
            {
                int value = (_barkSize * ((toBARK(_rate / 2 * i / count) << 11) / bark)) >> 11;
                result[i] = value >= _barkSize ? _barkSize - 1 : value;
            }
        }
        result[count] = -1; return result;
    }
    public override bool Decode(VorbisBits bits, VorbisCodebook[] books, Span<int> memo)
    {
        int amplitude = bits.Read(_ampBits);
        if (amplitude <= 0) return false;
        unchecked
        {
            int maximum = (1 << _ampBits) - 1;
            if (maximum == 0) return false;
            amplitude = ((amplitude * _ampDb) << 4) / maximum;
            int bookNumber = bits.Read(VorbisCodebook.ILog((uint)_books.Length));
            if ((uint)bookNumber >= _books.Length) return false;
            VorbisCodebook book = books[_books[bookNumber]];
            for (int i = 0; i < _order; i += book.Dimension)
                if (!book.DecodeVector(bits, memo[i..], book.Dimension, -24, add: false)) return false;
            int last = 0;
            for (int i = 0; i < _order;)
            {
                for (int k = 0; k < book.Dimension; k++, i++) memo[i] += last;
                last = memo[i - 1];
            }
            memo[_order] = amplitude;
        }
        return true;
    }
    public override void Apply(Span<int> data, int count, Span<int> memo, bool present)
    {
        if (!present) { data[..count].Clear(); return; }
        int[] map = count == _shortCount ? _shortMap : _longMap;
        vorbis_lsp_to_curve(new(data), new(map), count, _barkSize, new(memo), _order, memo[_order], _ampDb, new(_lspLook));
    }
    private static int MULT32(int x, int y) => VorbisTransform.MULT32(x, y);
    private static int MULT31_SHIFT15(int x, int y) => VorbisTransform.MULT31_SHIFT15(x, y);
    private static int labs(int value) => Math.Abs(value);
    private static void ClearCurve(VorbisIntPointer curve, int count) => curve.AsSpan(count).Clear();
}
