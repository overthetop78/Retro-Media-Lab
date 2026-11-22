using System.Collections.ObjectModel;
using RetroMediaLab.App.Contracts.Input;
using RetroMediaLab.App.Enums.Input;
using RetroMediaLab.App.Functions.Input.Bindings;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.ViewModels.Input;
using RetroMediaLab.Emulation;

namespace RetroMediaLab.App.Controllers.Emulation.Input;

internal sealed class InputBindingRows
{
    private readonly ObservableCollection<InputBindingRow> _rows = [];
    private IReadOnlySet<string> _reservedBindings = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private bool _allowIdentityKeyboardBindings;

    internal ReadOnlyObservableCollection<InputBindingRow> Rows { get; }
    internal InputCaptureSources CaptureSources { get; private set; } = InputCaptureSources.Keyboard;
    internal bool PrefixKeyboardSource { get; private set; }
    internal bool HasErrors => Rows.Any(HasError);

    internal InputBindingRows() => Rows = new(_rows);

    internal void ConfigureCaptureSources(InputCaptureSources sources, bool prefixKeyboardSource, bool allowIdentityKeyboardBindings)
    {
        CaptureSources = sources;
        PrefixKeyboardSource = prefixKeyboardSource;
        _allowIdentityKeyboardBindings = allowIdentityKeyboardBindings;
    }

    internal void SetRows(IEnumerable<InputBindingDefinition> definitions, IReadOnlyDictionary<string, string>? values, string? moduleId)
    {
        _rows.Clear();
        foreach (var definition in definitions)
        {
            var label = definition.InvariantDisplayValue ?? (moduleId is null
                ? LocExtension.Get(definition.DisplayResourceKey)
                : LocExtension.GetForModule(moduleId, definition.DisplayResourceKey));
            _rows.Add(new InputBindingRow(definition.Id, label,
                values?.GetValueOrDefault(definition.Id) ?? definition.DefaultBinding, definition.DefaultBinding)
                { OppositeDirectionId = definition.OppositeDirectionId });
        }
    }

    internal void SetReservedBindings(IEnumerable<string> bindings) =>
        _reservedBindings = bindings.Where(value => !string.IsNullOrWhiteSpace(value)).ToHashSet(StringComparer.OrdinalIgnoreCase);

    internal void Validate() =>
        InputBindingValidationFunctions.Validate(Rows, CaptureSources, _reservedBindings, _allowIdentityKeyboardBindings);

    internal void RestoreDefaults()
    {
        foreach (var row in Rows) row.Binding = row.DefaultBinding;
    }

    internal void ClearConflicts()
    {
        foreach (var row in Rows.Where(HasError)) row.Binding = string.Empty;
    }

    internal InputBindingRow? Find(string id) => Rows.FirstOrDefault(row => string.Equals(row.Id, id, StringComparison.Ordinal));

    internal void ApplyOppositeAxisBinding(InputBindingRow row)
    {
        if (row.OppositeDirectionId is { } oppositeId && Find(oppositeId) is { } oppositeRow
            && EmulationSdk.InputProcessing.OppositeControllerAxisSource(row.Binding) is { } oppositeSource)
            oppositeRow.Binding = oppositeSource;
    }

    private static bool HasError(InputBindingRow row) => row.State is InputBindingState.Conflict or InputBindingState.Reserved;
}
