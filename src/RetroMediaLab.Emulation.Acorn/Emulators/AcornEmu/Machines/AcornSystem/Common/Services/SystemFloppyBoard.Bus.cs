using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Constants.SystemFloppyBoardConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Services;

internal sealed partial class SystemFloppyBoard
{
    private ushort _address;
    private byte _busValue;
    private byte _decodePins;
    private bool _busAddressKnown;
    private bool _nRead = true, _nWrite = true;
    private bool? _csHigh, _dackHigh;
    private bool _controllerBusActive;
    internal bool ChipSelected => _csHigh == false;
    internal bool DataAcknowledged => _dackHigh == false;
    internal byte? BusData
    {
        get
        {
            if (_csHigh == false || _dackHigh == false) SynchronizeController();
            return _fdc.BusData;
        }
    }

    private void WireBus()
    {
        _ic4.SetObservedOutputs(2); // /Y1 is the only connected output.
        _ic4.OutputChanged = (_, high) =>
        {
            _csHigh = high;
            _ic2.AdvanceTo(_time);
            _ic2.SetInputA(3, high); // IC2/12, with IC2/13 tied high.
            UpdateControllerBus();
        };
        _ic2.OutputChanged = (gate, high) =>
        {
            switch (gate)
            {
                case 0:
                    if (high.HasValue && !_disposed) { SynchronizeController(); _fdc.SetReset(high.Value); }
                    break;
                case 1: if (high.HasValue) IndexClockChanged(high.Value); break;
                case 2: _dackHigh = high; UpdateControllerBus(); break;
                case 3: _ic2.SetInputB(2, high); break; // IC2/11 -> IC2/10.
            }
        };
    }

    private void InitializeBus()
    {
        _ic2.SetInputs(0, true, true); // NRST high, constant+5 V on pin1.
        _ic2.SetInputB(3, true); // +5 V on pin13, invert /CS.
        SetBusAddress(0);
        UpdateControllerBus();
    }

    internal void SetBusAddress(ushort address, byte writeValue = 0)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var changed = _address ^ address;
        var first = !_busAddressKnown;
        if (!first && changed == 0 && _busValue == writeValue) return;
        _address = address; _busValue = writeValue;
        var pins = (byte)(((address >> 9) & 3) | ((address >> 6) & 4)
            | ((address >> 8) & 8) | ((address >> 3) & 16)
            | ((address & C.CpuPageMask) != 0 ? 32 : 0));
        // Received wire levels, not a second decoder: initialize all pins once,
        // then notify only actual input changes. Propagation remains in the ICs.
        _busAddressKnown = true;
        if (first || _decodePins != pins)
        {
            _decodePins = pins;
            _ic4.AdvanceTo(_time);
            _ic4.SetInputs(pins, C.AllPinsKnown);
        }
        if (first || (changed & 4) != 0)
        {
            _ic2.AdvanceTo(_time);
            _ic2.SetInputA(2, (address & 4) != 0); // IC2/9 <- A2.
        }
        UpdateControllerBus();
    }

    internal void SetBusStrobes(bool nRead, bool nWrite)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_nRead == nRead && _nWrite == nWrite) return;
        _nRead = nRead; _nWrite = nWrite;
        UpdateControllerBus();
    }

    internal void SetResetLine(bool nReset)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _ic2.AdvanceTo(_time);
        _ic2.SetInputB(0, nReset);
    }

    private void UpdateControllerBus()
    {
        if (!_disposed)
        {
            var active = _csHigh == false || _dackHigh == false;
            // No host-bus transaction reaches a deselected8271. The card already
            // retains all received pins and presents them together on selection.
            // Always deliver the selected->deselected edge to release the bus.
            if (!active && !_controllerBusActive) return;
            if (active || _controllerBusActive) SynchronizeController();
            _controllerBusActive = active;
            _fdc.SetBus(_csHigh == false, !_nRead, !_nWrite, _dackHigh == false, _address & 3, _busValue);
        }
    }
}
