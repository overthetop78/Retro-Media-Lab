using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using RetroMediaLab.App.Services.Updates;
using RetroMediaLab.App.Services.Emulation;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.Views.Controls.Options;
using RetroMediaLab.Updates.Contracts;
using RetroMediaLab.App.Contracts.Updates;
using RetroMediaLab.App.Constants.Controls.Visual;
using RetroMediaLab.App.Options.Models.Updates;
using RetroMediaLab.App.Constants.Options.Updates;

namespace RetroMediaLab.App.Options.Controllers;

internal sealed partial class UpdateOptionsController : IDisposable
{
    private readonly OptionsUpdatesSection _section;
    private readonly HashSet<Task> _pendingOperations = [];
    private readonly CancellationTokenSource _lifetime = new();
    private bool _stopping;
    private bool _disposed;

    internal Task PendingOperations => Task.WhenAll(_pendingOperations);

    internal Task StopAsync()
    {
        _stopping = true;
        _lifetime.Cancel();
        _preparationCancellation?.Cancel();
        return PendingOperations;
    }

    private async Task TrackAsync(Func<Task> action)
    {
        if (_stopping) return;
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingOperations.Add(completion.Task);
        try { await action(); }
        finally { _pendingOperations.Remove(completion.Task); completion.SetResult(); }
    }
    private readonly Window _owner;
    private readonly ApplicationUpdateService _applicationService;
    private readonly ModuleUpdateService _moduleService;
    private readonly ModuleDirectoryService _moduleDirectoryService;
    private readonly UpdatePackagePreparationService _packagePreparation;
    private readonly ModuleInstallationService _moduleInstallation;
    private readonly PendingModuleInstallationStore _pendingInstallations;
    private readonly Func<string, object[], string> _localize;
    private readonly Action<Exception> _reportError;
    private readonly Dictionary<string, string> _applicationSelections = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _moduleSelections = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, UpdateSearchResult> _moduleResultsById = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, UpdateVisualState> _moduleStatesById = new(StringComparer.OrdinalIgnoreCase);
    private UpdateSearchResult? _applicationResult;
    private UpdateSearchResult? _moduleResult;
    private CancellationTokenSource? _preparationCancellation;
    private UpdateVisualState _applicationState = UpdateVisualState.Neutral;
    private UpdateVisualState _directoryState = UpdateVisualState.Neutral;
    private UpdateVisualState _modulesState
    {
        get => _section.SelectedModuleId is { } id ? _moduleStatesById.GetValueOrDefault(id, UpdateVisualState.Current) : UpdateVisualState.Current;
        set { if (_section.SelectedModuleId is { } id) _moduleStatesById[id] = value; }
    }
    private UpdateVisualState _advancedState = UpdateVisualState.Neutral;
    private ModuleOperationPage _moduleOperationPage = ModuleOperationPage.Module;

    internal ObservableCollection<UpdateComponentRow> ApplicationResults { get; } = [];
    internal ObservableCollection<UpdateComponentRow> ModuleResults { get; } = [];
    internal ObservableCollection<AvailableModuleRow> AvailableModuleResults { get; } = [];

