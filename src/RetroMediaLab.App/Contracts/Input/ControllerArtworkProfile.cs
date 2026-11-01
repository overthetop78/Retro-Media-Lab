using System.Windows.Media;

namespace RetroMediaLab.App.Contracts.Input;

internal sealed record ControllerArtworkProfile(string VisualId, ImageSource Artwork, IReadOnlyList<ControllerVisualZone> Zones);
