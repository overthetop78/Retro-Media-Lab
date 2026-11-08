using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Constants.UserPortPointerConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services;

/// <summary>Adaptation des entrées hôtes aux broches du port utilisateur, sans sondage du VIA.</summary>
/// <remarks>Les compteurs bornés représentent les déplacements hôtes à rééchantillonner, pas une FIFO
/// ajoutée au matériel. Les fronts de quadrature entrent dans le VIA à leur échéance ; ses registres,
/// modes, interruptions et horloges restent entièrement possédés par le circuit existant.</remarks>
internal sealed class UserPortPointer
{
    private readonly byte _xMask, _yMask, _leftMask, _middleMask, _rightMask;
    private readonly Action<bool> _setCb1, _setCb2;
    private Motion _x, _y;
    private long _lastInputTime;
    private long _sampleCycles = C.InitialHostSampleCycles;
    private bool _hasInputTime;
    private bool _phaseX, _phaseY;
    private long _nextSignal = long.MaxValue;
    internal byte PortB { get; private set; } = byte.MaxValue;
    internal long Time { get; private set; }
    internal long? NextSignal => _nextSignal == long.MaxValue ? null : _nextSignal;
    internal Action<long?>? NextSignalChanged { get; set; }
    internal Action<bool>? Pb6Changed { get; set; }

    internal UserPortPointer(bool acornPinout, Action<bool> setCb1, Action<bool> setCb2)
    {
        (_xMask, _yMask, _leftMask, _middleMask, _rightMask) = acornPinout
            ? (C.CompactX, C.CompactY, C.CompactLeft, C.CompactMiddle, C.CompactRight)
            : (C.UserX, C.UserY, C.UserLeft, C.UserMiddle, C.UserRight);
        _setCb1 = setCb1;
        _setCb2 = setCb2;
    }

    internal void SetInput(EmulationPointerState pointer, long time)
    {
        AdvanceTo(time);
        var buttons = _leftMask | _middleMask | _rightMask;
        var previous = PortB;
        PortB = (byte)((PortB | buttons)
            & ~(pointer.Left ? _leftMask : 0) & ~(pointer.Middle ? _middleMask : 0) & ~(pointer.Right ? _rightMask : 0));
        if (((previous ^ PortB) & 0x40) != 0) Pb6Changed?.Invoke((PortB & 0x40) != 0);
        if (_hasInputTime && time > _lastInputTime) _sampleCycles = time - _lastInputTime;
        _lastInputTime = time;
        _hasInputTime = true;
        // Les deltas décrivent le mouvement sur un intervalle d'entrée, pas un ordre de
        // faire tourner une roue à débit constant jusqu'à épuisement d'une file.
        _x.Schedule(pointer.DeltaX, time, _sampleCycles);
        _y.Schedule(pointer.DeltaY, time, _sampleCycles);
        SetNext(Math.Min(_x.Next, _y.Next));
    }

    internal void AdvanceTo(long time)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(time, Time);
        while (_nextSignal <= time && _nextSignal != long.MaxValue)
        {
            Time = _nextSignal;
            var x = _x.Next == Time;
            var y = _y.Next == Time;
            if (x)
            {
                _phaseX = !_phaseX;
                SetDirection(_xMask, _x.Positive ? !_phaseX : _phaseX);
                _x.Step();
            }
            if (y)
            {
                _phaseY = !_phaseY;
                SetDirection(_yMask, _y.Positive ? _phaseY : !_phaseY);
                _y.Step();
            }
            // Les données des deux axes sont stables avant les fronts d'entrée.
            if (x) _setCb1(_phaseX);
            if (y) _setCb2(_phaseY);
            SetNext(Math.Min(_x.Next, _y.Next));
        }
        Time = time;
    }

    /// <summary>Réapplique les niveaux lors du raccordement ou du reset du récepteur, sans déplacer la souris.</summary>
    internal void SynchronizePins() { _setCb1(_phaseX); _setCb2(_phaseY); }

    private void SetDirection(byte mask, bool high) => PortB = high ? (byte)(PortB | mask) : (byte)(PortB & ~mask);

    /// <summary>Répartit les déplacements relatifs reçus sur leur fenêtre temporelle, axe par axe.</summary>
    private struct Motion
    {
        private long _start, _duration, _total, _emitted, _remaining;
        internal readonly bool Positive => _remaining > 0;
        internal readonly long Next => _remaining == 0 ? long.MaxValue
            : checked(_start + ((_emitted + 1) * _duration + _total - 1) / _total);

        internal void Schedule(int delta, long time, long duration)
        {
            // Une arrivée anticipée peut recouvrir la fin du lot précédent. Conserver son
            // déplacement restant, mais le répartir dans la nouvelle fenêtre sans dette temporelle.
            _remaining = Math.Clamp(_remaining + delta, int.MinValue, int.MaxValue);
            _total = Math.Abs(_remaining);
            _emitted = 0;
            _start = time;
            _duration = duration;
        }

        internal void Step() { _remaining -= Math.Sign(_remaining); _emitted++; }
    }

    private void SetNext(long time)
    {
        if (_nextSignal == time) return;
        _nextSignal = time;
        NextSignalChanged?.Invoke(NextSignal);
    }
}
