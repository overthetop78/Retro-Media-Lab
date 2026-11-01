using RetroMediaLab.App.Enums.Explorer;

namespace RetroMediaLab.App.Contracts.Explorer;

public sealed record ExplorerFileTypeDefinition(
    string Extension,
    ExplorerFileCategory Category,
    string TypeResourceKey,
    ExplorerExecutionKind ExecutionKind,
    ExplorerIconCategory IconCategory,
    ExplorerContentFormat ContentFormat,
    ExplorerTextEncoding TextEncoding,
    ExplorerPreviewKind PreviewKind);
