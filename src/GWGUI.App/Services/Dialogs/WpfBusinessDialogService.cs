using ConversionOutput = GWGUI.MediaEngine.Images.Conversion.ConversionOutput;
using GWGUI.Infrastructure.Settings.Hardware;
using GWGUI.App.Contracts.Services.Dialogs;
using GWGUI.App.Enums.Services.Dialogs;
using GWGUI.App.Interfaces.Services.Dialogs;
using GWGUI.App.Views.Dialogs.Conversion;
using GWGUI.App.Views.Dialogs.Hardware;
using GWGUI.App.Views.Dialogs.Profiles;
using GWGUI.App.Views.Dialogs.Read;
using System.Windows;
using GWGUI.App.Services.Windows;

namespace GWGUI.App.Services.Dialogs;

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
