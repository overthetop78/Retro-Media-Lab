using RetroMediaLab.App.Enums.ViewModels.Visualization;

namespace RetroMediaLab.App.Contracts.ViewModels.Visualization;

public sealed record MediaInspectorEntry(string Label, string Value, string? Unit = null, MediaInspectorEntryLevel Level = MediaInspectorEntryLevel.Information);
