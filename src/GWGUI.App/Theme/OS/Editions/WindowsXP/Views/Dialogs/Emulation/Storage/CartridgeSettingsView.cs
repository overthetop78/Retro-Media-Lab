using System.Windows;
using System.Windows.Controls;
using GWGUI.App.Localization.Extensions;
using GWGUI.App.Theme.Common;
using GWGUI.App.Views.Dialogs.Emulation.Storage;
using GWGUI.Emulation;

namespace GWGUI.App.Theme.OS.Editions.WindowsXP.Views;

internal sealed class CartridgeSettingsView : BusinessDialogView
{
    private readonly StorageFieldsView form = new();

    internal CartridgeSettingsView(CartridgeSlotConfigurationDialog source, WindowsXPColorScheme scheme = WindowsXPColorScheme.Blue) : base(source, scheme)
    {
        try
        {
            var commands = ThemeDialogCompletion.CommandButtons(source);
            var save = Forward(commands.Accept);
            save.IsDefault = true;
            Footer(save, Command("Common.Cancel", Cancel));
            if (source.PresentationOption.Content is string caption && !string.IsNullOrWhiteSpace(caption))
                form.AddField(new EmulationSettingsField(nameof(source.PresentationOption), EmulationMachineTab.Storage,
                    string.Empty, string.Empty, EmulationSettingsEditor.Toggle, null), source.PresentationOption, caption);
            if (!string.IsNullOrWhiteSpace(source.PresentationHint))
                form.Children.Add(new TextBlock { Text = source.PresentationHint, TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 4, 0, 8) });
            foreach (var field in source.PresentationFields)
                if (source.PresentationControls.TryGetValue(field.Id, out var endpoint))
                    form.AddField(field, endpoint, LocExtension.GetForModule(source.PresentationModule ?? string.Empty, field.LabelResourceKey));
            Children.Add(new ScrollViewer { Content = form, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
        }
        catch { Dispose(); throw; }
    }

    protected override void Detach() => form.Dispose();
}
