using RetroMediaLab.MediaEngine.Images.Visualization;

using RetroMediaLab.MediaEngine.Images.Models.Sectors;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Images.Reading.Decoding.Definitions;

namespace RetroMediaLab.MediaEngine.Images.Writing.Encoding;

/// <summary>Transforme une image sectorielle en pistes encodées en appliquant les politiques communes du moteur.</summary>
public sealed class SectorImageTrackEncoder
{
    private readonly FluxEncoderRegistry encoders;
    private readonly SectorImageVisualizationPolicyRegistry policies;

    /// <summary>Crée un encodeur de disque avec les registres par défaut.</summary>
    public SectorImageTrackEncoder() : this(new FluxEncoderRegistry(), new SectorImageVisualizationPolicyRegistry()) { }

    /// <summary>Crée un encodeur de disque avec des registres injectés.</summary>
    internal SectorImageTrackEncoder(FluxEncoderRegistry encoders, SectorImageVisualizationPolicyRegistry policies)
    {
        this.encoders = encoders;
        this.policies = policies;
    }

    /// <summary>Indique si une politique d'encodage accepte l'image.</summary>
    public bool CanEncode(SectorImage image) => image.AddressingKind == SectorImageAddressingKind.Physical && policies.Resolve(image) is not null;

    /// <summary>Encode une piste ciblée avec le codage et les durées fournis par le lecteur.</summary>
    /// <remarks>Le conteneur reste propriétaire des données ; aucune activité mécanique n'est déclenchée ici.</remarks>
    public EncodedTrack EncodeTrack(SectorImage image, int cylinder, int head, string encoderId,
        IReadOnlyDictionary<string, int>? attributes, uint bitCellTicks, uint indexTimeTicks)
    {
        ArgumentNullException.ThrowIfNull(image);
        if (image.AddressingKind == SectorImageAddressingKind.Logical)
            throw SectorImageVisualizationExceptions.LogicalAddressing(image.FormatId);
        var policy = policies.Resolve(image) ?? throw SectorImageVisualizationExceptions.MissingPolicy(image.FormatId);
        var items = image.AvailableBlocks
            .Select(block => (Block: block, Address: policy.VisualAddress(image, block.Address)))
            .Where(item => item.Address.Cylinder == cylinder && item.Address.Head == head)
            .OrderBy(item => item.Block.RecordedTrackOrder ?? item.Address.Number).ToArray();
        var sectors = policy.CreateTrackSectors(image, items);
        return encoders.Encode(encoderId, new(cylinder, head, sectors, attributes, bitCellTicks, indexTimeTicks));
    }

    /// <summary>Produit les transitions physiques dans le codage du média ; le réglage du contrôleur ne le transforme pas.</summary>
    /// <remarks>Contrairement à Encode, toutes les durées de cette opération sont en nanosecondes.</remarks>
    public EncodedTrack EncodePhysicalTrack(SectorImage image, int cylinder, int head, uint indexTimeNanoseconds)
    {
        ArgumentNullException.ThrowIfNull(image);
        var policy = policies.Resolve(image) ?? throw SectorImageVisualizationExceptions.MissingPolicy(image.FormatId);
        var encoding = policy.EncoderId(image);
        var count = image.AvailableBlocks.Count(block =>
        {
            var address = policy.VisualAddress(image, block.Address);
            return address.Cylinder == cylinder && address.Head == head;
        });
        var attributes = PhysicalAttributes(encoding, policy.TrackAttributes(image, count));
        return EncodeTrack(image, cylinder, head, encoding, attributes,
            checked(policy.BitCellTicks(image, cylinder) * EncodedTrackTiming.TickNanoseconds), indexTimeNanoseconds);
    }

    /// <summary>Encode toutes les pistes disponibles dans l'ordre cylindre puis face.</summary>
    public IReadOnlyList<EncodedDiskTrack> Encode(SectorImage image, CancellationToken cancellationToken = default)
        => Encode(image, physical: false, cancellationToken);

    /// <summary>Reconstruit les pistes physiques avec leurs gaps enregistrés pour conversion ou écriture.</summary>
    /// <remarks>Les durées conservent les unités communes de25 ns, contrairement à EncodePhysicalTrack.</remarks>
    public IReadOnlyList<EncodedDiskTrack> EncodePhysical(SectorImage image, CancellationToken cancellationToken = default)
        => Encode(image, physical: true, cancellationToken);

    private IReadOnlyList<EncodedDiskTrack> Encode(SectorImage image, bool physical, CancellationToken cancellationToken)
    {
        if (image.AddressingKind == SectorImageAddressingKind.Logical)
            throw SectorImageVisualizationExceptions.LogicalAddressing(image.FormatId);
        var policy = policies.Resolve(image) ?? throw SectorImageVisualizationExceptions.MissingPolicy(image.FormatId);
        var tracks = new List<EncodedDiskTrack>();
        foreach (var group in image.AvailableBlocks.Select(block => (Block: block, Address: policy.VisualAddress(image, block.Address))).GroupBy(item => (item.Address.Cylinder, item.Address.Head)).OrderBy(group => group.Key.Cylinder).ThenBy(group => group.Key.Head))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var items = group.OrderBy(item => item.Block.RecordedTrackOrder ?? item.Address.Number).ToArray();
            var sectors = policy.CreateTrackSectors(image, items);
            if (sectors.Count == 0) continue;
            var bitCellTicks = policy.BitCellTicks(image, group.Key.Cylinder);
            var indexTimeTicks = policy.IndexTimeTicks(image, group.Key.Cylinder);
            var encoding = policy.EncoderId(image);
            var attributes = policy.TrackAttributes(image, sectors.Count);
            if (physical) attributes = PhysicalAttributes(encoding, attributes, requireComplete: true);
            EncodedTrack encoded;
            try { encoded = encoders.Encode(encoding, new TrackEncodeRequest(group.Key.Cylinder, group.Key.Head, sectors, attributes, bitCellTicks, indexTimeTicks)); }
            catch (KeyNotFoundException) { throw SectorImageVisualizationExceptions.MissingPolicy(image.FormatId); }
            tracks.Add(new(group.Key.Cylinder, group.Key.Head, bitCellTicks, encoded));
        }
        if (tracks.Count == 0) throw SectorImageVisualizationExceptions.NoTrack(image.FormatId);
        return tracks.AsReadOnly();
    }

    private static IReadOnlyDictionary<string, int>? PhysicalAttributes(string encoding, IReadOnlyDictionary<string, int>? supplied, bool requireComplete = false)
    {
        if (encoding is not (FluxCodecIds.IsoFm or FluxCodecIds.IsoMfm)) return supplied;
        var attributes = supplied is null ? new Dictionary<string, int>() : new Dictionary<string, int>(supplied);
        if (encoding == FluxCodecIds.IsoFm) attributes[TrackEncodingAttributeKeys.FmPhysicalLayout] = IsoFmFormat.PhysicalLayoutEnabled;
        else attributes[TrackEncodingAttributeKeys.MfmPhysicalLayout] = IsoMfmFormat.PhysicalLayoutEnabled;
        if (requireComplete) attributes[TrackEncodingAttributeKeys.RequireCompletePhysicalTrack] = 1;
        return attributes;
    }
}
