using System.Windows;
using System.Windows.Controls;
using RetroMediaLab.App.Theme.Common;
using RetroMediaLab.App.Views.Controls.Write;

namespace RetroMediaLab.App.Theme.Orbit.Views;

internal sealed class WriteOptionsView : UserControl, IDisposable
{
    private readonly ThemeBoundFields<WriteOptionField> fields;
    private bool disposed;
    private OrbitResources? resourceScope;

    internal WriteOptionsView(WriteAdvancedSection source, bool? dark = null)
    {
        fields = new(source, WriteOptionsFields.CellMargin);
        try
        {
            resourceScope = new OrbitResources(this, dark);
            var tabs = OrbitOptionPages.Initialize(this, WriteOptionsFields.MinimumWidth, WriteOptionsFields.MaximumHeight);
            var general = OrbitOptionPages.Page(tabs, OrbitOptionPages.GeneralResource);
            foreach (var option in WriteOptionsFields.Options)
                OrbitOptionPages.Row(general, fields.Toggle(option.Toggle), option.Value is { } value ? fields.Value(value) : null);

            var expert = OrbitOptionPages.Page(tabs, OrbitOptionPages.ExpertResource);
            OrbitOptionPages.Row(expert, fields.Toggle(WriteOptionField.DiskDefsEnabled));
            var disk = new DockPanel();
            var browse = fields.Action(WriteOptionField.BrowseDiskDefs);
            DockPanel.SetDock(browse, Dock.Right);
            disk.Children.Add(browse);
            disk.Children.Add(fields.Value(WriteOptionField.DiskDefsValue));
            OrbitOptionPages.Row(expert, disk);
            OrbitOptionPages.Row(expert, OrbitOptionPages.Label(WriteOptionsFields.ExpertResource));
            OrbitOptionPages.Row(expert, fields.Value(WriteOptionField.ExpertArguments));
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
