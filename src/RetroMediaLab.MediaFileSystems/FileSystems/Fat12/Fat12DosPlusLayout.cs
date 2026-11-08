using RetroMediaLab.MediaFileSystems.Constants;

namespace RetroMediaLab.MediaFileSystems.FileSystems.Fat12;

/// <summary>Disposition native DOS Plus 800 Kio : une FAT de deux secteurs, sans secteur d'amorçage.</summary>
internal static class Fat12DosPlusLayout
{
    internal const int SectorSize = 1024;
    internal const int SectorCount = 800;
    private const int FatSectors = 2;
    private const int RootSectors = 6;

    internal static bool IsFormat(string formatId) => formatId.Equals(MediaImageFormatIds.AcornDosPlus800, StringComparison.OrdinalIgnoreCase);

    /// <summary>Valide le profil déjà reconnu et son en-tête FAT, sans interpréter les allocations comme un BPB.</summary>
    internal static bool TryRead(ReadOnlySpan<byte> firstSector, int availableSectors, out Fat12Layout layout)
    {
        layout = null!;
        if (availableSectors != SectorCount || firstSector.Length != SectorSize
            || firstSector[0] != 0xfd || firstSector[1] != 0xff || firstSector[2] != 0xff) return false;
        const int dataStart = FatSectors + RootSectors;
        layout = new(0, FatSectors, FatSectors, RootSectors, dataStart, 1, SectorCount - dataStart);
        return true;
    }
}
