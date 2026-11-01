using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.Theme.Common;
using RetroMediaLab.App.Views.Controls.Emulation.Input;
using RetroMediaLab.Emulation;

namespace RetroMediaLab.App.Theme.OS.Editions.Windows95.Views;

internal partial class ControllerPortView : UserControl, IDisposable
{
    private readonly EmulationControllerPortEditor source;
    private readonly ThemeBindingScope bindings = new();
    private readonly List<IDisposable> parts = [];
    private readonly List<SettingsFieldView> fields = [];
    private readonly IReadOnlyList<EmulationSettingsField> definitions;
    private readonly IReadOnlyDictionary<string, FrameworkElement> endpoints;
    private readonly Func<EmulationSettingsField, string> label;
    private readonly Func<Task> commit;
    private bool disposed;
    private const double LabelWidth = 140;

    internal ControllerPortView(EmulationControllerPortEditor source, IReadOnlyList<EmulationSettingsField> definitions,
        IReadOnlyDictionary<string, FrameworkElement> endpoints, Func<EmulationSettingsField, string> label, Func<Task> commit)
    {
        this.source = source;
        this.definitions = definitions;
        this.endpoints = endpoints;
        this.label = label;
        this.commit = commit;
        try
        {
            InitializeComponent();
            TypeLabel.Text = LocExtension.Get(source.IsUsb ? "Emulation.Controller.PeripheralType" : "Emulation.Controller.Type");
            Choice(source.Type, Type);
            Choice(source.Visual, Visual);
            Visual.DisplayMemberPath = nameof(RetroMediaLab.App.Contracts.Emulation.Controllers.EmulationControllerVisualChoiceView.Value);
            bindings.OneWay(source.Availability, TextBlock.TextProperty, Availability, TextBlock.TextProperty);
            bindings.OneWay(source.Availability, VisibilityProperty, Availability, VisibilityProperty);
            var editor = new InputBindingsView(source.Bindings);
            parts.Add(editor);
            BindingsHost.Content = editor;
            var drawing = new InputDrawingView(source.Visualizer, source.Bindings, control =>
            {
                if (source.Visualizer.VisualCommandIds?.TryGetValue(control, out var commandId) == true) editor.RequestCapture(commandId);
            });
            parts.Add(drawing);
            DrawingHost.Content = drawing;
            var outputs = new ControllerOutputsView(source.Outputs);
            parts.Add(outputs);
            OutputsHost.Content = outputs;
            source.Type.SelectionChanged += SourceSelectionChanged;
            source.Visual.SelectionChanged += SourceSelectionChanged;
            RebuildSettings();
        }
        catch { Dispose(); throw; }
    }

    private void Choice(ComboBox source, ComboBox target)
    {
        bindings.OneWay(source, ItemsControl.DisplayMemberPathProperty, target, ItemsControl.DisplayMemberPathProperty);
        bindings.OneWay(source, Selector.SelectedValuePathProperty, target, Selector.SelectedValuePathProperty);
        bindings.OneWay(source, ItemsControl.ItemsSourceProperty, target, ItemsControl.ItemsSourceProperty);
        bindings.TwoWay(source, Selector.SelectedItemProperty, target, Selector.SelectedItemProperty);
        bindings.OneWay(source, IsEnabledProperty, target, IsEnabledProperty);
        bindings.OneWay(source, VisibilityProperty, target, VisibilityProperty);
    }
    private void SourceSelectionChanged(object sender, SelectionChangedEventArgs args) { if (!disposed) RebuildSettings(); }
    private void RebuildSettings()
    {
        ReleaseSettings();
        var selectedId = source.SelectedControllerChoice?.Id;
        foreach (var definition in definitions.Where(field => field.IsVisible
            && (field.ControllerPortNumber is not { } number || number == source.Number)
            && (field.AppliesToControllerIds is not { } types || selectedId is not null && types.Contains(selectedId))))
        {
            if (!endpoints.TryGetValue(definition.Id, out var endpoint)) throw new InvalidOperationException(definition.Id);
            var view = new SettingsFieldView(definition, endpoint, commit);
            fields.Add(view);
            var row = new Grid { Margin = new Thickness(0, 3, 0, 5) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(LabelWidth) });
            row.ColumnDefinitions.Add(new ColumnDefinition());
            row.Children.Add(new TextBlock { Text = label(definition), TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) });
            row.SetBinding(VisibilityProperty, new System.Windows.Data.Binding(nameof(Visibility)) { Source = view });
            Grid.SetColumn(view, 1);
            row.Children.Add(view);
            Settings.Children.Add(row);
        }
    }
    private void ReleaseSettings()
    {
        Exception? failure = null;
        try
        {
            foreach (var field in fields)
                try { field.Dispose(); }
                catch (Exception error) { failure ??= error; }
        }
        finally { fields.Clear(); Settings.Children.Clear(); }
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        source.Type.SelectionChanged -= SourceSelectionChanged;
        source.Visual.SelectionChanged -= SourceSelectionChanged;
        Exception? failure = null;
        try { bindings.Dispose(); }
        finally
        {
            try
            {
                try { ReleaseSettings(); }
                catch (Exception error) { failure ??= error; }
                foreach (var part in parts)
                    try { part.Dispose(); }
                    catch (Exception error) { failure ??= error; }
            }
            finally
            {
                parts.Clear();
                BindingsHost.Content = null;
                DrawingHost.Content = null;
                OutputsHost.Content = null;
                Type.ItemsSource = null;
                Visual.ItemsSource = null;
                Content = null;
                Resources.Clear();
            }
        }
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
