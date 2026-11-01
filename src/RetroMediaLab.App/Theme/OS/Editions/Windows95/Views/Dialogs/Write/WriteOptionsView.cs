using System.Windows;
using System.Windows.Controls;
using RetroMediaLab.App.Theme.Common;
using RetroMediaLab.App.Views.Controls.Write;

namespace RetroMediaLab.App.Theme.OS.Editions.Windows95.Views;

internal sealed class WriteOptionsView : UserControl, IDisposable
{
    private readonly ThemeBoundFields<WriteOptionField> fields;
    private bool disposed;

    internal WriteOptionsView(WriteAdvancedSection source)
    {
        fields = new(source, WriteOptionsFields.CellMargin);
        try
        {
            var tabs = Windows95PropertyPages.Initialize(this, WriteOptionsFields.MinimumWidth, WriteOptionsFields.MaximumHeight);
            var general = Windows95PropertyPages.Page(tabs, Windows95PropertyPages.GeneralResource);
            foreach (var option in WriteOptionsFields.Options)
                Windows95PropertyPages.Row(general, fields.Toggle(option.Toggle), option.Value is { } value ? fields.Value(value) : null);

            var expert = Windows95PropertyPages.Page(tabs, Windows95PropertyPages.ExpertResource);
            Windows95PropertyPages.Row(expert, fields.Toggle(WriteOptionField.DiskDefsEnabled));
            var disk = new DockPanel();
            var browse = fields.Action(WriteOptionField.BrowseDiskDefs);
            DockPanel.SetDock(browse, Dock.Right);
            disk.Children.Add(browse);
            disk.Children.Add(fields.Value(WriteOptionField.DiskDefsValue));
            Windows95PropertyPages.Row(expert, disk);
            Windows95PropertyPages.Row(expert, Windows95PropertyPages.Label(WriteOptionsFields.ExpertResource));
            Windows95PropertyPages.Row(expert, fields.Value(WriteOptionField.ExpertArguments));
        }
        catch { Dispose(); throw; }
    }

    internal FrameworkElement Field(WriteOptionField field) => fields.Field(field);

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        try { fields.Dispose(); }
        finally { Content = null; Resources.MergedDictionaries.Clear(); }
    }
}
