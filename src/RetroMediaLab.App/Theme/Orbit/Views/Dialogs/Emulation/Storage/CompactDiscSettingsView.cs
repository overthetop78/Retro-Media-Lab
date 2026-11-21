using System.Windows;
using System.Windows.Controls;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.Theme.Common;
using RetroMediaLab.App.Views.Dialogs.Emulation.Storage;
using RetroMediaLab.Emulation;

namespace RetroMediaLab.App.Theme.Orbit.Views;

internal sealed class CompactDiscSettingsView : BusinessDialogView
{
    private readonly List<StorageFieldsView> forms = [];

    internal CompactDiscSettingsView(CompactDiscDriveConfigurationDialog source, bool? dark = null) : base(source, dark)
    {
        try
        {
            var commands = ThemeDialogCompletion.CommandButtons(source);
            var save = Forward(commands.Accept);
            save.IsDefault = true;
            Footer(save, Command("Common.Cancel", Cancel));
            var content = new StackPanel();
            var groups = source.PresentationBlocks;
            if (groups.Count == 0)
                content.Children.Add(BuildForm(source.PresentationFields, includeEntries: true));
            else
                for (var index = 0; index < groups.Count; index++)
                {
                    var block = groups[index];
                    content.Children.Add(new GroupBox
                    {
                        Header = LocExtension.GetForModule(source.PresentationModule ?? string.Empty, block.TitleResourceKey),
                        Content = BuildForm(block.Fields, includeEntries: index == 0),
                        Margin = new Thickness(0, 0, 0, 10)
                    });
                }
            content.Children.Add(Label(RetroMediaLab.App.Constants.Localization.EmulationResourceKeys.StorageRuntimeHint));
            Children.Add(new ScrollViewer { Content = content, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });

            StorageFieldsView BuildForm(IReadOnlyList<EmulationSettingsField> fields, bool includeEntries)
            {
                var form = new StorageFieldsView();
                forms.Add(form);
                if (includeEntries)
                    foreach (var entry in source.PresentationEntries)
                    {
                        var editor = entry.Control is CheckBox ? EmulationSettingsEditor.Toggle
                            : entry.Control is ComboBox ? EmulationSettingsEditor.Selection : EmulationSettingsEditor.Text;
                        form.AddField(new EmulationSettingsField(entry.Label, EmulationMachineTab.Storage,
                            string.Empty, string.Empty, editor, null), entry.Control, entry.Label);
                    }
                foreach (var field in fields)
                    if (source.PresentationControls.TryGetValue(field.Id, out var endpoint))
                        form.AddField(field, endpoint, LocExtension.GetForModule(source.PresentationModule ?? string.Empty, field.LabelResourceKey));
                return form;
            }
        }
        catch { Dispose(); throw; }
    }

    protected override void Detach()
    {
        try
        {
            foreach (var form in forms) form.Dispose();
        }
        finally { forms.Clear(); }
    }
}
