using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using RetroMediaLab.App.Enums.Explorer;
using RetroMediaLab.App.Theme.OS.Editions.Windows31;
using RetroMediaLab.App.Views.Controls.Common;
using RetroMediaLab.Tests.Application.TestInfrastructure;

namespace RetroMediaLab.Tests.Interface.Themes;

[Collection("WPF")]
public sealed class Windows31FileImagesTests(StaExecutionScenarios sta)
{
    [Fact]
    public Task ArtworkMatchesTheOriginalBitmapExceptForItsWindowBackgroundMarker() => sta.Run(() =>
    {
        var resource = System.Windows.Application.GetResourceStream(new Uri(
            "/retromedialab.app;component/Theme/OS/Editions/Windows31/Assets/Images/FileManager.bmp", UriKind.Relative));
        using var stream = resource!.Stream;
        using var bytes = new MemoryStream();
        stream.CopyTo(bytes);
        Assert.Equal("A47609903FC528A95967300E9D99E0F23E751698C3A2C668103394174517E9AA", Convert.ToHexString(SHA256.HashData(bytes.ToArray())));
        bytes.Position = 0;
        var source = BitmapFrame.Create(bytes, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        Assert.Equal(208, source.PixelWidth);
        Assert.Equal(46, source.PixelHeight);
        var provider = new Windows31FileImages();
        foreach (var (category, expanded, column) in new[]
                 {
                     (ExplorerIconCategory.File, false, 2), (ExplorerIconCategory.Text, false, 1),
                     (ExplorerIconCategory.Program, false, 0), (ExplorerIconCategory.Folder, false, 5),
                     (ExplorerIconCategory.Folder, true, 7)
                 })
        {
            var expected = new FormatConvertedBitmap(new CroppedBitmap(source, new Int32Rect(column * 16, 0, 16, 16)), PixelFormats.Bgra32, null, 0);
            var actual = Assert.IsAssignableFrom<BitmapSource>(provider.GetImage(category, 16, expanded));
            Assert.True(actual.IsFrozen);
            var before = new byte[16 * 16 * 4];
            var after = new byte[before.Length];
            expected.CopyPixels(before, 64, 0);
            actual.CopyPixels(after, 64, 0);
            for (var index = 0; index < before.Length; index += 4)
            {
                Assert.Equal(before[index..(index + 3)], after[index..(index + 3)]);
                Assert.Equal(before[index] == 255 && before[index + 1] == 0 && before[index + 2] == 0 ? (byte)0 : (byte)255, after[index + 3]);
            }
        }
    });

    [Fact]
    public Task TheExistingIconControlUsesThemeArtworkAndReturnsToBaseAfterDetachment() => sta.Run(() =>
    {
        var icon = new FileEntryIcon { Category = ExplorerIconCategory.Executable, Width = 16, Height = 16 };
        var original = Assert.IsType<Image>(icon.FindName("NativeIcon")).Source;
        var placement = new Windows31WindowPlacement(new Rect(0, 0, 800, 600), new Rect(0, 0, 400, 300), new Size(180, 120));
        var frame = new Windows31Frame(placement, icon);
        try
        {
            frame.Measure(new Size(800, 600));
            frame.Arrange(new Rect(0, 0, 800, 600));
            frame.UpdateLayout();
            var themed = Assert.IsType<Image>(icon.FindName("NativeIcon"));
            Assert.Equal(16, Assert.IsAssignableFrom<BitmapSource>(themed.Source).PixelWidth);
            icon.Tone = ExplorerEntryTone.Executable;
            Assert.Equal(Visibility.Visible, themed.Visibility);
            Assert.Equal(Visibility.Collapsed, Assert.IsAssignableFrom<System.Windows.Shapes.Shape>(icon.FindName("TintedIcon")).Visibility);
            icon.Category = ExplorerIconCategory.Folder;
            var closed = themed.Source;
            icon.IsExpanded = true;
            Assert.NotSame(closed, themed.Source);
        }
        finally { frame.Dispose(); }
        icon.Category = ExplorerIconCategory.Executable;
        icon.Tone = ExplorerEntryTone.Default;
        Assert.Same(original, Assert.IsType<Image>(icon.FindName("NativeIcon")).Source);
    });
}
