namespace RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS139.Models;

/// <summary>TI SDLS013A p3; select two/three levels then enable, rise/fall, in ns.</summary>
public sealed class Sn74Ls139Timing
{
    public static Sn74Ls139Timing Typical { get; } = new(13, 22, 18, 25, 16, 21);
    public static Sn74Ls139Timing CharacterizedMaximum { get; } = new(20, 33, 29, 38, 24, 32);
    internal long[] Values { get; }
    public Sn74Ls139Timing(long selectTwoRise, long selectTwoFall, long selectThreeRise,
        long selectThreeFall, long enableRise, long enableFall)
    {
        Values = [selectTwoRise, selectTwoFall, selectThreeRise, selectThreeFall, enableRise, enableFall];
        foreach (long value in Values) ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
    }
}
