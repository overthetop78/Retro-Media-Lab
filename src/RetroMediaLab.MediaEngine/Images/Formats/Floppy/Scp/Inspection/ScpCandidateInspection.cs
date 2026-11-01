using RetroMediaLab.MediaEngine.Contracts.Explorer;

using RetroMediaLab.MediaEngine.Images.Models.Sectors;

namespace RetroMediaLab.MediaEngine.Images.Formats.Floppy.Scp.Inspection;

/// <summary>Conserve l'identité, l'image, les correspondances normalisées et le diagnostic d'un candidat SCP.</summary>
internal sealed record ScpCandidateInspection(string CandidateId, SectorImage? Image, IReadOnlyList<ExploredFileSystem> Matches, InvalidDataException? Diagnostic);
