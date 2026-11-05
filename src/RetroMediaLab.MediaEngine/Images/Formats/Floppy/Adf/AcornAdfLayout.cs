using RetroMediaLab.MediaEngine.Images.Models.Sectors;
using RetroMediaLab.MediaEngine.Images.Reading.Reconstruction;

namespace RetroMediaLab.MediaEngine.Images.Formats.Floppy.Adf;

/// <summary>Représentation physique BBC ADFS S/M/L : l'ID H reste zéro sur les deux faces.</summary>
internal static class AcornAdfLayout
{
    internal const int RecordedHead = 0;

    internal static SectorImage CreateBbc(byte[] data, RegularSectorGeometry geometry, CancellationToken cancellationToken)
    {
        if (data.Length != geometry.Capacity) throw AdfExceptions.InvalidSize(data.Length, [geometry.Capacity]);
        var blocks = new SectorBlock[geometry.BlockCount];
        var perCylinder = geometry.Heads * geometry.SectorsPerTrack;
        for (var logical = 0; logical < blocks.Length; logical++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var address = new SectorAddress(logical / perCylinder, logical / geometry.SectorsPerTrack % geometry.Heads,
                logical % geometry.SectorsPerTrack + geometry.FirstSectorNumber);
            blocks[logical] = new(logical, address, data.AsSpan(logical * geometry.BlockSize, geometry.BlockSize).ToArray())
            {
                RecordedAddress = new(address.Cylinder, RecordedHead, address.Number)
            };
        }
        return new(geometry.FormatId, geometry.BlockSize, geometry.Cylinders, geometry.Heads, geometry.SectorsPerTrack, blocks);
    }
}
