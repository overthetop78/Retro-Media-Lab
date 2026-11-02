namespace RetroMediaLab.Chips.Controllers.Generic.ATA.PIO.Constants;

public static class AtaTimingConstants
{
    /// <summary>Vitesse de transfert initiale, en pourcentage de la vitesse normale.</summary>
    public const int DefaultSpeedPercent = 100;
    /// <summary>Latence initiale des accès au disque dur, en millisecondes.</summary>
    public const int DefaultHardDiskLatencyMilliseconds = 2;
    /// <summary>Latence maximale configurable, en millisecondes.</summary>
    public const int MaximumLatencyMilliseconds = 10000;

}
