using RetroMediaLab.Emulation;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.Services.Input.GameInput;
using RetroMediaLab.App.ViewModels.Input;
using RetroMediaLab.Emulation.Functions;

namespace RetroMediaLab.App.Functions.Input.Bindings;

internal static class AnalogBindingFunctions
{
    internal sealed record AnalogChoice(string Label, IReadOnlyDictionary<string, string>? Bindings);

    internal static IReadOnlyList<IReadOnlyDictionary<string, InputBindingRow>> GroupRows(IReadOnlyList<InputBindingRow> bindings)
    {
        var rows = bindings.ToDictionary(row => row.Id, StringComparer.Ordinal);
        var used = new HashSet<string>(StringComparer.Ordinal);
        var groups = new List<IReadOnlyDictionary<string, InputBindingRow>>();
        bool Paired(string first, string second) => rows.TryGetValue(first, out var a)
            && rows.TryGetValue(second, out var b) && a.OppositeDirectionId == second && b.OppositeDirectionId == first;
        foreach (var up in bindings.Where(row => row.Id.EndsWith("Up", StringComparison.Ordinal)))
        {
            var prefix = up.Id[..^2];
            if (!Paired(prefix + "Up", prefix + "Down") || !Paired(prefix + "Left", prefix + "Right")) continue;
            var group = new Dictionary<string, InputBindingRow>(StringComparer.Ordinal);
            foreach (var direction in new[] { "Left", "Right", "Up", "Down" })
            {
                group[direction] = rows[prefix + direction];
                used.Add(prefix + direction);
            }
            groups.Add(group);
        }
        foreach (var row in bindings)
        {
            if (used.Contains(row.Id) || row.OppositeDirectionId is not { } opposite || !Paired(row.Id, opposite)) continue;
            var other = rows[opposite];
            var negative = row.Id.EndsWith("Right", StringComparison.Ordinal) || row.Id.EndsWith("Down", StringComparison.Ordinal)
                ? other : row;
            var positive = ReferenceEquals(negative, row) ? other : row;
            groups.Add(new Dictionary<string, InputBindingRow> { ["Left"] = negative, ["Right"] = positive });
            used.Add(row.Id);
            used.Add(opposite);
        }
        return groups;
    }

    internal static IReadOnlyList<AnalogChoice> ReadChoices(IGameInputControllerSource controllerSource, IReadOnlyDictionary<string, InputBindingRow> group)
    {
        controllerSource.StartMonitoring();
        var choices = new List<AnalogChoice>
        {
            new(LocExtension.Get("Emulation.Input.Analog.Custom"), null),
            new(LocExtension.Get("Controllers.None"), group.ToDictionary(item => item.Key, _ => string.Empty))
        };
        void Add(string label, string device, Func<string, string> source) => choices.Add(new(label,
            group.ToDictionary(item => item.Key, item => InputBindingSyntax.Controller(device, source(item.Key)))));
        var devices = controllerSource.GetConnectedDevices();
        for (var i = 0; i < devices.Count; i++)
        {
            var device = devices[i];
            var productName = ControllerVisualProfileStore.TryGet(device.Id, out var profile)
                ? ControllerVisualProfileStore.DisplayName(profile.Model, profile.DisplayName)
                : device.ProductName;
            var name = productName + " #" + (i + 1) + " · ";
            if (device.StandardCapabilities.HasGamepad)
                foreach (var stick in new[] { "Left", "Right" })
                {
                    if (group.Count == 4)
                        Add(name + LocExtension.Get("Emulation.Input.Analog." + stick), device.Id, direction => stick + "Stick" + direction);
                    else
                    {
                        Add(name + LocExtension.Get("Controllers." + stick + "StickX"), device.Id, direction => stick + "Stick" + direction);
                        Add(name + LocExtension.Get("Controllers." + stick + "StickY"), device.Id, direction => stick + "Stick" + (direction == "Left" ? "Up" : "Down"));
                    }
                }
            if (device.StandardCapabilities.HasGamepad) continue;
            var axes = device.Controls.Where(control => control.Type == GameInputControlType.Axis).Select(control => control.Index).Distinct().Order().ToArray();
            foreach (var x in axes)
            {
                if (group.Count == 2)
                    Add(name + string.Format(LocExtension.Get("Emulation.Input.Analog.Axis"), x), device.Id,
                        direction => "Axis" + x + (direction == "Left" ? "Negative" : "Positive"));
                else foreach (var y in axes.Where(y => y != x))
                    Add(name + string.Format(LocExtension.Get("Emulation.Input.Analog.Axes"), x, y), device.Id,
                        direction => "Axis" + (direction is "Left" or "Right" ? x : y) + (direction is "Left" or "Up" ? "Negative" : "Positive"));
            }
        }
        if (!choices.Any(choice => Matches(choice, group)) && group.Values.All(row => row.Binding.StartsWith("Controller:", StringComparison.Ordinal)))
        {
            var device = EmulationSdk.InputProcessing.ParseControllerDeviceId(group.Values.First().Binding);
            if (device is not null && group.Values.All(row => EmulationSdk.InputProcessing.ParseControllerDeviceId(row.Binding) == device))
                choices.Add(new(device + " · " + LocExtension.Get("Emulation.Input.Analog.Saved"), group.ToDictionary(item => item.Key, item => item.Value.Binding)));
        }
        return choices;
    }

    internal static bool Matches(AnalogChoice choice, IReadOnlyDictionary<string, InputBindingRow> group) => choice.Bindings is not null
        && group.All(item => string.Equals(choice.Bindings.GetValueOrDefault(item.Key), item.Value.Binding, StringComparison.OrdinalIgnoreCase));

}
