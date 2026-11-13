using RetroMediaLab.Chips.EmuChip.Common.Contracts;
using System.Runtime.InteropServices;
using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Enums;
using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Contracts;
using RetroMediaLab.Chips.Logic.Common.Contracts;
using RetroMediaLab.Chips.Logic.Common.Enums;
using RetroMediaLab.Chips.Memory.Common.Contracts;
using RetroMediaLab.Chips.Memory.Toshiba.TMM20xx.TMM2009.Contracts;
using RetroMediaLab.Chips.Memory.Toshiba.TMM20xx.TMM2016.Contracts;
using RetroMediaLab.Chips.Memory.Toshiba.TMM23xx.TMM23256.Contracts;
using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.SG1000.Enums;
using C = RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.SG1000.Constants.Sg1000Constants;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.SG1000.Services;

internal sealed partial class Sg1000Board
{
    private enum BusRecipient { Decoder, Gates, Ram, RomLow, RomHigh, InternalRom, CartridgeRam, JoyLow, JoyHigh, Video, Sound }
    private const uint GateConsumers = (1u << (int)BusRecipient.RomLow) | (1u << (int)BusRecipient.JoyLow)
        | (1u << (int)BusRecipient.JoyHigh) | (1u << (int)BusRecipient.Video);
    private const uint DirectConsumers = (1u << (int)BusRecipient.Ram) | (1u << (int)BusRecipient.RomHigh)
        | (1u << (int)BusRecipient.InternalRom) | (1u << (int)BusRecipient.CartridgeRam) | (1u << (int)BusRecipient.Sound);
    private readonly struct CpuBusWires
    {
        public readonly ushort AddressValue;
        public readonly byte DataValue, DataKnowledge, DecoderLevels, DecoderKnown, GateCpuLevels, GateCpuKnown;
        public readonly byte Controls, ControlKnown;
        public readonly bool AddressKnown, DataIsKnown;
        public readonly ulong MemoryBits;
        public bool MemoryInactive => (ControlKnown & 1) == 0 || (Controls & 1) != 0;
        public bool ReadInactive => (ControlKnown & 4) == 0 || (Controls & 4) != 0;
        public bool WriteInactive => (ControlKnown & 8) == 0 || (Controls & 8) != 0;

        public CpuBusWires(ulong pins)
        {
            AddressValue = (ushort)pins; AddressKnown = (pins & (1UL<<32)) != 0;
            DataValue = (byte)(pins>>16); DataIsKnown = (pins & (1UL<<33)) != 0;
            DataKnowledge = DataIsKnown ? (byte)255 : (byte)0;
            MemoryBits=(ulong)AddressValue|(AddressKnown?EmuMemoryPins.AddressKnownMask:0)
                |((ulong)(DataValue&DataKnowledge)<<24)|((ulong)DataKnowledge<<32);
            ControlKnown = (byte)((pins>>34)&15);
            Controls = (byte)((pins>>24) & ControlKnown);
            DecoderLevels = (byte)(((AddressValue >> 14) & 3) | ((Controls & 1) << 2)
                | ((AddressValue >> 3) & 24) | ((Controls & 2) << 4));
            DecoderKnown = (byte)((AddressKnown ? 27 : 0) | ((ControlKnown & 1) << 2)
                | ((ControlKnown & 2) << 4));
            GateCpuLevels = (byte)(((AddressValue & 0x8000) >> 15) | ((Controls & 1) << 1)
                | ((Controls & 4) << 1) | ((Controls & 4) << 5) | ((Controls & 8) << 2));
            GateCpuKnown = (byte)((AddressKnown ? 1 : 0) | ((ControlKnown & 1) << 1)
                | ((ControlKnown & 4) << 1) | ((ControlKnown & 4) << 5) | ((ControlKnown & 8) << 2));
        }
    }
    private readonly record struct SoundWireChange(long Time, byte Data, bool Selected, bool Write);
    private readonly record struct DataWire(byte Data, byte Known, byte Driven);
    // Only cross-domain video/audio words survive the current source prefix.
    // Logic and memory inputs are delivered along their fixed connections.
    private const int MaximumBusHistory = 16_384;
    private readonly ulong[] _logicLevels = new ulong[4], _logicKnown = new ulong[4];
    private readonly List<EmuSignalWord> _videoWireChanges = new(16);
    private readonly List<SoundWireChange> _soundWireChanges = new(16);
    private readonly ulong[] _romWirePins = new ulong[3];
    private ulong _ramWirePins;
    private ulong _cartridgeRamWirePins;
    private long _ramObservation, _videoObservation;
    private ushort _rangePressed;
    private CpuBusWires _wiredCpu;
    private readonly long[] _logicResponseDue = new long[4], _memoryResponseDue = new long[4];
    private readonly ushort[] _logicOutputWords = new ushort[4];

