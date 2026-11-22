using RetroMediaLab.App.Services.Dialogs;
using RetroMediaLab.Infrastructure.Settings.Emulation;
using RetroMediaLab.App.Constants.Emulation;
using RetroMediaLab.App.Constants.Localization;
using RetroMediaLab.App.Constants.Machine;
using RetroMediaLab.App.Contracts.Machine;
using RetroMediaLab.App.Contracts.Input;
using RetroMediaLab.App.Views.Controls.Emulation.Machine;
using RetroMediaLab.App.Functions.Machine;
using RetroMediaLab.App.Functions.Emulation.Shortcuts;
using RetroMediaLab.App.Functions.Input.Bindings;
using RetroMediaLab.App.Functions.Emulation.Storage;
using RetroMediaLab.App.Functions.Emulation.Machine;
using RetroMediaLab.App.Functions.Rendering.Emulation;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.Presenters.Emulation.Machine;
using RetroMediaLab.App.Services.Emulation;
using RetroMediaLab.App.Services.Input.GameInput;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using RetroMediaLab.Emulation;
using RetroMediaLab.App.Views.Dialogs.Emulation;
using Microsoft.Win32;
using RetroMediaLab.App.Views.Dialogs.Emulation.Storage;
using RetroMediaLab.App.Services.Storage;
using RetroMediaLab.App.Services.Windows;
using RetroMediaLab.App.Theme.Common;
using RetroMediaLab.VideoPresentation.Windows.Osd;

namespace RetroMediaLab.App.Controllers.Emulation.Machine;

internal sealed partial class MachineController : UserControl, IAsyncDisposable
{
    internal static readonly EmulationTapeCommand[] TapeCommandOrder =
    [
        EmulationTapeCommand.Record,
        EmulationTapeCommand.Play,
        EmulationTapeCommand.Rewind,
        EmulationTapeCommand.FastForward,
        EmulationTapeCommand.Stop,
        EmulationTapeCommand.Pause
    ];

    private readonly MachineControllerOptions _options;
    private EmulationMachinePresentation _view;
    private ContentControl _presentationHost;
    private Func<EmulationMachinePresentation> _presentationFactory;
    private readonly MachineSession _session;
    private readonly MachineQuickStates _quickStates;
    private readonly MachineVideoPresenter _video;
    private readonly MachineCommandBar _commands;
    private readonly MachineInputController _input;
    private IReadOnlyList<EmulationMediaDevice> _mediaDevices;
    private Func<EmulationMedia, CancellationToken, ValueTask<EmulationMedia>>? _prepareMediaAsync;
    private readonly Dictionary<EmulationMediaSlot, DateTime> _mediaActivityUntil = [];
    private Window? _fullscreenWindow;
    private Grid? _fullscreenHost;
    private FullscreenOsdIndicator? _fullscreenShaderLoading;
    private FullscreenOsdWindow? _osd;
    private bool _osdPausedEmulation;
    private readonly SemaphoreSlim _osdToggleGate = new(1);
    private Task _osdPauseTask = Task.CompletedTask;
    private Task _osdCloseTask = Task.CompletedTask;
    private bool _audioMuted;
    private bool _disposed;

    internal static async Task<MachineController> CreateAsync(MachineControllerOptions options)
    {
        MachineSession? session = null;
        MachineController? controller = null;
        try
        {
            session = new MachineSession(options.ModuleId, options.ConfigurationId, options.WindowTitle,
                options.Machine, options.MachineFactory, options.MountedMedia, options.PersistMountedMedia);
            controller = new MachineController(options, session);
            return controller;
        }
        finally
        {
            if (controller is null)
            {
                if (session is not null) await session.DisposeAsync();
                else await options.Machine.DisposeAsync();
            }
        }
    }

