using RetroMediaLab.App.Views.Controls.Explorer;
using RetroMediaLab.App.Contracts.Dialogs;
using RetroMediaLab.App.Views.Dialogs.Common;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace RetroMediaLab.Tests.Interface.ExplorerViews;
internal static class ExplorerFailureScenarios
{
    public static async Task LoadingAndRetry(bool staleFailure)
    {
        using var workspace = new RetroMediaLab.Tests.Interface.VisualizerViews.VisualizerDocumentScenarios.Workspace();
        var pending = new TaskCompletionSource<RetroMediaLab.MediaEngine.Contracts.Explorer.ExploredDiskImage>();
        var oldToken = default(CancellationToken);
        var latest = ValidScpDocument("latest");
        workspace.Explore = (path, _, token) => {
            if (path == "old.scp") { oldToken = token; return pending.Task; }
            Assert.Equal("latest.scp", path); return Task.FromResult(latest);
        };
        var old = workspace.Controller.LoadExplorerAsync("old.scp");
        Assert.Equal(Visibility.Visible, ExplorerDocumentScenarios.Find<Border>(workspace.Explorer, "LoadingOverlay").Visibility);
        Assert.Empty(ExplorerDocumentScenarios.Find<ListView>(workspace.Explorer, "ContentsList").Items);
        Assert.Same(latest, await workspace.Controller.LoadExplorerAsync("latest.scp"));
        Assert.True(oldToken.IsCancellationRequested);
        if (staleFailure) pending.SetException(new IOException("obsolete failure"));
        else pending.SetResult(ValidScpDocument("old"));
        Assert.Null(await old);
        Assert.Same(latest, workspace.Controller.LastReadImage);
        Assert.Equal("latest", ExplorerDocumentScenarios.Find<TextBlock>(workspace.Explorer, "VolumeNameText").Text);
        Assert.Empty(workspace.Errors);
        workspace.Explore = (_, _, _) => Task.FromException<RetroMediaLab.MediaEngine.Contracts.Explorer.ExploredDiskImage>(new IOException("current failure"));
        Assert.Null(await workspace.Controller.LoadExplorerAsync("failure.scp"));
        Assert.IsType<IOException>(Assert.Single(workspace.Errors));
        Assert.Empty(ExplorerDocumentScenarios.Find<ListView>(workspace.Explorer, "ContentsList").Items);
        Assert.Equal(Visibility.Collapsed, ExplorerDocumentScenarios.Find<Border>(workspace.Explorer, "LoadingOverlay").Visibility);
        workspace.Explore = (_, _, _) => Task.FromResult(latest);
        Assert.Same(latest, await workspace.Controller.LoadExplorerAsync("retry.scp"));
        Assert.Equal("latest", ExplorerDocumentScenarios.Find<TextBlock>(workspace.Explorer, "VolumeNameText").Text);
    }

    private static RetroMediaLab.MediaEngine.Contracts.Explorer.ExploredDiskImage ValidScpDocument(string name)
    {
        var source = ExplorerDocumentScenarios.Document(name);
        var scp = new RetroMediaLab.MediaEngine.Images.Formats.Floppy.Scp.ScpImage(
            source.ScpImage!.Header,
            [new RetroMediaLab.MediaEngine.Images.Formats.Floppy.Scp.ScpTrack(0, 0, 0, [new RetroMediaLab.MediaEngine.Images.Formats.Floppy.Scp.ScpRevolution(8_000_000, 3, [80, 120, 160])])],
            source.ScpImage.ChecksumValid,
            source.ScpImage.FileSize);
        return new RetroMediaLab.MediaEngine.Contracts.Explorer.ExploredDiskImage( source.SourcePath, source.Image, source.Volume, source.Metadata, source.FileSystemRecognized, source.DetectedFileSystems, source.DetectedSectorImages, source.PrimaryFormatId, scp);
    }

