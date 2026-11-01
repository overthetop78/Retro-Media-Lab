using System.Collections.Generic;
using System.Linq;
using RetroMediaLab.MediaEngine.Contracts;

namespace RetroMediaLab.MediaEngine.Constants;

/// <summary>Catalogues typés des enregistrements mécaniques, regroupés par type et matériel.</summary>
internal static partial class RecordedMechanicalSoundFiles
{
    /// <summary>Catalogues des catégories de lecteurs enregistrées.</summary>
    internal static IReadOnlyList<IReadOnlyList<RecordedMechanicalSoundProfile>> Catalogs { get; } =
    [
        Tape.Catalogs,
        Floppy.Catalogs,
        HardDisk.Catalogs,
        Optical.Catalogs,
    ];

    /// <summary>Définitions audio utilisées directement par le lecteur, sans catalogue externe.</summary>
    internal static IReadOnlyList<RecordedMechanicalSoundProfile> All { get; } = System.Array.AsReadOnly(Catalogs.SelectMany(profiles => profiles).ToArray());

}
