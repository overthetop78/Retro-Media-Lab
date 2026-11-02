using RetroMediaLab.App.Constants.Machine;
using System.Globalization;
using RetroMediaLab.Emulation;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.App.Localization.Extensions;

namespace RetroMediaLab.App.Functions.Machine;

internal static class MachinePresentationFunctions
{
    internal static string RendererName(EmulationVideoRenderer renderer) => renderer switch
    {
        EmulationVideoRenderer.Direct3D11 => MachinePresentationConstants.Direct3D11Renderer,
        EmulationVideoRenderer.Wpf => MachinePresentationConstants.WpfRenderer,
        _ => renderer.ToString()
    };

    internal static string Status(VideoFrame frame, double expectedFramesPerSecond, double measuredFramesPerSecond, EmulationExecutionRate? executionRate = null)
    {
        var frequency = expectedFramesPerSecond > MachinePresentationConstants.EmptyMeasurement ? expectedFramesPerSecond : measuredFramesPerSecond;
        var status = string.Format(CultureInfo.CurrentCulture, MachinePresentationConstants.StatusFormat, frame.Width, frame.Height, frequency, measuredFramesPerSecond);
        return executionRate?.EffectiveMegahertz is { } megahertz
            ? status + LocExtension.Get(MachinePresentationConstants.ExecutionRateResourceKey, megahertz,
                executionRate.Value.SpeedRatio * MachinePresentationConstants.PercentScale)
            : status;
    }
}
