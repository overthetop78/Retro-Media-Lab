using RetroMediaLab.App.Constants.Emulation;
using RetroMediaLab.App.Constants.Input.Windows;
using RetroMediaLab.App.Contracts.Input;
using RetroMediaLab.App.Enums.Emulation.Shortcuts;
using RetroMediaLab.App.Functions.Emulation.Shortcuts;
using RetroMediaLab.App.Functions.Input.Bindings;
using RetroMediaLab.App.Functions.Input.Keyboard;
using RetroMediaLab.App.Input.Mouse;
using RetroMediaLab.App.Services.Input.GameInput;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using RetroMediaLab.Emulation;
using RetroMediaLab.App.Theme.Common;


namespace RetroMediaLab.App.Controllers.Emulation.Machine;

internal sealed class MachineInputController : IDisposable
{
    private EmulationMachinePresentation _view;
    private readonly MachineControllerFeedbackController _feedback;
    private readonly Func<IEmulatedMachine> _machine;
    private readonly IReadOnlyList<GlobalShortcutBinding> _globalShortcuts;
    private readonly Func<string, Task> _executeShortcut;
    private readonly Func<bool> _isActive;
    private IReadOnlyList<KeyboardShortcutBinding> _keyboardShortcuts;
    private readonly IRelativeMouseCapture _pointerCapture;
    private readonly Func<GameInputPhysicalState> _readPhysicalInput;
    private readonly Action<FrameworkElement, IntPtr> _focus;
    private readonly bool _automaticPolling;
    private readonly HashSet<EmulationKey> _keys = [];
    private readonly HashSet<Key> _physicalKeys = [];
    private readonly KeyboardModifierState _modifierState = new();
    private readonly Dictionary<Key, EmulationKey> _pressedShortcutKeys = [];
    private readonly HashSet<string> _activeShortcuts = new(StringComparer.Ordinal);
    private readonly DispatcherTimer _timer = new()
    {
        Interval = EmulationRuntimeConstants.EmulationInputPollingInterval
    };
    private FrameworkElement _inputView;
    private IntPtr _configuredInputHandle;
    private readonly Func<IntPtr>? _inputHandleProvider;
    private IntPtr InputHandle => _inputHandleProvider?.Invoke() ?? _configuredInputHandle;
    private HwndSource? _windowSource;
    private Window? _focusWindow;
    private DispatcherOperation? _clickFocus;
    private bool _powered;
    private bool _hostTransition;
    private bool _restorePointerAfterHostTransition;
    private bool _disposed;

    internal MachineInputController(EmulationMachinePresentation view, FrameworkElement inputView, IntPtr inputHandle,
        Func<IEmulatedMachine> machine, IReadOnlyList<GlobalShortcutBinding> globalShortcuts,
        Func<string, Task> executeShortcut, Func<bool> isActive,
        IRelativeMouseCapture? pointerCapture = null, Func<GameInputPhysicalState>? readPhysicalInput = null,
        Action<FrameworkElement, IntPtr>? focus = null, bool automaticPolling = true, IGameInputControllerSource? feedbackSource = null,
        Func<IntPtr>? inputHandleProvider = null)
    {
        _view = view;
        _feedback = new MachineControllerFeedbackController(feedbackSource ?? GameInputControllerSource.Instance);
        _pointerCapture = pointerCapture ?? new RelativeMouseCapture();
        _readPhysicalInput = readPhysicalInput ?? GameInputControllerReader.ReadPhysicalInput;
        _focus = focus ?? RelativeMouseCapture.Focus;
        _automaticPolling = automaticPolling;
        _inputView = inputView;
        _configuredInputHandle = inputHandle;
        _inputHandleProvider = inputHandleProvider;
        _machine = machine;
        _globalShortcuts = globalShortcuts;
        _executeShortcut = executeShortcut;
        _isActive = isActive;
        _keyboardShortcuts = EmulationShortcutMap.KeyboardShortcuts(machine().Input.KeyboardBindings);
        Attach();
        _view.DisplayHost.MouseLeftButtonDown += DisplayHostMouseLeftButtonDown;
        _timer.Tick += TimerTick;
        _view.Loaded += ViewLoaded;
        _view.Unloaded += ViewUnloaded;
    }

    internal bool IsPointerCaptured => _pointerCapture.IsCaptured;

