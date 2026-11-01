using System.Windows;
using System.Windows.Controls;
using GWGUI.App.Theme.Common;
using GWGUI.App.Views.Controls.Read;

namespace GWGUI.App.Theme.Orbit.Views;

internal sealed class AcquisitionOptionsView : UserControl, IDisposable
{
    private readonly ThemeBoundFields<AcquisitionOptionField> fields;
    private bool disposed;
    private OrbitResources? resourceScope;

    internal AcquisitionOptionsView(ReadAdvancedSection source, bool? dark = null)
    {
        fields = new(source, AcquisitionOptionsFields.CellMargin);
        try
        {
            resourceScope = new OrbitResources(this, dark);
            var tabs = OrbitOptionPages.Initialize(this, AcquisitionOptionsFields.MinimumWidth, AcquisitionOptionsFields.MaximumHeight);
            var general = OrbitOptionPages.Page(tabs, OrbitOptionPages.GeneralResource);
            foreach (var option in AcquisitionOptionsFields.Capture)
                OrbitOptionPages.Row(general, fields.Toggle(option.Enabled), option.Value is { } value ? fields.Value(value) : null);

            var numbering = OrbitOptionPages.Page(tabs, OrbitOptionPages.NumberingResource);
            OrbitOptionPages.Row(numbering, fields.Toggle(AcquisitionOptionField.AutoNumber));
            OrbitOptionPages.Row(numbering, fields.Choice(AcquisitionOptionField.SequenceKind));
            OrbitOptionPages.Row(numbering, fields.Choice(AcquisitionOptionField.SequenceWidth));
            OrbitOptionPages.Row(numbering, OrbitOptionPages.Label(AcquisitionOptionsFields.NextResource), fields.Value(AcquisitionOptionField.SequenceValue));
            OrbitOptionPages.Row(numbering, fields.Information(AcquisitionOptionField.NamePreview));

            var expert = OrbitOptionPages.Page(tabs, OrbitOptionPages.ExpertResource);
            OrbitOptionPages.Row(expert, fields.Toggle(AcquisitionOptionField.DiskDefsEnabled));
            var disk = new DockPanel();
            var browse = fields.Action(AcquisitionOptionField.BrowseDiskDefs);
            DockPanel.SetDock(browse, Dock.Right);
            disk.Children.Add(browse);
            disk.Children.Add(fields.Value(AcquisitionOptionField.DiskDefsValue));
            OrbitOptionPages.Row(expert, disk);
            OrbitOptionPages.Row(expert, OrbitOptionPages.Label(AcquisitionOptionsFields.ExpertResource));
            OrbitOptionPages.Row(expert, fields.Value(AcquisitionOptionField.ExpertArguments));
        }
        catch { Dispose(); throw; }
    }

    internal FrameworkElement Field(AcquisitionOptionField field) => fields.Field(field);

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        try { fields.Dispose(); }
        finally { try { resourceScope?.Dispose(); } finally { resourceScope = null; Content = null; } }
    }
}