    private readonly DataWire[] _busMemoryDrivers = new DataWire[5]; // low/high/internal/cart SRAM/main SRAM
    private bool _initializeBusInputs, _executingCpuRange, _cpuBusDirty = true;
    private uint _connectedBusRecipients, _wiredMemoryTouched, _wiredLogicTouched;
    private byte? _busCpuData;
    private readonly DataWire[] _busJoyDrivers = new DataWire[2];
    private long _cpuBusResolutions;
    internal long CpuBusResolutions => _cpuBusResolutions;
    private long _memoryResponseRecordsOmitted;
    internal long MemoryResponseRecordsOmitted => _memoryResponseRecordsOmitted;
    private long _directCauseVisits, _gateCauseVisits;
    internal long DirectCauseVisits => _directCauseVisits;
    internal long GateCauseVisits => _gateCauseVisits;
    internal long GateSkippedCpuWords => 0;
    private EmuSignalRangeHandler? _busRangeReceiver;
    private ulong _busVideoPins;
    private (byte Data, bool Selected, bool Write) _busSoundPins;
    private DataWire _busVideoDriver;
    private long _busRangeDeliveries;
    internal long BusRangeDeliveries => _busRangeDeliveries;
    private long _cpuOutputWords;
    internal long CpuOutputWords => _cpuOutputWords;
    private long _logicInputWords;
    internal long LogicInputWords => _logicInputWords;
    private long _memoryInputWords;
    internal long MemoryInputWords => _memoryInputWords;
    private long _vramReadWaveDeliveries;
    internal long VramReadWaveDeliveries => _vramReadWaveDeliveries;
    private long _dramExplicitPinDeliveries;
    internal long DramExplicitPinDeliveries => _dramExplicitPinDeliveries;
    private long _videoSynchronizationCalls;
    internal long VideoSynchronizationCalls => _videoSynchronizationCalls;
    internal long NativeVideoSamples => _vdp.RasterSamplesProduced;
    internal long InactiveCpuVideoPrograms => _vdp.InactiveCpuPrograms;
    internal long InactiveCpuVideoWords => _vdp.InactiveCpuProgramWords;
    internal long RasterProgramRuns => _vdp.RasterProgramRuns;
    internal long RasterProgramPixels => _vdp.RasterProgramPixels;
    internal long ConstantRasterRuns => _vdp.ConstantRasterRuns;
    internal long ConstantRasterPixels => _vdp.ConstantRasterPixels;
    internal long RasterDirectWrites => _vdp.RasterDirectWrites;
    internal long RasterScalarDeliveries => _vdp.RasterScalarDeliveries;
    internal long VideoSignalMaterializations => _vdp.VideoSignalMaterializations;
    private long _dramOutputNotifications, _vramDataDeliveries, _vramDataGroups;
    internal long DramReadOutputPrograms { get; private set; }
    internal long VramReadProgramWords { get; private set; }
    internal long DramReadOutputFallbacks { get; private set; }
    internal long ClosedDramReadResolutions => _dram.Sum(chip => chip.ClosedReadResolutions);
    internal long ActiveCyclePortions => _cpu.ActiveCyclePortions;
    internal long ActiveCycleHalfEdges => _cpu.ActiveCycleHalfEdges;
    internal long QuietCycleHalfEdges => _cpu.QuietCycleHalfEdges;
    internal long ReadPortBytePrograms => _dramReadPort?.ByteReadPrograms ?? 0;
    internal long ReadPortLaneRecordsEliminated => _dramReadPort?.LaneOutputRecordsEliminated ?? 0;
    internal long ReadPortProjectedReads => _dramReadPort?.ProjectedReads ?? 0;
    internal long ReadPortCanonicalReceives => _dramReadPort?.CanonicalReceives ?? 0;
    internal long ReadPortEpochs => _dramReadPort?.Epochs ?? 0;
    internal long DramOutputNotifications => _dramOutputNotifications;
    internal long VramDataDeliveries => _vramDataDeliveries;
    internal long VramDataGroups => _vramDataGroups;

    private static void StoreBusWord<T>(List<T> buffer, T word)
    {
        if (buffer.Count == MaximumBusHistory) throw new InvalidOperationException("Electrical prefix exceeds its transport bound.");
        buffer.Add(word);
    }