    internal UpdateOptionsController(
        Window owner,
        OptionsUpdatesSection section,
        ApplicationUpdateService applicationService,
        Func<string, object[], string> localize,
        Action<Exception> reportError,
        ModuleUpdateService? moduleService = null,
        ModuleDirectoryService? moduleDirectoryService = null,
        UpdatePackagePreparationService? packagePreparation = null,
        ModuleInstallationService? moduleInstallation = null,
        PendingModuleInstallationStore? pendingInstallations = null)
    {
        _owner = owner;
        _section = section;
        _applicationService = applicationService;
        _moduleService = moduleService ?? new ModuleUpdateService();
        _moduleDirectoryService = moduleDirectoryService ?? new ModuleDirectoryService();
        _packagePreparation = packagePreparation ?? new UpdatePackagePreparationService();
        _pendingInstallations = pendingInstallations ?? new PendingModuleInstallationStore();
        _moduleInstallation = moduleInstallation ?? new ModuleInstallationService(packagePreparation: _packagePreparation, isPending: _pendingInstallations.Contains);
        _localize = localize;
        _reportError = reportError;
        _section.Results.ItemsSource = ApplicationResults;
        _section.ModuleResults.ItemsSource = ModuleResults;
        _section.AvailableModuleResults.ItemsSource = AvailableModuleResults;
        _section.SearchRequested += SearchApplication;
        _section.SearchModulesRequested += SearchModules;
        _section.SelectedModuleChanged += ShowModuleResult;
        _section.SearchAvailableModulesRequested += SearchAvailableModules;
        _section.InstallAvailableModuleRequested += InstallAvailableModule;
        _section.VersionSelectionChanged += VersionChanged;
        _section.InstallRequested += InstallApplication;
        _section.InstallModulesRequested += InstallModules;
        _section.CancelRequested += Cancel;
        _section.CancelModulesRequested += Cancel;
        _section.InstallModuleFileRequested += InstallModuleFromFile;
        _section.InstallModuleUrlRequested += InstallModuleFromUrl;
        _pendingInstallations.Changed += PendingInstallationsChanged;
        RefreshLocalizedContent();
        PendingInstallationsChanged(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _stopping = true;
        _lifetime.Cancel();
        _preparationCancellation?.Cancel();
        _pendingInstallations.Changed -= PendingInstallationsChanged;
        _section.SelectedModuleChanged -= ShowModuleResult;
        _section.SearchRequested -= SearchApplication;
        _section.SearchModulesRequested -= SearchModules;
        _section.SearchAvailableModulesRequested -= SearchAvailableModules;
        _section.InstallAvailableModuleRequested -= InstallAvailableModule;
        _section.VersionSelectionChanged -= VersionChanged;
        _section.InstallRequested -= InstallApplication;
        _section.InstallModulesRequested -= InstallModules;
        _section.CancelRequested -= Cancel;
        _section.CancelModulesRequested -= Cancel;
        _section.InstallModuleFileRequested -= InstallModuleFromFile;
        _section.InstallModuleUrlRequested -= InstallModuleFromUrl;
        _lifetime.Dispose();
    }

    internal void RefreshLocalizedContent()
    {
        RebuildNavigation();
        if (_applicationResult is null) _section.Status.Text = _localize("Updates.Ready", []);
        else RenderApplication(_applicationResult);
        if (_moduleResult is null) _section.ModuleStatus.Text = _localize("Updates.Ready", []);
        else RenderModules(_moduleResult);
        if (AvailableModuleResults.Count == 0)
            _section.AvailableModuleStatus.Text = _localize("Updates.ModuleDirectoryReady", []);
    }

    private void RebuildNavigation()
    {
        var items = new List<UpdateNavigationItem>
        {
            new(UpdateOptionsConstants.ApplicationNavigationId, UpdateNavigationPage.Application, _localize("Updates.Application", []), _localize("Updates.NavigationApplicationSubtitle", []), IconGlyphs.Information, _applicationState),
            new(UpdateOptionsConstants.DirectoryNavigationId, UpdateNavigationPage.Directory, _localize("Updates.ModuleDirectoryTitle", []), _localize("Updates.NavigationDirectorySubtitle", []), IconGlyphs.Directory, _directoryState)
        };
        foreach (var package in EmulationModuleRegistry.Packages.OrderBy(package => LocExtension.GetForModule(package.Module, package.Module.DisplayResourceKey), StringComparer.CurrentCultureIgnoreCase))
        {
            var state = _moduleStatesById.GetValueOrDefault(package.Module.Id, UpdateVisualState.Current);
            items.Add(new($"{UpdateOptionsConstants.ModuleNavigationIdPrefix}{package.Module.Id}",
                UpdateNavigationPage.Module,
                LocExtension.GetForModule(package.Module, package.Module.DisplayResourceKey),
                _localize("Updates.InstalledVersion", [package.Manifest.ModuleVersion]),
                IconGlyphs.Controller, state, package.Module.Id));
        }
        items.Add(new(UpdateOptionsConstants.AdvancedNavigationId, UpdateNavigationPage.Advanced, _localize("Updates.ModuleAdvancedInstallationTitle", []), _localize("Updates.NavigationAdvancedSubtitle", []), IconGlyphs.Settings, _advancedState));
        _section.SetNavigationItems(items);
    }

}
