using GWGUI.App.Functions.Explorer;
using GWGUI.App.ViewModels.Explorer;
using GWGUI.MediaEngine.Enums;

namespace GWGUI.App.Views.Controls.Explorer;

public partial class ExplorerSection
{
    internal IReadOnlyList<string> CurrentIssues => _document is not null ? BuildIssues(_document)
        : _mediaDocument is not null && _mediaVolume is not null
            ? ExplorerIssueBuilder.Build(_mediaDocument, _mediaVolume) : [];

    internal void RequestToggleFolder(ExplorerFolderItem item)
    {
        if (item.Children.Count == 0) return;
        item.IsExpanded = !item.IsExpanded;
        RefreshVisibleFolders(item);
        ShowContents(item.Entry?.Children ?? _rootEntries);
    }

    internal void RequestOpenEntry(ExplorerContentItem selected)
    {
        if (selected.Entry.Kind != FileSystemEntryKind.Directory || _rootFolder is null) return;
        var folder = ExplorerTreeNavigator.Find(_rootFolder, selected.Entry);
        if (folder is null) return;
        ExplorerTreeNavigator.ExpandPathTo(_rootFolder, folder);
        RefreshVisibleFolders(folder);
        ShowContents(folder.Entry!.Children);
    }
}