    internal void RefreshBindings()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        // Relâcher les anciennes commandes avant d'interpréter les touches avec la nouvelle association.
        _keys.Clear();
        _physicalKeys.Clear();
        _modifierState.Reset();
        _pressedShortcutKeys.Clear();
        _activeShortcuts.Clear();
        _keyboardShortcuts = EmulationShortcutMap.KeyboardShortcuts(_machine().Input.KeyboardBindings);
        if (_powered && CanAcceptInput(_machine()))
            _machine().Input.SetInput(EmulationInputSnapshot.Empty);
        if (!_machine().Input.SupportsPointerCapture || !_machine().Input.CapturePointerOnClick)
            ReleasePointer();
    }

    internal void SetPowered(bool powered)
    {
        _powered = powered;
        if (powered) { if (_automaticPolling) _timer.Start(); }
        else
        {
            _timer.Stop();
            try { ReleasePointer(); }
            finally { _feedback.Reset(); }
            _keys.Clear();
            _physicalKeys.Clear();
            _pressedShortcutKeys.Clear();
            _activeShortcuts.Clear();
        }
    }

    internal void SetInputView(FrameworkElement inputView, IntPtr inputHandle)
    {
        ReleasePointer();
        DetachWindowHook();
        Detach();
        _inputView = inputView;
        _configuredInputHandle = inputHandle;
        Attach();
        if (_inputView.IsLoaded) AttachWindowHook();
    }

    internal void SetPresentation(EmulationMachinePresentation presentation)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (ReferenceEquals(_view, presentation)) return;
        ReleasePointer();
        DetachWindowHook();
        _view.Loaded -= ViewLoaded;
        _view.Unloaded -= ViewUnloaded;
        _view.DisplayHost.MouseLeftButtonDown -= DisplayHostMouseLeftButtonDown;
        _view = presentation;
        _view.Loaded += ViewLoaded;
        _view.Unloaded += ViewUnloaded;
        _view.DisplayHost.MouseLeftButtonDown += DisplayHostMouseLeftButtonDown;
        if (_view.IsLoaded) AttachWindowHook();
    }

    internal void RestoreFocus()
    {
        if (_disposed || !_powered || !_isActive()) return;
        _focus(_inputView, InputHandle);
    }

    internal void ReleasePointer() => _pointerCapture.Release(_inputView, InputHandle);

    internal void BeginHostTransition()
    {
        _hostTransition = true;
        _restorePointerAfterHostTransition = _pointerCapture.IsCaptured;
    }

    internal void CompleteHostTransition()
    {
        _inputView.Dispatcher.BeginInvoke(DispatcherPriority.Input, () =>
        {
            var restorePointer = _restorePointerAfterHostTransition;
            _restorePointerAfterHostTransition = false;
            _hostTransition = false;
            if (!_powered || _disposed) return;
            DetachWindowHook();
            AttachWindowHook();
            RestoreFocus();
            if (restorePointer)
                _pointerCapture.Capture(_inputView, _view.Screen, InputHandle);
        });
    }

    public void Dispose()
    {
        if (_disposed) return;
        try { SetPowered(false); }
        finally
        {
            _timer.Tick -= TimerTick;
            _view.Loaded -= ViewLoaded;
            _view.Unloaded -= ViewUnloaded;
            _view.DisplayHost.MouseLeftButtonDown -= DisplayHostMouseLeftButtonDown;
            try { DetachWindowHook(); }
            finally
            {
                try { Detach(); }
                finally { _feedback.Dispose(); _disposed = true; }
            }
        }
    }

    private void Attach()
    {
        _inputView.KeyDown += KeyDown;
        _inputView.KeyUp += KeyUp;
        _inputView.MouseDown += MouseDown;
        _inputView.MouseMove += MouseMove;
        _inputView.MouseWheel += MouseWheel;
        _inputView.LostKeyboardFocus += LostKeyboardFocus;
        if (_inputView is HwndHost host) host.MessageHook += NativeMessage;
    }

    private void Detach()
    {
        _inputView.KeyDown -= KeyDown;
        _inputView.KeyUp -= KeyUp;
        _inputView.MouseDown -= MouseDown;
        _inputView.MouseMove -= MouseMove;
        _inputView.MouseWheel -= MouseWheel;
        _inputView.LostKeyboardFocus -= LostKeyboardFocus;
        if (_inputView is HwndHost host) host.MessageHook -= NativeMessage;
    }

    private void KeyDown(object sender, KeyEventArgs args)
    {
        var source = args.Key == Key.System ? args.SystemKey : args.Key;
        args.Handled = HandleKeyDown(source, Keyboard.Modifiers, args.Timestamp);
    }

    private void KeyUp(object sender, KeyEventArgs args)
    {
        var source = args.Key == Key.System ? args.SystemKey : args.Key;
        args.Handled = HandleKeyUp(source, Keyboard.Modifiers);
    }

    internal bool HandleKeyDown(Key source, ModifierKeys modifiers, int? timestamp = null)
    {
        if (!_physicalKeys.Contains(source)) _modifierState.Press(source, timestamp, _physicalKeys);
        if (_modifierState.HasSyntheticLeftControl) _keys.Remove(EmulationKey.LeftControl);
        modifiers = _modifierState.Normalize(modifiers, _physicalKeys);
        _physicalKeys.Add(source);
        ReleaseInactiveKeyboardShortcuts(modifiers);
        var shortcut = EmulationShortcutFunctions.ResolveGlobal(_globalShortcuts, modifiers, _physicalKeys, source, _activeShortcuts);
        if (shortcut.Category == EmulationShortcutMatchCategory.Global)
        {
            if (shortcut.ShouldExecute && shortcut.Action is not null
                && _activeShortcuts.Add(shortcut.Action)) _ = _executeShortcut(shortcut.Action);
            return true;
        }
        if (shortcut.Category == EmulationShortcutMatchCategory.ReservedForGlobal) return true;
        var machineShortcut = _keyboardShortcuts.FirstOrDefault(binding => KeyboardChordFunctions.Matches(binding.Chord, modifiers, _physicalKeys));
        if (machineShortcut is not null)
        {
            _pressedShortcutKeys[source] = machineShortcut.EmulationKey;
            _keys.Add(machineShortcut.EmulationKey);
            Publish();
            return true;
        }
        if (!EmulationKeyMapper.TryMap(source, out var key)) return false;
        _keys.Add(key);
        Publish();
        return true;
    }

    internal bool HandleKeyUp(Key source, ModifierKeys modifiers)
    {
        _physicalKeys.Remove(source);
        _modifierState.Release(source);
        modifiers = _modifierState.Normalize(modifiers, _physicalKeys);
        ReleaseInactiveKeyboardShortcuts(modifiers);
        EmulationShortcutFunctions.ReleaseInactive(_activeShortcuts, _globalShortcuts, modifiers, _physicalKeys);
        if (_pressedShortcutKeys.Remove(source, out var shortcutKey))
        {
            _keys.Remove(shortcutKey);
            Publish();
            return true;
        }
        if (!EmulationKeyMapper.TryMap(source, out var key)) return false;
        _keys.Remove(key);
        Publish();
        return true;
    }

    private void DisplayHostMouseLeftButtonDown(object sender, MouseButtonEventArgs args)
    {
        if (ReferenceEquals(args.OriginalSource, _view.DisplayHost)) RestoreFocus();
    }

    private void ReleaseInactiveKeyboardShortcuts(ModifierKeys modifiers)
    {
        foreach (var active in _pressedShortcutKeys.ToArray())
        {
            if (_keyboardShortcuts.Any(binding => binding.EmulationKey == active.Value && KeyboardChordFunctions.Matches(binding.Chord, modifiers, _physicalKeys))) continue;
            _pressedShortcutKeys.Remove(active.Key);
            if (!_pressedShortcutKeys.ContainsValue(active.Value)) _keys.Remove(active.Value);
        }
    }

    private void MouseDown(object sender, MouseButtonEventArgs args) => RequestPointerCapture();

    internal void RequestPointerCapture()
    {
        if (!_powered || _disposed || !_isActive()) return;
        _focus(_inputView, InputHandle);
        if (_machine().Input.SupportsPointerCapture && _machine().Input.CapturePointerOnClick
            && !_pointerCapture.IsCaptured)
            _pointerCapture.Capture(_inputView, _view.Screen, InputHandle);
        Publish();
    }

    private void MouseMove(object sender, MouseEventArgs args) => _pointerCapture.ProcessMovement(_view.Screen, (x, y) => Publish(x, y));

    private void MouseWheel(object sender, MouseWheelEventArgs args) => Publish(wheel: args.Delta);

    private void LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs args) => Deactivate();

    internal void Deactivate()
    {
        _feedback.Reset();
        if (_hostTransition) return;
        ReleasePointer();
        _keys.Clear();
        _physicalKeys.Clear();
        _modifierState.Reset();
        _pressedShortcutKeys.Clear();
        _activeShortcuts.Clear();
        if (_powered && !_disposed && CanAcceptInput(_machine()))
            _machine().Input.SetInput(EmulationInputSnapshot.Empty);
    }

    private void ViewLoaded(object sender, RoutedEventArgs args) => AttachWindowHook();

    private void ViewUnloaded(object sender, RoutedEventArgs args) => DetachWindowHook();

    private void AttachWindowHook()
    {
        if (_focusWindow is not null || (Window.GetWindow(_inputView) ?? Window.GetWindow(_view)) is not Window window) return;
        _focusWindow = window;
        window.AddHandler(Mouse.PreviewMouseUpEvent, new MouseButtonEventHandler(WindowMouseUp), handledEventsToo: true);
        _windowSource = PresentationSource.FromVisual(window) as HwndSource;
        _windowSource?.AddHook(WindowMessage);
    }

    private void DetachWindowHook()
    {
        _clickFocus?.Abort();
        _clickFocus = null;
        _focusWindow?.RemoveHandler(Mouse.PreviewMouseUpEvent, new MouseButtonEventHandler(WindowMouseUp));
        _focusWindow = null;
        _windowSource?.RemoveHook(WindowMessage);
        _windowSource = null;
    }

    private void WindowMouseUp(object sender, MouseButtonEventArgs args)
    {
        if (_disposed || !_powered || !_isActive() || !_inputView.IsVisible || _hostTransition) return;
        var editor = Keyboard.FocusedElement is TextBoxBase { IsReadOnly: false } or PasswordBox
            ? Keyboard.FocusedElement as Visual : null;
        var clickedEditor = editor is not null && args.OriginalSource is DependencyObject source
            && (ReferenceEquals(source, editor) || source is Visual && editor.IsAncestorOf(source));
        _clickFocus?.Abort();
        // Let the clicked command and tab selection finish before returning keyboard input.
        _clickFocus = _inputView.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, () =>
        {
            _clickFocus = null;
            if (_disposed || !_powered || !_isActive() || !_inputView.IsVisible || _hostTransition
                || _focusWindow is not { IsActive: true, IsEnabled: true }) return;
            // Editing and open popups keep keyboard ownership until their interaction ends.
            if (clickedEditor || Mouse.Captured is MenuBase or MenuItem or ComboBox
                || Keyboard.FocusedElement is ComboBox { IsDropDownOpen: true }) return;
            RestoreFocus();
        });
    }

    private IntPtr WindowMessage(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message != WindowsInputMessages.MouseHorizontalWheel || !_pointerCapture.IsCaptured
            || !_inputView.IsMouseOver) return IntPtr.Zero;
        var delta = unchecked((short)((wParam.ToInt64() >> WindowsInputMessages.WheelHighWordShift) & WindowsInputMessages.UnsignedWordMask));
        if (delta != WindowsInputMessages.NeutralWheelDelta) Publish(horizontalWheel: delta);
        return IntPtr.Zero;
    }

    private void TimerTick(object? sender, EventArgs args) => Publish();

    internal void Publish(int deltaX = 0, int deltaY = 0, int wheel = 0, int horizontalWheel = 0)
    {
        if (!_powered || _disposed) return;
        try
        {
            var machine = _machine();
            _feedback.Update(machine as IEmulationControllerFeedbackSource, _powered && !_disposed && _isActive() && machine.State == EmulationMachineState.Running);
            PublishInput(deltaX, deltaY, wheel, horizontalWheel);
        }
        catch { _feedback.Reset(); throw; }
    }

    private void PublishInput(int deltaX, int deltaY, int wheel, int horizontalWheel)
    {
        if (!_powered || _disposed) return;
        if (!_isActive())
        {
            Deactivate();
            return;
        }
        var physical = _readPhysicalInput();
        var keys = new HashSet<EmulationKey>(physical.Keys);
        keys.UnionWith(_keys);
        keys.Remove(EmulationKey.LeftWindows);
        keys.Remove(EmulationKey.RightWindows);
        if (_modifierState.HasSyntheticLeftControl) keys.Remove(EmulationKey.LeftControl);
        foreach (var shortcutKey in _pressedShortcutKeys.Values) keys.Add(shortcutKey);
        var pointer = _pointerCapture.IsCaptured
            ? physical.Pointer
            : physical.Pointer with
            {
                DeltaX = 0, DeltaY = 0, Wheel = 0, HorizontalWheel = 0,
                Left = false, Right = false, Middle = false,
                ExtendedButton1 = false, ExtendedButton2 = false
            };
        var machine = _machine();
        if (!CanAcceptInput(machine)) return;
        var physicalSnapshot = new EmulationInputSnapshot(keys, physical.Pointer, physical.Controllers);
        foreach (var shortcut in _globalShortcuts.Where(binding => binding.SourceBinding is not null))
        {
            if (EmulationSdk.InputProcessing.InputSourcePressure(shortcut.SourceBinding!, EmulationControllerState.Empty, physicalSnapshot) > .5f)
            {
                if (_activeShortcuts.Add(shortcut.Action)) _ = _executeShortcut(shortcut.Action);
            }
            else _activeShortcuts.Remove(shortcut.Action);
        }
        if (!_powered || _disposed || !_isActive() || !ReferenceEquals(machine, _machine()) || !CanAcceptInput(machine)) return;
        foreach (var binding in machine.Input.KeyboardBindings)
            if (binding.Value.Contains(':') && Enum.TryParse<EmulationKey>(binding.Key, out var target)
                && !_globalShortcuts.Any(shortcut => shortcut.SourceBinding is { } source && InputBindingSyntax.Conflicts(source, binding.Value))
                && EmulationSdk.InputProcessing.InputSourcePressure(binding.Value, EmulationControllerState.Empty, physicalSnapshot) > .5f)
                keys.Add(target);
        // La lecture physique peut encore contenir une touche déjà consommée par un raccourci.
        // Retirer sa source avant de construire les caractères, sans retirer la commande cible.
        foreach (var binding in _globalShortcuts.Where(binding => _activeShortcuts.Contains(binding.Action)))
            foreach (var source in binding.Chord.Keys)
                if (EmulationKeyMapper.TryMap(source, out var consumed)) keys.Remove(consumed);
        foreach (var binding in _pressedShortcutKeys)
            if (EmulationKeyMapper.TryMap(binding.Key, out var consumed) && consumed != binding.Value) keys.Remove(consumed);
        var characters = EmulationKeyMapper.HostCharacters(keys).ToDictionary(item => item.Key, item => item.Value);
        // Une affectation choisie par l'utilisateur désigne une commande, pas le caractère du clavier hôte.
        foreach (var binding in machine.Input.KeyboardBindings)
            if (Enum.TryParse<EmulationKey>(binding.Key, out var target)) characters.Remove(target);
        machine.Input.SetInput(new EmulationInputSnapshot(keys, pointer, physical.Controllers) { HostCharacters = characters });
    }

    private static bool CanAcceptInput(IEmulatedMachine machine) => machine.State is EmulationMachineState.Running or EmulationMachineState.Paused;

    private IntPtr NativeMessage(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        switch (message)
        {
            case WindowsInputMessages.KeyDown:
            case WindowsInputMessages.SystemKeyDown:
                handled = HandleKeyDown(KeyInterop.KeyFromVirtualKey(unchecked((int)wParam.ToInt64())), Keyboard.Modifiers, GetMessageTime());
                break;
            case WindowsInputMessages.KeyUp:
            case WindowsInputMessages.SystemKeyUp:
                handled = HandleKeyUp(KeyInterop.KeyFromVirtualKey(unchecked((int)wParam.ToInt64())), Keyboard.Modifiers);
                break;
            case WindowsInputMessages.LeftButtonDown:
            case WindowsInputMessages.RightButtonDown:
            case WindowsInputMessages.MiddleButtonDown:
            case WindowsInputMessages.XButtonDown:
                RelativeMouseCapture.FocusNative(hwnd);
                if (_machine().Input.SupportsPointerCapture && _machine().Input.CapturePointerOnClick
                    && !_pointerCapture.IsCaptured)
                    _pointerCapture.Capture(_inputView, _view.Screen, InputHandle);
                Publish();
                break;
            case WindowsInputMessages.LeftButtonUp:
            case WindowsInputMessages.RightButtonUp:
            case WindowsInputMessages.MiddleButtonUp:
            case WindowsInputMessages.XButtonUp:
                Publish();
                break;
            case WindowsInputMessages.MouseMove when _pointerCapture.IsCaptured:
                _pointerCapture.ProcessMovement(_view.Screen, (x, y) => Publish(x, y));
                break;
            case WindowsInputMessages.MouseWheel when _pointerCapture.IsCaptured:
                Publish(wheel: unchecked((short)((wParam.ToInt64() >> WindowsInputMessages.WheelHighWordShift) & WindowsInputMessages.UnsignedWordMask)));
                break;
            case WindowsInputMessages.MouseHorizontalWheel when _pointerCapture.IsCaptured:
                Publish(horizontalWheel: unchecked((short)((wParam.ToInt64() >> WindowsInputMessages.WheelHighWordShift) & WindowsInputMessages.UnsignedWordMask)));
                break;
            case WindowsInputMessages.SetCursor when _pointerCapture.IsCaptured:
                RelativeMouseCapture.HideNativeCursor();
                handled = true;
                break;
        }
        return IntPtr.Zero;
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern int GetMessageTime();
}
