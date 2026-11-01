using System.Windows;
using System.Windows.Controls;
using GWGUI.App.Controllers.Emulation.Input;
using GWGUI.App.Theme.Common;
using GWGUI.App.Views.Controls.Emulation.Input;
using GWGUI.App.Views.Controls.Options;
using GWGUI.Emulation.Enums;

namespace GWGUI.App.Theme.Orbit.Views;

internal sealed class InputDrawingView : UserControl, IDisposable
{
    private readonly ControllerVisualizer source;
    private readonly ControllerVisualizer drawing = new() { ClipToBounds = true };
    private readonly EmulationBindingVisualizationController visualization;
    private readonly Action<EmulationControllerVisualControl> capture;
    private readonly ThemeBindingScope bindings = new();
    private bool disposed;

    internal InputDrawingView(ControllerVisualizer source, InputBindingEditor editor,
        Action<EmulationControllerVisualControl> capture)
    {
        this.source = source;
        this.capture = capture;
        visualization = new EmulationBindingVisualizationController(editor, drawing);
        try
        {
            Content = drawing;
            source.PresentationChanged += SourceChanged;
            drawing.VisualZoneClicked += ZoneClicked;
            Loaded += LoadedView;
            Unloaded += UnloadedView;
            IsVisibleChanged += VisibilityChanged;
            bindings.OneWay(source, VisibilityProperty, this, VisibilityProperty);
            bindings.OneWay(source, IsEnabledProperty, this, IsEnabledProperty);
            SourceChanged(this, EventArgs.Empty);
        }
        catch { Dispose(); throw; }
    }

    private void SourceChanged(object? sender, EventArgs args)
    {
        if (disposed) return;
        drawing.Model = source.Model;
        drawing.State = source.State;
        drawing.VisualState = source.VisualState;
        drawing.ArtworkProfile = source.ArtworkProfile;
        drawing.VisualCommandIds = source.VisualCommandIds;
    }
    private void ZoneClicked(EmulationControllerVisualControl control) { if (!disposed) capture(control); }
    private void LoadedView(object sender, RoutedEventArgs args) => UpdateMonitoring();
    private void UnloadedView(object sender, RoutedEventArgs args) => visualization.Stop();
    private void VisibilityChanged(object sender, DependencyPropertyChangedEventArgs args) => UpdateMonitoring();
    private void UpdateMonitoring()
    {
        if (disposed) return;
        if (IsLoaded && IsVisible) visualization.Start();
        else visualization.Stop();
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        try { visualization.Stop(); }
        finally
        {
            try { bindings.Dispose(); }
            finally
            {
                source.PresentationChanged -= SourceChanged;
                drawing.VisualZoneClicked -= ZoneClicked;
                Loaded -= LoadedView;
                Unloaded -= UnloadedView;
                IsVisibleChanged -= VisibilityChanged;
                drawing.ArtworkProfile = null;
                drawing.VisualCommandIds = null;
                drawing.State = null;
                drawing.VisualState = null;
                Content = null; Resources.Clear();
            }
        }
    }
}
