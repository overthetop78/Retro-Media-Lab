using System.Windows;

namespace RetroMediaLab.App.Input.Mouse;

internal interface IRelativeMouseCapture
{
    bool IsCaptured { get; }
    void Capture(FrameworkElement display, FrameworkElement screen, IntPtr nativeHandle);
    void Release(FrameworkElement display, IntPtr nativeHandle);
    void ProcessMovement(FrameworkElement screen, Action<int, int> moved);
}
