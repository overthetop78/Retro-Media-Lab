using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants;
using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Enums;
using C = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000MfmConstants;
using F = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000FieldConstants;
using A = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000AicRegisterConstants;
using R = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000ReadSequencerConstants;
using S = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000SequencerConstants;
using D = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000ReadSyncConstants;

namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

/// <summary>Détection et assemblage sériel des champs ; le propriétaire fournit chaque cellule de lecture.</summary>
public sealed class Acb4000ReadChannel
{
    private readonly Acb4000EccRegisters ecc;
    private readonly Acb4000AicRegisters? aic;
    private readonly Acb4000ReadSynchronizer synchronizer = new();
    private readonly byte[] buffer = new byte[Acb4000ErrorCheckConstants.MaximumRecordLength];
    private ushort shift;
    private int seenCells;
    private int fieldCells;
    private int count;
    private int expectedLength;
    private int completedLength;
    private bool active;
    private bool previousData;
    private bool clocksValid;
    private int blockSize;
    private Acb4000FieldKind kind;
    private bool nativeDataCell;
    private bool nativeAccepted;
    private bool nativeEnabled;
    private ulong nativeTransferStart;
    private uint nativeReadPipeline;
    private bool nativeAddressMarkDetected;

    public Acb4000ReadChannel(int blockSize = Acb4000ParameterConstants.MinimumBlockSize)
        : this(Acb4000EccRegisters.CreateFirmwareConfiguration(), blockSize) { }

    public Acb4000ReadChannel(Acb4000EccRegisters ecc, int blockSize = Acb4000ParameterConstants.MinimumBlockSize)
    {
        ArgumentNullException.ThrowIfNull(ecc);
        this.ecc = ecc;
        Configure(blockSize);
    }

    internal Acb4000ReadChannel(Acb4000AicRegisters aic, int blockSize = Acb4000ParameterConstants.MinimumBlockSize)
        : this(aic?.Ecc ?? throw new ArgumentNullException(nameof(aic)), blockSize) => this.aic = aic;

    public bool IntegrityValid => completedLength != 0 && (aic is null ? ecc.CheckPassed
        : nativeAccepted && aic.Sequencer.ReadFieldCompleted && aic.Sequencer.ReadFieldEccValid);
    public ReadOnlySpan<byte> Field => buffer.AsSpan(0, completedLength);
    internal ulong TransferredBytes => aic is null ? 0 : unchecked(aic.Sequencer.TransferByteCount - nativeTransferStart);
    public bool FieldClockValid { get; private set; }
    /// <summary>Compteur des synchronisations, y compris les champs interrompus ou de marque inconnue.</summary>
    public uint FieldSequence { get; private set; }

    public void Configure(int size)
    {
        if (size is not (Acb4000ParameterConstants.MinimumBlockSize or Acb4000ParameterConstants.MediumBlockSize or Acb4000ParameterConstants.MaximumBlockSize))
            throw new ArgumentOutOfRangeException(nameof(size));
        blockSize = size;
        Reset();
        BeginIdRead();
    }

    public void Reset()
    {
        StopRead();
        shift = 0;
        seenCells = fieldCells = count = expectedLength = completedLength = 0;
        active = previousData = clocksValid = false;
        FieldClockValid = false;
        FieldSequence = 0;
        kind = Acb4000FieldKind.None;
        nativeDataCell = nativeAccepted = false;
        nativeTransferStart = 0;
    }

    internal void BeginIdRead()
    {
        if (aic is null) return;
        synchronizer.Reset();
        nativeAccepted = nativeAddressMarkDetected = false;
        nativeReadPipeline = 0;
        nativeEnabled = true;
        aic.TryWrite((byte)(S.RamBase + R.IdCompletionState), S.StopState);
        aic.TryWrite((byte)(S.RamBase + R.IdEccState), R.CheckedIdNext);
        aic.TryWrite(A.Branch, R.RetryBranch);
        aic.TryWrite(A.StartStatus, R.ReadIdStart);
    }

    internal void BeginDataRead()
    {
        if (aic is null) return;
        synchronizer.Reset();
        nativeAccepted = nativeAddressMarkDetected = false;
        nativeReadPipeline = 0;
        nativeEnabled = true;
        aic.TryWrite(A.Branch, R.RetryBranch);
        aic.TryWrite(A.StartStatus, R.ReadDataStart);
    }

    internal void StopRead()
    {
        synchronizer.Reset();
        nativeAccepted = nativeAddressMarkDetected = false;
        nativeReadPipeline = 0;
        nativeEnabled = false;
        if (aic is not null) aic.TryWrite(A.StartStatus, S.StopState);
        active = false;
    }

