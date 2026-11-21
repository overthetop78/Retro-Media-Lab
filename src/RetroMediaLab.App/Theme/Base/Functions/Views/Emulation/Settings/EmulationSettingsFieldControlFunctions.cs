using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using RetroMediaLab.App.Constants.Views.Emulation;
using RetroMediaLab.App.Contracts.Emulation.Settings;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.Emulation;

namespace RetroMediaLab.App.Functions.Views.Emulation.Settings;

internal static partial class EmulationSettingsFieldControlFunctions
{
    internal static string? Read(FrameworkElement control) => control switch
    {
        Panel { Tag: Func<string> read } => read(),
        ComboBox { SelectedItem: EmulationSettingsChoiceView choice } => choice.Choice.Id,
        CheckBox { Tag: EmulationSettingsField field } toggle => toggle.IsChecked == true ? field.EnabledValue : field.DisabledValue,
        Grid { Tag: Slider slider } => slider.Value.ToString(EmulationSliderConstants.NumericFormat, CultureInfo.InvariantCulture),
        Slider slider => slider.Value.ToString(EmulationSliderConstants.NumericFormat, CultureInfo.InvariantCulture),
        _ => null
    };

    internal static CheckBox CreateToggle(EmulationSettingsField field) => new() { IsChecked = field.Value == field.EnabledValue, Tag = field };

