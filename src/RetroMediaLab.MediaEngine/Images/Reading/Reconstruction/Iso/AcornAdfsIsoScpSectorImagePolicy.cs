using System.IO;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Adf;
using RetroMediaLab.MediaEngine.Images.Models.Sectors;
using RetroMediaLab.MediaEngine.Images.Reading.Decoding.Definitions;

namespace RetroMediaLab.MediaEngine.Images.Reading.Reconstruction.Iso;

/// <summary>Reconstruit les profils ADFS sans confondre la face physique et H enregistré à zéro en S/M/L.</summary>
internal sealed class AcornAdfsIsoScpSectorImagePolicy : IIsoScpSectorImagePolicy
{
    public IReadOnlyList<string> DecoderIds { get; } = [FluxCodecIds.IsoMfm];

    public SectorImage Build(string? formatId, IsoSectorCandidateSet candidateSet)
    {
        var geometry = AcornAdfGeometry.All.Single(item => item.FormatId.Equals(formatId, StringComparison.OrdinalIgnoreCase));
        var candidates = geometry.BlockSize == 256 ? candidateSet.Physical : candidateSet.Addressed;
        if (candidates.Count == 0 || candidates.Any(pair => pair.Key.Cylinder < 0 || pair.Key.Cylinder >= geometry.Cylinders
            || pair.Key.Head < 0 || pair.Key.Head >= geometry.Heads
            || pair.Key.Number < geometry.FirstSectorNumber || pair.Key.Number >= geometry.FirstSectorNumber + geometry.SectorsPerTrack
            || pair.Value.Any(candidate => candidate.Sector.Data!.Count != geometry.BlockSize)))
            throw new InvalidDataException($"Decoded sectors do not match the requested ADFS geometry '{formatId}'.");
        return IsoSectorImageBuilder.CreateUniform(geometry.FormatId, candidates, geometry.BlockSize,
            geometry.Cylinders, geometry.Heads, geometry.SectorsPerTrack, address => address.Number - geometry.FirstSectorNumber);
    }
}
