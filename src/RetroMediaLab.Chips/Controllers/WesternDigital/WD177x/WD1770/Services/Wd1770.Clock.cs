using C = RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Constants.Wd1770Constants;

namespace RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Services;

public sealed partial class Wd1770
{
    /// <summary>Cycles CLK réellement avancés ; le compteur de demi-cycles reste dans le canal interne.</summary>
    public ulong ClockCount => _writer.HalfClocks / C.HalfClocksPerClock;

    /// <summary>Avancement absolu dans l'horloge propre du contrôleur, sans conversion depuis le CPU.</summary>
    public void AdvanceTo(ulong clockCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(clockCount, ClockCount);
        var remaining = clockCount - ClockCount;
        while (remaining != 0)
        {
            var clocks = (int)Math.Min(remaining, (ulong)int.MaxValue);
            Advance(clocks);
            remaining -= (ulong)clocks;
        }
    }
}
