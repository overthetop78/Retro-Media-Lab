using System.ComponentModel;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GWGUI.App.Localization.Extensions;

namespace GWGUI.App.Theme.OS.Editions.Windows31;

internal sealed partial class Windows31Desktop
{
    private sealed record DialogEntry(Windows31ApplicationId Owner, Windows31DialogKind Kind,
        Windows31DialogMode Mode, Windows31Document Document, Windows31WindowPlacement Placement,
        Windows31Frame Frame, PropertyChangedEventHandler StateHandler, MouseButtonEventHandler ActivateHandler,
        CancelEventHandler? ClosingHandler, Windows31DialogKind? ParentKind, Window? Instance, Window? ParentInstance)
    {
        internal Dictionary<Windows31Frame, bool> DisabledFrames { get; } = [];
    }

    private readonly Dictionary<(Windows31ApplicationId Owner, Windows31DialogKind Kind, Window? Instance), DialogEntry> dialogs = [];

    internal Windows31WindowPlacement ShowDialog(Windows31ApplicationId owner, Windows31DialogKind kind,
        Windows31DialogMode mode, string titleResource, Size size, Func<Action, Windows31Document> create, Action? requestClose = null, Windows31DialogKind? parent = null, Window? instance = null, Window? parentInstance = null)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(create);
        var managerOwned = owner == Windows31ApplicationId.ProgramManager;
        var ownerFrame = managerOwned ? managerFrame : applications[owner].Frame;
        var ownerPlacement = managerOwned ? managerPlacement : applications[owner].Placement;
        var modal = dialogs.Values.FirstOrDefault(dialog => dialog.Owner == owner && dialog.Mode == Windows31DialogMode.Modal
            && (parent is null || (dialog.ParentKind == parent && ReferenceEquals(dialog.ParentInstance, parentInstance))));
        if (modal is not null) { ActivateDialog(modal); return modal.Placement; }
        if (dialogs.TryGetValue((owner, kind, instance), out var existing))
        {
            ActivateDialog(existing);
            return existing.Placement;
        }

