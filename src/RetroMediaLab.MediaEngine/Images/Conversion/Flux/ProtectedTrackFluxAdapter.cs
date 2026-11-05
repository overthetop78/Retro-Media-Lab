using System.IO;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Images.Models.Flux;

namespace RetroMediaLab.MediaEngine.Images.Conversion.Flux;

/// <summary>Convertit les cellules temporelles ou révolutions capturées en impulsions neutres.</summary>
internal static class ProtectedTrackFluxAdapter
{
    internal static MediaFluxTrackData Read(ProtectedTrack track)
    {
        if (track.Bits is null)
        {
            if (track.Revolutions.Count == 0) throw new NotSupportedException();
            return new(track.Revolutions.Select(revolution => new MediaFluxRevolutionData(
                checked(revolution.Flux.IndexTimeTicks * (uint)revolution.ResolutionNanoseconds),
                revolution.Flux.FluxIntervals.Select(interval => checked(interval * (uint)revolution.ResolutionNanoseconds)).ToArray())).ToArray());
        }
        var intervals = new List<uint>();
        double time = 0;
        uint previous = 0;
        var offset = 0;
        foreach (var segment in track.Timing)
        {
            if (segment.BitOffset != offset) throw new InvalidDataException();
            var end = checked(segment.BitOffset + segment.BitLength);
            for (; offset < end; offset++)
            {
                time += segment.BitCellNanoseconds;
                if (!track.Bits[offset]) continue;
                var position = checked((uint)Math.Round(time));
                intervals.Add(checked(position - previous));
                previous = position;
            }
        }
        if (offset != track.Bits.Count) throw new InvalidDataException();
        return new([new(checked((uint)Math.Round(time)), intervals)]);
    }
}
