using System.Windows;
using System.Windows.Controls;
using GWGUI.App.Theme.Common;
using GWGUI.App.Views.Controls.Conversion;

namespace GWGUI.App.Theme.OS.Editions.WindowsXP.Views;

internal sealed class ConversionOptionsView : UserControl, IDisposable
{
    private readonly ThemeBoundFields<ConversionOptionField> fields;
    private bool disposed;
    private WindowsXPResources? resourceScope;

    internal ConversionOptionsView(ConversionAdvancedSection source, WindowsXPColorScheme scheme = WindowsXPColorScheme.Blue)
    {
        fields = new(source, ConversionOptionsFields.CellMargin);
        try
        {
            resourceScope = new WindowsXPResources(this, scheme);
            var tabs = WindowsXPPropertyPages.Initialize(this, ConversionOptionsFields.MinimumWidth, ConversionOptionsFields.MaximumHeight);
            var general = WindowsXPPropertyPages.Page(tabs, WindowsXPPropertyPages.GeneralResource);
            foreach (var option in ConversionOptionsFields.Rows)
                WindowsXPPropertyPages.Row(general, fields.Toggle(option.Toggle), option.Value is { } value ? fields.Value(value) : null);

            var expert = WindowsXPPropertyPages.Page(tabs, WindowsXPPropertyPages.ExpertResource);
            WindowsXPPropertyPages.Row(expert, fields.Toggle(ConversionOptionField.DiskDefsEnabled));
            var disk = new DockPanel();
            var browse = fields.Action(ConversionOptionField.BrowseDiskDefs);
            DockPanel.SetDock(browse, Dock.Right);
            disk.Children.Add(browse);
            disk.Children.Add(fields.Value(ConversionOptionField.DiskDefsValue));
            WindowsXPPropertyPages.Row(expert, disk);
            WindowsXPPropertyPages.Row(expert, WindowsXPPropertyPages.Label(ConversionOptionsFields.ExpertResource));
            WindowsXPPropertyPages.Row(expert, fields.Value(ConversionOptionField.ExpertArguments));
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
