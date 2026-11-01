using System.Windows;

namespace RetroMediaLab.App.Theme.OS.Editions.WindowsXP;

internal sealed record WindowsXPImageDefinition(string NativeClass, int PixelWidth, int PixelHeight,
    int ImageCount, WindowsXPImageLayout ImageLayout, WindowsXPImageSizing SizingType,
    IReadOnlyList<int> SizingMargins, IReadOnlyList<int> ContentMargins, bool Glyph,
    bool Transparent, string NativeImageField)
{
    private const int LeftIndex = 0;
    private const int RightIndex = 1;
    private const int TopIndex = 2;
    private const int BottomIndex = 3;

    internal int FrameWidth => ImageLayout == WindowsXPImageLayout.Horizontal ? PixelWidth / ImageCount : PixelWidth;
    internal int FrameHeight => ImageLayout == WindowsXPImageLayout.Vertical ? PixelHeight / ImageCount : PixelHeight;
    internal Thickness Insets => new(SizingMargins[LeftIndex], SizingMargins[TopIndex], SizingMargins[RightIndex], SizingMargins[BottomIndex]);
    internal Thickness ContentInsets => new(ContentMargins[LeftIndex], ContentMargins[TopIndex], ContentMargins[RightIndex], ContentMargins[BottomIndex]);
}
