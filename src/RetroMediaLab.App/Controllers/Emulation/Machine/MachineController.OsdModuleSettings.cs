using System.Globalization;
using RetroMediaLab.App.Constants.Machine;
using RetroMediaLab.App.Constants.Emulation;
using RetroMediaLab.App.Constants.Localization;
using RetroMediaLab.App.Functions.Emulation.Settings;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.Emulation;
using Entry = RetroMediaLab.VideoPresentation.Windows.Osd.FullscreenOsdWindow.Entry;
using Setting = RetroMediaLab.VideoPresentation.Windows.Osd.FullscreenOsdWindow.Setting;

namespace RetroMediaLab.App.Controllers.Emulation.Machine;

internal sealed partial class MachineController
{
    private IEmulationConfiguration? _osdConfiguration;
    private EmulationMachineSettings? _osdModuleSettings;
    private IReadOnlyList<EmulationSettingsBlock> OsdModuleBlocks => _osdModuleSettings?.Blocks ?? [];
    private bool IsOsdModuleTabVisible(EmulationMachineTab tab) =>
        _osdModuleSettings?.Visibility.Tabs.GetValueOrDefault(tab) == true;

    private Entry OsdAudioSettings()
    {
        var children = new List<Entry>
        {
            new(LocExtension.Get("Emulation.Shortcut.Mute"), ToggleAudioAsync,
                IsEnabled: () => _session.IsPowered, Status: () => _audioMuted ? "✓" : "—")
        };
        children.AddRange(OsdModuleGroups(EmulationMachineTab.Audio));
        return new(LocExtension.Get("Emulation.Shortcut.OsdAudioSettings"), Children: children,
            Glyph: MachineCommandGlyphConstants.Audio, FullHeight: true);
    }

    private Entry OsdMachineSettings()
    {
        var tabs = EmulationMachineTabConstants.Definitions
            .Where(tab => tab.Tab is not (EmulationMachineTab.Video or EmulationMachineTab.Audio) && !IsOsdControlTab(tab.Tab))
            .Select(tab =>
            {
                var groups = OsdModuleGroups(tab.Tab);
                return new Entry(LocExtension.Get(tab.ResourceKey), Children: groups, FullHeight: true,
                    IsVisible: () => groups.Any(group => group.IsVisible?.Invoke() != false));
            }).ToArray();
        return new(LocExtension.Get("Emulation.Machine.Settings"), Children: tabs,
            Glyph: MachineCommandGlyphConstants.CoreOptions, FullHeight: true,
            IsVisible: () => tabs.Any(tab => tab.IsVisible!()));
    }

    private IReadOnlyList<Entry> OsdModuleGroups(EmulationMachineTab tab, EmulationVideoSettingsSection? videoSection = null)
    {
        if (_options.Module is not { } module) return [];
        var groups = new List<Entry>();
        foreach (var block in OsdModuleBlocks.Where(block => block.Tab == tab))
        {
            var availableFields = block.Fields.Where(field =>
                field.Editor is EmulationSettingsEditor.Toggle or EmulationSettingsEditor.Selection
                    or EmulationSettingsEditor.Information or EmulationSettingsEditor.Slider
                && (videoSection is null || field.VideoSection == videoSection)
                && (tab != EmulationMachineTab.Video
                    || field.LabelResourceKey != EmulationResourceKeys.VideoRenderingSettings));
            var fields = availableFields.Select(field => OsdModuleField(module, block.Id, field)).ToArray();
            if (fields.Length == 0) continue;
            var title = tab == EmulationMachineTab.Audio && block.Fields.Any(field => field.ChoiceSource == EmulationSettingsChoiceSource.AudioOutputDevices)
                ? LocExtension.Get("Emulation.Audio.Output")
                : LocExtension.GetForModule(module, block.TitleResourceKey);
            groups.Add(new(title, Children: fields, FullHeight: true,
                IsVisible: () => IsOsdModuleTabVisible(tab) && OsdModuleBlockVisible(block.Id)
                    && fields.Any(field => field.IsVisible?.Invoke() != false)));
        }
        return groups;
    }

    private void RefreshOsdModuleBlocks()
    {
        _osdModuleSettings = _options.Module is { } module && _osdConfiguration is { } configuration
            ? module.Describe(configuration.MachineId, configuration) : null;
    }

    private bool OsdModuleBlockVisible(string id)
    {
        var block = OsdModuleBlocks.FirstOrDefault(candidate => candidate.Id == id);
        if (block is null || !block.IsVisible || !block.Fields.Any(field => field.IsVisible)) return false;
        return block.VisibleWhenFieldId is null || OsdModuleBlocks.SelectMany(candidate => candidate.Fields)
            .Any(field => field.Id == block.VisibleWhenFieldId && field.Value == block.VisibleWhenValue);
    }