    internal static FrameworkElement CreateSelection(EmulationSettingsField field, IReadOnlyList<EmulationSettingsChoiceView> choices)
    {
        if (choices.Count > 0 && (field.UseRadioButtons || choices.Count == 2 && field.ChoiceSource == EmulationSettingsChoiceSource.Declared))
        {
            var radios = new WrapPanel();
            var group = Guid.NewGuid().ToString();
            var selected = choices.FirstOrDefault(choice => choice.Choice.Id == field.Value) ?? choices[0];
            foreach (var choice in choices)
            {
                var radio = new RadioButton { Content = new TextBlock { Text = choice.DisplayName, TextWrapping = TextWrapping.Wrap }, Tag = choice,
                    GroupName = group, IsChecked = choice.Choice.Id == selected.Choice.Id,
                    HorizontalContentAlignment = HorizontalAlignment.Stretch, Margin = new Thickness(0, 4, 18, 4) };
                System.Windows.Automation.AutomationProperties.SetName(radio, choice.DisplayName);
                radios.Children.Add(radio);
            }
            radios.Tag = (Func<string>)(() => radios.Children.OfType<RadioButton>()
                .Where(radio => radio.IsChecked == true).Select(radio => ((EmulationSettingsChoiceView)radio.Tag).Choice.Id).FirstOrDefault() ?? field.Value ?? string.Empty);
            return radios;
        }
        var selection = new ComboBox { ItemsSource = choices,
            SelectedItem = choices.FirstOrDefault(choice => choice.Choice.Id == field.Value) ?? choices.FirstOrDefault() };
        var text = new FrameworkElementFactory(typeof(TextBlock));
        text.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding(nameof(EmulationSettingsChoiceView.DisplayName)));
        text.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);
        selection.ItemTemplate = new DataTemplate { VisualTree = text };
        return selection;
    }

    internal static FrameworkElement CreateSlider(EmulationSettingsField field, Func<EmulationSettingsChoice, string> choiceLabel, Func<Task>? changed = null)
    {
        var range = field.NumericRange ?? throw new ArgumentException(nameof(field));
        if (range.ShowNumericInput) return CreateEditableSlider(field, changed);
        var value = double.TryParse(field.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : range.Minimum;
        var slider = new Slider
        {
            Minimum = range.Minimum,
            Maximum = range.Maximum,
            TickFrequency = range.Step,
            SmallChange = range.Step,
            LargeChange = range.Step,
            IsSnapToTickEnabled = true,
            TickPlacement = range.Step > 1 && (range.Maximum - range.Minimum) / range.Step <= 32 ? TickPlacement.BottomRight : TickPlacement.None,
            Orientation = range.IsVertical ? Orientation.Vertical : Orientation.Horizontal,
            IsDirectionReversed = range.IsVertical,
            Height = range.IsVertical ? 128 : double.NaN,
            Value = Math.Clamp(value, range.Minimum, range.Maximum),
            VerticalAlignment = VerticalAlignment.Center,
        };
        var label = new TextBlock
        {
            MinWidth = EmulationSliderConstants.ValueLabelMinimumWidth,
            Margin = new Thickness(EmulationSliderConstants.ValueLabelSpacing, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Right,
        };
        UpdateLabel();
        var savedValue = slider.Value;
        slider.ValueChanged += (_, _) => UpdateLabel();
        slider.AddHandler(Thumb.DragCompletedEvent, new DragCompletedEventHandler(async (_, _) => await SaveValueAsync()));
        slider.PreviewMouseLeftButtonUp += async (_, _) =>
        {
            if (!slider.IsMouseCaptureWithin) await SaveValueAsync();
        };
        slider.PreviewKeyUp += async (_, _) => await SaveValueAsync();
        var row = new Grid { Tag = slider };
        row.ColumnDefinitions.Add(new ColumnDefinition());
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.Children.Add(slider);
        Grid.SetColumn(label, 1);
        row.Children.Add(label);
        if (range.IsVertical || field.UseChoiceLabelsForSlider)
        {
            row.ColumnDefinitions.Clear();
            row.ColumnDefinitions.Add(new ColumnDefinition());
            row.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            row.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Grid.SetColumn(label, 0); Grid.SetRow(label, 1);
            label.Margin = new Thickness(0, 6, 0, 0);
            label.TextAlignment = TextAlignment.Left;
            label.TextWrapping = TextWrapping.Wrap;
            if (range.IsVertical)
            {
                slider.HorizontalAlignment = HorizontalAlignment.Center; slider.Width = 30;
                label.HorizontalAlignment = HorizontalAlignment.Center;
                label.TextAlignment = TextAlignment.Center; label.MinWidth = 0;
            }
        }
        if (field.AutomaticValue is { } automaticValue)
        {
            var automatic = new CheckBox { Content = LocExtension.Get("Visual.Automatic"),
                IsChecked = field.Value == automaticValue, Margin = new Thickness(0, 0, 0, 8) };
            slider.IsEnabled = automatic.IsChecked != true;
            var panel = new StackPanel();
            panel.Children.Add(automatic); panel.Children.Add(row);
            panel.Tag = (Func<string>)(() => automatic.IsChecked == true ? automaticValue
                : slider.Value.ToString(EmulationSliderConstants.NumericFormat, CultureInfo.InvariantCulture));
            automatic.Checked += async (_, _) => { slider.IsEnabled = false; if (changed is not null) await changed(); };
            automatic.Unchecked += async (_, _) => { slider.IsEnabled = true; if (changed is not null) await changed(); };
            return panel;
        }
        return row;

        void UpdateLabel()
        {
            var choice = field.UseChoiceLabelsForSlider ? field.Choices?.FirstOrDefault(item => item.Id == slider.Value.ToString(EmulationSliderConstants.NumericFormat, CultureInfo.InvariantCulture)) : null;
            label.Text = choice is not null ? choiceLabel(choice)
                : EmulationSettingsValuePresentationFunctions.WithUnit(slider.Value.ToString(EmulationSliderConstants.NumericFormat, CultureInfo.InvariantCulture), field.ValueUnitResourceKey) + range.UnitSuffix;
        }

        async Task SaveValueAsync()
        {
            if (slider.Value == savedValue) return;
            savedValue = slider.Value;
            if (changed is not null) await changed();
        }
    }
    private static FrameworkElement CreateEditableSlider(EmulationSettingsField field, Func<Task>? changed)
    {
        var range = field.NumericRange!;
        double fineStep = range.FineStep ?? range.Step;
        double Normalize(double value) => Math.Clamp(Math.Round(value / fineStep) * fineStep, range.Minimum, range.Maximum);
        var slider = new Slider
        {
            Minimum = range.Minimum, Maximum = range.Maximum, TickFrequency = fineStep,
            SmallChange = range.Step, LargeChange = range.Step, IsSnapToTickEnabled = true,
            Value = double.TryParse(field.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                ? Normalize(value) : range.Minimum,
            VerticalAlignment = VerticalAlignment.Center, MinWidth = 100
        };
        var input = CreateNumber(field with { Value = slider.Value.ToString("0.00", CultureInfo.InvariantCulture) });
        input.Text = slider.Value.ToString("0.00", CultureInfo.CurrentCulture);
        input.MinWidth = 64; input.Width = 80; input.Margin = new Thickness(8, 0, 0, 0);
        var decrement = new Button { Content = "−", MinWidth = 28, Margin = new Thickness(4, 0, 0, 0) };
        var increment = new Button { Content = "+", MinWidth = 28, Margin = new Thickness(4, 0, 0, 0) };
        var row = new Grid();
        bool mouse = field.Tab == EmulationMachineTab.Mouse;
        if (mouse)
        {
            slider.MaxWidth = 260;
            row.HorizontalAlignment = HorizontalAlignment.Left;
        }
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = mouse ? GridLength.Auto : new GridLength(1, GridUnitType.Star) });
        for (int i = 0; i < (mouse ? 2 : 3); i++) row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.Children.Add(slider); Grid.SetColumn(input, 1); row.Children.Add(input);
        if (mouse)
        {
            slider.Width = 260;
            increment.MinHeight = decrement.MinHeight = 0;
            increment.Padding = decrement.Padding = new Thickness(6, 0, 6, 0);
            var buttons = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Children = { increment, decrement } };
            Grid.SetColumn(buttons, 2);
            row.Children.Add(buttons);
        }
        else
        {
            Grid.SetColumn(decrement, 2); row.Children.Add(decrement);
            Grid.SetColumn(increment, 3); row.Children.Add(increment);
        }
        bool updating = false;
        double saved = slider.Value;
        row.Tag = (Func<string>)(() => slider.Value.ToString("0.00", CultureInfo.InvariantCulture));
        slider.ValueChanged += (_, _) =>
        {
            if (updating) return;
            updating = true;
            try { slider.Value = Normalize(slider.Value); input.Text = slider.Value.ToString("0.00", CultureInfo.CurrentCulture); }
            finally { updating = false; }
        };
        input.TextChanged += (_, _) =>
        {
            if (updating || !double.TryParse(input.CommittedValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)) return;
            updating = true;
            try { slider.Value = Normalize(number); }
            finally { updating = false; }
        };
        async Task SaveAsync()
        {
            input.Text = slider.Value.ToString("0.00", CultureInfo.CurrentCulture);
            if (saved == slider.Value) return;
            saved = slider.Value;
            if (changed is not null) await changed();
        }
        decrement.Click += async (_, _) => { slider.Value = Normalize(slider.Value - range.Step); await SaveAsync(); };
        increment.Click += async (_, _) => { slider.Value = Normalize(slider.Value + range.Step); await SaveAsync(); };
        input.LostKeyboardFocus += async (_, _) => await SaveAsync();
        input.PreviewKeyUp += async (_, args) =>
        {
            if (args.Key is System.Windows.Input.Key.Up or System.Windows.Input.Key.Down)
            {
                args.Handled = true; await SaveAsync();
            }
            else if (args.Key == System.Windows.Input.Key.Enter) { args.Handled = true; await SaveAsync(); }
        };
        slider.AddHandler(Thumb.DragCompletedEvent, new DragCompletedEventHandler(async (_, _) => await SaveAsync()));
        slider.PreviewMouseLeftButtonUp += async (_, _) => { if (!slider.IsMouseCaptureWithin) await SaveAsync(); };
        slider.PreviewKeyUp += async (_, _) => await SaveAsync();
        if (field.AutomaticValue is not { } automaticValue) return row;
        var automatic = new CheckBox { Content = LocExtension.Get("Visual.Automatic"), IsChecked = field.Value == automaticValue };
        var panel = new StackPanel(); panel.Children.Add(automatic); panel.Children.Add(row);
        panel.Tag = (Func<string>)(() => automatic.IsChecked == true ? automaticValue : slider.Value.ToString("0.00", CultureInfo.InvariantCulture));
        row.IsEnabled = automatic.IsChecked != true;
        automatic.Checked += (_, _) => row.IsEnabled = false;
        automatic.Unchecked += (_, _) => row.IsEnabled = true;
        automatic.Click += async (_, _) => { row.IsEnabled = automatic.IsChecked != true; if (changed is not null) await changed(); };
        return panel;
    }
}
