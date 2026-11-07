using RetroMediaLab.MediaEngine.Contracts;

namespace RetroMediaLab.MediaEngine.Constants;

internal static partial class RecordedMechanicalSoundFiles
{
    /// <summary>Profils des lecteurs de bande définis par matériel.</summary>
    internal static partial class Tape
    {
        /// <summary>Regroupe les magnétophones enregistrés.</summary>
        internal static IReadOnlyList<RecordedMechanicalSoundProfile> Catalogs => [UnidentifiedTapePlayer];
    }
}
