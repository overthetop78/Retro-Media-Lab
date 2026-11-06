using RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Contracts;

namespace RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Services;

internal sealed partial class Wd1770WriteChannel
{
    internal Wd1770WriteState CaptureState() => new()
    {
        DoubleDensity = DoubleDensity,
        PrecompensationEnabled = PrecompensationEnabled,
        WriteData = WriteData,
        Word = _word,
        Gate = _gate,
        HalfClocks = _halfClocks,
        RemainingCells = _remainingCells,
        PreviousData = _previousData,
        DataHistory = _dataHistory,
        NextDataBit = _nextDataBit,
        NextCell = _nextCell,
        RisingEdge = _risingEdge,
        FallingEdge = _fallingEdge,
        Crc = _crc,
    };

    internal void RestoreState(Wd1770WriteState state)
    {
        DoubleDensity = state.DoubleDensity;
        PrecompensationEnabled = state.PrecompensationEnabled;
        WriteData = state.WriteData;
        _word = state.Word;
        _gate = state.Gate;
        _halfClocks = state.HalfClocks;
        _remainingCells = state.RemainingCells;
        _previousData = state.PreviousData;
        _dataHistory = state.DataHistory;
        _nextDataBit = state.NextDataBit;
        _nextCell = state.NextCell;
        _risingEdge = state.RisingEdge;
        _fallingEdge = state.FallingEdge;
        _crc = state.Crc;
    }
}