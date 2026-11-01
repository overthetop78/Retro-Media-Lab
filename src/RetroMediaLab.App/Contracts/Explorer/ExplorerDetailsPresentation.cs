using RetroMediaLab.App.Enums.Explorer;
namespace RetroMediaLab.App.Contracts.Explorer;

public sealed record ExplorerDetailsPresentation(string Title, ExplorerIconCategory IconCategory, IReadOnlyList<ExplorerDetailRow> Rows, bool IsSyntheticTitle = false, ExplorerEntryTone Tone = ExplorerEntryTone.Default);
