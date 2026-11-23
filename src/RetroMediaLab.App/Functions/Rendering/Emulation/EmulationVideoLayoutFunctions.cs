using RetroMediaLab.App.Constants.Rendering.Emulation;
using System.Windows;

namespace RetroMediaLab.App.Functions.Rendering.Emulation;

internal static class EmulationVideoLayoutFunctions
{
    internal static Size FitFourThree(double availableWidth, double availableHeight) => Fit(availableWidth, availableHeight, EmulationVideoLayoutConstants.FourThreeAspectRatio);

    internal static (Size Screen, Size Image) FitDisplay(double availableWidth, double availableHeight,
        double screenAspect, double imageAspect)
    {
        var screen = Fit(availableWidth, availableHeight, screenAspect);
        return screen.IsEmpty ? (Size.Empty, Size.Empty) : (screen, Fit(screen.Width, screen.Height, imageAspect));
    }

    internal static Size Fit(double availableWidth, double availableHeight, double aspectRatio)
    {
        if (availableWidth <= EmulationVideoLayoutConstants.EmptyDimension
            || availableHeight <= EmulationVideoLayoutConstants.EmptyDimension
            || !double.IsFinite(aspectRatio)
            || aspectRatio <= EmulationVideoLayoutConstants.EmptyDimension)
            return Size.Empty;
        var width = Math.Min(availableWidth, availableHeight * aspectRatio);
        return new Size(width, width / aspectRatio);
    }
}
