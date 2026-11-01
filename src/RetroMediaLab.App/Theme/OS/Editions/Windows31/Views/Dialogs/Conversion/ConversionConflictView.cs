using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using RetroMediaLab.App.Views.Dialogs.Conversion;

namespace RetroMediaLab.App.Theme.OS.Editions.Windows31.Views;

internal sealed class ConversionConflictView : BusinessDialogView
{
    private readonly ListBox rows = new();
    private const double ChoiceWidth = 170;

    internal ConversionConflictView(ConversionConflictWindow source) : base(source)
    {
        try
        {
            Footer(Command("Common.Cancel", Cancel), Command("Common.Continue", source.RequestContinue, true));
            var heading = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };
            heading.Children.Add(Label("Conflict.Heading"));
            heading.Children.Add(Label("Conflict.Help"));
            var all = new WrapPanel { Margin = new Thickness(0, 8, 0, 0) };
            all.Children.Add(Command("Conflict.AllOverwrite", () => source.RequestApplyAll(ConversionConflictChoice.Overwrite)));
            all.Children.Add(Command("Conflict.AllSkip", () => source.RequestApplyAll(ConversionConflictChoice.Skip)));
            all.Children.Add(Command("Conflict.AllNumber", () => source.RequestApplyAll(ConversionConflictChoice.Number)));
            heading.Children.Add(all);
            SetDock(heading, Dock.Top);
            Children.Add(heading);
            var row = new FrameworkElementFactory(typeof(DockPanel));
            var choices = new FrameworkElementFactory(typeof(ComboBox));
            choices.SetValue(DockProperty, Dock.Right);
            choices.SetValue(FrameworkElement.WidthProperty, ChoiceWidth);
            choices.SetValue(FrameworkElement.MarginProperty, ActionMargin);
            choices.SetBinding(ItemsControl.ItemsSourceProperty, new Binding(nameof(ConversionConflictRow.Choices)));
            choices.SetValue(ItemsControl.DisplayMemberPathProperty, nameof(ConversionConflictOption.Label));
            choices.SetBinding(System.Windows.Controls.Primitives.Selector.SelectedItemProperty,
                new Binding(nameof(ConversionConflictRow.SelectedChoice)) { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
            row.AppendChild(choices);
            var name = new FrameworkElementFactory(typeof(TextBlock));
            name.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
            name.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
            name.SetBinding(TextBlock.TextProperty, new Binding(nameof(ConversionConflictRow.FileName)));
            name.SetBinding(ToolTipProperty, new Binding(nameof(ConversionConflictRow.Output) + "." + nameof(RetroMediaLab.MediaEngine.Images.Conversion.ConversionOutput.OutputPath)));
            row.AppendChild(name);
            rows.ItemTemplate = new DataTemplate(typeof(ConversionConflictRow)) { VisualTree = row };
            rows.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            rows.ItemsSource = source.Rows;
            Children.Add(rows);
        }
        catch { Dispose(); throw; }
    }
    protected override void Detach() => rows.ItemsSource = null;
}
