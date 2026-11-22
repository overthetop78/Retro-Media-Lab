using OutputChoice = RetroMediaLab.App.Functions.Emulation.Controllers.ControllerOutputChoices.OutputChoice;
using RumbleDeviceChoice = RetroMediaLab.App.Functions.Emulation.Controllers.ControllerOutputChoices.RumbleDeviceChoice;
using RetroMediaLab.App.Functions.Emulation.Controllers;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.Services.Input.GameInput;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Constants;
using System.Windows;
using System.Windows.Controls;

namespace RetroMediaLab.App.Views.Controls.Emulation.Input;

internal sealed class EmulationControllerOutputEditor : StackPanel
{
    internal sealed record PresentationRow(string Label, ComboBox Selector, Action Refresh);
    private readonly List<PresentationRow> _presentationRows = [];
    internal IReadOnlyList<PresentationRow> PresentationRows => _presentationRows;
    internal event EventHandler? PresentationChanged;
    private readonly IGameInputControllerSource _source;
    private readonly Dictionary<string, EmulationControllerOutputBinding> _bindings = new(StringComparer.Ordinal);
    private IReadOnlyList<EmulationControllerOutputDefinition> _definitions = [];

    internal EmulationControllerOutputEditor(IGameInputControllerSource? source = null)
    {
        _source = source ?? GameInputControllerSource.Instance;
        Visibility = Visibility.Collapsed;
        Margin = new Thickness(0, 8, 0, 0);
    }

    internal event EventHandler? BindingsChanged;

    internal IReadOnlyDictionary<string, EmulationControllerOutputBinding> Values => _definitions
        .Where(definition => _bindings.ContainsKey(definition.Id)).ToDictionary(definition => definition.Id, definition => _bindings[definition.Id], StringComparer.Ordinal);

    internal void SetBindings(IReadOnlyDictionary<string, EmulationControllerOutputBinding> bindings)
    {
        _bindings.Clear();
        foreach (var binding in bindings) _bindings[binding.Key] = binding.Value;
    }

    internal void SetDefinitions(IReadOnlyList<EmulationControllerOutputDefinition>? definitions, string? moduleId)
    {
        _definitions = definitions ?? [];
        Children.Clear();
        _presentationRows.Clear();
        Visibility = _definitions.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        if (_definitions.Count > 0 && _definitions.All(definition => definition.Id is
            EmulationControllerFeedbackCommands.Strong or EmulationControllerFeedbackCommands.Weak))
        {
            BuildRumbleDeviceSelector();
            PresentationChanged?.Invoke(this, EventArgs.Empty);
            return;
        }
        foreach (var definition in _definitions)
        {
            var row = new Grid { Margin = new Thickness(0, 4, 0, 4) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(180) });
            row.ColumnDefinitions.Add(new ColumnDefinition());
            row.Children.Add(new TextBlock { Text = LocExtension.GetForModule(moduleId, definition.DisplayResourceKey), VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap });
            var selector = new ComboBox { DisplayMemberPath = nameof(OutputChoice.Label), MinWidth = 0 };
            Populate(selector, definition.Id);
            selector.DropDownOpened += (_, _) => Populate(selector, definition.Id);
            selector.SelectionChanged += (_, _) =>
            {
                if (selector.SelectedItem is not OutputChoice choice) return;
                if (choice.Binding is null) _bindings.Remove(definition.Id);
                else _bindings[definition.Id] = choice.Binding;
                BindingsChanged?.Invoke(this, EventArgs.Empty);
            };
            Grid.SetColumn(selector, 1);
            row.Children.Add(selector);
            Children.Add(row);
            _presentationRows.Add(new(((TextBlock)row.Children[0]).Text, selector, () => Populate(selector, definition.Id)));
        }
        PresentationChanged?.Invoke(this, EventArgs.Empty);
    }

    private void BuildRumbleDeviceSelector()
    {
        var selectedDevice = _definitions.Select(definition => _bindings.GetValueOrDefault(definition.Id)?.DeviceId)
            .FirstOrDefault(id => !string.IsNullOrWhiteSpace(id));
        ControllerOutputChoices.AssignRumbleDevice(selectedDevice, _definitions, _bindings);
        var row = new Grid { Margin = new Thickness(0, 4, 0, 4) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(180) });
        row.ColumnDefinitions.Add(new ColumnDefinition());
        row.Children.Add(new TextBlock { Text = LocExtension.Get("Emulation.Controller.Feedback.Device"),
            VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap });
        var selector = new ComboBox { DisplayMemberPath = nameof(RumbleDeviceChoice.Label), MinWidth = 0 };
        var populating = false;
        void PopulateDevices()
        {
            populating = true;
            try
            {
                var choices = ControllerOutputChoices.RumbleDevices(_source, selectedDevice);
                selector.ItemsSource = choices;
                selector.SelectedItem = choices.First(choice => choice.DeviceId == selectedDevice);
            }
            finally { populating = false; }
        }
        PopulateDevices();
        selector.DropDownOpened += (_, _) => PopulateDevices();
        selector.SelectionChanged += (_, _) =>
        {
            if (populating || selector.SelectedItem is not RumbleDeviceChoice choice) return;
            selectedDevice = choice.DeviceId;
            ControllerOutputChoices.AssignRumbleDevice(selectedDevice, _definitions, _bindings);
            BindingsChanged?.Invoke(this, EventArgs.Empty);
        };
        Grid.SetColumn(selector, 1);
        row.Children.Add(selector);
        Children.Add(row);
        _presentationRows.Add(new(((TextBlock)row.Children[0]).Text, selector, PopulateDevices));
    }

    private void Populate(ComboBox selector, string commandId)
    {
        _bindings.TryGetValue(commandId, out var selected);
        var choices = ControllerOutputChoices.Motors(_source, selected);
        selector.ItemsSource = choices;
        selector.SelectedItem = choices.FirstOrDefault(choice => choice.Binding == selected) ?? choices[0];
    }
}