    private void ConnectRangeBus()
    {
        _cpu.PinsChanged = null; _cpu.NextEventNanosecondsChanged = null;
        _vdp.VramPinsChanged = null;
        _vdp.UnrepresentedVramPinsChanged = OnVramPins;
        _vdp.VramReadCycleProduced = ReceiveVramReadCycle;
        _vdp.VramReadCycleCancelled = CancelVramReadCycle;
        _decoder.OutputChanged = null; _decoder.NextEventNanosecondsChanged = null;
        _gates.OutputChanged = null; _gates.NextEventNanosecondsChanged = null;
        _ram.OutputChanged = null;
        foreach (var joy in _joy) { joy.OutputChanged = null; joy.NextEventNanosecondsChanged = null; }
        if (_cartridgeLow is { } low) { low.OutputChanged = null; low.NextEventNanosecondsChanged = null; }
        if (_cartridgeHigh is { } high) { high.OutputChanged = null; high.NextEventNanosecondsChanged = null; }
        if (_internalRom is { } rom) { rom.OutputChanged = null; rom.NextEventNanosecondsChanged = null; }
        if (_cartridgeRam is { } ram) { ram.OutputChanged = null; ram.NextEventNanosecondsChanged = null; }
        _busRangeReceiver ??= ReceiveCpuBusRange;
        // Rebind transport from canonical package states. Initial physical
        // levels are delivered separately at power-on and slot replacement;
        // restoring a snapshot must not synthesize another input transition.
        _busMemoryDrivers[0] = RomDriver(_cartridgeLow?.Output ?? default);
        _busMemoryDrivers[1] = RomDriver(_cartridgeHigh?.Output ?? default);
        _busMemoryDrivers[2] = RomDriver(_internalRom?.Output ?? default);
        var cartRam = _cartridgeRam?.Output ?? default;
        _busMemoryDrivers[3] = new(cartRam.Data, cartRam.Known, cartRam.Driven ? (byte)255 : (byte)0);
        var mainRam = _ram.Output;
        _busMemoryDrivers[4] = new(mainRam.Data, mainRam.Known, mainRam.Driven ? (byte)255 : (byte)0);
        var video = _vdp.CpuOutputs;
        _busVideoDriver = new(video.Data, video.DataKnown ? (byte)255 : (byte)0, video.DataDriven ? (byte)255 : (byte)0);
        var logicStates = new[] { _decoder.CaptureState(), _gates.CaptureState(), _joy[0].CaptureState(), _joy[1].CaptureState() };
        for (int i = 0; i < logicStates.Length; i++)
        {
            _logicLevels[i] = logicStates[i].InputLevels; _logicKnown[i] = logicStates[i].InputKnown;
            _logicOutputWords[i] = logicStates[i].Outputs;
            _logicResponseDue[i] = logicStates[i].DueNanoseconds.Where(value => value.HasValue).Select(value => value!.Value).DefaultIfEmpty(long.MaxValue).Min();
        }
        _memoryResponseDue[0] = _cartridgeLow?.NextEventNanoseconds ?? long.MaxValue;
        _memoryResponseDue[1] = _cartridgeHigh?.NextEventNanoseconds ?? long.MaxValue;
        _memoryResponseDue[2] = _internalRom?.NextEventNanoseconds ?? long.MaxValue;
        _memoryResponseDue[3] = _cartridgeRam?.NextEventNanoseconds ?? long.MaxValue;
        _ramWirePins = PackMainRam(_ram.CaptureState().Pins);
        _ramObservation = _ram.TimeNanoseconds;
        _cartridgeRamWirePins = PackCartridgeRam(_cartridgeRam?.CaptureState().Pins ?? default);
        _romWirePins[0] = PackRom(_cartridgeLow?.CaptureState().Pins ?? default);
        _romWirePins[1] = PackRom(_cartridgeHigh?.CaptureState().Pins ?? default);
        _romWirePins[2] = PackRom(_internalRom?.CaptureState().Pins ?? default);
        _busVideoPins = RetroMediaLab.Chips.EmuChip.Video.TexasInstruments.TMS99xx.TMS9918A.Services.EmuTms9918A.PackCpuInput(_vdp.CaptureState().CpuInterface.Pins);
        var sound = _psg.CaptureState();
        _busSoundPins = (sound.BusData, sound.ChipSelected, sound.WriteEnabled);
        _busCpuData = _cpu.Pins.Data;
        SetJoyDriver(0, _logicOutputWords[2]);
        SetJoyDriver(1, _logicOutputWords[3]);
        _connectedBusRecipients = Bit(BusRecipient.Decoder) | Bit(BusRecipient.Gates) | Bit(BusRecipient.Ram)
            | Bit(BusRecipient.JoyLow) | Bit(BusRecipient.JoyHigh) | Bit(BusRecipient.Video) | Bit(BusRecipient.Sound);
        if (_cartridgeLow is not null) _connectedBusRecipients |= Bit(BusRecipient.RomLow);
        if (_cartridgeHigh is not null) _connectedBusRecipients |= Bit(BusRecipient.RomHigh);
        if (_internalRom is not null) _connectedBusRecipients |= Bit(BusRecipient.InternalRom);
        if (_cartridgeRam is not null) _connectedBusRecipients |= Bit(BusRecipient.CartridgeRam);
        _rangePressed = _pressed;
        _cpuBusDirty = true;
    }

