using RetroMediaLab.Chips.Memory.Toshiba.TMM23xx.TMM23256.Enums;
using RetroMediaLab.Chips.Memory.Toshiba.TMM23xx.TMM23256.Models;

namespace RetroMediaLab.Chips.Memory.NEC.UPD23xx.UPD23256A.Models;

/// <summary>NEC23256ADS-5-84-CAT-L p6-9/6-10. All three mask-programmed OE options.</summary>
public static class Upd23256AProfile
{
    public static Tmm23256Timing ActiveLow { get; } = Create(Tmm23256OutputEnableMode.ActiveLow);
    public static Tmm23256Timing ActiveHigh { get; } = Create(Tmm23256OutputEnableMode.ActiveHigh);
    public static Tmm23256Timing AlwaysEnabled { get; } = Create(Tmm23256OutputEnableMode.AlwaysEnabled);

    private static Tmm23256Timing Create(Tmm23256OutputEnableMode mode)
        => new("NEC-uPD23256A",200,100,70,300,0,50,50,0,100,mode);
}
