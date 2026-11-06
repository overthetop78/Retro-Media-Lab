using RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Contracts;

namespace RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Services;

internal sealed partial class Wd1770ReadChannel
{
    internal Wd1770ReadState CaptureState() => new()
    {
        DoubleDensity = _doubleDensity,
        PreviousReadData = _previousReadData,
        Sampling = _sampling,
        Pulse = _pulse,
        Aligned = _aligned,
        PreviousDataBit = _previousDataBit,
        SampleCountdown = _sampleCountdown,
        WordBits = _wordBits,
        A1Run = _a1Run,
        C2Run = _c2Run,
        Shift = _shift,
        History = _history,
    };

    internal void RestoreState(Wd1770ReadState state)
    {
        _doubleDensity = state.DoubleDensity;
        _previousReadData = state.PreviousReadData;
        _sampling = state.Sampling;
        _pulse = state.Pulse;
        _aligned = state.Aligned;
        _previousDataBit = state.PreviousDataBit;
        _sampleCountdown = state.SampleCountdown;
        _wordBits = state.WordBits;
        _a1Run = state.A1Run;
        _c2Run = state.C2Run;
        _shift = state.Shift;
        _history = state.History;
    }
}