using RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Enums;
using C = RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Constants.Wd1770Constants;

namespace RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Services;

/// <summary>
/// Canal de lecture numérique, cadencé par CLK. RD est normalisé actif.
/// Les transitions recalent la fenêtre de cellule ; le registre glissant détecte les
/// horloges manquantes et établit les frontières d'octet. Aucune piste n'est reconstruite.
/// </summary>
internal sealed partial class Wd1770ReadChannel
{
    private bool _doubleDensity = true;
    private bool _previousReadData;
    private bool _sampling;
    private bool _pulse;
    private bool _aligned;
    private bool _previousDataBit;
    private int _sampleCountdown;
    private int _wordBits;
    private int _a1Run;
    private int _c2Run;
    private ushort _shift;
    private uint _history;

    internal event Action<byte, Wd1770ReadWordKind, bool>? ByteReceived;

    internal bool DoubleDensity
    {
        get => _doubleDensity;
        set
        {
            if (_doubleDensity == value) return;
            _doubleDensity = value;
            Reset();
        }
    }

    private int CellClocks => _doubleDensity ? C.MfmCellClocks : C.FmCellClocks;

    internal void Reset(bool readTrack = false)
    {
        _previousReadData = false;
        _sampling = false;
        _pulse = false;
        _aligned = readTrack;
        _previousDataBit = false;
        _sampleCountdown = 0;
        _wordBits = 0;
        _a1Run = 0;
        _c2Run = 0;
        _shift = 0;
        _history = 0;
    }

    internal void Tick(bool readData)
    {
        var edge = readData && !_previousReadData;
        _previousReadData = readData;
        if (edge)
        {
            _pulse = true;
            if (!_sampling)
            {
                _sampling = true;
                _sampleCountdown = CellClocks / C.HalfCellDivisor;
                return;
            }
            // Un raccord d'écriture peut déphaser les cellules déjà établies. Garder
            // les fronts de frontière sans correction bloque alors le séparateur
            // dans la mauvaise phase. Les ramener progressivement vers le centre,
            // sans ajouter/supprimer immédiatement une cellule sur un seul front.
            var quarter = CellClocks / C.QuarterCellDivisor;
            if (_sampleCountdown >= quarter && _sampleCountdown <= CellClocks - quarter)
                _sampleCountdown = CellClocks / C.HalfCellDivisor;
            else if (_sampleCountdown < quarter) _sampleCountdown++;
            else _sampleCountdown--;
        }
        if (!_sampling || --_sampleCountdown != 0) return;
        ShiftCell(_pulse);
        _pulse = false;
        _sampleCountdown = CellClocks;
    }

    private void ShiftCell(bool transition)
    {
        _history = _history << 1 | (transition ? 1U : 0U);
        _shift = (ushort)_history;
        if (_doubleDensity)
        {
            // Une fois la phase clock/data établie, un motif décalé d'une demi-cellule
            // ne constitue pas un mot à horloge manquante. Les données MFM ordinaires
            // peuvent présenter le même motif dans cette mauvaise phase.
            var clockPhase = !_aligned || (_wordBits & C.LowBit) != 0;
            var zeroPreamble = (_history >> C.CellsPerWord) == C.MfmZeroWord;
            if ((clockPhase || zeroPreamble) && _shift == C.MfmA1Word)
            {
                Synchronize(C.SyncA1, Wd1770ReadWordKind.SyncA1);
                return;
            }
            var consecutiveC2 = _c2Run != 0 && _wordBits == C.CellsPerWord - 1;
            if (clockPhase && (zeroPreamble || consecutiveC2) && _shift == C.MfmC2Word)
            {
                Synchronize(C.SyncC2, Wd1770ReadWordKind.SyncC2);
                return;
            }
        }
        else if (((_history >> C.CellsPerWord) & C.FmZeroPrefixMask) == C.FmZeroPrefix)
        {
            var clocks = _shift & C.AlternatingClockMask;
            if (clocks == C.FmAddressClockLane)
            {
                var data = DecodeLane(_shift);
                if (data is >= C.FirstFmAddressMark and <= C.LastFmAddressMark)
                {
                    Synchronize(data, Wd1770ReadWordKind.AddressMark);
                    return;
                }
            }
            else if (clocks == C.FmIndexClockLane && DecodeLane(_shift) == C.IndexAddressMark)
            {
                Synchronize(C.IndexAddressMark, Wd1770ReadWordKind.IndexMark);
                return;
            }
        }

        if (!_aligned || ++_wordBits != C.CellsPerWord) return;
        _wordBits = 0;
        var value = DecodeLane(_shift);
        var valid = ValidClocks();
        var kind = Wd1770ReadWordKind.Data;
        if (_doubleDensity && valid)
        {
            if (_a1Run >= C.MfmSyncWords && (value == C.IdAddressMark
                || value is >= C.FirstDataAddressMark and <= C.LastDataAddressMark))
                kind = Wd1770ReadWordKind.AddressMark;
            else if (_c2Run >= C.MfmSyncWords && value == C.IndexAddressMark)
                kind = Wd1770ReadWordKind.IndexMark;
        }
        _a1Run = 0;
        _c2Run = 0;
        ByteReceived?.Invoke(value, kind, valid);
    }

    private void Synchronize(byte value, Wd1770ReadWordKind kind)
    {
        var consecutive = _aligned && _wordBits == C.CellsPerWord - 1;
        _aligned = true;
        _wordBits = 0;
        _previousDataBit = (value & C.LowBit) != 0;
        _a1Run = kind == Wd1770ReadWordKind.SyncA1 ? (consecutive ? Math.Min(C.MfmSyncWords, _a1Run + 1) : 1) : 0;
        _c2Run = kind == Wd1770ReadWordKind.SyncC2 ? (consecutive ? Math.Min(C.MfmSyncWords, _c2Run + 1) : 1) : 0;
        ByteReceived?.Invoke(value, kind, true);
    }

    private bool ValidClocks()
    {
        if (!_doubleDensity) return (_shift & C.AlternatingClockMask) == C.AlternatingClockMask;
        var data = _shift & C.AlternatingDataMask;
        var previous = _previousDataBit ? C.CrcHighBit : 0;
        var expected = ~(data << 1 | data >> 1 | previous) & C.AlternatingClockMask;
        _previousDataBit = (data & C.LowBit) != 0;
        return (_shift & C.AlternatingClockMask) == expected;
    }

    private static byte DecodeLane(int word)
    {
        var value = word & C.AlternatingDataMask;
        value = (value | value >> 1) & C.PairMergeMask;
        value = (value | value >> 2) & C.NibbleMergeMask;
        return (byte)((value | value >> 4) & C.ByteMask);
    }
}
