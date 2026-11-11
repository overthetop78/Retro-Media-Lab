using RetroMediaLab.MediaEngine.Images.Models.Sectors;

namespace RetroMediaLab.MediaEngine.Images.Reading.Reconstruction.Sectors;

/// <summary>Conserve les emplacements manquants d'une géométrie sectorielle uniforme sans ajouter de données.</summary>
internal static class UniformSectorImageLayout
{
    internal static SectorImage? TryCreate(string formatId, int blockSize, int cylinders, int heads, int sectorsPerTrack, IReadOnlyCollection<SectorBlock> blocks)
    {
        if (blocks.Count == 0) return null;
        var firstNumber = blocks.Min(block => block.Address.Number);
        if (firstNumber is not (0 or 1)) return null;
        if (blocks.Any(block => block.Address.Cylinder < 0 || block.Address.Cylinder >= cylinders
            || block.Address.Head < 0 || block.Address.Head >= heads || block.Address.Number < firstNumber
            || block.Address.Number >= firstNumber + sectorsPerTrack || block.Data.Count > blockSize)) return null;
        var mapped = blocks.Select(block => block with
        {
            LogicalBlock = (block.Address.Cylinder * heads + block.Address.Head) * sectorsPerTrack + block.Address.Number - firstNumber
        }).ToArray();
        if (mapped.Select(block => block.LogicalBlock).Distinct().Count() != mapped.Length) return null;
        return new(formatId, blockSize, cylinders, heads, sectorsPerTrack, mapped);
    }
}
