namespace RetroMediaLab.Chips.Controllers.Common.Contracts;

/// <summary>Secteur fourni à un contrôleur émulé ; les données et diagnostics restent distincts.</summary>
public record DiskSector(long Index, ReadOnlyMemory<byte> Data, int Cylinder, int Head, int Number, bool? IntegrityValid = null, byte? DiagnosticCode = null)
{
    /// <summary>Octets de contrôle réellement enregistrés à la suite du champ DATA ; vide s'ils ne sont pas conservés par le format.</summary>
    public ReadOnlyMemory<byte> RecordedErrorCheckCode { get; init; }
    /// <summary>Octets enregistrés avant les données dans le champ physique ; distincts de la charge utile.</summary>
    public ReadOnlyMemory<byte> RecordedDataPrefix { get; init; }
    /// <summary>Intégrité du champ identifiant ; null si le conteneur ne la distingue pas.</summary>
    public bool? HeaderIntegrityValid { get; init; }
    /// <summary>Intégrité du champ de données, distincte de celle de son identifiant.</summary>
    public bool? DataIntegrityValid { get; init; }
    /// <summary>Marque de données supprimées réellement observée ; null si elle n'est pas conservée.</summary>
    public bool? DeletedDataMark { get; init; }
    /// <summary>Code N réellement enregistré dans un identifiant ISO ; distinct de la taille disponible.</summary>
    public byte? RecordedSizeCode { get; init; }
}
