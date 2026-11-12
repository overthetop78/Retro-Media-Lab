namespace RetroMediaLab.Chips.Memory.Toshiba.TMM20xx.TMM2016.Models;

/// <summary>Toshiba1983 p73/79,ns. Access/disable values are maxima; write requirements are minima.</summary>
public sealed record Tmm2016Timing(string Id, long Access, long OeAccess, long CsDisable,
    long OeDisable, long WeDisable, long WriteCycle, long CsWrite, long WritePulse,
    long WriteRecovery, long DataSetup, long DataHold, long AddressSetup = 20,
    long AddressToWriteEnd = 0, long AddressHold = 10, long CsEnable = 10,
    long OeEnable = 5, long WeEnable = 5, bool SelectionDuringWriteCanDrive = false,
    bool ActiveOutputWriteNeedsDisable = false, int Capacity = 2048,
    bool HasSecondChipSelect = false, int MinimumRetentionMillivolts = 0,
    int MinimumOperatingMillivolts = 4500, int MaximumOperatingMillivolts = 5500)
{
    public static Tmm2016Timing PD { get; } = new("TMM2016P/D",150,55,55,50,50,150,120,100,10,60,15);
    public static Tmm2016Timing PD1 { get; } = new("TMM2016P/D-1",100,35,40,35,35,100,90,70,10,40,15);
    public static Tmm2016Timing PD2 { get; } = new("TMM2016P/D-2",200,55,55,50,50,200,150,120,10,60,15);
    public static Tmm2016Timing AP90 { get; } = new("TMM2016AP-90",90,35,40,35,25,90,70,60,0,35,0);
    public static Tmm2016Timing AP10 { get; } = new("TMM2016AP-10",100,35,40,35,30,100,80,70,0,40,0);
    public static Tmm2016Timing AP12 { get; } = new("TMM2016AP-12",120,50,40,35,35,120,100,85,5,50,0);
    public static Tmm2016Timing AP15 { get; } = new("TMM2016AP-15",150,55,55,50,50,150,120,100,10,60,0);
    internal long Validate()
    {
        ArgumentException.ThrowIfNullOrEmpty(Id);
        long[] values = [Access,OeAccess,CsDisable,OeDisable,WeDisable,WriteCycle,CsWrite,WritePulse,WriteRecovery,
            DataSetup,DataHold,AddressSetup,AddressToWriteEnd,AddressHold,CsEnable,OeEnable,WeEnable];
        foreach (long value in values) ArgumentOutOfRangeException.ThrowIfNegative(value);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(Access);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(WriteCycle);
        if (Capacity <= 0 || Capacity > Constants.Tmm2016Constants.MaximumCapacity || (Capacity & (Capacity - 1)) != 0)
            throw new ArgumentOutOfRangeException(nameof(Capacity));
        if (MinimumRetentionMillivolts < 0 || MinimumOperatingMillivolts < MinimumRetentionMillivolts
            || MaximumOperatingMillivolts < MinimumOperatingMillivolts)
            throw new ArgumentOutOfRangeException(nameof(MinimumRetentionMillivolts));
        return Math.Max(values.Max(), 20);
    }
}
