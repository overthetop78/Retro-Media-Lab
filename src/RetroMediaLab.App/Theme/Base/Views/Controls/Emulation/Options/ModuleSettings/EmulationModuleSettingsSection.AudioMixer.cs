using System.Windows;
using System.Windows.Controls;
using RetroMediaLab.App.Constants.Controls.Visual;
using RetroMediaLab.App.Functions.Views.Emulation.Settings;
using RetroMediaLab.App.Functions.Emulation.Storage;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.Emulation;

namespace RetroMediaLab.App.Views.Controls.Emulation.Options;

internal sealed partial class EmulationModuleSettingsSection
{
    private FrameworkElement BuildAudioMixer(IReadOnlyList<EmulationSettingsBlock> channels)
    {
        var strip = new StackPanel { Orientation = Orientation.Horizontal };
        var storage = (_module as IEmulationStorageSettingsManager)?.DescribeStorageSettings(_configuration);
        foreach (var channel in channels)
        {
            var slot = channel.AudioMixerSlot!.Value;
            var column = new StackPanel { Width = 96, Margin = new Thickness(4) };
            var device = storage?.AvailableDevices.FirstOrDefault(device => device.Slot == slot);
            FrameworkElement icon = device is not null
                ? EmulationMediaIconFunctions.Create(EmulationMediaIconFunctions.Select(
                    EmulationMediaIconFunctions.WithConfiguredModel(device, storage?.DeviceSettings)), 28)
                : new TextBlock { Text = IconGlyphs.Audio, FontFamily = ControlVisualConstants.IconFont, FontSize = 24 };
            icon.HorizontalAlignment = HorizontalAlignment.Center;
            icon.Margin = new Thickness(0, 4, 0, 8);
            column.Children.Add(icon);
            column.Children.Add(new TextBlock
            {
                Text = slot.ToString(), HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            });
            foreach (var field in channel.Fields.Where(field => IsFieldDisplayed(EmulationMachineTab.Audio, field)))
            {
                var control = CreateField(field);
                if (control.Tag is Slider volume) { volume.IsDirectionReversed = false; volume.Height = 96; }
                control.HorizontalAlignment = HorizontalAlignment.Center;
                control.Margin = new Thickness(4, 6, 4, 6);
                control.ToolTip = LocExtension.GetForModule(_module, field.LabelResourceKey);
                if (control is CheckBox mute)
                {
                    mute.Content = new TextBlock
                    {
                        Text = IconGlyphs.Muted, FontFamily = ControlVisualConstants.IconFont, FontSize = 18
                    };
                    System.Windows.Automation.AutomationProperties.SetName(mute,
                        LocExtension.GetForModule(_module, field.LabelResourceKey));
                }
                column.Children.Add(control);
            }
            var separator = new Border
            {
                BorderThickness = new Thickness(0, 0, 1, 0),
                Child = column
            };
            separator.SetResourceReference(Border.BorderBrushProperty, "BorderBrush");
            strip.Children.Add(separator);
        }
        return EmulationSettingsLayout.IconCard(new ScrollViewer
        {
            Content = strip, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled
        }, LocExtension.Get(RetroMediaLab.Emulation.Constants.EmulationMechanicalSoundConstants.MixerResource), IconGlyphs.Audio);
    }
}
