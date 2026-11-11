using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Functions;

namespace RetroMediaLab.MediaEngine.PhysicalMedia.Reading;

/// <summary>Reconstructs a tape signal from physical acquisition without interpreting a machine protocol.</summary>
public sealed class TapeMediaAcquisitionService
{
    public MediaTapeSignal CreateSignal(MediaAcquisitionResult acquisition)
    {
        ArgumentNullException.ThrowIfNull(acquisition);
        if (acquisition.MediaKind != MediaKind.Tape || acquisition.RepresentationKind != MediaRepresentationKind.Sequential)
            throw new NotSupportedException("A sequential tape acquisition is required.");
        return MediaTapePhysicalDataFunctions.ReadSignal(acquisition.DataUnits);
    }
}
