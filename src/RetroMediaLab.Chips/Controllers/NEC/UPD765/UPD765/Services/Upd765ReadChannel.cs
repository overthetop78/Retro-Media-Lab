using RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Enums;
using C = RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Constants.Upd765Constants;

namespace RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Services;

/// <summary>Réception RDATA/WINDOW. Le PLL est externe au µPD765 ; aucune rotation
/// ni récupération d'horloge hôte n'est inventée ici. NEC accepte les deux polarités de WINDOW.</summary>
internal sealed class Upd765ReadChannel
{
    private bool _doubleDensity;
    private bool _windowKnown;
    private bool _window;
    private bool _readData;
    private bool _pulse;
    private bool _aligned;
    private bool _previousData;
    private int _cells;
    private int _a1Run;
    private int _c2Run;
    private uint _history;

    internal event Action<byte, Upd765ReadWordKind, bool>? ByteReceived;

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

    internal void Reset()
    {
        _windowKnown = _window = _readData = _pulse = _aligned = _previousData = false;
        _cells = _a1Run = _c2Run = 0;
        _history = 0;
    }

    internal void SetInputs(bool window, bool readData)
    {
        if (_windowKnown && window != _window)
        {
            ShiftCell(_pulse);
            _pulse = false;
        }
        _windowKnown = true;
        _window = window;
        if (readData && !_readData) _pulse = true;
        _readData = readData;
    }

    private void ShiftCell(bool transition)
    {
        _history = _history << 1 | (transition ? 1U : 0U);
        var word = (ushort)_history;
        if (_doubleDensity)
        {
            var wordBoundary = !_aligned || (_cells & C.LowBit) != 0;
            var zeroPrefix = (_history >> C.EncodedCellsPerByte) == C.MfmZeroWord;
            if ((wordBoundary || zeroPrefix) && word == C.MfmA1Word)
            {
                Synchronize(C.SyncA1, Upd765ReadWordKind.SyncA1);
                return;
            }
            var nextC2 = _c2Run != 0 && _cells == C.EncodedCellsPerByte - 1;
            if (wordBoundary && (zeroPrefix || nextC2) && word == C.MfmC2Word)
            {
                Synchronize(C.SyncC2, Upd765ReadWordKind.SyncC2);
                return;
            }
        }
        else
        {
            var clocks = word & C.ClockLaneMask;
            var data = DecodeData(word);
            if (clocks == C.FmAddressClockLane && IsAddressMark(data))
            {
                Synchronize(data, Upd765ReadWordKind.AddressMark);
                return;
            }
            if (clocks == C.FmIndexClockLane && data == C.IndexAddressMark)
            {
                Synchronize(data, Upd765ReadWordKind.IndexMark);
                return;
            }
        }

        if (!_aligned || ++_cells != C.EncodedCellsPerByte) return;
        _cells = 0;
        var value = DecodeData(word);
        var valid = ValidClocks(word);
        var kind = Upd765ReadWordKind.Data;
        if (_doubleDensity && valid)
        {
            if (_a1Run >= C.MfmSyncWords && IsAddressMark(value)) kind = Upd765ReadWordKind.AddressMark;
            else if (_c2Run >= C.MfmSyncWords && value == C.IndexAddressMark) kind = Upd765ReadWordKind.IndexMark;
        }
        _a1Run = _c2Run = 0;
        ByteReceived?.Invoke(value, kind, valid);
    }

    private void Synchronize(byte value, Upd765ReadWordKind kind)
    {
        var consecutive = _aligned && _cells == C.EncodedCellsPerByte - 1;
        _aligned = true;
        _cells = 0;
        _previousData = (value & C.LowBit) != 0;
        _a1Run = kind == Upd765ReadWordKind.SyncA1 ? (consecutive ? Math.Min(C.MfmSyncWords, _a1Run + 1) : 1) : 0;
        _c2Run = kind == Upd765ReadWordKind.SyncC2 ? (consecutive ? Math.Min(C.MfmSyncWords, _c2Run + 1) : 1) : 0;
        ByteReceived?.Invoke(value, kind, true);
    }

    private bool ValidClocks(ushort word)
    {
        if (!_doubleDensity) return (word & C.ClockLaneMask) == C.ClockLaneMask;
        var data = word & C.DataLaneMask;
        var previous = _previousData ? C.CrcHighBit : 0;
        var expected = ~(data << 1 | data >> 1 | previous) & C.ClockLaneMask;
        _previousData = (data & C.LowBit) != 0;
        return (word & C.ClockLaneMask) == expected;
    }

    private static bool IsAddressMark(byte data) =>
        data is C.IdAddressMark or C.DataAddressMark or C.DeletedAddressMark;

    private static byte DecodeData(int word)
    {
        var data = word & C.DataLaneMask;
        data = (data | data >> 1) & C.PairMergeMask;
        data = (data | data >> 2) & C.NibbleMergeMask;
        return (byte)((data | data >> 4) & C.ByteMask);
    }
}
