using RetroMediaLab.Emulation;
using RetroMediaLab.App.Constants.Input.Bindings;
using RetroMediaLab.App.Constants.Input.Controllers;
using RetroMediaLab.App.Enums.Input;
using RetroMediaLab.App.ViewModels.Input;

namespace RetroMediaLab.App.Functions.Input.Bindings;

internal static class InputBindingValidationFunctions
{
    internal static void Validate(IReadOnlyList<InputBindingRow> rows, InputCaptureSources captureSources,
        IReadOnlySet<string> reservedBindings, bool allowIdentityKeyboardBindings)
    {
        var duplicates = rows.Where(row => !string.IsNullOrWhiteSpace(row.Binding) && rows.Any(other => !ReferenceEquals(row, other)
            && !string.IsNullOrWhiteSpace(other.Binding)
            && !(IsIdentityKeyboardBinding(row) && IsIdentityKeyboardBinding(other))
            && InputBindingSyntax.Conflicts(row.Binding, other.Binding))).ToHashSet();
        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.Binding)) row.SetState(InputBindingState.Unassigned);
            else if (!IsIdentityKeyboardBinding(row) && (!TryValidateBinding(row.Binding, captureSources, reservedBindings, out var reserved) || reserved)) row.SetState(InputBindingState.Reserved);
            else if (duplicates.Contains(row)) row.SetState(InputBindingState.Conflict);
            else row.SetState(InputBindingState.Valid);
        }

        bool IsIdentityKeyboardBinding(InputBindingRow row) =>
            allowIdentityKeyboardBindings && captureSources.HasFlag(InputCaptureSources.Keyboard)
            && Enum.TryParse<RetroMediaLab.Emulation.Enums.EmulationKey>(row.Id, out var key)
            && key != RetroMediaLab.Emulation.Enums.EmulationKey.Unknown
            && InputBindingSyntax.Conflicts(row.Binding, row.Id);
    }

    private static bool TryValidateBinding(string value, InputCaptureSources captureSources, IReadOnlySet<string> reservedBindings, out bool reserved)
    {
        reserved = false;
        var trimmed = value.Trim();
        reserved = reservedBindings.Any(binding => InputBindingSyntax.Conflicts(trimmed, binding));
        var parts = trimmed.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var keyboardParts = new List<string>();
        var hasPhysicalSource = false;
        foreach (var part in parts)
        {
            if (part.StartsWith(InputBindingSyntaxConstants.MousePrefix, StringComparison.OrdinalIgnoreCase))
            {
                if (!captureSources.HasFlag(InputCaptureSources.Mouse) || !EmulationSdk.InputProcessing.IsValidBinding(part)) return false;
                hasPhysicalSource = true;
            }
            else if (part.StartsWith(InputBindingSyntaxConstants.ControllerPrefix, StringComparison.OrdinalIgnoreCase))
            {
                if (!captureSources.HasFlag(InputCaptureSources.Controller) || !EmulationSdk.InputProcessing.IsValidBinding(part)) return false;
                hasPhysicalSource = true;
            }
            else keyboardParts.Add(InputBindingSyntax.TryRemovePrefix(part, InputBindingSyntaxConstants.KeyboardPrefix, out var source) ? source : part);
        }
        if (hasPhysicalSource && keyboardParts.Count == 0) return true;
        if (ControllerInputConstants.LegacyButtonNames.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
            return captureSources.HasFlag(InputCaptureSources.Controller);
        var keyboard = string.Join("+", keyboardParts);
        if (!captureSources.HasFlag(InputCaptureSources.Keyboard) || !KeyboardChordFunctions.TryParse(keyboard, out var chord)) return false;
        reserved |= KeyboardChordFunctions.IsWindowsReserved(chord);
        return true;
    }
}
