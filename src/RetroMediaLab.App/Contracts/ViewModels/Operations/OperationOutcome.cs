namespace RetroMediaLab.App.Contracts.ViewModels.Operations;

public sealed record OperationOutcome<T>(bool HasResult, T? Result, Exception? Error);
