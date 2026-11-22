using RetroMediaLab.App.Functions.Localization;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.Presenters.Tools;
using RetroMediaLab.App.Services.Logging;
using RetroMediaLab.Infrastructure.Commands;
using RetroMediaLab.Infrastructure.Commands.Building;
using RetroMediaLab.Infrastructure.Commands.Execution;
using RetroMediaLab.Infrastructure.Processes;

namespace RetroMediaLab.App.Services.Maintenance;

internal sealed class GwToolSession(string executable, string verb, string? device, string? drive,
    IGreaseweazleRunner runner, IGwCommandBuilder commandBuilder, ConsoleLogSession? consoleLog,
    Action<Exception, string> logError)
{
    private CancellationTokenSource? _cancellation;

    internal bool IsRunning => _cancellation is not null || runner.IsRunning;

    internal void Cancel() => _cancellation?.Cancel();

    internal GwCommand BuildCommand(IReadOnlyDictionary<string, string> fields, IReadOnlySet<string> checks) =>
        commandBuilder.BuildTool(new(executable, verb, fields, checks, device, drive));

    internal async Task ExecuteAsync(Func<GwCommand> buildCommand, Func<bool> confirmBootloader,
        Func<Func<Task>, Task> dispatch, Action<string> showLine, Action<string> showSummary)
    {
        var cancellation = new CancellationTokenSource();
        _cancellation = cancellation;
        var progress = new ToolProgress(dispatch, async line =>
        {
            showLine(line.Text);
            if (consoleLog is not null) await consoleLog.AppendAsync(line.Text);
        });
        try
        {
            if (verb == "update" && !confirmBootloader()) return;
            var command = buildCommand();
            if (consoleLog is not null) await consoleLog.BeginAsync(verb, command.ToDisplayString());
            var result = await runner.RunAsync(command, progress, cancellation.Token);
            await progress.CompleteAsync();
            var summary = GwToolResultPresenter.Summary(verb, result);
            showSummary(summary);
            if (consoleLog is not null) await consoleLog.AppendAsync(summary);
        }
        catch (Exception exception)
        {
            logError(exception, $"Running GW tool '{verb}'");
            var summary = LocExtension.Get("Error.Unexpected", ExceptionDescriptionFunctions.Describe(exception));
            showSummary(summary);
            if (consoleLog is not null) await consoleLog.AppendAsync(summary);
        }
        finally
        {
            try { await progress.CompleteAsync(); }
            finally
            {
                _cancellation = null;
                cancellation.Dispose();
            }
        }
    }

    private sealed class ToolProgress(Func<Func<Task>, Task> dispatch, Func<GwOutputLine, Task> publish) : IProgress<GwOutputLine>
    {
        private readonly List<Task> _pending = [];
        private readonly object _gate = new();

        public void Report(GwOutputLine line)
        {
            var task = dispatch(() => publish(line));
            lock (_gate) _pending.Add(task);
        }

        internal Task CompleteAsync()
        {
            lock (_gate) return Task.WhenAll(_pending);
        }
    }
}
