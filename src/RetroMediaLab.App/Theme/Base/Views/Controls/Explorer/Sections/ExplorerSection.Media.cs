using RetroMediaLab.MediaEngine.Images.Formats;
using RetroMediaLab.App.Constants.Controls.Visual;
using RetroMediaLab.App.Enums.Explorer;
using RetroMediaLab.App.Functions.Explorer;
using RetroMediaLab.App.Functions.Storage;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.Presenters.Explorer;
using RetroMediaLab.App.ViewModels.Explorer;
using RetroMediaLab.App.Views.Dialogs.Explorer;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using RetroMediaLab.MediaEngine.Contracts.Explorer;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Images.Models.Optical;
using RetroMediaLab.MediaEngine.Images.Models.Sequential;
using RetroMediaLab.MediaEngine.Images.Reading.Recognition;

namespace RetroMediaLab.App.Views.Controls.Explorer;

public partial class ExplorerSection
{
    private DiskFormat? FormatForIdentity(string? formatId) => ExplorerMediaChoicesPresenter.FormatForIdentity(_formats, formatId);

    private void ConfigureSessions(int? preferredSession)
    {
        _updatingSessionSelector = true;
        var choices = ExplorerMediaChoicesPresenter.Sessions(_mediaDocument);
        if (choices is null)
        {
            SessionSelector.ItemsSource = null;
            SessionSelector.Visibility = Visibility.Collapsed;
            SessionControl.Visibility = Visibility.Collapsed;
            _updatingSessionSelector = false;
            return;
        }
        SessionSelector.ItemsSource = choices;
        SessionControl.Visibility = choices.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
        SessionSelector.Visibility = choices.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
        SessionSelector.SelectedItem = choices.FirstOrDefault(choice => choice.SessionNumber == preferredSession) ?? choices.FirstOrDefault();
        _updatingSessionSelector = false;
    }

    private void ConfigureVolumesAndAudio(ExploredMediaVolume? preferredVolume)
    {
        if (_mediaDocument is null) return;
        var session = (SessionSelector.SelectedItem as ExplorerOpticalSessionChoice)?.SessionNumber;
        var (choices, selectedVolume) = ExplorerMediaChoicesPresenter.Volumes(_mediaDocument, session, preferredVolume);
        _updatingVolumeSelector = true;
        VolumeSelector.ItemsSource = choices;
        VolumeSelector.Visibility = choices.Length > 1 ? Visibility.Visible : Visibility.Collapsed;
        VolumeSelector.SelectedItem = choices.FirstOrDefault(choice => ReferenceEquals(choice.Volume, selectedVolume));
        _updatingVolumeSelector = false;

        var audioTracks = ExplorerMediaChoicesPresenter.AudioTracks(_mediaDocument, session);
        AudioTrackList.ItemsSource = audioTracks;
        AudioTrackList.SelectedItem = null;
        OpticalTracksSection.Visibility = audioTracks.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
        DisplayMediaVolume(selectedVolume);
    }

    private void DisplayMediaVolume(ExploredMediaVolume? exploredVolume)
    {
        if (_mediaDocument is null) return;
        _mediaVolume = exploredVolume;

        var volume = _mediaVolume?.FileSystem;
        var isTape = _mediaDocument.Document.MediaKind == RetroMediaLab.MediaEngine.Enums.MediaKind.Tape;
        if (isTape)
        {
            VolumeLabel.Text = LocExtension.Get("Explorer.Volume");
            FileSystemLabel.Text = LocExtension.Get("Explorer.Format");
            CapacityLabel.Text = LocExtension.Get("Explorer.Size");
            FreeLabel.Text = LocExtension.Get("Visual.DurationLabel");
            EntryCountLabel.Text = LocExtension.Get("Explorer.Entries");
        }
        else
        {
            ResetSummaryLabels();
        }
        var descriptorName = _mediaVolume?.Descriptor.Name;
        var syntheticName = string.IsNullOrWhiteSpace(volume?.Name) && string.IsNullOrWhiteSpace(descriptorName);
        var volumeName = !string.IsNullOrWhiteSpace(volume?.Name) ? volume.Name : !string.IsNullOrWhiteSpace(descriptorName) ? descriptorName : $"({LocExtension.Get("Explorer.Unnamed")})";
        VolumeNameText.Foreground = BrushFor(syntheticName);
        VolumeNameText.Text = volumeName;
        SystemText.Text = CurrentSystem(_mediaDocument);
        ProtectionText.Text = LocExtension.Get("Explorer.Metadata.None");
        FileSystemText.Text = isTape ? TapeDescription(_mediaDocument) : volume?.FileSystemDisplayName ?? ControlVisualConstants.EmptyValue;
        var capacity = volume?.Capacity ?? _mediaVolume?.Descriptor.Length;
        CapacityText.Text = capacity.HasValue ? StorageSizeFormatter.FormatBytes(capacity.Value) : ControlVisualConstants.EmptyValue;
        FreeText.Text = isTape ? (_mediaDocument.Document.Representation as SequentialMediaImageRepresentation)?.Duration?.ToString("g") ?? ControlVisualConstants.EmptyValue : volume?.FreeSpaceKnown == true ? StorageSizeFormatter.FormatBytes(volume.FreeBytes) : ControlVisualConstants.EmptyValue;
        _rootEntries = volume?.Entries ?? [];
        EntryCountText.Text = CountEntries(_rootEntries).ToString();
        _rootFolder = new ExplorerFolderItem(volumeName, null, 0, _rootEntries, syntheticName) { IsExpanded = true };
        RefreshVisibleFolders(_rootFolder);
        FolderList.SelectedItem = _rootFolder;
        ShowContents(_rootEntries);

        if (_mediaVolume is not null)
            DetailsPanel.ShowMedia(_mediaDocument, _mediaVolume, CurrentSystem(_mediaDocument));
        else
            DetailsPanel.Clear();
        var issues = _mediaVolume is null ? _mediaDocument.Diagnostics : ExplorerIssueBuilder.Build(_mediaDocument, _mediaVolume);
        WarningsButton.Visibility = issues.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        WarningsText.Text = $"{LocExtension.Get("Explorer.Warnings")} : {issues.Count}";
    }

    private void SessionSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updatingSessionSelector) return;
        ConfigureVolumesAndAudio(null);
    }

    private void VolumeSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updatingVolumeSelector || VolumeSelector.SelectedItem is not ExplorerMediaVolumeChoice choice) return;
        AudioTrackList.SelectedItem = null;
        DisplayMediaVolume(choice.Volume);
    }

    private void AudioTrackList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (AudioTrackList.SelectedItem is ExplorerOpticalTrackChoice choice)
            DetailsPanel.ShowOpticalTrack(choice.Track);
        else if (_mediaDocument is not null && _mediaVolume is not null)
            DetailsPanel.ShowMedia(_mediaDocument, _mediaVolume, CurrentSystem(_mediaDocument));
    }

}
