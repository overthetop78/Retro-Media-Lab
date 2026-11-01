using RetroMediaLab.MediaEngine.Images.Formats.Tape;
using System.Reflection;
using RetroMediaLab.App.Views.Windows.Shell;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine;
using RetroMediaLab.MediaEngine.Images.Reading;
using RetroMediaLab.MediaEngine.Contracts.Explorer;
using RetroMediaLab.MediaFileSystems.Definitions;
using RetroMediaLab.MediaFileSystems.Exploration;
using RetroMediaLab.MediaAnalysis.Enums;

namespace RetroMediaLab.Tests.Architecture;

public sealed class MediaEngineProjectBoundaryTests
{
    [Fact]
    public void LoadedAssembliesStayWithinMediaLibraryBoundaries()
    {
        AssertRetroMediaLabReferences(typeof(MediaKind).Assembly, ["retromedialab.mediafilesystems"]);
        AssertRetroMediaLabReferences(typeof(MediaContentCategory).Assembly, []);
        AssertRetroMediaLabReferences(typeof(MediaVolumeDetectorRegistry).Assembly, ["retromedialab.mediaanalysis"]);
        AssertRetroMediaLabReferences(typeof(MediaEngineComposition).Assembly, ["retromedialab.mediafilesystems"]);
        AssertRetroMediaLabReferences(typeof(RetroMediaLab.Infrastructure.Processes.GreaseweazleRunner).Assembly, ["retromedialab.mediaengine", "retromedialab.mediafilesystems"]);

        var appReferences = References(typeof(MainWindow).Assembly);
        Assert.DoesNotContain("retromedialab.domain", appReferences);
        Assert.Contains("retromedialab.infrastructure", appReferences);
        Assert.Contains("retromedialab.mediaengine", appReferences);
        Assert.Contains("retromedialab.emulation", appReferences);
        Assert.DoesNotContain("retromedialab.mediafilesystems", appReferences);
        Assert.DoesNotContain("retromedialab.mediaanalysis", appReferences);

        var readers = RetroMediaLab.MediaFileSystems.Exploration.FileSystemReaderCatalog.CreateDefault();
        foreach (var id in new[]
        {
            FileSystemIds.AcornAdfs,
            FileSystemIds.AcornDfs,
            FileSystemIds.AmigaDos,
            FileSystemIds.AmigaFlatResourceArchive,
            FileSystemIds.AppleDos,
            FileSystemIds.CommodoreDos,
            FileSystemIds.Fat12
        })
            Assert.Same(typeof(RetroMediaLab.MediaFileSystems.Exploration.FileSystemReaderCatalog).Assembly, Assert.Single(readers, reader => reader.Id == id).GetType().Assembly);

        var exploration = RetroMediaLab.MediaFileSystems.Exploration.MediaExplorer.CreateDefault([new SequentialContentDecoderAdapter(SequentialMediaComposition.CreateDefault().Decoders)]);
        Assert.IsType<SequentialContentDecoderAdapter>(Assert.Single(exploration.Readers, reader => reader.Id == FileSystemIds.SequentialContent));
    }

    private static void AssertRetroMediaLabReferences(Assembly assembly, IEnumerable<string> expected)
    {
        var actual = References(assembly).Where(name => name.StartsWith("retromedialab.", StringComparison.OrdinalIgnoreCase)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Equal(expected.Order(StringComparer.OrdinalIgnoreCase), actual.Order(StringComparer.OrdinalIgnoreCase));
    }

    private static IReadOnlySet<string> References(Assembly assembly) => assembly.GetReferencedAssemblies().Select(reference => reference.Name ?? string.Empty).Where(name => name.Length > 0).ToHashSet(StringComparer.OrdinalIgnoreCase);
}