    internal bool TryReadId(out Acb4000IdField id) => ReadIdFromStack(parameterOnly: false, out id);
    internal bool TryReadParameterId(out Acb4000IdField id) => ReadIdFromStack(parameterOnly: true, out id);

    private bool ReadIdFromStack(bool parameterOnly, out Acb4000IdField id)
    {
        id = default;
        if (aic is null) return Acb4000FieldCodec.TryDecodeId(Field, out id);
        if (completedLength != F.IdRecordLength || !nativeAccepted) return false;
        Span<byte> record = stackalloc byte[F.IdRecordLength];
        Field.CopyTo(record);
        var first = parameterOnly ? F.FlagsOffset : F.MarkOffset;
        for (var index = F.FlagsOffset; index >= first; index--)
        {
            if (!aic.Sequencer.TryPop(out var value)) return false;
            record[index] = value;
        }
        return Acb4000FieldCodec.TryDecodeId(record, out id);
    }

    public Acb4000FieldKind Feed(bool cell)
    {
        if (aic is not null && nativeEnabled && !active && aic.Sequencer.Stopped) BeginIdRead();
        var physicalSynchronization = false;
        if (aic is not null)
        {
            aic.ClockReferenceCell();
            physicalSynchronization = synchronizer.ClockCell(cell, nativeEnabled && aic.Sequencer.ReadGate);
            var delayedCell = (nativeReadPipeline & D.ReadDelayOutputMask) != 0;
            nativeReadPipeline = (nativeReadPipeline << 1) | (cell ? 1u : 0u);
            cell = delayedCell;
        }
        shift = (ushort)((shift << 1) | (cell ? 1 : 0));
        if (seenCells < C.CellsPerByte) seenCells++;
        var synchronization = seenCells == C.CellsPerByte && shift == C.MissingClockA1;
        var syncByte = F.SyncByte;
        if (aic is not null) synchronization = false;
        if (aic is not null && nativeEnabled)
        {
            if (physicalSynchronization)
            {
                nativeAddressMarkDetected = true;
                nativeDataCell = false;
            }
            else nativeDataCell = !nativeDataCell;
            if (!nativeDataCell)
            {
                if (nativeAddressMarkDetected && !nativeAccepted)
                {
                    var previous = false;
                    syncByte = Acb4000MfmEncoding.Decode(shift, ref previous, out _);
                    synchronization = aic.TrySynchronizeRead(syncByte, addressMarkDetected: true);
                    if (synchronization)
                    {
                        nativeAccepted = true;
                        nativeTransferStart = aic.Sequencer.TransferByteCount;
                    }
                }
                if (!synchronization) aic.Sequencer.ClockBit(cell);
            }
        }
        if (synchronization)
        {
            FieldSequence = unchecked(FieldSequence + 1);
            active = true;
            previousData = (syncByte & 1) != 0;
            clocksValid = true;
            count = 1;
            fieldCells = expectedLength = completedLength = 0;
            kind = Acb4000FieldKind.None;
            FieldClockValid = false;
            buffer[0] = syncByte;
            if (aic is null)
            {
                ecc.BeginField();
                ecc.TryClockDataByte(F.SyncByte);
            }
            return Acb4000FieldKind.None;
        }
        if (!active) return Acb4000FieldKind.None;
        fieldCells++;
        // After synchronization the first cell is CLOCK, the second DATA.
        // Keep the visible syndrome current even between assembled bytes.
        if (aic is null && fieldCells % C.CellsPerBit == 0) ecc.TryClockDataBit(cell);
        if (fieldCells != C.CellsPerByte) return Acb4000FieldKind.None;
        fieldCells = 0;
        var value = Acb4000MfmEncoding.Decode(shift, ref previousData, out var valid);
        clocksValid &= valid;
        buffer[count++] = value;
        if (count == F.DataOffset)
        {
            kind = value switch { F.IdMark => Acb4000FieldKind.Id, F.DataMark => Acb4000FieldKind.Data, _ => Acb4000FieldKind.None };
            expectedLength = kind switch
            {
                Acb4000FieldKind.Id => F.IdRecordLength,
                Acb4000FieldKind.Data => blockSize + F.DataOverhead,
                _ => 0
            };
            if (kind == Acb4000FieldKind.None) active = false;
        }
        if (!active || count != expectedLength) return Acb4000FieldKind.None;
        active = false;
        completedLength = count;
        FieldClockValid = clocksValid;
        return kind;
    }
}
