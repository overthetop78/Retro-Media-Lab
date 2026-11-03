using RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Enums;
using C = RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Constants.Upd765Constants;

namespace RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Services;

/// <summary>Registre série du 765 : WCLK est fourni par la carte, WDATA reste non
/// précompensé. Les demandes de présélection PS et leur décalage externe sont distincts.</summary>
internal sealed class Upd765WriteChannel
{
    private bool _doubleDensity;
    private bool _clock;
    private bool _gate;
    private bool _previousData;
    private bool _queued;
    private byte _queuedValue;
    private Upd765WriteWordKind _queuedKind;
    private ushort _word;
    private int _cells;
    private bool _wordComplete;
    private bool _outputPending;
    private bool _outputBit;
    private ulong _outputAt;
    private bool _fallPending;
    private ulong _fallAt;
    private ulong _halfClocks;
    private int _pulseDistance;
    private bool _havePulse;

    internal event Action? WordRequested;
    internal event Action? WordCompleted;
    internal event Action<ulong, bool>? WriteDataChanged;
    internal event Action<ulong, bool>? WriteEnableChanged;
    internal event Action<ulong, Upd765Precompensation>? PrecompensationChanged;
    internal Func<(byte Value, Upd765WriteWordKind Kind)?>? PreviewNextWord { get; set; }
    internal Upd765Precompensation Precompensation { get; private set; }

    // Preview only: neither CRC nor the sequencer advances until WCLK actually shifts DATA.
    internal ushort CrcAtWordEnd
    {
        get
        {
            var crc = Crc;
            if (CrcEnabled)
                for (var bit = _cells - 1; bit >= 0; bit--)
                    if ((bit & C.LowBit) == 0) crc = Upd765Crc.AddBit(crc, (_word >> bit & C.LowBit) != 0);
            return crc;
        }
    }

    internal bool DoubleDensity
    {
        get => _doubleDensity;
        set
        {
            if (_doubleDensity == value) return;
            Reset();
            _doubleDensity = value;
        }
    }

    internal bool Gate => _gate;
    internal bool WriteEnable { get; private set; }
    internal bool WriteData { get; private set; }
    internal bool CrcEnabled { get; set; } = true;
    internal ushort Crc { get; private set; } = C.CrcInitial;
    internal ulong HalfClocks => _halfClocks;
    internal long NextEventClocks
    {
        get
        {
            if (!_outputPending && !_fallPending) return long.MaxValue;
            var next = _outputPending ? _outputAt : _fallAt;
            if (_fallPending) next = Math.Min(next, _fallAt);
            return Math.Max(1, (long)(next - _halfClocks + C.HalfClocksPerClock - 1) / C.HalfClocksPerClock);
        }
    }

    internal void Reset()
    {
        SetGate(false);
        _clock = false;
        _previousData = false;
        CrcEnabled = true;
        ResetCrc();
    }

    internal void ResetCrc(ushort seed = C.CrcInitial) => Crc = seed;

    internal bool TryQueue(byte value, Upd765WriteWordKind kind = Upd765WriteWordKind.Data)
    {
        if (_queued) return false;
        _queuedValue = value;
        _queuedKind = kind;
        _queued = true;
        return true;
    }

    internal void SetGate(bool active)
    {
        _gate = active;
        if (active) return;
        _cells = 0;
        _queued = _wordComplete = _outputPending = _fallPending = false;
        _previousData = _havePulse = false;
        _pulseDistance = 0;
        SetPrecompensation(Upd765Precompensation.Normal);
        SetWriteEnable(false);
        SetWriteData(false);
    }

    internal void SetClock(bool high)
    {
        if (_clock == high) return;
        _clock = high;
        if (!_gate) return;
        if (!high)
        {
            _fallPending = true;
            _fallAt = _halfClocks + C.WriteSignalDelayHalfClocks;
            return;
        }
        if (_cells == 0)
        {
            if (!_queued) WordRequested?.Invoke();
            if (!_gate || !_queued) return;
            LoadWord();
        }

        var position = --_cells;
        var data = (_word >> position & C.LowBit) != 0;
        if ((position & C.LowBit) == 0 && CrcEnabled) Crc = Upd765Crc.AddBit(Crc, data);
        _wordComplete = _cells == 0;
        _pulseDistance = Math.Min(_pulseDistance + 1, C.PrecompensationLookaheadCells + 1);
        var preshift = SelectPrecompensation(data);
        if (data) { _havePulse = true; _pulseDistance = 0; }
        SetPrecompensation(preshift);
        if (!_gate) return;
        // Délai numérique nominal de 1/2 CLK : 62,5 ns à 8 MHz, dans
        // l'intervalle NEC tCD/tCWE 20..100 ns (valeurs doublées à 4 MHz).
        _outputBit = data;
        _outputAt = _halfClocks + C.WriteSignalDelayHalfClocks;
        _outputPending = true;
    }

