using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GWGUI.App.Contracts.Explorer;
using GWGUI.App.Enums.Explorer;

namespace GWGUI.App.Theme.OS.Editions.Windows31;

internal sealed class Windows31FileImages : IFileEntryImageProvider
{
    private enum Glyph
    {
        Program = 0,
        Document = 1,
        File = 2,
        Folder = 5,
        CollapsedBranch = 6,
        OpenFolder = 7,
        ExpandedBranch = 8
    }

    private const string Resource = "/gwgui.app;component/Theme/OS/Editions/Windows31/Assets/Images/FileManager.bmp";
    private const int GlyphSize = 16;
    private const int BytesPerPixel = 4;
    private const int Dpi = 96;
    private const byte Opaque = byte.MaxValue;
    private static readonly Color BackgroundMarker = Colors.Blue;
    private static readonly IReadOnlyDictionary<Glyph, BitmapSource> Images = Load();

    public Windows31FileImages() { }
    public bool SupportsTint => false;

    internal static ImageSource FolderBranch(bool hasChildren, bool expanded) => Images[
        hasChildren ? expanded ? Glyph.ExpandedBranch : Glyph.CollapsedBranch
            : expanded ? Glyph.OpenFolder : Glyph.Folder];

    public ImageSource GetImage(ExplorerIconCategory category, int requestedSize, bool expanded)
        => Images[category switch
        {
            ExplorerIconCategory.Folder => expanded ? Glyph.OpenFolder : Glyph.Folder,
            ExplorerIconCategory.Program or ExplorerIconCategory.Executable or ExplorerIconCategory.Command
                or ExplorerIconCategory.BasicProgram or ExplorerIconCategory.ObjectCode => Glyph.Program,
            ExplorerIconCategory.Text or ExplorerIconCategory.Document or ExplorerIconCategory.SourceCode
                or ExplorerIconCategory.Configuration => Glyph.Document,
            _ => Glyph.File
        }];

    private static IReadOnlyDictionary<Glyph, BitmapSource> Load()
    {
        var resource = Application.GetResourceStream(new Uri(Resource, UriKind.Relative))
            ?? throw new FileNotFoundException(Resource);
        using var stream = resource.Stream;
        var strip = BitmapFrame.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        var images = new Dictionary<Glyph, BitmapSource>();
        foreach (var glyph in Enum.GetValues<Glyph>())
        {
            var crop = new CroppedBitmap(strip, new Int32Rect((int)glyph * GlyphSize, 0, GlyphSize, GlyphSize));
            var converted = new FormatConvertedBitmap(crop, PixelFormats.Bgra32, null, 0);
            var pixels = new byte[GlyphSize * GlyphSize * BytesPerPixel];
            var stride = GlyphSize * BytesPerPixel;
            converted.CopyPixels(pixels, stride, 0);
            for (var index = 0; index < pixels.Length; index += BytesPerPixel)
                pixels[index + 3] = pixels[index] == BackgroundMarker.B
                    && pixels[index + 1] == BackgroundMarker.G && pixels[index + 2] == BackgroundMarker.R
                        ? (byte)0 : Opaque;
            var image = BitmapSource.Create(GlyphSize, GlyphSize, Dpi, Dpi, PixelFormats.Bgra32, null, pixels, stride);
            image.Freeze();
            images.Add(glyph, image);
        }
        return images;
    }
}
