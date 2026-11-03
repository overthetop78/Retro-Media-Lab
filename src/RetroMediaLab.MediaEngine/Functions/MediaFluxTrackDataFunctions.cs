using System.IO;
using System.Buffers.Binary;
using MediaPhysicalEncodingIds = RetroMediaLab.MediaEngine.Constants.MediaPhysicalEncodingIds;
using MediaFluxRevolutionData = RetroMediaLab.MediaEngine.Contracts.MediaFluxRevolutionData;
using MediaFluxTrackData = RetroMediaLab.MediaEngine.Contracts.MediaFluxTrackData;

namespace RetroMediaLab.MediaEngine.Functions;

/// <summary>Serializes and deserializes the versioned neutral flux-track payload.</summary>
public static class MediaFluxTrackDataFunctions
{
    /// <summary>Remplace uniquement les transitions traversées par une fenêtre d'écriture physique.</summary>
    /// <param name="revolution">Révolution empruntée, jamais modifiée.</param>
    /// <param name="startNanoseconds">Position de début, ramenée modulo la révolution.</param>
    /// <param name="durationNanoseconds">Durée effective d'ouverture de la porte d'écriture.</param>
    /// <param name="transitionOffsetsNanoseconds">Instants strictement croissants des impulsions réellement émises,
    /// relatifs au début de la fenêtre.</param>
    /// <param name="includeEndingInstant">Inclut l’instant final déjà traité avant fermeture de la porte.
    /// Faux pour découper une écriture continue : cet instant appartient alors au morceau suivant.</param>
    /// <remarks>Le passage de l'index ne clôt pas la fenêtre. Une impulsion à l'index est représentée
    /// à la fin de la révolution. Aucune transition n'est inventée pour combler sa fin silencieuse.</remarks>
    public static MediaFluxRevolutionData ReplaceWindow(MediaFluxRevolutionData revolution,
        uint startNanoseconds, uint durationNanoseconds, IReadOnlyList<uint> transitionOffsetsNanoseconds,
        bool includeEndingInstant = false)
    {
        ArgumentNullException.ThrowIfNull(revolution);
        ArgumentNullException.ThrowIfNull(transitionOffsetsNanoseconds);
        uint previous = 0;
        for (var index = 0; index < transitionOffsetsNanoseconds.Count; index++)
        {
            var offset = transitionOffsetsNanoseconds[index];
            if (offset > durationNanoseconds || !includeEndingInstant && offset == durationNanoseconds
                || index > 0 && offset <= previous)
                throw new ArgumentOutOfRangeException(nameof(transitionOffsetsNanoseconds));
            previous = offset;
        }
        var period = revolution.IndexTimeNanoseconds;
        var start = startNanoseconds % period;
        var transitions = new SortedSet<uint>();
        ulong position = 0;
        foreach (var interval in revolution.FluxIntervalsNanoseconds)
        {
            position += interval;
            if (position > period) throw new InvalidDataException(nameof(revolution));
            var relative = (position % period + period - start) % period;
            if (relative > durationNanoseconds || !includeEndingInstant && relative == durationNanoseconds)
                transitions.Add((uint)position);
        }
        foreach (var offset in transitionOffsetsNanoseconds)
        {
            // Un passage ultérieur de la porte efface aussi une ancienne impulsion sans impulsion de remplacement.
            var nextVisit = (ulong)offset + period;
            if (nextVisit < durationNanoseconds || includeEndingInstant && nextVisit == durationNanoseconds) continue;
            var phase = (uint)(((ulong)start + offset) % period);
            transitions.Add(phase == 0 ? period : phase);
        }
        var intervals = new uint[transitions.Count];
        previous = 0;
        var output = 0;
        foreach (var transition in transitions)
        {
            intervals[output++] = transition - previous;
            previous = transition;
        }
        return new(period, intervals);
    }

