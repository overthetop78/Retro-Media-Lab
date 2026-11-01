using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using GWGUI.App.Contracts.Theming;
using GWGUI.App.Theme.Common;
using GWGUI.Tests.Application.TestInfrastructure;

namespace GWGUI.Tests.Interface.Themes;

[Collection("WPF")]
public sealed class ThemeElementLeaseTests(StaExecutionScenarios sta)
{
    private const string InputId = "destination";
    private const string ActionId = "execute";
    private const string DocumentId = "journal";
    private const string BackgroundResource = "Lease.Background";

    [Fact]
    public Task NewFormKeepsExistingDataAndAction() => sta.RunAsync(async () =>
    {
        var model = new TextBox { Text = "before" };
        var original = new StackPanel { DataContext = model };
        var input = new TextBox();
        input.SetBinding(TextBox.TextProperty, new Binding(nameof(TextBox.Text)) { Mode = BindingMode.TwoWay });
        var execute = new Button();
        var executed = string.Empty;
        execute.Click += (_, _) => executed = model.Text;
        original.Children.Add(input);
        original.Children.Add(execute);
        var form = new Grid();
        var originalBinding = BindingOperations.GetBindingBase(input, TextBox.TextProperty);
        using (var lease = new ThemeElementLease([
            new(InputId, input, ThemeElementRole.Input), new(ActionId, execute, ThemeElementRole.Action)]))
        {
            form.Children.Add(lease[InputId]);
            form.Children.Add(lease[ActionId]);
            Arrange(form);
            await Dispatcher.Yield(DispatcherPriority.DataBind);
            Assert.Equal("before", input.Text);
            input.SetCurrentValue(TextBox.TextProperty, "after");
            input.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
            execute.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.Equal("after", executed);
            Assert.Same(originalBinding, BindingOperations.GetBindingBase(input, TextBox.TextProperty));
        }
        Assert.Equal(new UIElement[] { input, execute }, original.Children.Cast<UIElement>());
        Assert.Equal("after", input.Text);
        Assert.Same(originalBinding, BindingOperations.GetBindingBase(input, TextBox.TextProperty));
    });

    [Fact]
    public Task NewParentCannotEraseAvailabilityOrDataContext() => sta.RunAsync(async () =>
    {
        var firstModel = new TextBox { Text = "first" };
        var original = new Grid { DataContext = firstModel };
        var input = new TextBox();
        input.SetBinding(TextBox.TextProperty, new Binding(nameof(TextBox.Text)));
        original.Children.Add(input);
        using (var lease = new ThemeElementLease([new(InputId, input, ThemeElementRole.Input)]))
        {
            var unrelated = new Grid { DataContext = new object() };
            unrelated.Children.Add(lease[InputId]);
            Arrange(unrelated);
            await Dispatcher.Yield(DispatcherPriority.DataBind);
            Assert.Same(firstModel, input.DataContext);
            original.IsEnabled = false;
            await Dispatcher.Yield(DispatcherPriority.DataBind);
            Assert.False(input.IsEnabled);
            original.IsEnabled = true;
            await Dispatcher.Yield(DispatcherPriority.DataBind);
            Assert.True(input.IsEnabled);
            var secondModel = new TextBox { Text = "second" };
            original.DataContext = secondModel;
            await Dispatcher.Yield(DispatcherPriority.DataBind);
            Assert.Same(secondModel, input.DataContext);
            Assert.Equal("second", input.Text);
            input.IsEnabled = false;
        }
        Assert.False(input.IsEnabled);
        Assert.Same(original.DataContext, input.DataContext);
    });

    [Fact]
    public Task BusinessVisibilityIsExplicitAndOldFormCollapseIsNotReused() => sta.Run(() =>
    {
        var original = new Grid { Visibility = Visibility.Collapsed };
        var input = new TextBox();
        original.Children.Add(input);
        using (var lease = new ThemeElementLease([new(InputId, input, ThemeElementRole.Input)]))
            Assert.Equal(Visibility.Visible, lease[InputId].Visibility);
        var condition = new Grid();
        using (var lease = new ThemeElementLease([new(InputId, input, ThemeElementRole.Input, condition)]))
        {
            condition.Visibility = Visibility.Collapsed;
            Assert.Equal(Visibility.Collapsed, lease[InputId].Visibility);
            condition.Visibility = Visibility.Visible;
            Assert.Equal(Visibility.Visible, lease[InputId].Visibility);
        }
    });

    [Fact]
    public Task RefusedContainersLeaveOriginalFormUntouched() => sta.Run(() =>
    {
        var original = new StackPanel();
        var input = new TextBox();
        var card = new UserControl();
        original.Children.Add(input);
        original.Children.Add(card);
        Assert.Throws<ArgumentException>(() => new ThemeElementLease([
            new(InputId, input, ThemeElementRole.Input), new(ActionId, card, ThemeElementRole.Document)]));
        Assert.Equal(new UIElement[] { input, card }, original.Children.Cast<UIElement>());
        Assert.Throws<ArgumentException>(() => new ThemeElementLease([
            new(InputId, original, ThemeElementRole.Document)]));
    });

    [Fact]
    public Task DuplicateSlotsCannotStealAnExistingLease() => sta.Run(() =>
    {
        var original = new Grid();
        var input = new TextBox();
        original.Children.Add(input);
        Assert.Throws<ArgumentException>(() => new ThemeElementLease([
            new(InputId, input, ThemeElementRole.Input), new(ActionId, input, ThemeElementRole.Input)]));
        Assert.Same(original, input.Parent);
        using (var first = new ThemeElementLease([new(InputId, input, ThemeElementRole.Input)]))
        {
            Assert.Throws<InvalidOperationException>(() => new ThemeElementLease([
                new(InputId, input, ThemeElementRole.Input)]));
            Assert.Same(first[InputId], input.Parent);
        }
        Assert.Same(original, input.Parent);
    });

