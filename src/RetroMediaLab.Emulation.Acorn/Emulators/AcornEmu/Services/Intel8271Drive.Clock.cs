using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants.Intel8271DriveConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;

internal sealed partial class Intel8271Drive
{
    private int _clockMutationDepth;
    private long? _publishedClockEvent;
    private Action<long?>? _clockEventChanged;
    private bool _publishedClockGrouping = true;

    internal bool CanGroupClocks => !_motor || (_pulses.Length == 0 && _writingImage is null);
    internal event Action<bool>? ClockGroupingChanged;

    private void PublishClockGrouping()
    {
        var allowed = CanGroupClocks;
        if (_publishedClockGrouping == allowed) return;
        _publishedClockGrouping = allowed;
        ClockGroupingChanged?.Invoke(allowed);
    }

    /// <summary>Prochaine période source nécessitant le parcours canonique du lecteur.</summary>
    internal long? NextClockEvent
    {
        get
        {
            if (_ownerTick == long.MaxValue) return null;
            var immediate = _ownerTick + 1;
            if (_signalsChanged || _readDataHigh || (_motor && _writingImage is null && _trackChanged)
                || _nextControllerTick <= immediate) return immediate;
            var next = _nextControllerTick;
            if (_motor)
            {
                var rotation = _rotation[_previousDrive];
                // Les cadences non usuelles restent sur leur parcours de référence.
                if (rotation >= C.RevolutionNanoseconds || _nanosecondsPerTick >= C.RevolutionNanoseconds
                    || _indexLevels[_previousDrive] != (rotation < C.IndexNanoseconds)) return immediate;
                var distance = rotation < C.IndexNanoseconds
                    ? C.IndexNanoseconds - rotation : C.RevolutionNanoseconds - rotation;
                if (_writingImage is null && _nextPulse < _pulses.Length)
                {
                    if (_pulses[_nextPulse] <= rotation) return immediate;
                    distance = Math.Min(distance, _pulses[_nextPulse] - rotation);
                }
                next = Math.Min(next, ClockAtDistance(distance));
            }
            if (_writingImage is not null)
            {
                var elapsed = _time - _writeStarted;
                if (elapsed >= C.RevolutionNanoseconds) return immediate;
                next = Math.Min(next, ClockAtDistance(C.RevolutionNanoseconds - elapsed));
            }
            return next == long.MaxValue ? null : next;
        }
    }

    internal event Action<long?>? NextClockEventChanged
    {
        add
        {
            if (_clockEventChanged is null) _publishedClockEvent = NextClockEvent;
            _clockEventChanged += value;
        }
        remove => _clockEventChanged -= value;
    }

    /// <summary>
    /// Représente les périodes sans effet, strictement avant la prochaine frontière.
    /// Les entrées modifiées et transactions doivent être livrées après rattrapage
    /// à leur date source ; la carte ne doit pas traverser une échéance notifiée.
    /// </summary>
    internal void AdvanceTo(long ownerTick)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(ownerTick, _ownerTick);
        if (ownerTick == _ownerTick) return;
        if (ownerTick - _ownerTick == 1) { Tick(); return; }
        _clockMutationDepth++;
        try
        {
            while (_ownerTick < ownerTick)
            {
                // Les intervalles de flux FM et l'écriture sont trop courts pour
                // amortir le calcul des préfixes (mesure Debug). Garder leurs clocks
                // ordinaires ; les signaux peuvent toujours changer ce choix.
                if (_motor && (_pulses.Length != 0 || _writingImage is not null))
                {
                    TickCore();
                    continue;
                }
                var next = NextClockEvent;
                var end = next.HasValue ? Math.Min(ownerTick, next.Value - 1) : ownerTick;
                if (end > _ownerTick) AdvanceQuietClocks(end - _ownerTick);
                // Le préfixe ne change aucune cause de l'échéance calculée :
                // traiter sa frontière directement, sans la rechercher à nouveau.
                if (_ownerTick < ownerTick) TickCore();
            }
        }
        finally { _clockMutationDepth--; PublishClockEvent(); }
    }

    private long ClockAtDistance(ulong nanoseconds)
    {
        // floor((n*1e9 + fraction)/fréquence) >= distance ; arrondi identique à Tick.
        // distance <= une révolution (200 ms), fréquence <= Int32.MaxValue :
        // le produit tient dans UInt64, sans division UInt128 sur chaque flux.
        var numerator = nanoseconds * (uint)_ownerClock - (ulong)_timeFraction;
        var clocks = (numerator + C.NanosecondsPerSecond - 1) / C.NanosecondsPerSecond;
        if (clocks > (ulong)(long.MaxValue - _ownerTick)) return long.MaxValue;
        return _ownerTick + Math.Max(1, (long)clocks);
    }

    private void AdvanceQuietClocks(long clocks)
    {
        if (_nanosecondRemainder == 0)
        {
            // Même arithmétique modulo UInt64 que les ajouts Tick, y compris
            // sur les grandes avances ; aucun calcul de fraction nécessaire.
            var integralElapsed = unchecked((ulong)clocks * _nanosecondsPerTick);
            _time = unchecked(_time + integralElapsed);
            if (_motor) _rotation[_previousDrive] += (uint)integralElapsed;
            _ownerTick += clocks;
            return;
        }
        var fraction = (UInt128)(ulong)clocks * (uint)_nanosecondRemainder + (ulong)_timeFraction;
        var elapsed = (UInt128)(ulong)clocks * _nanosecondsPerTick + fraction / (uint)_ownerClock;
        _timeFraction = (long)(fraction % (uint)_ownerClock);
        _time = unchecked(_time + (ulong)elapsed);
        if (_motor) _rotation[_previousDrive] += (uint)elapsed;
        _ownerTick += clocks;
    }

    private void PublishClockEvent()
    {
        if (_clockMutationDepth != 0 || _clockEventChanged is null) return;
        var next = NextClockEvent;
        if (_publishedClockEvent == next) return;
        _publishedClockEvent = next;
        _clockEventChanged?.Invoke(next);
    }
}
