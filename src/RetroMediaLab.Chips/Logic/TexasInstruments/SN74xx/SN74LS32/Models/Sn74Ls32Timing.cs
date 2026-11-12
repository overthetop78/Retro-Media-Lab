namespace RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS32.Models;

/// <summary>TI LS32 page 4, 5 V,25°C,15 pF,2 kΩ; nanoseconds.</summary>
public sealed class Sn74Ls32Timing
{
    public static Sn74Ls32Timing Typical { get; } = new(14, 14);
    public static Sn74Ls32Timing CharacterizedMaximum { get; } = new(22, 22);
    public long Rise { get; }
    public long Fall { get; }
    public Sn74Ls32Timing(long rise, long fall)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rise);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(fall);
        Rise = rise; Fall = fall;
    }
}