    public static void Clear()
    {
        var section = new ExplorerSection();
        section.Display(ExplorerDocumentScenarios.Document("OLD"));
        section.SetLoading(true);
        Assert.Equal(Visibility.Visible, ExplorerDocumentScenarios.Find<Border>(section, "LoadingOverlay").Visibility);
        section.SetLoadingProgress("Reading sectors", "Track 12 / 80", 64);
        Assert.Equal("Reading sectors", section.LoadingStage);
        Assert.Equal("Track 12 / 80", section.LoadingDetail);
        Assert.Equal(64, section.LoadingValue);
        Assert.Equal("64 %", section.LoadingPercent);
        section.Clear("virtual/new.scp");
        Assert.Empty(ExplorerDocumentScenarios.Find<ListBox>(section, "FolderList").Items);
        Assert.Empty(ExplorerDocumentScenarios.Find<ListView>(section, "ContentsList").Items);
        Assert.Equal("virtual/new.scp", ExplorerDocumentScenarios.Find<TextBox>(section, "PathText").Text);
        Assert.Equal(Visibility.Collapsed, ExplorerDocumentScenarios.Find<Border>(section, "ExplorerEmptyState").Visibility);
        Assert.Equal(2, Grid.GetRow(ExplorerDocumentScenarios.Find<Border>(section, "LoadingOverlay")));
        Assert.Equal(1, Grid.GetRowSpan(ExplorerDocumentScenarios.Find<Border>(section, "LoadingOverlay")));
        Assert.Equal(Visibility.Visible, ExplorerDocumentScenarios.Find<ListView>(section, "ContentsList").Visibility);
        section.SetLoading(false);
        Assert.Equal(Visibility.Collapsed, ExplorerDocumentScenarios.Find<Border>(section, "LoadingOverlay").Visibility);
        section.Display(ExplorerDocumentScenarios.Document("NEW"));
        Assert.Equal("NEW", ExplorerDocumentScenarios.Find<TextBlock>(section, "VolumeNameText").Text);
    }

    public static void ManualFormatFailureUsesInformationalDialog()
    {
        Assert.Equal("Explorer.SelectedFormatUnsupported", RetroMediaLab.App.Services.DiskImages.DiskImageWorkspaceController.LoadFailureMessageKey(false, "atari.90"));
        Assert.Equal("Explorer.LoadFailed", RetroMediaLab.App.Services.DiskImages.DiskImageWorkspaceController.LoadFailureMessageKey(true, "atari.90"));
        Assert.Equal("Explorer.LoadFailed", RetroMediaLab.App.Services.DiskImages.DiskImageWorkspaceController.LoadFailureMessageKey(false, null));

        var localization = RetroMediaLab.App.Localization.Sources.LocalizationSource.Instance;
        var previousCultures = (localization.Culture, localization.UiCulture);
        try
        {
            localization.SetCultures(RetroMediaLab.App.Functions.Localization.UiLanguageResolver.GetCulture("en-US"), RetroMediaLab.App.Functions.Localization.UiLanguageResolver.GetUiCulture("en-US"));
            Assert.Equal(
                "Unable to interpret “disk.atr” using the “Atari 8-bit — 90 KiB” format.\nSelect another format to continue.",
                RetroMediaLab.App.Localization.Extensions.LocExtension.Get("Explorer.SelectedFormatUnsupported", "Atari 8-bit — 90 KiB", "disk.atr"));
        }
        finally
        {
            localization.SetCultures(previousCultures.Culture, previousCultures.UiCulture);
        }

        var dialog = new CommonErrorDialog(new CommonErrorDialogContent("Explorateur", "Format incompatible", CommonErrorDialog.InformationIcon, Brushes.SteelBlue));
        Assert.Equal(WindowStyle.None, dialog.WindowStyle);
        Assert.True(dialog.AllowsTransparency);
        Assert.False(dialog.ShowInTaskbar);
        Assert.Equal(new CornerRadius(12), Assert.IsType<Border>(dialog.Content).CornerRadius);
    }
}
