using RetroMediaLab.App.Contracts.Services.Dialogs;
namespace RetroMediaLab.App.Interfaces.Services.Dialogs;

public interface IFileDialogService
{
    string? OpenFile(OpenFileRequest request);
    string? SaveFile(SaveFileRequest request);
    string? SelectFolder(SelectFolderRequest request);
}
