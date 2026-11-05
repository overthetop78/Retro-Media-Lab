using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Enums;
using C = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000FormatChannelConstants;
using F = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000FieldConstants;
using L = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000FormatConstants;
using M = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000MfmConstants;
using P = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000ParameterConstants;
using R = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000PhysicalParameterConstants;

namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

/// <summary>Sortie FORMAT du séquenceur : une cellule par horloge fournie, aucun accès à une image.</summary>
public sealed partial class Acb4000FormatChannel
{
    private readonly byte[] map = new byte[L.SmallBlockInterleavedSectors];
    private readonly byte[] parameterFlags = new byte[R.FlagCount];
    private int parameterFlagCount;
    private readonly byte[] id = new byte[F.IdRecordLength];
    private readonly ushort[] idWords = new ushort[F.IdRecordLength];
    private readonly byte[] data = new byte[P.MaximumBlockSize + F.DataOverhead];
    private readonly ushort[] dataWords = new ushort[P.MaximumBlockSize + F.DataOverhead];
    private Acb4000FormatChannelPhase phase;
    private int slots;
    private int slot;
    private int position;
    private int dataLength;
    private int cellsLeft;
    private ushort word;
    private bool previousData;
    private bool sequential;
    private uint firstAddress;
    private byte defectCount;
    private uint polynomial;

    public bool WriteGate => aic is not null ? nativeGate
        : phase is >= Acb4000FormatChannelPhase.Gap and <= Acb4000FormatChannelPhase.Tail;
    public bool WaitingForIndex => phase == Acb4000FormatChannelPhase.WaitForIndex;
    public bool Complete => phase == Acb4000FormatChannelPhase.Complete;

    /// <summary>Prend une copie de la carte de slots établie par le contrôleur ; les entrées restent empruntées.</summary>
    public void BeginTrack(ReadOnlySpan<byte> physicalMap, uint firstLogicalAddress, int blockSize,
        bool sequential, byte fill, byte precedingDefects = 0, ReadOnlySpan<byte> physicalParameters = default, uint polynomial = Constants.Acb4000ErrorCheckConstants.Polynomial)
    {
        Validate(physicalMap, firstLogicalAddress, blockSize, precedingDefects);
        if (!physicalParameters.IsEmpty)
        {
            if (physicalParameters.Length != R.FlagCount || physicalMap.Length < R.FlagCount
                || firstLogicalAddress != 0 || precedingDefects != 0)
                throw new ArgumentException(nameof(physicalParameters));
            for (var index = 0; index < R.FlagCount; index++)
                if (physicalParameters[index] > R.FlagMask || physicalMap[index] == L.DefectiveSlot)
                    throw new ArgumentException(nameof(physicalParameters));
        }
        physicalParameters.CopyTo(parameterFlags);
        parameterFlagCount = physicalParameters.Length;
        physicalMap.CopyTo(map);
        slots = physicalMap.Length;
        firstAddress = firstLogicalAddress;
        this.sequential = sequential;
        defectCount = precedingDefects;
        this.polynomial = polynomial;
        dataLength = blockSize + F.DataOverhead;
        if (aic is null)
        {
            data.AsSpan(F.DataOffset, blockSize).Fill(fill);
            Acb4000FieldCodec.WriteData(data.AsSpan(0, dataLength), data.AsSpan(F.DataOffset, blockSize), polynomial);
            var previous = false;
            Acb4000MfmEncoding.WriteField(data.AsSpan(0, dataLength), dataWords, ref previous);
        }
        slot = position = cellsLeft = 0;
        word = 0;
        previousData = false;
        phase = Acb4000FormatChannelPhase.WaitForIndex;
        if (aic is not null) BeginNativeTrack(fill);
    }

    /// <summary>Le bit retourné est valable seulement lorsque WriteGate est actif après cet appel.</summary>
    /// <remarks>INDEX ne termine l'écriture qu'au remplissage final, comme l'état 16 du séquenceur.</remarks>
    public bool ClockCell(bool index)
    {
        if (aic is not null) return ClockNativeCell(index);
        if (phase == Acb4000FormatChannelPhase.WaitForIndex)
        {
            if (!index) return false;
            phase = Acb4000FormatChannelPhase.Gap;
        }
        if (!WriteGate) return false;
        if (cellsLeft == 0) AdvancePhase();
        if (phase == Acb4000FormatChannelPhase.Tail && index)
        {
            phase = Acb4000FormatChannelPhase.Complete;
            cellsLeft = 0;
            return false;
        }
        if (cellsLeft == 0)
        {
            word = NextWord();
            cellsLeft = M.CellsPerByte;
        }
        return (word & (1 << --cellsLeft)) != 0;
    }

