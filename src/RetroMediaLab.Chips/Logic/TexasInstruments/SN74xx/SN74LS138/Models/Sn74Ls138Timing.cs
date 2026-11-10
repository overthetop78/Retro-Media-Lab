namespace RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS138.Models;

/// <summary>TI SDLS014 p4, LS variant at 5 V, 25 °C, RL=2 kΩ, CL=15 pF.
/// Paths retain their complete characterized delay; no invented internal gate delay.</summary>
public sealed class Sn74Ls138Timing
{
    public static Sn74Ls138Timing Typical { get; } = new(11, 18, 21, 20, 12, 20, 14, 13);
    public static Sn74Ls138Timing CharacterizedMaximum { get; } = new(20, 41, 27, 39, 18, 32, 26, 38);
    public long SelectTwoRise { get; }
    public long SelectTwoFall { get; }
    public long SelectThreeRise { get; }
    public long SelectThreeFall { get; }
    public long EnableTwoRise { get; }
    public long EnableTwoFall { get; }
    public long EnableThreeRise { get; }
    public long EnableThreeFall { get; }
    internal long Maximum { get; }

    public Sn74Ls138Timing(long selectTwoRise, long selectTwoFall, long selectThreeRise,
        long selectThreeFall, long enableTwoRise, long enableTwoFall, long enableThreeRise, long enableThreeFall)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(selectTwoRise);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(selectTwoFall);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(selectThreeRise);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(selectThreeFall);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(enableTwoRise);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(enableTwoFall);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(enableThreeRise);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(enableThreeFall);
        SelectTwoRise = selectTwoRise; SelectTwoFall = selectTwoFall;
        SelectThreeRise = selectThreeRise; SelectThreeFall = selectThreeFall;
        EnableTwoRise = enableTwoRise; EnableTwoFall = enableTwoFall;
        EnableThreeRise = enableThreeRise; EnableThreeFall = enableThreeFall;
        Maximum = Math.Max(Math.Max(Math.Max(selectTwoRise, selectTwoFall), Math.Max(selectThreeRise, selectThreeFall)),
            Math.Max(Math.Max(enableTwoRise, enableTwoFall), Math.Max(enableThreeRise, enableThreeFall)));
    }
}
