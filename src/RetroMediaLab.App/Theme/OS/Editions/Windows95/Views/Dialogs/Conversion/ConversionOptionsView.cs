using System.Windows;
using System.Windows.Controls;
using RetroMediaLab.App.Theme.Common;
using RetroMediaLab.App.Views.Controls.Conversion;

namespace RetroMediaLab.App.Theme.OS.Editions.Windows95.Views;

internal sealed class ConversionOptionsView : UserControl, IDisposable
{
    private readonly ThemeBoundFields<ConversionOptionField> fields;
    private bool disposed;

    internal ConversionOptionsView(ConversionAdvancedSection source)
    {
        fields = new(source, ConversionOptionsFields.CellMargin);
        try
        {
            var tabs = Windows95PropertyPages.Initialize(this, ConversionOptionsFields.MinimumWidth, ConversionOptionsFields.MaximumHeight);
            var general = Windows95PropertyPages.Page(tabs, Windows95PropertyPages.GeneralResource);
            foreach (var option in ConversionOptionsFields.Rows)
                Windows95PropertyPages.Row(general, fields.Toggle(option.Toggle), option.Value is { } value ? fields.Value(value) : null);

            var expert = Windows95PropertyPages.Page(tabs, Windows95PropertyPages.ExpertResource);
            Windows95PropertyPages.Row(expert, fields.Toggle(ConversionOptionField.DiskDefsEnabled));
            var disk = new DockPanel();
            var browse = fields.Action(ConversionOptionField.BrowseDiskDefs);
            DockPanel.SetDock(browse, Dock.Right);
            disk.Children.Add(browse);
            disk.Children.Add(fields.Value(ConversionOptionField.DiskDefsValue));
            Windows95PropertyPages.Row(expert, disk);
            Windows95PropertyPages.Row(expert, Windows95PropertyPages.Label(ConversionOptionsFields.ExpertResource));
            Windows95PropertyPages.Row(expert, fields.Value(ConversionOptionField.ExpertArguments));
        }
        catch { Dispose(); throw; }
    }

    internal FrameworkElement Field(ConversionOptionField field) => fields.Field(field);

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        try { fields.Dispose(); }
        finally { Content = null; Resources.MergedDictionaries.Clear(); }
    }
}
