using RetroMediaLab.App.Theme.Common;
using System.Windows;
using System.Windows.Controls;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.Views.Controls.Conversion;
using static RetroMediaLab.App.Theme.OS.Editions.Windows31.Views.ConversionOptionsFields;

namespace RetroMediaLab.App.Theme.OS.Editions.Windows31.Views;

internal sealed class ConversionOptionsView : UserControl, IDisposable
{
    private readonly ThemeBoundFields<ConversionOptionField> fields;
    private bool disposed;

    internal ConversionOptionsView(ConversionAdvancedSection source)
    {
        fields = new(source, CellMargin);
        try
        {
            var form = new Grid { Margin = new Thickness(OuterMargin), MinWidth = MinimumWidth };
            form.ColumnDefinitions.Add(new ColumnDefinition());
            form.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(ValueWidth) });
            var row = 0;
            foreach (var option in Rows)
            {
                form.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                var toggle = fields.Toggle(option.Toggle);
                Add(form, toggle, row, 0);
                if (option.Value is { } value) Add(form, fields.Value(value), row, 1);
                else Grid.SetColumnSpan(toggle, form.ColumnDefinitions.Count);
                row++;
            }
            form.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var diskEnabled = fields.Toggle(ConversionOptionField.DiskDefsEnabled);
            Add(form, diskEnabled, row++, 0);
            Grid.SetColumnSpan(diskEnabled, form.ColumnDefinitions.Count);
            form.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var disk = new DockPanel();
            var browse = fields.Action(ConversionOptionField.BrowseDiskDefs);
            DockPanel.SetDock(browse, Dock.Right);
            disk.Children.Add(browse);
            disk.Children.Add(fields.Value(ConversionOptionField.DiskDefsValue));
            Add(form, disk, row++, 0);
            Grid.SetColumnSpan(disk, form.ColumnDefinitions.Count);
            form.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var label = new Label { Margin = new Thickness(CellMargin) };
            label.SetBinding(ContentControl.ContentProperty, LocExtension.CreateBinding(ExpertResource));
            Add(form, label, row++, 0);
            Grid.SetColumnSpan(label, form.ColumnDefinitions.Count);
            form.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var expert = fields.Value(ConversionOptionField.ExpertArguments);
            label.Target = expert;
            Add(form, expert, row, 0);
            Grid.SetColumnSpan(expert, form.ColumnDefinitions.Count);
            Content = new ScrollViewer
            {
                Content = form,
                MaxHeight = MaximumHeight,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
            };
        }
        catch { Dispose(); throw; }
    }

    internal FrameworkElement Field(ConversionOptionField field) => fields.Field(field);

    private static void Add(Grid form, FrameworkElement field, int row, int column)
    {
        Grid.SetRow(field, row);
        Grid.SetColumn(field, column);
        form.Children.Add(field);
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        try { fields.Dispose(); }
        finally
        {
            if (Content is ScrollViewer scroll) scroll.Content = null;
            Content = null;
        }
    }
}