    [Fact]
    public Task PresentationErrorRestoresEverySiblingAndLayout() => sta.Run(() =>
    {
        var original = new Grid();
        var before = new Border();
        var first = new TextBox { Width = 123, Margin = new Thickness(7) };
        var second = new Button();
        original.Children.Add(before);
        original.Children.Add(first);
        original.Children.Add(second);
        Grid.SetRow(first, 2);
        Assert.Throws<ArgumentException>(() =>
        {
            using var lease = new ThemeElementLease([
                new(ActionId, second, ThemeElementRole.Action), new(InputId, first, ThemeElementRole.Input)]);
            lease[InputId].SetPresentationValue(FrameworkElement.WidthProperty, 300d);
            lease[InputId].SetPresentationValue(FrameworkElement.MarginProperty, new Thickness());
            lease[InputId].SetPresentationValue(TextBox.TextProperty, "must not modify business data");
        });
        Assert.Equal(new UIElement[] { before, first, second }, original.Children.Cast<UIElement>());
        Assert.Equal(123, first.Width);
        Assert.Equal(new Thickness(7), first.Margin);
        Assert.Equal(2, Grid.GetRow(first));
        Assert.Equal(string.Empty, first.Text);
    });

    [Fact]
    public Task PresentationRestoresBindingsAndDynamicResources() => sta.Run(() =>
    {
        var original = new Grid();
        original.Resources[BackgroundResource] = Brushes.Blue;
        var model = new Border { Width = 120 };
        var input = new TextBox();
        input.SetBinding(FrameworkElement.WidthProperty, new Binding(nameof(FrameworkElement.Width)) { Source = model });
        input.SetResourceReference(Control.BackgroundProperty, BackgroundResource);
        original.Children.Add(input);
        using (var lease = new ThemeElementLease([new(InputId, input, ThemeElementRole.Input)]))
        {
            lease[InputId].SetPresentationValue(FrameworkElement.WidthProperty, 260d);
            lease[InputId].SetPresentationValue(Control.BackgroundProperty, Brushes.Red);
            model.Width = 170;
        }
        Assert.Equal(170, input.Width);
        original.Resources[BackgroundResource] = Brushes.Green;
        Assert.Same(Brushes.Green, input.Background);
        model.Width = 200;
        Assert.Equal(200, input.Width);
    });

    [Fact]
    public Task JournalDocumentIsBorrowedAndHostsReleaseBindings() => sta.Run(() =>
    {
        var original = new Border();
        var output = new RichTextBox();
        original.Child = output;
        var document = output.Document;
        ThemeElementHost host;
        using (var lease = new ThemeElementLease([new(DocumentId, output, ThemeElementRole.Document)]))
        {
            host = lease[DocumentId];
            document.Blocks.Add(new Paragraph(new Run("operation completed")));
        }
        Assert.Same(output, original.Child);
        Assert.Same(document, output.Document);
        Assert.Contains("operation completed", new TextRange(document.ContentStart, document.ContentEnd).Text);
        Assert.Null(host.Content);
        Assert.False(BindingOperations.IsDataBound(host, FrameworkElement.DataContextProperty));
        Assert.False(BindingOperations.IsDataBound(host, UIElement.IsEnabledProperty));
        Assert.Throws<ObjectDisposedException>(() => host.SetPresentationValue(FrameworkElement.WidthProperty, 100d));
    });
    [Fact]
    public Task SelectingInNewFormKeepsTheOriginalSelectionModel() => sta.RunAsync(async () =>
    {
        var model = new ListBox { ItemsSource = new[] { "first", "second", "third" }, SelectedIndex = 1 };
        var original = new Grid { DataContext = model };
        var choice = new ComboBox();
        choice.SetBinding(ItemsControl.ItemsSourceProperty, new Binding(nameof(ItemsControl.ItemsSource)));
        choice.SetBinding(ComboBox.SelectedItemProperty, new Binding(nameof(ComboBox.SelectedItem)) { Mode = BindingMode.TwoWay });
        original.Children.Add(choice);
        Arrange(original);
        await Dispatcher.Yield(DispatcherPriority.DataBind);
        Assert.Equal("second", choice.SelectedItem);
        var changed = new List<object?>();
        choice.SelectionChanged += (_, _) => changed.Add(choice.SelectedItem);
        using (var lease = new ThemeElementLease([new(InputId, choice, ThemeElementRole.Input)]))
        {
            var form = new Grid();
            form.Children.Add(lease[InputId]);
            Arrange(form);
            await Dispatcher.Yield(DispatcherPriority.DataBind);
            Assert.Equal("second", model.SelectedItem);
            Assert.DoesNotContain(null, changed);
            choice.SelectedItem = "third";
            Assert.Equal("third", model.SelectedItem);
        }
        await Dispatcher.Yield(DispatcherPriority.DataBind);
        Assert.Same(original, choice.Parent);
        Assert.Equal("third", choice.SelectedItem);
        Assert.Equal("third", model.SelectedItem);
        Assert.DoesNotContain(null, changed);
    });

    private static void Arrange(FrameworkElement root)
    {
        root.Measure(new Size(640, 400));
        root.Arrange(new Rect(0, 0, 640, 400));
        root.UpdateLayout();
    }
}
