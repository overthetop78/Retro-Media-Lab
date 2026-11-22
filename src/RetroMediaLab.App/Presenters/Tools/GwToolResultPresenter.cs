using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.Infrastructure.Commands.Execution;
using RetroMediaLab.Infrastructure.Hardware.Parsing;

namespace RetroMediaLab.App.Presenters.Tools;

internal static class GwToolResultPresenter
{
    internal static string Summary(string verb, GwExecutionResult result)
    {
        if (verb != "info")
            return result.IsSuccess ? LocExtension.Get("Operation.Succeeded")
                : result.WasCancelled ? LocExtension.Get("Operation.Cancelled")
                : LocExtension.Get("Operation.ExitCode", result.ExitCode);

        var info = GwInfoParser.Parse(string.Join(Environment.NewLine, result.Output.Select(line => line.Text)));
        var model = info.Model ?? LocExtension.Get("Tool.ControllerFallback");
        var firmware = info.FirmwareVersion ?? LocExtension.Get("Tool.FirmwareUnknown");
        var port = info.Port ?? LocExtension.Get("Tool.PortUnknown");
        return $"{model} · {firmware} · {port}" + (info.HasNetworkWarning ? "\n" + LocExtension.Get("Tool.NetworkWarning") : "");
    }
}
