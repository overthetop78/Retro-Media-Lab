using System.IO;

namespace RetroMediaLab.App.ViewModels.Operations;

/// <summary>Capture state shared by presentations; contains no visual element or theme color.</summary>
public sealed class ReadCaptureViewModel : ObservableObject
{
    private bool isCompleted;
    private bool isArchived;
    private string summary = string.Empty;

    internal ReadCaptureViewModel(string path)
    {
        Path = path;
        FileName = System.IO.Path.GetFileName(path);
    }

    public string Path { get; }
    public string FileName { get; }
    public string Summary => summary;
    public bool IsCompleted => isCompleted;
    public bool IsArchived => isArchived;

    internal void Complete(string value)
    {
        Set(ref summary, value, nameof(Summary));
        Set(ref isCompleted, true, nameof(IsCompleted));
    }

    internal void Archive() => Set(ref isArchived, true, nameof(IsArchived));
}
