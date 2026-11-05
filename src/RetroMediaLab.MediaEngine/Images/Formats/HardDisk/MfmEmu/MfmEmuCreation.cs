using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Images.Formats.HardDisk.Acorn;
using RetroMediaLab.MediaEngine.Images.Formats.HardDisk.Adaptec;
using RetroMediaLab.MediaFileSystems.Definitions;
using RetroMediaLab.MediaFileSystems.FileSystems.Acorn.Adfs;
using C = RetroMediaLab.MediaEngine.Images.Formats.HardDisk.MfmEmu.MfmEmuFormat;
using A = RetroMediaLab.MediaEngine.Images.Formats.HardDisk.Adaptec.AdaptecMfmFormat;

namespace RetroMediaLab.MediaEngine.Images.Formats.HardDisk.MfmEmu;

internal static class MfmEmuCreation
{
    internal static async Task CreateAsync(string path, HardDiskGeometry geometry, string? fileSystemId,
        CancellationToken cancellationToken, bool preallocate)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!AcornWinchesterFormat.SupportsGeometry(geometry) || geometry.Cylinders > A.MaximumCylinders
            || geometry.Heads > A.MaximumHeads) throw new ArgumentException(nameof(geometry));
        if (fileSystemId is not null && (fileSystemId != FileSystemIds.AcornAdfs || geometry.SectorSize != AcornAdfsFormatter.SectorSize))
            throw new NotSupportedException(nameof(fileSystemId));
        var header = MfmEmuHeader.Create(checked((int)geometry.Cylinders), geometry.Heads,
            C.AdaptecTrackByteLength, C.AdaptecSampleRate);
        var container = fileSystemId is null ? MfmEmuContainer.CreateBlank(header)
            : MfmEmuContainer.CreateFormatted(header, geometry,
                AcornAdfsFormatter.CreateHeader(checked((int)geometry.LogicalSectorCount)));
        var document = MediaImageDocument.CreateUnexplored(new(path, []), HardDiskImageFormatIds.MfmEmu,
            MediaKind.HardDisk, new MfmEmuImageRepresentation(container), [], new Dictionary<string, string>());
        await new MfmEmuWriter().CreateAsync(document, path, cancellationToken, preallocate).ConfigureAwait(false);
    }
}
