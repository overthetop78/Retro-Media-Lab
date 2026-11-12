using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Contracts;
using RetroMediaLab.Chips.Logic.Common.Enums;
using RetroMediaLab.Chips.Memory.Fujitsu.MB81xx.MB8118.Contracts;
using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.SG1000.Services;

internal sealed partial class Sg1000Board
{
    private Z80BusPins _cpuPins;
    private void OnCpuPins(Z80BusPins pins)
    {
        _cpuPins = pins;
        long time = _cpu.TimeNanoseconds;
        ushort address = pins.Address ?? 0;
        byte levels = (byte)(((address >> 14) & 3) | (pins.MemoryRequestHigh == true ? 4 : 0)
            | ((address >> 3) & 24) | (pins.IoRequestHigh == true ? 32 : 0));
        byte known = (byte)((pins.Address.HasValue ? 27 : 0)
            | (pins.MemoryRequestHigh.HasValue ? 4 : 0) | (pins.IoRequestHigh.HasValue ? 32 : 0));
        _decoder.AdvanceTo(time);
        _decoder.SetInputs(levels, known);
        UpdateGateInputs(time);
        UpdateRamBus(time);
        UpdateVdpBus(time);
        UpdatePsgBus(time);
        UpdateJoyBus(time);
        UpdateRomBus(time);
        ResolveCpuData();
    }
    private static bool? Level(LogicPinState pin) => pin == LogicPinState.High ? true : pin == LogicPinState.Low ? false : null;
    private void UpdateGateInputs(long time)
    {
        _gates.AdvanceTo(time);
        _gates.SetInputs(0, _cpuPins.Address is { } address ? (address & 0x8000) != 0 : null, _cpuPins.MemoryRequestHigh);
        _gates.SetInputs(1, Level(_decodePins[6]), _cpuPins.ReadHigh);
        _gates.SetInputs(2, Level(_decodePins[6]), _cpuPins.WriteHigh);
        _gates.SetInputs(3, Level(_decodePins[7]), _cpuPins.ReadHigh);
    }
    private void UpdateRamBus(long time)
    {
        var pins = _cpuPins;
        _ram.ReceivePins(time, new((ushort)((pins.Address ?? 0) & 1023), pins.Address.HasValue,
            _decodePins[3] != LogicPinState.Low, false, pins.ReadHigh != false, pins.WriteHigh != false,
            pins.Data ?? 0, pins.Data.HasValue ? (byte)255 : (byte)0));
    }
    private void UpdateVdpBus(long time) => _vdp.ReceiveCpuPins(time, new(_cpuPins.Data ?? 0,
        _cpuPins.Data.HasValue, ((_cpuPins.Address ?? 0) & 1) != 0,
        _gatePins[1] != LogicPinState.Low, _gatePins[2] != LogicPinState.Low));
    private void UpdatePsgBus(long time) => _psg.SetBus(time, _cpuPins.Data ?? 0,
        _decodePins[5] == LogicPinState.Low, _cpuPins.WriteHigh == false);

    private void UpdateJoyBus(long time)
    {
        byte p0 = (byte)(~_pressed & 63), p1 = (byte)((~_pressed >> 6) & 63);
        byte even = (byte)(p0 | ((p1 & 3) << 6));
        byte odd = (byte)((p1 >> 2) | 0xf0);
        bool? select = _cpuPins.Address is { } address ? (address & 1) != 0 : null;
        bool? disable = Level(_gatePins[3]);
        _joy[0].AdvanceTo(time); _joy[1].AdvanceTo(time);
        _joy[0].SetInputs((byte)(even & 15), 15, (byte)(odd & 15), 15, select, disable);
        _joy[1].SetInputs((byte)(even >> 4), 15, (byte)(odd >> 4), 15, select, disable);
    }
    private void ResolveCpuData()
    {
        byte levels = 255, known = 0, driven = 0;
        if (_cpuPins.Data is { } cpuData) Add(cpuData,255,255);
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
        for (int index = 0; index < 8; index++)
            _dram[index].ReceivePins(signal.TimeNanoseconds, new((byte)(pins.AddressData & 127), pins.AddressDataKnown,
                pins.RasHigh, pins.CasHigh, pins.ReadWriteHigh,
                pins.AddressDataKnown ? (pins.AddressData & (1 << index)) != 0 : null));
    }
    private void UpdateVramReadData(long time)
    {
        byte value = 0;
        bool known = true;
        for (int bit = 0; bit < 8; bit++)
        {
            if (_dramPins[bit] == LogicPinState.High) value |= (byte)(1 << bit);
            else if (_dramPins[bit] != LogicPinState.Low) known = false;
        }
        _vdp.SetVramReadData(time, value, known);
    }
}
