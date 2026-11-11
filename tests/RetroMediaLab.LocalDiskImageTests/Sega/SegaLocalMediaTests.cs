using System.IO;
using RetroMediaLab.MediaEngine;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Images.Formats;
using RetroMediaLab.MediaEngine.Images.Formats.Cartridge.Console;
using RetroMediaLab.MediaEngine.Images.Conversion;
using RetroMediaLab.MediaEngine.Images.Reading.Recognition;
using RetroMediaLab.MediaEngine.Images.Visualization;
using RetroMediaLab.MediaEngine.Interfaces.Reading;
using RetroMediaLab.MediaFileSystems.FileSystems.Console.Cartridge;
using RetroMediaLab.MediaFileSystems.Contracts;

namespace RetroMediaLab.MediaAudit.Sega;

public sealed class SegaLocalMediaTests
{
    [CorpusFact]
    public async Task RealSegaCorpusCartridgesUseAllMediaPathsWhenCorpusIsProvided()
    {
        var corpus = Environment.GetEnvironmentVariable("RetroMediaLab_SEGA_MEDIA_ROOT");
        if (string.IsNullOrWhiteSpace(corpus) || !Directory.Exists(corpus)) return;

        var directory = Path.Combine(Path.GetTempPath(), $"retromedialab-sega-real-media-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var cases = new[]
            {
                (Extension: DiskImageFileExtensions.Sms, FormatId: DiskImageFormatIds.SegaMasterSystem),
                (Extension: DiskImageFileExtensions.Sg, FormatId: DiskImageFormatIds.SegaSg1000),
                (Extension: DiskImageFileExtensions.Mv, FormatId: DiskImageFormatIds.SegaSg1000),
                (Extension: DiskImageFileExtensions.Md, FormatId: DiskImageFormatIds.SegaMegaDrive),
                (Extension: DiskImageFileExtensions.Gg, FormatId: DiskImageFormatIds.SegaGameGear),
                (Extension: DiskImageFileExtensions.ThirtyTwoX, FormatId: DiskImageFormatIds.SegaThirtyTwoX)
            };
            var engine = MediaEngineComposition.CreateDefault();
            var reader = new ConsoleCartridgeReader();
            var explorer = new ConsoleCartridgeFileSystemReader();
            var found = 0;
            foreach (var item in cases)
            {
                var sourcePath = Directory.EnumerateFiles(corpus, $"*{item.Extension}", SearchOption.AllDirectories).FirstOrDefault();
                if (sourcePath is null) continue;
                found++;

                var document = await ReadAsync(reader, sourcePath);
                Assert.Equal(item.FormatId, document.FormatId);
                var visualization = MediaVisualizationComposition.CreateDefault().Registry.CreateDescriptor(document);
                Assert.NotEmpty(visualization.Elements);
                Assert.NotEmpty(explorer.Read(document, Volume(document)).Entries);

                var outputPath = Path.Combine(directory, $"export-{found}{item.Extension}");
                var conversion = await engine.ConversionService.ConvertAsync(new MediaConversionRequest(document, outputPath, item.FormatId));
                Assert.Equal([outputPath], conversion.ProducedFiles);
                Assert.True(File.Exists(outputPath));
                Assert.True(new FileInfo(outputPath).Length > 0);
            }

            Assert.Equal(cases.Length, found);
        }
        finally
        {
            DeleteTemporaryOutputs(directory);
        }
    }

