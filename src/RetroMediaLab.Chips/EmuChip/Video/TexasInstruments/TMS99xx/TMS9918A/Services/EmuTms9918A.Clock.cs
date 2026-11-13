using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;

namespace RetroMediaLab.Chips.EmuChip.Video.TexasInstruments.TMS99xx.TMS9918A.Services;

public sealed partial class EmuTms9918A
{
    public void ReceiveMasterClock(long timeNanoseconds, bool high)
    {
        AdvanceTo(timeNanoseconds);
        if (_clock.MasterHigh == high) return;
        if (_deliveringClock || _publishing) throw new InvalidOperationException("Recursive master clock transition.");
        DetachMasterClockSource();
        _ = checked(_clock.HalfEdges + 1);
        AdvanceMasterHalfEdges(1);
    }

    public void ReceiveMasterClockTrain(Tms9918ClockTrain train)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(train.OriginNanoseconds);
        ArgumentOutOfRangeException.ThrowIfNegative(train.FirstEdgeIndex);
        ArgumentOutOfRangeException.ThrowIfNegative(train.EdgeCount);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(train.HalfPeriodDenominator);
        ArgumentOutOfRangeException.ThrowIfLessThan(train.HalfPeriodNumerator, train.HalfPeriodDenominator);
        if (train.EdgeCount == 0) return;
        if (_deliveringClock || _publishing) throw new InvalidOperationException("Recursive master clock train.");
        long lastIndex = checked(train.FirstEdgeIndex + train.EdgeCount - 1L);
        long lastTime = TrainDate(train, lastIndex);
        long firstTime = TrainDate(train, train.FirstEdgeIndex);
        ArgumentOutOfRangeException.ThrowIfLessThan(firstTime, _time);
        _ = checked(_clock.HalfEdges + train.EdgeCount);
        DetachMasterClockSource();

        int consumed = 0;
        while (consumed < train.EdgeCount)
        {
            // L'état des demi-fronts sans effet est calculé directement. Les
            // frontières CPUCLK, GROMCLK et pixel sont toujours résolues et publiées.
            int phase = _clock.HalfEdgePhase;
            int distance = Math.Min(Math.Min(3 - phase % 3, 2 - phase % 2),
                24 - _clock.GromHalfEdgePhase % 24);
            int count = Math.Min(distance, train.EdgeCount - consumed);
            long date = TrainDate(train, train.FirstEdgeIndex + consumed + count - 1L);
            if (count > 1 && _nextDue >= 0 && _nextDue <= date)
            {
                long firstDate = TrainDate(train, train.FirstEdgeIndex + consumed);
                // Même une échéance <= premier front peut en réarmer une autre
                // entre les fronts. Appliquer ce premier front avant cette future
                // observation, comme dans le parcours par niveaux explicites.
                count = 1;
                date = firstDate;
            }
            AdvanceTo(date);
            AdvanceMasterHalfEdges(count);
            consumed += count;
        }
        // lastTime est calculé avant mutation pour valider le débordement du train entier.
        System.Diagnostics.Debug.Assert(_time == lastTime);
    }

    private static long TrainDate(Tms9918ClockTrain train, long index)
    {
        Int128 value = (Int128)train.OriginNanoseconds
            + (Int128)index * train.HalfPeriodNumerator / train.HalfPeriodDenominator;
        if (value > long.MaxValue) throw new ArgumentOutOfRangeException(nameof(train));
        return (long)value;
    }

    private void AdvanceMasterHalfEdges(int count)
    {
        ResolveVramReadInputs(_time);
        bool hadCpuObserver = CpuClockChanged is not null, hadGromObserver = GromClockChanged is not null;
        int phase = (_clock.HalfEdgePhase + count) % 12;
        int cpuTransitions = (_clock.HalfEdgePhase % 3 + count) / 3;
        int pixelTransitions = (_clock.HalfEdgePhase % 2 + count) / 2;
        bool cpuChanged = cpuTransitions != 0;
        bool pixelChanged = pixelTransitions != 0;
        // TI MP010A §3.6 : maître /24, soit 48 demi-fronts par période.
        // Rapport cyclique 50 % et phase de départ : représentation nominale.
        int gromPhase = (_clock.GromHalfEdgePhase + count) % 48;
        int gromTransitions = (_clock.GromHalfEdgePhase % 24 + count) / 24;
        bool gromChanged = gromTransitions != 0;
        _clock = new((count & 1) == 0 ? _clock.MasterHigh : !_clock.MasterHigh, phase,
            (cpuTransitions & 1) != 0 ? !_clock.CpuClockHigh : _clock.CpuClockHigh,
            (pixelTransitions & 1) != 0 ? !_clock.PixelClockHigh : _clock.PixelClockHigh, _clock.HalfEdges + count,
            gromPhase, (gromTransitions & 1) != 0 ? !_clock.GromClockHigh : _clock.GromClockHigh);
        _deliveringClock = true;
        try
        {
            if (_ramLoadActive && _clock.HalfEdges >= _ramLoadHalfEdge) ApplyRamLoad();
            if (pixelChanged && _clock.PixelClockHigh && !_reset)
            { ApplyExternalSyncAtPixel(); ProduceRasterPixel(); }
            // A listener attached by the pixel callback did not observe an
            // earlier edge inside this prefix. Only a coincident edge may be
            // delivered to that new connection at the current date.
            if (cpuChanged && (hadCpuObserver || _clock.HalfEdgePhase % 3 == 0)
                && CpuClockChanged is { } listener)
            {
                bool wasPublishing = _publishing;
                _publishing = true;
                try { listener(new(_time, _clock.CpuClockHigh)); }
                finally { _publishing = wasPublishing; }
            }
            if (gromChanged && (hadGromObserver || _clock.GromHalfEdgePhase % 24 == 0)
                && GromClockChanged is { } gromListener)
            {
                bool wasPublishing = _publishing;
                _publishing = true;
                try { gromListener(new(_time, _clock.GromClockHigh)); }
                finally { _publishing = wasPublishing; }
            }
        }
        finally { _deliveringClock = false; }
    }
}
