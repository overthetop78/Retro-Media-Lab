using System.ComponentModel;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.Theme.Common;

namespace RetroMediaLab.App.Theme.OS.Editions.Windows31;

/// <summary>Owns internal application frames and explicitly transferred presentation lifetimes.</summary>
internal sealed partial class Windows31Desktop : Grid, IDisposable
{
    private sealed record Entry(Windows31ApplicationDefinition Definition, Windows31Document Document,
        Windows31WindowPlacement Placement, Windows31Frame Frame, ListBoxItem Icon,
        PropertyChangedEventHandler StateHandler, MouseButtonEventHandler ActivateHandler);

    private const double InitialDesktopWidth = 1280;
    private const double InitialDesktopHeight = 800;
    private readonly IReadOnlyDictionary<Windows31ApplicationId, Windows31ApplicationDefinition> definitions;
    private readonly Canvas workspace = new() { ClipToBounds = true };
    private readonly Dictionary<Windows31ApplicationId, Entry> applications = [];
    private Windows31ProgramManager manager = null!;
    private Windows31WindowPlacement managerPlacement = null!;
    private Windows31Frame managerFrame = null!;
    private ListBox icons = null!;
    private ListBoxItem managerIcon = null!;
    private bool disposed;
    private int zIndex;
    private ThemeNativeWorkspaceHost? nativeWorkspace;

    internal Windows31Desktop(IReadOnlyList<Windows31ApplicationDefinition> definitions)
        : this(_ => definitions) { }

