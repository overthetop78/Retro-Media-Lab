namespace RetroMediaLab.Emulation.Example.Common.Machines.Common.Functions;

/// <summary>Règles de démarrage automatique d’une tape explicitement choisies par l’utilisateur.</summary>
internal static class TapeBootFunctions
{
    /// <summary>Détermine si le contenu inséré demande une impulsion Retour après le démarrage du cœur.</summary>
    /// <param name="configuration">Configuration portant les montages et le choix de démarrage automatique.</param>
    /// <returns>True seulement pour une tape insérée et une demande de démarrage automatique.</returns>
    internal static bool RequiresDelayedReturn(MachineConfiguration configuration) => configuration.Media.Any(media => media.Category == MediaCategory.Tape && media.IsInserted
        && (media.TapeAutoBoot || string.Equals(configuration.Options.GetValueOrDefault(SettingsConstants.TapeAutoBoot), bool.TrueString, StringComparison.OrdinalIgnoreCase)));
}

/// <summary>Projection de la présence de bande et du moteur vers l’état public du transport.</summary>
internal static class TapeStateFunctions
{
    /// <summary>Distingue un lecteur vide, une tape arrêtée et une lecture effectivement active.</summary>
    /// <param name="mounted">Indique la présence d’une tape insérée.</param>
    /// <param name="motorActive">Activité du lecteur annoncée par le cœur.</param>
    /// <returns>État du transport public correspondant à ces deux informations.</returns>
    internal static EmulationTapeState From(bool mounted, bool motorActive) => !mounted ? EmulationTapeState.Empty : motorActive ? EmulationTapeState.Playing : EmulationTapeState.Stopped;
}
