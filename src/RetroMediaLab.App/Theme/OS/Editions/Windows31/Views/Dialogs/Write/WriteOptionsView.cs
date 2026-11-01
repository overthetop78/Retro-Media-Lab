using RetroMediaLab.App.Theme.Common;
﻿using System.Windows;
using System.Windows.Controls;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.Views.Controls.Write;
using static RetroMediaLab.App.Theme.OS.Editions.Windows31.Views.WriteOptionsFields;

namespace RetroMediaLab.App.Theme.OS.Editions.Windows31.Views;

internal sealed class WriteOptionsView : UserControl, IDisposable
{
    private readonly ThemeBoundFields<WriteOptionField> fields;
    private bool disposed;

    internal WriteOptionsView(WriteAdvancedSection source)
    {
        fields = new(source, CellMargin);
        try
        {
            var form = new Grid { Margin = new Thickness(OuterMargin), MinWidth = MinimumWidth };
            form.ColumnDefinitions.Add(new ColumnDefinition());
            form.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(ValueWidth) });
            var row = 0;
            foreach (var option in WriteOptionsFields.Options)
            {
                form.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                var toggle = Toggle(option.Toggle);
                Add(form, toggle, row, 0);
                if (option.Value is { } value) Add(form, Value(value), row, 1);
                else Grid.SetColumnSpan(toggle, form.ColumnDefinitions.Count);
                row++;
            }
            form.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var diskEnabled = Toggle(WriteOptionField.DiskDefsEnabled);
            Add(form, diskEnabled, row++, 0);
            Grid.SetColumnSpan(diskEnabled, form.ColumnDefinitions.Count);
            form.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var disk = new DockPanel();
            var browse = fields.Action(WriteOptionField.BrowseDiskDefs);
            DockPanel.SetDock(browse, Dock.Right);
            disk.Children.Add(browse);
            disk.Children.Add(Value(WriteOptionField.DiskDefsValue));
            Add(form, disk, row++, 0);
            Grid.SetColumnSpan(disk, form.ColumnDefinitions.Count);
            form.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var expertLabel = new Label { Margin = new Thickness(CellMargin) };
            expertLabel.SetBinding(ContentControl.ContentProperty, LocExtension.CreateBinding(ExpertResource));
            Add(form, expertLabel, row++, 0);
            Grid.SetColumnSpan(expertLabel, form.ColumnDefinitions.Count);
            form.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var expert = Value(WriteOptionField.ExpertArguments);
            expertLabel.Target = expert;
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

    internal FrameworkElement Field(WriteOptionField field) => fields.Field(field);

    private CheckBox Toggle(WriteOptionField field) => fields.Toggle(field);
    private FrameworkElement Value(WriteOptionField field) => fields.Value(field);

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
