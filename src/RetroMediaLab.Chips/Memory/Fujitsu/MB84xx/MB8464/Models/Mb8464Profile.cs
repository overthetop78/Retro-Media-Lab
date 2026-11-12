using RetroMediaLab.Chips.Memory.Toshiba.TMM20xx.TMM2016.Models;

namespace RetroMediaLab.Chips.Memory.Fujitsu.MB84xx.MB8464.Models;

/// <summary>Fujitsu1986 p3-135/3-140: original MB8464, not the later MB8464A.</summary>
public static class Mb8464Profile
{
    public static Tmm2016Timing Grade12 { get; } = Create("MB8464-12",false);
    public static Tmm2016Timing Grade12L { get; } = Create("MB8464-12L",false);
    public static Tmm2016Timing Grade12LL { get; } = Create("MB8464-12LL",false);
    public static Tmm2016Timing Grade15 { get; } = Create("MB8464-15",true);
    public static Tmm2016Timing Grade15L { get; } = Create("MB8464-15L",true);
    public static Tmm2016Timing Grade15LL { get; } = Create("MB8464-15LL",true);

    private static Tmm2016Timing Create(string id,bool slow) => new(id,
        slow ? 150 : 120,slow ? 60 : 50,slow ? 50 : 40,slow ? 50 : 40,slow ? 50 : 40,
        slow ? 150 : 120,slow ? 100 : 85,slow ? 90 : 70,0,slow ? 50 : 40,0,
        AddressSetup:0,AddressToWriteEnd:slow ? 100 : 85,Capacity:8192,
        HasSecondChipSelect:true,MinimumRetentionMillivolts:2000);
}
