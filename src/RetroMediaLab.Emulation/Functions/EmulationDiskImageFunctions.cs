using RetroMediaLab.Emulation.Enums;
using RetroMediaLab.Emulation.Interfaces;
using RetroMediaLab.MediaEngine.Images.Formats.HardDisk.Acorn;
using RetroMediaLab.MediaEngine.Images.Formats.HardDisk.Adaptec;

namespace RetroMediaLab.Emulation.Functions;

/// <summary>Traduit les demandes du contrat disque SDK vers les identifiants internes du moteur média.</summary>
internal static class EmulationDiskImageFunctions
{
    internal static string EncodingId(EmulationDiskTrackEncoding encoding) => encoding switch
    {
        EmulationDiskTrackEncoding.AdaptecMfm => AdaptecMfmFormat.EncodingId,
        _ => throw new ArgumentOutOfRangeException(nameof(encoding))
    };

    internal static byte[]? ReadDeviceParameters(IEmulationDiskImage image)
        => image.Metadata.TryGetValue(AcornWinchesterFormat.DescriptorMetadataKey, out var value)
            ? Convert.FromBase64String(value) : null;

    internal static void WriteDeviceParameters(IEmulationDiskImage image, ReadOnlySpan<byte> parameters)
        => image.UpdateMetadata(new Dictionary<string, string>
        {
            [AcornWinchesterFormat.DescriptorMetadataKey] = Convert.ToBase64String(parameters)
        });
}
