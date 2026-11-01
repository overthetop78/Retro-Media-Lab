namespace RetroMediaLab.MediaFileSystems.FileSystems.Atari.SpartaDos;

internal sealed record SpartaDosFileData(IReadOnlyList<byte> Content, long OccupiedSize, bool IsSparse);
