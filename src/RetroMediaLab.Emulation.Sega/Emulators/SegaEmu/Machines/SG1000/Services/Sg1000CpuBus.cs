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
    private delegate LogicOutputRange LogicRangeReceiver(in LogicInputRange input);
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
        public bool MemoryInactive => (ControlKnown & 1) == 0 || (Controls & 1) != 0;
        public bool ReadInactive => (ControlKnown & 4) == 0 || (Controls & 4) != 0;
        public bool WriteInactive => (ControlKnown & 8) == 0 || (Controls & 8) != 0;

        public CpuBusWires(Z80BusPins pins)
        {
            AddressValue = pins.GetAddressOrZero(); AddressKnown = pins.AddressDriven;
            DataValue = pins.GetDataOrZero(); DataIsKnown = pins.DataDriven;
            DataKnowledge = DataIsKnown ? (byte)255 : (byte)0;
            ControlKnown = pins.ControlDriven;
            Controls = (byte)(pins.ControlLevels & ControlKnown);
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
    private readonly struct CpuWireChange(long time, CpuBusWires wires, uint recipients)
    {
        internal readonly long Time = time;
        internal readonly CpuBusWires Wires = wires;
        internal readonly uint Recipients = recipients;
    }
    private readonly record struct VideoWireChange(long Time, Tms9918CpuPins Pins);
    private readonly record struct SoundWireChange(long Time, byte Data, bool Selected, bool Write);
    private readonly record struct DataWire(byte Data, byte Known, byte Driven);
    // Bounded scratch histories for one finite source prefix, never chip state
    // or a pending hardware FIFO. Fixed feed-forward fan-out bounds their size.
    private const int MaximumBusHistory = 16_384;
    private readonly List<TimedLogicInput>[] _logicInputs = [new(16),new(16),new(16),new(16)];
    private readonly ulong[] _logicLevels = new ulong[4], _logicKnown = new ulong[4];
    private readonly List<CpuWireChange> _cpuWireChanges = new(16);
    private readonly List<VideoWireChange> _videoWireChanges = new(16);
    private readonly List<SoundWireChange> _soundWireChanges = new(16);
    private readonly List<TimedMemoryInput<Tmm23256Pins>>[] _romInputs = [new(16),new(16),new(16)];
    private readonly Tmm23256Pins[] _romWirePins = new Tmm23256Pins[3];
    private Tmm2009Pins _ramWirePins;
    private Tmm2016Pins _cartridgeRamWirePins;
    private long _ramObservation, _videoObservation;
    private ushort _rangePressed;
    private CpuBusWires _initialCpuWires;
    private readonly long[] _logicResponseDue = new long[4], _memoryResponseDue = new long[4];
    private readonly ushort[] _logicOutputWords = new ushort[4];
    private readonly List<TimedLogicOutput> _busDecoderOutputs = new(32), _busGateOutputs = new(32);
    private readonly List<TimedMemoryInput<Tmm2009Pins>> _busRamInputs = new(32);
    private readonly List<TimedMemoryInput<Tmm2016Pins>> _busCartridgeRamInputs = new(32);

    private readonly DataWire[] _busMemoryDrivers = new DataWire[5]; // low/high/internal/cart SRAM/main SRAM
    private LogicRangeReceiver? _busDecoderReceiver, _busGateReceiver;
    private readonly LogicRangeReceiver?[] _busJoyReceivers = new LogicRangeReceiver?[2];
    private bool _initializeBusInputs, _executingCpuRange, _cpuBusDirty = true;
    private uint _connectedBusRecipients;
    private byte? _busCpuData;
    private readonly DataWire[] _busJoyDrivers = new DataWire[2];
    private long _cpuBusResolutions;
    internal long CpuBusResolutions => _cpuBusResolutions;
    private long _memoryResponseRecordsOmitted;
    internal long MemoryResponseRecordsOmitted => _memoryResponseRecordsOmitted;
    private long _directCauseVisits, _gateCauseVisits;
    private long _gateSkippedCpuWords;
    internal long DirectCauseVisits => _directCauseVisits;
    internal long GateCauseVisits => _gateCauseVisits;
    internal long GateSkippedCpuWords => _gateSkippedCpuWords;
    private Z80BusRangeHandler? _busRangeReceiver;
    private Tms9918CpuPins _busVideoPins;
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
    private long _nativeVideoSamples;
    internal long NativeVideoSamples => _nativeVideoSamples;
    private long _dramOutputNotifications, _vramDataDeliveries, _vramDataGroups;
    internal long DramReadOutputPrograms { get; private set; }
    internal long VramReadProgramWords { get; private set; }
    internal long DramReadOutputFallbacks { get; private set; }
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
        _busDecoderReceiver ??= _decoder.ExecuteInputRange;
        _busRangeReceiver ??= ReceiveCpuBusRange;
        _busGateReceiver ??= _gates.ExecuteInputRange;
        for (int i = 0; i < 2; i++) _busJoyReceivers[i] ??= _joy[i].ExecuteInputRange;
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
        _ramWirePins = _ram.CaptureState().Pins;
        _ramObservation = _ram.TimeNanoseconds;
        _cartridgeRamWirePins = _cartridgeRam?.CaptureState().Pins ?? default;
        _romWirePins[0] = _cartridgeLow?.CaptureState().Pins ?? default;
        _romWirePins[1] = _cartridgeHigh?.CaptureState().Pins ?? default;
        _romWirePins[2] = _internalRom?.CaptureState().Pins ?? default;
        _busVideoPins = _vdp.CaptureState().CpuInterface.Pins;
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
        var boundary = new Z80BusRange(TimeNanoseconds, TimeNanoseconds, _cpu.Pins, []);
        ReceiveCpuBusRange(in boundary);
    }
    private static LogicPinState Logic(ushort word, int pin) => (LogicPinState)((word >> (pin * 2)) & 3);
    private static DataWire RomDriver(Tmm23256Output output)
        => new(output.Data, output.Known ? (byte)255 : (byte)0, output.Driven ? (byte)255 : (byte)0);

    private ushort ConsumeLogicInputs(int node, LogicRangeReceiver receive, long end, List<TimedLogicOutput>? history)
    {
        var words = _logicInputs[node];
        _logicInputWords += words.Count;
        history?.Clear();
        if (words.Count == 0 && _logicResponseDue[node] > end) return _logicOutputWords[node];
        ushort final = 0;
        var inputs = CollectionsMarshal.AsSpan(words);
        for (int offset = 0; offset < inputs.Length || offset == 0;)
        {
            int count = Math.Min(LogicInputRange.MaximumChanges, inputs.Length - offset);
            long until = offset + count == inputs.Length ? end : inputs[offset + count - 1].TimeNanoseconds;
            var input = new LogicInputRange(inputs.Slice(offset, count), until);
            var output = receive(in input);
            _logicResponseDue[node] = output.NextEventNanoseconds ?? long.MaxValue;
            final = output.InitialLevels;
            foreach (var change in output.Changes)
            {
                final = change.Levels;
                if (history is not null) StoreBusWord(history, change);
            }
            offset += count;
            if (count == 0) break;
        }
        _logicOutputWords[node] = final;
        return final;
    }

    private void ReceiveCpuBusRange(in Z80BusRange source)
    {
        var changes = source.Changes;
        long start = source.StartNanoseconds, end = source.EndNanoseconds;
        var initialPins = source.InitialPins;
        _busRangeDeliveries++;
        _cpuOutputWords += changes.Length;
        foreach (var list in _logicInputs) list.Clear();
        foreach (var list in _romInputs) list.Clear();
        _busRamInputs.Clear(); _busCartridgeRamInputs.Clear(); _cpuWireChanges.Clear();
        _videoWireChanges.Clear(); _soundWireChanges.Clear(); _videoObservation = -1;
        if (!changes.IsEmpty)
        {
            long lastOutputDate = changes[^1].TimeNanoseconds;
            _ramObservation = lastOutputDate;
            _videoObservation = lastOutputDate;
        }
        ushort decoderInitial = _logicOutputWords[0], gateInitial = _logicOutputWords[1];
        _initialCpuWires = new(initialPins);
        var previous = _initialCpuWires;
        if (_initializeBusInputs)
        {
            _initializeBusInputs = false;
            ProjectBusInputs(BusRecipient.Decoder, start, previous, decoderInitial, gateInitial);
            _cpuWireChanges.Add(new(start, previous, _connectedBusRecipients));
        }
        foreach (var change in changes)
        {
            var wires = new CpuBusWires(change.Pins);
            uint recipients = ChangedRecipients(previous, wires);
            if (recipients != 0) _cpuWireChanges.Add(new(change.TimeNanoseconds, wires, recipients));
            if ((recipients & Bit(BusRecipient.Decoder)) != 0)
                ProjectBusInputs(BusRecipient.Decoder, change.TimeNanoseconds, wires, decoderInitial, gateInitial);
            previous = wires;
        }
        ConsumeLogicInputs(0, _busDecoderReceiver!, end, _busDecoderOutputs);
        DistributeDirectCauses(decoderInitial, gateInitial);
        ConsumeLogicInputs(1, _busGateReceiver!, end, _busGateOutputs);
        DistributeGateCauses(start, gateInitial);
        ConsumeMainRam();
        if (_cartridgeLow is not null) ConsumeRom(BusRecipient.RomLow, 0, end);
        if (_cartridgeHigh is not null) ConsumeRom(BusRecipient.RomHigh, 1, end);
        if (_internalRom is not null) ConsumeRom(BusRecipient.InternalRom, 2, end);
        if (_cartridgeRam is not null) ConsumeCartridgeRam(end - _cartridgeOrigin);
        for (int i = 0; i < 2; i++)
        {
            ushort before = _logicOutputWords[i + 2];
            ushort final = ConsumeLogicInputs(i + 2, _busJoyReceivers[i]!, end, null);
            if (before != final) SetJoyDriver(i, final);
        }
        var finalCpu = changes.IsEmpty ? initialPins : changes[^1].Pins;
        if (_busCpuData != finalCpu.Data) { _busCpuData = finalCpu.Data; _cpuBusDirty = true; }
        foreach (ref readonly var change in CollectionsMarshal.AsSpan(_videoWireChanges))
        {
            if (!_vdp.TryReceiveInactiveCpuPins(change.Time, change.Pins))
            { SynchronizeVideoDomain(change.Time); _vdp.ReceiveCpuPins(change.Time, change.Pins); }
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

    // Visit each CPU/decoder cause once for the gate inputs and all sinks
    // independent of those gates. The downstream pass never replays decoder
    // changes or redistributes these direct recipients.
    private void DistributeDirectCauses(ushort decoderInitial, ushort gateInitial)
    {
        // These histories are complete and are not appended to by projection.
        // Keep their storage views for the entire merge, rather than repeatedly
        // crossing List<T> indexers and copying the CPU wire bundle.
        var cpuChanges = CollectionsMarshal.AsSpan(_cpuWireChanges);
        var decoderOutputs = CollectionsMarshal.AsSpan(_busDecoderOutputs);
        var cpu = _initialCpuWires;
        ushort decoder = decoderInitial;
        int c = 0, d = 0;
        while (c < cpuChanges.Length || d < decoderOutputs.Length)
        {
            _directCauseVisits++;
            long ct = c < cpuChanges.Length ? cpuChanges[c].Time : long.MaxValue;
            long dt = d < decoderOutputs.Length ? decoderOutputs[d].TimeNanoseconds : long.MaxValue;
            long time = Math.Min(ct, dt);
            uint recipients = 0;
            if (d < decoderOutputs.Length && dt <= ct)
            {
                ref readonly var output = ref decoderOutputs[d++];
                byte changed = output.ChangedPins;
                decoder = output.Levels;
                if ((changed & 192) != 0) recipients |= Bit(BusRecipient.Gates);
                if ((changed & 8) != 0) { recipients |= Bit(BusRecipient.Ram); _ramObservation = Math.Max(_ramObservation, time); }
                if ((changed & 4) != 0) recipients |= Bit(BusRecipient.RomHigh) | Bit(BusRecipient.CartridgeRam);
                if ((changed & 32) != 0) recipients |= Bit(BusRecipient.Sound);
            }
            else
            {
                ref readonly var change = ref cpuChanges[c++]; cpu = change.Wires; recipients = change.Recipients;
            }
            recipients &= (DirectConsumers | Bit(BusRecipient.Gates)) & _connectedBusRecipients;
            while (recipients != 0)
            {
                int index = System.Numerics.BitOperations.TrailingZeroCount(recipients);
                recipients &= recipients - 1;
                ProjectBusInputs((BusRecipient)index, time, cpu, decoder, gateInitial);
            }
        }
    }

    private void DistributeGateCauses(long start, ushort gateInitial)
    {
        var cpuChanges = CollectionsMarshal.AsSpan(_cpuWireChanges);
        var gateOutputs = CollectionsMarshal.AsSpan(_busGateOutputs);
        var cpu = _initialCpuWires;
        ushort gates = gateInitial;
        if (_rangePressed != _pressed)
        {
            ProjectBusInputs(BusRecipient.JoyLow, start, cpu, 0, gates);
            ProjectBusInputs(BusRecipient.JoyHigh, start, cpu, 0, gates);
            _rangePressed = _pressed;
        }
        int c = 0, g = 0;
        while (true)
        {
            while (c < cpuChanges.Length && (cpuChanges[c].Recipients & GateConsumers) == 0)
            { _gateSkippedCpuWords++; c++; }
            if (c == cpuChanges.Length && g == gateOutputs.Length) break;
            _gateCauseVisits++;
            long ct = c < cpuChanges.Length ? cpuChanges[c].Time : long.MaxValue;
            long gt = g < gateOutputs.Length ? gateOutputs[g].TimeNanoseconds : long.MaxValue;
            long time = Math.Min(ct, gt);
            uint recipients;
            // Retain the established tie order: resolved gate outputs before
            // CPU input changes at the same electrical date.
            if (g < gateOutputs.Length && gt <= ct)
            {
                ref readonly var output = ref gateOutputs[g++];
                byte changed = output.ChangedPins;
                gates = output.Levels;
                recipients = (changed & 1) != 0 ? Bit(BusRecipient.RomLow) : 0;
                if ((changed & 6) != 0) { recipients |= Bit(BusRecipient.Video); _videoObservation = Math.Max(_videoObservation, time); }
                if ((changed & 8) != 0) recipients |= Bit(BusRecipient.JoyLow) | Bit(BusRecipient.JoyHigh);
            }
            else
            {
                ref readonly var change = ref cpuChanges[c++]; cpu = change.Wires;
                recipients = change.Recipients & GateConsumers;
            }
            recipients &= _connectedBusRecipients;
            while (recipients != 0)
            {
                int index = System.Numerics.BitOperations.TrailingZeroCount(recipients);
                recipients &= recipients - 1;
                ProjectBusInputs((BusRecipient)index, time, cpu, 0, gates);
            }
        }
    }

    private void StoreLogicInput(int node, long time, ulong levels, ulong known)
    {
        levels &= known;
        if (_logicLevels[node] == levels && _logicKnown[node] == known) return;
        _logicLevels[node] = levels; _logicKnown[node] = known;
        StoreBusWord(_logicInputs[node], new TimedLogicInput(time, levels, known));
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
                var ramPins = new Tmm2009Pins((ushort)(address & 1023), addressKnown,
                    Logic(decoder, 3) != LogicPinState.Low, false, cpu.ReadInactive, cpu.WriteInactive, data, dataKnown);
                if (ramPins != _ramWirePins) { StoreBusWord(_busRamInputs, new(time, ramPins)); _ramWirePins = ramPins; }
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
                var romPins = new Tmm23256Pins((ushort)(address & mask), addressKnown, ce, oe);
                if (romPins != _romWirePins[romIndex])
                { StoreBusWord(_romInputs[romIndex], new(time - (internalRom ? 0 : _cartridgeOrigin), romPins)); _romWirePins[romIndex] = romPins; }
                break;
            case BusRecipient.CartridgeRam:
                int ramMask = _romConfiguration.CartridgeLayout == Sg1000CartridgeLayout.TheCastle ? 8191 : 2047;
                var cartPins = new Tmm2016Pins((ushort)(address & ramMask), addressKnown, Logic(decoder, 2) != LogicPinState.Low,
                    cpu.ReadInactive, cpu.WriteInactive, data, dataKnown, true);
                if (cartPins != _cartridgeRamWirePins)
                { StoreBusWord(_busCartridgeRamInputs, new(time - _cartridgeOrigin, cartPins)); _cartridgeRamWirePins = cartPins; }
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
                var videoPins = MakeVideoPins(cpu, gates);
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

    private static Tms9918CpuPins MakeVideoPins(in CpuBusWires cpu, ushort gates)
        => new(cpu.DataValue, cpu.DataIsKnown, (cpu.AddressValue & 1) != 0,
            Logic(gates, 1) != LogicPinState.Low, Logic(gates, 2) != LogicPinState.Low);
    private static (byte Data, bool Selected, bool Write) MakeSoundPins(in CpuBusWires cpu, ushort decoder)
        => (cpu.DataValue, Logic(decoder, 5) == LogicPinState.Low, !cpu.WriteInactive);

    private void ConsumeMainRam()
    {
        _memoryInputWords += _busRamInputs.Count;
        var inputs = CollectionsMarshal.AsSpan(_busRamInputs);
        if (inputs.IsEmpty) { _ram.AdvanceTo(_ramObservation); return; }
        // This untimed functional SRAM retains the last actual pin-delivery
        // date. Unlike delayed memories it has no propagation to materialize
        // at a CPU sampling fence with no incoming cause.
        long end = _ramObservation;
        for (int offset = 0; offset < inputs.Length;)
        {
            int count = Math.Min(MemoryInputRange<Tmm2009Pins>.MaximumChanges, inputs.Length - offset);
            var input = new MemoryInputRange<Tmm2009Pins>(inputs.Slice(offset, count),
                offset + count == inputs.Length ? end : inputs[offset + count - 1].TimeNanoseconds);
            var output = _ram.ExecuteInputRange(in input, retainResponseHistory: false);
            _memoryResponseRecordsOmitted += output.ResponseCount;
            var final = output.FinalOutput;
            SetMemoryDriver(4, new(final.Data, final.Known, final.Driven ? (byte)255 : (byte)0));
            offset += count;
        }
    }
    private void ConsumeCartridgeRam(long end)
    {
        _memoryInputWords += _busCartridgeRamInputs.Count;
        if (_busCartridgeRamInputs.Count == 0 && _memoryResponseDue[3] > end) return;
        var inputs = CollectionsMarshal.AsSpan(_busCartridgeRamInputs);
        for (int offset = 0; offset < inputs.Length || offset == 0;)
        {
            int count = Math.Min(MemoryInputRange<Tmm2016Pins>.MaximumChanges, inputs.Length - offset);
            var input = new MemoryInputRange<Tmm2016Pins>(inputs.Slice(offset, count),
                offset + count == inputs.Length ? end : inputs[offset + count - 1].TimeNanoseconds);
            var output = _cartridgeRam!.ExecuteInputRange(in input, retainResponseHistory: false);
            _memoryResponseRecordsOmitted += output.ResponseCount;
            _memoryResponseDue[3] = output.NextEventNanoseconds ?? long.MaxValue;
            var final = output.FinalOutput;
            SetMemoryDriver(3, new(final.Data, final.Known, final.Driven ? (byte)255 : (byte)0));
            offset += count;
            if (count == 0) break;
        }
    }
    private void ConsumeRom(BusRecipient recipient, int driver, long sourceEnd)
    {
        _memoryInputWords += _romInputs[driver].Count;
        var chip = recipient == BusRecipient.InternalRom ? _internalRom! : recipient == BusRecipient.RomLow ? _cartridgeLow! : _cartridgeHigh!;
        long end = sourceEnd - (recipient == BusRecipient.InternalRom ? 0 : _cartridgeOrigin);
        if (_romInputs[driver].Count == 0 && _memoryResponseDue[driver] > end) return;
        var inputs = CollectionsMarshal.AsSpan(_romInputs[driver]);
        for (int offset = 0; offset < inputs.Length || offset == 0;)
        {
            int count = Math.Min(MemoryInputRange<Tmm23256Pins>.MaximumChanges, inputs.Length - offset);
            var input = new MemoryInputRange<Tmm23256Pins>(inputs.Slice(offset, count),
                offset + count == inputs.Length ? end : inputs[offset + count - 1].TimeNanoseconds);
            var output = chip.ExecuteInputRange(in input, retainResponseHistory: false);
            _memoryResponseRecordsOmitted += output.ResponseCount;
            _memoryResponseDue[driver] = output.NextEventNanoseconds ?? long.MaxValue;
            SetMemoryDriver(driver, RomDriver(output.FinalOutput));
            offset += count;
            if (count == 0) break;
        }
    }

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
                _cpu.AdvanceBusRangeUntil(target, _busRangeReceiver!);
                TimeNanoseconds = target;
                SynchronizeVideoDomain(target);
                if (!_vdpReleased && target == C.VdpReleaseNanoseconds) { _vdpReleased = true; _vdp.SetReset(target, false); }
                if (!_cpuReleased && target == C.CpuReleaseNanoseconds) { _cpuReleased = true; _cpu.SetReset(false); }
                _cpu.AdvanceBusRangeUntil(checked(target + 1), _busRangeReceiver!);
            }
        }
        finally { _executingCpuRange = false; }
        SynchronizeVideoDomain(TimeNanoseconds);
        SynchronizeReceivers(TimeNanoseconds);
        ResolveRangeCpuData();
        AdvanceAudio(TimeNanoseconds);
    }
}

