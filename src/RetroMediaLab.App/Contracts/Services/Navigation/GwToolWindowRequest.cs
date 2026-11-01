using RetroMediaLab.Infrastructure.Settings.Logging;
namespace RetroMediaLab.App.Contracts.Services.Navigation;

public sealed record GwToolWindowRequest(string Executable, string Verb, string? Device, string? Drive, string LogsDirectory, OperationLogSettings Logging);
