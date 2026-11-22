using RetroMediaLab.App.Constants.Emulation;
using RetroMediaLab.App.Constants.Input.Windows;
using RetroMediaLab.App.Contracts.Input;
using RetroMediaLab.App.Controllers.Emulation.Input;
using RetroMediaLab.App.Enums.Emulation.Shortcuts;
using RetroMediaLab.App.Functions.Emulation.Shortcuts;
using RetroMediaLab.App.Functions.Input.Bindings;
using RetroMediaLab.App.Functions.Input.Keyboard;
using RetroMediaLab.App.Functions.Input.Mouse;
using RetroMediaLab.App.Input.Mouse;
using RetroMediaLab.App.Services.Input.GameInput;
using RetroMediaLab.App.Services.Input.Midi;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using RetroMediaLab.Emulation;
using RetroMediaLab.Infrastructure.Settings.Emulation;
using RetroMediaLab.App.Theme.Common;


namespace RetroMediaLab.App.Controllers.Emulation.Machine;

internal sealed class MachineInputController : IDisposable
{
    private EmulationMachinePresentation _view;
    private readonly MachineControllerFeedbackController _feedback;
    private readonly Func<IEmulatedMachine> _machine;
    private readonly IReadOnlyList<GlobalShortcutBinding> _globalShortcuts;
    private IReadOnlyList<GlobalShortcutBinding> _fullscreenShortcuts;
    private IReadOnlyList<GlobalShortcutBinding> _osdShortcuts;
    private readonly Func<IReadOnlyDictionary<string, string>>? _readShortcutSettings;
    private IReadOnlyDictionary<string, string>? _shortcutSettings;
    private readonly Func<bool> _isFullscreen;
    private readonly Func<bool>? _isOsdEditing;
    private readonly Func<long> _inputClock;
    private readonly Func<string, Task> _executeShortcut;
    private readonly Func<bool> _isActive;
    private IReadOnlyList<KeyboardShortcutBinding> _keyboardShortcuts;
    private readonly IRelativeMouseCapture _pointerCapture;
    private readonly Func<GameInputPhysicalState> _readPhysicalInput;
    private readonly Action<FrameworkElement, IntPtr> _focus;
    private readonly bool _automaticPolling;
    private bool _hasMidiShortcuts;
    private MidiShortcutInput? _midiShortcuts;
    private readonly HashSet<EmulationKey> _keys = [];
    private readonly HashSet<Key> _physicalKeys = [];
    private readonly HashSet<EmulationKey> _keysHeldAtOsdClose = [];
    private readonly OsdControllerReleaseFilter _controllersHeldAtOsdClose = new();
    private GameInputMouseButtons _buttonsHeldAtOsdClose;
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
    private bool _osdHasFocus;
    private string? _repeatedOsdAction;
    private long _nextOsdRepeat;
    private bool _restorePointerAfterOsd;
    private bool _disposed;

    private IReadOnlyList<GlobalShortcutBinding> AvailableShortcuts =>
        _osdHasFocus ? _osdShortcuts : _isFullscreen() ? _fullscreenShortcuts : _globalShortcuts;

    internal MachineInputController(EmulationMachinePresentation view, FrameworkElement inputView, IntPtr inputHandle,
        Func<IEmulatedMachine> machine, IReadOnlyList<GlobalShortcutBinding> globalShortcuts,
        Func<string, Task> executeShortcut, Func<bool> isActive,
        IRelativeMouseCapture? pointerCapture = null, Func<GameInputPhysicalState>? readPhysicalInput = null,
        Action<FrameworkElement, IntPtr>? focus = null, bool automaticPolling = true, IGameInputControllerSource? feedbackSource = null,
        Func<IntPtr>? inputHandleProvider = null,
        IReadOnlyList<GlobalShortcutBinding>? osdShortcuts = null, Func<bool>? isFullscreen = null,
        Func<IReadOnlyDictionary<string, string>>? readShortcutSettings = null, Func<bool>? isOsdEditing = null,
        Func<long>? inputClock = null)
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
        _readShortcutSettings = readShortcutSettings;
        _shortcutSettings = readShortcutSettings?.Invoke();
        _osdShortcuts = _shortcutSettings is null ? osdShortcuts ?? [] : EmulationShortcutMap.OsdShortcuts(_shortcutSettings);
        _hasMidiShortcuts = _osdShortcuts.Any(shortcut => MidiShortcutInput.IsMidiBinding(shortcut.SourceBinding));
        _fullscreenShortcuts = [.. globalShortcuts, .. _osdShortcuts.Where(shortcut => EmulationShortcutMap.IsOsdToggle(shortcut.Action))];
        _isFullscreen = isFullscreen ?? (() => false);
        _isOsdEditing = isOsdEditing;
        _inputClock = inputClock ?? (() => Environment.TickCount64);
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
    internal IReadOnlyList<GlobalShortcutBinding> OsdShortcuts => _osdShortcuts;

