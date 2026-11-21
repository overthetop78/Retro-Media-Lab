using RetroMediaLab.App.Constants.Emulation;
using RetroMediaLab.App.Contracts.Emulation.Settings;
using RetroMediaLab.App.Views.Controls.Emulation.Options;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace RetroMediaLab.App.Functions.Views.Emulation.Settings;

internal static partial class EmulationSettingsLayout
{
    internal static Grid SettingsFields(int columns, params EmulationSettingsControlField[] fields) => SettingsFieldGrid(columns, fields);

    internal static Grid SettingsFields(int columns, params (string Label, FrameworkElement Control)[] fields) => SettingsFieldGrid(columns, fields.Select(field => new EmulationSettingsControlField(field.Label, field.Control)).ToArray());

    private static Grid SettingsFieldGrid(params EmulationSettingsControlField[] fields) => SettingsFieldGrid(1, fields);

    private static Grid SettingsFieldGrid(int columns, EmulationSettingsControlField[] fields, bool memoryPresentation = false)
    {
        var grid = new Grid { Margin = new Thickness(12, 6, 12, 10) };
        for (var column = 0; column < columns; column++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = memoryPresentation
                ? GridLength.Auto : new GridLength(EmulationHardwareSettingsConstants.FieldLabelWidth) });
            grid.ColumnDefinitions.Add(new ColumnDefinition());
        }
        var rowCount = (int)Math.Ceiling(fields.Length / (double)columns);
        for (var row = 0; row < rowCount; row++) grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        for (var index = 0; index < fields.Length; index++)
        {
            var row = index / columns;
            var column = index % columns * 2;
            var field = fields[index];
            var label = new EmulationSettingsFieldLabel(field.Label, field.Explanation,
                field.DetailedExplanation, field.Control)
            {
                Margin = new Thickness(column == 0 ? 0 : 18, 8, 10, 8)
            };
            label.SetBinding(UIElement.VisibilityProperty, new Binding(nameof(UIElement.Visibility))
            {
                Source = field.Control,
                Mode = BindingMode.OneWay
            });
            Grid.SetRow(label, row);
            Grid.SetColumn(label, column);
            grid.Children.Add(label);
            var control = field.Control;
            FrameworkElement value = control;
            if (memoryPresentation && control is TextBlock)
            {
                var frame = new Border
                {
                    Child = control, BorderThickness = new Thickness(1),
                    Padding = new Thickness(10, 5, 10, 5), HorizontalAlignment = HorizontalAlignment.Left
                };
                frame.SetResourceReference(Border.BorderBrushProperty, "BorderBrush");
                frame.SetResourceReference(Border.BackgroundProperty, "WindowBrush");
                frame.SetBinding(UIElement.VisibilityProperty, new Binding(nameof(UIElement.Visibility)) { Source = control });
                value = frame;
            }
            else
            {
                control.MinWidth = control is CheckBox ? 0 : EmulationHardwareSettingsConstants.FieldControlMinimumWidth;
            }
            value.Margin = new Thickness(0, 4, 0, 4);
            value.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetRow(value, row);
            Grid.SetColumn(value, column + 1);
            grid.Children.Add(value);
        }
        return grid;
    }
}
