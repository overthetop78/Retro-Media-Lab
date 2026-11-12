namespace RetroMediaLab.Chips.Memory.Toshiba.TMM23xx.TMM23256.Constants;

public static class Tmm23256Constants
{
    public const int Capacity = 32768, StateVersion = 4;
    public const long AddressHold = 30, CePulse = 150, CeOff = 70, Cycle = 230, Startup = 100_000;
    public const long MaximumAccess = 150, MaximumOutputEnable = 70, MaximumOutputDisable = 70;
}
