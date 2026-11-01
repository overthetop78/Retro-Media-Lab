namespace RetroMediaLab.MediaFileSystems.FileSystems.Coherent;

/// <summary>Contient les données positionnées d'un inode et leur validité.</summary>
internal sealed record CoherentFileData(byte[] Content, bool IsValid);
