using RetroMediaLab.App.Constants.Input.Controllers;
using RetroMediaLab.App.Controllers.Emulation.Input;
using RetroMediaLab.App.Contracts.Input;
using RetroMediaLab.App.Enums.Input;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.ViewModels.Input;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;


namespace RetroMediaLab.App.Views.Controls.Emulation.Input;

public partial class InputBindingEditor : UserControl
{
    private readonly InputBindingRows _bindings = new();
    private InputBindingCapture? _capture;
    private InputBindingRow? _captureRow;
    private Button? _captureButton;
    private object? _captureButtonContent;
    private readonly DispatcherTimer _controllerCaptureTimer;
    private HwndSource? _windowSource;
    private System.Windows.Window? _captureWindow;

    public InputBindingEditor()
    {
        InitializeComponent();
        BindingsList.ItemsSource = _bindings.Rows;
        SearchBox.ToolTip = LocExtension.Get("Emulation.Input.Binding.Search");
        AddHandler(PreviewKeyDownEvent, new KeyEventHandler(CaptureKeyDown), true);
        AddHandler(PreviewKeyUpEvent, new KeyEventHandler(CaptureKeyUp), true);
        AddHandler(PreviewMouseDownEvent, new MouseButtonEventHandler(CaptureMouseDown), true);
        AddHandler(PreviewMouseUpEvent, new MouseButtonEventHandler(CaptureMouseUp), true);
        AddHandler(PreviewMouseWheelEvent, new MouseWheelEventHandler(CaptureMouseWheel), true);
        _controllerCaptureTimer = new DispatcherTimer(ControllerCaptureConstants.PollingInterval, DispatcherPriority.Input, CaptureControllerInput, Dispatcher);
        _controllerCaptureTimer.Stop();
        Loaded += (_, _) => AttachWindowHook();
        Unloaded += (_, _) => { CancelCapture(); DetachWindowHook(); };
    }

    public bool HasErrors => _bindings.HasErrors;
    public IReadOnlyList<InputBindingRow> Rows => _bindings.Rows;
    public event EventHandler? BindingsChanged;
    public event EventHandler<ControllerCapturedEventArgs>? ControllerCaptured;

    public void ConfigurePresentation(string firstColumnHeader, string searchPlaceholder)
    {
        TargetHeader.Text = firstColumnHeader;
        SearchPlaceholder.Text = searchPlaceholder;
    }

    public void ConfigureCaptureSources(InputCaptureSources sources, bool prefixKeyboardSource = false, bool allowIdentityKeyboardBindings = false)
    {
        _bindings.ConfigureCaptureSources(sources, prefixKeyboardSource, allowIdentityKeyboardBindings);
        ValidateBindings();
    }

    public void SetRows(IEnumerable<InputBindingDefinition> definitions, IReadOnlyDictionary<string, string>? values, string? moduleId = null)
    {
        _analogSelectors.Clear();
        _bindings.SetRows(definitions, values, moduleId);
        ValidateBindings();
        RebuildAnalogBindings();
    }

    internal bool SelectAndStartCapture(string bindingId, Button? button = null)
    {
        var row = _bindings.Find(bindingId);
        if (row is null) return false;
        BindingsList.SelectedItem = row;
        BindingsList.ScrollIntoView(row);
        BeginCapture(row, button);
        return true;
    }

    public void SetReservedBindings(IEnumerable<string> bindings)
    {
        _bindings.SetReservedBindings(bindings);
        ValidateBindings();
    }
}