    internal Windows31Desktop(Func<Windows31Desktop, IReadOnlyList<Windows31ApplicationDefinition>> createDefinitions)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(createDefinitions);
            var definitions = createDefinitions(this);
            this.definitions = definitions.ToDictionary(item => item.Id);
            Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(Windows31Metrics.ControlsResource, UriKind.Relative) });
            SetResourceReference(TextElement.FontFamilyProperty, Windows31Metrics.BodyFontResource);
            SetValue(TextElement.FontSizeProperty, Windows31Metrics.BodyFontSize);
            TextOptions.SetTextFormattingMode(this, TextFormattingMode.Ideal);
            TextOptions.SetTextRenderingMode(this, TextRenderingMode.Aliased);
            Windows31PaletteResources.Initialize(this);
            SetResourceReference(BackgroundProperty, Windows31PaletteResources.Desktop);
            workspace.SetResourceReference(BackgroundProperty, Windows31PaletteResources.Desktop);
            Children.Add(workspace);
            nativeWorkspace = new ThemeNativeWorkspaceHost(workspace);
            nativeWorkspace.NativePointerPressed += NativePointerPressed;
            manager = new Windows31ProgramManager(definitions, id => Open(id));
            managerIcon = manager.CreateIcon(Windows31Metrics.ProgramManagerResource, null, Windows31ApplicationId.ProgramManager);
            icons = new ListBox { Style = manager.IconListStyle };
            icons.MouseDoubleClick += IconDoubleClick;
            icons.KeyDown += IconKeyDown;
            workspace.Children.Add(icons);
            managerPlacement = new Windows31WindowPlacement(DesktopBounds(),
                new Rect(Windows31Metrics.DesktopInset, Windows31Metrics.DesktopInset, Windows31Metrics.ManagerInitialWidth, Windows31Metrics.ManagerInitialHeight),
                new Size(Windows31Metrics.ApplicationMinimumWidth, Windows31Metrics.ApplicationMinimumHeight));
            managerFrame = new Windows31Frame(managerPlacement, manager);
            managerFrame.SetBinding(Windows31Frame.TitleProperty, LocExtension.CreateBinding(Windows31Metrics.ProgramManagerResource));
            managerFrame.PreviewMouseDown += ActivateManager;
            managerFrame.Closing += ManagerClosing;
            managerPlacement.PropertyChanged += ManagerPlacementChanged;
            workspace.Children.Add(managerFrame);
            manager.CloseRequested += ManagerCloseRequested;
            manager.DocumentationRequested += ManagerDocumentationRequested;
            manager.AboutRequested += ManagerAboutRequested;
            workspace.SizeChanged += WorkspaceSizeChanged;
            BringManagerForward();
            InitializeMachineWindows();
        }
        catch { Dispose(); throw; }
    }

    internal event EventHandler? PreferencesRequested;
    internal event EventHandler? EmulationPreferencesRequested;
    internal void EnablePreferences() => manager.EnablePreferences();
    internal void ConnectMenus(RetroMediaLab.App.Views.Controls.Shell.MainMenu source) => manager.ConnectMenus(source);
    internal event EventHandler? CloseRequested;
    internal event EventHandler? DocumentationRequested;
    internal event EventHandler? AboutRequested;
    internal event Action<Windows31ApplicationId>? ApplicationActivated;
    internal Windows31WindowPlacement ProgramManagerPlacement => managerPlacement;
    internal IReadOnlyList<Windows31WindowPlacement> OpenApplications => applications.Values.Select(item => item.Placement).ToArray();

    private Rect DesktopBounds() => new(0, 0,
        workspace.ActualWidth > 0 ? workspace.ActualWidth : InitialDesktopWidth,
        workspace.ActualHeight > 0 ? workspace.ActualHeight : InitialDesktopHeight);

    internal Windows31WindowPlacement Open(Windows31ApplicationId id)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (id == Windows31ApplicationId.Preferences && PreferencesRequested is not null)
        {
            PreferencesRequested.Invoke(this, EventArgs.Empty);
            return managerPlacement;
        }
        if (id == Windows31ApplicationId.EmulationPreferences && EmulationPreferencesRequested is not null)
        {
            EmulationPreferencesRequested.Invoke(this, EventArgs.Empty);
            return managerPlacement;
        }
        if (id == Windows31ApplicationId.ProgramManager) { BringManagerForward(); return managerPlacement; }
        if (id == Windows31ApplicationId.Emulation && machineSource is not null && !definitions.ContainsKey(id))
        {
            BringManagerForward();
            manager.ShowGroup(Windows31ProgramGroup.Emulation);
            return managerPlacement;
        }
        if (applications.TryGetValue(id, out var existing))
        {
            Activate(existing);
            return existing.Placement;
        }
        var definition = definitions[id];
        Windows31Document? document = null;
        Windows31Frame? frame = null;
        Entry? entry = null;
        try
        {
            document = definition.CreateDocument() ?? throw new InvalidOperationException(nameof(definition.CreateDocument));
            var area = DesktopBounds();
            var offset = Windows31Metrics.DesktopInset + applications.Count * Windows31Metrics.CascadeStep;
            var placement = new Windows31WindowPlacement(area, new Rect(offset, offset, definition.InitialSize.Width, definition.InitialSize.Height),
                new Size(Windows31Metrics.ApplicationMinimumWidth, Windows31Metrics.ApplicationMinimumHeight));
            frame = new Windows31Frame(placement, document.View);
            frame.SetBinding(Windows31Frame.TitleProperty, LocExtension.CreateBinding(definition.TitleResource));
            PropertyChangedEventHandler state = (_, _) => { if (entry is not null) UpdateEntry(entry); };
            MouseButtonEventHandler activate = (_, _) => { if (entry is not null) Activate(entry); };
            entry = new Entry(definition, document, placement, frame, manager.CreateIcon(definition.TitleResource, definition.Icon, id), state, activate);
            applications.Add(id, entry);
            placement.PropertyChanged += state;
            frame.PreviewMouseDown += activate;
            workspace.Children.Add(frame);
            Activate(entry);
            return placement;
        }
        catch
        {
            if (entry is not null && applications.ContainsKey(id)) ReleaseEntry(entry);
            else
            {
                try { frame?.Dispose(); }
                finally { document?.Dispose(); }
            }
            throw;
        }
    }

    internal void CloseApplication(Windows31ApplicationId id)
    {
        if (applications.TryGetValue(id, out var application)) application.Placement.Close();
    }

    private void Activate(Entry entry)
    {
        DeactivateMachineWindows();
        DeactivateDialogs();
        managerPlacement.Deactivate();
        foreach (var other in applications.Values) if (!ReferenceEquals(entry, other)) other.Placement.Deactivate();
        entry.Placement.Activate();
        Panel.SetZIndex(entry.Frame, ++zIndex);
        ActivateOwnedDialogs(entry);
        ApplicationActivated?.Invoke(entry.Definition.Id);
    }

    private void NativePointerPressed(FrameworkElement layer)
    {
        if (ReferenceEquals(layer, managerFrame)) { BringManagerForward(); return; }
        if (ActivateNativeDialog(layer)) return;
        if (ActivateNativeMachine(layer)) return;
        var entry = applications.Values.FirstOrDefault(item => ReferenceEquals(item.Frame, layer));
        if (entry is not null) Activate(entry);
    }

    private void UpdateEntry(Entry entry)
    {
        if (disposed) return;
        if (entry.Placement.State == Windows31WindowState.Closed) { ReleaseEntry(entry); return; }
        var reduced = entry.Placement.State == Windows31WindowState.Iconified;
        if (reduced && !icons.Items.Contains(entry.Icon)) icons.Items.Add(entry.Icon);
        if (!reduced) icons.Items.Remove(entry.Icon);
        ArrangeIcons();
    }

    private void ReleaseEntry(Entry entry)
    {
        if (!applications.Remove(entry.Definition.Id)) return;
        entry.Placement.PropertyChanged -= entry.StateHandler;
        entry.Frame.PreviewMouseDown -= entry.ActivateHandler;
        try
        {
            try { ReleaseOwnedDialogs(entry.Definition.Id); }
            finally
            {
                try { nativeWorkspace?.ReleaseLayer(entry.Frame); }
                finally { entry.Frame.Dispose(); }
            }
        }
        finally
        {
            try { entry.Document.Dispose(); }
            finally
            {
                workspace.Children.Remove(entry.Frame);
                icons.Items.Remove(entry.Icon);
                ArrangeIcons();
                if (!disposed)
                {
                    var next = applications.Values.Where(item => item.Placement.IsVisible).OrderByDescending(item => Panel.GetZIndex(item.Frame)).FirstOrDefault();
                    if (next is not null) Activate(next);
                    else BringManagerForward();
                }
            }
        }
    }

    private void BringManagerForward()
    {
        DeactivateMachineWindows();
        DeactivateDialogs();
        foreach (var entry in applications.Values) entry.Placement.Deactivate();
        managerPlacement.Activate();
        Panel.SetZIndex(managerFrame, ++zIndex);
    }

    private void ActivateManager(object sender, MouseButtonEventArgs args) => BringManagerForward();
    private void ManagerPlacementChanged(object? sender, PropertyChangedEventArgs args)
    {
        manager.SetApplicationActive(managerPlacement.IsActive && managerPlacement.IsVisible);
        if (managerPlacement.State == Windows31WindowState.Iconified)
        {
            if (!icons.Items.Contains(managerIcon)) icons.Items.Add(managerIcon);
        }
        else icons.Items.Remove(managerIcon);
        ArrangeIcons();
    }
    private void ManagerClosing(object? sender, CancelEventArgs args)
    {
        args.Cancel = true;
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
    private void ManagerCloseRequested(object? sender, EventArgs args) => CloseRequested?.Invoke(this, args);
    private void ManagerDocumentationRequested(object? sender, EventArgs args) => DocumentationRequested?.Invoke(this, args);
    private void ManagerAboutRequested(object? sender, EventArgs args) => AboutRequested?.Invoke(this, args);

    private void RestoreSelectedIcon()
    {
        if (icons.SelectedItem is ListBoxItem icon && RestoreMachineIcon(icon.Tag)) return;
        if (icons.SelectedItem is ListBoxItem { Tag: Windows31ApplicationId id }) Open(id);
    }
    private void IconDoubleClick(object sender, MouseButtonEventArgs args) { if (args.ChangedButton == MouseButton.Left) RestoreSelectedIcon(); }
    private void IconKeyDown(object sender, KeyEventArgs args) { if (args.Key == Key.Enter) { RestoreSelectedIcon(); args.Handled = true; } }
    private void ArrangeIcons()
    {
        icons.Width = DesktopBounds().Width;
        Canvas.SetTop(icons, Math.Max(0, DesktopBounds().Height - Windows31Metrics.IconHeight));
    }
    private void WorkspaceSizeChanged(object sender, SizeChangedEventArgs args)
    {
        if (disposed || workspace.ActualWidth <= 0 || workspace.ActualHeight <= 0) return;
        var area = DesktopBounds();
        managerPlacement.SetDesktop(area);
        foreach (var entry in applications.Values) entry.Placement.SetDesktop(area);
        foreach (var entry in machineWindows.Values) entry.Placement.SetDesktop(area);
        foreach (var dialog in dialogs.Values) dialog.Placement.SetDesktop(area);
        foreach (var device in deviceWindows.Values) device.Placement.SetDesktop(area);
        ArrangeIcons();
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        Exception? failure = null;
        try
        {
            try { DisconnectMachines(); }
            catch (Exception error) { failure ??= error; }
            if (nativeWorkspace is not null)
            {
                nativeWorkspace.NativePointerPressed -= NativePointerPressed;
                try { nativeWorkspace.Dispose(); }
                catch (Exception error) { failure ??= error; }
                finally { nativeWorkspace = null; }
            }
            foreach (var dialog in dialogs.Values.ToArray())
                try { ReleaseDialog(dialog); }
                catch (Exception error) { failure ??= error; }
            foreach (var entry in applications.Values.ToArray())
                try { ReleaseEntry(entry); }
                catch (Exception error) { failure ??= error; }
        }
        finally
        {
            workspace.SizeChanged -= WorkspaceSizeChanged;
            try
            {
                if (managerFrame is not null)
                {
                    managerFrame.PreviewMouseDown -= ActivateManager;
                    managerFrame.Closing -= ManagerClosing;
                    managerFrame.Dispose();
                }
            }
            finally
            {
                if (managerPlacement is not null) managerPlacement.PropertyChanged -= ManagerPlacementChanged;
                try
                {
                    if (manager is not null)
                    {
                        manager.CloseRequested -= ManagerCloseRequested;
                        manager.DocumentationRequested -= ManagerDocumentationRequested;
                        manager.AboutRequested -= ManagerAboutRequested;
                        manager.Dispose();
                    }
                }
                finally
                {
                    if (icons is not null) { icons.MouseDoubleClick -= IconDoubleClick; icons.KeyDown -= IconKeyDown; icons.Items.Clear(); }
                    workspace.Children.Clear();
                    Children.Clear();
                    ApplicationActivated = null;
                    CloseRequested = null;
                    PreferencesRequested = null;
                    EmulationPreferencesRequested = null;
                    DocumentationRequested = null;
                    AboutRequested = null;
                }
            }
        }
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
