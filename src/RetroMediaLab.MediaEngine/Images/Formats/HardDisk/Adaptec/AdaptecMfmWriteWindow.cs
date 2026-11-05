namespace RetroMediaLab.MediaEngine.Images.Formats.HardDisk.Adaptec;

/// <summary>Fenêtre DATA MFM possédée par son appelant ; aucune image n'est modifiée par le codec.</summary>
internal readonly record struct AdaptecMfmWriteWindow(uint StartNanoseconds, uint DurationNanoseconds,
    IReadOnlyList<uint> Transitions);
