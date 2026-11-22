using System.Windows;
using System.Windows.Controls;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.Theme.Common;
using RetroMediaLab.App.Views.Dialogs.Emulation.Storage;
using RetroMediaLab.Emulation;

namespace RetroMediaLab.App.Theme.OS.Editions.Windows31.Views;

internal sealed class CartridgeSettingsView : BusinessDialogView
{
    private readonly StorageFieldsView form = new();
    private readonly ContentControl optionFields = new() { HorizontalContentAlignment = HorizontalAlignment.Stretch };
    private readonly CartridgeSlotConfigurationDialog source;

    internal CartridgeSettingsView(CartridgeSlotConfigurationDialog source) : base(source)
    {
        this.source = source;
        try
        {
            var commands = ThemeDialogCompletion.CommandButtons(source);
            var save = Forward(commands.Accept);
            save.IsDefault = true;
            Footer(Command("Common.Cancel", Cancel), save);
            if (source.PresentationOption.Content is string caption && !string.IsNullOrWhiteSpace(caption))
                form.AddField(new EmulationSettingsField(nameof(source.PresentationOption), EmulationMachineTab.Storage,
                    string.Empty, string.Empty, EmulationSettingsEditor.Toggle, null), source.PresentationOption, caption);
            if (!string.IsNullOrWhiteSpace(source.PresentationHint))
                form.Children.Add(new TextBlock { Text = source.PresentationHint, TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 4, 0, 8) });
            form.Children.Add(optionFields);
            source.PresentationFieldsChanged += RefreshOptions;
            RefreshOptions(source, EventArgs.Empty);
            Children.Add(new ScrollViewer { Content = form, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
        }
        catch { Dispose(); throw; }
    }

    private void RefreshOptions(object? sender, EventArgs args)
    {
        var updated = new StorageFieldsView();
        try
        {
            foreach (var field in source.PresentationFields)
                if (source.PresentationControls.TryGetValue(field.Id, out var endpoint))
                    updated.AddField(field, endpoint, LocExtension.GetForModule(source.PresentationModule ?? string.Empty, field.LabelResourceKey));
            if (optionFields.Content is StorageFieldsView previous) previous.Dispose();
            optionFields.Content = updated;
        }
        catch { updated.Dispose(); throw; }
    }

    protected override void Detach()
    {
        source.PresentationFieldsChanged -= RefreshOptions;
        try { (optionFields.Content as StorageFieldsView)?.Dispose(); }
        finally
        {
            optionFields.Content = null;
            form.Dispose();
        }
    }
}
