using System.Security.Cryptography;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using RetroMediaLab.App.Theme.OS.Editions.Windows31;
using RetroMediaLab.Tests.Application.TestInfrastructure;

namespace RetroMediaLab.Tests.Interface.Themes;

[Collection("WPF")]
public sealed class Windows31IconTests(StaExecutionScenarios sta)
{
    private const string ResourcePath = "/retromedialab.app;component/Theme/OS/Editions/Windows31/Styles/Windows31Icons.xaml";
    private const int BytesPerPixel = 4;

    [Fact]
    public Task OriginalIconsAreEmbeddedTransparentAndDistinctForEachTool() => sta.Run(() =>
    {
        var dictionary = new ResourceDictionary { Source = new Uri(ResourcePath, UriKind.Relative) };
        var signatures = new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in Enum.GetValues<Windows31ApplicationId>())
        {
            var original = Assert.IsAssignableFrom<BitmapSource>(dictionary[id]);
            Assert.Equal(Windows31Metrics.IconImageSize, original.PixelWidth);
            Assert.Equal(Windows31Metrics.IconImageSize, original.PixelHeight);
            var pixels = new FormatConvertedBitmap(original, PixelFormats.Bgra32, null, 0);
            var stride = pixels.PixelWidth * BytesPerPixel;
            var buffer = new byte[stride * pixels.PixelHeight];
            pixels.CopyPixels(buffer, stride, 0);
            Assert.Contains((byte)0, buffer.Where((_, index) => index % BytesPerPixel == BytesPerPixel - 1));
            Assert.Contains((byte)255, buffer.Where((_, index) => index % BytesPerPixel == BytesPerPixel - 1));
            if (id != Windows31ApplicationId.ProgramManager)
                Assert.True(signatures.Add(Convert.ToHexString(SHA256.HashData(buffer))), id.ToString());
        }
        Assert.Equal(Enum.GetValues<Windows31ApplicationId>().Length - 1, signatures.Count);
    });
}
