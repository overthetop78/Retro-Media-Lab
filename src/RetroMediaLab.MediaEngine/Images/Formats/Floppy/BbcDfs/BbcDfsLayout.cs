using RetroMediaLab.MediaEngine.Images.Models.Sectors;

namespace RetroMediaLab.MediaEngine.Images.Formats.Floppy.BbcDfs;

/// <summary>Décrit l'ordre DSD où les pistes complètes sont entrelacées par face pour chaque cylindre.</summary>
internal static class BbcDfsLayout
{
    /// <summary>Les faces DFS sont deux disques logiques ; le formateur Acorn inscrit H=0 sur chacune.</summary>
    internal const int RecordedHead = 0;

    /// <summary>Construit les secteurs complets du conteneur, en séparant leur face physique de l'ID enregistré.</summary>
    internal static SectorImage CreateImage(byte[] data, BbcDfsGeometry geometry, CancellationToken cancellationToken)
    {
        var blocks = new SectorBlock[data.Length / BbcDfsGeometry.SectorSize];
        for (var logical = 0; logical < blocks.Length; logical++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var perCylinder = geometry.Heads * BbcDfsGeometry.SectorsPerTrack;
            var address = new SectorAddress(logical / perCylinder, logical / BbcDfsGeometry.SectorsPerTrack % geometry.Heads, logical % BbcDfsGeometry.SectorsPerTrack);
            blocks[logical] = new(logical, address, data.AsSpan(logical * BbcDfsGeometry.SectorSize, BbcDfsGeometry.SectorSize).ToArray())
            {
                RecordedAddress = new(address.Cylinder, RecordedHead, address.Number)
            };
        }
        return new(geometry.FormatId, BbcDfsGeometry.SectorSize, geometry.Cylinders, geometry.Heads, BbcDfsGeometry.SectorsPerTrack, blocks);
    }

    /// <summary>Calcule l'offset source d'un secteur dans l'ordre cylindre, face, secteur du fichier.</summary>
    public static int SourceOffset(int cylinder, int head, int sector, int heads) => checked(((cylinder * heads + head) * BbcDfsGeometry.SectorsPerTrack + sector) * BbcDfsGeometry.SectorSize);
}
