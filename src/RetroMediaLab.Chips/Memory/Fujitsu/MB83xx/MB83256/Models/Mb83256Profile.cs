using RetroMediaLab.Chips.Memory.Toshiba.TMM23xx.TMM23256.Enums;
using RetroMediaLab.Chips.Memory.Toshiba.TMM23xx.TMM23256.Models;

namespace RetroMediaLab.Chips.Memory.Fujitsu.MB83xx.MB83256.Models;

/// <summary>Fujitsu1986 p8-4: static address tracking, documented250ns grade.</summary>
public static class Mb83256Profile
{
    public static Tmm23256Timing Grade25 { get; } = new("MB83256-25",250,100,80,
        0,0,0,0,0,0,Tmm23256OutputEnableMode.ActiveLow,latchesAddress:false,outputHold:0);
}
