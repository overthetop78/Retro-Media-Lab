using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;
using T = RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Constants.Tms9918VramTimingRequirements;

namespace RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Services;

/// <summary>Broches du chemin VRAM, pas un arbitre ni une mémoire. Le séquenceur
/// du VDP fournit ses sorties réellement résolues. RD provient du câblage externe.</summary>
internal sealed class Tms9918VramBus
{
    private long _time, _inputChanged;
    private Tms9918VramPins _pins;
    private byte _inputData;
    private bool _inputKnown;
    private Tms9918VramRead? _lastRead;
    private bool _publishing;
    internal Action<Tms9918VramPins>? PinsChanged { get; set; }
    internal Action<Tms9918VramRead>? ReadCaptured { get; set; }

    internal Tms9918VramBus(Tms9918VramBusState initial)
    {
        if (initial.TimeNanoseconds < 0 || initial.InputDataChanged < 0
            || initial.InputDataChanged > initial.TimeNanoseconds || initial.LastRead?.TimeNanoseconds < 0
            || initial.LastRead?.TimeNanoseconds > initial.TimeNanoseconds)
            throw new ArgumentException("Invalid TMS9918 VRAM bus state.");
        _time = initial.TimeNanoseconds;
        _pins = initial.Pins;
        _inputData = initial.InputData;
        _inputKnown = initial.InputDataKnown;
        _inputChanged = initial.InputDataChanged;
        _lastRead = initial.LastRead;
    }

    internal Tms9918VramPins Pins => _pins;
    internal Tms9918VramBusState Capture() => new(_time, _pins, _inputData, _inputKnown, _inputChanged, _lastRead);

    // The owning VDP has already delivered these output phases as a complete
    // read waveform. No pin listener is waiting for their scalar publication.
    // Restore the exact prefix without replaying phases through the clock
    // driver. CAS rising, and therefore the data capture, stays in ApplyOutputs.
    internal void MaterializeReadPrefix(long lastPinDate, Tms9918VramPins pins)
    {
        if (PinsChanged is not null || !pins.ReadWriteHigh || !_pins.ReadWriteHigh
            || !_pins.CasHigh && pins.CasHigh)
            throw new InvalidOperationException("A represented read prefix cannot discard an observed transition or capture.");
        _pins = pins;
        _time = Math.Max(_time, lastPinDate);
    }

    internal void SetInputData(long timeNanoseconds, byte data, bool known, bool intermediateTransition = false)
    {
        ValidateInputTime(timeNanoseconds);
        if (intermediateTransition || data != _inputData || known != _inputKnown) _inputChanged = timeNanoseconds;
        _time = timeNanoseconds;
        _inputData = data;
        _inputKnown = known;
    }

    internal void ApplyOutputs(long timeNanoseconds, Tms9918VramPins pins)
    {
        ValidateInputTime(timeNanoseconds);
        if (_publishing) throw new InvalidOperationException("Cannot recursively change VRAM sequencing.");
        var previous = _pins;
        bool read = !previous.CasHigh && pins.CasHigh && previous.ReadWriteHigh;
        Tms9918VramRead? captured = read ? new(timeNanoseconds, _inputData,
            _inputKnown && timeNanoseconds - _inputChanged >= T.ReadDataSetup) : null;
        _time = timeNanoseconds;
        _pins = pins;
        if (captured is { } valueRead) _lastRead = valueRead;
        _publishing = true;
        try
        {
            if (previous != pins) PinsChanged?.Invoke(pins);
            // La capture est antérieure au callback : le DRAM peut relâcher RD
            // immédiatement à CAS haut (hold 0 ns), sans modifier l'octet échantillonné.
            if (captured is { } value) ReadCaptured?.Invoke(value);
        }
        finally { _publishing = false; }
    }

    private void ValidateInputTime(long timeNanoseconds)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(timeNanoseconds, _time);
        if (_publishing && timeNanoseconds != _time)
            throw new InvalidOperationException("Cannot advance VRAM input time from an output callback.");
    }
}
