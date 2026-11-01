using System.Windows;
using System.Windows.Controls;
using RetroMediaLab.App.Theme.Common;
using RetroMediaLab.App.Views.Controls.Read;

namespace RetroMediaLab.App.Theme.OS.Editions.WindowsXP.Views;

internal sealed class AcquisitionOptionsView : UserControl, IDisposable
{
    private readonly ThemeBoundFields<AcquisitionOptionField> fields;
    private bool disposed;
    private WindowsXPResources? resourceScope;

    internal AcquisitionOptionsView(ReadAdvancedSection source, WindowsXPColorScheme scheme = WindowsXPColorScheme.Blue)
    {
        fields = new(source, AcquisitionOptionsFields.CellMargin);
        try
        {
            resourceScope = new WindowsXPResources(this, scheme);
            var tabs = WindowsXPPropertyPages.Initialize(this, AcquisitionOptionsFields.MinimumWidth, AcquisitionOptionsFields.MaximumHeight);
            var general = WindowsXPPropertyPages.Page(tabs, WindowsXPPropertyPages.GeneralResource);
            foreach (var option in AcquisitionOptionsFields.Capture)
                WindowsXPPropertyPages.Row(general, fields.Toggle(option.Enabled), option.Value is { } value ? fields.Value(value) : null);

            var numbering = WindowsXPPropertyPages.Page(tabs, WindowsXPPropertyPages.NumberingResource);
            WindowsXPPropertyPages.Row(numbering, fields.Toggle(AcquisitionOptionField.AutoNumber));
            WindowsXPPropertyPages.Row(numbering, fields.Choice(AcquisitionOptionField.SequenceKind));
            WindowsXPPropertyPages.Row(numbering, fields.Choice(AcquisitionOptionField.SequenceWidth));
            WindowsXPPropertyPages.Row(numbering, WindowsXPPropertyPages.Label(AcquisitionOptionsFields.NextResource), fields.Value(AcquisitionOptionField.SequenceValue));
            WindowsXPPropertyPages.Row(numbering, fields.Information(AcquisitionOptionField.NamePreview));

            var expert = WindowsXPPropertyPages.Page(tabs, WindowsXPPropertyPages.ExpertResource);
            WindowsXPPropertyPages.Row(expert, fields.Toggle(AcquisitionOptionField.DiskDefsEnabled));
            var disk = new DockPanel();
            var browse = fields.Action(AcquisitionOptionField.BrowseDiskDefs);
            DockPanel.SetDock(browse, Dock.Right);
            disk.Children.Add(browse);
            disk.Children.Add(fields.Value(AcquisitionOptionField.DiskDefsValue));
            WindowsXPPropertyPages.Row(expert, disk);
            WindowsXPPropertyPages.Row(expert, WindowsXPPropertyPages.Label(AcquisitionOptionsFields.ExpertResource));
            WindowsXPPropertyPages.Row(expert, fields.Value(AcquisitionOptionField.ExpertArguments));
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
