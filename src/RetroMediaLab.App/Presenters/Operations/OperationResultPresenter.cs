using RetroMediaLab.Infrastructure.Commands.Execution;
using RetroMediaLab.App.Contracts.ViewModels.Operations;
using RetroMediaLab.App.Enums.ViewModels.Operations;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.Functions.Localization;
using RetroMediaLab.App.Services.Logging;

namespace RetroMediaLab.App.Presenters.Operations;

public sealed class OperationResultPresenter(Action<Exception, string>? logError = null)
{
    private readonly Action<Exception, string> writeError = logError ?? ((error, context) => ErrorLog.Write(error, context));
    public OperationResultPresentation Present(OperationOutcome<GwExecutionResult> outcome)
    {
        if (outcome.Error is { } error) return Error(error);
        var result = outcome.Result!;
        var state = result.WasCancelled ? OperationResultState.Cancelled : result.IsSuccess ? OperationResultState.Success : OperationResultState.Error;
        var statusKey = state switch
        {
            OperationResultState.Success => "Operation.Succeeded",
            OperationResultState.Cancelled => "Operation.Cancelled",
            _ => "Operation.ExitCode"
        };
        var statusArguments = state == OperationResultState.Error ? new object[] { result.ExitCode } : [];
        return new(state, [new(statusKey, statusArguments, true), new("Operation.Finished", [result.ExitCode, result.Duration.ToString("g")], true)]);
    }

    public OperationResultPresentation Present(OperationOutcome<GwBatchExecutionResult> outcome)
    {
        if (outcome.Error is { } error) return Error(error);
        var result = outcome.Result!;
        var state = result.WasCancelled ? OperationResultState.Cancelled : result.FailedLabels.Count == 0 ? OperationResultState.Success : OperationResultState.Error;
        var messages = new List<OperationResultMessage>
        {
            new("Conversion.Summary", [result.SuccessfulCount, result.FailedLabels.Count], true)
        };
        if (result.FailedLabels.Count > 0)
            messages.Add(new("Conversion.Failures", [string.Join(", ", result.FailedLabels)]));
        return new(state, messages);
    }

    private OperationResultPresentation Error(Exception error)
    {
        writeError(error, "Running Greaseweazle operation");
        var detail = ExceptionDescriptionFunctions.Describe(error);
        return new(OperationResultState.Error, [new("Error.Unexpected", [detail])]);
    }
}
