using RetroMediaLab.Chips.Network.Motorola.M6800.Chip6854.Services;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;
using C = RetroMediaLab.Chips.Network.Motorola.M6800.Chip6854.Constants.Mc6854Constants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services;

/// <summary>Câblage BBC de l'ADLC : station, registres répétés et verrou de validation NMI.</summary>
/// <remarks>Le transport est emprunté par ses callbacks ; le cœur conserve sa possession et sa fermeture.</remarks>
internal sealed class Econet
{
    private readonly bool _enabled;
    private readonly byte _station;
    private readonly Mc6854 _controller;
    private bool _nmiEnabled, _driving;
    private bool _clockPresent, _clearToSend = true, _idle = true;
    private bool _controllerIrq;
    private bool _nmi;
    private bool _rts;
    private bool _transmitting;
    internal bool Nmi => _nmi;
    internal Action<bool>? NmiChanged { get; set; }
    internal Action<ReadOnlyMemory<byte>>? FrameTransmitted { get => _controller.FrameTransmitted; set => _controller.FrameTransmitted = value; }

    internal Econet(bool enabled, int station, int cyclesPerByte = C.DefaultCyclesPerByte)
    {
        if (station is < 1 or > 254) throw new ArgumentOutOfRangeException(nameof(station));
        _enabled = enabled; _station = (byte)station; _controller = new(cyclesPerByte);
        _controller.IrqChanged = asserted =>
        {
            _controllerIrq = asserted;
            UpdateNmi();
        };
        _controllerIrq = _controller.Irq;
        _rts = _controller.Rts;
        _transmitting = _controller.Transmitting;
        _controller.TransmitStateChanged = (rts, transmitting) =>
        {
            _rts = rts;
            _transmitting = transmitting;
            UpdateLine();
        };
    }
    internal void Reset() { _nmiEnabled = _driving = false; UpdateNmi(); _controller.Reset(); UpdateLine(); }
    private void UpdateNmi()
    {
        var asserted = _enabled && _nmiEnabled && _controllerIrq;
        if (_nmi == asserted) return;
        _nmi = asserted;
        NmiChanged?.Invoke(asserted);
    }
    internal void Tick()
    {
        if (!_enabled) return;
        _controller.Tick();
    }
    internal void SetLine(bool clockPresent, bool clearToSend, bool idle)
    {
        _clockPresent = clockPresent; _clearToSend = clearToSend; _idle = idle; UpdateLine();
    }
    // La carte BBC renvoie CTS haut lorsque RTS n'active pas son émetteur ; NFS utilise ce retour pour sonder l'horloge.
    private void UpdateLine()
    {
        if (!_enabled) return;
        if (_rts) _driving = true;
        else if (!_transmitting) _driving = false;
        // IC87 détecte l’horloge vers DCD ; CTS est fourni séparément par la logique de collision.
        _controller.SetClockPresent(_clockPresent, _idle);
        _controller.SetCarrierPresent(_clockPresent);
        _controller.SetClearToSend(_clockPresent && _clearToSend && _driving);
    }
    internal bool Receive(ReadOnlySpan<byte> frame, bool validFcs) => _enabled && _controller.ReceiveFrame(frame, validFcs);

    /// <summary>FE18 masque la seule NMI réseau ; la lecture retourne les liens de station.</summary>
    internal byte ReadStation() { DisableNmi(); return _enabled ? _station : (byte)0xfe; }
    internal void DisableNmi() { _nmiEnabled = false; UpdateNmi(); }
    internal void EnableNmi() { if (_enabled) { _nmiEnabled = true; UpdateNmi(); } }
    internal byte Read(ushort address) => _enabled ? _controller.Read(address & 3) : (byte)0xfe;
    internal void Write(ushort address, byte value) { if (_enabled) _controller.Write(address & 3, value); }
}
