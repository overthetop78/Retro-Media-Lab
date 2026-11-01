using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using GWGUI.App.Contracts.Emulation.Settings;
using GWGUI.App.Functions.Views.Emulation.Settings;
using GWGUI.App.Theme.Common;
using GWGUI.App.Views.Controls.Common;
using GWGUI.Emulation;

namespace GWGUI.App.Theme.OS.Editions.WindowsXP.Views;

/// <summary>Rebuilds a described field over its existing functional endpoint, without borrowing its visual tree.</summary>
internal sealed class SettingsFieldView : UserControl, IDisposable
{
    private readonly ThemeBindingScope bindings = new();
    private readonly List<Action> detach = [];
    private readonly EmulationSettingsField field;
    private readonly Func<Task> commit;
    private bool disposed;
    private readonly Action<Slider>? beginSliderEdit;
    private readonly Action<Slider>? endSliderEdit;
    private const double VerticalSliderHeight = 80;
    private const double SizeUnitWidth = 72;
    private const double VerticalSliderWidth = 23;
    private const double HorizontalSliderMaximumWidth = 200;
    private static readonly Thickness ActionGap = new(8, 0, 0, 0);
    private static readonly Thickness RadioGap = new(0, 2, 12, 2);

    internal SettingsFieldView(EmulationSettingsField field, FrameworkElement source, Func<Task> commit,
        Action<Slider>? beginSliderEdit = null, Action<Slider>? endSliderEdit = null)
    {
        this.field = field;
        this.commit = commit;
        this.beginSliderEdit = beginSliderEdit;
        this.endSliderEdit = endSliderEdit;
        try
        {
            bindings.OneWay(source, IsEnabledProperty, this, IsEnabledProperty);
            bindings.OneWay(source, VisibilityProperty, this, VisibilityProperty);
            Content = Create(source);
        }
        catch { Dispose(); throw; }
    }

    private FrameworkElement Create(FrameworkElement source) => source switch
    {
        TextBox input => Text(input),
        TextBlock information => Information(information),
        CheckBox toggle => Toggle(toggle),
        ComboBox selection => Selection(selection),
        Button action => Action(action),
        Grid { Tag: Slider slider } grid => Range(grid, slider),
        Panel radios when radios.Children.OfType<RadioButton>().Any() => Radios(radios),
        Panel automatic when automatic.Children.OfType<CheckBox>().Any()
            && automatic.Children.OfType<Grid>().Any(grid => grid.Tag is Slider) => AutomaticRange(automatic),
        Grid single when single.Children.OfType<ComboBox>().Any() && single.Children.OfType<TextBlock>().Any() => SingleChoice(single),
        Grid size when size.Children.OfType<TextBox>().Any() && size.Children.OfType<ComboBox>().Any() => SizeWithUnit(size),
        Grid path when path.Children.OfType<Button>().Any()
            && path.Children.OfType<FrameworkElement>().Any(child => child is TextBox or TextBlock) => Path(path),
        Slider slider => Range(null, slider),
        _ => throw new NotSupportedException(field.Id)
    };

    private FrameworkElement Text(TextBox original)
    {
        TextBox target = field.Editor switch
        {
            EmulationSettingsEditor.Number => EmulationSettingsFieldControlFunctions.CreateNumber(field with
                { Value = original is ConstrainedTextBox constrained ? constrained.CommittedValue : original.Text }),
            EmulationSettingsEditor.IPv4 => EmulationSettingsFieldControlFunctions.CreateIpv4(original.Text),
            _ => new TextBox()
        };
        target.AcceptsReturn = original.AcceptsReturn;
        target.MinLines = original.MinLines;
        target.MaxLines = original.MaxLines;
        target.MaxLength = original.MaxLength;
        target.TextWrapping = original.TextWrapping;
        target.VerticalScrollBarVisibility = original.VerticalScrollBarVisibility;
        target.FlowDirection = original.FlowDirection;
        Availability(original, target);
        bindings.TwoWay(original, TextBox.TextProperty, target, TextBox.TextProperty);
        bindings.OneWay(original, TextBoxBase.IsReadOnlyProperty, target, TextBoxBase.IsReadOnlyProperty);
        KeyboardFocusChangedEventHandler lostFocus = async (_, _) => { if (!disposed) await commit(); };
        target.LostKeyboardFocus += lostFocus;
        detach.Add(() => target.LostKeyboardFocus -= lostFocus);
        return target;
    }

