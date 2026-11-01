using RetroMediaLab.Updates.Contracts;

namespace RetroMediaLab.App.Contracts.Updates;

internal sealed record PendingModuleInstallation(string ModuleId, string DisplayName, string ModuleVersion, string UpdateCatalogUrl, PreparedUpdateComponent Component, string WorkingDirectory);
