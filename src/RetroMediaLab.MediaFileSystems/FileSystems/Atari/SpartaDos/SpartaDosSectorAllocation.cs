namespace RetroMediaLab.MediaFileSystems.FileSystems.Atari.SpartaDos;

internal sealed record SpartaDosSectorAllocation(IReadOnlyList<int> DataSectors, int MapSectorCount);
