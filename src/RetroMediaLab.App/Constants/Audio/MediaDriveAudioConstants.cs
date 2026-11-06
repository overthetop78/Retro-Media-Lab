namespace RetroMediaLab.App.Constants.Audio;

/// <summary>Cadence et bornes de la sortie commune des bruitages de lecteurs dans les outils.</summary>
internal static class MediaDriveAudioConstants
{
    /// <summary>Fréquence de sortie mono commune, en échantillons par seconde.</summary>
    internal const int SampleRate = 44100;

    /// <summary>Durée des fragments remis à la sortie audio, en millisecondes.</summary>
    internal const int ChunkMilliseconds = 20;

    /// <summary>Nombre de trames mono produites pour un fragment.</summary>
    internal const int FramesPerChunk = SampleRate * ChunkMilliseconds / (int)System.TimeSpan.MillisecondsPerSecond;

    /// <summary>Réserve de PCM couvrant les prélèvements de la sortie et les réveils du producteur.</summary>
    internal const int TargetBufferedFrames = FramesPerChunk * 4;

    /// <summary>Attente maximale avant de réapprovisionner une sortie dont le tampon est rempli.</summary>
    internal const int RefillMilliseconds = ChunkMilliseconds / 2;

    /// <summary>Attente maximale de la fin du tampon après la dernière activité, en millisecondes.</summary>
    internal const int MaximumDrainMilliseconds = 1000;
}
