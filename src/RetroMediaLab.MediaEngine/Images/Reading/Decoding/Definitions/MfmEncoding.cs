using RetroMediaLab.MediaEngine.Functions;
using RetroMediaLab.MediaEngine.Images.Models.Sectors;

namespace RetroMediaLab.MediaEngine.Images.Reading.Decoding.Definitions;

/// <summary>Regroupe les unités communes au décodage MFM.</summary>
internal static class MfmEncoding
{
    /// <summary>Nombre de bits encodés nécessaires pour représenter un octet de données MFM.</summary>
    public const int EncodedByteBitCount = BitPrimitives.BitsPerByte * 2;
}
