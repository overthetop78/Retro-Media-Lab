using C = RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Constants.Wd1770Constants;

namespace RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Services;

/// <summary>
/// Registre à décalage WD, encodeur FM/MFM et générateur CRC du silicium.
/// Les temps des fronts sont exprimés en demi-clocks de CLK pour conserver
/// les largeurs nominales fractionnaires. Le propriétaire choisit quand charger DR.
/// </summary>
internal sealed class Wd1770WriteChannel
{
    private uint _word;
    private bool _gate;
    private ulong _halfClocks;
    private int _remainingCells;
    private bool _previousData;
    private int _dataHistory;
    private bool _nextDataBit;
    private ulong _nextCell;
    private ulong _risingEdge;
    private ulong _fallingEdge;
    private ushort _crc = C.CrcInitial;

    internal bool DoubleDensity { get; set; } = true;
    internal bool PrecompensationEnabled { get; set; }
    internal bool Gate => _gate;
    internal bool WriteData { get; private set; }
    internal int RemainingCells => _remainingCells;
    internal bool CanLoad => _remainingCells == 0;
    internal ushort Crc => _crc;
    internal ulong HalfClocks => _halfClocks;
    internal event Action<ulong, bool>? WriteDataChanged;

    private int CellHalfClocks => (DoubleDensity ? C.MfmCellClocks : C.FmCellClocks) * C.HalfClocksPerClock;

    internal void Reset()
    {
        SetGate(false);
        _previousData = false;
        _dataHistory = 0;
        _nextDataBit = false;
        _crc = C.CrcInitial;
        _word = 0;
    }

    internal void SetGate(bool active)
    {
        if (_gate == active) return;
        _gate = active;
        if (active)
        {
            _previousData = false;
            _dataHistory = 0;
            return;
        }
        _remainingCells = 0;
        _word = 0;
        _fallingEdge = 0;
        _risingEdge = 0;
        SetWriteData(false);
    }

    internal ushort PredictCrc()
    {
        var crc = _crc;
        for (var cell = _remainingCells - 1; cell >= 0; cell--)
            if ((cell & C.LowBit) == 0) crc = Wd1770Crc.AddBit(crc, (_word >> cell & C.LowBit) != 0);
        return crc;
    }

    internal void PresetCrc() => _crc = C.CrcInitial;

    /// <summary>Premier bit du prochain DR, disponible avant la fin du mot en cours.</summary>
    internal void SetLookAhead(byte value) => _nextDataBit = (value & C.DataHighBit) != 0;

    /// <summary>Octet DATA ordinaire : F5/F6/F7 ne sont pas des commandes de formatage ici.</summary>
    internal bool TryLoadData(byte value)
    {
        if (!_gate || !CanLoad) return false;
        LoadWord(Encode(value, C.FmOrdinaryClock), C.CellsPerWord);
        return true;
    }

    /// <summary>Octet fourni à Write Track ; F7 occupe deux octets de piste et un seul octet CPU.</summary>
    internal bool TryLoadFormat(byte value)
    {
        if (!_gate || !CanLoad) return false;
        if (value == C.FormatCrc)
        {
            var high = (byte)(_crc >> C.BitsPerByte);
            var low = (byte)_crc;
            var first = Encode(high, C.FmOrdinaryClock);
            var second = Encode(low, C.FmOrdinaryClock);
            LoadWord((uint)first << C.CellsPerWord | second, C.CellsPerWord * C.CrcBytes);
            return true;
        }

        ushort word;
        if (DoubleDensity && value == C.FormatA1)
        {
            // Deux A1 dans la graine, puis le troisième réellement décalé : CDB4 en fin de mot.
            _crc = C.MfmCrcBeforeSync;
            _previousData = true;
            word = C.MfmA1Word;
        }
        else if (DoubleDensity && value == C.FormatC2)
        {
            _previousData = false;
            word = C.MfmC2Word;
        }
        else
        {
            var clocks = C.FmOrdinaryClock;
            if (!DoubleDensity)
            {
                if (value == C.IdAddressMark || value is >= C.FirstDataAddressMark and <= C.LastDataAddressMark)
                {
                    clocks = C.FmAddressClock;
                    _crc = C.CrcInitial;
                }
                else if (value == C.IndexAddressMark) clocks = C.FmIndexClock;
                // F5/F6 sont interdits dans la fiche en FM : aucun effet spécial n'est inventé.
            }
            word = Encode(value, clocks);
        }
        LoadWord(word, C.CellsPerWord);
        return true;
    }

