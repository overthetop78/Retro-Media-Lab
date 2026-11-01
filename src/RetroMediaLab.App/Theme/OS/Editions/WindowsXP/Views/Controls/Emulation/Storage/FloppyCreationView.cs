using System.Windows;
using System.Windows.Controls;
using RetroMediaLab.App.Constants.Emulation;
using RetroMediaLab.App.Contracts.Storage;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.Theme.Common;
using RetroMediaLab.App.Views.Controls.Emulation.Storage;
using RetroMediaLab.Emulation;

namespace RetroMediaLab.App.Theme.OS.Editions.WindowsXP.Views;

internal sealed class FloppyCreationView : StackPanel, IDisposable
{
    private readonly ThemeBindingScope bindings = new();
    private readonly StorageFieldsView form = new();
    private readonly SettingsFieldView destination;
    private WindowsXPResources? resourceScope;

    internal FloppyCreationView(FloppyCreationSettingsView source, WindowsXPColorScheme scheme = WindowsXPColorScheme.Blue)
    {
        destination = new SettingsFieldView(Definition(nameof(source.PresentationDestination), EmulationSettingsEditor.Path),
            source.PresentationDestination, () => Task.CompletedTask);
        try
        {
            resourceScope = new WindowsXPResources(this, scheme);
            bindings.OneWay(source, IsEnabledProperty, this, IsEnabledProperty);
            form.AddField(Definition(nameof(source.PresentationDisk), EmulationSettingsEditor.Selection), source.PresentationDisk,
                LocExtension.Get(MediaCreationConstants.FormatLabel), nameof(StorageDialogChoice.Text));
            form.AddField(Definition(nameof(source.PresentationImage), EmulationSettingsEditor.Selection), source.PresentationImage,
                LocExtension.Get(MediaCreationConstants.ImageFormatLabel), nameof(FloppyCreationFormatChoice.DisplayName));
            Children.Add(form);
            var label = new TextBlock { Text = LocExtension.Get(MediaCreationConstants.DestinationLabel),
                TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 5) };
            Children.Add(label);
            var row = new DockPanel();
            var browse = new Button { Margin = new Thickness(8, 0, 0, 0) };
            bindings.Action(source.PresentationBrowse, browse, followCaption: true);
            DockPanel.SetDock(browse, Dock.Right);
            row.Children.Add(browse);
            row.Children.Add(destination);
            Children.Add(row);
        }
        catch { Dispose(); throw; }
    }

    private static EmulationSettingsField Definition(string id, EmulationSettingsEditor editor) =>
        new(id, EmulationMachineTab.Storage, string.Empty, string.Empty, editor, null);

    public void Dispose()
    {
        try { bindings.Dispose(); }
        finally
        {
            try { form.Dispose(); }
            finally
            {
                try { destination.Dispose(); }
                finally { try { resourceScope?.Dispose(); } finally { resourceScope = null; Children.Clear(); } }
            }
        }
    }
}
