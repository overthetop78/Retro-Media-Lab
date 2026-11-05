namespace RetroMediaLab.Chips.Controllers.Acorn.Winchester.Scsi.Interfaces;

/// <summary>Stockage à octets emprunté par la cible ; son propriétaire adapte le support et conserve sa possession.</summary>
/// <remarks>Les appels sont sérialisés par le propriétaire. Aucun conteneur, chemin, format de fichier ou
/// périphérique audio n'est connu de la cible. Les erreurs d'accès sont signalées au contrôleur appelant.</remarks>
public interface IScsiStorage
{
    long CapacityBytes { get; }
    long Cylinders { get; }
    int Heads { get; }
    int SectorsPerTrack { get; }
    int SectorSize { get; }
    bool IsReadOnly { get; }
    /// <summary>Copie exactement les octets demandés, y compris erronés ; false indique une intégrité incorrecte.
    /// Une lecture qui ne peut fournir les octets lève une exception au lieu de renvoyer des données inventées.</summary>
    bool Read(long byteOffset, Span<byte> destination);
    void Write(long byteOffset, ReadOnlySpan<byte> source);
    /// <summary>Déplace le support et retourne la position du cylindre pour les compteurs du contrôleur.</summary>
    long Seek(long byteOffset);
    /// <summary>Prépare les adresses du format sans remplissage ni sauvegarde des paramètres de carte.</summary>
    void PrepareFormat(long cylinders, int heads, int sectorsPerTrack, int sectorSize);
    byte[]? ReadParameters();
    void WriteParameters(ReadOnlySpan<byte> parameters);
    void Flush();
}
