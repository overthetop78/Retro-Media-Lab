using C = RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Constants.Wd1770Constants;
using Phase = RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Enums.Wd1770Phase;

namespace RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Services;

public sealed partial class Wd1770
{
    /// <summary>
    /// Consomme des clocks réellement reçues, à niveaux d'entrée maintenus. Les
    /// compteurs internes avancent strictement avant leur prochain effet. Le
    /// câblage reprend par Tick à chaque échéance : aucune notification ne peut
    /// intervenir avant qu'il ait présenté les entrées et son instant physique.
    /// L'écriture conserve son chemin unitaire. Le retour peut donc être zéro.
    /// </summary>
    public int AdvanceWithStableInputs(int clocks)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(clocks);
        if (_writer.Gate || !_reset && _phase >= Phase.WriteGap && _phase != Phase.WaitWriteTrack) return 0;
        var reading = !_reset && _phase is Phase.SearchId or Phase.ReadId or Phase.SearchData
            or Phase.ReadData or Phase.ReadDataCrc or Phase.ReadTrack;
        var quiet = clocks;
        if (!_reset && _timer > 0 && quiet >= _timer) quiet = _timer - 1;
        if (reading)
        {
            var sample = _reader.ClocksUntilWork(_readData);
            if (quiet >= sample) quiet = sample - 1;
        }
        if (quiet > 0)
        {
            _writer.AdvanceIdle(quiet);
            if (!_reset)
            {
                if (_timer > 0) _timer -= quiet;
                if (reading) _reader.AdvanceQuiet(_readData, quiet);
            }
        }
        return quiet;
    }

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
