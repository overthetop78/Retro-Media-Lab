using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.Options.Controllers;
using RetroMediaLab.App.Theme.OS.Editions.Windows31.Views;
using RetroMediaLab.App.ViewModels.Options;
using RetroMediaLab.App.Views.Controls.Options;
using RetroMediaLab.Infrastructure.Settings;
using RetroMediaLab.Tests.Application.TestInfrastructure;

namespace RetroMediaLab.Tests.Interface.Themes;

[Collection("WPF")]
public sealed class Windows31GeneralOptionsTests(StaExecutionScenarios sta)
{
    [Fact]
    public Task NewFieldsKeepTagControllerPersistenceHistoryAndExamplesWithoutBorrowingBaseControls() => sta.Run(() =>
    {
        var source = new OptionsGeneralSection();
        var settings = new AppSettings();
        var initializing = true;
        var saves = 0;
        var controller = new TagOptionsController(source, settings, () => initializing,
            () => { saves++; return Task.CompletedTask; },
            (key, args) => args.Length == 0 ? LocExtension.Get(key) : string.Format(LocExtension.Get(key), args));
        initializing = false;
        var parent = source.TagPattern.Parent;
        using var view = new GeneralOptionsView(source);
        var pattern = Field<TextBox>(view, "TagPattern");
        var tags = Field<CheckBox>(view, "UseTags");
        var preview = Field<TextBlock>(view, "Preview");
        Assert.NotSame(source.TagPattern, pattern);
        Assert.Same(parent, source.TagPattern.Parent);
        tags.IsChecked = !settings.Conversion.AddTags;
        Assert.Equal(tags.IsChecked == true, settings.Conversion.AddTags);
        Assert.True(saves > 0);
        pattern.Text = "{NAME}-{FAMILY}";
        Assert.Equal(pattern.Text, source.TagPattern.Text);
        Assert.Equal(source.TagPreview.Text, preview.Text);
        pattern.RaiseEvent(new KeyboardFocusChangedEventArgs(Keyboard.PrimaryDevice, Environment.TickCount, pattern, tags)
            { RoutedEvent = Keyboard.LostKeyboardFocusEvent });
        Assert.Contains(pattern.Text, settings.Conversion.RecentCustomTagPatterns);
        var recent = Field<ListBox>(view, "RecentPatterns");
        Assert.Same(source.RecentTagPatternsList.ItemsSource, recent.ItemsSource);
        var remembered = pattern.Text;
        pattern.Text = "{NAME}-other";
        ((TabControl)view.Content).SelectedIndex = 1;
        view.Measure(new Size(850, 500));
        view.Arrange(new Rect(0, 0, 850, 500));
        view.UpdateLayout();
        recent.SelectedIndex = 0;
        var item = (ListBoxItem)recent.ItemContainerGenerator.ContainerFromIndex(0);
        recent.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
            { RoutedEvent = Control.MouseDoubleClickEvent, Source = item });
        Assert.Equal(remembered, pattern.Text);
        var presets = Field<ComboBox>(view, "TagPresets");
        presets.SelectedIndex = 0;
        Assert.Equal(Assert.IsType<TagPresetOption>(presets.SelectedItem).Pattern, pattern.Text);
        Field<Button>(view, "NextAction").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.Equal(source.TagPreview.Text, preview.Text);
        view.Dispose();
        var last = source.TagPattern.Text;
        pattern.Text = "detached";
        Assert.Equal(last, source.TagPattern.Text);
        Assert.Null(view.Content);
    });

    [Fact]
    public Task FolderLanguageAndEditingActionsUseTheOriginalEndpoints() => sta.Run(() =>
    {
        var source = new OptionsGeneralSection();
        var choices = new[] { "fr", "en" };
        source.Languages.ItemsSource = choices;
        source.Languages.SelectedItem = choices[0];
        var browsed = 0;
        var folderSaved = 0;
        RoutedEventHandler browse = (_, _) => browsed++;
        KeyboardFocusChangedEventHandler saved = (_, _) => folderSaved++;
        source.BrowseImagesFolderRequested += browse;
        source.AutoSaveTextEditingFinished += saved;
        using var view = new GeneralOptionsView(source);
        try
        {
            var folder = Field<TextBox>(view, "ImagesFolder");
            folder.Text = @"F:\Retro\Archives";
            Assert.Equal(folder.Text, source.ImagesFolder.Text);
            Field<ComboBox>(view, "Languages").SelectedIndex = 1;
            Assert.Equal(choices[1], source.Languages.SelectedItem);
            Field<Button>(view, "BrowseAction").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            folder.RaiseEvent(new KeyboardFocusChangedEventArgs(Keyboard.PrimaryDevice, Environment.TickCount, folder, view)
                { RoutedEvent = Keyboard.LostKeyboardFocusEvent });
            Assert.Equal((1, 1), (browsed, folderSaved));
            view.Dispose();
            Field<Button>(view, "BrowseAction").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.Equal(1, browsed);
        }
        finally
        {
            source.BrowseImagesFolderRequested -= browse;
            source.AutoSaveTextEditingFinished -= saved;
        }
    });

    private static T Field<T>(FrameworkElement view, string name) => Assert.IsType<T>(view.FindName(name));
}
