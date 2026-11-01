using RetroMediaLab.MediaEngine.Contracts.Migration;
using RetroMediaLab.App.Functions.Localization;

namespace RetroMediaLab.App.ViewModels.Conversion;

public sealed record FileMigrationTargetOption(FileSystemMigrationTarget Target)
{
    public string Label => FileMigrationTargetLocalizer.GetDisplayName(Target.FormatId);
}
