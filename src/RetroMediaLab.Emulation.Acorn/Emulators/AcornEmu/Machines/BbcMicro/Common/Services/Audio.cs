using RetroMediaLab.Chips.Audio.TexasInstruments.SN764xx.SN76489.Services;
using AudioConstants = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Constants.AudioConstants;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services;

/// <summary>Raccorde le PSG BBC à l'horloge CPU et transforme son signal mono en PCM signé sur seize bits.</summary>
/// <remarks>Les échantillons intègrent les niveaux traversés pendant leur durée, au lieu de prendre seulement
/// la dernière valeur. La phase rationnelle reste conservée entre les images : aucun arrondi par frame ne modifie
/// la fréquence ou la durée. Le couplage continu retire le piédestal du PSG, sans filtre de présentation ajouté.
/// Cette classe ne crée aucune sortie, aucun thread et aucune ressource externe.</remarks>
internal sealed partial class Audio
{
    private readonly Sn76489 _chip = new();
    private readonly List<short> _samples = [];
    private readonly double _coupling = Math.Exp(-1 / (AudioConstants.SampleRate * AudioConstants.CouplingTimeSeconds));
    private int _phase;
    private int _level;
    private int _pendingTicks;
    private int _clockCountdown;

    internal Audio()
    {
        _level = _chip.Output;
        _chip.OutputChanged = level => _level = level;
        _clockCountdown = NextClockDistance();
    }
    private long _area;
    private double _previousInput, _previousOutput;

    /// <summary>Écrit le bus lent lorsque la carte valide le PSG.</summary>
    internal void Write(byte value)
    {
        FlushPendingClock();
        _chip.Write(value);
        _clockCountdown = NextClockDistance();
    }

    /// <summary>CE est câblé bas sur le BBC ; IC32 pilote WE. Les données restent présentes entre les accès CPU.</summary>
    internal void SetBus(byte value, bool writeEnabled)
    {
        FlushPendingClock();
        _chip.SetBus(value, chipSelected: true, writeEnabled);
        _clockCountdown = NextClockDistance();
    }

    /// <summary>Réinitialise le circuit, la phase et les données non encore transmises.</summary>
    internal void Reset()
    {
        _chip.Reset(); _samples.Clear(); _phase = 0; _area = 0; _previousInput = _previousOutput = 0;
        _pendingTicks = 0;
        _clockCountdown = NextClockDistance();
    }

    /// <summary>Un cycle CPU apporte deux périodes de l'horloge sonore BBC de 4 MHz.</summary>
    internal void Tick()
    {
        ++_pendingTicks;
        if (--_clockCountdown == 0) FlushPendingClock();
    }

    private int NextClockDistance()
    {
        const int clocks = AudioConstants.SoundClock / AudioConstants.CpuClock;
        var sound = (_chip.ClocksUntilNextEvent + clocks - 1) / clocks;
        var sample = (AudioConstants.CpuClock - _phase + AudioConstants.SampleRate - 1) / AudioConstants.SampleRate;
        return sound < sample ? sound : sample;
    }

    private void FlushPendingClock()
    {
        if (_pendingTicks == 0) return;
        var ticks = _pendingTicks;
        _pendingTicks = 0;
        // Aucun front PSG ni frontière PCM ne se trouve avant le dernier cycle
        // conservé. Son niveau peut changer : il garde son intégration séparée.
        if (ticks > 1) Integrate(_level, (ticks - 1) * AudioConstants.SampleRate);
        _chip.AdvanceClocks(ticks * (AudioConstants.SoundClock / AudioConstants.CpuClock));
        Integrate(_level, AudioConstants.SampleRate);
        _clockCountdown = NextClockDistance();
    }

    private void Integrate(int level, int durationUnits)
    {
        // La majorité des cycles ne franchit aucune frontière PCM. Conserver exactement
        // la même intégrale entière, sans boucle ni recherche de minimum dans ce cas.
        if (durationUnits < AudioConstants.CpuClock - _phase)
        {
            _area += (long)level * durationUnits;
            _phase += durationUnits;
            return;
        }
        var remaining = durationUnits;
        while (remaining > 0)
        {
            var duration = Math.Min(remaining, AudioConstants.CpuClock - _phase);
            _area += (long)level * duration; _phase += duration; remaining -= duration;
            if (_phase != AudioConstants.CpuClock) continue;
            var input = (double)_area / AudioConstants.CpuClock;
            _previousOutput = input - _previousInput + _coupling * _previousOutput;
            _previousInput = input;
            _samples.Add((short)Math.Clamp(Math.Round(_previousOutput), short.MinValue, short.MaxValue));
            _phase = 0; _area = 0;
        }
    }

    /// <summary>Transfère une copie possédée des échantillons terminés, en conservant l'intégrale partielle.</summary>
    internal short[] DrainSamples()
    {
        FlushPendingClock();
        var samples = _samples.ToArray(); _samples.Clear(); return samples;
    }
}
