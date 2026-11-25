using RetroMediaLab.App.Constants.Localization;
using RetroMediaLab.App.Constants.Controls.Visual;
using RetroMediaLab.App.Constants.Views.Emulation;
using RetroMediaLab.App.Functions.Views.Emulation.Settings;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.Emulation.Constants;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Dictionaries;
using RetroMediaLab.Emulation.Enums;
using RetroMediaLab.Emulation.Functions;
using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace RetroMediaLab.App.Views.Controls.Emulation.Options;

internal sealed partial class EmulationVideoProcessingSettingsSection : UserControl
{
    private FrameworkElement CreateDisplayAndRendering()
    {
        var processing = FramedGroup(EmulationResourceKeys.VideoRetroMediaLabProcessing, CreateSelectors());
        if (_displaySettings is null) return processing;

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        processing.Margin = new Thickness(0, 0, 9, 0);
        processing.VerticalAlignment = VerticalAlignment.Top;
        grid.Children.Add(processing);
        var hardware = FramedGroup("Emulation.Video.Hardware", _displaySettings);
        hardware.Margin = new Thickness(9, 0, 0, 0);
        hardware.VerticalAlignment = VerticalAlignment.Top;
        Grid.SetColumn(hardware, 1);
        grid.Children.Add(hardware);
        return grid;
    }

    private FrameworkElement CreateImageTab()
    {
        var panel = new StackPanel();
        panel.Children.Add(CreateImageGroups());
        if (CreateTechnologyGroup() is { } technology)
        {
            technology.Margin = new Thickness(0, 12, 0, 0);
            panel.Children.Add(technology);
        }
        return panel;
    }

    private FrameworkElement CreateImageGroups()
    {
        var screen = EmulationVideoProcessingConfigurationFunctions.ScreenType(_configuration);
        var adjustments = EmulationVideoProcessingConfigurationFunctions.ImageAdjustments(_configuration, screen);
        return FramedSection(EmulationImageParametersSettingsBlock.Create(
            adjustments, EmulationVideoProcessingConfigurationFunctions.AdjustmentControls(_configuration),
            update => SetAdjustments(screen, update), _configuration.Restoration,
            EmulationVideoProcessingConfigurationFunctions.UsesHostDisplay(_configuration),
            EmulationVideoProcessingConfigurationFunctions.SupportsDedithering(_configuration),
            EmulationVideoProcessingConfigurationFunctions.SupportsDeinterlacing(_configuration),
            update => SetRestoration(update(_configuration.Restoration))));
    }

    private FrameworkElement? CreateTechnologyGroup()
    {
        var technology = EmulationVideoProcessingConfigurationFunctions.ScreenType(_configuration) switch
        {
            EmulationVideoScreenType.Crt => CreateCrtSignalControls(),
            EmulationVideoScreenType.Lcd => CreateLcdSignalControls(led: false),
            EmulationVideoScreenType.LedBacklitLcd => CreateLcdSignalControls(led: true),
            EmulationVideoScreenType.Oled => CreateOledControls(),
            EmulationVideoScreenType.Plasma => CreatePlasmaPanel(),
            EmulationVideoScreenType.Projection => CreateProjectionPanel(),
            EmulationVideoScreenType.Vector => CreateVectorPanel(),
            EmulationVideoScreenType.LedMatrix => CreateLedMatrixPanel(),
            EmulationVideoScreenType.DotMatrix => CreateDotMatrixPanel(),
            EmulationVideoScreenType.Vfd => CreateVfdPanel(),
            EmulationVideoScreenType.EPaper => CreateEPaperPanel(),
            EmulationVideoScreenType.SegmentDisplay => CreateSegmentDisplayPanel(),
            _ => null
        };
        if (technology is null)
            return null;

        AutomationProperties.SetAutomationId(technology, EmulationVideoSettingsLayoutConstants.VideoTechnologyParametersAutomationId);
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        var frame = FramedSection(technology);
        frame.Margin = new Thickness(0, 0, 6, 0);
        grid.Children.Add(frame);
        return grid;
    }

