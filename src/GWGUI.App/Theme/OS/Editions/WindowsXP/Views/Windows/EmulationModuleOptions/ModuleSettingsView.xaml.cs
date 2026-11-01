using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using GWGUI.App.Constants.Emulation;
using GWGUI.App.Localization.Extensions;
using GWGUI.App.Theme.Common;
using GWGUI.App.Views.Controls.Emulation.Options;
using GWGUI.Emulation;

namespace GWGUI.App.Theme.OS.Editions.WindowsXP.Views;

internal partial class ModuleSettingsView : UserControl, IDisposable
{
    private readonly EmulationModuleSettingsSection source;
    private readonly Action close;
    private readonly Func<Task>? initialize;
    private readonly ThemeBindingScope bindings = new();
    private readonly List<IDisposable> fieldViews = [];
    private readonly List<string> pendingFields = [];
    private bool rebuilding;
    private bool disposed;
    private WindowsXPResources? resourceScope;
    private readonly WindowsXPColorScheme scheme;
    private const double LabelWidth = 160;
    private const string FirmwareTitle = "Emulation.Firmware";
    private static readonly Thickness FieldMargin = new(0, 2, 0, 3);
    private static readonly Thickness LabelMargin = new(0, 0, 8, 0);
    private static readonly Thickness GroupMargin = new(0, 0, 0, 10);

    internal IReadOnlyList<string> PendingFields => pendingFields;

    internal ModuleSettingsView(EmulationModuleSettingsSection source, Action close, Func<Task>? initialize = null, WindowsXPColorScheme scheme = WindowsXPColorScheme.Blue)
    {
        this.source = source;
        this.close = close;
        this.initialize = initialize;
        this.scheme = scheme;
        try
        {
            InitializeComponent();
            resourceScope = new WindowsXPResources(this, scheme);
            bindings.OneWay(source.PresentationMachines, ItemsControl.ItemsSourceProperty, Machines, ItemsControl.ItemsSourceProperty);
            bindings.TwoWay(source.PresentationMachines, Selector.SelectedItemProperty, Machines, Selector.SelectedItemProperty);
            source.PresentationChanged += SourceChanged;
            Loaded += OnLoaded;
            Rebuild();
        }
        catch { Dispose(); throw; }
    }

    private void SourceChanged(object? sender, EventArgs args) { if (!disposed) Rebuild(); }
    private async void OnLoaded(object sender, RoutedEventArgs args)
    {
        Loaded -= OnLoaded;
        if (!disposed && initialize is not null) await initialize();
    }
    private void Rebuild()
    {
        if (disposed || rebuilding) return;
        rebuilding = true;
        try
        {
            var selected = source.PresentationTab;
            Categories.Items.Clear();
            foreach (var definition in EmulationMachineTabConstants.Definitions.Where(definition => source.PresentationTabs.Contains(definition.Tab)))
            {
                var item = new ListBoxItem { Tag = definition.Tab };
                item.SetBinding(ContentControl.ContentProperty, LocExtension.CreateBinding(definition.ResourceKey));
                Categories.Items.Add(item);
                if (definition.Tab == selected) Categories.SelectedItem = item;
            }
            if (Categories.SelectedItem is null && Categories.Items.Count > 0) Categories.SelectedIndex = 0;
            CreateAction.Visibility = source.CanCreateConfiguration ? Visibility.Visible : Visibility.Collapsed;
            RebuildFields();
        }
        finally { rebuilding = false; }
    }

