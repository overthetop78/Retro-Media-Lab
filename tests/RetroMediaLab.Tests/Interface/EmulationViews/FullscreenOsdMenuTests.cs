using System.Windows;
using System.Windows.Controls;
using RetroMediaLab.Tests.Application.TestInfrastructure;
using RetroMediaLab.VideoPresentation.Windows.Osd;
using Entry = RetroMediaLab.VideoPresentation.Windows.Osd.FullscreenOsdWindow.Entry;
using OsdNavigation = RetroMediaLab.VideoPresentation.Windows.Osd.FullscreenOsdWindow.Navigation;

namespace RetroMediaLab.Tests.Interface.EmulationViews;

[Collection("WPF")]
public sealed class FullscreenOsdMenuTests(StaExecutionScenarios sta)
{
    [Fact]
    public Task NestedSubmenusReplaceEachOtherBesideTheMainMenu() => sta.RunAsync(async () =>
    {
        var layer = new Grid();
        var owner = new Window { Content = layer, ShowInTaskbar = false, Width = 500, Height = 480 };
        FullscreenOsdWindow? menu = null;
        try
        {
            Entry[] entries =
            [
                new("Video", Children:
                [
                    new("Image", Children:
                    [
                        new("Colors", Children: [new("Brightness")], FullHeight: true)
                    ])
                ])
            ];
            owner.Show();
            menu = new FullscreenOsdWindow(owner, layer, "Machine", null, entries, error => throw error, "Loading");
            menu.Show();
            var overlay = Assert.IsType<Grid>(menu.Content);
            var main = Assert.IsType<Border>(overlay.Children[0]);
            var submenu = Assert.IsType<Decorator>(overlay.Children[1]);
            Assert.Null(submenu.Child);

            await menu.NavigateAsync(OsdNavigation.Right);
            menu.UpdateLayout();
            var first = Assert.IsType<Border>(submenu.Child);
            var firstPosition = first.TranslatePoint(new Point(), overlay);
            var mainWidth = main.ActualWidth;

            await menu.NavigateAsync(OsdNavigation.Right);
            var second = Assert.IsType<Border>(submenu.Child);
            Assert.NotSame(first, second);
            Assert.Null(first.Parent);
            await menu.NavigateAsync(OsdNavigation.Right);
            menu.UpdateLayout();

            var third = Assert.IsType<Border>(submenu.Child);
            Assert.NotSame(second, third);
            Assert.Null(second.Parent);
            var thirdPosition = third.TranslatePoint(new Point(), overlay);
            Assert.Equal(mainWidth, main.ActualWidth);
            Assert.Equal(firstPosition, thirdPosition);
            Assert.InRange(Math.Abs(thirdPosition.X - mainWidth), 0, 1);
            Assert.True(firstPosition.Y > 0);
            Assert.InRange(Math.Abs(thirdPosition.X + third.ActualWidth - overlay.ActualWidth), 0, 1);

            await menu.NavigateAsync(OsdNavigation.Left);
            Assert.Same(second, submenu.Child);
            await menu.NavigateAsync(OsdNavigation.Cancel);
            Assert.Same(first, submenu.Child);
            await menu.NavigateAsync(OsdNavigation.Cancel);
            Assert.Null(submenu.Child);
        }
        finally
        {
            try { menu?.Close(); }
            finally { owner.Content = null; owner.Close(); }
        }
    });

    [Fact]
    public Task EditingKeepsTheCurrentSubmenuAndSavesOnlyOnConfirmation() => sta.RunAsync(async () =>
    {
        var layer = new Grid();
        var owner = new Window { Content = layer, ShowInTaskbar = false, Width = 800, Height = 480 };
        FullscreenOsdWindow? menu = null;
        try
        {
            int saved = 10;
            int preview = saved;
            bool closeOnPreview = false;
            var setting = new FullscreenOsdWindow.Setting(
                () => saved, value => value.ToString(), (value, direction) => value + direction,
                value =>
                {
                    preview = value;
                    if (closeOnPreview)
                    {
                        closeOnPreview = false;
                        menu!.Close();
                    }
                },
                value => { saved = value; return Task.CompletedTask; });
            Entry[] entries = [new("Video", Children: [new("Image", Children: [new("Brightness", Value: setting)])])];
            owner.Show();
            menu = new FullscreenOsdWindow(owner, layer, "Machine", null, entries, error => throw error, "Loading");
            menu.Show();
            var submenu = Assert.IsType<Decorator>(Assert.IsType<Grid>(menu.Content).Children[1]);

            await menu.NavigateAsync(OsdNavigation.Right);
            await menu.NavigateAsync(OsdNavigation.Right);
            var imagePanel = Assert.IsType<Border>(submenu.Child);
            await menu.NavigateAsync(OsdNavigation.Confirm);
            await menu.NavigateAsync(OsdNavigation.Right);
            Assert.Equal(11, preview);
            Assert.Equal(10, saved);

            await menu.NavigateAsync(OsdNavigation.Cancel);
            Assert.Equal(10, preview);
            Assert.Same(imagePanel, submenu.Child);

            await menu.NavigateAsync(OsdNavigation.Confirm);
            await menu.NavigateAsync(OsdNavigation.Left);
            await menu.NavigateAsync(OsdNavigation.Confirm);
            Assert.Equal(9, saved);
            Assert.Same(imagePanel, submenu.Child);

            await menu.NavigateAsync(OsdNavigation.Confirm);
            closeOnPreview = true;
            await menu.NavigateAsync(OsdNavigation.Right);
            Assert.Null(menu.Content);
            Assert.Equal(9, preview);
            Assert.Equal(9, saved);
        }
        finally
        {
            try { menu?.Close(); }
            finally { owner.Content = null; owner.Close(); }
        }
    });

    [Fact]
    public Task AHiddenParentClosesOnlyItsBranch() => sta.RunAsync(async () =>
    {
        bool visible = true;
        bool selectedSibling = false;
        var layer = new Grid();
        var owner = new Window { Content = layer, ShowInTaskbar = false, Width = 640, Height = 480 };
        FullscreenOsdWindow? menu = null;
        try
        {
            Entry[] entries =
            [
                new("Parent", FullHeight: true, Children:
                [
                    new("Branch", IsVisible: () => visible, FullHeight: true, Children:
                    [
                        new("Hide", Command: () => { visible = false; return Task.CompletedTask; })
                    ]),
                    new("Sibling", Command: () => { selectedSibling = true; return Task.CompletedTask; })
                ])
            ];
            owner.Show();
            menu = new FullscreenOsdWindow(owner, layer, "Machine", null, entries, error => throw error, "Loading");
            var submenu = Assert.IsType<Decorator>(Assert.IsType<Grid>(menu.Content).Children[1]);
            await menu.NavigateAsync(OsdNavigation.Right);
            var firstPanel = Assert.IsType<Border>(submenu.Child);
            await menu.NavigateAsync(OsdNavigation.Right);
            Assert.NotSame(firstPanel, submenu.Child);
            await menu.NavigateAsync(OsdNavigation.Confirm);
            Assert.Same(firstPanel, submenu.Child);
            await menu.NavigateAsync(OsdNavigation.Confirm);
            Assert.True(selectedSibling);
        }
        finally
        {
            try { menu?.Close(); }
            finally { owner.Content = null; owner.Close(); }
        }
    });
}
