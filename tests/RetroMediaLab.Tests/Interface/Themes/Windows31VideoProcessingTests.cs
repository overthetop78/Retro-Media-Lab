using System.Windows;
using System.Windows.Automation;
using RetroMediaLab.VideoPresentation.Dictionaries;
using RetroMediaLab.VideoPresentation.Functions;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using RetroMediaLab.App.Theme.OS.Editions.Windows31.Views;
using RetroMediaLab.App.Views.Controls.Emulation.Options;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Enums;
using RetroMediaLab.VideoPresentation.Contracts;
using RetroMediaLab.VideoPresentation.Enums;
using ImageControls = RetroMediaLab.VideoPresentation.Enums.EmulationImageAdjustmentControls;
using RetroMediaLab.Tests.Application.TestInfrastructure;

namespace RetroMediaLab.Tests.Interface.Themes;

[Collection("WPF")]
public sealed class Windows31VideoProcessingTests(StaExecutionScenarios sta)
{
    private static readonly IReadOnlyDictionary<FrameworkElement, EmulationSettingsField> NoModuleFields =
        new Dictionary<FrameworkElement, EmulationSettingsField>();

    [Fact]
    public Task EveryDeclaredDisplayTechnologyRetainsAllItsFunctionalControls() => sta.Run(() =>
    {
        var source = new EmulationVideoProcessingSettingsSection();
        using var view = new VideoProcessingView(source, NoModuleFields, _ => throw new InvalidOperationException(), () => Task.CompletedTask);
        var pages = (ComboBox)view.FindName("Pages");
        foreach (var screen in Enum.GetValues<EmulationVideoScreenType>())
        {
            source.SetConfiguration(EmulationVideoProcessingConfigurationFunctions.WithScreenType(new(), screen));
            Assert.Equal(source.PresentationPages.Count, pages.Items.Count);
            var display = source.PresentationPages[0];
            var choices = EmulationVideoProcessingSettingsSection.PresentationLeaves(display.Content, NoModuleFields)
                .OfType<ComboBox>().Select(AutomationProperties.GetAutomationId).ToArray();
            Assert.Equal(2, source.PresentationPages.Count);
            Assert.Contains(nameof(EmulationVideoScreenType), choices);
            Assert.Contains(nameof(EmulationVideoAspectRatio), choices);
            Assert.Equal(screen == EmulationVideoScreenType.Host, choices.Contains(nameof(EmulationVideoSampling)));
            var image = source.PresentationPages.Single(page => page.Id == "Image");
            var imageFields = EmulationVideoProcessingSettingsSection.PresentationLeaves(image.Content, NoModuleFields)
                .Select(AutomationProperties.GetAutomationId).ToHashSet();
            Assert.Equal(screen == EmulationVideoScreenType.Host, imageFields.Contains(EmulationVideoProcessingCatalog.Debanding));
            Assert.Equal(screen == EmulationVideoScreenType.Host, imageFields.Contains(EmulationVideoProcessingCatalog.DetailRecovery));
            Assert.Equal(EmulationVideoProcessingConfigurationFunctions.SupportsDedithering(source.Configuration),
                imageFields.Contains(EmulationVideoProcessingCatalog.Dedithering));
            Assert.Equal(EmulationVideoProcessingConfigurationFunctions.SupportsDeinterlacing(source.Configuration),
                imageFields.Contains(EmulationVideoProcessingCatalog.Deinterlacing));
            var controls = EmulationVideoProcessingConfigurationFunctions.AdjustmentControls(source.Configuration);
            Assert.Equal(controls.HasFlag(ImageControls.Brightness), imageFields.Contains(EmulationVideoProcessingCatalog.Brightness));
            Assert.Equal(controls.HasFlag(ImageControls.Contrast), imageFields.Contains(EmulationVideoProcessingCatalog.Contrast));
            Assert.Equal(controls.HasFlag(ImageControls.Gamma), imageFields.Contains(EmulationVideoProcessingCatalog.Gamma));
            Assert.Equal(controls.HasFlag(ImageControls.Saturation), imageFields.Contains(EmulationVideoProcessingCatalog.Saturation));
            Assert.Equal(controls.HasFlag(ImageControls.Sharpness), imageFields.Contains(EmulationVideoProcessingCatalog.Sharpness));
            foreach (var page in source.PresentationPages)
            {
                pages.SelectedItem = pages.Items.Cast<EmulationVideoProcessingSettingsSection.PresentationPage>().Single(item => item.Id == page.Id);
                Assert.Equal(EmulationVideoProcessingSettingsSection.PresentationLeaves(page.Content, NoModuleFields).Count(),
                    LogicalDescendants((StackPanel)view.FindName("Fields")).OfType<SettingsFieldView>().Count());
                Assert.All(EmulationVideoProcessingSettingsSection.PresentationLeaves(page.Content, NoModuleFields),
                    leaf => Assert.False(string.IsNullOrWhiteSpace(EmulationVideoProcessingSettingsSection.PresentationLabel(leaf))));
            }
        }
        source.SetShaderLoading(true);
        Assert.Equal(Visibility.Visible, ((StackPanel)view.FindName("ShaderLoading")).Visibility);
        source.SetShaderLoading(false);
        Assert.Equal(Visibility.Collapsed, ((StackPanel)view.FindName("ShaderLoading")).Visibility);
        view.Dispose();
        source.SetConfiguration(new());
        Assert.Null(view.Content);
    });

