using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GWGUI.App.ViewModels.Input;

namespace GWGUI.App.Views.Controls.Emulation.Input;

public partial class InputBindingEditor
{
    private Func<object>? presentationCaptureCaption;
    internal TextBlock PresentationTargetHeader => TargetHeader;
    internal TextBox PresentationSearch => SearchBox;
    internal ListBox PresentationRows => BindingsList;
    internal void RequestRestoreDefaults() => RestoreDefaultsClicked(this, new RoutedEventArgs());
    internal void RequestClearConflicts() => ClearConflictsClicked(this, new RoutedEventArgs());
    internal void RequestClear(InputBindingRow row) => ClearClicked(new Button { Tag = row }, new RoutedEventArgs());
    internal void RequestCancelCapture() => CancelCapture();

    internal IDisposable AttachPresentation(FrameworkElement host, Func<object> caption) => new CapturePresentation(this, host, caption);

    private sealed class CapturePresentation : IDisposable
    {
        private readonly InputBindingEditor source;
        private readonly FrameworkElement host;
        private readonly Func<object>? previousCaption;
        private bool disposed;

        internal CapturePresentation(InputBindingEditor source, FrameworkElement host, Func<object> caption)
        {
            this.source = source;
            this.host = host;
            previousCaption = source.presentationCaptureCaption;
            try
            {
                source.CancelCapture();
                source.DetachWindowHook();
                source.presentationCaptureCaption = caption;
                host.AddHandler(PreviewKeyDownEvent, new KeyEventHandler(source.CaptureKeyDown), true);
                host.AddHandler(PreviewKeyUpEvent, new KeyEventHandler(source.CaptureKeyUp), true);
                host.AddHandler(PreviewMouseDownEvent, new MouseButtonEventHandler(source.CaptureMouseDown), true);
                host.AddHandler(PreviewMouseUpEvent, new MouseButtonEventHandler(source.CaptureMouseUp), true);
                host.AddHandler(PreviewMouseWheelEvent, new MouseWheelEventHandler(source.CaptureMouseWheel), true);
                host.Loaded += Loaded;
                host.Unloaded += Unloaded;
                host.IsVisibleChanged += VisibilityChanged;
                if (host.IsLoaded) source.AttachWindowHook(host);
            }
            catch { Dispose(); throw; }
        }

        private void Loaded(object sender, RoutedEventArgs args) => source.AttachWindowHook(host);
        private void Unloaded(object sender, RoutedEventArgs args)
        {
            try { source.CancelCapture(); }
            finally { source.DetachWindowHook(); }
        }
        private void VisibilityChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            if (!host.IsVisible) source.CancelCapture();
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            try { source.CancelCapture(); }
            finally
            {
                try { source.DetachWindowHook(); }
                finally
                {
                    host.RemoveHandler(PreviewKeyDownEvent, new KeyEventHandler(source.CaptureKeyDown));
                    host.RemoveHandler(PreviewKeyUpEvent, new KeyEventHandler(source.CaptureKeyUp));
                    host.RemoveHandler(PreviewMouseDownEvent, new MouseButtonEventHandler(source.CaptureMouseDown));
                    host.RemoveHandler(PreviewMouseUpEvent, new MouseButtonEventHandler(source.CaptureMouseUp));
                    host.RemoveHandler(PreviewMouseWheelEvent, new MouseWheelEventHandler(source.CaptureMouseWheel));
                    host.Loaded -= Loaded;
                    host.Unloaded -= Unloaded;
                    host.IsVisibleChanged -= VisibilityChanged;
                    source.presentationCaptureCaption = previousCaption;
                    if (source.IsLoaded) source.AttachWindowHook();
                }
            }
        }
    }
}
