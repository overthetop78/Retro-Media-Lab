using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using GWGUI.App.Views.Windows.Tools;

namespace GWGUI.App.Theme.OS.Editions.Windows95.Views;

internal sealed class ToolView : BusinessDialogView
{
    private static readonly Thickness FieldMargin = new(0, 3, 12, 3);
    private static readonly Thickness SectionMargin = new(0, 0, 0, 8);
    private const double LabelWidth = 180;
    private const double ValueWidth = 150;
    private const double ParametersMaximumHeight = 230;

    internal ToolView(GwToolWindow source) : base(source)
    {
        try
        {
            Footer(Command("Common.Close", source.RequestPresentationClose), Forward(source.PresentationExecute));
            var layout = new Grid();
            layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            layout.RowDefinitions.Add(new RowDefinition());
            layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var parameters = new Grid { Margin = SectionMargin };
            parameters.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(LabelWidth) });
            parameters.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(ValueWidth) });
            foreach (var field in source.PresentationFields)
            {
                var row = parameters.RowDefinitions.Count;
                parameters.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                if (source.PresentationChecks.TryGetValue(field.Key, out var optional))
                    Add(parameters, Copy(optional), row, 0);
                else
                    Add(parameters, new TextBlock { Text = AutomationProperties.GetName(field.Value), TextWrapping = TextWrapping.Wrap }, row, 0);
                var input = new TextBox { MinWidth = ValueWidth, Margin = new Thickness(0, 3, 0, 3) };
                AutomationProperties.SetName(input, AutomationProperties.GetName(field.Value));
                Bindings.TwoWay(field.Value, TextBox.TextProperty, input, TextBox.TextProperty);
                Add(parameters, input, row, 1);
            }
            foreach (var check in source.PresentationChecks.Where(item => !source.PresentationFields.ContainsKey(item.Key)))
            {
                var row = parameters.RowDefinitions.Count;
                parameters.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                var input = Copy(check.Value);
                Grid.SetColumnSpan(input, 2);
                Add(parameters, input, row, 0);
            }
            if (parameters.RowDefinitions.Count > 0)
            {
                var scroll = new ScrollViewer
                {
                    Content = parameters,
                    MaxHeight = ParametersMaximumHeight,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
                };
                layout.Children.Add(scroll);
            }

            var summary = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = SectionMargin };
            Bindings.OneWay(source.PresentationSummary, TextBlock.TextProperty, summary, TextBlock.TextProperty);
            Grid.SetRow(summary, 1);
            layout.Children.Add(summary);

            var output = FixedText(source.PresentationOutput, "Tool.RawOutput");
            var results = new GroupBox { Header = Label("Tool.RawOutput"), Content = output, Margin = SectionMargin };
            Grid.SetRow(results, 2);
            layout.Children.Add(results);

            var command = FixedText(source.PresentationCommand, "Common.Command");
            command.MaxHeight = 72;
            Grid.SetRow(command, 3);
            layout.Children.Add(command);
            Children.Add(layout);
        }
        catch { Dispose(); throw; }
    }

    protected override void Cancel() => ((GwToolWindow)Source).RequestPresentationClose();

    private CheckBox Copy(CheckBox original)
    {
        var check = new CheckBox { Margin = FieldMargin };
        Bindings.OneWay(original, ContentControl.ContentProperty, check, ContentControl.ContentProperty);
        Bindings.TwoWay(original, ToggleButton.IsCheckedProperty, check, ToggleButton.IsCheckedProperty);
        Bindings.OneWay(original, UIElement.IsEnabledProperty, check, UIElement.IsEnabledProperty);
        return check;
    }

    private TextBox FixedText(TextBox original, string resource)
    {
        var text = new TextBox
        {
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.NoWrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            FontSize = Windows95Metrics.FontSize
        };
        text.FontFamily = new System.Windows.Media.FontFamily("Courier New");
        text.SetBinding(AutomationProperties.NameProperty, GWGUI.App.Localization.Extensions.LocExtension.CreateBinding(resource));
        Bindings.OneWay(original, TextBox.TextProperty, text, TextBox.TextProperty);
        TextChangedEventHandler scroll = (_, _) => text.ScrollToEnd();
        text.TextChanged += scroll;
        scrolling.Add((text, scroll));
        return text;
    }

    private readonly List<(TextBox Text, TextChangedEventHandler Handler)> scrolling = [];

    protected override void Detach()
    {
        foreach (var item in scrolling) item.Text.TextChanged -= item.Handler;
        scrolling.Clear();
    }

    private static void Add(Grid grid, FrameworkElement field, int row, int column)
    {
        field.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetRow(field, row);
        Grid.SetColumn(field, column);
        grid.Children.Add(field);
    }
}
