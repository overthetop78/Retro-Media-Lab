using RetroMediaLab.App.Contracts.Emulation.Controllers;
using RetroMediaLab.App.Contracts.Emulation.Settings;
using RetroMediaLab.App.Enums.Input;
using RetroMediaLab.App.Functions.Views.Emulation.Settings;
using RetroMediaLab.App.Views.Controls.Emulation.Input;
using System.Windows;


namespace RetroMediaLab.App.Views.Controls.Emulation.Options;

internal sealed class EmulationControllerSettingsSection
{
    internal static EmulationControllerPortEditor CreatePort(int number, InputCaptureSources captureSources, bool prefixKeyboardSource, string actionLabel, string searchLabel, string moduleId, string machineId) =>
        new(number, captureSources, prefixKeyboardSource, actionLabel, searchLabel, moduleId, machineId);

    internal UIElement Build(IReadOnlyList<EmulationControllerPortSettings> ports, EmulationSettingsControlField? behavior = null, string? behaviorTitle = null, string? behaviorGlyph = null)
    {
        return EmulationSettingsLayout.ControllerSettingsPage(ports, behavior, behaviorTitle, behaviorGlyph);
    }

    internal UIElement Build(IReadOnlyList<EmulationControllerPortSettings> ports, IReadOnlyList<EmulationSettingsControlField> behaviors, string? behaviorTitle = null, string? behaviorGlyph = null)
    {
        return EmulationSettingsLayout.ControllerSettingsPage(ports, behaviors, behaviorTitle, behaviorGlyph);
    }
}
