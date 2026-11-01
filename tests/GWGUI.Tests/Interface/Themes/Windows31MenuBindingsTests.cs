using System.Windows;
using System.Windows.Controls;
using GWGUI.App.Theme.OS.Editions.Windows31;
using GWGUI.Tests.Application.TestInfrastructure;

namespace GWGUI.Tests.Interface.Themes;

[Collection("WPF")]
public sealed class Windows31MenuBindingsTests(StaExecutionScenarios sta)
{
    [Fact]
    public Task CopiedMenusPreserveNativeCommandsStatesAndOriginalParents() => sta.Run(() =>
    {
        var source = new MenuItem();
        var native = new MenuItem { Header = "Original", IsEnabled = false };
        source.Items.Add(native);
        var target = new MenuItem();
        var calls = 0;
        native.Click += (sender, _) => { Assert.Same(native, sender); calls++; };
        using var binding = new Windows31MenuBindings(source, target);
        var copy = Assert.IsType<MenuItem>(Assert.Single(target.Items.Cast<object>()));
        Assert.NotSame(native, copy);
        Assert.False(copy.IsEnabled);
        native.IsEnabled = true;
        native.Header = "Updated";
        Assert.True(copy.IsEnabled);
        Assert.Equal("Updated", copy.Header);
        copy.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        Assert.Equal(1, calls);
        Assert.Same(source, native.Parent);
        binding.Dispose();
        copy.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        Assert.Equal(1, calls);
        Assert.Empty(target.Items);
    });

    [Fact]
    public Task NestedModuleChangesRebuildTheSameHistoricalMenuAndDetachOldCopies() => sta.Run(() =>
    {
        var options = new MenuItem();
        options.Items.Add(new MenuItem { Header = "Preferences" });
        options.Items.Add(new Separator());
        var modules = new MenuItem { Header = "Emulation" };
        var target = new MenuItem();
        using var binding = new Windows31MenuBindings(options, target, modules);
        var module = new MenuItem { Header = "Acorn" };
        var calls = 0;
        module.Click += (_, _) => calls++;
        modules.Items.Add(module);
        var group = Assert.IsType<MenuItem>(target.Items[2]);
        var copy = Assert.IsType<MenuItem>(Assert.Single(group.Items.Cast<object>()));
        copy.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        Assert.Equal(1, calls);
        modules.Items.Clear();
        Assert.Empty(Assert.IsType<MenuItem>(target.Items[2]).Items);
        copy.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        Assert.Equal(1, calls);
    });
}
