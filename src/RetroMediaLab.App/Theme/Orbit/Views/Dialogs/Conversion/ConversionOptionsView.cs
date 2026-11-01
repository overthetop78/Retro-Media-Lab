using System.Windows;
using System.Windows.Controls;
using RetroMediaLab.App.Theme.Common;
using RetroMediaLab.App.Views.Controls.Conversion;

namespace RetroMediaLab.App.Theme.Orbit.Views;

internal sealed class ConversionOptionsView : UserControl, IDisposable
{
    private readonly ThemeBoundFields<ConversionOptionField> fields;
    private bool disposed;
    private OrbitResources? resourceScope;

    internal ConversionOptionsView(ConversionAdvancedSection source, bool? dark = null)
    {
        fields = new(source, ConversionOptionsFields.CellMargin);
        try
        {
            resourceScope = new OrbitResources(this, dark);
            var tabs = OrbitOptionPages.Initialize(this, ConversionOptionsFields.MinimumWidth, ConversionOptionsFields.MaximumHeight);
            var general = OrbitOptionPages.Page(tabs, OrbitOptionPages.GeneralResource);
            foreach (var option in ConversionOptionsFields.Rows)
                OrbitOptionPages.Row(general, fields.Toggle(option.Toggle), option.Value is { } value ? fields.Value(value) : null);

            var expert = OrbitOptionPages.Page(tabs, OrbitOptionPages.ExpertResource);
            OrbitOptionPages.Row(expert, fields.Toggle(ConversionOptionField.DiskDefsEnabled));
            var disk = new DockPanel();
            var browse = fields.Action(ConversionOptionField.BrowseDiskDefs);
            DockPanel.SetDock(browse, Dock.Right);
            disk.Children.Add(browse);
            disk.Children.Add(fields.Value(ConversionOptionField.DiskDefsValue));
            OrbitOptionPages.Row(expert, disk);
            OrbitOptionPages.Row(expert, OrbitOptionPages.Label(ConversionOptionsFields.ExpertResource));
            OrbitOptionPages.Row(expert, fields.Value(ConversionOptionField.ExpertArguments));
        }
        catch { Dispose(); throw; }
    }

    internal FrameworkElement Field(ConversionOptionField field) => fields.Field(field);

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        try { fields.Dispose(); }
        finally { try { resourceScope?.Dispose(); } finally { resourceScope = null; Content = null; } }
    }
}
