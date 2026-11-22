using RetroMediaLab.App.Constants.Emulation;
using RetroMediaLab.App.Constants.Machine;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.ViewModels.Input;
using RetroMediaLab.Emulation;
using Entry = RetroMediaLab.VideoPresentation.Windows.Osd.FullscreenOsdWindow.Entry;

namespace RetroMediaLab.App.Controllers.Emulation.Machine;

internal sealed partial class MachineController
{
    private static bool IsOsdControlTab(EmulationMachineTab tab) =>
        tab is EmulationMachineTab.Controllers or EmulationMachineTab.Keyboard or EmulationMachineTab.Mouse;

    private Entry OsdControlsSettings()
    {
        var tabs = new List<Entry>();
        var module = _options.Module;
        var input = module is IEmulationInputSettingsManager manager && _osdConfiguration is { } configuration
            ? manager.DescribeInputSettings(configuration) : null;
        foreach (var tab in EmulationMachineTabConstants.Definitions.Where(tab => IsOsdControlTab(tab.Tab)))
        {
            var groups = OsdModuleGroups(tab.Tab).ToList();
            if (module is not null && input is not null)
            {
                switch (tab.Tab)
                {
                    case EmulationMachineTab.Controllers:
                        groups.AddRange(input.ControllerPorts.Select(port => OsdControllerPort(module, port)));
                        break;
                    case EmulationMachineTab.Keyboard:
                        groups.AddRange(OsdBindings(module, input.Keyboard));
                        break;
                    case EmulationMachineTab.Mouse:
                        groups.AddRange(OsdBindings(module, input.Mouse));
                        break;
                }
            }
            if (groups.Count == 0) continue;
            tabs.Add(new(LocExtension.Get(tab.ResourceKey), Children: groups, FullHeight: true,
                IsVisible: () => IsOsdModuleTabVisible(tab.Tab) && groups.Any(group => group.IsVisible?.Invoke() != false)));
        }
        return new(LocExtension.Get("Controllers.Controls"), Children: tabs,
            Glyph: MachineCommandGlyphConstants.Controller, FullHeight: true,
            IsVisible: () => tabs.Any(tab => tab.IsVisible!()));
    }

    private static Entry OsdControllerPort(IEmulationModule module, EmulationControllerPort port)
    {
        var choice = port.ControllerChoices.FirstOrDefault(item => item.Id == port.SelectedControllerId);
        var type = choice is null ? LocExtension.Get("Emulation.Controller.None")
            : choice.InvariantDisplayValue ?? LocExtension.GetForModule(module, choice.DisplayResourceKey);
        return new(port.DisplayName ?? LocExtension.Get("Emulation.Controller.Port", port.DisplayNumber ?? port.Number), Children:
            [
                new Entry(LocExtension.Get("Emulation.Controller.Type"), IsEnabled: () => false, Status: () => type),
                .. OsdBindings(module, port.Bindings)
            ], FullHeight: true);
    }

    private static IEnumerable<Entry> OsdBindings(IEmulationModule module, EmulationInputBindingSet? set)
    {
        if (set is null) yield break;
        foreach (var definition in set.Definitions)
        {
            var label = definition.InvariantDisplayValue ?? LocExtension.GetForModule(module, definition.DisplayResourceKey);
            var binding = set.Values.GetValueOrDefault(definition.Id) ?? definition.DefaultBinding;
            var value = string.IsNullOrWhiteSpace(binding) ? LocExtension.Get("Emulation.Input.Binding.LabelUnassigned")
                : string.Join(" + ", new InputBindingRow(definition.Id, label, binding, definition.DefaultBinding).BindingParts.Select(part => part.Text));
            yield return new(label, IsEnabled: () => false, Status: () => value);
        }
    }
}
