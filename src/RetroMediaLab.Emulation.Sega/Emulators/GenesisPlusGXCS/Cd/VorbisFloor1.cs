using System.IO;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cd;

internal sealed partial class VorbisFloor1 : VorbisFloor
{
    private readonly int[] _partitions, _classDim = new int[16], _classSubs = new int[16], _classBook = new int[16];
    private readonly int[][] _subBooks = new int[16][];
    private readonly int[] _posts, _forward, _low, _high;
    private readonly int _multiplier, _quant;
    public override int MemoLength => _posts.Length;
    private VorbisFloor1(VorbisBits bits, int bookCount)
    {
        int partitions = bits.Read(5), maxClass = -1;
        if (partitions < 0) throw new InvalidDataException("Truncated Vorbis floor partitions.");
        _partitions = new int[partitions];
        for (int i = 0; i < partitions; i++)
        {
            int value = bits.Read(4);
            if (value < 0) throw new InvalidDataException("Truncated Vorbis floor classes.");
            _partitions[i] = value; maxClass = Math.Max(maxClass, value);
        }
        for (int i = 0; i <= maxClass; i++)
        {
            _classDim[i] = bits.Read(3) + 1; _classSubs[i] = bits.Read(2);
            if (_classSubs[i] < 0) throw new InvalidDataException("Truncated Vorbis floor class setup.");
            if (_classSubs[i] != 0) _classBook[i] = bits.Read(8);
            if ((uint)_classBook[i] >= bookCount) throw new InvalidDataException("Invalid Vorbis floor class book.");
            int[] sub = _subBooks[i] = new int[1 << _classSubs[i]];
            for (int j = 0; j < sub.Length; j++)
            {
                int book = sub[j] = bits.Read(8) - 1;
                if (book < -1 || book >= bookCount) throw new InvalidDataException("Invalid Vorbis floor subclass book.");
            }
        }
        _multiplier = bits.Read(2) + 1;
        int rangeBits = bits.Read(4), count = 2;
        if (_multiplier < 1 || rangeBits < 0) throw new InvalidDataException("Truncated Vorbis floor range.");
        foreach (int partition in _partitions) count += _classDim[partition];
        if (count > 65) throw new InvalidDataException("Excess Vorbis floor posts.");
        _posts = new int[count]; _posts[1] = 1 << rangeBits;
        for (int i = 2; i < count; i++)
        {
            int value = bits.Read(rangeBits);
            if (value < 0 || value >= 1 << rangeBits) throw new InvalidDataException("Invalid Vorbis floor post.");
            _posts[i] = value;
        }
        _forward = new int[count];
        for (int i = 0; i < count; i++) _forward[i] = i;
        Array.Sort(_forward, (a, b) => _posts[a].CompareTo(_posts[b]));
        for (int i = 1; i < count; i++) if (_posts[_forward[i]] == _posts[_forward[i - 1]]) throw new InvalidDataException("Duplicate Vorbis floor post.");
        _quant = _multiplier switch { 1 => 256, 2 => 128, 3 => 86, _ => 64 };
        _low = new int[count - 2]; _high = new int[count - 2];
        for (int i = 2; i < count; i++)
        {
            int low = 0, high = 1, lx = 0, hx = _posts[1], x = _posts[i];
            for (int j = 0; j < i; j++)
            {
                int post = _posts[j];
                if (post > lx && post < x) { low = j; lx = post; }
                if (post < hx && post > x) { high = j; hx = post; }
            }
            _low[i - 2] = low; _high[i - 2] = high;
        }
    }
    public static VorbisFloor1 Unpack(VorbisBits bits, int books) => new(bits, books);
    private static int Predict(int x0, int x1, int y0, int y1, int x)
    {
        y0 &= 0x7fff; y1 &= 0x7fff;
        int dy = y1 - y0, error = Math.Abs(dy) * (x - x0), value = error / (x1 - x0);
        return dy < 0 ? y0 - value : y0 + value;
    }
    public override bool Decode(VorbisBits bits, VorbisCodebook[] books, Span<int> memo)
    {
        if (bits.Read(1) != 1) return false;
        int width = VorbisCodebook.ILog((uint)(_quant - 1));
        memo[0] = bits.Read(width); memo[1] = bits.Read(width);
        if (memo[0] < 0 || memo[1] < 0) return false;
        int output = 2;
        foreach (int partition in _partitions)
        {
            int dimension = _classDim[partition], subBits = _classSubs[partition], mask = (1 << subBits) - 1, cascade = 0;
            if (subBits != 0) { cascade = books[_classBook[partition]].Decode(bits); if (cascade < 0) return false; }
            for (int j = 0; j < dimension; j++)
            {
                int book = _subBooks[partition][cascade & mask]; cascade >>= subBits;
                int value = book >= 0 ? books[book].Decode(bits) : 0;
                if (value < 0) return false;
                memo[output++] = value;
            }
        }
        for (int i = 2; i < _posts.Length; i++)
        {
            int low = _low[i - 2], high = _high[i - 2];
            int predicted = Predict(_posts[low], _posts[high], memo[low], memo[high], _posts[i]);
            int highRoom = _quant - predicted, lowRoom = predicted, room = Math.Min(highRoom, lowRoom) << 1, value = memo[i];
            if (value != 0)
            {
                if (value >= room) value = highRoom > lowRoom ? value - lowRoom : -1 - (value - highRoom);
                else value = (value & 1) != 0 ? -((value + 1) >> 1) : value >> 1;
                memo[i] = value + predicted; memo[low] &= 0x7fff; memo[high] &= 0x7fff;
            }
            else memo[i] = predicted | 0x8000;
        }
        return true;
    }
    private static void RenderLine(int count, int x0, int x1, int y0, int y1, Span<int> data)
    {
        int dy = y1 - y0, dx = x1 - x0, absolute = Math.Abs(dy), step = dy / dx;
        int signStep = dy < 0 ? step - 1 : step + 1, x = x0, y = y0, error = 0;
        count = Math.Min(count, x1); absolute -= Math.Abs(step * dx);
        if (x < count) data[x] = VorbisTransform.MULT31_SHIFT15(data[x], FLOOR_fromdB_LOOKUP[y]);
        while (++x < count)
        {
            error += absolute;
            if (error >= dx) { error -= dx; y += signStep; } else y += step;
            data[x] = VorbisTransform.MULT31_SHIFT15(data[x], FLOOR_fromdB_LOOKUP[y]);
        }
    }
    public override void Apply(Span<int> data, int count, Span<int> memo, bool present)
    {
        if (!present) { data[..count].Clear(); return; }
        int hx = 0, lx = 0, ly = memo[0] * _multiplier;
        for (int i = 1; i < _posts.Length; i++)
        {
            int current = _forward[i], hy = memo[current] & 0x7fff;
            if (hy != memo[current]) continue;
            hy *= _multiplier; hx = _posts[current];
            RenderLine(count, lx, hx, ly, hy, data); lx = hx; ly = hy;
        }
        unchecked { for (int i = hx; i < count; i++) data[i] *= ly; }
    }
}
