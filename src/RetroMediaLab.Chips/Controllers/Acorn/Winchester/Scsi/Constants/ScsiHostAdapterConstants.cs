namespace RetroMediaLab.Chips.Controllers.Acorn.Winchester.Scsi.Constants;

/// <summary>Décodage local de la carte hôte Acorn ; l'adresse du bus BBC appartient à son propriétaire.</summary>
public static class ScsiHostAdapterConstants
{
    public const int RegisterMask = 3;
    public const int DataRegister = 0;
    public const int StatusRegister = 1;
    public const int SelectRegister = 2;
    public const int InterruptEnableRegister = 3;
    public const byte InterruptEnableMask = 1;
    public const byte InterruptStatusMask = 0x10;
    public const byte OpenBus = byte.MaxValue;
    public const byte InitialDataLatch = 0;
}
