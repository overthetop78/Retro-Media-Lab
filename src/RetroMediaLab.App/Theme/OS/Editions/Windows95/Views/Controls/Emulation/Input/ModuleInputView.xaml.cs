using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using RetroMediaLab.App.Controllers.Emulation.Input;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.Views.Controls.Emulation.Input;
using RetroMediaLab.Emulation;

namespace RetroMediaLab.App.Theme.OS.Editions.Windows95.Views;

internal partial class ModuleInputView : UserControl, IDisposable
{
    private sealed record Port(EmulationControllerPortEditor Editor, string Label);
    private readonly List<IDisposable> parts = [];
    private ControllerPortView? portView;
    private readonly IReadOnlyList<EmulationSettingsField> definitions;
    private readonly IReadOnlyDictionary<string, FrameworkElement> endpoints;
    private readonly Func<EmulationSettingsField, string> label;
    private readonly Func<Task> commit;
    private bool disposed;

    internal ModuleInputView(EmulationMachineTab tab, EmulationInputSettingsController source,
        IReadOnlyList<EmulationSettingsField> definitions, IReadOnlyDictionary<string, FrameworkElement> endpoints,
        Func<EmulationSettingsField, string> label, Func<Task> commit)
    {
        this.definitions = definitions;
        this.endpoints = endpoints;
        this.label = label;
        this.commit = commit;
        try
        {
            InitializeComponent();
            Ports.Visibility = tab == EmulationMachineTab.Controllers ? Visibility.Visible : Visibility.Collapsed;
            KeyboardArtwork.Visibility = Visibility.Collapsed;
            DrawingHost.Visibility = Visibility.Collapsed;
            Hint.Visibility = Visibility.Collapsed;
            if (tab == EmulationMachineTab.Controllers)
            {
                Ports.ItemsSource = source.PresentationPorts.Select(port => new Port(port,
                    port.DisplayName ?? LocExtension.Get("Emulation.Controller.Port", port.DisplayNumber ?? port.Number))).ToArray();
                if (Ports.Items.Count > 0) Ports.SelectedIndex = 0;
            }
            else if ((tab == EmulationMachineTab.Keyboard ? source.PresentationKeyboard : source.PresentationMouse) is { } editor)
            {
                var view = new InputBindingsView(editor) { Height = 300 };
                parts.Add(view);
                EditorHost.Content = view;
                if (tab == EmulationMachineTab.Keyboard)
                {
                    KeyboardArtwork.Source = source.PresentationKeyboardArtwork;
                    KeyboardArtwork.Visibility = KeyboardArtwork.Source is null ? Visibility.Collapsed : Visibility.Visible;
                    Hint.Text = source.PresentationKeyboardHint;
                    Hint.Visibility = Hint.Text.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
                }
                else if (source.PresentationMouseDrawing is { } original)
                {
                    var drawing = new InputDrawingView(original, editor, control =>
                    {
                        if (original.VisualCommandIds?.TryGetValue(control, out var commandId) == true) view.RequestCapture(commandId);
                    }) { Height = 130 };
                    parts.Add(drawing);
                    DrawingHost.Content = drawing;
                    DrawingHost.Visibility = Visibility.Visible;
                }
            }
        }
        catch { Dispose(); throw; }
    }

    private void PortChanged(object sender, SelectionChangedEventArgs args)
    {
        if (disposed) return;
        try { portView?.Dispose(); }
        finally { portView = null; EditorHost.Content = null; }
        if (Ports.SelectedItem is not Port port) return;
        portView = new ControllerPortView(port.Editor, definitions, endpoints, label, commit);
        EditorHost.Content = portView;
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        Ports.SelectionChanged -= PortChanged;
        Exception? failure = null;
        try
        {
            try { portView?.Dispose(); }
            catch (Exception error) { failure ??= error; }
            foreach (var part in parts)
                try { part.Dispose(); }
                catch (Exception error) { failure ??= error; }
        }
        finally
        {
            portView = null;
            parts.Clear();
            Ports.ItemsSource = null;
            KeyboardArtwork.Source = null;
            DrawingHost.Content = null;
            EditorHost.Content = null;
            Content = null;
            Resources.Clear();
        }
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
