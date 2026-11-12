namespace RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS257.Models;

/// <summary>TI SDLS148 p4 LS257B; data/select rise/fall, enable high/low, disable high/low, ns.</summary>
public sealed class Sn74Ls257Timing
{
    public static Sn74Ls257Timing Typical { get; } = new(8, 10, 16, 17, 15, 19, 18, 16);
    public static Sn74Ls257Timing CharacterizedMaximum { get; } = new(13, 15, 21, 24, 30, 30, 30, 25);
    internal long[] Values { get; }
    public Sn74Ls257Timing(long dataRise, long dataFall, long selectRise, long selectFall,
        long enableHigh, long enableLow, long disableHigh, long disableLow)
    {
        Values = [dataRise, dataFall, selectRise, selectFall, enableHigh, enableLow, disableHigh, disableLow];
        foreach (long value in Values) ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
    }
}