    private Entry OsdModuleField(IEmulationModule module, string blockId, EmulationSettingsField field)
    {
        EmulationSettingsField? Current() => OsdModuleBlocks.FirstOrDefault(block => block.Id == blockId)
            ?.Fields.FirstOrDefault(candidate => candidate.Id == field.Id);

        string? Read() => Current()?.Value;
        Task Save(string value) => SaveOsdModuleSettingAsync(module, field, value);
        var setting = OsdModuleEditor(module, field, Read, Save);
        return new(LocExtension.GetForModule(module, field.LabelResourceKey),
            IsEnabled: () => setting is not null && Current()?.IsEnabled == true,
            Value: setting,
            Status: setting is null ? () => EmulationSettingsValuePresentationFunctions.DisplayValue(Current() ?? field, module) : null,
            IsVisible: () => Current()?.IsVisible == true && !(_osdModuleSettings?.Rules ?? []).Any(rule =>
                rule.TargetFieldId == field.Id && EmulationSettingsRuleFunctions.HidesTarget(rule,
                    OsdModuleBlocks.SelectMany(block => block.Fields).FirstOrDefault(candidate => candidate.Id == rule.SourceFieldId)?.Value)));
    }

    private static Setting? OsdModuleEditor(IEmulationModule module, EmulationSettingsField field,
        Func<string?> read, Func<string, Task> save)
    {
        if (field.Editor == EmulationSettingsEditor.Slider && field.NumericRange is { Step: > 0 } range
            && !field.UseChoiceLabelsForSlider)
            return OsdModuleNumericEditor(field, range, read, save);

        if (field.Editor == EmulationSettingsEditor.Toggle)
        {
            return new(() => read() == field.EnabledValue ? 1 : 0,
                index => LocExtension.Get(VideoToggleResourceKeys[index != 0]),
                (index, direction) => Math.Clamp(index + direction, 0, 1),
                Preview: null, Save: index => save(index != 0 ? field.EnabledValue : field.DisabledValue));
        }
        if (field.Editor is not (EmulationSettingsEditor.Selection or EmulationSettingsEditor.Slider)) return null;
        var choices = EmulationSettingsChoiceFunctions.GetChoices(field);
        var ids = choices.Select(choice => choice.Id).ToArray();
        if (ids.Length == 0) return null;
        return new(() =>
            {
                int selected = Array.IndexOf(ids, read());
                return selected >= 0 ? selected : ids.Length;
            },
            index => index == ids.Length
                ? read() ?? string.Empty
                : EmulationSettingsValuePresentationFunctions.WithUnit(
                    choices[index].InvariantDisplayValue ?? LocExtension.GetForModule(module, choices[index].DisplayResourceKey),
                    field.ValueUnitResourceKey),
            (index, direction) => Math.Clamp(index + direction, 0, ids.Length - 1),
            Preview: null, Save: index => index == ids.Length ? Task.CompletedTask : save(ids[index]));
    }

    private static Setting OsdModuleNumericEditor(EmulationSettingsField field, EmulationSettingsNumericRange range,
        Func<string?> read, Func<string, Task> save)
    {
        var minimum = (decimal)range.Minimum;
        var maximum = (decimal)range.Maximum;
        var step = (decimal)range.Step;
        int last = (int)Math.Ceiling((maximum - minimum) / step);
        bool supportsAutomatic = field.AutomaticValue is not null;
        decimal Number(int index) => Math.Min(maximum, minimum + index * step);
        return new(() =>
            {
                var value = read();
                if (supportsAutomatic && value == field.AutomaticValue) return -1;
                return decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
                    ? (int)Math.Round((Math.Clamp(parsed, minimum, maximum) - minimum) / step) : 0;
            },
            index => index < 0
                ? LocExtension.Get("Emulation.Controller.Automatic")
                : string.IsNullOrEmpty(range.UnitSuffix)
                    ? EmulationSettingsValuePresentationFunctions.WithUnit(Number(index).ToString(CultureInfo.CurrentCulture), field.ValueUnitResourceKey)
                    : Number(index).ToString(CultureInfo.CurrentCulture) + " " + range.UnitSuffix,
            (index, direction) => Math.Clamp(index + direction, supportsAutomatic ? -1 : 0, last),
            Preview: null,
            Save: index => save(index < 0 ? field.AutomaticValue! : Number(index).ToString(CultureInfo.InvariantCulture)),
            Range: (supportsAutomatic ? -1 : 0, last));
    }

    private async Task SaveOsdModuleSettingAsync(IEmulationModule module, EmulationSettingsField field, string value)
    {
        var configuration = (await module.LoadConfigurationsAsync())
            .FirstOrDefault(item => item.Id == _options.ConfigurationId) ?? _osdConfiguration!;
        var settings = module.Describe(configuration.MachineId, configuration);
        var updated = module.ApplySettings(configuration, EmulationSettingsRuleFunctions.ChangesFor(settings, field.Id, value));
        await module.SaveConfigurationAsync(updated);
        await ApplyConfigurationAsync(updated);
        await ApplyRuntimeOptionsAsync(module.RuntimeOptions(updated));
        if (!_disposed && field.Tab == EmulationMachineTab.Audio
            && field.Editor == EmulationSettingsEditor.Toggle && field.LabelResourceKey == "Emulation.Audio.Enabled")
            SetAudioMuted(value != field.EnabledValue);
    }
}
