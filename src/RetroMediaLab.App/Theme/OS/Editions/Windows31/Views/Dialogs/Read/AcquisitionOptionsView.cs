using RetroMediaLab.App.Theme.Common;
﻿using System.Windows;
using System.Windows.Controls;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.Views.Controls.Read;
using static RetroMediaLab.App.Theme.OS.Editions.Windows31.Views.AcquisitionOptionsFields;

namespace RetroMediaLab.App.Theme.OS.Editions.Windows31.Views;

/// <summary>Independent acquisition settings; borrows functional endpoints, never their visual containers.</summary>
internal sealed class AcquisitionOptionsView : UserControl, IDisposable
{
    private readonly ThemeBoundFields<AcquisitionOptionField> fields;
    private bool disposed;

    internal AcquisitionOptionsView(ReadAdvancedSection source)
    {
        fields = new(source, CellMargin);
        try
        {
            var form = new Grid { Margin = new Thickness(OuterMargin), MinWidth = MinimumWidth };
            form.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            form.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(ValueWidth) });
            form.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(ColumnGap) });
            form.ColumnDefinitions.Add(new ColumnDefinition());
            var row = 0;
            foreach (var option in Capture)
            {
                form.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                Add(form, Toggle(option.Enabled), row, CaptionColumn);
                if (option.Value is { } value) Add(form, Value(value), row, ValueColumn);
                row++;
            }
            var numbering = new StackPanel { Margin = new Thickness(ColumnGap, CellMargin, CellMargin, CellMargin) };
            Grid.SetColumn(numbering, NumberingColumn);
            Grid.SetColumnSpan(numbering, NumberingSpan);
            Grid.SetRowSpan(numbering, row);
            form.Children.Add(numbering);
            numbering.Children.Add(Toggle(AcquisitionOptionField.AutoNumber));
            numbering.Children.Add(Choice(AcquisitionOptionField.SequenceKind));
            numbering.Children.Add(Choice(AcquisitionOptionField.SequenceWidth));
            numbering.Children.Add(Caption(NextResource));
            numbering.Children.Add(Value(AcquisitionOptionField.SequenceValue));
            var preview = fields.Information(AcquisitionOptionField.NamePreview);
            numbering.Children.Add(preview);
            form.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Add(form, Toggle(AcquisitionOptionField.DiskDefsEnabled), row, CaptionColumn);
            var disk = new DockPanel();
            var browse = fields.Action(AcquisitionOptionField.BrowseDiskDefs);
            DockPanel.SetDock(browse, Dock.Right);
            disk.Children.Add(browse);
            disk.Children.Add(Value(AcquisitionOptionField.DiskDefsValue));
            Add(form, disk, row++, ValueColumn);
            Grid.SetColumnSpan(disk, form.ColumnDefinitions.Count - ValueColumn);
            form.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Add(form, Caption(ExpertResource), row++, CaptionColumn);
            form.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var expert = Value(AcquisitionOptionField.ExpertArguments);
            Add(form, expert, row, CaptionColumn);
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

    internal FrameworkElement Field(AcquisitionOptionField field) => fields.Field(field);

    private CheckBox Toggle(AcquisitionOptionField field) => fields.Toggle(field);
    private FrameworkElement Value(AcquisitionOptionField field) => fields.Value(field);
    private ComboBox Choice(AcquisitionOptionField field) => fields.Choice(field);

    private static TextBlock Caption(string resource)
    {
        var caption = new TextBlock { Margin = new Thickness(CellMargin), TextWrapping = TextWrapping.Wrap };
        caption.SetBinding(TextBlock.TextProperty, LocExtension.CreateBinding(resource));
        return caption;
    }

    private static void Add(Grid grid, FrameworkElement element, int row, int column)
    {
        Grid.SetRow(element, row);
        Grid.SetColumn(element, column);
        grid.Children.Add(element);
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
