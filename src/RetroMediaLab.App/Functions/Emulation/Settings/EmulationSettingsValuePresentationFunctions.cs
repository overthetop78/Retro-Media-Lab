using RetroMediaLab.App.Contracts.Emulation.Settings;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.Emulation;
using System.Globalization;
using System.Text.RegularExpressions;

namespace RetroMediaLab.App.Functions.Emulation.Settings;

internal static class EmulationSettingsValuePresentationFunctions
{
    internal sealed record CoreOptionChoice(string Value, string Label)
    {
        public override string ToString() => Label;
    }

    internal static EmulationOption[] VisibleCoreOptions(IEnumerable<EmulationOption> options) =>
        options.Where(option => option.IsVisible).ToArray();

    internal static (CoreOptionChoice[] Choices, CoreOptionChoice? Selected) CoreOptionChoices(EmulationOption option, string? moduleId)
    {
        var choices = option.Values.Select(value => new CoreOptionChoice(value.Value, CoreOptionLabel(value.Label, moduleId))).ToArray();
        var selected = choices.FirstOrDefault(value => value.Value == option.CurrentValue)
            ?? choices.FirstOrDefault(value => value.Value == option.DefaultValue)
            ?? choices.FirstOrDefault();
        return (choices, selected);
    }

    internal static string CoreOptionLabel(string text, string? moduleId)
    {
        var value = text.StartsWith("Emulation.", StringComparison.Ordinal) ? LocExtension.GetForModule(moduleId, text) : text;
        value = Regex.Replace(value, @"\s*[\(\[](?=[^()\[\]]*\b(?:restart|reboot)\b)[^()\[\]]*[\)\]]", string.Empty, RegexOptions.IgnoreCase);
        return Regex.Replace(value, @"(^|(?<=[.!?])\s+)(?<sentence>.*?)(?=(?<=[.!?])\s+|$)", match =>
        {
            var sentence = match.Groups["sentence"].Value;
            var notice = Regex.IsMatch(sentence, @"\b(?:restart|reboot)\w*\b", RegexOptions.IgnoreCase)
                && Regex.IsMatch(sentence, @"\b(?:require\w*|need\w*|must|recommended)\b|after changing|take\w* effect|to apply", RegexOptions.IgnoreCase);
            return notice ? string.Empty : match.Value;
        }, RegexOptions.Singleline).Trim();
    }

    internal static (IReadOnlyList<EmulationSettingsChoiceView> Choices, int SelectedIndex) NumericPresets(
        EmulationSettingsField field, Func<EmulationSettingsChoice, string> choiceLabel)
    {
        var range = field.NumericRange!;
        var current = Math.Clamp(ReadNumericValue(field.Value, range.Minimum), range.Minimum, range.Maximum);
        var choices = field.Choices!.Where(choice => double.TryParse(choice.Id, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && value >= range.Minimum && value <= range.Maximum).Select(choice => new EmulationSettingsChoiceView(choice, choiceLabel(choice) + (string.IsNullOrEmpty(range.UnitSuffix) ? string.Empty : " " + range.UnitSuffix))).ToList();
        if (!choices.Any(choice => choice.Choice.Id == current.ToString(CultureInfo.InvariantCulture)))
        {
            var value = current.ToString(CultureInfo.InvariantCulture);
            choices.Add(new(new(value, string.Empty, value), value + " " + range.UnitSuffix));
        }
        choices = choices.OrderBy(choice => double.Parse(choice.Choice.Id, CultureInfo.InvariantCulture)).ToList();
        return (choices, choices.FindIndex(choice => choice.Choice.Id == current.ToString(CultureInfo.InvariantCulture)));
    }

    internal static double ReadNumericValue(string? value, double fallback) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;

    internal static double NormalizeEditableSlider(double value, EmulationSettingsNumericRange range)
    {
        var fineStep = range.FineStep ?? range.Step;
        return Math.Clamp(Math.Round(value / fineStep) * fineStep, range.Minimum, range.Maximum);
    }

    internal static string SliderLabel(EmulationSettingsField field, string value, Func<EmulationSettingsChoice, string> choiceLabel)
    {
        var choice = field.UseChoiceLabelsForSlider ? field.Choices?.FirstOrDefault(item => item.Id == value) : null;
        return choice is not null ? choiceLabel(choice) : WithUnit(value, field.ValueUnitResourceKey) + field.NumericRange!.UnitSuffix;
    }

    internal static string WithUnit(string value, string? resourceKey) => resourceKey is not null
        && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _)
            ? value + " " + LocExtension.Get(resourceKey) : value;
    internal static string? ScaledFrequency(long? nominalHz, string? scale)
    {
        if (nominalHz is null || !double.TryParse(scale?.TrimEnd('%'), NumberStyles.Float, CultureInfo.InvariantCulture, out var percentage) || !double.IsFinite(percentage)) return null;
        return $"{nominalHz.Value / 1_000_000d * percentage / 100d:0.####} MHz";
    }
    internal static string DisplayValue(EmulationSettingsField field, IEmulationModule? module = null)
    {
        var choice = field.Choices?.FirstOrDefault(value => value.Id == field.Value) ?? field.Choices?.FirstOrDefault();
        return WithUnit(choice?.InvariantDisplayValue ?? (choice is null ? field.Value ?? string.Empty : (module is null ? LocExtension.Get(choice.DisplayResourceKey) : LocExtension.GetForModule(module, choice.DisplayResourceKey))), field.ValueUnitResourceKey);
    }
    internal static long DefaultNumericValue(EmulationSettingsField field)
    {
        if (field.Editor == EmulationSettingsEditor.Toggle)
            return field.Value == field.EnabledValue ? field.NumericValue ?? 0 : 0;
        if (field.Editor == EmulationSettingsEditor.Information
            && field.NumericValue is { } information) return information;
        var selected = field.Choices?.FirstOrDefault(choice => choice.Id == field.Value);
        return MemoryBytes(selected?.InvariantDisplayValue ?? field.Value) ?? selected?.NumericValue ?? 0;
    }

    internal static long? MemoryBytes(string? display)
    {
        if (string.IsNullOrWhiteSpace(display)) return null;
        foreach (var (unit, multiplier) in MemoryUnits)
            if (display.EndsWith(unit, StringComparison.OrdinalIgnoreCase)
                && double.TryParse(display[..^unit.Length].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
                && double.IsFinite(number) && number >= 0 && number <= long.MaxValue / multiplier)
                return checked((long)(number * multiplier));
        return null;
    }

    private static readonly (string Unit, long Multiplier)[] MemoryUnits =
    [
        ("GiB", 1024L * 1024 * 1024), ("GB", 1024L * 1024 * 1024),
        ("MiB", 1024L * 1024), ("MB", 1024L * 1024),
        ("KiB", 1024L), ("KB", 1024L), ("B", 1L)
    ];

    internal static (string Value, string Unit) FormatMemorySize(long bytes) => bytes >= 1024 * 1024 ? ($"{bytes / (1024d * 1024d):0.##}", "MiB") : bytes >= 1024 ? ($"{bytes / 1024d:0.##}", "KiB") : (bytes.ToString(), "B");

}
