using System.Windows;
using System.Windows.Controls;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.Theme.Common;
using RetroMediaLab.App.Views.Dialogs.Emulation.Storage;
using RetroMediaLab.Emulation;

namespace RetroMediaLab.App.Theme.Orbit.Views;

internal sealed class CompactDiscSettingsView : BusinessDialogView
{
    private readonly StorageFieldsView form = new();

    internal CompactDiscSettingsView(CompactDiscDriveConfigurationDialog source, bool? dark = null) : base(source, dark)
    {
        try
        {
            var commands = ThemeDialogCompletion.CommandButtons(source);
            var save = Forward(commands.Accept);
            save.IsDefault = true;
            Footer(save, Command("Common.Cancel", Cancel));
            foreach (var entry in source.PresentationEntries)
            {
                var editor = entry.Control is CheckBox ? EmulationSettingsEditor.Toggle
                    : entry.Control is ComboBox ? EmulationSettingsEditor.Selection : EmulationSettingsEditor.Text;
                form.AddField(new EmulationSettingsField(entry.Label, EmulationMachineTab.Storage,
                    string.Empty, string.Empty, editor, null), entry.Control, entry.Label);
            }
            foreach (var field in source.PresentationFields)
                if (source.PresentationControls.TryGetValue(field.Id, out var endpoint))
                    form.AddField(field, endpoint, LocExtension.GetForModule(source.PresentationModule ?? string.Empty, field.LabelResourceKey));
            form.Children.Add(Label(RetroMediaLab.App.Constants.Localization.EmulationResourceKeys.StorageRuntimeHint));
            Children.Add(new ScrollViewer { Content = form, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
        }
        catch { Dispose(); throw; }
    }

    protected override void Detach() => form.Dispose();
}
