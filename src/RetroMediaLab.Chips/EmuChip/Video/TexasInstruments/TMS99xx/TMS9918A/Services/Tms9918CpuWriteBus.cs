using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;
using T = RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Constants.Tms9918CpuTimingRequirements;

namespace RetroMediaLab.Chips.EmuChip.Video.TexasInstruments.TMS99xx.TMS9918A.Services;

/// <summary>Récepteur interne de l'impulsion CPU, TI figure 5-3. N'invente aucune
/// horloge ni délai de service VRAM ; les pins sont reçues par changements datés.</summary>
internal sealed class Tms9918CpuWriteBus
{
    private Tms9918CpuWriteState _state;
    private long _observationTime;
    private bool _publishing;
    internal Action<Tms9918CpuWrite>? WriteCaptured { get; set; }

    internal Tms9918CpuWriteBus(Tms9918CpuPins initial, long timeNanoseconds)
        : this(new(timeNanoseconds, initial, initial.Mode, false, timeNanoseconds,
            timeNanoseconds, timeNanoseconds, null, false)) { }

    internal Tms9918CpuWriteBus(Tms9918CpuWriteState state)
    {
        if (state.TimeNanoseconds < 0 || state.WriteStart < 0 || state.ModeChanged < 0 || state.DataChanged < 0
            || state.WriteStart > state.TimeNanoseconds || state.ModeChanged > state.TimeNanoseconds
            || state.DataChanged > state.TimeNanoseconds || state.LastCapture < 0
            || state.LastCapture > state.TimeNanoseconds)
            throw new ArgumentException("Invalid TMS9918 CPU write state.");
        _state = state;
        _observationTime = state.TimeNanoseconds;
    }

    internal Tms9918CpuWriteState Capture() => _state with { TimeNanoseconds = _observationTime };

    internal void AdvanceInputTime(long timeNanoseconds)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(timeNanoseconds, _observationTime);
        if (_publishing && timeNanoseconds != _observationTime)
            throw new InvalidOperationException("Cannot advance input time from a CPU capture callback.");
        _observationTime = timeNanoseconds;
    }

    internal void ReceivePins(long timeNanoseconds, Tms9918CpuPins pins)
    {
        AdvanceInputTime(timeNanoseconds);
        var previous = _state;
        bool modeChanged = previous.Pins.Mode != pins.Mode;
        bool dataChanged = previous.Pins.Data != pins.Data || previous.Pins.DataKnown != pins.DataKnown;
        bool modeQualified = previous.ModeQualified;
        bool qualificationLost = previous.QualificationLost;
        if (modeChanged && !previous.Pins.CswHigh && timeNanoseconds - previous.WriteStart < T.ModeHold)
        { modeQualified = false; qualificationLost = true; }
        if (dataChanged && previous.LastCapture is { } last && timeNanoseconds - last < T.DataHold)
            qualificationLost = true;

        long modeChange = modeChanged ? timeNanoseconds : previous.ModeChanged;
        long dataChange = dataChanged ? timeNanoseconds : previous.DataChanged;
        bool falling = previous.Pins.CswHigh && !pins.CswHigh;
        bool rising = !previous.Pins.CswHigh && pins.CswHigh;
        bool writeMode = previous.WriteMode;
        long writeStart = previous.WriteStart;
        if (falling)
        {
            writeStart = timeNanoseconds; writeMode = pins.Mode;
            modeQualified = timeNanoseconds - modeChange >= T.ModeSetup;
            qualificationLost |= !modeQualified;
        }
        if (!pins.CswHigh && !pins.CsrHigh)
        { modeQualified = false; qualificationLost = true; }
        bool qualified = modeQualified && pins.CsrHigh && previous.Pins.CsrHigh && pins.DataKnown
            && timeNanoseconds - dataChange >= T.DataSetup
            && timeNanoseconds - writeStart >= T.WriteLow;
        if (rising && !qualified) qualificationLost = true;
        _state = new(timeNanoseconds, pins, writeMode, modeQualified, writeStart, modeChange, dataChange,
            rising ? timeNanoseconds : previous.LastCapture, qualificationLost);

        // État canonique mis à jour avant callback : un retour de câblage à la
        // même date ne sera ni écrasé ni rejoué par cette capture.
        if (rising && WriteCaptured is { } listener)
        {
            bool wasPublishing = _publishing;
            _publishing = true;
            try { listener(new(timeNanoseconds, writeMode, pins.Data, qualified)); }
            finally { _publishing = wasPublishing; }
        }
    }
}
