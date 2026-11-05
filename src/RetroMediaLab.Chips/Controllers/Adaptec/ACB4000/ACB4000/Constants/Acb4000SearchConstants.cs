namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants;

/// <summary>Compteurs et sense des recherches du firmware 400108-00, 15E1–15E6 et 1697–16AA.</summary>
public static class Acb4000SearchConstants
{
    public const int TrackAttempts = 4;
    public const byte IdDataCheck = 0x10;
    public const byte SectorNotFound = 0x14;
}
