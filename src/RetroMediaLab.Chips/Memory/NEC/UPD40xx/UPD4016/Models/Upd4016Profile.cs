using RetroMediaLab.Chips.Memory.Toshiba.TMM20xx.TMM2016.Models;

namespace RetroMediaLab.Chips.Memory.NEC.UPD40xx.UPD4016.Models;

/// <summary>NEC1982 p64–66: the existing SRAM mechanism receives NEC-only timing/behavior data.</summary>
public static class Upd4016Profile
{
    public static Tmm2016Timing Grade1 { get; } = new("NEC-uPD4016-1",250,110,80,80,80,250,200,130,10,80,0,
        AddressSetup:0,AddressToWriteEnd:150,AddressHold:10,CsEnable:10,OeEnable:10,WeEnable:10,SelectionDuringWriteCanDrive:true,ActiveOutputWriteNeedsDisable:true);
    public static Tmm2016Timing Grade2 { get; } = new("NEC-uPD4016-2",200,90,60,60,60,200,160,100,10,60,0,
        AddressSetup:0,AddressToWriteEnd:120,AddressHold:10,CsEnable:10,OeEnable:10,WeEnable:10,SelectionDuringWriteCanDrive:true,ActiveOutputWriteNeedsDisable:true);
    public static Tmm2016Timing Grade3 { get; } = new("NEC-uPD4016-3",150,70,50,50,50,150,120,80,10,50,0,
        AddressSetup:0,AddressToWriteEnd:90,AddressHold:10,CsEnable:10,OeEnable:10,WeEnable:10,SelectionDuringWriteCanDrive:true,ActiveOutputWriteNeedsDisable:true);
}
