using RetroMediaLab.MediaEngine.Images.Formats;
using RetroMediaLab.App.Constants.Controls.Visual;
using RetroMediaLab.App.Constants.DiskImages;
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
    private static IReadOnlyList<string> ReportedFormats(ExploredDiskImage document) => ExplorerMediaChoicesPresenter.ReportedFormats(document);

    private string DetectedFormatsSummary() => ExplorerMediaChoicesPresenter.DetectedFormatsSummary(_detectedFormatIds, _formats);

    private void UpdateDetectedFormatSelector(string? selectedId)
    {
        var choices = ExplorerMediaChoicesPresenter.DetectedFormats(_detectedFormatIds, _formats);
        _updatingDetectedFormatSelector = true;
        DetectedFormatSelector.ItemsSource = choices;
        DetectedFormatSelector.SelectedItem = choices.FirstOrDefault(choice => string.Equals(choice.Id, selectedId, StringComparison.OrdinalIgnoreCase)) ?? choices.FirstOrDefault();
        var selectorVisible = choices.Length > 1;
        DetectedFormatSelector.Visibility = selectorVisible ? Visibility.Visible : Visibility.Collapsed;
        DetectedFormatLabel.Visibility = selectorVisible ? Visibility.Visible : Visibility.Collapsed;
        _updatingDetectedFormatSelector = false;
    }

    private void DetectedFormatSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updatingDetectedFormatSelector || DetectedFormatSelector.SelectedItem is not ExplorerFormatChoice { Id: { } formatId }) return;
        Classification.SelectFormat(formatId);
        FormatChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ManualOptionsButton_Click(object sender, RoutedEventArgs e) => ManualOptionsPopup.IsOpen = !ManualOptionsPopup.IsOpen;

    private CustomPopupPlacement[] ManualOptionsPopup_Place(Size popupSize, Size targetSize, Point offset) => [new(new Point(targetSize.Width - popupSize.Width, targetSize.Height), PopupPrimaryAxis.Horizontal)];

    private string CurrentSystem(ExploredDiskImage document) =>
        ExplorerMediaChoicesPresenter.CurrentSystem(document, _formats, Classification.SelectedMachine);

    private string CurrentSystem(ExploredMediaImage document) =>
        ExplorerMediaChoicesPresenter.CurrentSystem(document, _formats, Classification.SelectedMachine);

    private string TapeDescription(ExploredMediaImage document) =>
        ExplorerMediaChoicesPresenter.TapeDescription(document, _formats, Classification.SelectedMachine);

    private void ResetSummaryLabels()
    {
        VolumeLabel.Text = LocExtension.Get("Explorer.Volume");
        FileSystemLabel.Text = LocExtension.Get("Explorer.FileSystem");
        CapacityLabel.Text = LocExtension.Get("Explorer.Capacity");
        FreeLabel.Text = LocExtension.Get("Explorer.Free");
        EntryCountLabel.Text = LocExtension.Get("Explorer.Entries");
    }

    private Brush BrushFor(bool synthetic)
    {
        var resourceKey = synthetic ? ControlVisualConstants.SyntheticNameBrushResource : ControlVisualConstants.TextBrushResource;
        return TryFindResource(resourceKey) as Brush ?? SystemColors.WindowTextBrush;
    }

}
