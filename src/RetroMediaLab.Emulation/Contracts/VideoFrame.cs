namespace RetroMediaLab.Emulation.Contracts;

public sealed record VideoFrame(ReadOnlyMemory<byte> Pixels, int Width, int Height, int Pitch, EmulationPixelFormat PixelFormat, float AspectRatio, long Sequence, TimeSpan Timestamp)
{
    /// <summary>En entrelacé, Pixels/Height contiennent uniquement le champ indiqué, à sa cadence native.</summary>
    public EmulationVideoScanMode ScanMode { get; init; }

    public EmulationVideoStandard? SignalStandard { get; init; }
    public double SignalFieldRate { get; init; }
    public int SignalFieldHeight { get; init; }

    /// <summary>
    /// Segments natifs immuables de cette trame. Null signale une source raster, une liste vide
    /// une trame vectorielle sans faisceau actif. Pixels reste l'image utilisable par les autres affichages.
    /// </summary>
    public ReadOnlyMemory<VideoVectorSegment>? VectorSegments { get; init; }

    /// <summary>Hauteur native de l'image ; Height reste la hauteur des lignes effectivement transférées.</summary>
    public int ImageHeight => ScanMode == EmulationVideoScanMode.Progressive ? Height : checked(Height * 2);
}