    private void InitializeBusPins()
    {
        _initializeBusInputs = true;
        DeliverExternalBusInputs();
    }

    private void DeliverExternalBusInputs()
    {
        var boundary = new EmuSignalRange(TimeNanoseconds, TimeNanoseconds, _cpu.PackedPins, []);
        ReceiveCpuBusRange(in boundary);
    }
    private static LogicPinState Logic(ushort word, int pin) => (LogicPinState)((word >> (pin * 2)) & 3);
    private static DataWire RomDriver(Tmm23256Output output)
        => new(output.Data, output.Known ? (byte)255 : (byte)0, output.Driven ? (byte)255 : (byte)0);

    private void ReceiveCpuBusRange(in EmuSignalRange source)
    {
        var changes = source.Words;
        long start = source.Start, end = source.End;
        var initialPins = source.Initial;
        _busRangeDeliveries++;
        _cpuOutputWords += changes.Length;
        _wiredMemoryTouched = _wiredLogicTouched = 0;
        _videoWireChanges.Clear(); _soundWireChanges.Clear(); _videoObservation = -1;
        if (!changes.IsEmpty)
        {
            _ramObservation = Math.Max(_ramObservation, changes[^1].Time);
            _videoObservation = changes[^1].Time;
        }
        _wiredCpu = new(initialPins);
        ResolveWiredLogicUntil(start);
        if (_initializeBusInputs)
        {
            _initializeBusInputs = false;
            DispatchWiredRecipients(_connectedBusRecipients, start);
        }
        if (_rangePressed != _pressed)
        {
            DispatchWiredRecipients(Bit(BusRecipient.JoyLow) | Bit(BusRecipient.JoyHigh), start);
            _rangePressed = _pressed;
        }
        foreach (ref readonly var change in changes)
        {
            // Each source cause is visited once. Propagations already due at
            // this date precede its new input, including gate/decoder ties.
            ResolveWiredLogicUntil(change.Time);
            var wires = new CpuBusWires(change.Bits);
            uint recipients = ChangedRecipients(_wiredCpu, wires);
            _wiredCpu = wires;
            _directCauseVisits++;
            DispatchWiredRecipients(recipients, change.Time);
        }
        ResolveWiredLogicUntil(end);
        _ram.AdvanceTo(_ramObservation);
        if (_cartridgeLow is not null) FinishWiredRom(0, end);
        if (_cartridgeHigh is not null) FinishWiredRom(1, end);
        if (_internalRom is not null) FinishWiredRom(2, end);
        if (_cartridgeRam is not null && ((_wiredMemoryTouched & 8) != 0 || _memoryResponseDue[3] <= end - _cartridgeOrigin))
            AcceptWiredMemory(3, _cartridgeRam.AdvanceWiredTime(end - _cartridgeOrigin));
        ResolveWiredJoyUntil(0, end);
        ResolveWiredJoyUntil(1, end);
        // Materialize the same owner times as a completed input portion,
        // without replaying its inputs or scanning disconnected receivers.
        if ((_wiredLogicTouched & 1) != 0) _decoder.AdvanceTo(end);
        if ((_wiredLogicTouched & 2) != 0) _gates.AdvanceTo(end);
        if ((_wiredLogicTouched & 4) != 0) _joy[0].AdvanceTo(end);
        if ((_wiredLogicTouched & 8) != 0) _joy[1].AdvanceTo(end);
        var finalCpu = changes.IsEmpty ? initialPins : changes[^1].Bits;
        byte? finalData=(finalCpu&(1UL<<33))!=0?(byte)(finalCpu>>16):null;
        if (_busCpuData != finalData) { _busCpuData = finalData; _cpuBusDirty = true; }
        var videoInputs = CollectionsMarshal.AsSpan(_videoWireChanges);
        for (int offset = 0; offset < videoInputs.Length;)
        {
            int inactive = _vdp.TryReceiveInactiveCpuProgram(videoInputs[offset..]);
            if (inactive != 0) { offset += inactive; continue; }
            ref readonly var input = ref videoInputs[offset++];
            SynchronizeVideoDomain(input.Time);
            _vdp.ReceiveCpuInputWord(input.Time, input.Bits);
        }
        if (_videoObservation >= 0 && !_vdp.TryAdvanceInactiveCpuInterfaceTo(_videoObservation))
        { SynchronizeVideoDomain(_videoObservation); _vdp.AdvanceCpuInterfaceTo(_videoObservation); }
        // An inactive CPU video port and the received IRQ/data horizon make
        // ordinary RAM/ROM transactions independent of raster execution.
        // Materialize video before any actual port interaction above, any
        // announced response below, and the caller's observation boundary.
        if (_videoCpuDue <= end) SynchronizeVideoDomain(end);
        foreach (ref readonly var change in CollectionsMarshal.AsSpan(_soundWireChanges)) _psg.SetBus(change.Time, change.Data, change.Selected, change.Write);
        _psg.AdvanceTo(end);
        ResolveRangeCpuData();
        if (_executingCpuRange)
        {
            // Only I/O transactions and interrupt acknowledge can reach the
            // selected PSG/VDP ports. Memory cycles still sample WAIT/INT at
            // every prescribed phase; the already received stable levels do
            // not require another distribution of their output prefix.
            const uint io = (1u << (int)Z80BusCycle.IoRead) | (1u << (int)Z80BusCycle.IoWrite)
                | (1u << (int)Z80BusCycle.InterruptAcknowledge);
            long logicDue = Math.Min(_logicResponseDue[0], _logicResponseDue[1]);
            long wait = Math.Max(end, Math.Min(_psgReadyDue, logicDue));
            long control = Math.Max(end, Math.Min(_videoCpuDue, logicDue));
            _cpu.ReceiveBusInputWindow(new(wait, control, io, io));
        }
    }

