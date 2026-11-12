using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;
using T = RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Constants.Tms9918VramTimingRequirements;

namespace RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Services;

/// <summary>Broches du chemin VRAM, pas un arbitre ni une mémoire. Le séquenceur
/// du VDP fournit ses sorties réellement résolues. RD provient du câblage externe.</summary>
internal sealed class Tms9918VramBus
{
    private Tms9918VramBusState _state;
    private bool _publishing;
    internal Action<Tms9918VramPins>? PinsChanged { get; set; }
    internal Action<Tms9918VramRead>? ReadCaptured { get; set; }

    internal Tms9918VramBus(Tms9918VramBusState initial)
    {
        if (initial.TimeNanoseconds < 0 || initial.InputDataChanged < 0
            || initial.InputDataChanged > initial.TimeNanoseconds || initial.LastRead?.TimeNanoseconds < 0
            || initial.LastRead?.TimeNanoseconds > initial.TimeNanoseconds)
            throw new ArgumentException("Invalid TMS9918 VRAM bus state.");
        _state = initial;
    }

    internal Tms9918VramBusState Capture() => _state;

    internal void SetInputData(long timeNanoseconds, byte data, bool known)
    {
        ValidateInputTime(timeNanoseconds);
        bool changed = data != _state.InputData || known != _state.InputDataKnown;
        _state = _state with { TimeNanoseconds = timeNanoseconds, InputData = data, InputDataKnown = known,
            InputDataChanged = changed ? timeNanoseconds : _state.InputDataChanged };
    }

    internal void ApplyOutputs(long timeNanoseconds, Tms9918VramPins pins)
    {
        ValidateInputTime(timeNanoseconds);
        if (_publishing) throw new InvalidOperationException("Cannot recursively change VRAM sequencing.");
        var previous = _state;
        bool read = !previous.Pins.CasHigh && pins.CasHigh && previous.Pins.ReadWriteHigh;
        Tms9918VramRead? captured = read ? new(timeNanoseconds, previous.InputData,
            previous.InputDataKnown && timeNanoseconds - previous.InputDataChanged >= T.ReadDataSetup) : null;
        _state = previous with { TimeNanoseconds = timeNanoseconds, Pins = pins, LastRead = captured ?? previous.LastRead };
        _publishing = true;
        try
        {
            if (previous.Pins != pins) PinsChanged?.Invoke(pins);
            // La capture est antérieure au callback : le DRAM peut relâcher RD
            // immédiatement à CAS haut (hold 0 ns), sans modifier l'octet échantillonné.
            if (captured is { } value) ReadCaptured?.Invoke(value);
        }
        finally { _publishing = false; }
    }

    private void ValidateInputTime(long timeNanoseconds)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(timeNanoseconds, _state.TimeNanoseconds);
        if (_publishing && timeNanoseconds != _state.TimeNanoseconds)
            throw new InvalidOperationException("Cannot advance VRAM input time from an output callback.");
    }
}
