using RetroMediaLab.MediaEngine.Images.Writing.Encoding;
using RetroMediaLab.MediaEngine.Functions;
using RetroMediaLab.MediaEngine.Images.Models.Sectors;
using RetroMediaLab.MediaEngine.Images.Visualization;

namespace RetroMediaLab.MediaEngine.Images.Visualization.Policies;
/// <summary>Fournit les comportements communs des politiques de visualisation sectorielle.</summary>
internal abstract class SectorImageVisualizationPolicy : ISectorImageVisualizationPolicy
{
    /// <inheritdoc/>
    public abstract bool CanHandle(SectorImage image);
    /// <inheritdoc/>
    public abstract string EncoderId(SectorImage image);
    /// <inheritdoc/>
    public virtual SectorAddress VisualAddress(SectorImage image, SectorAddress address) => address;
    /// <inheritdoc/>
    public virtual IReadOnlyList<TrackSector> CreateTrackSectors(SectorImage image,
        IReadOnlyList<(SectorBlock Block, SectorAddress Address)> items) => items.Select(item => new TrackSector(item.Block.RecordedAddress?.Number ?? item.Address.Number,
        item.Block.Data,
        Deleted: item.Block.DeletedDataMark == true,
        SizeCode: SectorSizeCode.TryFromByteCount(item.Block.Data.Count, out var code) ? code : null,
        Attributes: TagAttributes(item.Block.Tag))
        {
            RecordedCylinder = item.Block.RecordedAddress?.Cylinder,
            RecordedHead = item.Block.RecordedAddress?.Head,
            RecordedSizeCode = item.Block.RecordedSizeCode,
            HeaderIntegrityValid = item.Block.HeaderIntegrityValid,
            DataIntegrityValid = item.Block.DataIntegrityValid ?? item.Block.IntegrityValid,
            RecordedErrorCheckCode = item.Block.RecordedErrorCheckCode.ToArray()
        }).ToArray();
    /// <inheritdoc/>
    public virtual IReadOnlyDictionary<string, int>? TrackAttributes(SectorImage image, int sectorCount) => null;
    /// <inheritdoc/>
    public virtual uint BitCellTicks(SectorImage image, int cylinder) => SectorImageTrackTimingCatalog.BitCellTicks(image.FormatId);
    /// <inheritdoc/>
    public virtual uint IndexTimeTicks(SectorImage image, int cylinder) => SectorImageTrackTimingCatalog.IndexTimeTicks(image.FormatId);
    /// <summary>Convertit les tags binaires d'un secteur en attributs indexés d'encodage.</summary>
    /// <param name = "tag">Tags du secteur, ou <see langword="null"/>.</param>
    /// <returns>Attributs indexés, ou <see langword="null"/> en l'absence de tags.</returns>
    protected static IReadOnlyDictionary<string, int>? TagAttributes(IReadOnlyList<byte>? tag)
    {
        if (tag is null || tag.Count == 0)
            return null;
        return tag.Select((value, index) => (Key: TrackEncodingAttributeKeys.Tag(index), Value: (int)value)).ToDictionary(item => item.Key, item => item.Value);
    }
}
