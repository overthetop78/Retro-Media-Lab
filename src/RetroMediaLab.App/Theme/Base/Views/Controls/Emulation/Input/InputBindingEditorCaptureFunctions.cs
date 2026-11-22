using RetroMediaLab.App.Constants.Input.Windows;
using RetroMediaLab.App.Controllers.Emulation.Input;
using RetroMediaLab.App.Contracts.Input;
using RetroMediaLab.App.Enums.Input;
using RetroMediaLab.App.Functions.Input.Mouse;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.ViewModels.Input;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;


namespace RetroMediaLab.App.Views.Controls.Emulation.Input;

public partial class InputBindingEditor
{
    private void AssignClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: InputBindingRow row } button)
            SelectAndStartCapture(row.Id, button);
    }

    private void BeginCapture(InputBindingRow row, Button? button)
    {
        CancelCapture();
        _capture = new InputBindingCapture(row.Id, _bindings.CaptureSources, _bindings.PrefixKeyboardSource);
        _captureRow = row;
        _captureButton = button;
        _captureButtonContent = button?.Content;
        bool started = false;
        try
        {
            if (button is not null)
            {
                button.Content = presentationCaptureCaption?.Invoke() ?? new TextBlock
                {
                    Text = LocExtension.Get("Emulation.Input.Press"),
                    FontSize = 11,
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.NoWrap
                };
                button.Focus();
            }
            else BindingsList.Focus();
            if (_capture is not null) _controllerCaptureTimer.Start();
            started = true;
        }
        finally { if (!started) CancelCapture(); }
    }

    private void CaptureKeyDown(object sender, KeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (CaptureKeyboardKeyDown(key, Keyboard.Modifiers, e.Timestamp)) e.Handled = true;
    }

    internal bool CaptureKeyboardKeyDown(Key key, ModifierKeys modifiers, int? timestamp = null)
    {
        if (_capture is not { } capture || !_bindings.CaptureSources.HasFlag(InputCaptureSources.Keyboard)) return false;
        if (capture.PressKey(key, modifiers, timestamp)) return true;
        CancelCapture();
        return false;
    }

    private void CaptureKeyUp(object sender, KeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (CaptureKeyboardKeyUp(key)) e.Handled = true;
    }

    internal bool CaptureKeyboardKeyUp(Key key)
    {
        if (_capture is not { } capture || !_bindings.CaptureSources.HasFlag(InputCaptureSources.Keyboard)) return false;
        capture.ReleaseKey(key);
        CompleteReleasedCapture();
        return true;
    }

    internal bool CapturePhysicalSourceDown(string source) => _capture?.PressSource(source) == true;

    internal bool CapturePhysicalSourceUp(string source)
    {
        if (_capture is not { } capture) return false;
        capture.ReleaseSource(source);
        CompleteReleasedCapture();
        return true;
    }

    private void CompleteReleasedCapture()
    {
        if (_captureRow is not { } row || _capture?.ReadCompletedBinding() is not { } binding) return;
        row.Binding = binding;
        FinishCapture();
    }

    private void CaptureMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (_captureRow is null || !_bindings.CaptureSources.HasFlag(InputCaptureSources.Mouse)) return;
        var button = MouseInputFunctions.BindingSource(e.ChangedButton);
        if (button is null) return;
        CapturePhysicalSourceDown(button);
        e.Handled = true;
    }

    private void CaptureMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_captureRow is null || !_bindings.CaptureSources.HasFlag(InputCaptureSources.Mouse)) return;
        var source = MouseInputFunctions.BindingSource(e.ChangedButton);
        if (source is not null && CapturePhysicalSourceUp(source)) e.Handled = true;
    }

    private void CaptureMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (_captureRow is null || !_bindings.CaptureSources.HasFlag(InputCaptureSources.Mouse)
            || MouseInputFunctions.WheelBindingSource(e.Delta) is not { } source) return;
        CapturePhysicalSourceDown(source);
        CapturePhysicalSourceUp(source);
        e.Handled = true;
    }

    private void AttachWindowHook(FrameworkElement? presentation = null)
    {
        if (_windowSource is not null || Window.GetWindow(presentation ?? this) is not Window window) return;
        _windowSource = PresentationSource.FromVisual(window) as HwndSource;
        _windowSource?.AddHook(WindowMessageHook);
        _captureWindow = window;
        _captureWindow.Deactivated += CaptureWindowDeactivated;
    }

    private void DetachWindowHook()
    {
        if (_captureWindow is { } window) window.Deactivated -= CaptureWindowDeactivated;
        _captureWindow = null;
        _windowSource?.RemoveHook(WindowMessageHook);
        _windowSource = null;
    }

    private void CaptureWindowDeactivated(object? sender, EventArgs e) => CancelCapture();

    private IntPtr WindowMessageHook(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message != WindowsInputMessages.MouseHorizontalWheel || _captureRow is null ||
            !_bindings.CaptureSources.HasFlag(InputCaptureSources.Mouse)) return IntPtr.Zero;
        if (MouseInputFunctions.WheelBindingSource(MouseInputFunctions.WheelDelta(wParam), horizontal: true) is not { } source)
            return IntPtr.Zero;
        CapturePhysicalSourceDown(source);
        CapturePhysicalSourceUp(source);
        handled = true;
        return IntPtr.Zero;
    }

    private void CaptureControllerInput(object? sender, EventArgs e)
    {
        if (_capture is not { } capture)
        {
            _controllerCaptureTimer.Stop();
            return;
        }
        var remaining = capture.Remaining;
        if (remaining <= TimeSpan.Zero)
        {
            CancelCapture();
            return;
        }
        if (_captureButton?.Content is TextBlock prompt)
            prompt.Text = $"{LocExtension.Get("Emulation.Input.Press")} ({Math.Max(1, (int)Math.Ceiling(remaining.TotalSeconds))} s)";
        capture.PollControllers(deviceId => ControllerCaptured?.Invoke(this, new ControllerCapturedEventArgs(deviceId)));
        CompleteReleasedCapture();
    }

    private void CancelCapture()
    {
        var capture = _capture;
        _capture = null;
        try
        {
            if (_captureButton is not null)
                _captureButton.Content = _captureButtonContent;
        }
        finally
        {
            _controllerCaptureTimer.Stop();
            _captureButtonContent = null;
            _captureButton = null;
            _captureRow = null;
            capture?.Dispose();
        }
    }

    internal void ApplyOppositeAxisBinding(InputBindingRow row) => _bindings.ApplyOppositeAxisBinding(row);

    private void FinishCapture()
    {
        try
        {
            if (_captureRow is { } capturedRow) ApplyOppositeAxisBinding(capturedRow);
        }
        finally { CancelCapture(); }
        ValidateBindings();
        BindingsChanged?.Invoke(this, EventArgs.Empty);
    }
}
