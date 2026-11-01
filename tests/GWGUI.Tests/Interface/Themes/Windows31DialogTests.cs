using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GWGUI.App.Theme.OS.Editions.Windows31;
using GWGUI.Tests.Application.TestInfrastructure;

namespace GWGUI.Tests.Interface.Themes;

[Collection("WPF")]
public sealed class Windows31DialogTests(StaExecutionScenarios sta)
{
    [Fact]
    public Task AModalDialogDisablesOnlyItsApplicationAndReturnsToItOnClose()
        => sta.Run(() =>
        {
            var read = new Grid();
            var write = new Grid();
            using var desktop = Desktop(read, write);
            var owner = desktop.Open(Windows31ApplicationId.Read);
            desktop.Open(Windows31ApplicationId.Write);
            var created = 0;
            var released = 0;
            Action? close = null;
            var dialogView = new TextBox { Text = "option" };
            var dialog = desktop.ShowDialog(Windows31ApplicationId.Read, Windows31DialogKind.AcquisitionOptions,
                Windows31DialogMode.Modal, "Common.Advanced", new Size(400, 300), done =>
                {
                    close = done;
                    created++;
                    return new Windows31Document(dialogView, new Lifetime(() => released++));
                });
            Assert.False(Frame(desktop, read).IsEnabled);
            Assert.True(Frame(desktop, write).IsEnabled);
            Assert.False(owner.IsActive);
            Assert.True(dialog.IsActive);
            Assert.Equal(Visibility.Collapsed, Find<Button>(Frame(desktop, dialogView), "IconifyCommand").Visibility);
            Assert.Equal(Visibility.Collapsed, Find<Button>(Frame(desktop, dialogView), "MaximizeCommand").Visibility);
            desktop.Open(Windows31ApplicationId.Write);
            Assert.False(dialog.IsActive);
            desktop.Open(Windows31ApplicationId.Read);
            Assert.True(dialog.IsActive);
            Assert.Same(dialog, desktop.ShowDialog(Windows31ApplicationId.Read, Windows31DialogKind.Classification,
                Windows31DialogMode.Modal, "Common.Advanced", new Size(400, 300), _ => throw new InvalidOperationException()));
            Assert.Equal(1, created);
            close!();
            Assert.Equal(1, released);
            Assert.True(Frame(desktop, read).IsEnabled);
            Assert.True(owner.IsActive);
            Assert.Null(dialogView.Parent);
            Assert.Equal("option", dialogView.Text);
            close();
            Assert.Equal(1, released);
        });

    [Fact]
    public Task ModelessInspectorRemainsAvailableAndModalOptionsRestoreItsPreviousAvailability()
        => sta.Run(() =>
        {
            var read = new Grid();
            using var desktop = Desktop(read, new Grid());
            var owner = desktop.Open(Windows31ApplicationId.Read);
            var inspector = new Grid();
            var released = 0;
            var inspection = desktop.ShowDialog(Windows31ApplicationId.Read, Windows31DialogKind.Inspector,
                Windows31DialogMode.Modeless, "Visual.Inspector", new Size(400, 300),
                _ => new Windows31Document(inspector, new Lifetime(() => released++)));
            Assert.True(Frame(desktop, read).IsEnabled);
            Assert.True(Frame(desktop, inspector).IsEnabled);
            Assert.Same(inspection, desktop.ShowDialog(Windows31ApplicationId.Read, Windows31DialogKind.Inspector,
                Windows31DialogMode.Modeless, "Visual.Inspector", new Size(400, 300), _ => throw new InvalidOperationException()));
            desktop.ShowDialog(Windows31ApplicationId.Read, Windows31DialogKind.Classification,
                Windows31DialogMode.Modal, "Classification.Format", new Size(400, 300),
                _ => new Windows31Document(new Grid(), new Lifetime(() => released++)));
            Assert.False(Frame(desktop, inspector).IsEnabled);
            Assert.False(Frame(desktop, read).IsEnabled);
            desktop.CloseDialog(Windows31ApplicationId.Read, Windows31DialogKind.Classification);
            Assert.True(Frame(desktop, inspector).IsEnabled);
            Assert.True(Frame(desktop, read).IsEnabled);
            Assert.True(inspection.IsActive);
            owner.Close();
            Assert.Equal(2, released);
            Assert.Null(read.Parent);
            Assert.Null(inspector.Parent);
            Assert.Empty(desktop.OpenApplications);
        });

