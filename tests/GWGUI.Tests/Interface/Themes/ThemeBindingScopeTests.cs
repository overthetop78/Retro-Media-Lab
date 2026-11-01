using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using GWGUI.App.Theme.Common;
using GWGUI.Tests.Application.TestInfrastructure;

namespace GWGUI.Tests.Interface.Themes;

[Collection("WPF")]
public sealed class ThemeBindingScopeTests(StaExecutionScenarios sta)
{
    private sealed class InternalPresentation : DependencyObject
    {
        internal static readonly DependencyProperty IncomingProperty = DependencyProperty.Register(
            "Incoming", typeof(string), typeof(InternalPresentation), new PropertyMetadata(string.Empty));
    }

    [Fact]
    public Task OneWayPresentationCanReceiveDataWithoutPublishingAnEditableClrProperty() => sta.Run(() =>
    {
        var source = new TextBox { Text = "before" };
        var target = new InternalPresentation();
        using (var scope = new ThemeBindingScope())
        {
            scope.OneWay(source, TextBox.TextProperty, target, InternalPresentation.IncomingProperty);
            Assert.Equal("before", target.GetValue(InternalPresentation.IncomingProperty));
            source.Text = "after";
            Assert.Equal("after", target.GetValue(InternalPresentation.IncomingProperty));
        }
        source.Text = "detached";
        Assert.Equal("after", target.GetValue(InternalPresentation.IncomingProperty));
    });
    [Fact]
    public Task IndependentInputUsesTheExistingModelWithoutReplacingItsBinding() => sta.Run(() =>
    {
        var model = new FrameworkElement { Tag = "before" };
        var originalForm = new Grid();
        var source = new TextBox();
        originalForm.Children.Add(source);
        var binding = new Binding(nameof(FrameworkElement.Tag)) { Source = model, Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged };
        source.SetBinding(TextBox.TextProperty, binding);
        var newForm = new StackPanel();
        var input = new TextBox();
        newForm.Children.Add(input);
        using (var scope = new ThemeBindingScope())
        {
            scope.TwoWay(source, TextBox.TextProperty, input, TextBox.TextProperty);
            Assert.Equal("before", input.Text);
            input.SetCurrentValue(TextBox.TextProperty, "after");
            Assert.Equal("after", model.Tag);
            Assert.Same(binding, BindingOperations.GetBindingBase(source, TextBox.TextProperty));
            Assert.Same(originalForm, source.Parent);
            Assert.Same(newForm, input.Parent);
            model.Tag = "from model";
            Assert.Equal("from model", input.Text);
        }
        source.Text = "after disposal";
        Assert.Equal("from model", input.Text);
        input.Text = "detached input";
        Assert.Equal("after disposal", source.Text);
    });

    [Fact]
    public Task InterdependentChoicesDoNotWriteTransientTargetSelectionsBack() => sta.Run(() =>
    {
        var families = new ComboBox { ItemsSource = new[] { "first", "second" }, SelectedIndex = 0 };
        var formats = new ComboBox { ItemsSource = new[] { "a", "b" }, SelectedIndex = 1 };
        families.SelectionChanged += (_, _) =>
        {
            formats.ItemsSource = new[] { "c", "d" };
            formats.SelectedIndex = 1;
        };
        var formatChanges = new List<int>();
        formats.SelectionChanged += (_, _) => formatChanges.Add(formats.SelectedIndex);
        var newFamilies = new ComboBox();
        var newFormats = new ComboBox();
        using var scope = new ThemeBindingScope();
        scope.OneWay(families, ItemsControl.ItemsSourceProperty, newFamilies, ItemsControl.ItemsSourceProperty);
        scope.OneWay(formats, ItemsControl.ItemsSourceProperty, newFormats, ItemsControl.ItemsSourceProperty);
        scope.TwoWay(families, Selector.SelectedIndexProperty, newFamilies, Selector.SelectedIndexProperty);
        scope.TwoWay(formats, Selector.SelectedIndexProperty, newFormats, Selector.SelectedIndexProperty);
        newFamilies.SelectedIndex = 1;
        Assert.Equal("second", families.SelectedItem);
        Assert.Equal("d", formats.SelectedItem);
        Assert.Equal("d", newFormats.SelectedItem);
        Assert.Equal(new[] { -1, 1 }, formatChanges);
    });

    [Fact]
    public Task NewActionFollowsAvailabilityAndInvokesTheSameHandler() => sta.Run(() =>
    {
        var source = new Button { Content = "run" };
        var action = new Button();
        var calls = 0;
        source.Click += (_, _) => calls++;
        using (var scope = new ThemeBindingScope())
        {
            scope.Action(source, action, followCaption: true);
            Assert.Equal("run", action.Content);
            source.Content = "cancel";
            source.IsEnabled = false;
            Assert.Equal("cancel", action.Content);
            Assert.False(action.IsEnabled);
            action.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            Assert.Equal(0, calls);
            source.IsEnabled = true;
            action.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            Assert.Equal(1, calls);
        }
        action.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        Assert.Equal(1, calls);
    });

    [Fact]
    public Task ActionErrorDoesNotKeepSubscriptionsAfterFinally() => sta.Run(() =>
    {
        var source = new Button();
        var action = new Button();
        source.Click += (_, _) => throw new InvalidOperationException();
        Assert.Throws<InvalidOperationException>(() =>
        {
            using var scope = new ThemeBindingScope();
            scope.Action(source, action);
            action.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        });
        action.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
    });

    [Fact]
    public Task SourceVisualsCannotBecomeAnActionCaption() => sta.Run(() =>
    {
        var source = new Button { Content = new StackPanel() };
        using var scope = new ThemeBindingScope();
        Assert.Throws<ArgumentException>(() => scope.Action(source, new Button(), followCaption: true));
    });
    [Fact]
    public Task LaterCaptionChangeCannotInsertAnOriginalVisualBlock() => sta.Run(() =>
    {
        var source = new Button { Content = "execute" };
        var action = new Button();
        using var scope = new ThemeBindingScope();
        scope.Action(source, action, followCaption: true);
        Assert.Throws<ArgumentException>(() => source.Content = new UserControl());
        Assert.Equal("execute", action.Content);
    });

    [Fact]
    public Task NewMenuActionFollowsItsOriginalEndpointAndReleasesEverySubscription() => sta.Run(() =>
    {
        var source = new Button { Content = "write" };
        var action = new MenuItem();
        var calls = 0;
        source.Click += (_, _) => calls++;
        using (var scope = new ThemeBindingScope())
        {
            scope.Action(source, action, followCaption: true);
            Assert.Equal("write", action.Header);
            source.Content = "cancel";
            Assert.Equal("cancel", action.Header);
            source.IsEnabled = false;
            action.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            Assert.Equal(0, calls);
            source.IsEnabled = true;
            action.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            Assert.Equal(1, calls);
            Assert.Throws<ArgumentException>(() => source.Content = new UserControl());
            Assert.Equal("cancel", action.Header);
        }
        source.Content = "detached";
        Assert.Equal("cancel", action.Header);
        action.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        Assert.Equal(1, calls);
    });
}
