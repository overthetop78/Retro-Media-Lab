namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cd;

internal sealed partial class VorbisCodebook
{
    private static int UnpackFloat(int packed, out int point)
    {
        unchecked
        {
            int mantissa = packed & 0x1fffff, exponent = ((packed & 0x7fe00000) >> 21) - 788;
            if (mantissa != 0)
            {
                while ((mantissa & 0x40000000) == 0) { mantissa <<= 1; exponent--; }
                if ((packed & int.MinValue) != 0) mantissa = -mantissa;
            }
            else exponent = -9999;
            point = exponent; return mantissa;
        }
    }
    private static int FloatMultiplyInteger(int value, int valuePoint, int integer, ref int point)
    {
        unchecked
        {
            int integerPoint = ILog((uint)Math.Abs(integer)) - 31;
            int normalized = integer << -integerPoint;
            if (value == 0 || normalized == 0) return 0;
            point = valuePoint + integerPoint + 32;
            return VorbisTransform.MULT32(value, normalized);
        }
    }
    private static int FloatAdd(int a, int ap, int b, int bp, ref int point)
    {
        unchecked
        {
            if (a == 0) { point = bp; return b; }
            if (b == 0) { point = ap; return a; }
            if (ap > bp)
            {
                int shift = ap - bp + 1; point = ap + 1; a >>= 1;
                b = shift < 32 ? (b + (1 << (shift - 1))) >> shift : 0;
            }
            else
            {
                int shift = bp - ap + 1; point = bp + 1; b >>= 1;
                a = shift < 32 ? (a + (1 << (shift - 1))) >> shift : 0;
            }
            a += b;
            uint top = (uint)a & 0xc0000000;
            if (top is 0 or 0xc0000000) { a <<= 1; point--; }
            return a;
        }
    }
    private static int[] Unquantize(int dimension, int entries, int[] index, int mapType, int packedMin, int packedDelta,
        int[] quantized, bool sequence, out int binaryPoint)
    {
        int min = UnpackFloat(packedMin, out int minPoint), delta = UnpackFloat(packedDelta, out int deltaPoint);
        int[] result = new int[checked(index.Length * dimension)], points = new int[result.Length];
        int quantValues = mapType == 1 ? QuantValues(entries, dimension) : 0;
        int maximum = minPoint;
        unchecked
        {
            for (int i = 0; i < index.Length; i++)
            {
                int last = 0, lastPoint = 0, divisor = 1;
                for (int k = 0; k < dimension; k++)
                {
                    int quantIndex = mapType == 1 ? (index[i] / divisor) % quantValues : index[i] * dimension + k;
                    int point = 0;
                    int value = FloatMultiplyInteger(delta, deltaPoint, Math.Abs(quantized[quantIndex]), ref point);
                    value = FloatAdd(min, minPoint, value, point, ref point);
                    value = FloatAdd(last, lastPoint, value, point, ref point);
                    if (sequence) { last = value; lastPoint = point; }
                    int output = i * dimension + k;
                    result[output] = value; points[output] = point;
                    maximum = Math.Max(maximum, point);
                    if (mapType == 1) divisor *= quantValues;
                }
            }
            for (int i = 0; i < result.Length; i++) if (points[i] < maximum) result[i] >>= maximum - points[i];
        }
        binaryPoint = maximum; return result;
    }
    public bool DecodeVector(VorbisBits bits, Span<int> target, int count, int point, bool add)
    {
        if (UsedEntries == 0)
        { if (!add) target[..count].Clear(); return true; }
        int shift = point - BinaryPoint;
        unchecked
        {
            for (int i = 0; i < count;)
            {
                int entry = DecodePacked(bits); if (entry < 0) return false;
                int offset = entry * Dimension;
                for (int j = 0; j < Dimension; j++, i++)
                {
                    int value = shift >= 0 ? _values[offset + j] >> shift : _values[offset + j] << -shift;
                    target[i] = add ? target[i] + value : value;
                }
            }
        }
        return true;
    }
    public bool DecodeTransposedAdd(VorbisBits bits, Span<int> target, int count, int point)
    {
        if (UsedEntries == 0) return true;
        int step = count / Dimension, shift = point - BinaryPoint;
        Span<int> entries = stackalloc int[step];
        for (int i = 0; i < step; i++)
        { int entry = DecodePacked(bits); if (entry < 0) return false; entries[i] = entry * Dimension; }
        unchecked
        {
            for (int i = 0, offset = 0; i < Dimension; i++, offset += step)
                for (int j = 0; j < step; j++)
                {
                    int value = _values[entries[j] + i];
                    target[offset + j] += shift >= 0 ? value >> shift : value << -shift;
                }
        }
        return true;
    }
    public bool DecodeInterleavedAdd(VorbisBits bits, int[][] target, int offset, int channels, int count, int point)
    {
        if (UsedEntries == 0) return true;
        int shift = point - BinaryPoint, channel = 0;
        unchecked
        {
            for (int i = offset; i < offset + count;)
            {
                int entry = DecodePacked(bits); if (entry < 0) return false;
                int start = entry * Dimension;
                for (int j = 0; j < Dimension; j++)
                {
                    int value = _values[start + j];
                    target[channel++][i] += shift >= 0 ? value >> shift : value << -shift;
                    if (channel == channels) { channel = 0; i++; }
                }
            }
        }
        return true;
    }
}