    internal void Tick()
    {
        if (!_gate) { _halfClocks += C.HalfClocksPerClock; return; }
        for (var half = 0; half < C.HalfClocksPerClock; half++)
        {
            _halfClocks++;
            if (_risingEdge != 0 && _halfClocks == _risingEdge)
            {
                _risingEdge = 0;
                SetWriteData(true);
            }
            if (_fallingEdge != 0 && _halfClocks == _fallingEdge)
            {
                _fallingEdge = 0;
                SetWriteData(false);
            }
            if (!_gate || _remainingCells == 0) continue;
            if (_halfClocks == _nextCell - C.PrecompensationHalfClocks) PreparePulse();
            if (_halfClocks != _nextCell) continue;
            _remainingCells--;
            var bit = (_word >> _remainingCells & C.LowBit) != 0;
            if ((_remainingCells & C.LowBit) == 0)
            {
                _crc = Wd1770Crc.AddBit(_crc, bit);
                _dataHistory = (_dataHistory << 1 | (bit ? 1 : 0)) & C.PreviousDataMask;
            }
            _nextCell += (ulong)CellHalfClocks;
        }
    }

    internal void AdvanceIdle(int clocks) => _halfClocks += (ulong)clocks * C.HalfClocksPerClock;

    private void LoadWord(uint word, int cells)
    {
        _word = word;
        _remainingCells = cells;
        _nextDataBit = false;
        _nextCell = _halfClocks + (ulong)CellHalfClocks;
    }

    private void PreparePulse()
    {
        var position = _remainingCells - 1;
        if ((_word >> position & C.LowBit) == 0) return;
        var offset = 0;
        if (DoubleDensity && PrecompensationEnabled)
        {
            var dataPosition = position & ~C.LowBit;
            var current = (int)(_word >> dataPosition & C.LowBit);
            var next = dataPosition >= C.CellsPerDataBit
                ? (int)(_word >> (dataPosition - C.CellsPerDataBit) & C.LowBit)
                : _nextDataBit ? 1 : 0;
            var pattern = _dataHistory << C.CellsPerDataBit | current << 1 | next;
            if ((pattern & C.PrecompensationShortMask) == C.EarlyDataPattern || pattern == C.EarlyClockPattern)
                offset = -C.PrecompensationHalfClocks;
            else if ((pattern & C.PrecompensationShortMask) == C.LateDataPattern || pattern == C.LateClockPattern)
                offset = C.PrecompensationHalfClocks;
        }
        _risingEdge = (ulong)((long)_nextCell + offset);
        _fallingEdge = _nextCell + (ulong)(DoubleDensity ? C.MfmPulseHalfClocks : C.FmPulseHalfClocks);
        if (_risingEdge == _halfClocks)
        {
            _risingEdge = 0;
            SetWriteData(true);
        }
    }

    private void SetWriteData(bool active)
    {
        if (WriteData == active) return;
        WriteData = active;
        WriteDataChanged?.Invoke(_halfClocks, active);
    }

    private ushort Encode(byte value, byte clocks)
    {
        var word = 0;
        for (var bit = C.BitsPerByte - 1; bit >= 0; bit--)
        {
            var data = (value >> bit & C.LowBit) != 0;
            var clock = DoubleDensity ? !_previousData && !data : (clocks >> bit & C.LowBit) != 0;
            word = word << C.CellsPerDataBit | (clock ? C.RawClockBit : 0) | (data ? C.LowBit : 0);
            _previousData = data;
        }
        return (ushort)word;
    }
}
