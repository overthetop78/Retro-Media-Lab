using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Enums;
using C = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000PhysicalParameterConstants;

namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

/// <summary>Acquisition des paramètres des neuf premiers ID de C0/H0 ; les cellules viennent du lecteur.</summary>
/// <remarks>Le propriétaire calibre et sélectionne la tête avant Begin. L'échéance de recherche est
/// fournie séparément ; aucune durée analogique ou géométrie de secours n'est déduite du conteneur.</remarks>
public sealed class Acb4000ParameterChannel
{
    private readonly Acb4000ReadChannel reader;
    private readonly byte[] flags = new byte[C.FlagCount];
    private Acb4000DriveParameters previous;
    private bool active;
    private bool reading;
    private bool indexLevel;
    private int count;
    private int physicalOrdinal;
    private int idCheckAttempts;
    private uint lastIdSequence;
    private uint firstLogicalAddress;
    private bool sequentialLayout;

    public Acb4000ParameterChannel() : this(Acb4000EccRegisters.CreateFirmwareConfiguration()) { }

    public Acb4000ParameterChannel(Acb4000EccRegisters ecc) => reader = new(ecc);
    internal Acb4000ParameterChannel(Acb4000AicRegisters aic) => reader = new(aic);

    public int ParametersRead => count;
    public bool WaitingForIndex => active && !reading;
    public bool Complete { get; private set; }
    public bool FormatValid { get; private set; }
    public Acb4000DriveParameters Parameters { get; private set; }
    public bool SequentialLayout { get; private set; }

    public void Begin(Acb4000DriveParameters previousParameters)
    {
        if (!previousParameters.IsValid) throw new ArgumentOutOfRangeException(nameof(previousParameters));
        Reset();
        previous = previousParameters;
        Parameters = previousParameters;
        active = true;
        idCheckAttempts = C.IdCheckAttempts;
    }

    /// <summary>INDEX réinitialise l'ordinal physique ; les paramètres déjà acquis restent dans la RAM de carte.</summary>
    public void SetIndex(bool asserted)
    {
        var rising = asserted && !indexLevel;
        indexLevel = asserted;
        if (!active || !rising) return;
        physicalOrdinal = 0;
        lastIdSequence = 0;
        reader.Reset();
        reader.BeginIdRead();
        reading = true;
    }

    public Acb4000FieldKind ClockCell(bool cell)
    {
        if (!active || !reading) return Acb4000FieldKind.None;
        var kind = reader.Feed(cell);
        if (kind != Acb4000FieldKind.Id)
        {
            if (kind != Acb4000FieldKind.None) reader.BeginIdRead();
            return kind;
        }
        var integrityValid = reader.IntegrityValid;
        var decodedId = reader.TryReadParameterId(out var id);
        reader.BeginIdRead();
        var ordinal = physicalOrdinal++;
        var ordinalKnown = ordinal == 0 || unchecked(reader.FieldSequence - lastIdSequence) <= C.MaximumSyncsBetweenIds;
        lastIdSequence = reader.FieldSequence;
        if (!ordinalKnown)
        {
            RetryParameterId();
            return kind;
        }
        // 163C increments the physical ordinal before 1654 compares it with 80BB.
        // CRC is only examined at 165B for the parameter being requested.
        if (ordinal < count) return kind;
        if (!integrityValid || !decodedId)
        {
            RetryParameterId();
            return kind;
        }
        // Firmware 1576 selects physical ordinals, not LBA values or the defective-address flag.
        idCheckAttempts = C.IdCheckAttempts;
        if (count == 0) firstLogicalAddress = id.LogicalAddress;
        else if (count == 1) sequentialLayout = firstLogicalAddress == 0 && id.LogicalAddress == 1;
        flags[count++] = (byte)(id.Flags & C.FlagMask);
        if (count != flags.Length) return kind;
        FormatValid = Acb4000PhysicalParameters.TryRead(flags, previous, out var parameters);
        Parameters = parameters;
        SequentialLayout = FormatValid && sequentialLayout;
        Complete = true;
        active = reading = false;
        reader.StopRead();
        return kind;
    }

    private void RetryParameterId()
    {
        // Firmware 1666-166A retries from INDEX, four checks for each requested ordinal.
        reading = false;
        if (--idCheckAttempts == 0) Expire();
    }

    /// <summary>Abandon explicite du propriétaire lorsque la recherche matérielle expire.</summary>
    public void Expire()
    {
        if (!active) return;
        reader.StopRead();
        active = reading = false;
        Complete = true;
        FormatValid = false;
    }

    public void Reset()
    {
        active = reading = indexLevel = Complete = FormatValid = false;
        count = physicalOrdinal = idCheckAttempts = 0;
        lastIdSequence = 0;
        previous = Parameters = default;
        reader.Reset();
        firstLogicalAddress = 0;
        sequentialLayout = SequentialLayout = false;
    }
}
