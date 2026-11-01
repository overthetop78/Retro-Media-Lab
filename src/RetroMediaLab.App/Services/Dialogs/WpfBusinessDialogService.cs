using ConversionOutput = RetroMediaLab.MediaEngine.Images.Conversion.ConversionOutput;
using RetroMediaLab.Infrastructure.Settings.Hardware;
using RetroMediaLab.App.Contracts.Services.Dialogs;
using RetroMediaLab.App.Enums.Services.Dialogs;
using RetroMediaLab.App.Interfaces.Services.Dialogs;
using RetroMediaLab.App.Views.Dialogs.Conversion;
using RetroMediaLab.App.Views.Dialogs.Hardware;
using RetroMediaLab.App.Views.Dialogs.Profiles;
using RetroMediaLab.App.Views.Dialogs.Read;
using System.Windows;
using RetroMediaLab.App.Services.Windows;

namespace RetroMediaLab.App.Services.Dialogs;

public sealed class WpfBusinessDialogService(Window owner) : IBusinessDialogService
{
    public string? PromptProfileName(string? initialName = null)
    {
        var dialog = new ProfileNameWindow(initialName);
        return WpfWindowNavigationService.ShowOwnedDialog(owner, dialog) == true ? dialog.ProfileName : null;
    }

    public ReadConflictChoice? ResolveReadConflict(string outputPath)
    {
        var dialog = new ReadConflictWindow(outputPath);
        return WpfWindowNavigationService.ShowOwnedDialog(owner, dialog) == true ? dialog.Choice : null;
    }

    public IReadOnlyList<ConversionConflictDecision>? ResolveConversionConflicts(IReadOnlyList<ConversionOutput> outputs)
    {
        var dialog = new ConversionConflictWindow(outputs);
        return WpfWindowNavigationService.ShowOwnedDialog(owner, dialog) == true ? dialog.Rows.Select(row => new ConversionConflictDecision(row.Output, row.Choice)).ToArray() : null;
    }

    public MissingHardwareChoice ResolveMissingHardware(IReadOnlyList<ControllerSettings> controllers)
    {
        var dialog = new HardwareUnavailableWindow(controllers);
        return WpfWindowNavigationService.ShowOwnedDialog(owner, dialog) == true ? dialog.Choice : MissingHardwareChoice.Continue;
    }
}
