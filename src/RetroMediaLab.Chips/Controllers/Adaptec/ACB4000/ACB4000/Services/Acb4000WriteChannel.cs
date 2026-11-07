using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Enums;
using C = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000FormatChannelConstants;
using F = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000FieldConstants;
using M = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000MfmConstants;
using P = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000ParameterConstants;
using S = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000SequencerConstants;
using W = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000WriteSequencerConstants;
using Encoding = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000MfmWriteConstants;

namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

/// <summary>Segment de sortie DATA du séquenceur : préambule, marque, contenu, ECC et postambule.</summary>
/// <remarks>Le propriétaire décide quand ouvrir ce segment après lecture de l'ID ; aucune recherche
/// de secteur, impulsion INDEX, image ou commande SCSI n'est synthétisée ici.</remarks>
public sealed class Acb4000WriteChannel
{
    private readonly byte[] record = new byte[P.MaximumBlockSize + F.DataOverhead];
    private Acb4000WriteChannelPhase phase;
    private int recordLength;
    private int position;
    private int cellsLeft;
    private ushort word;
    private bool previousData;
    private readonly Acb4000AicRegisters? aic;
    private readonly Acb4000MfmWriteEncoder encoder = new();
    private bool nativeGate;
    private bool nativeStarted;
    private byte nativePair;
    private bool publishedGate;
    public event Action<bool>? WriteGateChanged;

    private void PublishWriteGate()
    {
        var gate = WriteGate;
        if (publishedGate == gate) return;
        publishedGate = gate;
        WriteGateChanged?.Invoke(gate);
    }

    public Acb4000WriteChannel() { }

    internal Acb4000WriteChannel(Acb4000AicRegisters aic) => this.aic = aic;

    public bool WriteGate => aic is not null ? nativeGate
        : phase is >= Acb4000WriteChannelPhase.Preamble and <= Acb4000WriteChannelPhase.Postamble;
    public bool Complete => phase == Acb4000WriteChannelPhase.Complete;

    /// <summary>Copie les données ; un refus ne modifie pas une émission déjà en cours.</summary>
    public void Begin(ReadOnlySpan<byte> payload, uint polynomial = Constants.Acb4000ErrorCheckConstants.Polynomial)
    {
        if (payload.Length is not (P.MinimumBlockSize or P.MediumBlockSize or P.MaximumBlockSize))
            throw new ArgumentOutOfRangeException(nameof(payload));
        if (WriteGate) throw new InvalidOperationException();
        recordLength = payload.Length + F.DataOverhead;
        Acb4000FieldCodec.WriteData(record.AsSpan(0, recordLength), payload, polynomial);
        position = cellsLeft = 0;
        word = 0;
        previousData = false;
        phase = Acb4000WriteChannelPhase.Preamble;
        PublishWriteGate();
    }

    /// <summary>La cellule retournée est valable si WriteGate reste actif après cet appel.</summary>
    public bool ClockCell()
    {
        if (aic is not null) return ClockNativeCell();
        if (!WriteGate) return false;
        if (cellsLeft == 0)
        {
            if (position == PhaseLength)
            {
                position = 0;
                phase = phase switch
                {
                    Acb4000WriteChannelPhase.Preamble => Acb4000WriteChannelPhase.Data,
                    Acb4000WriteChannelPhase.Data => Acb4000WriteChannelPhase.Postamble,
                    _ => Acb4000WriteChannelPhase.Complete
                };
                if (!WriteGate) { PublishWriteGate(); return false; }
            }
            if (phase == Acb4000WriteChannelPhase.Data && position == 0)
            {
                word = M.MissingClockA1;
                previousData = (F.SyncByte & 1) != 0;
            }
            else word = Acb4000MfmEncoding.Encode(
                phase == Acb4000WriteChannelPhase.Data ? record[position] : C.PreambleByte, ref previousData);
            position++;
            cellsLeft = M.CellsPerByte;
        }
        return (word & (1 << --cellsLeft)) != 0;
    }

    public void Reset()
    {
        phase = Acb4000WriteChannelPhase.Idle;
        recordLength = position = cellsLeft = 0;
        word = 0;
        previousData = false;
        nativeGate = nativeStarted = false;
        nativePair = 0;
        encoder.Reset();
        PublishWriteGate();
    }

    internal void BeginSequenced()
    {
        if (aic is null) throw new InvalidOperationException();
        Reset();
        phase = Acb4000WriteChannelPhase.Preamble;
        aic.Sequencer.Start(W.StartState);
    }

    private bool ClockNativeCell()
    {
        if (Complete || phase == Acb4000WriteChannelPhase.Idle) return false;
        aic!.ClockReferenceCell();
        if (cellsLeft != 0)
        {
            cellsLeft = 0;
            return (nativePair & 1) != 0;
        }
        var sequencer = aic!.Sequencer;
        sequencer.ClockBit(false);
        nativePair = encoder.ClockBit(sequencer.NrzOutput, aic.WriteAddressMarkOutput);
        var gate = sequencer.WriteGate;
        if (nativeGate != gate)
        {
            nativeGate = gate;
            PublishWriteGate();
        }
        nativeStarted |= nativeGate;
        if (nativeStarted && !nativeGate)
        {
            phase = Acb4000WriteChannelPhase.Complete;
            sequencer.Start(S.StopState);
            return false;
        }
        cellsLeft = 1;
        return (nativePair & (1 << Encoding.ClockCellShift)) != 0;
    }

    private int PhaseLength => phase switch
    {
        Acb4000WriteChannelPhase.Preamble => C.DataPreambleLength,
        Acb4000WriteChannelPhase.Data => recordLength,
        Acb4000WriteChannelPhase.Postamble => C.PostDataLength,
        _ => 0
    };
}
