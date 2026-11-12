using RetroMediaLab.Chips.Memory.Toshiba.TMM20xx.TMM2009.Contracts;
using RetroMediaLab.Chips.Memory.Toshiba.TMM20xx.TMM2009.Interfaces;

namespace RetroMediaLab.Chips.Memory.Toshiba.TMM20xx.TMM2009.Services;

/// <summary>Functional 1K×8 model; electrical delays and the chosen CE2 polarity remain uncharacterized.</summary>
public sealed partial class Tmm2009(bool ce2ActiveHigh) : ITmm2009
{
    private readonly byte[] _cells = new byte[1024], _known = new byte[1024];
    private Tmm2009Pins _pins = new(0, false, true, !ce2ActiveHigh, true, true, 0, 0);
    private bool _writing, _writeAddressKnown, _publishing;
    private ushort _writeAddress;
    private byte _writeData, _writeKnown;
    private Tmm2009Output _output;
    public long TimeNanoseconds { get; private set; }
    public bool ElectricalTimingQualified => false;
    public Tmm2009Output Output => _output;
    public Action<Tmm2009Output>? OutputChanged { get; set; }
    private bool Selected(Tmm2009Pins pins) => !pins.Ce1High && pins.Ce2High == ce2ActiveHigh;

    public void AdvanceTo(long timeNanoseconds)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(timeNanoseconds, TimeNanoseconds);
        if (_publishing && timeNanoseconds != TimeNanoseconds)
            throw new InvalidOperationException("Cannot advance SRAM during an output callback.");
        TimeNanoseconds = timeNanoseconds;
    }

    public void ReceivePins(long timeNanoseconds, Tmm2009Pins pins)
    {
        if (pins.Address > 1023) throw new ArgumentOutOfRangeException(nameof(pins));
        AdvanceTo(timeNanoseconds);
        if (ApplyPins(pins)) PublishOutput();
    }

    private bool ApplyPins(Tmm2009Pins pins)
    {
        pins = pins with { Data = (byte)(pins.Data & pins.DataKnown) };
        if (pins == _pins) return false;
        bool writing = Selected(pins) && !pins.WriteHigh;
        if (_writing && !writing && _writeAddressKnown)
        {
            _cells[_writeAddress] = _writeData;
            _known[_writeAddress] = _writeKnown;
        }
        if (writing)
        {
            // Moving the address within an active write is outside the characterized model.
            // Do not quietly convert such a waveform to a sequence of valid writes.
            bool stableAddress = !_writing || (_writeAddressKnown && pins.AddressKnown && _writeAddress == pins.Address);
            _writeAddress = pins.Address;
            _writeAddressKnown = pins.AddressKnown && stableAddress;
            _writeData = pins.Data;
            _writeKnown = stableAddress ? pins.DataKnown : (byte)0;
            _writeData &= _writeKnown;
        }
        _writing = writing;
        _pins = pins;
        var value = ResolveOutput();
        if (value == _output) return false;
        _output = value;
        return true;
    }

    private Tmm2009Output ResolveOutput()
    {
        if (!Selected(_pins) || _pins.OutputEnableHigh || !_pins.WriteHigh) return default;
        return _pins.AddressKnown ? new(_cells[_pins.Address], _known[_pins.Address], true) : new(0, 0, true);
    }
    private void PublishOutput()
    {
        var value = _output;
        bool publishing = _publishing;
        _publishing = true;
        try { OutputChanged?.Invoke(value); }
        finally { _publishing = publishing; }
    }

    public Tmm2009State CaptureState()
    {
        if (_publishing) throw new InvalidOperationException("Cannot capture SRAM during publication.");
        return new(1, ce2ActiveHigh, TimeNanoseconds, (byte[])_cells.Clone(), (byte[])_known.Clone(),
            _pins, _writing, _writeAddress, _writeAddressKnown, _writeData, _writeKnown);
    }
    public void RestoreState(Tmm2009State state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (_publishing) throw new InvalidOperationException("Cannot restore SRAM during publication.");
        if (state.Version != 1 || state.Ce2ActiveHigh != ce2ActiveHigh || state.Time < 0
            || state.Cells?.Length != 1024 || state.Known?.Length != 1024 || state.Pins.Address > 1023
            || state.WriteAddress > 1023 || state.Writing != (Selected(state.Pins) && !state.Pins.WriteHigh)
            || (state.Pins.Data & ~state.Pins.DataKnown) != 0 || (state.WriteData & ~state.WriteKnown) != 0)
            throw new ArgumentException(nameof(state));
        for (int address = 0; address < 1024; address++)
            if ((state.Cells[address] & ~state.Known[address]) != 0) throw new ArgumentException(nameof(state));
        state.Cells.CopyTo(_cells, 0); state.Known.CopyTo(_known, 0);
        TimeNanoseconds = state.Time; _pins = state.Pins; _writing = state.Writing;
        _writeAddress = state.WriteAddress; _writeAddressKnown = state.WriteAddressKnown;
        _writeData = state.WriteData; _writeKnown = state.WriteKnown;
        _output = ResolveOutput();
    }
}
