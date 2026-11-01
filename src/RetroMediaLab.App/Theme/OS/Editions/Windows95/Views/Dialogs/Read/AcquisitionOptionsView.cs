using System.Windows;
using System.Windows.Controls;
using RetroMediaLab.App.Theme.Common;
using RetroMediaLab.App.Views.Controls.Read;

namespace RetroMediaLab.App.Theme.OS.Editions.Windows95.Views;

internal sealed class AcquisitionOptionsView : UserControl, IDisposable
{
    private readonly ThemeBoundFields<AcquisitionOptionField> fields;
    private bool disposed;

    internal AcquisitionOptionsView(ReadAdvancedSection source)
    {
        fields = new(source, AcquisitionOptionsFields.CellMargin);
        try
        {
            var tabs = Windows95PropertyPages.Initialize(this, AcquisitionOptionsFields.MinimumWidth, AcquisitionOptionsFields.MaximumHeight);
            var general = Windows95PropertyPages.Page(tabs, Windows95PropertyPages.GeneralResource);
            foreach (var option in AcquisitionOptionsFields.Capture)
                Windows95PropertyPages.Row(general, fields.Toggle(option.Enabled), option.Value is { } value ? fields.Value(value) : null);

            var numbering = Windows95PropertyPages.Page(tabs, Windows95PropertyPages.NumberingResource);
            Windows95PropertyPages.Row(numbering, fields.Toggle(AcquisitionOptionField.AutoNumber));
            Windows95PropertyPages.Row(numbering, fields.Choice(AcquisitionOptionField.SequenceKind));
            Windows95PropertyPages.Row(numbering, fields.Choice(AcquisitionOptionField.SequenceWidth));
            Windows95PropertyPages.Row(numbering, Windows95PropertyPages.Label(AcquisitionOptionsFields.NextResource), fields.Value(AcquisitionOptionField.SequenceValue));
            Windows95PropertyPages.Row(numbering, fields.Information(AcquisitionOptionField.NamePreview));

            var expert = Windows95PropertyPages.Page(tabs, Windows95PropertyPages.ExpertResource);
            Windows95PropertyPages.Row(expert, fields.Toggle(AcquisitionOptionField.DiskDefsEnabled));
            var disk = new DockPanel();
            var browse = fields.Action(AcquisitionOptionField.BrowseDiskDefs);
            DockPanel.SetDock(browse, Dock.Right);
            disk.Children.Add(browse);
            disk.Children.Add(fields.Value(AcquisitionOptionField.DiskDefsValue));
            Windows95PropertyPages.Row(expert, disk);
            Windows95PropertyPages.Row(expert, Windows95PropertyPages.Label(AcquisitionOptionsFields.ExpertResource));
            Windows95PropertyPages.Row(expert, fields.Value(AcquisitionOptionField.ExpertArguments));
        }
        catch { Dispose(); throw; }
    }

    internal FrameworkElement Field(AcquisitionOptionField field) => fields.Field(field);

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        try { fields.Dispose(); }
        finally { Content = null; Resources.MergedDictionaries.Clear(); }
    }
}