    private void RefreshOsdShortcuts()
    {
        if (_readShortcutSettings?.Invoke() is not { } settings || ReferenceEquals(settings, _shortcutSettings)) return;
        var previous = _shortcutSettings;
        _shortcutSettings = settings;
        foreach (var shortcut in _osdShortcuts)
            if (previous?.GetValueOrDefault(shortcut.Action) != settings.GetValueOrDefault(shortcut.Action))
                _activeShortcuts.Remove(shortcut.Action);
        _osdShortcuts = EmulationShortcutMap.OsdShortcuts(settings);
        _fullscreenShortcuts = [.. _globalShortcuts, .. _osdShortcuts.Where(shortcut => EmulationShortcutMap.IsOsdToggle(shortcut.Action))];
        _hasMidiShortcuts = _osdShortcuts.Any(shortcut => MidiShortcutInput.IsMidiBinding(shortcut.SourceBinding));
        UpdatePolling();
    }

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
        UpdatePolling();
        if (!powered)
        {
            try { ReleasePointer(); }
            finally { _feedback.Reset(); }
            _keys.Clear();
            _physicalKeys.Clear();
            _keysHeldAtOsdClose.Clear();
            _controllersHeldAtOsdClose.Clear();
            _buttonsHeldAtOsdClose = GameInputMouseButtons.None;
            _pressedShortcutKeys.Clear();
            _activeShortcuts.Clear();
        }
    }

    private void UpdatePolling()
    {
        if (_hasMidiShortcuts && !_disposed && _isFullscreen()) _midiShortcuts ??= MidiShortcutInput.Open();
        else ReleaseMidiShortcuts();
        if (!_disposed && _automaticPolling && (_powered || _isFullscreen())) _timer.Start();
        else _timer.Stop();
    }

    private void ReleaseMidiShortcuts()
    {
        var input = _midiShortcuts;
        _midiShortcuts = null;
        input?.Dispose();
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
        if (_disposed || !(_powered || _isFullscreen()) || !_isActive() || _osdHasFocus) return;
        _focus(_inputView, InputHandle);
    }

    internal void ReleasePointer() => _pointerCapture.Release(_inputView, InputHandle);

    internal void SetOsdFocus(bool hasFocus)
    {
        if (_osdHasFocus == hasFocus) return;
        _osdHasFocus = hasFocus;
        _repeatedOsdAction = null;
        _keys.Clear();
        _pressedShortcutKeys.Clear();
        if (hasFocus)
        {
            ResetKeyboardShortcutsForFocusTransfer();
            _restorePointerAfterOsd = IsPointerCaptured;
            ReleasePointer();
            _feedback.Reset();
            if (_powered && CanAcceptInput(_machine()))
                _machine().Input.SetInput(EmulationInputSnapshot.Empty);
        }
        else
        {
            if (_powered && !_disposed)
            {
                var physical = _readPhysicalInput();
                _keysHeldAtOsdClose.UnionWith(OsdShortcutKeys(physical));
                _buttonsHeldAtOsdClose = MouseInputFunctions.PressedButtons(physical.Pointer);
                _controllersHeldAtOsdClose.Capture(physical.Controllers);
            }
            ResetKeyboardShortcutsForFocusTransfer();
            foreach (var shortcut in _osdShortcuts.Where(shortcut => !EmulationShortcutMap.IsOsdToggle(shortcut.Action)))
                _activeShortcuts.Remove(shortcut.Action);
            RestoreFocus();
            if (_restorePointerAfterOsd && _powered && !_disposed && _isActive() && !_hostTransition)
                _pointerCapture.Capture(_inputView, _view.Screen, InputHandle);
            _restorePointerAfterOsd = false;
        }
    }

    internal void BeginHostTransition()
    {
        ResetKeyboardShortcutsForFocusTransfer();
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
            if (_disposed) return;
            UpdatePolling();
            if (!(_powered || _isFullscreen())) return;
            DetachWindowHook();
            AttachWindowHook();
            RestoreFocus();
            if (restorePointer && _powered && !_osdHasFocus)
                _pointerCapture.Capture(_inputView, _view.Screen, InputHandle);
        });
    }

    public void Dispose()
    {
        if (_disposed) return;
        try { SetPowered(false); }
        finally
        {
            _timer.Stop();
            _timer.Tick -= TimerTick;
            _view.Loaded -= ViewLoaded;
            _view.Unloaded -= ViewUnloaded;
            _view.DisplayHost.MouseLeftButtonDown -= DisplayHostMouseLeftButtonDown;
            try { DetachWindowHook(); }
            finally
            {
                try { Detach(); }
                finally
                {
                    try { _feedback.Dispose(); }
                    finally { _disposed = true; ReleaseMidiShortcuts(); }
                }
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
        args.Handled = HandleKeyDown(source, Keyboard.Modifiers, args.Timestamp, args.IsRepeat);
    }

    private void KeyUp(object sender, KeyEventArgs args)
    {
        var source = args.Key == Key.System ? args.SystemKey : args.Key;
        args.Handled = HandleKeyUp(source, Keyboard.Modifiers);
    }

    internal bool HandleKeyDown(Key source, ModifierKeys modifiers, int? timestamp = null, bool isRepeat = false)
    {
        RefreshOsdShortcuts();
        if (isRepeat && !_physicalKeys.Contains(source)
            && AvailableShortcuts.Any(binding => binding.SourceBinding is null && KeyboardChordFunctions.Contains(binding.Chord, source)))
            return true;
        if (!_physicalKeys.Contains(source)) _modifierState.Press(source, timestamp, _physicalKeys);
        if (_modifierState.HasSyntheticLeftControl) _keys.Remove(EmulationKey.LeftControl);
        modifiers = _modifierState.Normalize(modifiers, _physicalKeys);
        _physicalKeys.Add(source);
        ReleaseInactiveKeyboardShortcuts(modifiers);
        var shortcut = EmulationShortcutFunctions.ResolveGlobal(AvailableShortcuts, modifiers, _physicalKeys, source);
        if (shortcut.Category == EmulationShortcutMatchCategory.Global)
        {
            if (!isRepeat && shortcut.Binding is { } binding && EmulationShortcutFunctions.UsesKey(binding.Chord, source)
                && _activeShortcuts.Add(binding.Action))
                _ = _executeShortcut(binding.Action);
            return true;
        }
        if (_osdHasFocus || shortcut.Category == EmulationShortcutMatchCategory.ReservedForGlobal) return true;
        if (EmulationKeyMapper.TryMap(source, out var heldKey) && _keysHeldAtOsdClose.Contains(heldKey)) return true;
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
        if (EmulationKeyMapper.TryMap(source, out var releasedKey)) _keysHeldAtOsdClose.Remove(releasedKey);
        _modifierState.Release(source);
        modifiers = _modifierState.Normalize(modifiers, _physicalKeys);
        ReleaseInactiveKeyboardShortcuts(modifiers);
        EmulationShortcutFunctions.ReleaseInactive(_activeShortcuts, AvailableShortcuts, modifiers, _physicalKeys);
        if (_repeatedOsdAction is { } repeating && !_activeShortcuts.Contains(repeating))
            _repeatedOsdAction = null;
        if (_osdHasFocus) return true;
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
        if (!_powered || _disposed || !_isActive() || _osdHasFocus) return;
        _focus(_inputView, InputHandle);
        if (_machine().Input.SupportsPointerCapture && _machine().Input.CapturePointerOnClick
            && !_pointerCapture.IsCaptured)
            _pointerCapture.Capture(_inputView, _view.Screen, InputHandle);
        Publish();
    }

    private void MouseMove(object sender, MouseEventArgs args) => _pointerCapture.ProcessMovement(_view.Screen, (x, y) => Publish(x, y));

    private void MouseWheel(object sender, MouseWheelEventArgs args) => Publish(wheel: args.Delta);

    private void LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs args)
    {
        if (!_osdHasFocus) Deactivate();
    }

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
        var delta = MouseInputFunctions.WheelDelta(wParam);
        if (delta != WindowsInputMessages.NeutralWheelDelta) Publish(horizontalWheel: delta);
        return IntPtr.Zero;
    }

    private void TimerTick(object? sender, EventArgs args) => Publish();

    internal void Publish(int deltaX = 0, int deltaY = 0, int wheel = 0, int horizontalWheel = 0)
    {
        if (_disposed || !(_powered || _isFullscreen())) return;
        try
        {
            RefreshOsdShortcuts();
            var machine = _machine();
            _feedback.Update(machine as IEmulationControllerFeedbackSource, _powered && !_disposed && _isActive()
                && !_osdHasFocus && machine.State == EmulationMachineState.Running);
            PublishInput(deltaX, deltaY, wheel, horizontalWheel);
        }
        catch { _feedback.Reset(); throw; }
    }

    private void PublishInput(int deltaX, int deltaY, int wheel, int horizontalWheel)
    {
        if (!_isActive())
        {
            Deactivate();
            return;
        }
        var physical = _readPhysicalInput();
        var gameControllers = _controllersHeldAtOsdClose.Filter(physical.Controllers);
        if (_keysHeldAtOsdClose.Count != 0)
            _keysHeldAtOsdClose.IntersectWith(OsdShortcutKeys(physical));
        var keys = new HashSet<EmulationKey>(physical.Keys);
        keys.UnionWith(_keys);
        keys.ExceptWith(_keysHeldAtOsdClose);
        keys.Remove(EmulationKey.LeftWindows);
        keys.Remove(EmulationKey.RightWindows);
        if (_modifierState.HasSyntheticLeftControl) keys.Remove(EmulationKey.LeftControl);
        foreach (var shortcutKey in _pressedShortcutKeys.Values) keys.Add(shortcutKey);
        _buttonsHeldAtOsdClose &= MouseInputFunctions.PressedButtons(physical.Pointer);
        var gamePointer = MouseInputFunctions.WithoutButtons(physical.Pointer, _buttonsHeldAtOsdClose);
        var pointer = _pointerCapture.IsCaptured
            ? gamePointer
            : gamePointer with
            {
                DeltaX = 0, DeltaY = 0, Wheel = 0, HorizontalWheel = 0,
                Left = false, Right = false, Middle = false,
                ExtendedButton1 = false, ExtendedButton2 = false
            };
        var machine = _machine();
        var canSendToMachine = _powered && CanAcceptInput(machine);
        var physicalSnapshot = new EmulationInputSnapshot(keys, physical.Pointer, physical.Controllers);
        EmulationInputSnapshot? osdSnapshot = null;
        foreach (var shortcut in AvailableShortcuts.Where(binding => binding.SourceBinding is not null))
        {
            bool isOsdShortcut = _osdShortcuts.Contains(shortcut);
            if (!canSendToMachine && !isOsdShortcut) continue;
            var snapshot = isOsdShortcut ? osdSnapshot ??= OsdShortcutSnapshot(physical) : physicalSnapshot;
            if (EmulationSdk.InputProcessing.InputSourcePressure(shortcut.SourceBinding!, EmulationControllerState.Empty, snapshot) > .5f)
            {
                if (_activeShortcuts.Add(shortcut.Action))
                {
                    _ = _executeShortcut(shortcut.Action);
                    if (isOsdShortcut) break;
                }
            }
            else _activeShortcuts.Remove(shortcut.Action);
        }
        if (_osdHasFocus)
        {
            RepeatOsdDirection();
            return;
        }
        if (_osdShortcuts.Any(shortcut => _activeShortcuts.Contains(shortcut.Action))) return;
        if (!canSendToMachine || !_powered || _disposed || !_isActive() || !ReferenceEquals(machine, _machine()) || !CanAcceptInput(machine)) return;
        var gameSources = _buttonsHeldAtOsdClose == GameInputMouseButtons.None && ReferenceEquals(gameControllers, physical.Controllers)
            ? physicalSnapshot : physicalSnapshot with { Pointer = gamePointer, Controllers = gameControllers };
        foreach (var binding in machine.Input.KeyboardBindings)
            if (binding.Value.Contains(':') && Enum.TryParse<EmulationKey>(binding.Key, out var target)
                && !_globalShortcuts.Any(shortcut => shortcut.SourceBinding is { } source && InputBindingSyntax.Conflicts(source, binding.Value))
                && EmulationSdk.InputProcessing.InputSourcePressure(binding.Value, EmulationControllerState.Empty, gameSources) > .5f)
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
        machine.Input.SetInput(new EmulationInputSnapshot(keys, pointer, gameControllers) { HostCharacters = characters });
    }

    private void RepeatOsdDirection()
    {
        var action = _isOsdEditing?.Invoke() == true
            ? _osdShortcuts.FirstOrDefault(shortcut => _activeShortcuts.Contains(shortcut.Action) && IsOsdDirection(shortcut.Action))?.Action
            : null;
        var now = _inputClock();
        if (action != _repeatedOsdAction)
        {
            _repeatedOsdAction = action;
            _nextOsdRepeat = now + 300;
        }
        else if (action is not null && now >= _nextOsdRepeat)
        {
            _nextOsdRepeat = now + 35;
            _ = _executeShortcut(action);
        }
    }

    private static bool IsOsdDirection(string action) => EmulationShortcutMap.OsdPrimaryAction(action) is
        EmulationShortcutDefaults.OsdUp or EmulationShortcutDefaults.OsdDown
        or EmulationShortcutDefaults.OsdLeft or EmulationShortcutDefaults.OsdRight;

    private void ResetKeyboardShortcutsForFocusTransfer()
    {
        // A key release may be delivered to the old window after the focus transfer.
        _physicalKeys.RemoveWhere(key => !KeyboardChordFunctions.IsModifierKey(key));
        var bindings = _globalShortcuts.Concat(_osdShortcuts);
        foreach (var binding in bindings)
            if (binding.SourceBinding is null) _activeShortcuts.Remove(binding.Action);
    }

    private HashSet<EmulationKey> OsdShortcutKeys(GameInputPhysicalState physical)
    {
        var keys = new HashSet<EmulationKey>(physical.Keys);
        // Les événements de la fenêtre restent des entrées hôte, même quand l’OSD les consomme.
        foreach (var source in _physicalKeys)
            if (EmulationKeyMapper.TryMap(source, out var key)) keys.Add(key);
        keys.Remove(EmulationKey.LeftWindows);
        keys.Remove(EmulationKey.RightWindows);
        if (_modifierState.HasSyntheticLeftControl) keys.Remove(EmulationKey.LeftControl);
        return keys;
    }

    private EmulationInputSnapshot OsdShortcutSnapshot(GameInputPhysicalState physical)
    {
        var controllers = _midiShortcuts is null ? physical.Controllers
            : [.. physical.Controllers, .. _midiShortcuts.ReadControllers()];
        return new EmulationInputSnapshot(OsdShortcutKeys(physical), physical.Pointer, controllers);
    }

    private static bool CanAcceptInput(IEmulatedMachine machine) => machine.State is EmulationMachineState.Running or EmulationMachineState.Paused;

    private IntPtr NativeMessage(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        switch (message)
        {
            case WindowsInputMessages.KeyDown:
            case WindowsInputMessages.SystemKeyDown:
                handled = HandleKeyDown(KeyInterop.KeyFromVirtualKey(unchecked((int)wParam.ToInt64())), Keyboard.Modifiers,
                    GetMessageTime(), (lParam.ToInt64() & WindowsInputMessages.KeyWasDownMask) != 0);
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
                Publish(wheel: MouseInputFunctions.WheelDelta(wParam));
                break;
            case WindowsInputMessages.MouseHorizontalWheel when _pointerCapture.IsCaptured:
                Publish(horizontalWheel: MouseInputFunctions.WheelDelta(wParam));
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