    private static uint Bit(BusRecipient recipient) => 1U << (int)recipient;
    private uint ChangedRecipients(in CpuBusWires old, in CpuBusWires current)
    {
        uint recipients = 0;
        int addressBits = old.AddressKnown == current.AddressKnown ? old.AddressValue ^ current.AddressValue : 65535;
        bool address = addressBits != 0;
        byte controls = (byte)((old.Controls ^ current.Controls) | (old.ControlKnown ^ current.ControlKnown));
        bool data = old.DataValue != current.DataValue || old.DataIsKnown != current.DataIsKnown;
        bool read = (controls & 4) != 0, write = (controls & 8) != 0, memory = (controls & 1) != 0;
        if ((addressBits & 0xc0c0) != 0 || memory || (controls & 2) != 0) recipients |= Bit(BusRecipient.Decoder);
        if ((addressBits & 0x8000) != 0 || memory || read || write) recipients |= Bit(BusRecipient.Gates);
        if ((addressBits & 1023) != 0 || data || read || write) recipients |= Bit(BusRecipient.Ram);
        if (address || memory) recipients |= Bit(BusRecipient.RomLow) | Bit(BusRecipient.RomHigh);
        if (address || memory || read) recipients |= Bit(BusRecipient.InternalRom);
        if (address || data || read || write) recipients |= Bit(BusRecipient.CartridgeRam);
        if ((addressBits & 1) != 0) recipients |= Bit(BusRecipient.JoyLow) | Bit(BusRecipient.JoyHigh);
        if ((addressBits & 1) != 0 || data) recipients |= Bit(BusRecipient.Video);
        if (data || write) recipients |= Bit(BusRecipient.Sound);
        return recipients & _connectedBusRecipients;
    }

    private void DispatchWiredRecipients(uint recipients, long time)
    {
        recipients &= _connectedBusRecipients;
        while (recipients != 0)
        {
            int recipient = System.Numerics.BitOperations.TrailingZeroCount(recipients);
            recipients &= recipients - 1;
            ProjectBusInputs((BusRecipient)recipient, time, _wiredCpu, _logicOutputWords[0], _logicOutputWords[1]);
        }
    }

    private void ResolveWiredLogicUntil(long time)
    {
        // This is the fixed two-stage circuit, not an agenda or a scan of
        // consumers. Resolved gates precede new decoder inputs at equal dates.
        while (true)
        {
            long decoderDue = _logicResponseDue[0], gateDue = _logicResponseDue[1];
            if (decoderDue > time && gateDue > time) return;
            if (gateDue <= decoderDue)
            {
                _wiredLogicTouched |= 2;
                ulong output = _gates.ResolveWiredOutput();
                _logicOutputWords[1] = (ushort)output;
                _logicResponseDue[1] = _gates.NextEventNanoseconds ?? long.MaxValue;
                _gateCauseVisits++;
                byte changed = (byte)(output >> 16);
                uint recipients = (changed & 1) != 0 ? Bit(BusRecipient.RomLow) : 0;
                if ((changed & 6) != 0) { recipients |= Bit(BusRecipient.Video); _videoObservation = Math.Max(_videoObservation, gateDue); }
                if ((changed & 8) != 0) recipients |= Bit(BusRecipient.JoyLow) | Bit(BusRecipient.JoyHigh);
                DispatchWiredRecipients(recipients, gateDue);
            }
            else
            {
                _wiredLogicTouched |= 1;
                ulong output = _decoder.ResolveWiredOutput();
                _logicOutputWords[0] = (ushort)output;
                _logicResponseDue[0] = _decoder.NextEventNanoseconds ?? long.MaxValue;
                _directCauseVisits++;
                byte changed = (byte)(output >> 16);
                uint recipients = (changed & 192) != 0 ? Bit(BusRecipient.Gates) : 0;
                if ((changed & 8) != 0) { recipients |= Bit(BusRecipient.Ram); _ramObservation = Math.Max(_ramObservation, decoderDue); }
                if ((changed & 4) != 0) recipients |= Bit(BusRecipient.RomHigh) | Bit(BusRecipient.CartridgeRam);
                if ((changed & 32) != 0) recipients |= Bit(BusRecipient.Sound);
                DispatchWiredRecipients(recipients, decoderDue);
            }
        }
    }