    public static byte[] Serialize(MediaFluxTrackData track)
    {
        ArgumentNullException.ThrowIfNull(track);
        if (track.Revolutions.Count > MediaPhysicalEncodingIds.MaximumFluxRevolutions)
            throw new InvalidDataException("The flux track contains too many revolutions.");
        if (track.Revolutions.Any(revolution => revolution.FluxIntervalsNanoseconds.Count > MediaPhysicalEncodingIds.MaximumFluxIntervalsPerRevolution))
            throw new InvalidDataException("A flux revolution contains too many intervals.");

        var length = checked((long)MediaPhysicalEncodingIds.FluxTrackV1HeaderSize
            + track.Revolutions.Sum(revolution => (long)MediaPhysicalEncodingIds.FluxTrackV1RevolutionHeaderSize + (long)revolution.FluxIntervalsNanoseconds.Count * MediaPhysicalEncodingIds.UInt32Size));
        if (length > int.MaxValue) throw new InvalidDataException("The serialized flux track is too large.");
        var result = new byte[(int)length];
        BinaryPrimitives.WriteUInt32LittleEndian(result, MediaPhysicalEncodingIds.FluxTrackV1Magic);
        BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(4), checked((uint)track.Revolutions.Count));
        var offset = MediaPhysicalEncodingIds.FluxTrackV1HeaderSize;
        foreach (var revolution in track.Revolutions)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(offset), revolution.IndexTimeNanoseconds);
            BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(offset + MediaPhysicalEncodingIds.UInt32Size), checked((uint)revolution.FluxIntervalsNanoseconds.Count));
            offset += MediaPhysicalEncodingIds.FluxTrackV1RevolutionHeaderSize;
            foreach (var interval in revolution.FluxIntervalsNanoseconds)
            {
                BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(offset), interval);
                offset += MediaPhysicalEncodingIds.UInt32Size;
            }
        }
        return result;
    }

    public static MediaFluxTrackData Deserialize(ReadOnlySpan<byte> data)
    {
        if (data.Length < MediaPhysicalEncodingIds.FluxTrackV1HeaderSize)
            throw new InvalidDataException("The neutral flux-track header is incomplete.");
        if (BinaryPrimitives.ReadUInt32LittleEndian(data) != MediaPhysicalEncodingIds.FluxTrackV1Magic)
            throw new InvalidDataException("The neutral flux-track signature is invalid.");
        var revolutionCount = BinaryPrimitives.ReadUInt32LittleEndian(data[4..]);
        if (revolutionCount is 0 or > MediaPhysicalEncodingIds.MaximumFluxRevolutions)
            throw new InvalidDataException("The neutral flux-track revolution count is invalid.");

        var revolutions = new List<MediaFluxRevolutionData>(checked((int)revolutionCount));
        var offset = MediaPhysicalEncodingIds.FluxTrackV1HeaderSize;
        for (var revolutionIndex = 0; revolutionIndex < revolutionCount; revolutionIndex++)
        {
            if (offset > data.Length - MediaPhysicalEncodingIds.FluxTrackV1RevolutionHeaderSize)
                throw new InvalidDataException("A neutral flux-revolution header is incomplete.");
            var indexTime = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
            var intervalCount = BinaryPrimitives.ReadUInt32LittleEndian(data[(offset + MediaPhysicalEncodingIds.UInt32Size)..]);
            if (intervalCount > MediaPhysicalEncodingIds.MaximumFluxIntervalsPerRevolution)
                throw new InvalidDataException("A neutral flux revolution contains too many intervals.");
            offset += MediaPhysicalEncodingIds.FluxTrackV1RevolutionHeaderSize;
            var byteCount = checked((long)intervalCount * MediaPhysicalEncodingIds.UInt32Size);
            if (byteCount > data.Length - offset)
                throw new InvalidDataException("A neutral flux revolution exceeds its track payload.");
            var intervals = new uint[checked((int)intervalCount)];
            for (var intervalIndex = 0; intervalIndex < intervals.Length; intervalIndex++)
            {
                intervals[intervalIndex] = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
                offset += MediaPhysicalEncodingIds.UInt32Size;
            }
            revolutions.Add(new MediaFluxRevolutionData(indexTime, intervals));
        }
        if (offset != data.Length)
            throw new InvalidDataException("The neutral flux-track payload contains trailing data.");
        return new MediaFluxTrackData(revolutions);
    }
}
