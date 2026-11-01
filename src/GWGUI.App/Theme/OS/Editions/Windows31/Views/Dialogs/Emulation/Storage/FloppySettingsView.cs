using System.Windows;
using System.Windows.Controls;
using GWGUI.App.Constants.Localization;
using GWGUI.App.Localization.Extensions;
using GWGUI.App.Theme.Common;
using GWGUI.App.Views.Dialogs.Emulation.Storage;
using GWGUI.Emulation;

namespace GWGUI.App.Theme.OS.Editions.Windows31.Views;

internal sealed class FloppySettingsView : BusinessDialogView
{
    private readonly List<StorageFieldsView> forms = [];

    internal FloppySettingsView(FloppyDriveConfigurationDialog source) : base(source)
    {
        try
        {
            var commands = ThemeDialogCompletion.CommandButtons(source);
            var save = Forward(commands.Accept);
            save.IsDefault = true;
            var cancel = Command("Common.Cancel", Cancel);
            if (source.PresentationCreate is { } create) Footer(Forward(create), cancel, save);
            else Footer(cancel, save);
            var body = new StackPanel();
            var drive = Form();
            if (source.PresentationOptions.Models.Count > 0)
                drive.AddField(Definition(nameof(source.PresentationModel), EmulationSettingsEditor.Selection),
                    source.PresentationModel, LocExtension.Get(EmulationResourceKeys.Model));
            if (source.PresentationOptions.CanConfigureBehavior)
            {
                drive.AddField(source.PresentationSpeedField, source.PresentationSpeed, LocExtension.Get("Emulation.Storage.Floppy.Speed"));
                if (source.PresentationOptions.CanConfigureWriteProtection)
                    drive.AddField(Definition(nameof(source.PresentationWriteProtection), EmulationSettingsEditor.Toggle),
                        source.PresentationWriteProtection, LocExtension.Get("Emulation.Storage.Floppy.WriteProtection"));
                if (source.PresentationOptions.CanRedirectWrites)
                    drive.AddField(Definition(nameof(source.PresentationRedirectWrites), EmulationSettingsEditor.Toggle),
                        source.PresentationRedirectWrites, LocExtension.Get("Emulation.Storage.Floppy.WriteRedirect"));
            }
            body.Children.Add(new GroupBox { Header = LocExtension.Get("Emulation.Storage.Floppy.Drive"), Content = drive,
                Margin = new Thickness(0, 0, 0, 8) });
            foreach (var group in source.PresentationFields.Where(field => field.IsVisible).GroupBy(field => field.BlockId))
            {
                var form = Form();
                foreach (var field in group)
                    if (source.PresentationControls.TryGetValue(field.Id, out var endpoint))
                        form.AddField(field, endpoint, LocExtension.GetForModule(source.PresentationModule ?? string.Empty, field.LabelResourceKey));
                var title = LocExtension.GetForModule(source.PresentationModule ?? string.Empty, group.Key);
                if (title == group.Key) title = LocExtension.Get("Emulation.Input.Behavior");
                body.Children.Add(new GroupBox { Header = title, Content = form, Margin = new Thickness(0, 0, 0, 8) });
            }
            Children.Add(new ScrollViewer { Content = body, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
        }
        catch { Dispose(); throw; }
    }

    private StorageFieldsView Form()
    {
        var form = new StorageFieldsView();
        forms.Add(form);
        return form;
    }

    private static EmulationSettingsField Definition(string id, EmulationSettingsEditor editor) =>
        new(id, EmulationMachineTab.Storage, string.Empty, string.Empty, editor, null);

    protected override void Detach()
    {
        Exception? failure = null;
        try
        {
            foreach (var form in forms)
                try { form.Dispose(); }
                catch (Exception error) { failure ??= error; }
        }
        finally { forms.Clear(); }
        if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
