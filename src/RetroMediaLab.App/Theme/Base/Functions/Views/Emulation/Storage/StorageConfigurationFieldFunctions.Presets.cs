using RetroMediaLab.App.Functions.Emulation.Settings;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using RetroMediaLab.App.Contracts.Emulation.Settings;
using RetroMediaLab.Emulation.Contracts;
using C = RetroMediaLab.App.Constants.Emulation.StorageConfigurationLayoutConstants;

namespace RetroMediaLab.App.Functions.Views.Emulation.Storage;

internal static partial class StorageConfigurationFieldFunctions
{
    /// <summary>Présélections numériques fournies par le module ; aucune valeur métier n'est définie dans App.</summary>
    private static FrameworkElement CreatePresets(EmulationSettingsField field, string? moduleId)
    {
        var range = field.NumericRange!;
        var (choices, selectedIndex) = EmulationSettingsValuePresentationFunctions.NumericPresets(field, choice => ChoiceLabel(choice, field, moduleId));
        if (range.IsVertical)
        {
            var verticalSlider = new Slider { Minimum = 0, Maximum = choices.Count - 1, Value = selectedIndex,
                TickFrequency = 1, SmallChange = 1, LargeChange = 1, IsSnapToTickEnabled = true, TickPlacement = TickPlacement.Both,
                Orientation = Orientation.Vertical, Height = C.VerticalSliderHeight, Width = 30, HorizontalAlignment = HorizontalAlignment.Center };
            var valueLabel = new TextBlock { TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 6, 0, 0) };
            void Update() => valueLabel.Text = choices[(int)Math.Round(verticalSlider.Value)].DisplayName;
            verticalSlider.ValueChanged += (_, _) => Update();
            Update();
            var column = new StackPanel { Tag = (Func<string>)(() => choices[(int)Math.Round(verticalSlider.Value)].Choice.Id) };
            column.Children.Add(verticalSlider); column.Children.Add(valueLabel);
            return column;
        }
        var slider = new Slider
        {
            Minimum = 0,
            Maximum = choices.Count - 1,
            Value = selectedIndex,
            SmallChange = 1,
            LargeChange = 1,
            TickFrequency = 1,
            IsSnapToTickEnabled = true,
            TickPlacement = TickPlacement.BottomRight,
            MinWidth = C.SliderMinimumWidth,
            MaxWidth = C.SliderMaximumWidth,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        var horizontalValueLabel = new TextBlock { TextAlignment = TextAlignment.Right, MinWidth = C.PresetValueWidth, VerticalAlignment = VerticalAlignment.Center };
        void UpdateValue() => horizontalValueLabel.Text = choices[(int)Math.Round(slider.Value)].DisplayName;
        slider.ValueChanged += (_, _) => UpdateValue();
        UpdateValue();
        var row = new Grid { Tag = (Func<string>)(() => choices[(int)Math.Round(slider.Value)].Choice.Id) };
        row.ColumnDefinitions.Add(new ColumnDefinition());
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.Children.Add(slider);
        Grid.SetColumn(horizontalValueLabel, 1);
        row.Children.Add(horizontalValueLabel);
        return row;
    }
}
