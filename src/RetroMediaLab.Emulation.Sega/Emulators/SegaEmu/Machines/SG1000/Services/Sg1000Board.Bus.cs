using RetroMediaLab.Chips.Logic.Common.Enums;
using RetroMediaLab.Chips.Memory.Fujitsu.MB81xx.MB8118.Contracts;
using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.SG1000.Services;

internal sealed partial class Sg1000Board
{
    private bool _vramWiresInitialized;
    private Tms9918VramPins _vramWirePins;
    private byte _vramReadLevels, _vramReadKnown;
    private static bool? Level(LogicPinState pin) => pin == LogicPinState.High ? true : pin == LogicPinState.Low ? false : null;

    private void OnVramPins(Tms9918VramSignal signal)
    {
        var pins = signal.Pins;
        var previous = _vramWirePins;
        byte addressData = pins.AddressData;
        bool known = pins.AddressDataKnown, rasHigh = pins.RasHigh,
            casHigh = pins.CasHigh, writeHigh = pins.ReadWriteHigh;
        int changes = addressData ^ previous.AddressData;
        bool all = !_vramWiresInitialized || (changes & 127) != 0
            || known != previous.AddressDataKnown || rasHigh != previous.RasHigh
            || casHigh != previous.CasHigh || writeHigh != previous.ReadWriteHigh;
        _vramWirePins = pins; _vramWiresInitialized = true;
        byte address = (byte)(addressData & 127);
        for (int index = 0; index < 8; index++)
        {
            if (!all && (changes & (1 << index)) == 0) continue;
            _dramExplicitPinDeliveries++;
            _dram[index].ReceiveMultiplexedPins(signal.TimeNanoseconds, address, known,
                rasHigh, casHigh, writeHigh, known ? (addressData & (1 << index)) != 0 : null);
        }
        PublishVideoInputBoundary();
    }

    private bool ReceiveVramReadCycle(in Tms9918VramReadCycle cycle)
    {
        // The next explicit fallback must deliver all pins: the old per-phase
        // wire cache is intentionally not walked during a represented cycle.
        _vramWiresInitialized = false;
        _vramReadWaveDeliveries++;
        var common = new Mb8118ReadCycle(cycle.RowTime, cycle.RasFall, cycle.CasFall,
            cycle.RasRise, cycle.CasRise, cycle.Row, cycle.Column, cycle.AddressKnown, cycle.AddressKnown, null, null);
        for (int bit = 0; bit < 8; bit++)
            _dram[bit].ReceiveReadCycle(in common,
                cycle.AddressKnown ? (cycle.Row & (1 << bit)) != 0 : null,
                cycle.AddressKnown ? (cycle.Column & (1 << bit)) != 0 : null);
        PublishVideoInputBoundary();
        return true;
    }

    private void CancelVramReadCycle(long time)
    {
        foreach (var memory in _dram) memory.CancelReadCycle(time);
        PublishVideoInputBoundary();
    }
    private void CacheDramOutput(int bit, LogicPinState value)
    {
        byte mask = (byte)(1 << bit);
        _vramReadLevels = (byte)((_vramReadLevels & ~mask) | (value == LogicPinState.High ? mask : 0));
        _vramReadKnown = (byte)((_vramReadKnown & ~mask) | (value is LogicPinState.High or LogicPinState.Low ? mask : 0));
    }
    private void UpdateVramReadData(long time, int bit, LogicPinState value)
    {
        CacheDramOutput(bit, value);
        _vdp.SetVramReadData(time, _vramReadLevels, _vramReadKnown == 255);
    }
}
