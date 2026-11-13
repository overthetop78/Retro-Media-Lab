using RetroMediaLab.Chips.EmuChip.Common.Contracts;
using RetroMediaLab.Chips.Memory.Toshiba.TMM20xx.TMM2009.Contracts;
using RetroMediaLab.Chips.Memory.Toshiba.TMM20xx.TMM2009.Interfaces;

namespace RetroMediaLab.Chips.EmuChip.Memory.Toshiba.TMM20xx.TMM2009.Services;

/// <summary>Functional 1K×8 model; electrical delays and the chosen CE2 polarity remain uncharacterized.</summary>
public sealed partial class EmuTmm2009(bool ce2ActiveHigh) : ITmm2009
{
    private readonly byte[] _cells = new byte[1024], _known = new byte[1024];
    private ulong _pins = EmuMemoryPins.Encode(0,false,true,true,true,!ce2ActiveHigh);
    private bool _writing, _writeAddressKnown, _publishing;
    private ushort _writeAddress;
    private byte _writeData, _writeKnown;
    private uint _outputBits;
    private long _time;
    public long TimeNanoseconds => _time;
    public bool ElectricalTimingQualified => false;
    public Tmm2009Output Output => new((byte)_outputBits,(byte)(_outputBits>>8),(_outputBits&0x10000)!=0);
    public Action<Tmm2009Output>? OutputChanged { get; set; }
    private bool Selected(ulong pins) => (pins & EmuMemoryPins.CsMask)==0 && ((pins & EmuMemoryPins.Cs2Mask)!=0)==ce2ActiveHigh;
    private static ulong PackPins(Tmm2009Pins p) => EmuMemoryPins.Encode(p.Address,p.AddressKnown,p.Ce1High,p.OutputEnableHigh,p.WriteHigh,p.Ce2High,p.Data,p.DataKnown);
    private static Tmm2009Pins UnpackPins(ulong p) => new((ushort)p,(p&EmuMemoryPins.AddressKnownMask)!=0,(p&EmuMemoryPins.CsMask)!=0,(p&EmuMemoryPins.Cs2Mask)!=0,(p&EmuMemoryPins.OeMask)!=0,(p&EmuMemoryPins.WeMask)!=0,(byte)(p>>24),(byte)(p>>32));

    public void AdvanceTo(long timeNanoseconds)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(timeNanoseconds, _time);
        if (_publishing && timeNanoseconds != _time)
            throw new InvalidOperationException("Cannot advance SRAM during an output callback.");
        _time = timeNanoseconds;
    }

    public void ReceivePins(long timeNanoseconds, Tmm2009Pins pins)
    {
        if (pins.Address > 1023) throw new ArgumentOutOfRangeException(nameof(pins));
        AdvanceTo(timeNanoseconds);
        if (ApplyPackedPins(PackPins(pins))) PublishOutput();
    }

    private bool ApplyPackedPins(ulong pins)
    {
        pins=(pins&~(0xffUL<<24))|(((pins>>24)&(pins>>32)&255)<<24);
        if(pins==_pins)return false;
        bool writing=Selected(pins)&&(pins&EmuMemoryPins.WeMask)==0;
        if(_writing&&!writing&&_writeAddressKnown)
        { _cells[_writeAddress]=_writeData; _known[_writeAddress]=_writeKnown; }
        if(writing)
        {
            bool addressKnown=(pins&EmuMemoryPins.AddressKnownMask)!=0;
            ushort address=(ushort)pins;
            bool stableAddress=!_writing||(_writeAddressKnown&&addressKnown&&_writeAddress==address);
            _writeAddress=address; _writeAddressKnown=addressKnown&&stableAddress;
            _writeData=(byte)(pins>>24); _writeKnown=stableAddress?(byte)(pins>>32):(byte)0;
            _writeData&=_writeKnown;
        }
        _writing=writing; _pins=pins;
        uint value=ResolveOutputBits();
        if(value==_outputBits)return false;
        _outputBits=value; return true;
    }

    private uint ResolveOutputBits()
    {
        if(!Selected(_pins)||(_pins&EmuMemoryPins.OeMask)!=0||(_pins&EmuMemoryPins.WeMask)==0)return 0;
        return (_pins&EmuMemoryPins.AddressKnownMask)!=0
            ? (uint)_cells[(ushort)_pins]|(uint)_known[(ushort)_pins]<<8|0x10000u : 0x10000u;
    }
    private void PublishOutput()
    {
        var value = Output;
        bool publishing = _publishing;
        _publishing = true;
        try { OutputChanged?.Invoke(value); }
        finally { _publishing = publishing; }
    }

    public Tmm2009State CaptureState()
    {
        if (_publishing) throw new InvalidOperationException("Cannot capture SRAM during publication.");
        return new(1, ce2ActiveHigh, _time, (byte[])_cells.Clone(), (byte[])_known.Clone(),
            UnpackPins(_pins), _writing, _writeAddress, _writeAddressKnown, _writeData, _writeKnown);
    }
    public void RestoreState(Tmm2009State state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (_publishing) throw new InvalidOperationException("Cannot restore SRAM during publication.");
        if (state.Version != 1 || state.Ce2ActiveHigh != ce2ActiveHigh || state.Time < 0
            || state.Cells?.Length != 1024 || state.Known?.Length != 1024 || state.Pins.Address > 1023
            || state.WriteAddress > 1023 || state.Writing != (Selected(PackPins(state.Pins)) && !state.Pins.WriteHigh)
            || (state.Pins.Data & ~state.Pins.DataKnown) != 0 || (state.WriteData & ~state.WriteKnown) != 0)
            throw new ArgumentException(nameof(state));
        for (int address = 0; address < 1024; address++)
            if ((state.Cells[address] & ~state.Known[address]) != 0) throw new ArgumentException(nameof(state));
        state.Cells.CopyTo(_cells, 0); state.Known.CopyTo(_known, 0);
        _time = state.Time; _pins = PackPins(state.Pins); _writing = state.Writing;
        _writeAddress = state.WriteAddress; _writeAddressKnown = state.WriteAddressKnown;
        _writeData = state.WriteData; _writeKnown = state.WriteKnown;
        _outputBits = ResolveOutputBits();
    }
}
