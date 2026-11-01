using RetroMediaLab.MediaEngine.Contracts;
using M = RetroMediaLab.MediaEngine.Constants.MechanicalSoundConstants;
using R = RetroMediaLab.MediaEngine.Constants.RecordedMechanicalSoundConstants;

namespace RetroMediaLab.MediaEngine.Constants;

internal static partial class RecordedMechanicalSoundFiles
{
    internal static partial class Floppy
    {
        /// <summary>Dossier externe des sons de Panasonic JU-256.</summary>
        private const string PanasonicJu256Directory = "Floppy/Panasonic/JU-256/";

        /// <summary>Enregistrements Panasonic JU-256.</summary>
        internal static RecordedMechanicalSoundProfile PanasonicJu256 { get; } = new(
            Id: PanasonicJu256Directory[..^1],
            Device: M.Floppy,
            DisplayName: "Panasonic JU-256",
            Samples:
            [
                new(PanasonicJu256Directory + "Disk-eject.flac", [new(M.Eject)]),
                new(PanasonicJu256Directory + "Disk-insert.flac", [new(M.Insert)]),
            ]);
    }
}
