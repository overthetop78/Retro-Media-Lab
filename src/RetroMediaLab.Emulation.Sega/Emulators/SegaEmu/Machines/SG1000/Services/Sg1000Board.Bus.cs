using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Contracts;
using RetroMediaLab.Chips.Logic.Common.Enums;
using RetroMediaLab.Chips.Memory.Fujitsu.MB81xx.MB8118.Contracts;
using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.SG1000.Services;

internal sealed partial class Sg1000Board
{
    // Cached levels of the connected wires, decoded once per source notification.
    private ushort? _cpuAddress;
    private byte? _cpuData;
    private bool? _memoryHigh, _ioHigh, _readHigh, _writeHigh;
    private bool _cpuWiresInitialized, _vramWiresInitialized;
    private Tms9918VramPins _vramWirePins;
    private byte _vramReadLevels, _vramReadKnown;

    private void CacheCpuWires(Z80BusPins pins)
    {
        _cpuAddress = pins.Address; _cpuData = pins.Data;
        _memoryHigh = pins.MemoryRequestHigh; _ioHigh = pins.IoRequestHigh;
        _readHigh = pins.ReadHigh; _writeHigh = pins.WriteHigh;
        _cpuWiresInitialized = true;
    }
    private void OnCpuPins(Z80BusPins pins)
    {
        bool initialize = !_cpuWiresInitialized;
        var addressPin = pins.Address;
        var data = pins.Data;
        var memory = pins.MemoryRequestHigh; var io = pins.IoRequestHigh;
        var read = pins.ReadHigh; var write = pins.WriteHigh;
        ushort address = addressPin.GetValueOrDefault();
        int addressChanges = address ^ _cpuAddress.GetValueOrDefault();
        bool addressKnowledge = initialize || addressPin.HasValue != _cpuAddress.HasValue;
        bool addressChanged = addressKnowledge || addressChanges != 0;
        bool dataChanged = initialize || data != _cpuData;
        bool memoryChanged = initialize || memory != _memoryHigh;
        bool ioChanged = initialize || io != _ioHigh;
        bool readChanged = initialize || read != _readHigh;
        bool writeChanged = initialize || write != _writeHigh;
        bool a0Changed = addressKnowledge || (addressChanges & 1) != 0;
        _cpuAddress = addressPin; _cpuData = data;
        _memoryHigh = memory; _ioHigh = io; _readHigh = read; _writeHigh = write;
        _cpuWiresInitialized = true;
        long time = _cpu.TimeNanoseconds;
        if (addressKnowledge || (addressChanges & 0xc0c0) != 0 || memoryChanged || ioChanged)
        {
            byte levels = (byte)(((address >> 14) & 3) | (memory == true ? 4 : 0)
                | ((address >> 3) & 24) | (io == true ? 32 : 0));
            byte known = (byte)((addressPin.HasValue ? 27 : 0)
                | (memory.HasValue ? 4 : 0) | (io.HasValue ? 32 : 0));
            _decoder.AdvanceTo(time); _decoder.SetInputs(levels, known);
        }
        int gates = (addressKnowledge || (addressChanges & 0x8000) != 0 || memoryChanged ? 1 : 0)
            | (readChanged ? 10 : 0) | (writeChanged ? 4 : 0);
        if (gates != 0) UpdateGateInputs(time, gates);
        if (addressKnowledge || (addressChanges & 1023) != 0 || dataChanged || readChanged || writeChanged)
            UpdateRamBus(time);
        else _ram.AdvanceTo(time); // Preserve the existing observation timestamp without redispatching pins.
        if (dataChanged || a0Changed) UpdateVdpBus(time);
        else _vdp.AdvanceCpuInterfaceTo(time);
        if (dataChanged || writeChanged) UpdatePsgBus(time);
        if (a0Changed) UpdateJoyBus(time);
        int memories = addressChanged ? 15 : (memoryChanged ? 7 : 0)
            | (readChanged ? 12 : 0) | (writeChanged || dataChanged ? 8 : 0);
        if (memories != 0) UpdateRomBus(time, memories);
        if (dataChanged) ResolveCpuData();
    }
    private static bool? Level(LogicPinState pin) => pin == LogicPinState.High ? true : pin == LogicPinState.Low ? false : null;
    private void UpdateGateInputs(long time, int gates = 15)
    {
        _gates.AdvanceTo(time);
        if ((gates & 1) != 0) _gates.SetInputs(0, _cpuAddress is { } address ? (address & 0x8000) != 0 : null, _memoryHigh);
        if ((gates & 2) != 0) _gates.SetInputs(1, Level(_decodePins[6]), _readHigh);
        if ((gates & 4) != 0) _gates.SetInputs(2, Level(_decodePins[6]), _writeHigh);
        if ((gates & 8) != 0) _gates.SetInputs(3, Level(_decodePins[7]), _readHigh);
    }
    private void UpdateRamBus(long time)
    {
        _ram.ReceivePins(time, new((ushort)(_cpuAddress.GetValueOrDefault() & 1023), _cpuAddress.HasValue,
            _decodePins[3] != LogicPinState.Low, false, _readHigh != false, _writeHigh != false,
            _cpuData.GetValueOrDefault(), _cpuData.HasValue ? (byte)255 : (byte)0));
    }
    private void UpdateVdpBus(long time) => _vdp.ReceiveCpuPins(time, new(_cpuData.GetValueOrDefault(),
        _cpuData.HasValue, (_cpuAddress.GetValueOrDefault() & 1) != 0,
        _gatePins[1] != LogicPinState.Low, _gatePins[2] != LogicPinState.Low));
    private void UpdatePsgBus(long time) => _psg.SetBus(time, _cpuData.GetValueOrDefault(),
        _decodePins[5] == LogicPinState.Low, _writeHigh == false);

