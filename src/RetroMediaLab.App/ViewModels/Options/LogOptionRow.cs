using RetroMediaLab.Infrastructure.Settings.Logging;
namespace RetroMediaLab.App.ViewModels.Options;

public sealed record LogOptionRow(string Action, string Label, ActionLogSettings Settings);
