using RetroMediaLab.App.Enums.ViewModels.Operations;
namespace RetroMediaLab.App.Contracts.ViewModels.Operations;

public sealed record OperationResultPresentation(OperationResultState State, IReadOnlyList<OperationResultMessage> Messages);
