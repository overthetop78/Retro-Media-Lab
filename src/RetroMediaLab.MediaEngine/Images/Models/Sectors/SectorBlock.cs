using IMediaSectorBlock = RetroMediaLab.MediaFileSystems.Interfaces.IMediaSectorBlock;

namespace RetroMediaLab.MediaEngine.Images.Models.Sectors;

/// <summary>Décrit un bloc logique, son adresse physique, ses données et les métadonnées issues de sa lecture.</summary>
/// <param name="LogicalBlock">Indice logique du bloc, compté à partir de zéro.</param>
/// <param name="Address">Adresse physique associée au bloc.</param>
/// <param name="Data">Données du bloc, exprimées en octets.</param>
/// <param name="IntegrityValid"><see langword="true"/> si l'intégrité est valide, <see langword="false"/> si elle est invalide, ou <see langword="null"/> si elle n'a pas pu être déterminée.</param>
/// <param name="Revolution">Indice de la révolution source, compté à partir de zéro.</param>
/// <param name="Tag">Métadonnées sectorielles facultatives, exprimées en octets.</param>
/// <param name="FormatCode">Octet de format sectoriel facultatif.</param>
/// <param name="DiagnosticCode">Code de diagnostic facultatif conservé depuis le conteneur source.</param>
public sealed record SectorBlock(int LogicalBlock, SectorAddress Address, IReadOnlyList<byte> Data, bool? IntegrityValid = true, int Revolution = 0, IReadOnlyList<byte>? Tag = null, byte? FormatCode = null, byte? DiagnosticCode = null) : IMediaSectorBlock
{
    /// <summary>Code d'intégrité enregistré dans le champ physique, lorsqu'il est conservé par le conteneur.</summary>
    public ReadOnlyMemory<byte> RecordedErrorCheckCode { get; init; }
    /// <summary>Préfixe enregistré du champ DATA, hors charge utile ; aucun préfixe n'est déduit du diagnostic.</summary>
    public ReadOnlyMemory<byte> RecordedDataPrefix { get; init; }
    /// <summary>Intégrité du champ identifiant ; null si le conteneur ne la distingue pas.</summary>
    public bool? HeaderIntegrityValid { get; init; }
    /// <summary>Intégrité du champ de données, distincte de celle de son identifiant.</summary>
    public bool? DataIntegrityValid { get; init; }
    /// <summary>Marque de données supprimées réellement observée ; null si elle n'est pas conservée.</summary>
    public bool? DeletedDataMark { get; init; }
    /// <summary>Code N réellement enregistré dans un identifiant ISO ; distinct de la taille disponible.</summary>
    public byte? RecordedSizeCode { get; init; }
    /// <summary>Identifiant CHR lu dans l'en-tête, lorsqu'il diffère de l'emplacement physique Address.</summary>
    public SectorAddress? RecordedAddress { get; init; }
    /// <summary>Rang du champ dans sa piste physique ; indépendant du numéro logique de bloc.</summary>
    public int? RecordedTrackOrder { get; init; }
    int IMediaSectorBlock.Cylinder => Address.Cylinder;
    int IMediaSectorBlock.Head => Address.Head;
    int IMediaSectorBlock.PhysicalSectorNumber => Address.Number;
}