    [Fact]
    public Task ChoicesRebuildTheProfileAndSliderEditsSaveOnlyAtTheirEnd() => sta.Run(() =>
    {
        var source = new EmulationVideoProcessingSettingsSection();
        var saves = 0;
        source.ConfigurationSaveRequested += (_, _) => saves++;
        using var view = new VideoProcessingView(source, NoModuleFields, _ => throw new InvalidOperationException(), () => Task.CompletedTask);
        var pages = (ComboBox)view.FindName("Pages");
        Arrange(view);
        var sampling = Descendants(view).OfType<ComboBox>().Single(combo =>
            combo.Items.Count == EmulationVideoProcessingCatalog.SamplingResourceKeys.Count);
        var previous = source.Configuration.Sampling;
        sampling.SelectedIndex = (sampling.SelectedIndex + 1) % sampling.Items.Count;
        Assert.NotEqual(previous, source.Configuration.Sampling);
        Assert.Equal(1, saves);
        pages.SelectedItem = pages.Items.Cast<EmulationVideoProcessingSettingsSection.PresentationPage>().Single(page => page.Id == "Image");
        Arrange(view);
        var slider = Descendants(view).OfType<Slider>().First();
        var original = source.Configuration.Adjustments.Brightness;
        slider.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
            { RoutedEvent = UIElement.PreviewMouseLeftButtonDownEvent });
        slider.Value = original + 1;
        slider.Value = original + 2;
        Assert.Equal(original + 2, source.Configuration.Adjustments.Brightness);
        Assert.Equal(1, saves);
        slider.RaiseEvent(new DragCompletedEventArgs(0, 0, false) { RoutedEvent = Thumb.DragCompletedEvent });
        Assert.Equal(2, saves);
        slider.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
            { RoutedEvent = UIElement.PreviewMouseLeftButtonDownEvent });
        slider.Value = original + 3;
        view.Dispose();
        Assert.Equal(3, saves);
    });

    private static IEnumerable<DependencyObject> LogicalDescendants(DependencyObject root)
    {
        foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
        {
            yield return child;
            foreach (var nested in LogicalDescendants(child)) yield return nested;
        }
    }

    private static void Arrange(FrameworkElement view)
    {
        view.Measure(new Size(700, 600));
        view.Arrange(new Rect(0, 0, 700, 600));
        view.UpdateLayout();
    }
    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        for (var index = 0; index < System.Windows.Media.VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(root, index);
            yield return child;
            foreach (var nested in Descendants(child)) yield return nested;
        }
    }
}
