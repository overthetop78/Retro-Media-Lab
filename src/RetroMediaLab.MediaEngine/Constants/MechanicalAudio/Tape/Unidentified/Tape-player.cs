using RetroMediaLab.MediaEngine.Contracts;
using M = RetroMediaLab.MediaEngine.Constants.MechanicalSoundConstants;

namespace RetroMediaLab.MediaEngine.Constants;

internal static partial class RecordedMechanicalSoundFiles
{
    internal static partial class Tape
    {
        /// <summary>Dossier externe du magnétophone sans référence commerciale identifiée.</summary>
        private const string TapePlayerDirectory = "Tape/Unidentified/Tape-player/";
        /// <summary>Commande enregistrée de mise en lecture.</summary>
        private const string PlayStart = "TapePlayStart";
        /// <summary>Commande enregistrée de mise en avance rapide.</summary>
        private const string ForwardStart = "TapeForwardStart";
        /// <summary>Commande enregistrée de mise en rembobinage.</summary>
        private const string RewindStart = "TapeRewindStart";

        /// <summary>Actions et rotations du même magnétophone ; lecture et enregistrement partagent le mécanisme de défilement.</summary>
        internal static RecordedMechanicalSoundProfile UnidentifiedTapePlayer { get; } = new(
            M.Standard, M.Tape, "Tape player",
            [
                new(TapePlayerDirectory + "Play-start.flac", [new(PlayStart)]),
                new(TapePlayerDirectory + "Play-running.flac", [new(M.Play, Loop: true, StartOperation: PlayStart), new(M.Record, Loop: true, StartOperation: PlayStart), new(M.Motor, Loop: true, StartOperation: PlayStart)]),
                new(TapePlayerDirectory + "Forward-start.flac", [new(ForwardStart)]),
                new(TapePlayerDirectory + "Forward-running.flac", [new(M.FastForward, Loop: true, StartOperation: ForwardStart)]),
                new(TapePlayerDirectory + "Rewind-start.flac", [new(RewindStart)]),
                new(TapePlayerDirectory + "Rewind-running.flac", [new(M.Rewind, Loop: true, StartOperation: RewindStart)]),
                new(TapePlayerDirectory + "Stop.flac", [new(M.Stop)]),
                new(TapePlayerDirectory + "Pause.flac", [new(M.Pause)]),
                new(TapePlayerDirectory + "Tape-insert.flac", [new(M.Insert)]),
                new(TapePlayerDirectory + "Tape-eject.flac", [new(M.Eject)]),
            ]);
    }
}