    [Fact]
    public Task ClosingAnInactiveDialogDoesNotActivateItsApplicationOverTheCurrentOne()
        => sta.Run(() =>
        {
            using var desktop = Desktop(new Grid(), new Grid());
            var read = desktop.Open(Windows31ApplicationId.Read);
            desktop.ShowDialog(Windows31ApplicationId.Read, Windows31DialogKind.Inspector,
                Windows31DialogMode.Modeless, "Visual.Inspector", new Size(400, 300),
                _ => new Windows31Document(new Grid()));
            var write = desktop.Open(Windows31ApplicationId.Write);
            Assert.True(write.IsActive);
            desktop.CloseDialog(Windows31ApplicationId.Read, Windows31DialogKind.Inspector);
            Assert.True(write.IsActive);
            Assert.False(read.IsActive);
        });

    [Fact]
    public Task FailedCreationReleasesTransferredLifetimesAndNeverDisablesTheOwner()
        => sta.Run(() =>
        {
            var read = new Grid();
            using var desktop = Desktop(read, new Grid());
            desktop.Open(Windows31ApplicationId.Read);
            var released = 0;
            var invalid = new Grid();
            Assert.Throws<ArgumentOutOfRangeException>(() => desktop.ShowDialog(Windows31ApplicationId.Read,
                Windows31DialogKind.AcquisitionOptions, Windows31DialogMode.Modal, "Common.Advanced", new Size(0, 0),
                _ => new Windows31Document(invalid, new Lifetime(() => released++))));
            Assert.Equal(1, released);
            Assert.Null(invalid.Parent);
            Assert.True(Frame(desktop, read).IsEnabled);
            Assert.Throws<InvalidOperationException>(() => desktop.ShowDialog(Windows31ApplicationId.Read,
                Windows31DialogKind.AcquisitionOptions, Windows31DialogMode.Modal, "Common.Advanced", new Size(400, 300),
                _ => throw new InvalidOperationException()));
            Assert.True(Frame(desktop, read).IsEnabled);
        });

    [Fact]
    public Task DisposalFailureStillClosesOtherDialogsAndTheirApplications()
        => sta.Run(() =>
        {
            var read = new Grid();
            var write = new Grid();
            var desktop = Desktop(read, write);
            var released = 0;
            var badView = new Grid();
            var goodView = new Grid();
            try
            {
                desktop.Open(Windows31ApplicationId.Read);
                desktop.Open(Windows31ApplicationId.Write);
                desktop.ShowDialog(Windows31ApplicationId.Read, Windows31DialogKind.AcquisitionOptions,
                    Windows31DialogMode.Modal, "Common.Advanced", new Size(400, 300),
                    _ => new Windows31Document(badView, new Lifetime(() => throw new InvalidOperationException())));
                desktop.ShowDialog(Windows31ApplicationId.Write, Windows31DialogKind.WriteOptions,
                    Windows31DialogMode.Modal, "Common.Advanced", new Size(400, 300),
                    _ => new Windows31Document(goodView, new Lifetime(() => released++)));
                Assert.Throws<InvalidOperationException>(desktop.Dispose);
                Assert.Equal(1, released);
                Assert.Empty(desktop.OpenApplications);
                Assert.Empty(desktop.Children);
                Assert.Null(read.Parent);
                Assert.Null(write.Parent);
                Assert.Null(badView.Parent);
                Assert.Null(goodView.Parent);
            }
            finally { desktop.Dispose(); }
        });

    private static Windows31Desktop Desktop(Grid read, Grid write) => new([
        new Windows31ApplicationDefinition(Windows31ApplicationId.Read, Windows31ProgramGroup.Media, "Tab.Read",
            () => new Windows31Document(read), new Size(600, 400)),
        new Windows31ApplicationDefinition(Windows31ApplicationId.Write, Windows31ProgramGroup.Media, "Tab.Write",
            () => new Windows31Document(write), new Size(600, 400))]);

    private static Windows31Frame Frame(DependencyObject root, FrameworkElement content)
        => Descendants(root).OfType<Windows31Frame>().Single(frame => ReferenceEquals(Find<ContentControl>(frame, "DocumentHost").Content, content));
    private static T Find<T>(FrameworkElement root, string name) => Assert.IsType<T>(root.FindName(name));
    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            yield return child;
            foreach (var nested in Descendants(child)) yield return nested;
        }
    }
    private sealed class Lifetime(Action dispose) : IDisposable { public void Dispose() => dispose(); }
}
