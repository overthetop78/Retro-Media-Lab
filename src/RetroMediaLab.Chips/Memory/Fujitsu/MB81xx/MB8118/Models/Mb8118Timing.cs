namespace RetroMediaLab.Chips.Memory.Fujitsu.MB81xx.MB8118.Models;

/// <summary>Fujitsu MB8118 data sheet p3: guaranteed access maxima and cycle minima, ns.</summary>
public sealed record Mb8118Timing
{
    public static Mb8118Timing Speed10 { get; } = new(100, 55, 45, 235, 285, 125,
        110, 115, 70, 50, 60, 55, 100, 25, 45, 60, 30, 75, 60, 45, 55, 20);
    public static Mb8118Timing Speed12 { get; } = new(120, 65, 50, 270, 320, 145,
        120, 140, 85, 55, 70, 65, 120, 35, 55, 70, 35, 90, 65, 50, 65, 25);
    public long RasAccess { get; }
    public long CasAccess { get; }
    public long OutputOff { get; }
    public long Cycle { get; }
    public long ReadWriteCycle { get; }
    public long PageCycle { get; }
    public long RasPrecharge { get; }
    public long RasPulse { get; }
    public long RasHold { get; }
    public long CasPrecharge { get; }
    public long PagePrecharge { get; }
    public long CasPulse { get; }
    public long CasHold { get; }
    public long RasCasMinimum { get; }
    public long RasCasMaximum { get; }
    public long ColumnFromRas { get; }
    public long WriteHold { get; }
    public long WriteHoldFromRas { get; }
    public long WriteToRas { get; }
    public long WriteToCas { get; }
    public long CasToWrite { get; }
    public long ReadHoldFromRas { get; }
    internal long[] Signature => [RasAccess, CasAccess, OutputOff, Cycle, ReadWriteCycle, PageCycle,
        RasPrecharge, RasPulse, RasHold, CasPrecharge, PagePrecharge, CasPulse, CasHold,
        RasCasMinimum, RasCasMaximum, ColumnFromRas, WriteHold, WriteHoldFromRas, WriteToRas,
        WriteToCas, CasToWrite, ReadHoldFromRas];

    private Mb8118Timing(long rasAccess, long casAccess, long outputOff, long cycle, long readWriteCycle,
        long pageCycle, long rasPrecharge, long rasPulse, long rasHold, long casPrecharge,
        long pagePrecharge, long casPulse, long casHold, long rasCasMinimum, long rasCasMaximum,
        long columnFromRas, long writeHold, long writeHoldFromRas, long writeToRas, long writeToCas,
        long casToWrite, long readHoldFromRas)
    {
        RasAccess = rasAccess; CasAccess = casAccess; OutputOff = outputOff; Cycle = cycle;
        ReadWriteCycle = readWriteCycle; PageCycle = pageCycle; RasPrecharge = rasPrecharge;
        RasPulse = rasPulse; RasHold = rasHold; CasPrecharge = casPrecharge; PagePrecharge = pagePrecharge;
        CasPulse = casPulse; CasHold = casHold; RasCasMinimum = rasCasMinimum; RasCasMaximum = rasCasMaximum;
        ColumnFromRas = columnFromRas; WriteHold = writeHold; WriteHoldFromRas = writeHoldFromRas;
        WriteToRas = writeToRas; WriteToCas = writeToCas; CasToWrite = casToWrite; ReadHoldFromRas = readHoldFromRas;
    }
}
