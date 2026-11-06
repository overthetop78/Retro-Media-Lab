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
        Output = _output
    };

    /// <summary>Restaure les phases et le bus sans remplacer le récepteur de sortie.</summary>
    public void RestoreState(Sn76489State state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.FormatVersion != C.StateFormatVersion
            || state.Periods?.Length != C.ToneChannels || state.Counts?.Length != C.ChannelCount
            || state.Amplitudes?.Length != C.ChannelCount || state.Levels?.Length != C.ToneChannels)
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
    }
}
