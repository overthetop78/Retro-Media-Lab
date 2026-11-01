using RetroMediaLab.MediaEngine.Interfaces.Reading.Recognition;
using RetroMediaLab.MediaFileSystems.Definitions;
using RetroMediaLab.MediaEngine.Images.Reading.Recognition.Msx;

using RetroMediaLab.MediaEngine.Images.Models.Sectors;

namespace RetroMediaLab.MediaEngine.Images.Reading.Recognition.Normalizers;

/// <summary>Normalise une image MSX uniquement après reconnaissance effective de FAT12.</summary>
internal sealed class MsxRecognizedImageNormalizer(MsxSectorImageInterpreter interpreter) : IRecognizedImageNormalizer
{
    /// <summary>Transmet l'image à l'interpréteur commun lorsque le Reader est FAT12.</summary>
    public bool TryNormalize(SectorImage image, string readerId, out SectorImage normalized)
    {
        normalized = image;
        return readerId.Equals(FileSystemIds.Fat12, StringComparison.OrdinalIgnoreCase) && interpreter.TryInterpret(image, out normalized);
    }
}
