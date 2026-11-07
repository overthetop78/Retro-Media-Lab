using RetroMediaLab.App.Constants.Machine;
using System.Globalization;
using RetroMediaLab.Emulation;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.Contracts.Machine;

namespace RetroMediaLab.App.Functions.Machine;

internal static class MachinePresentationFunctions
{
    internal static void UpdateStatus(MachineCommandPresentation presentation, VideoFrame frame,
        double expectedFramesPerSecond, double measuredFramesPerSecond, EmulationExecutionRate? executionRate = null)
    {
        presentation.Status.Text = Status(frame, expectedFramesPerSecond, measuredFramesPerSecond, executionRate);
        presentation.SizeStatus.Text = string.Format(CultureInfo.CurrentCulture, MachinePresentationConstants.SizeStatusFormat, frame.Width, frame.ImageHeight);
        presentation.FpsStatus.Text = string.Format(CultureInfo.CurrentCulture, MachinePresentationConstants.FpsStatusFormat, measuredFramesPerSecond);
        presentation.FrequencyStatus.Text = string.Format(CultureInfo.CurrentCulture, MachinePresentationConstants.FrequencyStatusFormat,
            expectedFramesPerSecond > MachinePresentationConstants.EmptyMeasurement ? expectedFramesPerSecond : measuredFramesPerSecond);
        presentation.ExecutionStatus.Text = executionRate?.EffectiveMegahertz is { } megahertz
            ? LocExtension.Get(MachinePresentationConstants.ExecutionRateResourceKey, megahertz,
                executionRate.Value.SpeedRatio * MachinePresentationConstants.PercentScale).Trim(MachinePresentationConstants.StatusSeparator.ToCharArray()) : string.Empty;
    }

    internal static string RendererName(EmulationVideoRenderer renderer) => renderer switch
    {
        EmulationVideoRenderer.Direct3D11 => MachinePresentationConstants.Direct3D11Renderer,
        EmulationVideoRenderer.Wpf => MachinePresentationConstants.WpfRenderer,
        _ => renderer.ToString()
    };

    internal static string Status(VideoFrame frame, double expectedFramesPerSecond, double measuredFramesPerSecond, EmulationExecutionRate? executionRate = null)
    {
        var frequency = expectedFramesPerSecond > MachinePresentationConstants.EmptyMeasurement ? expectedFramesPerSecond : measuredFramesPerSecond;
        var status = string.Format(CultureInfo.CurrentCulture, MachinePresentationConstants.StatusFormat, frame.Width, frame.ImageHeight, frequency, measuredFramesPerSecond);
        return executionRate?.EffectiveMegahertz is { } megahertz
            ? status + LocExtension.Get(MachinePresentationConstants.ExecutionRateResourceKey, megahertz,
                executionRate.Value.SpeedRatio * MachinePresentationConstants.PercentScale)
            : status;
    }
}