    private void RebuildFields()
    {
        ReleaseFields();
        pendingFields.Clear();
        if (Categories.SelectedItem is not ListBoxItem { Tag: EmulationMachineTab tab }) return;
        var settings = source.PresentationSettings;
        if (tab == EmulationMachineTab.General && source.PresentationCore is { } core)
        {
            var coreView = new CoreManagementView(core, scheme);
            fieldViews.Add(coreView);
            Fields.Children.Add(coreView);
        }
        if (tab == EmulationMachineTab.Video)
        {
            var described = settings.Blocks.SelectMany(block => block.Fields).ToDictionary(field => field.Id, StringComparer.Ordinal);
            var endpoints = source.PresentationFields.Where(pair => described.ContainsKey(pair.Key))
                .ToDictionary(pair => pair.Value, pair => described[pair.Key]);
            var videoView = new VideoProcessingView(source.PresentationVideo, endpoints,
                field => LocExtension.GetForModule(source.PresentationModule, field.LabelResourceKey), source.RequestPresentationCommitAsync, scheme);
            fieldViews.Add(videoView);
            Fields.Children.Add(videoView);
            return;
        }
        if (tab == EmulationMachineTab.Storage && source.PresentationStorage is { } storage)
        {
            var storageView = new ModuleStorageView(storage, scheme);
            fieldViews.Add(storageView);
            Fields.Children.Add(storageView);
        }
        var blocks = settings.Blocks.Where(block => block.IsVisible && block.Tab == tab)
            .OrderBy(block => tab == EmulationMachineTab.Rom && block.TitleResourceKey == FirmwareTitle ? 0 : 1);
        foreach (var block in blocks)
        {
            var form = new StackPanel();
            foreach (var field in block.Fields.Where(field => EmulationModuleSettingsSection.IsFieldDisplayed(tab, field)))
            {
                if (tab == EmulationMachineTab.Controllers && source.PresentationInput is not null
                    && !field.IsGlobalControllerOption && (field.AppliesToControllerIds is not null || field.ControllerPortNumber is not null)) continue;
                if (!source.PresentationFields.TryGetValue(field.Id, out var endpoint))
                {
                    pendingFields.Add(field.Id);
                    continue;
                }
                var view = new SettingsFieldView(field, endpoint, source.RequestPresentationCommitAsync);
                fieldViews.Add(view);
                var row = new Grid { Margin = FieldMargin };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(LabelWidth) });
                row.ColumnDefinitions.Add(new ColumnDefinition());
                var label = new TextBlock { TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center, Margin = LabelMargin };
                label.SetBinding(TextBlock.TextProperty, LocExtension.CreateBinding(source.PresentationModule, field.LabelResourceKey));
                if (field.DetailedExplanationResourceKey is { } help) label.ToolTip = LocExtension.GetForModule(source.PresentationModule, help);
                else if (field.ExplanationResourceKey is { } shortHelp) label.ToolTip = LocExtension.GetForModule(source.PresentationModule, shortHelp);
                row.SetBinding(VisibilityProperty, new Binding(nameof(Visibility)) { Source = view });
                Grid.SetColumn(view, 1);
                row.Children.Add(label);
                row.Children.Add(view);
                form.Children.Add(row);
            }
            if (form.Children.Count == 0) continue;
            var group = new GroupBox { Content = form, Margin = GroupMargin };
            group.SetBinding(HeaderedContentControl.HeaderProperty, LocExtension.CreateBinding(source.PresentationModule, block.TitleResourceKey));
            Fields.Children.Add(group);
        }
        if (tab == EmulationMachineTab.Ram && source.PresentationMemoryTotal is { } total)
        {
            var summary = new TextBlock { Margin = FieldMargin, TextWrapping = TextWrapping.Wrap };
            var scope = new ThemeBindingScope();
            fieldViews.Add(scope);
            scope.OneWay(total, TextBlock.TextProperty, summary, TextBlock.TextProperty);
            Fields.Children.Add(summary);
        }
        if (tab is EmulationMachineTab.Keyboard or EmulationMachineTab.Mouse or EmulationMachineTab.Controllers
            && source.PresentationInput is { } input)
        {
            var scopedFields = settings.Blocks.Where(block => block.IsVisible && block.Tab == tab).SelectMany(block => block.Fields)
                .Where(field => !field.IsGlobalControllerOption && (field.AppliesToControllerIds is not null || field.ControllerPortNumber is not null)).ToArray();
            var inputView = new ModuleInputView(tab, input, scopedFields, source.PresentationFields,
                field => LocExtension.GetForModule(source.PresentationModule, field.LabelResourceKey), source.RequestPresentationCommitAsync, scheme);
            fieldViews.Add(inputView);
            Fields.Children.Add(inputView);
        }
    }

    private async void CategoryChanged(object sender, SelectionChangedEventArgs args)
    {
        if (disposed || rebuilding || Categories.SelectedItem is not ListBoxItem { Tag: EmulationMachineTab tab }) return;
        RebuildFields();
        await source.RequestPresentationTabAsync(tab);
    }
    private async void CreateClick(object sender, RoutedEventArgs args)
    {
        if (!disposed) await source.RequestCreateConfigurationAsync();
    }
    private void CloseClick(object sender, RoutedEventArgs args) { if (!disposed) close(); }

    private void ReleaseFields()
    {
        Exception? failure = null;
        try
        {
            foreach (var view in fieldViews)
                try { view.Dispose(); }
                catch (Exception error) { failure ??= error; }
        }
        finally { fieldViews.Clear(); if (Fields is not null) Fields.Children.Clear(); }
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        source.PresentationChanged -= SourceChanged;
        Loaded -= OnLoaded;
        try { bindings.Dispose(); }
        finally
        {
            try { ReleaseFields(); }
            finally
            {
                if (Categories is not null) { Categories.SelectionChanged -= CategoryChanged; Categories.Items.Clear(); }
                pendingFields.Clear();
                Content = null;
                try { resourceScope?.Dispose(); }
                finally { resourceScope = null; Resources.Clear(); }
            }
        }
    }
}