    private MachineController(MachineControllerOptions options, MachineSession session)
    {
        _options = options;
        _osdConfiguration = options.Configuration;
        _session = session;
        _presentationHost = this;
        _presentationFactory = options.CreatePresentation;
        var initialized = false;
        try
        {
            _view = _presentationFactory();
            _mediaDevices = options.MediaDevices;
            _prepareMediaAsync = options.PrepareMediaAsync;
            _audioMuted = options.Machine.Audio.IsMuted;
            _quickStates = new MachineQuickStates(() => _session.Machine.SavedStates, options.QuickStatePath);
            _video = new MachineVideoPresenter(_view, options.Machine, options.VideoRenderer, options.VideoProcessing);
            _video.FramePresented += FramePresented;
            _video.ShaderLoadingChanged += VideoShaderLoadingChanged;
            VideoShaderLoadingChanged(_video.IsShaderLoading);
            _session.MachineChanged += MachineChanged;
            _input = new MachineInputController(_view, _video.InputView, _video.InputHandle, () => _session.Machine, options.GlobalShortcuts, ExecuteShortcutAsync,
                () => PresentationActivation?.Invoke() ?? options.IsActive(), inputHandleProvider: () => _video.InputHandle,
                osdShortcuts: EmulationShortcutMap.OsdShortcuts(options.ShortcutSettings), isFullscreen: () => IsFullscreen,
                readShortcutSettings: options.ReadShortcutSettings, isOsdEditing: () => _osd?.IsEditing == true);
            _commands = new MachineCommandBar(_view.Toolbar, CreateActions(), options.GlobalShortcuts, options.ShowError, _input.RestoreFocus);
            _commands.Presentation.StatusAboveDisplay = MachinePresentationFunctions.StatusAboveDisplay(options.ModuleId, options.MachineId);
            _view.AttachCommands(_commands.Presentation);
            _commands.SetSavedStateAvailability(_quickStates.IsSupported, _quickStates.IsAvailable);
            _video.SurfaceChanged += VideoSurfaceChanged;
            _commands.RendererStatus.Text = MachinePresentationFunctions.RendererName(_video.Renderer);
            _commands.SetPowered(false);
            _commands.SetMuted(_audioMuted);
            _video.SetVisible(false);
            RebuildMediaDevices();
            Content = _view;
            initialized = true;
        }
        finally
        {
            if (!initialized)
            {
                _session.MachineChanged -= MachineChanged;
                if (_video is not null)
                {
                    _video.FramePresented -= FramePresented;
                    _video.ShaderLoadingChanged -= VideoShaderLoadingChanged;
                    _video.SurfaceChanged -= VideoSurfaceChanged;
                }
                EmulationVideoCleanupFunctions.RunAll(() => _input?.Dispose(), () => _video?.Dispose(), () => _view?.Dispose());
            }
        }
    }

    internal IEmulatedMachine Machine => _session.Machine;
    internal bool IsFullscreen => _fullscreenWindow is not null;
    internal EmulationMachinePresentation Presentation => _view;
    internal ContentControl PresentationHost => _presentationHost;
    internal Func<EmulationMachinePresentation> PresentationFactory => _presentationFactory;
    internal Func<bool>? PresentationActivation { get; set; }
    internal bool IsStopped => _disposed;

    internal void MovePresentationTo(ContentControl host)
    {
        if (ReferenceEquals(host, _presentationHost)) return;
        if (host.Content is not null) throw new InvalidOperationException(nameof(host));
        var previous = _presentationHost;
        if (!_disposed) _input.BeginHostTransition();
        try
        {
            if (ReferenceEquals(previous.Content, _view)) previous.Content = null;
            try
            {
                if (!_disposed) host.Content = _view;
                _presentationHost = host;
            }
            catch
            {
                if (!_disposed) previous.Content = _view;
                throw;
            }
        }
        finally { if (!_disposed) _input.CompleteHostTransition(); }
    }