    private static FrameworkElement SideBySideGroups(FrameworkElement left, FrameworkElement right)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        left.Margin = new Thickness(0, 0, 9, 0);
        right.Margin = new Thickness(9, 0, 0, 0);
        grid.Children.Add(left);
        Grid.SetColumn(right, 1);
        grid.Children.Add(right);
        return grid;
    }

    private FrameworkElement CreateSelectors()
    {
        var panel = new StackPanel();
        _shaderLoadingIndicator = CreateShaderLoadingIndicator();
        panel.Children.Add(_shaderLoadingIndicator);

        var render = new StackPanel();
        if (_rendererChoice is not null) render.Children.Add(_rendererChoice);
        if (EmulationVideoProcessingConfigurationFunctions.UsesHostDisplay(_configuration))
            render.Children.Add(ChoiceField(EmulationResourceKeys.VideoSampling,
                EmulationVideoProcessingCatalog.SamplingResourceKeys, _configuration.Sampling,
                value => Update(configuration => configuration with { Sampling = value })));

        var screen = new StackPanel();
        screen.Children.Add(ChoiceField(EmulationResourceKeys.VideoTechnology,
            EmulationVideoProcessingCatalog.ScreenTypeResourceKeys,
            EmulationVideoProcessingConfigurationFunctions.ScreenType(_configuration), value =>
            {
                Update(configuration => EmulationVideoProcessingConfigurationFunctions.WithScreenType(configuration, value));
                RebuildContent();
            }));
        var aspectRatios = EmulationVideoProcessingConfigurationFunctions.AspectRatioChoices(
            EmulationVideoProcessingConfigurationFunctions.ScreenType(_configuration));
        var aspectRatioLabels = aspectRatios.ToDictionary(value => value, value =>
        {
            if (value == EmulationVideoAspectRatio.Original) return LocExtension.Get("Emulation.Video.AspectRatio.Original");
            var (width, height) = EmulationVideoProcessingConfigurationFunctions.AspectRatioDimensions(value);
            return FormattableString.Invariant($"{width}/{height}");
        });
        screen.Children.Add(ChoiceField("Emulation.Video.AspectRatio", aspectRatioLabels,
            EmulationVideoProcessingConfigurationFunctions.EffectiveAspectRatio(_configuration),
            value => Update(configuration => configuration with { AspectRatio = value }),
            translateChoices: false));
        panel.Children.Add(SideBySideGroups(render, screen));
        return panel;
    }

    private FrameworkElement CreateShaderLoadingIndicator()
    {
        var loading = new StackPanel
        {
            Margin = new Thickness(0, 0, 0, 12),
            Visibility = _shaderLoading ? Visibility.Visible : Visibility.Collapsed
        };
        loading.Children.Add(new TextBlock
        {
            Text = LocExtension.Get(EmulationResourceKeys.VideoShaderLoading),
            FontWeight = FontWeights.SemiBold
        });
        loading.Children.Add(new ProgressBar
        {
            IsIndeterminate = true,
            Height = 4,
            Margin = new Thickness(0, 7, 0, 0)
        });
        AutomationProperties.SetAutomationId(loading, EmulationVideoSettingsLayoutConstants.VideoShaderLoadingAutomationId);
        return loading;
    }

    private static TabItem Tab(string id, string headerResourceKey, FrameworkElement content, double maximumWidth = EmulationVideoSettingsLayoutConstants.TabContentMaximumWidth, bool compactFields = true, bool frameContent = true)
    {
        if (compactFields) content = CompactSection(content);
        var container = new Border
        {
            Child = content,
            MaxWidth = maximumWidth,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(12)
        };
        if (frameContent)
            container.SetResourceReference(StyleProperty, ControlVisualConstants.CardStyleResource);
        var scroller = new ScrollViewer
        {
            Content = container,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            HorizontalContentAlignment = HorizontalAlignment.Stretch
        };
        return new TabItem
        {
            Header = LocExtension.Get(headerResourceKey),
            Tag = id,
            Content = scroller
        };
    }

    private static Border FramedSection(FrameworkElement content)
    {
        var frame = new Border { Child = content };
        frame.SetResourceReference(StyleProperty, ControlVisualConstants.CardStyleResource);
        return frame;
    }

    internal static Border FramedGroup(string titleResourceKey, FrameworkElement content, bool compact = false)
    {
        var panel = new StackPanel();
        panel.Children.Add(new TextBlock
        {
            Text = LocExtension.Get(titleResourceKey),
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 10)
        });
        panel.Children.Add(compact ? CompactFields(content) : content);
        return FramedSection(panel);
    }

    private static FrameworkElement CompactFields(FrameworkElement content)
    {
        if (content is not StackPanel panel || panel.Children.Count < 2) return content;
        var children = panel.Children.Cast<UIElement>().ToArray();
        panel.Children.Clear();
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        for (var index = 0; index < children.Length; index++)
        {
            var child = children[index];
            if (child is FrameworkElement element)
                element.Margin = new Thickness(index % 2 == 0 ? 0 : 8, 0, index % 2 == 0 ? 8 : 0, 0);
            Grid.SetColumn(child, index % 2);
            Grid.SetRow(child, index / 2);
            grid.Children.Add(child);
        }
        for (var row = 0; row < (children.Length + 1) / 2; row++)
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        return grid;
    }

    private static FrameworkElement CompactSection(FrameworkElement content)
    {
        if (content is not StackPanel section || section.Children.Count < 2) return content;
        var children = section.Children.Cast<UIElement>().ToArray();
        section.Children.Clear();
        section.Children.Add(children[0]);
        var fields = new Grid();
        fields.ColumnDefinitions.Add(new ColumnDefinition());
        fields.ColumnDefinitions.Add(new ColumnDefinition());
        var columns = new[] { new StackPanel(), new StackPanel() };
        for (var index = 1; index < children.Length; index++)
        {
            var field = children[index];
            var cell = index - 1;
            if (field is FrameworkElement element)
                element.Margin = new Thickness(cell % 2 == 0 ? 0 : 10, 2, cell % 2 == 0 ? 10 : 0, 6);
            columns[cell % 2].Children.Add(field);
        }
        fields.Children.Add(columns[0]);
        Grid.SetColumn(columns[1], 1);
        fields.Children.Add(columns[1]);
        section.Children.Add(fields);
        return section;
    }

}
