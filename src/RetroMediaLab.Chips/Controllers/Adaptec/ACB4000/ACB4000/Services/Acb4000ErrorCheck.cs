using System.Buffers.Binary;
using System.Numerics;
using C = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000ErrorCheckConstants;

namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

/// <summary>Contrôle de champ et correction de rafale, sans modifier les pistes ni posséder le tampon du contrôleur.</summary>
public static class Acb4000ErrorCheck
{
    private static readonly uint[] Table = CreateTable();

    public static uint Compute(ReadOnlySpan<byte> bytes, uint polynomial = C.Polynomial)
    {
        var residue = C.InitialValue;
        foreach (var value in bytes)
            residue = AdvanceByte(residue, value, polynomial);
        return residue;
    }

    /// <summary>Complète les quatre derniers octets réservés au contrôle, fort d'abord.</summary>
    public static void Write(Span<byte> record, uint polynomial = C.Polynomial)
    {
        if (record.Length < C.CheckByteCount) throw new ArgumentException(nameof(record));
        BinaryPrimitives.WriteUInt32BigEndian(record[^C.CheckByteCount..], Compute(record[..^C.CheckByteCount], polynomial));
    }

    /// <summary>Vrai pour un champ intact ou corrigé ; un échec conserve exactement les octets reçus.</summary>
    public static bool TryCorrect(Span<byte> record, out Acb4000ErrorCorrection correction, uint polynomial = C.Polynomial)
    {
        if (record.Length < C.CheckByteCount || record.Length > C.MaximumRecordLength)
            throw new ArgumentOutOfRangeException(nameof(record));
        correction = default;
        var residue = Compute(record, polynomial);
        if (residue == C.InitialValue) return true;
        // Compute inclut les 32 décalages de l'augmentation du message par le contrôle.
        var shifted = residue;
        for (var bit = 0; bit < C.CheckBitCount; bit++) shifted = ReverseShift(shifted, polynomial);
        var recordBits = record.Length * C.BitsPerByte;
        var found = false;
        var foundTail = 0;
        uint foundPattern = 0;
        for (var tail = 0; tail < recordBits; tail++)
        {
            if (shifted is > 0 and <= C.BurstMask)
            {
                var trailing = BitOperations.TrailingZeroCount(shifted);
                var pattern = shifted >> trailing;
                var normalizedTail = tail + trailing;
                var width = BitOperations.Log2(pattern) + 1;
                if (normalizedTail + width <= recordBits)
                {
                    if (found && (foundTail != normalizedTail || foundPattern != pattern)) return false;
                    found = true;
                    foundTail = normalizedTail;
                    foundPattern = pattern;
                }
            }
            shifted = ReverseShift(shifted, polynomial);
        }
        if (!found) return false;
        Apply(record, foundTail, foundPattern);
        if (Compute(record, polynomial) != C.InitialValue)
        {
            Apply(record, foundTail, foundPattern);
            return false;
        }
        var bitCount = BitOperations.Log2(foundPattern) + 1;
        correction = new(recordBits - foundTail - bitCount, bitCount, (byte)foundPattern);
        return true;
    }

    internal static uint AdvanceByte(uint residue, byte value, uint polynomial)
    {
        if (polynomial == C.Polynomial)
            return (residue << C.BitsPerByte) ^ Table[(byte)((residue >> (C.CheckBitCount - C.BitsPerByte)) ^ value)];
        residue ^= (uint)value << (C.CheckBitCount - C.BitsPerByte);
        for (var bit = 0; bit < C.BitsPerByte; bit++)
            residue = (residue << 1) ^ ((residue & C.HighBit) != 0 ? polynomial : C.InitialValue);
        return residue;
    }

    private static uint ReverseShift(uint value, uint polynomial) => (value & 1) == 0 ? value >> 1
        : ((value ^ polynomial) >> 1) | C.HighBit;

    private static void Apply(Span<byte> record, int tail, uint pattern)
    {
        for (var bit = 0; bit < C.MaximumBurstBits; bit++)
        {
            if ((pattern & (1U << bit)) == 0) continue;
            var position = record.Length * C.BitsPerByte - 1 - tail - bit;
            record[position / C.BitsPerByte] ^= (byte)(1 << (C.BitsPerByte - 1 - position % C.BitsPerByte));
        }
    }

    private static uint[] CreateTable()
    {
        var table = new uint[1 << C.BitsPerByte];
        for (var value = 0; value < table.Length; value++)
        {
            var residue = (uint)value << (C.CheckBitCount - C.BitsPerByte);
            for (var bit = 0; bit < C.BitsPerByte; bit++)
                residue = (residue << 1) ^ ((residue & C.HighBit) != 0 ? C.Polynomial : C.InitialValue);
            table[value] = residue;
        }
        return table;
    }
}
