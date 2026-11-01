using System.Windows;
using System.Windows.Controls;
using RetroMediaLab.App.Contracts.Storage;
using RetroMediaLab.App.Theme.OS.Editions.Windows31.Views;
using RetroMediaLab.App.Views.Controls.Common;
using RetroMediaLab.MediaEngine.Images.Formats;
using RetroMediaLab.Tests.Application.TestInfrastructure;

namespace RetroMediaLab.Tests.Interface.Themes;

[Collection("WPF")]
public sealed class Windows31ClassificationViewTests(StaExecutionScenarios sta)
{
    [Fact]
    public Task NewMachineAndFormatControlsKeepTheExistingRulesAndDetectionMarkers()
        => sta.Run(() =>
        {
            var source = Source();
            var notifications = 0;
            EventHandler changed = (_, _) => notifications++;
            source.ValueChanged += changed;
            try
            {
                using var view = new ClassificationView(source, () => { });
                var machines = Find<ComboBox>(view, "Machines");
                var formats = Find<ListBox>(view, "Formats");
                Assert.NotSame(source.FindName("Machine"), machines);
                Assert.Equal("first", Assert.IsType<DiskMachineChoice>(machines.SelectedItem).DisplayName);
                Assert.True(Assert.IsType<DiskFormatChoice>(formats.SelectedItem).IsDetected);
                machines.SelectedItem = machines.Items.Cast<DiskMachineChoice>().Single(item => item.DisplayName == "second");
                Assert.Equal("second", source.SelectedMachine);
                Assert.All(formats.Items.Cast<DiskFormatChoice>(), item => Assert.Equal("second", item.Format.Family));
                formats.SelectedItem = formats.Items.Cast<DiskFormatChoice>().Single(item => item.Format.Id == "test.second.two");
                Assert.Equal("test.second.two", source.SelectedFormatId);
                Assert.Equal(2, notifications);
                source.ApplyDetection("test.first", null);
                Assert.Equal("first", Assert.IsType<DiskMachineChoice>(machines.SelectedItem).DisplayName);
                Assert.Equal("test.first", Assert.IsType<DiskFormatChoice>(formats.SelectedItem).Format.Id);
                Assert.Equal(2, notifications);
                Find<CheckBox>(view, "Automatic").IsChecked = false;
                Assert.False(source.AutomaticDetection);
                Assert.Equal(3, notifications);
                source.ShowAutomatic = false;
                Assert.Equal(Visibility.Collapsed, Find<CheckBox>(view, "Automatic").Visibility);
                Assert.Equal(Visibility.Collapsed, Find<DockPanel>(view, "ProtectionGroup").Visibility);
            }
            finally { source.ValueChanged -= changed; }
        });

    [Fact]
    public Task ProtectionChoicesFollowTheExistingCatalogAndDisappearForAnUnsupportedFormat()
        => sta.Run(() =>
        {
            var source = new DiskClassificationSelector();
            source.SetCatalog([
                new DiskFormat("apple2.appledos.140", "Apple II", "Apple DOS", []),
                new DiskFormat("apple2.prodos.140", "Apple II", "ProDOS", [])]);
            source.ApplyDetection("apple2.appledos.140", "apple2.rwts18");
            using var view = new ClassificationView(source, () => { });
            var protections = Find<ComboBox>(view, "Protections");
            Assert.Equal(Visibility.Visible, Find<DockPanel>(view, "ProtectionGroup").Visibility);
            Assert.Equal("apple2.rwts18", Assert.IsType<DiskProtection>(protections.SelectedItem).Id);
            protections.SelectedIndex = 0;
            Assert.Null(source.SelectedProtectionId);
            var formats = Find<ListBox>(view, "Formats");
            formats.SelectedItem = formats.Items.Cast<DiskFormatChoice>().Single(item => item.Format.Id == "apple2.prodos.140");
            Assert.Empty(protections.Items);
            Assert.Equal(Visibility.Collapsed, Find<DockPanel>(view, "ProtectionGroup").Visibility);
        });

    [Fact]
    public Task ClosingDisposesOnlyTheNewControlsAndDetachesCallbacks()
        => sta.Run(() =>
        {
            var source = Source();
            var closes = 0;
            var originalMachine = Find<ComboBox>(source, "Machine");
            var parent = originalMachine.Parent;
            var view = new ClassificationView(source, () => closes++);
            var close = Find<Button>(view, "CloseCommand");
            var machines = Find<ComboBox>(view, "Machines");
            try
            {
                close.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Assert.Equal(1, closes);
            }
            finally { view.Dispose(); }
            close.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.Equal(1, closes);
            Assert.Empty(machines.Items);
            Assert.Same(parent, originalMachine.Parent);
            originalMachine.SelectedItem = originalMachine.Items.Cast<DiskMachineChoice>().Single(item => item.DisplayName == "second");
            Assert.Equal("second", source.SelectedMachine);
            Assert.Empty(machines.Items);
        });

    private static DiskClassificationSelector Source()
    {
        var source = new DiskClassificationSelector();
        source.SetCatalog([
            new DiskFormat("test.first", "first", "detected", [new(".one", "one", true)]),
            new DiskFormat("test.second.one", "second", "one", [new(".two", "two", true)]),
            new DiskFormat("test.second.two", "second", "two", [new(".three", "three", true)])]);
        source.ApplyDetection("test.first", null);
        return source;
    }

    private static T Find<T>(FrameworkElement view, string name) => Assert.IsType<T>(view.FindName(name));
}