    public void Reset()
    {
        phase = Acb4000FormatChannelPhase.Idle;
        slots = slot = position = dataLength = cellsLeft = 0;
        parameterFlagCount = 0;
        word = 0;
        firstAddress = defectCount = 0;
        previousData = sequential = false;
        ResetNativeOutput();
    }

    private int PhaseLength => phase switch
    {
        Acb4000FormatChannelPhase.Gap => slot == 0 ? C.FirstGapLength
            : sequential ? C.SequentialGapLength : C.InterleavedGapLength,
        Acb4000FormatChannelPhase.IdPreamble => C.IdPreambleLength,
        Acb4000FormatChannelPhase.Id => F.IdRecordLength,
        Acb4000FormatChannelPhase.DataPreamble => C.DataPreambleLength,
        Acb4000FormatChannelPhase.Data => dataLength,
        Acb4000FormatChannelPhase.PostData => C.PostDataLength,
        _ => int.MaxValue
    };

    private void AdvancePhase()
    {
        if (position < PhaseLength) return;
        position = 0;
        phase = phase switch
        {
            Acb4000FormatChannelPhase.Gap => Acb4000FormatChannelPhase.IdPreamble,
            Acb4000FormatChannelPhase.IdPreamble => Acb4000FormatChannelPhase.Id,
            Acb4000FormatChannelPhase.Id => Acb4000FormatChannelPhase.DataPreamble,
            Acb4000FormatChannelPhase.DataPreamble => Acb4000FormatChannelPhase.Data,
            Acb4000FormatChannelPhase.Data => Acb4000FormatChannelPhase.PostData,
            Acb4000FormatChannelPhase.PostData => ++slot < slots ? Acb4000FormatChannelPhase.Gap : Acb4000FormatChannelPhase.Tail,
            _ => throw new InvalidOperationException()
        };
        if (phase != Acb4000FormatChannelPhase.Id) return;
        var entry = map[slot];
        var defective = entry == L.DefectiveSlot;
        if (defective) defectCount++;
        var lowFlags = slot < parameterFlagCount ? parameterFlags[slot] : defectCount;
        var flags = (byte)(lowFlags | ((entry & L.LastLogicalSectorMask) != 0 ? F.LastSectorMask : 0));
        Acb4000FieldCodec.WriteId(id, firstAddress + (uint)(entry & L.LogicalSectorMask), flags, defective, polynomial);
        var previous = false;
        Acb4000MfmEncoding.WriteField(id, idWords, ref previous);
    }

    private ushort NextWord()
    {
        var offset = position;
        // Tail is unbounded and need not increment a counter during an absent INDEX.
        if (phase != Acb4000FormatChannelPhase.Tail) position++;
        if (phase is Acb4000FormatChannelPhase.Id or Acb4000FormatChannelPhase.Data)
        {
            var encoded = phase == Acb4000FormatChannelPhase.Id ? idWords[offset] : dataWords[offset];
            previousData = (encoded & 1) != 0;
            return encoded;
        }
        var value = phase is Acb4000FormatChannelPhase.Gap or Acb4000FormatChannelPhase.Tail
            ? C.GapByte : C.PreambleByte;
        return Acb4000MfmEncoding.Encode(value, ref previousData);
    }

    private static void Validate(ReadOnlySpan<byte> physicalMap, uint firstAddress, int blockSize, byte precedingDefects)
    {
        if (blockSize is not (P.MinimumBlockSize or P.MediumBlockSize or P.MaximumBlockSize))
            throw new ArgumentOutOfRangeException(nameof(blockSize));
        if (physicalMap.IsEmpty || physicalMap.Length > L.SmallBlockInterleavedSectors)
            throw new ArgumentOutOfRangeException(nameof(physicalMap));
        var defects = 0;
        var count = 0;
        var last = -1;
        ulong used = 0;
        foreach (var entry in physicalMap)
        {
            if (entry == L.DefectiveSlot) { defects++; continue; }
            var logical = entry & L.LogicalSectorMask;
            if ((entry & L.DefectiveSlot) != 0 || logical >= physicalMap.Length || (used & (1UL << logical)) != 0)
                throw new ArgumentException(nameof(physicalMap));
            if ((entry & L.LastLogicalSectorMask) != 0)
            {
                if (last != -1) throw new ArgumentException(nameof(physicalMap));
                last = logical;
            }
            used |= 1UL << logical;
            count++;
        }
        if (count == 0 || last != count - 1 || used != (1UL << count) - 1)
            throw new ArgumentException(nameof(physicalMap));
        if (precedingDefects + defects > F.DefectCountMask)
            throw new ArgumentOutOfRangeException(nameof(precedingDefects));
        if (firstAddress > F.LogicalAddressMask || firstAddress + (uint)count - 1 > F.LogicalAddressMask)
            throw new ArgumentOutOfRangeException(nameof(firstAddress));
    }
}