    private TextBlock Information(TextBlock original)
    {
        var text = new TextBlock { TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center };
        bindings.OneWay(original, TextBlock.TextProperty, text, TextBlock.TextProperty);
        if (original.ToolTip is string tooltip) text.ToolTip = tooltip;
        return text;
    }

    private CheckBox Toggle(CheckBox original)
    {
        var target = new CheckBox { VerticalAlignment = VerticalAlignment.Center };
        if (original.Content is string caption) bindings.OneWay(original, ContentControl.ContentProperty, target, ContentControl.ContentProperty);
        bindings.TwoWay(original, ToggleButton.IsCheckedProperty, target, ToggleButton.IsCheckedProperty);
        Availability(original, target);
        return target;
    }

    private ComboBox Selection(ComboBox original)
    {
        var target = new ComboBox();
        bindings.OneWay(original, ItemsControl.DisplayMemberPathProperty, target, ItemsControl.DisplayMemberPathProperty);
        bindings.OneWay(original, Selector.SelectedValuePathProperty, target, Selector.SelectedValuePathProperty);
        bindings.OneWay(original, ItemsControl.ItemsSourceProperty, target, ItemsControl.ItemsSourceProperty);
        bindings.TwoWay(original, Selector.SelectedItemProperty, target, Selector.SelectedItemProperty);
        if (string.IsNullOrEmpty(original.DisplayMemberPath)
            && original.ItemsSource is IEnumerable<EmulationSettingsChoiceView>)
            target.DisplayMemberPath = nameof(EmulationSettingsChoiceView.DisplayName);
        Availability(original, target);
        return target;
    }

    private Button Action(Button original)
    {
        var target = new Button { HorizontalAlignment = HorizontalAlignment.Left };
        bindings.Action(original, target, followCaption: true);
        return target;
    }

    private WrapPanel Radios(Panel source)
    {
        var target = new WrapPanel();
        var group = Guid.NewGuid().ToString();
        foreach (var original in source.Children.OfType<RadioButton>())
        {
            var radio = new RadioButton { GroupName = group, Margin = RadioGap };
            if (original.Content is TextBlock label)
            {
                var text = new TextBlock { TextWrapping = TextWrapping.Wrap };
                bindings.OneWay(label, TextBlock.TextProperty, text, TextBlock.TextProperty);
                radio.Content = text;
            }
            else if (original.Content is string) bindings.OneWay(original, ContentControl.ContentProperty, radio, ContentControl.ContentProperty);
            else throw new NotSupportedException(field.Id);
            Availability(original, radio);
            bindings.TwoWay(original, ToggleButton.IsCheckedProperty, radio, ToggleButton.IsCheckedProperty);
            target.Children.Add(radio);
        }
        return target;
    }

