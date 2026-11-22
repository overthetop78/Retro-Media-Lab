using System.Collections.ObjectModel;
using RetroMediaLab.App.Services.Input.GameInput;

namespace RetroMediaLab.App.ViewModels.Input;

internal sealed class ControllerInputRows
{
    private readonly ObservableCollection<ControllerInputRow> _rows = [];
    private readonly Dictionary<(GameInputControlType Type, int Index), ControllerInputRow> _byKey = [];

    internal ControllerInputRows() => Items = new(_rows);

    internal ReadOnlyObservableCollection<ControllerInputRow> Items { get; }

    internal void Update(GameInputLiveState state)
    {
        var controls = state.Controls.Where(control => control.Type != GameInputControlType.RawByte).ToArray();
        var activeKeys = controls.Select(control => (control.Type, control.Index)).ToHashSet();
        for (var index = _rows.Count - 1; index >= 0; index--)
        {
            var row = _rows[index];
            if (activeKeys.Contains(row.Key)) continue;
            _rows.RemoveAt(index);
            _byKey.Remove(row.Key);
        }
        foreach (var control in controls)
        {
            var key = (control.Type, control.Index);
            if (_byKey.TryGetValue(key, out var row)) row.Update(control);
            else
            {
                row = new ControllerInputRow(control);
                _byKey.Add(key, row);
                _rows.Add(row);
            }
        }
    }

    internal void RefreshLabels(GameInputLiveState state)
    {
        foreach (var control in state.Controls.Where(control => control.Type != GameInputControlType.RawByte))
            if (_byKey.TryGetValue((control.Type, control.Index), out var row))
                row.RefreshLabel(control);
    }

    internal void Clear()
    {
        _rows.Clear();
        _byKey.Clear();
    }
}