    [CorpusFact]
    public async Task EveryRealSegaCartridgeUsesVisualizationAndExploration()
    {
        var corpus = Environment.GetEnvironmentVariable("RetroMediaLab_SEGA_MEDIA_ROOT");
        if (string.IsNullOrWhiteSpace(corpus) || !Directory.Exists(corpus)) return;

        var directory = Path.Combine(Path.GetTempPath(), $"retromedialab-sega-all-media-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var formats = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [DiskImageFileExtensions.Sms] = DiskImageFormatIds.SegaMasterSystem,
                [DiskImageFileExtensions.Sg] = DiskImageFormatIds.SegaSg1000,
                [DiskImageFileExtensions.Mv] = DiskImageFormatIds.SegaSg1000,
                [DiskImageFileExtensions.Md] = DiskImageFormatIds.SegaMegaDrive,
                [DiskImageFileExtensions.Gg] = DiskImageFormatIds.SegaGameGear,
                [DiskImageFileExtensions.ThirtyTwoX] = DiskImageFormatIds.SegaThirtyTwoX
            };
            var paths = Directory.EnumerateFiles(corpus, "*", SearchOption.AllDirectories).Where(path => formats.ContainsKey(Path.GetExtension(path))).ToArray();
            Assert.NotEmpty(paths);

            var reader = new ConsoleCartridgeReader();
            var explorer = new ConsoleCartridgeFileSystemReader();
            var engine = MediaEngineComposition.CreateDefault();
            var catalog = new BuiltInImageFormatCatalog();
            var convertedFormats = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var converted = 0;
            foreach (var path in paths)
            {
                var extension = Path.GetExtension(path);
                var document = await ReadAsync(reader, path);
                Assert.Equal(formats[extension], document.FormatId);
                var visualization = MediaVisualizationComposition.CreateDefault().Registry.CreateDescriptor(document);
                Assert.NotEmpty(visualization.Elements);
                Assert.NotEmpty(explorer.Read(document, Volume(document)).Entries);
                counts[extension] = counts.TryGetValue(extension, out var count) ? count + 1 : 1;

                if (!convertedFormats.Add(document.FormatId)) continue;
                var format = catalog.Formats.Single(candidate => candidate.Id == document.FormatId);
                var destinations = engine.ConversionService.GetAvailableDestinations(document);
                foreach (var destinationExtension in format.Extensions.Select(item => item.Extension))
                {
                    Assert.Contains(destinations, destination => destination.FormatId == document.FormatId && destination.Extension.Equals(destinationExtension, StringComparison.OrdinalIgnoreCase));
                    var outputPath = Path.Combine(directory, $"export-{++converted}{destinationExtension}");
                    var result = await engine.ConversionService.ConvertAsync(new MediaConversionRequest(document, outputPath, document.FormatId));
                    Assert.Equal([outputPath], result.ProducedFiles);
                    Assert.Equal(new FileInfo(path).Length, new FileInfo(outputPath).Length);
                }
            }

            Assert.Equal(formats.Keys.OrderBy(key => key), counts.Keys.OrderBy(key => key));
            Assert.All(counts.Values, count => Assert.True(count > 0));
        }
        finally
        {
            DeleteTemporaryOutputs(directory);
        }
    }

    private static async Task<MediaImageDocument> ReadAsync(IMediaImageReader reader, string path, string? requestedFormatId = null)
    {
        var context = new MediaRecognitionContext(new MediaSourceDescriptor(path, [], RequestedFormatId: requestedFormatId));
        Assert.True(await reader.CanReadAsync(context, CancellationToken.None));
        return await reader.ReadAsync(context, CancellationToken.None);
    }

    private static MediaVolumeDescriptor Volume(MediaImageDocument document)
    {
        var volume = Assert.Single(document.Volumes);
        return new MediaVolumeDescriptor(volume.Start, volume.Length, volume.Origin, name: volume.Name);
    }

    private static void DeleteTemporaryOutputs(string directory)
    {
        var fullPath = Path.GetFullPath(directory);
        var temporaryRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.GetTempPath())) + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(temporaryRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Invalid temporary output path.");
        if (Directory.Exists(fullPath)) Directory.Delete(fullPath, recursive: true);
    }

    private sealed class CorpusFactAttribute : FactAttribute
    {
        public CorpusFactAttribute()
        {
            var root = Environment.GetEnvironmentVariable("RetroMediaLab_SEGA_MEDIA_ROOT");
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
                Skip = "RetroMediaLab_SEGA_MEDIA_ROOT must identify the local Sega corpus.";
        }
    }
}