        Windows31Document? document = null;
        Windows31Frame? frame = null;
        DialogEntry? entry = null;
        try
        {
            document = create(() => CloseDialog(owner, kind, instance)) ?? throw new InvalidOperationException(nameof(create));
            var bounds = parent is { } parentKind && dialogs.TryGetValue((owner, parentKind, parentInstance), out var parentDialog)
                ? parentDialog.Placement.Bounds : ownerPlacement.Bounds;
            var center = new Point(bounds.Left + (bounds.Width - size.Width) / 2,
                bounds.Top + (bounds.Height - size.Height) / 2);
            var placement = new Windows31WindowPlacement(DesktopBounds(), new Rect(center, size),
                new Size(Math.Min(size.Width, Windows31Metrics.ApplicationMinimumWidth), Math.Min(size.Height, Windows31Metrics.ApplicationMinimumHeight)));
            frame = new Windows31Frame(placement, document.View);
            frame.ConfigureAsDialog();
            frame.SetBinding(Windows31Frame.TitleProperty, LocExtension.CreateBinding(titleResource));
            PropertyChangedEventHandler changed = (_, _) =>
            {
                if (entry is not null && entry.Placement.State == Windows31WindowState.Closed) ReleaseDialog(entry);
            };
            MouseButtonEventHandler activate = (_, _) => { if (entry is not null) ActivateDialog(entry); };
            CancelEventHandler? closing = requestClose is null ? null : (_, args) =>
            {
                args.Cancel = true;
                requestClose();
            };
            entry = new DialogEntry(owner, kind, mode, document, placement, frame, changed, activate, closing, parent, instance, parentInstance);
            if (closing is not null) frame.Closing += closing;
            dialogs.Add((owner, kind, instance), entry);
            placement.PropertyChanged += changed;
            frame.PreviewMouseDown += activate;
            workspace.Children.Add(frame);
            if (mode == Windows31DialogMode.Modal)
            {
                var targets = managerOwned
                    ? applications.Values.Select(item => item.Frame)
                        .Concat(machineWindows.Values.Select(item => item.Frame))
                        .Concat(deviceWindows.Values.Select(item => item.Frame))
                        .Concat(dialogs.Values.Where(item => !ReferenceEquals(item, entry)).Select(item => item.Frame))
                        .Prepend(managerFrame)
                    : dialogs.Values.Where(item => item.Owner == owner && !ReferenceEquals(item, entry)).Select(item => item.Frame).Prepend(ownerFrame);
                foreach (var target in targets.Distinct())
                {
                    entry.DisabledFrames.Add(target, target.IsEnabled);
                    target.SetCurrentValue(IsEnabledProperty, false);
                }
            }
            ActivateDialog(entry);
            frame.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
            return placement;
        }
        catch
        {
            if (entry is not null && dialogs.ContainsKey((owner, kind, instance))) ReleaseDialog(entry);
            else
            {
                try { frame?.Dispose(); }
                finally { document?.Dispose(); }
            }
            throw;
        }
    }

    internal void CloseDialog(Windows31ApplicationId owner, Windows31DialogKind kind, Window? instance = null)
    {
        if (dialogs.TryGetValue((owner, kind, instance), out var entry)) entry.Placement.Close();
    }

    private void DeactivateDialogs()
    {
        foreach (var dialog in dialogs.Values) dialog.Placement.Deactivate();
    }

    private void ActivateOwnedDialogs(Entry owner)
    {
        var owned = dialogs.Values.Where(dialog => dialog.Owner == owner.Definition.Id)
            .OrderBy(dialog => dialog.Mode == Windows31DialogMode.Modal)
            .ThenBy(dialog => Panel.GetZIndex(dialog.Frame)).ToArray();
        foreach (var dialog in owned) Panel.SetZIndex(dialog.Frame, ++zIndex);
        if (owned.LastOrDefault() is { } active)
        {
            owner.Placement.Deactivate();
            active.Placement.Activate();
        }
    }

    private void ActivateDialog(DialogEntry entry)
    {
        if (entry.Owner == Windows31ApplicationId.ProgramManager)
        {
            BringManagerForward();
            managerPlacement.Deactivate();
        }
        else if (applications.TryGetValue(entry.Owner, out var application)) Activate(application);
        else return;
        if (!entry.Frame.IsEnabled) return;
        DeactivateDialogs();
        entry.Placement.Activate();
        Panel.SetZIndex(entry.Frame, ++zIndex);
    }

    private bool ActivateNativeDialog(FrameworkElement layer)
    {
        var dialog = dialogs.Values.FirstOrDefault(item => ReferenceEquals(item.Frame, layer));
        if (dialog is null) return false;
        ActivateDialog(dialog);
        return true;
    }

    private void ReleaseOwnedDialogs(Windows31ApplicationId owner)
    {
        Exception? failure = null;
        foreach (var dialog in dialogs.Values.Where(item => item.Owner == owner).ToArray())
            try { ReleaseDialog(dialog); }
            catch (Exception error) { failure ??= error; }
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }

    private void ReleaseDialog(DialogEntry entry)
    {
        if (!dialogs.Remove((entry.Owner, entry.Kind, entry.Instance))) return;
        entry.Placement.PropertyChanged -= entry.StateHandler;
        entry.Frame.PreviewMouseDown -= entry.ActivateHandler;
        if (entry.ClosingHandler is not null) entry.Frame.Closing -= entry.ClosingHandler;
        try
        {
            try { nativeWorkspace?.ReleaseLayer(entry.Frame); }
            finally { entry.Frame.Dispose(); }
        }
        finally
        {
            try { entry.Document.Dispose(); }
            finally
            {
                workspace.Children.Remove(entry.Frame);
                foreach (var pair in entry.DisabledFrames) pair.Key.SetCurrentValue(IsEnabledProperty, pair.Value);
                entry.DisabledFrames.Clear();
                if (!disposed && entry.Placement.IsActive)
                {
                    if (entry.ParentKind is { } parentKind && dialogs.TryGetValue((entry.Owner, parentKind, entry.ParentInstance), out var parentDialog)) ActivateDialog(parentDialog);
                    else if (entry.Owner == Windows31ApplicationId.ProgramManager) BringManagerForward();
                    else if (applications.TryGetValue(entry.Owner, out var application)) Activate(application);
                }
            }
        }
    }
}
