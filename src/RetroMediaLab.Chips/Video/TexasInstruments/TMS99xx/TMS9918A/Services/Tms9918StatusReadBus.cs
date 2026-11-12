using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;

namespace RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Services;

/// <summary>Acquittement provisoire à CSR montant, autorisé le 12 novembre 2026.
/// Ne modélise pas les délais de pilotage CD ni les lectures de données VRAM.</summary>
internal sealed class Tms9918StatusReadBus
{
    private readonly Tms9918Status _status;
    private readonly Tms9918CpuProtocol _protocol;
    private Tms9918StatusReadState _state;
    private bool _publishing;
    internal Action<long>? StatusAcknowledged { get; set; }

    internal Tms9918StatusReadBus(Tms9918StatusReadState initial,
        Tms9918Status status, Tms9918CpuProtocol protocol)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(initial.TimeNanoseconds);
        ArgumentNullException.ThrowIfNull(status);
        ArgumentNullException.ThrowIfNull(protocol);
        if (initial.Pins.CsrHigh && initial.StatusSelected)
            throw new ArgumentException("A status read cannot be selected with CSR high.");
        _state = initial;
        _status = status;
        _protocol = protocol;
    }

    internal Tms9918StatusReadState Capture() => _state;

    internal void ReceivePins(long timeNanoseconds, Tms9918CpuPins pins)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(timeNanoseconds, _state.TimeNanoseconds);
        if (_publishing && timeNanoseconds != _state.TimeNanoseconds)
            throw new InvalidOperationException("Cannot advance input time from a status callback.");
        var previous = _state;
        bool selected = previous.StatusSelected;
        bool qualified = previous.SelectionQualified;
        if (previous.Pins.CsrHigh && !pins.CsrHigh)
        {
            selected = pins.Mode;
            qualified = pins.CswHigh;
        }
        else if (!previous.Pins.CsrHigh)
        {
            // Hors protocole documenté : conserver la qualification, sans
            // choisir un résultat du silicium pour un changement de sélection.
            qualified &= pins.Mode == previous.Pins.Mode && pins.CswHigh;
        }
        bool accepted = !previous.Pins.CsrHigh && pins.CsrHigh && selected && qualified;
        _state = new(timeNanoseconds, pins, pins.CsrHigh ? false : selected, qualified);
        if (!accepted) return;

        _status.ReadAccepted();
        _protocol.StatusReadAccepted();
        bool wasPublishing = _publishing;
        _publishing = true;
        try { StatusAcknowledged?.Invoke(timeNanoseconds); }
        finally { _publishing = wasPublishing; }
    }
}