    internal void SetPresentation(Func<EmulationMachinePresentation> create)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_presentationFactory == create) return;
        var previous = _view;
        var next = create() ?? throw new InvalidOperationException(nameof(create));
        if (ReferenceEquals(previous, next)) return;
        var committed = false;
        try
        {
            next.AttachCommands(_commands.Presentation);
            if (IsFullscreen) ExitFullscreen();
            _input.BeginHostTransition();
            try
            {
                _presentationHost.Content = null;
                _commands.Rehost(next.Toolbar);
                _video.SetPresentation(next);
                _input.SetPresentation(next);
                _view = next;
                RebuildMediaDevices();
                _presentationHost.Content = next;
                _presentationFactory = create;
                committed = true;
            }
            catch
            {
                _view = previous;
                try
                {
                    _commands.Rehost(previous.Toolbar);
                    _video.SetPresentation(previous);
                    _input.SetPresentation(previous);
                    RebuildMediaDevices();
                }
                finally { _presentationHost.Content = previous; }
                throw;
            }
            finally { _input.CompleteHostTransition(); }
        }
        finally
        {
            if (committed) previous.Dispose();
            else next.Dispose();
        }
    }

    internal async Task StopAsync()
    {
        if (_disposed) return;
        _disposed = true;
        try
        {
            try { ExitFullscreen(); }
            finally { await _osdCloseTask; }
        }
        finally
        {
            _video.FramePresented -= FramePresented;
            _video.ShaderLoadingChanged -= VideoShaderLoadingChanged;
            _video.SurfaceChanged -= VideoSurfaceChanged;
            _session.MachineChanged -= MachineChanged;
            try { EmulationVideoCleanupFunctions.RunAll(_input.Dispose, _video.Dispose); }
            finally
            {
                try { EmulationVideoShaderLoadingStatus.Set(_options.ModuleId, _options.ConfigurationId, false); }
                finally
                {
                    try { await _session.DisposeAsync(); }
                    finally
                    {
                        try { _view.Dispose(); }
                        finally
                        {
                            if (ReferenceEquals(_presentationHost.Content, _view)) _presentationHost.Content = null;
                        }
                    }
                }
            }
        }
    }

    internal void ApplyVideoRenderer(EmulationVideoRenderer renderer)
    {
        _video.SetRenderer(renderer);
        _commands.RendererStatus.Text = MachinePresentationFunctions.RendererName(_video.Renderer);
    }

    internal void ApplyVideoConfiguration(EmulationVideoRenderer renderer, EmulationVideoProcessingConfiguration videoProcessing)
    {
        _video.SetVideoProcessing(videoProcessing);
        ApplyVideoRenderer(renderer);
    }

    internal async Task ApplyRuntimeOptionsAsync(IReadOnlyDictionary<string, string> options)
    {
        if (_disposed || _session.Machine.State is not EmulationMachineState.Running
            and not EmulationMachineState.Paused) return;
        foreach (var option in EmulationRuntimeOptionFunctions.HotChanges(options, _session.Machine.Runtime.AvailableOptions))
            await _session.Machine.Runtime.SetOptionAsync(option.Key, option.Value);
    }

    internal async Task ApplyConfigurationAsync(IEmulationConfiguration configuration)
    {
        if (_disposed) return;
        if (_session.IsPowered)
        {
            _session.Machine.Input.UpdateBindings(configuration);
            _input.RefreshBindings();
        }
        var runtime = await _options.CreateRuntimeAsync(configuration, CancellationToken.None);
        _mediaDevices = runtime.MediaDevices;
        _prepareMediaAsync = runtime.PrepareMediaAsync;
        await _session.UpdateMachineFactoryAsync(runtime.CreateMachine, runtime.MediaDevices, runtime.MountedMedia);
        RebuildMediaDevices();
        _osdConfiguration = configuration;
        if (_osd is not null)
            RefreshOsdModuleBlocks();
    }

    public async ValueTask DisposeAsync() => await StopAsync();

    private void VideoShaderLoadingChanged(bool isLoading)
    {
        var windowedLoading = isLoading && !IsFullscreen;
        _view.SetShaderLoading(windowedLoading);
        _osd?.SetShaderLoading(isLoading);
        _fullscreenShaderLoading?.SetVisible(isLoading && _osd is null);
        EmulationVideoShaderLoadingStatus.Set(_options.ModuleId, _options.ConfigurationId, windowedLoading);
    }

    private MachineCommandActions CreateActions() => new(
        TogglePowerAsync,
        TogglePauseAsync,
        () => _session.Machine.Lifecycle.SoftResetAsync().AsTask(),
        HardResetAsync,
        QuickSaveAsync,
        QuickLoadAsync,
        CaptureScreenAsync,
        ToggleFullscreenAsync,
        ToggleAudioAsync,
        _options.SwitchControllerPointer ?? (_session.Machine.Input.SupportsControllerPointerSwitch ? SwitchControllerPointerAsync : null),
        _options.OpenSettings);

    private async Task SwitchControllerPointerAsync()
    {
        var pointerMode = await _session.Machine.Input.SwitchControllerPointerAsync();
        _commands.SetControllerPointerMode(pointerMode);
    }

    private async Task TogglePowerAsync()
    {
        await _session.TogglePowerAsync();
        _video.ResetFrameRate();
        _input.SetPowered(_session.IsPowered);
        _commands.SetPowered(_session.IsPowered);
        _video.SetVisible(_session.IsPowered);
        if (!_session.IsPowered) _commands.Status.Text = string.Empty;
        RebuildMediaDevices();
    }

    private Task HardResetAsync()
    {
        var requiresRecreation = _mediaDevices.Any(device => device.RequiresMachineRecreation && _session.MountedMedia.Any(media => media.Slot == device.Slot));
        return requiresRecreation ? _session.RecreateRunningMachineAsync() : _session.Machine.Lifecycle.HardResetAsync().AsTask();
    }

    private async Task TogglePauseAsync()
    {
        await _session.TogglePauseAsync();
        _video.ResetFrameRate();
        if (_session.Machine.Video.LatestFrame is { } frame)
            MachinePresentationFunctions.UpdateStatus(_commands.Presentation, frame, _session.Machine.Video.FramesPerSecond, _video.MeasuredFramesPerSecond);
        _commands.SetPaused(_session.Machine.State == EmulationMachineState.Paused);
    }

    private Task ToggleAudioAsync()
    {
        SetAudioMuted(!_audioMuted);
        return Task.CompletedTask;
    }

    private void SetAudioMuted(bool muted)
    {
        _session.Machine.Audio.SetMuted(muted);
        _audioMuted = muted;
        _commands.SetMuted(muted);
    }

    private async Task QuickSaveAsync()
    {
        if (await _quickStates.SaveAsync()) _commands.SetSavedStateAvailability(true, true);
    }

    private async Task QuickLoadAsync()
    {
        await _quickStates.LoadAsync();
    }

    private async Task CaptureScreenAsync()
    {
        var snapshot = await _video.CaptureSnapshotAsync() ?? throw new InvalidOperationException(LocExtension.Get("Emulation.Shortcut.Unavailable"));
        var capture = snapshot.Clone();
        capture.Freeze();
        var path = await Task.Run(() => MachineCaptureFunctions.Save(capture, _options.CaptureFolder, DateTime.Now));
        _commands.Status.Text = Path.GetFileName(path);
    }

    private Task ToggleFullscreenAsync()
    {
        if (_fullscreenWindow is null) EnterFullscreen();
        else ExitFullscreen();
        return Task.CompletedTask;
    }

    private void EnterFullscreen()
    {
        _input.BeginHostTransition();
        _view.DisplayHost.Children.Remove(_view.Screen);
        _fullscreenHost = new Grid
        {
            Background = Brushes.Black,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        _fullscreenHost.Children.Add(_view.Screen);
        _fullscreenShaderLoading = new FullscreenOsdIndicator(_fullscreenHost, LocExtension.Get(EmulationResourceKeys.VideoShaderLoading));
        _video.SetDisplayHost(_fullscreenHost);
        _fullscreenWindow = new Window
        {
            Title = _options.WindowTitle,
            Content = _fullscreenHost,
            WindowStyle = WindowStyle.None,
            WindowState = WindowState.Maximized,
            Background = Brushes.Black
        };
        _fullscreenWindow.Closed += FullscreenWindowClosed;
        _fullscreenWindow.ContentRendered += FullscreenContentRendered;
        _fullscreenWindow.Show();
        _fullscreenWindow.Activate();
        VideoShaderLoadingChanged(_video.IsShaderLoading);
        _input.CompleteHostTransition();
    }

    private void ExitFullscreen(bool closeWindow = true)
    {
        if (_fullscreenWindow is null) return;
        _input.BeginHostTransition();
        var window = _fullscreenWindow;
        window.Closed -= FullscreenWindowClosed;
        window.ContentRendered -= FullscreenContentRendered;
        _fullscreenHost?.Children.Remove(_view.Screen);
        window.Content = null;
        _fullscreenHost = null;
        _fullscreenWindow = null;
        var shaderPopup = _fullscreenShaderLoading;
        _fullscreenShaderLoading = null;
        try { shaderPopup?.Dispose(); }
        finally
        {
            try { CloseOsd(); }
            finally
            {
                try
                {
                    try
                    {
                        _view.DisplayHost.Children.Add(_view.Screen);
                        _video.SetDisplayHost(_view.DisplayHost);
                    }
                    finally { if (closeWindow) window.Close(); }
                    VideoShaderLoadingChanged(_video.IsShaderLoading);
                }
                finally { _input.CompleteHostTransition(); }
            }
        }
    }

    private void FullscreenWindowClosed(object? sender, EventArgs args) => ExitFullscreen(closeWindow: false);

    private void FullscreenContentRendered(object? sender, EventArgs args)
    {
        _video.FitScreen();
        _input.RestoreFocus();
    }

    private async Task ToggleOsdAsync(bool pause)
    {
        if (_disposed || _fullscreenWindow is not { } owner || _fullscreenHost is not { } host) return;
        await _osdToggleGate.WaitAsync();
        try
        {
            await _osdCloseTask;
            if (_disposed || !ReferenceEquals(owner, _fullscreenWindow)) return;
            if (_osd is not null)
            {
                CloseOsd();
                return;
            }

            var osd = new FullscreenOsdWindow(owner, host, _options.WindowTitle, OsdGameName(), CreateOsdEntries(),
                _options.ShowError, LocExtension.Get(EmulationResourceKeys.VideoShaderLoading), CreateOsdFooterHints());
            _osd = osd;
            var opened = false;
            try
            {
                osd.PreviewKeyDown += OsdKeyDown;
                osd.PreviewKeyUp += OsdKeyUp;
                osd.Closed += OsdClosed;
                _input.SetOsdFocus(true);
                VideoShaderLoadingChanged(_video.IsShaderLoading);
                osd.Show();
                if (pause && _session.Machine.State == EmulationMachineState.Running)
                {
                    _osdPausedEmulation = true;
                    _osdPauseTask = TogglePauseAsync();
                    await _osdPauseTask;
                }
                opened = true;
            }
            finally
            {
                if (!opened) CloseOsd();
            }
        }
        finally { _osdToggleGate.Release(); }
    }

    private void CloseOsd()
    {
        if (_osd is not { } osd) return;
        try { osd.Close(); }
        finally
        {
            if (ReferenceEquals(_osd, osd)) OsdClosed(osd, EventArgs.Empty);
        }
    }

    private void OsdClosed(object? sender, EventArgs args)
    {
        if (sender is not FullscreenOsdWindow osd || !ReferenceEquals(_osd, osd)) return;
        _osd = null;
        osd.PreviewKeyDown -= OsdKeyDown;
        osd.PreviewKeyUp -= OsdKeyUp;
        osd.Closed -= OsdClosed;
        try { _input.SetOsdFocus(false); }
        finally { _osdCloseTask = RestoreAfterOsdAsync(osd.PendingSettingSave); }
    }

    private async Task RestoreAfterOsdAsync(Task pendingSettingSave)
    {
        try
        {
            try { await pendingSettingSave; }
            catch (Exception error) { _options.ShowError(error); }
            if (_osdPausedEmulation)
            {
                await _osdPauseTask;
                if (!_disposed && _session.Machine.State == EmulationMachineState.Paused) await TogglePauseAsync();
            }
        }
        catch (Exception error) { _options.ShowError(error); }
        finally
        {
            _osdPausedEmulation = false;
            _osdPauseTask = Task.CompletedTask;
            if (!_disposed) VideoShaderLoadingChanged(_video.IsShaderLoading);
        }
    }

    private void OsdKeyDown(object sender, KeyEventArgs args)
    {
        args.Handled = true;
        if (!args.IsRepeat) _input.HandleKeyDown(args.Key == Key.System ? args.SystemKey : args.Key, Keyboard.Modifiers, args.Timestamp);
    }

    private void OsdKeyUp(object sender, KeyEventArgs args)
    {
        args.Handled = true;
        _input.HandleKeyUp(args.Key == Key.System ? args.SystemKey : args.Key, Keyboard.Modifiers);
    }

    private string? OsdGameName()
    {
        if (!_session.IsPowered) return null;
        var media = _session.MountedMedia.FirstOrDefault(item => item.IsInserted && item.Type is EmulationMediaType.Cartridge or EmulationMediaType.CompactDisc)
            ?? _session.MountedMedia.FirstOrDefault(item => item.IsInserted && item.Type != EmulationMediaType.MemoryCard);
        return Path.GetFileNameWithoutExtension(media?.Path);
    }

    private IReadOnlyList<FullscreenOsdWindow.Entry> CreateOsdEntries()
    {
        RefreshOsdModuleBlocks();
        bool IsPowered() => _session.IsPowered;
        return
        [
            new(LocExtension.Get("Emulation.Shortcut.OsdResume"), () => { CloseOsd(); return Task.CompletedTask; },
                Glyph: MachineCommandGlyphConstants.Continue),
            new(LocExtension.Get("Emulation.Shortcut.OsdSavedStates"), Children:
            [
                new(LocExtension.Get(EmulationResourceKeys.QuickSave), QuickSaveAsync, IsEnabled: () => IsPowered() && _quickStates.IsSupported,
                    Glyph: MachineCommandGlyphConstants.QuickSave),
                new(LocExtension.Get(EmulationResourceKeys.QuickLoad), QuickLoadAsync, IsEnabled: () => IsPowered() && _quickStates.IsAvailable,
                    Glyph: MachineCommandGlyphConstants.QuickLoad)
            ], Glyph: MachineCommandGlyphConstants.QuickSave),
            OsdVideoSettings(),
            OsdAudioSettings(),
            OsdControlsSettings(),
            OsdMachineSettings(),
            new(LocExtension.Get("Emulation.Shortcut.OsdExitFullscreen"), ToggleFullscreenAsync,
                Glyph: MachineCommandGlyphConstants.Power, SeparatorBefore: true)
        ];
    }

    private FullscreenOsdWindow.FooterHints CreateOsdFooterHints()
    {
        return new(
            Keycaps(EmulationShortcutDefaults.OsdUp, EmulationShortcutDefaults.OsdDown),
            Keycaps(EmulationShortcutDefaults.OsdLeft, EmulationShortcutDefaults.OsdRight),
            Keycaps(EmulationShortcutDefaults.OsdConfirm),
            Keycaps(EmulationShortcutDefaults.OsdCancel),
            LocExtension.Get("Emulation.Shortcut.OsdNavigateHint"),
            LocExtension.Get("Common.Modify"),
            LocExtension.Get("Emulation.Shortcut.OsdConfirmHint"),
            LocExtension.Get("Emulation.Shortcut.OsdBackHint"));

        string[] Keycaps(params string[] actions) => actions
            .SelectMany(action => _input.OsdShortcuts
                .Where(binding => EmulationShortcutMap.OsdPrimaryAction(binding.Action) == action)
                .Select(Keycap))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        static string Keycap(GlobalShortcutBinding binding)
        {
            if (binding.SourceBinding is { } source)
                return string.Join(" + ", source.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                    .Select(RetroMediaLab.App.ViewModels.Input.InputBindingRow.DisplayPart));
            var chord = binding.Chord;
            if (chord.Keys.Count != 1 || chord.Modifiers != ModifierKeys.None)
                return KeyboardChordFunctions.Format(chord.Modifiers, chord.Keys);
            return chord.Keys[0] switch
            {
                Key.Up => "↑", Key.Down => "↓", Key.Left => "←", Key.Right => "→",
                Key.Return => "↵", Key.Escape => "Esc", var key => key.ToString()
            };
        }
    }

    private void MachineChanged(object? sender, IEmulatedMachine machine)
    {
        _input.RefreshBindings();
        _video.SetMachine(machine);
        machine.Audio.SetMuted(_audioMuted);
        _commands.SetMuted(_audioMuted);
        _commands.SetSavedStateAvailability(_quickStates.IsSupported, _quickStates.IsAvailable);
        RebuildMediaDevices();
    }

    private void RebuildMediaDevices()
    {
        _osd?.SetGameName(OsdGameName());
        _view.SetDevices(_mediaDevices.Select(device =>
        {
            var canOpenMedia = device.IsRemovable && (device.MediaType != EmulationMediaType.MemoryCard
                || device.ConfigurationKind == EmulationStorageConfigurationKind.MemoryCard);
            var mounted = _session.MountedMedia.FirstOrDefault(media => media.Slot == device.Slot);
            var tape = device.MediaType == EmulationMediaType.Tape ? _session.Machine.TapeTransport : null;
            var state = tape?.State ?? EmulationTapeState.Empty;
            var activeOperation = tape?.ActiveOperation;
            return new MachineViewDevice(device.Slot.ToString(), device.DisplayLabel ?? device.Slot.ToString(),
                DeviceGlyph(device.MediaType), canOpenMedia, mounted is not null,
                canOpenMedia ? () => InsertMediaAsync(device) : null,
                canOpenMedia && mounted is not null ? () => EjectMediaAsync(device) : null,
                device.MediaType == EmulationMediaType.Tape ? TapeStateText(state) : null,
                device.MediaType == EmulationMediaType.Tape ? TapeCommandOrder.Select(command => TapeCommand(command, tape, state, activeOperation)).ToArray() : null, device.MediaType,
                EmulationMediaIconFunctions.Select(device));
        }), _options.ShowError, _input.RestoreFocus);
    }

    private MachineViewDeviceCommand TapeCommand(EmulationTapeCommand command, IEmulationTapeTransport? transport, EmulationTapeState state, EmulationTapeCommand? activeOperation)
    {
        var supported = transport?.AvailableCommands.Contains(command) == true;
        return new MachineViewDeviceCommand(command, TapeCommandText(command),
            TapeCommandGlyph(command), supported,
            TapeTransportPresentationFunctions.IsEnabled(_session.IsPowered, supported, state, activeOperation, command),
            TapeTransportPresentationFunctions.IsActive(state, activeOperation, command),
            TapeTransportPresentationFunctions.IsBlinking(state, activeOperation, command),
            () => ExecuteTapeCommandAsync(command));
    }

    private async Task InsertMediaAsync(EmulationMediaDevice device)
    {
        if (device.ConfigurationKind == EmulationStorageConfigurationKind.MemoryCard)
        {
            await ConfigureMemoryCardAsync(device);
            return;
        }
        var extensions = device.AcceptedExtensions.Select(extension => $"*{extension}").ToArray();
        var dialog = new OpenFileDialog
        {
            Filter = $"{LocExtension.Get("Emulation.Storage.Media.Associated")} ({string.Join(";", extensions)})|{string.Join(";", extensions)}",
            InitialDirectory = _options.InitialMediaDirectory(device),
            ShowReadOnly = device.MediaType is EmulationMediaType.Floppy or EmulationMediaType.Tape,
            ClientGuid = EmulationMediaDialogFunctions.ClientGuid(_options.ModuleId, _options.MachineId, device.Slot)
        };
        if (WpfFileDialogService.ShowFileDialog(dialog, Window.GetWindow(this)) != true) return;
        var directory = Path.GetDirectoryName(dialog.FileName);
        if (!string.IsNullOrWhiteSpace(directory)) _options.RememberMediaDirectory(device, directory);
        var media = new EmulationMedia(dialog.FileName, device.Slot, device.MediaType, dialog.ReadOnlyChecked || new FileInfo(dialog.FileName).IsReadOnly, true);
        if (_prepareMediaAsync is not null)
            media = await _prepareMediaAsync(media, CancellationToken.None);
        await _session.InsertAsync(media, device.RequiresMachineRecreation);
        RebuildMediaDevices();
    }

    private async Task ConfigureMemoryCardAsync(EmulationMediaDevice device)
    {
        var current = _session.MountedMedia.FirstOrDefault(media => media.Slot == device.Slot)?.Path;
        var folder = device.ImageDirectory ?? (device.DefaultImageSubdirectory is { } subdirectory
            ? Path.Combine(StoragePaths.EmulationStorageDirectory, subdirectory)
            : _options.InitialMediaDirectory(device) ?? StoragePaths.EmulationStorageDirectory);
        string? selected;
        if (device.UsesDirectory)
        {
            var dialog = new MemoryCardDirectoryConfigurationDialog(device.DisplayLabel ?? device.Slot.ToString(), current, folder);
            try { if (WpfWindowNavigationService.ShowOwnedDialog(Window.GetWindow(this), dialog) != true) return; selected = dialog.SupportPath; }
            finally { WpfWindowNavigationService.CloseOwnedDialog(dialog); }
        }
        else
        {
            var dialog = new HardDiskDriveConfigurationDialog(device.DisplayLabel ?? device.Slot.ToString(),
                _options.WindowTitle, current, folder, device.HardDiskFormats ?? [],
                EmulationMediaDialogFunctions.ClientGuid(_options.ModuleId, _options.MachineId, device.Slot),
                fixedImageSizeBytes: device.FixedImageSizeBytes, defaultImageFileName: device.DefaultImageFileName,
                presentation: device.DialogPresentation);
            try { if (WpfWindowNavigationService.ShowOwnedDialog(Window.GetWindow(this), dialog) != true) return; selected = dialog.SupportPath; }
            finally { WpfWindowNavigationService.CloseOwnedDialog(dialog); }
        }
        if (string.IsNullOrWhiteSpace(selected))
            await _session.EjectAsync(device.Slot, device.RequiresMachineRecreation);
        else
            await _session.InsertAsync(new EmulationMedia(selected, device.Slot, EmulationMediaType.MemoryCard, false, true), device.RequiresMachineRecreation);
        RebuildMediaDevices();
    }

    private async Task EjectMediaAsync(EmulationMediaDevice device)
    {
        await _session.EjectAsync(device.Slot, device.RequiresMachineRecreation);
        RebuildMediaDevices();
    }

    private async Task ExecuteTapeCommandAsync(EmulationTapeCommand command)
    {
        var tape = _session.Machine.TapeTransport;
        if (tape is null) return;
        if (command == EmulationTapeCommand.Record)
        {
            var device = _mediaDevices.FirstOrDefault(device => device.MediaType == EmulationMediaType.Tape);
            if (device is null) return;
            var extensions = EmulationSdk.MediaProcessing.TapeRecordingExtensions.Where(extension => device.AcceptedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase)).ToArray();
            if (extensions.Length == 0) return;
            var dialog = new SaveFileDialog
            {
                Title = TapeCommandText(command),
                Filter = string.Join("|", extensions.Select(extension => $"{extension[1..].ToUpperInvariant()} (*{extension})|*{extension}")),
                AddExtension = true, DefaultExt = extensions[0], OverwritePrompt = true,
                InitialDirectory = _options.InitialMediaDirectory(device),
                ClientGuid = EmulationMediaDialogFunctions.ClientGuid(_options.ModuleId, _options.MachineId, device.Slot)
            };
            if (WpfFileDialogService.ShowFileDialog(dialog, Window.GetWindow(this)) != true) return;
            IReadOnlyDictionary<string, string>? pcm = null;
            var recordingRate = new EmulationTapePcmSettings().SampleRate;
            if (Path.GetExtension(dialog.FileName).Equals(EmulationSdk.MediaProcessing.FileExtensions["Raw"], StringComparison.OrdinalIgnoreCase))
            {
                var defaults = (device.ConfigurationFields ?? []).Concat((device.ConfigurationBlocks ?? []).SelectMany(block => block.Fields)).Where(field => field.Id.StartsWith(EmulationTapePcmSettings.Prefix, StringComparison.Ordinal) && field.Value is not null).ToDictionary(field => field.Id, field => field.Value!);
                var profileDialog = new ManagedStorageConfigurationDialog(TapeCommandText(command), "RAW", _options.ModuleId, EmulationTapePcmSettings.Describe(defaults), null);
                try
                {
                    if (WpfWindowNavigationService.ShowOwnedDialog(Window.GetWindow(this), profileDialog) != true) return;
                    var profile = EmulationTapePcmSettings.Read(profileDialog.Options);
                    pcm = profile.ToParameters(); recordingRate = profile.SampleRate;
                }
                finally { WpfWindowNavigationService.CloseOwnedDialog(profileDialog); }
            }
            var creation = new MediaCreationDialog(Window.GetWindow(this)!, TapeCommandText(command), dialog.FileName,
                () => EmulationSdk.MediaProcessing.WriteTapeAsync(dialog.FileName, new EmulationTapeSignal(ReadOnlyMemory<short>.Empty, recordingRate), parameters: pcm));
            try { if (WpfWindowNavigationService.ShowOwnedDialog(Window.GetWindow(this), creation) != true) return; }
            finally { WpfWindowNavigationService.CloseOwnedDialog(creation); }
            if (tape.AvailableCommands.Contains(EmulationTapeCommand.Stop)) await tape.ExecuteAsync(EmulationTapeCommand.Stop);
            var current = _session.MountedMedia.FirstOrDefault(media => media.Slot == device.Slot);
            var profileChanged = pcm is not null && (current?.Parameters is null || pcm.Any(pair => !current.Parameters.TryGetValue(pair.Key, out var value) || value != pair.Value));
            if (current is null || profileChanged || !Path.GetFullPath(current.Path).Equals(Path.GetFullPath(dialog.FileName), StringComparison.OrdinalIgnoreCase))
                await _session.InsertAsync(new EmulationMedia(dialog.FileName, device.Slot, EmulationMediaType.Tape, new FileInfo(dialog.FileName).IsReadOnly, true) { Parameters = pcm }, device.RequiresMachineRecreation);
            tape = _session.Machine.TapeTransport;
            if (tape is null) return;
            RebuildMediaDevices();
        }
        await tape.ExecuteAsync(command);
        UpdateTapeTransport(tape);
    }

    private void UpdateTapeTransport(IEmulationTapeTransport tape)
    {
        var key = EmulationMediaSlot.Tape0.ToString();
        _view.SetDeviceStatus(key, TapeStateText(tape.State));
        _view.SetTapeTransport(key, tape.State, tape.ActiveOperation, tape.AvailableCommands, _session.IsPowered);
    }

    private static string TapeStateText(EmulationTapeState state) => LocExtension.Get($"Emulation.Tape.State.{state}");

    private static string TapeCommandText(EmulationTapeCommand command) => LocExtension.Get($"Emulation.Tape.Command.{command}");

    private static string TapeCommandGlyph(EmulationTapeCommand command) => command switch
    {
        EmulationTapeCommand.Play => "▶",
        EmulationTapeCommand.Stop => "■",
        EmulationTapeCommand.Pause => "Ⅱ",
        EmulationTapeCommand.Rewind => "⏪",
        EmulationTapeCommand.FastForward => "⏩",
        EmulationTapeCommand.Record => "●",
        _ => "?"
    };

    private static string DeviceGlyph(EmulationMediaType type) => type switch
    {
        EmulationMediaType.Floppy => MachinePresentationConstants.FloppyGlyph,
        EmulationMediaType.HardDisk => MachinePresentationConstants.HardDiskGlyph,
        EmulationMediaType.CompactDisc => MachinePresentationConstants.CompactDiscGlyph,
        EmulationMediaType.Cartridge => MachinePresentationConstants.CartridgeGlyph,
        EmulationMediaType.Tape => MachinePresentationConstants.TapeGlyph,
        EmulationMediaType.MemoryCard => MachinePresentationConstants.MemoryCardGlyph,
        _ => MachinePresentationConstants.FloppyGlyph
    };

    private void VideoSurfaceChanged(object? sender, EventArgs args) => _input.SetInputView(_video.InputView, _video.InputHandle);

    private async Task ExecuteShortcutAsync(string action)
    {
        try
        {
            var primaryAction = EmulationShortcutMap.OsdPrimaryAction(action);
            switch (primaryAction)
            {
                case EmulationShortcutDefaults.OsdWithPause:
                    await ToggleOsdAsync(true);
                    break;
                case EmulationShortcutDefaults.OsdWithoutPause:
                    await ToggleOsdAsync(false);
                    break;
                case EmulationShortcutDefaults.OsdUp or EmulationShortcutDefaults.OsdDown
                    or EmulationShortcutDefaults.OsdLeft or EmulationShortcutDefaults.OsdRight
                    or EmulationShortcutDefaults.OsdConfirm or EmulationShortcutDefaults.OsdCancel:
                    if (_osd is { } osd) await osd.NavigateAsync(primaryAction switch
                    {
                        EmulationShortcutDefaults.OsdUp => FullscreenOsdWindow.Navigation.Up,
                        EmulationShortcutDefaults.OsdDown => FullscreenOsdWindow.Navigation.Down,
                        EmulationShortcutDefaults.OsdLeft => FullscreenOsdWindow.Navigation.Left,
                        EmulationShortcutDefaults.OsdRight => FullscreenOsdWindow.Navigation.Right,
                        EmulationShortcutDefaults.OsdConfirm => FullscreenOsdWindow.Navigation.Confirm,
                        _ => FullscreenOsdWindow.Navigation.Cancel
                    });
                    break;
                case EmulationShortcutDefaults.OpenSettings:
                    if (_options.OpenSettings is not null) await _options.OpenSettings();
                    break;
                case EmulationShortcutDefaults.Power: await TogglePowerAsync(); break;
                case EmulationShortcutDefaults.PauseResume: await TogglePauseAsync(); break;
                case EmulationShortcutDefaults.SoftReset:
                    await _session.Machine.Lifecycle.SoftResetAsync();
                    break;
                case EmulationShortcutDefaults.HardReset:
                    await HardResetAsync();
                    break;
                case EmulationShortcutDefaults.QuickSave: await QuickSaveAsync(); break;
                case EmulationShortcutDefaults.QuickLoad: await QuickLoadAsync(); break;
                case EmulationShortcutDefaults.Screenshot: await CaptureScreenAsync(); break;
                case EmulationShortcutDefaults.ToggleFullscreen: await ToggleFullscreenAsync(); break;
                case EmulationShortcutDefaults.ReleaseMouse: _input.ReleasePointer(); break;
                case EmulationShortcutDefaults.ToggleMute: await ToggleAudioAsync(); break;
            }
        }
        catch (Exception error) { _options.ShowError(error); }
        finally { _input.RestoreFocus(); }
    }

    private void FramePresented(object? sender, VideoFrame frame)
    {
        MachinePresentationFunctions.UpdateStatus(_commands.Presentation, frame, _session.Machine.Video.FramesPerSecond, _video.MeasuredFramesPerSecond, _video.ExecutionRate);
        var now = DateTime.UtcNow;
        var activityStates = _session.Machine.Runtime.MediaActivity;
        if (_session.Machine.TapeTransport is { } tape) UpdateTapeTransport(tape);
        foreach (var device in _mediaDevices)
        {
            if (!_view.DeviceLeds.TryGetValue(device.Slot.ToString(), out var led)) continue;
            if (activityStates.GetValueOrDefault(device.Slot))
                _mediaActivityUntil[device.Slot] = now + EmulationRuntimeConstants.MediaActivityPersistence;
            var active = _mediaActivityUntil.GetValueOrDefault(device.Slot) > now;
            var present = _session.MountedMedia.Any(media => media.Slot == device.Slot) || !device.IsRemovable;
            led.Fill = active ? Brushes.LimeGreen : present ? Brushes.ForestGreen : Brushes.Gray;
        }
        _commands.SetInputStatus(_input.IsPointerCaptured, GameInputControllerReader.ReadAll().Any(state => state != EmulationControllerState.Empty));
    }
}
