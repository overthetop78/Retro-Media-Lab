using RetroMediaLab.Emulation;
using RetroMediaLab.Emulation.Contracts;

namespace RetroMediaLab.App.Controllers.Emulation.Storage;

internal sealed class CartridgeConfigurationSession(
    CartridgeSlotSettings settings,
    IReadOnlyList<EmulationSettingsField>? configurationFields,
    Func<IReadOnlyDictionary<string, string>, IReadOnlyList<EmulationSettingsField>>? describeOptions)
{
    private readonly Dictionary<string, string> _configured = settings.Options is { } options
        ? new(options, StringComparer.Ordinal)
        : new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _selected = new(StringComparer.Ordinal);

    internal bool RefreshesOnSelection => describeOptions is not null;

    internal IReadOnlyList<(EmulationSettingsField Field, EmulationSettingsChoice Choice)> Describe()
    {
        var fields = describeOptions?.Invoke(_configured) ?? configurationFields ?? [];
        _selected.Clear();
        var display = new List<(EmulationSettingsField, EmulationSettingsChoice)>(fields.Count);
        foreach (var field in fields)
        {
            if (field.Editor != EmulationSettingsEditor.Selection || field.Choices is not { Count: > 0 } choices)
                throw new ArgumentException(nameof(configurationFields));
            var value = _configured.TryGetValue(field.Id, out var configured) ? configured : field.Value;
            var choice = choices.FirstOrDefault(candidate => candidate.Id == value) ?? choices[0];
            _selected[field.Id] = choice.Id;
            display.Add((field, choice));
        }
        foreach (var id in _configured.Keys.Where(id => !_selected.ContainsKey(id)).ToArray())
            _configured.Remove(id);
        return display;
    }

    internal void Select(string fieldId, string choiceId)
    {
        _selected[fieldId] = choiceId;
        if (RefreshesOnSelection) _configured[fieldId] = choiceId;
    }

    internal CartridgeSlotSettings Capture(bool enabled) => new(enabled, new Dictionary<string, string>(_selected, StringComparer.Ordinal));
}
