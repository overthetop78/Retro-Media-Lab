using System.IO;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Adf;

namespace RetroMediaLab.MediaEngine.Images.Formats.Detection;

internal sealed class AdfImageFormatDetectionRule : IImageFormatDetectionRule
{
    public bool TryDetect(ImageFormatDetectionContext context, out DetectedImageFormat result)
    {
        if (context.Extension == DiskImageFileExtensions.AcornAdfs640)
        {
            result = context.KnownLength == 655360
                ? context.Result(DiskImageFormatIds.AcornAdfs640, FormatConfidence.Certain, "Detection.AcornSize")
                : context.Ambiguous("Detection.Multiple");
            return true;
        }
        if (context.KnownLength == AcornAdfGeometry.Capacity && context.Extension is ".adf" or ".img" or ".800")
        {
            try
            {
                using var source = context.OpenRead(context.FilePath);
                Span<byte> header = stackalloc byte[3];
                if (source.ReadAtLeast(header, header.Length, throwOnEndOfStream: false) == header.Length
                    && AcornAdfGeometry.HasDosPlusHeader(header))
                {
                    result = context.Result(DiskImageFormatIds.AcornDosPlus800, FormatConfidence.Certain, "Detection.AcornSize");
                    return true;
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
        if (context.Extension != ".adf")
        {
            result = null!;
            return false;
        }

        result = context.KnownLength switch
        {
            901120 => context.Result("amiga.amigados", FormatConfidence.Certain, "Detection.AmigaSize"),
            1802240 => context.Result("amiga.amigados_hd", FormatConfidence.Certain, "Detection.AmigaSize"),
            819200 or 820224 => context.Result("acorn.adfs.800", FormatConfidence.Certain, "Detection.AcornSize"),
            _ => context.Ambiguous("Detection.Multiple")
        };
        return context.KnownLength is 901120 or 1802240 or 819200 or 820224;
    }
}