    private void UpdateJoyBus(long time)
    {
        byte p0 = (byte)(~_pressed & 63), p1 = (byte)((~_pressed >> 6) & 63);
        byte even = (byte)(p0 | ((p1 & 3) << 6));
        byte odd = (byte)((p1 >> 2) | 0xf0);
        bool? select = _cpuAddress is { } address ? (address & 1) != 0 : null;
        bool? disable = Level(_gatePins[3]);
        _joy[0].AdvanceTo(time); _joy[1].AdvanceTo(time);
        _joy[0].SetInputs((byte)(even & 15), 15, (byte)(odd & 15), 15, select, disable);
        _joy[1].SetInputs((byte)(even >> 4), 15, (byte)(odd >> 4), 15, select, disable);
    }
    private void ResolveCpuData()
    {
        byte levels = 255, known = 0, driven = 0;
        if (_cpuData is { } cpuData) Add(cpuData,255,255);
        if (_cartridgeLow?.Output is { Driven:true } low) Add(low.Data,low.Known ? (byte)255 : (byte)0,255);
        if (_cartridgeHigh?.Output is { Driven:true } high) Add(high.Data,high.Known ? (byte)255 : (byte)0,255);
        if (_internalRom?.Output is { Driven:true } internalRom) Add(internalRom.Data,internalRom.Known ? (byte)255 : (byte)0,255);
        if (_cartridgeRam?.Output is { Driven:true } cartridgeRam) Add(cartridgeRam.Data,cartridgeRam.Known,255);
        var ram = _ram.Output;
        if (ram.Driven) Add(ram.Data, ram.Known, 255);
        var video = _vdp.CpuOutputs;
        if (video.DataDriven) Add(video.Data, video.DataKnown ? (byte)255 : (byte)0, 255);
        for (int pin = 0; pin < 8; pin++)
        {
            var state = _joyPins[pin];
            if (state == LogicPinState.HighImpedance) continue;
            byte mask = (byte)(1 << pin);
            Add(state == LogicPinState.Low ? (byte)0 : mask,
                state is LogicPinState.High or LogicPinState.Low ? mask : (byte)0, mask);
        }
        DataBusKnown = known == 255;
        _cpu.SetData((byte)(levels | ~known)); // uncharacterized floating bits use an explicit high seed
        void Add(byte data, byte dataKnown, byte mask)
        {
            byte first = (byte)(mask & ~driven), overlap = (byte)(mask & driven);
            byte agree = (byte)(~(levels ^ data) & known & dataKnown & overlap);
            levels = (byte)((levels & ~first) | (data & first));
            known = (byte)((known & ~mask) | (dataKnown & first) | agree);
            driven |= mask;
        }
    }
    private void OnVramPins(Tms9918VramSignal signal)
    {
        var pins = signal.Pins;
        var previous = _vramWirePins;
        bool all = !_vramWiresInitialized || ((pins.AddressData ^ previous.AddressData) & 127) != 0
            || pins.AddressDataKnown != previous.AddressDataKnown || pins.RasHigh != previous.RasHigh
            || pins.CasHigh != previous.CasHigh || pins.ReadWriteHigh != previous.ReadWriteHigh;
        _vramWirePins = pins; _vramWiresInitialized = true;
        for (int index = 0; index < 8; index++)
        {
            if (!all && ((pins.AddressData ^ previous.AddressData) & (1 << index)) == 0) continue;
            _dram[index].ReceivePins(signal.TimeNanoseconds, new((byte)(pins.AddressData & 127), pins.AddressDataKnown,
                pins.RasHigh, pins.CasHigh, pins.ReadWriteHigh,
                pins.AddressDataKnown ? (pins.AddressData & (1 << index)) != 0 : null));
        }
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
