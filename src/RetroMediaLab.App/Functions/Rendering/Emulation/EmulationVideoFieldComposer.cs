using RetroMediaLab.Emulation;

namespace RetroMediaLab.App.Functions.Rendering.Emulation;

/// <summary>Historique de présentation : reçoit tous les champs natifs, même si le rendu en saute.</summary>
/// <remarks>Les pixels du module sont empruntés et immuables. La composition n'est calculée qu'à la demande
/// du présentateur et reste indépendante des circuits, de leur cadence et de la résolution hôte.</remarks>
internal sealed class EmulationVideoFieldComposer
{
    private VideoFrame? _latest;
    private VideoFrame? _even;
    private VideoFrame? _odd;
    private VideoFrame? _composed;

    internal void Reset() => _latest = _even = _odd = _composed = null;

    internal void Receive(VideoFrame frame)
    {
        if (ReferenceEquals(frame, _latest)) return;
        if (frame.ScanMode == EmulationVideoScanMode.Progressive || _latest is null
            || _latest.ScanMode == EmulationVideoScanMode.Progressive
            || frame.Width != _latest.Width || frame.Height != _latest.Height || frame.Pitch != _latest.Pitch
            || frame.PixelFormat != _latest.PixelFormat || frame.Sequence <= _latest.Sequence
            || frame.Timestamp < _latest.Timestamp)
            Reset();
        _latest = frame;
        _composed = null;
        if (frame.ScanMode == EmulationVideoScanMode.InterlacedEvenField) _even = frame;
        else if (frame.ScanMode == EmulationVideoScanMode.InterlacedOddField) _odd = frame;
    }

    internal VideoFrame? Compose()
    {
        if (_latest is null || _latest.ScanMode == EmulationVideoScanMode.Progressive) return _latest;
        if (_composed is not null) return _composed;
        var pixels = GC.AllocateUninitializedArray<byte>(checked(_latest.Pitch * _latest.ImageHeight));
        for (var row = 0; row < _latest.Height; row++)
        {
            CopyRow(_even, row, pixels.AsSpan(row * 2 * _latest.Pitch, _latest.Pitch));
            CopyRow(_odd, row, pixels.AsSpan((row * 2 + 1) * _latest.Pitch, _latest.Pitch));
        }
        return _composed = _latest with
        {
            Pixels = pixels, Height = _latest.ImageHeight, ScanMode = EmulationVideoScanMode.Progressive
        };
    }

    private static void CopyRow(VideoFrame? field, int row, Span<byte> destination)
    {
        if (field is null) destination.Clear();
        else field.Pixels.Span.Slice(row * field.Pitch, field.Pitch).CopyTo(destination);
    }
}