    private void ResolveWiredJoyUntil(int index, long time)
    {
        while (_logicResponseDue[index + 2] <= time)
        {
            _wiredLogicTouched |= 1u << (index + 2);
            ulong output = _joy[index].ResolveWiredOutput();
            _logicOutputWords[index + 2] = (ushort)output;
            _logicResponseDue[index + 2] = _joy[index].NextEventNanoseconds ?? long.MaxValue;
            SetJoyDriver(index, (ushort)output);
        }
    }

    private void StoreLogicInput(int node, long time, ulong levels, ulong known)
    {
        levels &= known;
        if (_logicLevels[node] == levels && _logicKnown[node] == known) return;
        _logicLevels[node] = levels; _logicKnown[node] = known;
        _logicInputWords++;
        _wiredLogicTouched |= 1u << node;
        ulong word = levels | known << 32;
        if (node == 0)
        {
            _decoder.ReceiveWiredInput(time, word);
            _logicResponseDue[0] = _decoder.NextEventNanoseconds ?? long.MaxValue;
        }
        else if (node == 1)
        {
            _gates.ReceiveWiredInput(time, word);
            _logicResponseDue[1] = _gates.NextEventNanoseconds ?? long.MaxValue;
        }
        else
        {
            int index = node - 2;
            ResolveWiredJoyUntil(index, time);
            _joy[index].ReceiveWiredInput(time, word);
            _logicResponseDue[node] = _joy[index].NextEventNanoseconds ?? long.MaxValue;
        }
    }

    private static void InputBit(ref ulong levels, ref ulong known, int bit, bool? value)
    {
        if (value.HasValue) known |= 1UL << bit;
        if (value == true) levels |= 1UL << bit;
    }

