using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Images.Models.Sectors;

namespace RetroMediaLab.MediaEngine.Functions;

/// <summary>Remplace des champs sectoriels fournis sans décider de leur compatibilité avec une machine.</summary>
internal static class SectorTrackFunctions
{
    internal static SectorImage Replace(SectorImage image, int cylinder, int head,
        IReadOnlyList<MediaDiskSector> fields, bool retainGeometry)
    {
        Validate(image, cylinder, head, fields);
        var original = image.AvailableBlocks.ToArray();
        var previous = original.Where(block => block.Address.Cylinder == cylinder && block.Address.Head == head).ToArray();
        SectorBlock Create(MediaDiskSector field, int logical, int order) => new(logical,
            new(cylinder, head, field.Number), field.Data.ToArray(), field.IntegrityValid)
        {
            RecordedAddress = new(field.Cylinder, field.Head, field.Number),
            RecordedSizeCode = field.RecordedSizeCode,
            RecordedTrackOrder = order,
            HeaderIntegrityValid = field.HeaderIntegrityValid,
            DataIntegrityValid = field.DataIntegrityValid,
            DeletedDataMark = field.DeletedDataMark,
            DiagnosticCode = field.DiagnosticCode,
            RecordedErrorCheckCode = field.RecordedErrorCheckCode.ToArray(),
            RecordedDataPrefix = field.RecordedDataPrefix.ToArray()
        };

        if (retainGeometry)
        {
            if (fields.Count != previous.Length || fields.Select(field => field.Number).Distinct().Count() != fields.Count
                || previous.Select(block => block.Address.Number).Distinct().Count() != previous.Length)
                throw new NotSupportedException();
            var replacements = new Dictionary<int, SectorBlock>();
            for (var order = 0; order < fields.Count; order++)
            {
                var field = fields[order];
                var block = previous.FirstOrDefault(block => block.Address.Number == field.Number);
                if (block is null || field.Cylinder != cylinder || field.Head != head || field.Data.Length != block.Data.Count
                    || field.DeletedDataMark == true || field.HeaderIntegrityValid == false || field.DataIntegrityValid == false
                    || field.RecordedSizeCode is { } size && (size > SectorSizeCode.MaximumCode || SectorSizeCode.ToByteCount(size) != field.Data.Length))
                    throw new NotSupportedException();
                replacements.Add(block.LogicalBlock, Create(field, block.LogicalBlock, order));
            }
            return new(image.FormatId, image.BlockSize, image.Cylinders, image.Heads, image.SectorsPerTrack,
                original.Select(block => replacements.GetValueOrDefault(block.LogicalBlock, block)),
                image.AllowsVariableBlockSize, image.Capacity, image.BlockCount, image.AddressingKind);
        }

        // Missing logical slots need their container's descriptors (for example IMD unavailable fields).
        // The runtime rebuilds those through that container rather than silently losing the slots here.
        if (image.MissingBlocks.Count != 0) throw new NotSupportedException();
        var blocks = original.Where(block => block.Address.Cylinder != cylinder || block.Address.Head != head)
            .Concat(fields.Select((field, order) => Create(field, order, order)))
            .OrderBy(block => block.Address.Cylinder).ThenBy(block => block.Address.Head).ThenBy(block => block.Address.Number)
            .Select((block, logical) => block with { LogicalBlock = logical }).ToArray();
        var sectorsPerTrack = blocks.GroupBy(block => (block.Address.Cylinder, block.Address.Head)).Max(track => track.Count());
        return new(image.FormatId, image.BlockSize, image.Cylinders, image.Heads, sectorsPerTrack, blocks,
            blocks.Any(block => block.Data.Count != image.BlockSize), blocks.Sum(block => (long)block.Data.Count), blocks.Length, image.AddressingKind);
    }

    internal static void Validate(SectorImage image, int cylinder, int head, IReadOnlyList<MediaDiskSector> fields)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(fields);
        ArgumentOutOfRangeException.ThrowIfNegative(cylinder);
        ArgumentOutOfRangeException.ThrowIfNegative(head);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(cylinder, image.Cylinders);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(head, image.Heads);
        if (fields.Count == 0 || fields.Any(field => field.Data.IsEmpty || field.Cylinder < 0 || field.Head < 0 || field.Number < 0))
            throw new ArgumentException(null, nameof(fields));
    }
}