    private FrameworkElement Range(Grid? sourceRow, Slider original)
    {
        var vertical = original.Orientation == Orientation.Vertical;
        var slider = new Slider
        {
            Minimum = original.Minimum, Maximum = original.Maximum,
            Orientation = original.Orientation, IsDirectionReversed = original.IsDirectionReversed,
            Height = vertical ? VerticalSliderHeight : double.NaN,
            Width = vertical ? VerticalSliderWidth : HorizontalSliderMaximumWidth,
            MaxWidth = vertical ? VerticalSliderWidth : HorizontalSliderMaximumWidth,
            HorizontalAlignment = vertical ? HorizontalAlignment.Center : HorizontalAlignment.Stretch
        };
        foreach (var property in new[] { RangeBase.MinimumProperty, RangeBase.MaximumProperty, RangeBase.SmallChangeProperty,
            RangeBase.LargeChangeProperty, Slider.TickFrequencyProperty, Slider.TickPlacementProperty, Slider.IsSnapToTickEnabledProperty })
            bindings.OneWay(original, property, slider, property);
        bindings.TwoWay(original, RangeBase.ValueProperty, slider, RangeBase.ValueProperty);
        Availability(original, slider);
        var saved = slider.Value;
        async Task Changed()
        {
            if (disposed) return;
            endSliderEdit?.Invoke(original);
            if (slider.Value == saved) return;
            saved = slider.Value;
            await commit();
        }
        MouseButtonEventHandler press = (_, _) => beginSliderEdit?.Invoke(original);
        slider.PreviewMouseLeftButtonDown += press;
        DragCompletedEventHandler drag = async (_, _) => await Changed();
        MouseButtonEventHandler release = async (_, _) => { if (!slider.IsMouseCaptureWithin) await Changed(); };
        KeyEventHandler key = async (_, _) => await Changed();
        slider.AddHandler(Thumb.DragCompletedEvent, drag);
        MouseEventHandler lostCapture = async (_, _) => await Changed();
        slider.LostMouseCapture += lostCapture;
        slider.PreviewMouseLeftButtonUp += release;
        slider.PreviewKeyUp += key;
        detach.Add(() =>
        {
            slider.PreviewMouseLeftButtonDown -= press;
            slider.RemoveHandler(Thumb.DragCompletedEvent, drag);
            slider.LostMouseCapture -= lostCapture;
            slider.PreviewMouseLeftButtonUp -= release;
            slider.PreviewKeyUp -= key;
        });
        detach.Add(() => endSliderEdit?.Invoke(original));
        var panel = new StackPanel { Orientation = vertical ? Orientation.Vertical : Orientation.Horizontal };
        panel.Children.Add(slider);
        var valueLabel = sourceRow?.Children.OfType<TextBlock>().SingleOrDefault()
            ?? (original.Parent is Panel parent && parent.Children.OfType<TextBlock>().Count() > 1
                ? parent.Children.OfType<TextBlock>().Last() : null);
        if (valueLabel is { } originalLabel)
        {
            var label = Information(originalLabel);
            label.Margin = vertical ? new Thickness(0, 4, 0, 0) : ActionGap;
            label.HorizontalAlignment = vertical ? HorizontalAlignment.Center : HorizontalAlignment.Left;
            panel.Children.Add(label);
        }
        else
        {
            var label = new TextBlock { Margin = vertical ? new Thickness(0, 4, 0, 0) : ActionGap,
                VerticalAlignment = VerticalAlignment.Center };
            label.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding(nameof(Slider.Value))
                { Source = slider, StringFormat = "{0:0.##}" });
            panel.Children.Add(label);
        }
        return panel;
    }

    private StackPanel AutomaticRange(Panel original)
    {
        var result = new StackPanel();
        result.Children.Add(Toggle(original.Children.OfType<CheckBox>().Single()));
        var row = original.Children.OfType<Grid>().Single(grid => grid.Tag is Slider);
        result.Children.Add(Range(row, (Slider)row.Tag));
        return result;
    }

    private Grid SingleChoice(Grid original)
    {
        var result = new Grid();
        var selection = Selection(original.Children.OfType<ComboBox>().Single());
        var originalText = original.Children.OfType<TextBlock>().Single();
        var text = Information(originalText);
        Availability(originalText, text);
        result.Children.Add(selection);
        result.Children.Add(text);
        return result;
    }

    private DockPanel SizeWithUnit(Grid original)
    {
        var result = new DockPanel();
        var unit = Selection(original.Children.OfType<ComboBox>().Single());
        unit.Width = SizeUnitWidth;
        unit.Margin = ActionGap;
        DockPanel.SetDock(unit, Dock.Right);
        result.Children.Add(unit);
        result.Children.Add(Text(original.Children.OfType<TextBox>().Single()));
        return result;
    }

    private DockPanel Path(Grid original)
    {
        var result = new DockPanel();
        var browse = Action(original.Children.OfType<Button>().Single());
        browse.Margin = ActionGap;
        DockPanel.SetDock(browse, Dock.Right);
        result.Children.Add(browse);
        var sourceValue = original.Children.OfType<FrameworkElement>().Single(child => child is TextBox or TextBlock);
        result.Children.Add(sourceValue is TextBox input ? Text(input) : Information((TextBlock)sourceValue));
        return result;
    }

    private void Availability(FrameworkElement original, FrameworkElement target)
    {
        bindings.OneWay(original, IsEnabledProperty, target, IsEnabledProperty);
        bindings.OneWay(original, VisibilityProperty, target, VisibilityProperty);
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        try { bindings.Dispose(); }
        finally
        {
            try { foreach (var action in detach) action(); }
            finally { detach.Clear(); Content = null; }
        }
    }
}
