using System.Windows;
using System.Runtime.ExceptionServices;
using System.Windows.Automation;
using System.Windows.Controls;
using GWGUI.App.Localization.Extensions;
using GWGUI.App.Theme.Common;
using GWGUI.App.Views.Controls.Emulation.Options;
using GWGUI.Emulation;

namespace GWGUI.App.Theme.OS.Editions.Windows31.Views;

internal partial class VideoProcessingView : UserControl, IDisposable
{
    private readonly EmulationVideoProcessingSettingsSection source;
    private readonly IReadOnlyDictionary<FrameworkElement, EmulationSettingsField> moduleFields;
    private readonly Func<EmulationSettingsField, string> label;
    private readonly Func<Task> commit;
    private readonly List<SettingsFieldView> fields = [];
    private ThemeBindingScope shaderBindings = new();
    private bool rebuilding;
    private bool disposed;
    private const double LabelWidth = 220;

    internal VideoProcessingView(EmulationVideoProcessingSettingsSection source,
        IReadOnlyDictionary<FrameworkElement, EmulationSettingsField> moduleFields,
        Func<EmulationSettingsField, string> label, Func<Task> commit)
    {
        this.source = source;
        this.moduleFields = moduleFields;
        this.label = label;
        this.commit = commit;
        try
        {
            InitializeComponent();
            source.PresentationChanged += SourceChanged;
            Rebuild();
        }
        catch { Dispose(); throw; }
    }

    private void SourceChanged(object? sender, EventArgs args) => Rebuild();
    private void Rebuild()
    {
        if (disposed || rebuilding) return;
        rebuilding = true;
        try
        {
            var selected = (Pages.SelectedItem as EmulationVideoProcessingSettingsSection.PresentationPage)?.Id;
            Pages.ItemsSource = source.PresentationPages;
            Pages.SelectedItem = Pages.Items.Cast<EmulationVideoProcessingSettingsSection.PresentationPage>().FirstOrDefault(page => page.Id == selected)
                ?? Pages.Items.Cast<object>().FirstOrDefault();
            shaderBindings.Dispose();
            shaderBindings = new ThemeBindingScope();
            if (source.PresentationShaderLoading is { } shader)
                shaderBindings.OneWay(shader, VisibilityProperty, ShaderLoading, VisibilityProperty);
            else ShaderLoading.Visibility = Visibility.Collapsed;
            RebuildFields();
        }
        finally { rebuilding = false; }
    }

    private void PageChanged(object sender, SelectionChangedEventArgs args)
    {
        if (!disposed && !rebuilding) RebuildFields();
    }

    private void RebuildFields()
    {
        ReleaseFields();
        if (Pages.SelectedItem is not EmulationVideoProcessingSettingsSection.PresentationPage page) return;
        WrapPanel? verticalFields = null;
        foreach (var endpoint in EmulationVideoProcessingSettingsSection.PresentationLeaves(page.Content, moduleFields))
        {
            var isModule = moduleFields.TryGetValue(endpoint, out var field);
            field ??= new EmulationSettingsField(AutomationProperties.GetAutomationId(endpoint), EmulationMachineTab.Video,
                page.Id, string.Empty, endpoint is TextBox ? EmulationSettingsEditor.Text : EmulationSettingsEditor.Information, null);
            var caption = isModule ? label(field) : EmulationVideoProcessingSettingsSection.PresentationLabel(endpoint);
            var view = new SettingsFieldView(field, endpoint, isModule ? commit : () => Task.CompletedTask,
                source.BeginPresentationSliderEdit, source.EndPresentationSliderEdit);
            fields.Add(view);
            if (endpoint is Slider { Orientation: Orientation.Vertical })
            {
                if (verticalFields is null)
                {
                    verticalFields = new WrapPanel { Margin = new Thickness(0, 4, 0, 10) };
                    Fields.Children.Add(verticalFields);
                }
                var column = new StackPanel { Width = 112, Margin = new Thickness(0, 0, 8, 12) };
                column.Children.Add(new TextBlock { Text = caption, TextWrapping = TextWrapping.Wrap,
                    TextAlignment = TextAlignment.Center, MinHeight = 32, Margin = new Thickness(0, 0, 0, 6) });
                column.Children.Add(view);
                verticalFields.Children.Add(column);
                continue;
            }
            verticalFields = null;
            var row = new Grid { Margin = new Thickness(0, 3, 0, 5) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(LabelWidth) });
            row.ColumnDefinitions.Add(new ColumnDefinition());
            var text = new TextBlock { Text = caption, TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0) };
            row.Children.Add(text);
            Grid.SetColumn(view, 1);
            row.Children.Add(view);
            row.SetBinding(VisibilityProperty, new System.Windows.Data.Binding(nameof(Visibility)) { Source = view });
            Fields.Children.Add(row);
        }
    }

    private void ReleaseFields()
    {
        Exception? failure = null;
        try
        {
            foreach (var field in fields)
                try { field.Dispose(); }
                catch (Exception error) { failure ??= error; }
        }
        finally { fields.Clear(); Fields.Children.Clear(); }
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        source.PresentationChanged -= SourceChanged;
        try { shaderBindings.Dispose(); }
        finally
        {
            try { ReleaseFields(); }
            finally { Pages.SelectionChanged -= PageChanged; Pages.ItemsSource = null; Content = null; }
        }
    }
}
