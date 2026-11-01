using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using GWGUI.App.Contracts.ViewModels.Visualization;
using GWGUI.App.Theme.OS.Editions.Windows31.Views;
using GWGUI.App.Views.Controls.Visualization;
using GWGUI.Tests.Application.TestInfrastructure;

namespace GWGUI.Tests.Interface.Themes;

[Collection("WPF")]
public sealed class Windows31InspectorViewTests(StaExecutionScenarios sta)
{
    [Fact]
    public Task InspectorShowsTheCompleteExistingModelAndFollowsSurfaceSelection()
        => sta.Run(() =>
        {
            var firstModel = Model("first", "track", 5);
            var secondModel = Model("second", "sector", 2);
            var first = new MediaInspectorPanel { SurfaceTitle = "surface 0", Model = firstModel };
            var second = new MediaInspectorPanel { SurfaceTitle = "surface 1", Model = secondModel };
            var original = new Grid();
            original.Children.Add(first);
            original.Children.Add(second);
            try
            {
                using var view = new InspectorView(first, second, () => { });
                var sections = Assert.IsType<ItemsControl>(view.FindName("Sections"));
                var title = Assert.IsType<TextBlock>(view.FindName("Title"));
                Assert.Equal(firstModel.Title, title.Text);
                Assert.Same(firstModel.Sections[0], Assert.Single(sections.Items.Cast<MediaInspectorSection>()));
                Assert.Equal(5, sections.Items.Cast<MediaInspectorSection>().SelectMany(item => item.Entries).Count());
                Assert.All(sections.Items.Cast<MediaInspectorSection>().SelectMany(item => item.Entries), entry => Assert.Equal("KiB", entry.Unit));
                var choices = Assert.IsType<ComboBox>(view.FindName("SurfaceChoice"));
                Assert.Equal("surface 0", Assert.IsType<ComboBoxItem>(choices.Items[0]).Content);
                choices.SelectedIndex = 1;
                Assert.Equal(secondModel.Title, title.Text);
                Assert.Equal("sector", Assert.IsType<TextBlock>(view.FindName("SelectedElement")).Text);
                second.SurfaceTitle = "renamed";
                Assert.Equal("renamed", Assert.IsType<ComboBoxItem>(choices.Items[1]).Content);
                Assert.Same(original, first.Parent);
                Assert.Same(original, second.Parent);
            }
            finally { original.Children.Clear(); }
        });

    [Fact]
    public Task HidingASurfaceSelectsAnAvailableOneAndClearingItsModelClearsTheDetails()
        => sta.Run(() =>
        {
            var first = new MediaInspectorPanel { Model = Model("first", null, 1) };
            var second = new MediaInspectorPanel { Model = Model("second", "selection", 1) };
            using var view = new InspectorView(first, second, () => { });
            var choices = Assert.IsType<ComboBox>(view.FindName("SurfaceChoice"));
            first.Visibility = Visibility.Collapsed;
            Assert.Equal(1, choices.SelectedIndex);
            Assert.False(choices.IsEnabled);
            Assert.Equal("second", Assert.IsType<TextBlock>(view.FindName("Title")).Text);
            second.Model = null;
            Assert.Empty(Assert.IsType<ItemsControl>(view.FindName("Sections")).Items);
            Assert.Equal(Visibility.Collapsed, Assert.IsType<TextBlock>(view.FindName("SelectedElement")).Visibility);
            second.Visibility = Visibility.Collapsed;
            Assert.Null(choices.SelectedItem);
            first.Visibility = Visibility.Visible;
            Assert.Equal(0, choices.SelectedIndex);
            first.Model = Model("replacement", "changed", 3);
            Assert.Equal("replacement", Assert.IsType<TextBlock>(view.FindName("Title")).Text);
            second.Visibility = Visibility.Visible;
            Assert.True(choices.IsEnabled);
        });

    [Fact]
    public Task ClosingTheInspectorDetachesModelsAndCommandsWithoutDestroyingTheSource()
        => sta.Run(() =>
        {
            var first = new MediaInspectorPanel { Model = Model("before", null, 1) };
            var second = new MediaInspectorPanel();
            var closeCalls = 0;
            var view = new InspectorView(first, second, () => closeCalls++);
            var close = Assert.IsType<Button>(view.FindName("CloseCommand"));
            var title = Assert.IsType<TextBlock>(view.FindName("Title"));
            try
            {
                close.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                Assert.Equal(1, closeCalls);
            }
            finally { view.Dispose(); }
            first.Model = Model("after", "new", 2);
            Assert.Equal("before", title.Text);
            Assert.Equal("after", first.Model.Title);
            close.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            Assert.Equal(1, closeCalls);
            Assert.Null(view.Content);
            Assert.Empty(Assert.IsType<ItemsControl>(view.FindName("Sections")).Items);
            view.Dispose();
        });

    private static MediaInspectorModel Model(string title, string? selected, int count) =>
        new(title, selected, [new MediaInspectorSection("details", string.Empty,
            Enumerable.Range(1, count).Select(index => new MediaInspectorEntry("field " + index, index.ToString(), "KiB")).ToArray())]);
}
