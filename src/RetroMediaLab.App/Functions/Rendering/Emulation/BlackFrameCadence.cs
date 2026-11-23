namespace RetroMediaLab.App.Functions.Rendering.Emulation;

internal sealed class BlackFrameCadence
{
    private long? _lastRefresh;
    private long _imageRefresh;
    private long _generation;

    internal static bool IsAvailable(double hostHz, double emulatedHz) =>
        double.IsFinite(hostHz) && double.IsFinite(emulatedHz) && emulatedHz > 0 && hostHz > emulatedHz;

    internal bool ShouldBlank(long refresh, long generation, bool newImage)
    {
        if (_lastRefresh is not { } last || refresh < last || generation != _generation || newImage)
        {
            _generation = generation;
            _lastRefresh = _imageRefresh = refresh;
            return false;
        }

        // Blank only the first spare physical refresh after a game image.
        _lastRefresh = refresh;
        return refresh == _imageRefresh + 1 && refresh == last + 1;
    }
}
