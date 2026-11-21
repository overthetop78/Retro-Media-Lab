namespace RetroMediaLab.Emulation.Sega.Common.Machines.Common.Functions;

internal static class CartridgeStackFunctions
{
    internal const string Option = "configuration.cartridgeStack";
    internal const string ThirtyTwoX = "32x";
    internal const string GameGenie = "game genie";
    internal const string ActionReplay = "action replay (pro)";
    internal const string SonicAndKnuckles = "sonic & knuckles";
    internal const string CdMemory = "mega-cd-memory";
    private const string None = "none";
    private const string Label = "Emulation.Option.segaemu.segaemu_lock_on";

    internal static string[] Read(MachineConfiguration configuration)
    {
        var options = configuration.Options;
        if (options?.TryGetValue(Option, out var stack) == true)
            return Validate(configuration, stack.Split('|', StringSplitOptions.RemoveEmptyEntries));

        var items = new List<string>();
        if (configuration.ThirtyTwoXEnabled || ModelConstants.HasThirtyTwoX(configuration.Model))
            items.Add(ThirtyTwoX);
        if (ModelConstants.GetMasterSystemConverter(configuration.Model, options) is { } converter)
            items.Add(converter);
        if (options?.GetValueOrDefault("segaemu_lock_on") is GameGenie or ActionReplay or SonicAndKnuckles)
            items.Add(options["segaemu_lock_on"]);
        if (HasCd(configuration) && options?.GetValueOrDefault("segaemu_cart_size") is not (null or "disabled"))
            items.Add(CdMemory);
        return Validate(configuration, items);
    }

    internal static IReadOnlyDictionary<string, string> EditorValues(MachineConfiguration configuration) =>
        Read(configuration).Select((item, index) => (Key(index), item))
            .ToDictionary(pair => pair.Item1, pair => pair.item, StringComparer.Ordinal);

    internal static IReadOnlyList<EmulationSettingsField> Describe(MachineConfiguration configuration, IReadOnlyDictionary<string, string> values)
    {
        var fields = new List<EmulationSettingsField>();
        var selected = new List<string>();
        while (Choices(configuration, selected) is { Length: > 0 } available)
        {
            string id = Key(selected.Count);
            string requested = values.GetValueOrDefault(id, None);
            string value = available.Contains(requested, StringComparer.Ordinal) ? requested : None;
            fields.Add(new(id, EmulationMachineTab.Storage, "cartridge", Label, EmulationSettingsEditor.Selection, value,
                Choices: available.Prepend(None).Select(Choice).ToArray()));
            if (value == None) break;
            selected.Add(value);
        }
        return fields;
    }

    internal static bool Apply(MachineConfiguration configuration, IReadOnlyDictionary<string, string> values, Dictionary<string, string> options)
    {
        string[] stack = Describe(configuration, values).Select(field => field.Value!)
            .TakeWhile(value => value != None).ToArray();
        if (Read(configuration).SequenceEqual(stack, StringComparer.Ordinal)) return false;
        options[Option] = string.Join('|', stack);
        options[SettingsConstants.MegaDriveThirtyTwoX] = stack.Contains(ThirtyTwoX) ? "enabled" : "disabled";
        options.Remove("segaemu_lock_on");
        if (stack.FirstOrDefault() is ModelConstants.MasterSystemConverterI or ModelConstants.MasterSystemConverterII)
            options[SettingsConstants.MasterSystemConverter] = stack[0];
        else
            options.Remove(SettingsConstants.MasterSystemConverter);
        return true;
    }

    private static string[] Validate(MachineConfiguration configuration, IEnumerable<string> requested)
    {
        var stack = new List<string>();
        foreach (var item in requested)
        {
            if (!Choices(configuration, stack).Contains(item, StringComparer.Ordinal)) break;
            stack.Add(item);
        }
        return stack.ToArray();
    }

    private static string[] Choices(MachineConfiguration configuration, IReadOnlyList<string> current)
    {
        if (current.LastOrDefault() is ModelConstants.MasterSystemConverterI or ModelConstants.MasterSystemConverterII or SonicAndKnuckles or CdMemory)
            return [];
        var choices = new List<string>();
        if (current.Count == 0)
        {
            choices.Add(ThirtyTwoX);
            if (!HasCd(configuration))
            {
                if (!configuration.IsMegaDriveModelII)
                    choices.Add(ModelConstants.MasterSystemConverterI);
                choices.Add(ModelConstants.MasterSystemConverterII);
            }
        }
        choices.AddRange([GameGenie, ActionReplay, SonicAndKnuckles]);
        if (HasCd(configuration))
            choices.Add(CdMemory);
        return choices.Except(current, StringComparer.Ordinal).ToArray();
    }

    private static bool HasCd(MachineConfiguration configuration) =>
        ModelConstants.HasMegaCd(configuration.Model) || configuration.MegaCdEnabled;

    private static string Key(int index) => Option + "." + index.ToString(System.Globalization.CultureInfo.InvariantCulture);

    private static EmulationSettingsChoice Choice(string id) => id switch
    {
        ThirtyTwoX => new(id, string.Empty, "32X"),
        ModelConstants.MasterSystemConverterI => new(id, string.Empty, "Master System Converter I"),
        ModelConstants.MasterSystemConverterII => new(id, string.Empty, "Master System Converter II"),
        CdMemory => new(id, "Emulation.Option.segaemu.segaemu_cart_size"),
        None => new(id, "Emulation.Option.segaemu.segaemu_add_on.Value.none"),
        _ => new(id, Label + ".Value." + id)
    };
}
