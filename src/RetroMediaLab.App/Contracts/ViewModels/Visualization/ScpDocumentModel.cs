
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Scp;

namespace RetroMediaLab.App.Contracts.ViewModels.Visualization;

public sealed record ScpDocumentModel(ScpImage Image, string FileName, string Summary, IReadOnlySet<int> Heads);
