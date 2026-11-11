using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Functions;
using RetroMediaLab.MediaEngine.Interfaces;

namespace RetroMediaLab.MediaEngine.PhysicalMedia.Writing;

/// <summary>Prepares the tape signal for a physical writer using the shared container readers.</summary>
/// <remarks>The runtime preparation service is borrowed. Creating a plan never opens an audio device.</remarks>
public sealed class TapeMediaWritePlanningService
{
    private readonly IMediaRuntimePreparation preparation;

    public TapeMediaWritePlanningService(IMediaRuntimePreparation preparation)
    {
        ArgumentNullException.ThrowIfNull(preparation);
        this.preparation = preparation;
    }

    public async Task<MediaWritePlan> CreatePlanAsync(string sourcePath,
        IReadOnlyDictionary<string, string>? parameters = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        cancellationToken.ThrowIfCancellationRequested();
        var signal = await preparation.ReadTapeAsync(sourcePath, cancellationToken, parameters: parameters).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        return CreatePlan(signal);
    }

    public MediaWritePlan CreatePlan(MediaTapeSignal signal) => new(MediaKind.Tape,
        MediaRepresentationKind.Sequential, [MediaTapePhysicalDataFunctions.CreateUnit(signal)], [0],
        new Dictionary<string, string>(StringComparer.Ordinal));
}
