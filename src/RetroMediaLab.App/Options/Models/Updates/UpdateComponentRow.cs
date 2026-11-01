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

namespace RetroMediaLab.App.Options.Models.Updates;

internal sealed class UpdateComponentRow
{
    internal UpdateComponentRow(string id, UpdateComponentKind kind, string displayName, string installedLabel, string statusLabel, IReadOnlyList<string> versions, string? selectedVersion, bool canSelectVersion, UpdateVisualState state)
    {
        Id = id;
        Kind = kind;
        DisplayName = displayName;
        InstalledLabel = installedLabel;
        StatusLabel = statusLabel;
        Versions = versions;
        SelectedVersion = selectedVersion;
        CanSelectVersion = canSelectVersion;
        State = state;
        StateIcon = state switch
        {
            UpdateVisualState.Current => IconGlyphs.Current,
            UpdateVisualState.Available => IconGlyphs.Sync,
            UpdateVisualState.Busy => IconGlyphs.Sync,
            UpdateVisualState.Error => IconGlyphs.Error,
            _ => IconGlyphs.Controller
        };
    }

    public string Id { get; }
    public UpdateComponentKind Kind { get; }
    public string DisplayName { get; }
    public string InstalledLabel { get; }
    public string StatusLabel { get; }
    public IReadOnlyList<string> Versions { get; }
    public string? SelectedVersion { get; set; }
    public bool CanSelectVersion { get; }
    public UpdateVisualState State { get; }
    public string StateIcon { get; }
}
