using RetroMediaLab.App.Functions.Input.Bindings;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.Services.Input.GameInput;
using RetroMediaLab.App.ViewModels.Input;
using System.Windows;
using System.Windows.Controls;
using AnalogChoice = RetroMediaLab.App.Functions.Input.Bindings.AnalogBindingFunctions.AnalogChoice;

namespace RetroMediaLab.App.Views.Controls.Emulation.Input;

public partial class InputBindingEditor
{
    internal IGameInputControllerSource AnalogControllerSource { get; set; } = GameInputControllerSource.Instance;
    private readonly List<(ComboBox Selector, IReadOnlyDictionary<string, InputBindingRow> Rows)> _analogSelectors = [];
    private bool _updatingAnalog;

    private void RebuildAnalogBindings()
    {
        _analogSelectors.Clear();
        AnalogBindings.Children.Clear();
        if (!_bindings.CaptureSources.HasFlag(RetroMediaLab.App.Enums.Input.InputCaptureSources.Controller)) return;
        var groups = AnalogBindingFunctions.GroupRows(_bindings.Rows);
        if (groups.Count == 0) return;
        AnalogBindings.Children.Add(new TextBlock { Text = LocExtension.Get("Emulation.Input.Analog.Title"),
            FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 6) });
        var number = 0;
        foreach (var group in groups)
        {
            var grid = new Grid { Margin = new Thickness(0, 3, 0, 3) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.Children.Add(new TextBlock { Text = group.Count == 4
                ? string.Format(LocExtension.Get("Emulation.Input.Analog.Stick"), ++number)
                : group["Left"].Label + " / " + group["Right"].Label,
                TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0) });
            var selector = new ComboBox { DisplayMemberPath = nameof(AnalogChoice.Label), MinWidth = 0 };
            _analogSelectors.Add((selector, group));
            PopulateAnalogSelector(selector, group);
            selector.DropDownOpened += (_, _) => PopulateAnalogSelector(selector, group);
            selector.SelectionChanged += (_, _) =>
            {
                if (_updatingAnalog || selector.SelectedItem is not AnalogChoice { Bindings: { } bindings }) return;
                CancelCapture();
                _updatingAnalog = true;
                try
                {
                    foreach (var (direction, target) in group) target.Binding = bindings[direction];
                    ValidateBindings();
                }
                finally { _updatingAnalog = false; }
                RefreshAnalogSelections();
                BindingsChanged?.Invoke(this, EventArgs.Empty);
            };
            Grid.SetColumn(selector, 1);
            grid.Children.Add(selector);
            AnalogBindings.Children.Add(grid);
        }
    }

    private void PopulateAnalogSelector(ComboBox selector, IReadOnlyDictionary<string, InputBindingRow> group)
    {
        _updatingAnalog = true;
        try
        {
            var choices = AnalogBindingFunctions.ReadChoices(AnalogControllerSource, group);
            selector.ItemsSource = choices;
            selector.SelectedItem = choices.FirstOrDefault(choice => AnalogBindingFunctions.Matches(choice, group)) ?? choices[0];
        }
        finally { _updatingAnalog = false; }
    }

    private void RefreshAnalogSelections()
    {
        if (_updatingAnalog) return;
        _updatingAnalog = true;
        try
        {
            foreach (var (selector, rows) in _analogSelectors)
                selector.SelectedItem = selector.Items.OfType<AnalogChoice>().FirstOrDefault(choice => AnalogBindingFunctions.Matches(choice, rows))
                    ?? selector.Items.OfType<AnalogChoice>().FirstOrDefault();
        }
        finally { _updatingAnalog = false; }
    }
}
