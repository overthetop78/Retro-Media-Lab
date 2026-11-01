using ConversionOutput = RetroMediaLab.MediaEngine.Images.Conversion.ConversionOutput;
using RetroMediaLab.Infrastructure.Settings.Hardware;
using RetroMediaLab.App.Contracts.Services.Dialogs;
using RetroMediaLab.App.Enums.Services.Dialogs;

namespace RetroMediaLab.App.Interfaces.Services.Dialogs;

public interface IBusinessDialogService
{
    string? PromptProfileName(string? initialName = null);
    ReadConflictChoice? ResolveReadConflict(string outputPath);
    IReadOnlyList<ConversionConflictDecision>? ResolveConversionConflicts(IReadOnlyList<ConversionOutput> outputs);
    MissingHardwareChoice ResolveMissingHardware(IReadOnlyList<ControllerSettings> controllers);
}
