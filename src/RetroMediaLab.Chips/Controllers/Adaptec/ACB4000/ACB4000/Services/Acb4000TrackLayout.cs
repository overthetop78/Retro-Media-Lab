using C = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000FormatConstants;

namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

/// <summary>Carte des slots FORMAT : algorithme du firmware 400108-00, routines 0232 et 0CFE.</summary>
public static class Acb4000TrackLayout
{
    /// <summary>Écrit les numéros logiques, marques de défaut et de fin ; conserve la destination en cas d'erreur.</summary>
    public static bool TryBuild(Acb4000DriveParameters parameters, Acb4000FormatGeometry geometry,
        int cylinder, int head, Acb4000FormatDefects defects, Span<byte> destination, out int logicalSectorCount)
    {
        logicalSectorCount = 0;
        var slots = geometry.SectorsPerTrack;
        if (!parameters.IsValid || cylinder < 0 || cylinder >= parameters.Cylinders || head < 0 || head >= parameters.Heads
            || slots <= 0 || slots > C.SmallBlockInterleavedSectors || destination.Length < slots
            || geometry.BlockCount != (long)parameters.Cylinders * parameters.Heads * slots
            || geometry.CapacityBytes != (long)parameters.Cylinders * parameters.Heads * slots * parameters.BlockSize)
            return false;
        Span<byte> scratch = stackalloc byte[C.SmallBlockInterleavedSectors];
        var map = scratch[..slots];
        map.Fill(C.UnassignedSlot);
        var count = slots;
        var recordLength = parameters.BlockSize + (geometry.Interleave == C.SequentialInterleave
            ? C.SequentialRecordOverhead : C.InterleavedRecordOverhead);
        for (var index = 0; index < defects.Count; index++)
        {
            var defect = defects[index];
            if (defect.Cylinder < cylinder || defect.Cylinder == cylinder && defect.Head < head) continue;
            if (defect.Cylinder > cylinder || defect.Head > head) break;
            var slot = (int)(defect.BytePosition / recordLength);
            // Firmware consumes defects in the unused end of the track without losing a logical block.
            if (slot >= slots || map[slot] == C.DefectiveSlot) continue;
            map[slot] = C.DefectiveSlot;
            count--;
        }
        if (count == 0) return false;
        var position = 0;
        var last = 0;
        for (byte logical = 0; logical < count; logical++)
        {
            while (map[position] != C.UnassignedSlot)
                if (++position == slots) position = 0;
            map[position] = logical;
            last = position;
            position += geometry.Interleave;
            if (position >= slots) position -= slots;
        }
        map[last] |= C.LastLogicalSectorMask;
        map.CopyTo(destination);
        logicalSectorCount = count;
        return true;
    }
}
