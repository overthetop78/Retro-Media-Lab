using RetroMediaLab.MediaEngine.Images.Formats;
namespace RetroMediaLab.App.Contracts.Storage;

/// <summary>Format présenté par le sélecteur avec son état de détection pour l'image courante.</summary>
public sealed record DiskFormatChoice(DiskFormat Format, bool IsDetected)
{
    public string DisplayName => Format.DisplayName;
}
