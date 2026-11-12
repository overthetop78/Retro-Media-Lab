using RetroMediaLab.Chips.Audio.TexasInstruments.SN764xx.SN76489.Contracts;
using C = RetroMediaLab.Chips.Audio.TexasInstruments.SN764xx.SN76489.Constants.Sn76489Constants;

namespace RetroMediaLab.Chips.Audio.TexasInstruments.SN764xx.SN76489.Services;

public sealed partial class Sn76489
{
    /// <summary>Copie indépendante ; ne produit ni horloge ni changement de sortie.</summary>
    public Sn76489State CaptureState() => new()
    {
        FormatVersion = C.StateFormatVersion,
        Periods = (int[])_periods.Clone(),
        Counts = (int[])_counts.Clone(),
        Amplitudes = (int[])_amplitudes.Clone(),
        Levels = (bool[])_levels.Clone(),
        Divider = _divider,
        Selected = _selected,
        Noise = _noise,
        NoisePeriod = _noisePeriod,
        Shift = _shift,
        TransferClocks = _transferClocks,
        BusData = _busData,
        ChipSelected = _chipSelected,
        WriteEnabled = _writeEnabled,
        Output = _output,
        ReadyAssertDelayNanoseconds = _readyAssertDelay,
        ReadyOutput = _readyOutput,
        DirectTransfer = _directTransfer,
        ClockHigh = _clockHigh,
        InputClockCount = _inputClockCount,
        TimeNanoseconds = TimeNanoseconds,
        ReadyDueNanoseconds = _readyDue
    };

    /// <summary>Restaure les phases et le bus sans remplacer le récepteur de sortie.</summary>
    public void RestoreState(Sn76489State state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (_publishing) throw new InvalidOperationException("Cannot restore PSG during an output callback.");
        if (state.FormatVersion != C.StateFormatVersion
            || state.Periods?.Length != C.ToneChannels || state.Counts?.Length != C.ChannelCount
            || state.Amplitudes?.Length != C.ChannelCount || state.Levels?.Length != C.ToneChannels)
            throw new ArgumentException(nameof(state));
        if (state.ReadyAssertDelayNanoseconds != _readyAssertDelay || state.TimeNanoseconds < 0
            || state.InputClockCount < 0 || state.ReadyDueNanoseconds < state.TimeNanoseconds
            || state.Divider is < 0 or >= C.ClockDivider || state.Selected is < 0 or > 7
            || state.Noise is < 0 or > 7 || state.NoisePeriod is < 1 or > 2048
            || state.Shift is < 0 or > 0x1ffff || state.TransferClocks is < 0 or > C.TransferClocks
            || state.Periods.Any(value => value is < 0 or > 1023)
            || state.Counts.Any(value => value is < 0 or > 2048)
            || state.Amplitudes.Any(value => value is < 0 or > 8192))
            throw new ArgumentException(nameof(state));
        bool target = state.TransferClocks == 0 || (!state.ChipSelected && !state.DirectTransfer);
        int output = (state.Levels[0] ? state.Amplitudes[0] : 0)
            + (state.Levels[1] ? state.Amplitudes[1] : 0)
            + (state.Levels[2] ? state.Amplitudes[2] : 0)
            + ((state.Shift & C.NoiseOutputBit) != 0 ? state.Amplitudes[3] : 0);
        if (state.Output != output || (state.ReadyOutput && !target && state.ReadyDueNanoseconds is null)
            || (!state.ReadyOutput && target) || (state.ReadyDueNanoseconds is not null
            && (!state.ReadyOutput || target || _readyAssertDelay == 0)))
            throw new ArgumentException(nameof(state));
        state.Periods.CopyTo(_periods, 0);
        state.Counts.CopyTo(_counts, 0);
        state.Amplitudes.CopyTo(_amplitudes, 0);
        state.Levels.CopyTo(_levels, 0);
        _divider = state.Divider;
        _selected = state.Selected;
        _noise = state.Noise;
        _noisePeriod = state.NoisePeriod;
        _shift = state.Shift;
        _transferClocks = state.TransferClocks;
        _busData = state.BusData;
        _chipSelected = state.ChipSelected;
        _writeEnabled = state.WriteEnabled;
        _output = state.Output;
        _readyOutput = state.ReadyOutput;
        _publishedReadyOutput = state.ReadyOutput;
        _directTransfer = state.DirectTransfer;
        _clockHigh = state.ClockHigh;
        _inputClockCount = state.InputClockCount;
        TimeNanoseconds = state.TimeNanoseconds;
        _readyDue = _announcedReadyDue = state.ReadyDueNanoseconds;
        _hasClockSource = false;
    }
}