    private void ProjectBusInputs(BusRecipient recipient, long time, in CpuBusWires cpu, ushort decoder, ushort gates)
    {
        ushort address = cpu.AddressValue;
        bool addressKnown = cpu.AddressKnown;
        byte data = cpu.DataValue, dataKnown = cpu.DataKnowledge;
        ulong levels = 0, known = 0;
        switch (recipient)
        {
            case BusRecipient.Decoder:
                StoreLogicInput(0, time, cpu.DecoderLevels, cpu.DecoderKnown);
                break;
            case BusRecipient.Gates:
                levels = cpu.GateCpuLevels; known = cpu.GateCpuKnown;
                var select6 = Logic(decoder, 6);
                if (select6 is LogicPinState.Low or LogicPinState.High) known |= 20;
                if (select6 == LogicPinState.High) levels |= 20;
                var select7 = Logic(decoder, 7);
                if (select7 is LogicPinState.Low or LogicPinState.High) known |= 64;
                if (select7 == LogicPinState.High) levels |= 64;
                StoreLogicInput(1, time, levels, known);
                break;
            case BusRecipient.Ram:
                ulong ramPins=(cpu.MemoryBits&~EmuMemoryPins.AddressMask)|(uint)(address&1023)
                    |(Logic(decoder,3)!=LogicPinState.Low?EmuMemoryPins.CsMask:0)
                    |(cpu.ReadInactive?EmuMemoryPins.OeMask:0)|(cpu.WriteInactive?EmuMemoryPins.WeMask:0);
                if (ramPins != _ramWirePins)
                {
                    _memoryInputWords++;
                    AcceptWiredMemory(4, _ram.ReceiveWiredPins(time, ramPins));
                    _ramObservation = Math.Max(_ramObservation, time); _ramWirePins = ramPins;
                }
                break;
            case BusRecipient.RomLow:
            case BusRecipient.RomHigh:
            case BusRecipient.InternalRom:
                bool internalRom = recipient == BusRecipient.InternalRom;
                int mask = internalRom ? 16383 : recipient == BusRecipient.RomLow ? _cartridgeLowMask : _cartridgeHighMask;
                bool ce = internalRom ? cpu.MemoryInactive || CartridgePresent || address >= 0x4000
                    : recipient == BusRecipient.RomLow && _romConfiguration.CartridgeLayout == Sg1000CartridgeLayout.TheCastle
                        ? false : cpu.MemoryInactive;
                bool oe = internalRom ? cpu.ReadInactive : recipient == BusRecipient.RomLow
                    ? Logic(gates, 0) != LogicPinState.Low : Logic(decoder, 2) != LogicPinState.Low;
                int romIndex = internalRom ? 2 : recipient == BusRecipient.RomLow ? 0 : 1;
                ulong romPins=(uint)(address&mask)|(addressKnown?EmuMemoryPins.AddressKnownMask:0)
                    |(ce?EmuMemoryPins.CsMask:0)|(oe?EmuMemoryPins.OeMask:0)|EmuMemoryPins.WeMask;
                if (romPins != _romWirePins[romIndex])
                {
                    _memoryInputWords++;
                    _wiredMemoryTouched |= 1u << romIndex;
                    var rom = internalRom ? _internalRom! : romIndex == 0 ? _cartridgeLow! : _cartridgeHigh!;
                    AcceptWiredMemory(romIndex, rom.ReceiveWiredPins(time - (internalRom ? 0 : _cartridgeOrigin), romPins));
                    _romWirePins[romIndex] = romPins;
                }
                break;
            case BusRecipient.CartridgeRam:
                int ramMask = _romConfiguration.CartridgeLayout == Sg1000CartridgeLayout.TheCastle ? 8191 : 2047;
                ulong cartPins=(cpu.MemoryBits&~EmuMemoryPins.AddressMask)|(uint)(address&ramMask)|EmuMemoryPins.Cs2Mask
                    |(Logic(decoder,2)!=LogicPinState.Low?EmuMemoryPins.CsMask:0)
                    |(cpu.ReadInactive?EmuMemoryPins.OeMask:0)|(cpu.WriteInactive?EmuMemoryPins.WeMask:0);
                if (cartPins != _cartridgeRamWirePins)
                {
                    _memoryInputWords++;
                    _wiredMemoryTouched |= 8;
                    AcceptWiredMemory(3, _cartridgeRam!.ReceiveWiredPins(time - _cartridgeOrigin, cartPins));
                    _cartridgeRamWirePins = cartPins;
                }
                break;
            case BusRecipient.JoyLow:
            case BusRecipient.JoyHigh:
                levels = recipient == BusRecipient.JoyLow ? _muxContactLow : _muxContactHigh;
                known = 255;
                levels |= (ulong)(address & 1) << 8; known |= addressKnown ? 256UL : 0;
                InputBit(ref levels, ref known, 9, Level(Logic(gates, 3)));
                StoreLogicInput(recipient == BusRecipient.JoyLow ? 2 : 3, time, levels, known);
                break;
            case BusRecipient.Video:
                ulong videoPins = MakeVideoWord(cpu, gates);
                if (videoPins != _busVideoPins)
                { StoreBusWord(_videoWireChanges, new(time, videoPins)); _busVideoPins = videoPins; }
                break;
            case BusRecipient.Sound:
                var soundPins = MakeSoundPins(cpu, decoder);
                if (soundPins != _busSoundPins)
                { StoreBusWord(_soundWireChanges, new(time, soundPins.Data, soundPins.Selected, soundPins.Write)); _busSoundPins = soundPins; }
                break;
        }
    }

    private static ulong MakeVideoWord(in CpuBusWires cpu, ushort gates)
        => cpu.DataValue | (cpu.DataIsKnown ? 256UL : 0) | ((ulong)(cpu.AddressValue & 1) << 9)
            | (Logic(gates, 1) != LogicPinState.Low ? 1024UL : 0)
            | (Logic(gates, 2) != LogicPinState.Low ? 2048UL : 0);
    private static (byte Data, bool Selected, bool Write) MakeSoundPins(in CpuBusWires cpu, ushort decoder)
        => (cpu.DataValue, Logic(decoder, 5) == LogicPinState.Low, !cpu.WriteInactive);

