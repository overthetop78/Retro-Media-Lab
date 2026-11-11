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

        if (context.KnownLength == AmigaAdfGeometry.DoubleDensityCapacity || context.KnownLength == AmigaAdfGeometry.HighDensityCapacity)
        {
            result = context.Result(context.KnownLength == AmigaAdfGeometry.DoubleDensityCapacity
                ? DiskImageFormatIds.AmigaDos : DiskImageFormatIds.AmigaDosHighDensity, FormatConfidence.Certain, "Detection.AmigaSize");
            return true;
        }

        if (context.KnownLength is > 512 and < AcornAdfGeometry.HighDensityCapacity)
        {
            try
            {
                using var source = context.OpenRead(context.FilePath);
                var header = new byte[Math.Min(AcornAdfPartialImage.ProbeLength, (int)context.KnownLength.Value)];
                var count = source.ReadAtLeast(header, header.Length, throwOnEndOfStream: false);
                if (AcornAdfPartialImage.TryDescribe(header.AsSpan(0, count), context.KnownLength.Value, out _, out _))
                {
                    var bytes = new byte[(int)context.KnownLength.Value];
                    header.AsSpan(0, count).CopyTo(bytes);
                    source.ReadExactly(bytes.AsSpan(count));
                    if (AcornAdfPartialImage.TryCreate(bytes, out var partial))
                    {
                        result = context.Result(partial.FormatId, FormatConfidence.Certain, "Detection.AcornSize");
                        return true;
                    }
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        if (AcornAdfGeometry.BbcGeometries.FirstOrDefault(geometry => geometry.Capacity == context.KnownLength) is { } bbc)
        {
            result = context.Result(bbc.FormatId, FormatConfidence.Certain, "Detection.AcornSize");
            return true;
        }

        result = context.KnownLength switch
        {
            819200 or 820224 => context.Result("acorn.adfs.800", FormatConfidence.Certain, "Detection.AcornSize"),
            _ => context.Ambiguous("Detection.Multiple")
        };
        return context.KnownLength is 819200 or 820224;
    }
}
