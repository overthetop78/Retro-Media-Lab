using System.Windows;
using System.Windows.Controls;
using RetroMediaLab.App.Theme.Common;
using RetroMediaLab.App.Views.Controls.Write;

namespace RetroMediaLab.App.Theme.OS.Editions.WindowsXP.Views;

internal sealed class WriteOptionsView : UserControl, IDisposable
{
    private readonly ThemeBoundFields<WriteOptionField> fields;
    private bool disposed;
    private WindowsXPResources? resourceScope;

    internal WriteOptionsView(WriteAdvancedSection source, WindowsXPColorScheme scheme = WindowsXPColorScheme.Blue)
    {
        fields = new(source, WriteOptionsFields.CellMargin);
        try
        {
            resourceScope = new WindowsXPResources(this, scheme);
            var tabs = WindowsXPPropertyPages.Initialize(this, WriteOptionsFields.MinimumWidth, WriteOptionsFields.MaximumHeight);
            var general = WindowsXPPropertyPages.Page(tabs, WindowsXPPropertyPages.GeneralResource);
            foreach (var option in WriteOptionsFields.Options)
                WindowsXPPropertyPages.Row(general, fields.Toggle(option.Toggle), option.Value is { } value ? fields.Value(value) : null);

            var expert = WindowsXPPropertyPages.Page(tabs, WindowsXPPropertyPages.ExpertResource);
            WindowsXPPropertyPages.Row(expert, fields.Toggle(WriteOptionField.DiskDefsEnabled));
            var disk = new DockPanel();
            var browse = fields.Action(WriteOptionField.BrowseDiskDefs);
            DockPanel.SetDock(browse, Dock.Right);
            disk.Children.Add(browse);
            disk.Children.Add(fields.Value(WriteOptionField.DiskDefsValue));
            WindowsXPPropertyPages.Row(expert, disk);
            WindowsXPPropertyPages.Row(expert, WindowsXPPropertyPages.Label(WriteOptionsFields.ExpertResource));
            WindowsXPPropertyPages.Row(expert, fields.Value(WriteOptionField.ExpertArguments));
        }
        catch { Dispose(); throw; }
    }

    internal FrameworkElement Field(WriteOptionField field) => fields.Field(field);

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        try { fields.Dispose(); }
        finally { try { resourceScope?.Dispose(); } finally { resourceScope = null; Content = null; } }
    }
}
