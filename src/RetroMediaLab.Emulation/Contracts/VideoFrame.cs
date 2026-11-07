namespace RetroMediaLab.Emulation.Contracts;

public sealed record VideoFrame(ReadOnlyMemory<byte> Pixels, int Width, int Height, int Pitch, EmulationPixelFormat PixelFormat, float AspectRatio, long Sequence, TimeSpan Timestamp)
{
    /// <summary>En entrelacé, Pixels/Height contiennent uniquement le champ indiqué, à sa cadence native.</summary>
    public EmulationVideoScanMode ScanMode { get; init; }

    /// <summary>Hauteur native de l'image ; Height reste la hauteur des lignes effectivement transférées.</summary>
    public int ImageHeight => ScanMode == EmulationVideoScanMode.Progressive ? Height : checked(Height * 2);
}