    private static ulong PackMainRam(Tmm2009Pins p) => EmuMemoryPins.Encode(p.Address,p.AddressKnown,p.Ce1High,p.OutputEnableHigh,p.WriteHigh,p.Ce2High,p.Data,p.DataKnown);
    private static ulong PackCartridgeRam(Tmm2016Pins p) => EmuMemoryPins.Encode(p.Address,p.AddressKnown,p.CsHigh,p.OeHigh,p.WeHigh,p.Cs2High,p.Data,p.DataKnown);
    private static ulong PackRom(Tmm23256Pins p) => EmuMemoryPins.Encode(p.Address,p.AddressKnown,p.CeHigh,p.OeHigh,true,false);
    private void AcceptWiredMemory(int driver, EmuMemoryResponse response)
    {
        _memoryResponseRecordsOmitted += response.Responses;
        if (driver < 4) _memoryResponseDue[driver] = response.Next < 0 ? long.MaxValue : response.Next;
        SetNativeMemoryDriver(driver, response.Final);
    }

    private void FinishWiredRom(int driver, long sourceEnd)
    {
        var chip = driver == 2 ? _internalRom! : driver == 0 ? _cartridgeLow! : _cartridgeHigh!;
        long end = sourceEnd - (driver == 2 ? 0 : _cartridgeOrigin);
        if ((_wiredMemoryTouched & (1u << driver)) != 0 || _memoryResponseDue[driver] <= end)
            AcceptWiredMemory(driver, chip.AdvanceWiredTime(end));
    }

    private void SetNativeMemoryDriver(int index,uint bits)
        => SetMemoryDriver(index,new((byte)bits,(byte)(bits>>8),(bits&0x10000)!=0?(byte)255:(byte)0));

    private void SetMemoryDriver(int index, DataWire driver)
    {
        if (_busMemoryDrivers[index] == driver) return;
        _busMemoryDrivers[index] = driver; _cpuBusDirty = true;
    }

    private void SetJoyDriver(int index, ushort outputs)
    {
        byte data = 0, known = 0, driven = 0;
        for (int pin = 0; pin < 4; pin++)
        {
            var state = Logic(outputs, pin);
            byte mask = (byte)(1 << (index * 4 + pin));
            if (state != LogicPinState.HighImpedance) driven |= mask;
            if (state is LogicPinState.High or LogicPinState.Low) known |= mask;
            if (state != LogicPinState.Low) data |= mask;
        }
        var driver = new DataWire(data, known, driven);
        if (_busJoyDrivers[index] == driver) return;
        _busJoyDrivers[index] = driver; _cpuBusDirty = true;
    }

    private void ResolveRangeCpuData()
    {
        if (!_cpuBusDirty) return;
        _cpuBusDirty = false; _cpuBusResolutions++;
        byte levels = 255, known = 0, driven = 0;
        if (_busCpuData is { } cpu) Add(new(cpu, 255, 255));
        foreach (var wire in _busMemoryDrivers) Add(wire);
        Add(_busVideoDriver);
        Add(_busJoyDrivers[0]); Add(_busJoyDrivers[1]);
        DataBusKnown = known == 255;
        _cpu.SetData((byte)(levels | ~known));
        void Add(DataWire wire)
        {
            byte first = (byte)(wire.Driven & ~driven), overlap = (byte)(wire.Driven & driven);
            byte agree = (byte)(~(levels ^ wire.Data) & known & wire.Known & overlap);
            levels = (byte)((levels & ~first) | (wire.Data & first));
            known = (byte)((known & ~wire.Driven) | (wire.Known & first) | agree);
            driven |= wire.Driven;
        }
    }

    private void AdvanceRangeBus(long requestedTime, bool stopAtFrame)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentOutOfRangeException.ThrowIfLessThan(requestedTime, TimeNanoseconds);
        _completedRaster = false;
        _executingCpuRange = true;
        try
        {
            while (TimeNanoseconds < requestedTime && !(stopAtFrame && _completedRaster))
            {
                long target = requestedTime;
                if (stopAtFrame && _vdp.NextFrameBoundaryNanoseconds is { } frame) target = Math.Min(target, frame);
                if (!_vdpReleased) target = Math.Min(target, C.VdpReleaseNanoseconds);
                if (!_cpuReleased) target = Math.Min(target, C.CpuReleaseNanoseconds);
                _cpu.ExecuteBusProgramUntil(target, _busRangeReceiver!);
                TimeNanoseconds = target;
                SynchronizeVideoDomain(target);
                if (!_vdpReleased && target == C.VdpReleaseNanoseconds) { _vdpReleased = true; _vdp.SetReset(target, false); }
                if (!_cpuReleased && target == C.CpuReleaseNanoseconds) { _cpuReleased = true; _cpu.SetReset(false); }
                _cpu.ExecuteBusProgramUntil(checked(target + 1), _busRangeReceiver!);
            }
        }
        finally { _executingCpuRange = false; }
        SynchronizeVideoDomain(TimeNanoseconds);
        SynchronizeReceivers(TimeNanoseconds);
        ResolveRangeCpuData();
        AdvanceAudio(TimeNanoseconds);
    }
}