    internal void Advance(int clocks)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(clocks);
        AdvanceHalfClocks((long)clocks * C.HalfClocksPerClock);
    }

    internal void AdvanceHalfClocks(long halfClocks)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(halfClocks);
        var target = _halfClocks + (ulong)halfClocks;
        while (_outputPending || _fallPending)
        {
            var next = _outputPending ? _outputAt : _fallAt;
            if (_fallPending) next = Math.Min(next, _fallAt);
            if (next > target) break;
            _halfClocks = next;
            if (_outputPending && _outputAt == next)
            {
                _outputPending = false;
                SetWriteEnable(true);
                if (_gate) SetWriteData(_outputBit);
            }
            if (_fallPending && _fallAt == next)
            {
                _fallPending = false;
                SetWriteData(false);
                if (_wordComplete && _gate)
                {
                    _wordComplete = false;
                    WordCompleted?.Invoke();
                }
            }
        }
        _halfClocks = target;
    }

    private void LoadWord()
    {
        _queued = false;
        _cells = C.EncodedCellsPerByte;
        _word = EncodeWord(_queuedValue, _queuedKind, ref _previousData);
    }

    private ushort EncodeWord(byte value, Upd765WriteWordKind kind, ref bool previous)
    {
        if (_doubleDensity && kind is Upd765WriteWordKind.SyncA1 or Upd765WriteWordKind.SyncC2)
        {
            var sync = kind == Upd765WriteWordKind.SyncA1 ? C.MfmA1Word : C.MfmC2Word;
            previous = (sync & C.LowBit) != 0;
            return sync;
        }
        var clocks = kind switch
        {
            Upd765WriteWordKind.FmAddress => C.FmAddressClock,
            Upd765WriteWordKind.FmIndex => C.FmIndexClock,
            _ => C.FmOrdinaryClock
        };
        var word = 0;
        for (var bit = C.BitsPerByte - 1; bit >= 0; bit--)
        {
            var data = (value >> bit & C.LowBit) != 0;
            var clock = _doubleDensity ? !(previous || data) : (clocks >> bit & C.LowBit) != 0;
            word = word << C.CellsPerDataBit | (clock ? C.RawClockBit : 0) | (data ? C.LowBit : 0);
            previous = data;
        }
        return (ushort)word;
    }

    private Upd765Precompensation SelectPrecompensation(bool pulse)
    {
        if (!_doubleDensity || !pulse || !_havePulse) return Upd765Precompensation.Normal;
        var next = NextPulseDistance();
        if (next == 0) return Upd765Precompensation.Normal;
        // Modèle nominal MFM à un niveau : compenser l'asymétrie autour d'un
        // espacement court. NEC ne publie pas la table interne des motifs PS.
        if (_pulseDistance == C.PrecompensationCloseCells && next > C.PrecompensationCloseCells)
            return Upd765Precompensation.Early;
        if (_pulseDistance > C.PrecompensationCloseCells && next == C.PrecompensationCloseCells)
            return Upd765Precompensation.Late;
        return Upd765Precompensation.Normal;
    }

    private int NextPulseDistance()
    {
        ushort? preview = null;
        for (var distance = 1; distance <= C.PrecompensationLookaheadCells; distance++)
        {
            if (distance <= _cells)
            {
                if ((_word >> (_cells - distance) & C.LowBit) != 0) return distance;
                continue;
            }
            if (!preview.HasValue)
            {
                var next = _queued ? (_queuedValue, _queuedKind) : PreviewNextWord?.Invoke();
                if (!next.HasValue) return 0;
                var previous = _previousData;
                preview = EncodeWord(next.Value.Item1, next.Value.Item2, ref previous);
            }
            var position = C.EncodedCellsPerByte - (distance - _cells);
            if ((preview.Value >> position & C.LowBit) != 0) return distance;
        }
        return C.PrecompensationLookaheadCells + 1;
    }

    private void SetPrecompensation(Upd765Precompensation value)
    {
        if (Precompensation == value) return;
        Precompensation = value;
        PrecompensationChanged?.Invoke(_halfClocks, value);
    }

    private void SetWriteData(bool active)
    {
        if (WriteData == active) return;
        WriteData = active;
        WriteDataChanged?.Invoke(_halfClocks, active);
    }

    private void SetWriteEnable(bool active)
    {
        if (WriteEnable == active) return;
        WriteEnable = active;
        WriteEnableChanged?.Invoke(_halfClocks, active);
    }
}
