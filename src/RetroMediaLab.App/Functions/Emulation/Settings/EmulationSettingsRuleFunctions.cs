using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Enums;

namespace RetroMediaLab.App.Functions.Emulation.Settings;

internal static class EmulationSettingsRuleFunctions
{
    internal static bool Conflicts(EmulationSettingsRule rule, string? source, string? target) =>
        rule.Category == EmulationSettingsRuleCategory.MutuallyExclusive
        && source != rule.ComparedValue && target != rule.ComparedValue;

    internal static bool HidesTarget(EmulationSettingsRule rule, string? source) =>
        rule.Category == EmulationSettingsRuleCategory.VisibleWhenSourceDiffers && source == rule.ComparedValue;

    internal static IReadOnlyDictionary<string, string?> ChangesFor(
        EmulationMachineSettings settings, string fieldId, string value)
    {
        var changes = new Dictionary<string, string?>(StringComparer.Ordinal) { [fieldId] = value };
        var fields = settings.Blocks.SelectMany(block => block.Fields).ToLookup(field => field.Id, StringComparer.Ordinal);
        string? Read(string id) => changes.TryGetValue(id, out var changed) ? changed : fields[id].FirstOrDefault()?.Value;
        foreach (var rule in settings.Rules ?? [])
        {
            var otherId = rule.SourceFieldId == fieldId ? rule.TargetFieldId
                : rule.TargetFieldId == fieldId ? rule.SourceFieldId : null;
            if (otherId is not null && fields.Contains(otherId) && Conflicts(rule, Read(rule.SourceFieldId), Read(rule.TargetFieldId)))
                changes[otherId] = rule.ComparedValue;
        }
        return changes;
    }
}
