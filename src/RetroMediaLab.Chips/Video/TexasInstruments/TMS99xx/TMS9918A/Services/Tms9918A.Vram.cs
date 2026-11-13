using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;
using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Enums;
using V = RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Models.Tms9918VramTiming;
using K = RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Enums.Tms9918VramSlotKind;

namespace RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Services;

public sealed partial class Tms9918A
{
    private Tms9918VramBus _vramBus;
    private Tms9918BackgroundPipeline _background = new(default);
    private long _vramDue = -1, _vramWriteEnd = -1;
    private long _vramStarted, _vramStartedHalfEdge, _cpuReadyHalfEdge;
    private int _vramStep;
    private bool _vramActive, _vramWrite, _vramAddressKnown;
    private byte _vramRow, _vramColumn, _vramWriteData, _refreshRow;
    private Tms9918VramSlot _vramSlot, _ramSlot;
    private bool _ramLoadActive, _ramDataKnown, _ramCpuWrite;
    private byte _ramData;
    private long _ramLoadHalfEdge;
    private bool _cpuVramInService;

    public Tms9918VramPins VramPins
    {
        get { MaterializeProjectedReadPins(); return _vramBus.Pins; }
    }
    private readonly Action<Tms9918VramPins> _vramPinDispatch;
    private Action<Tms9918VramSignal>? _vramPinsObserver, _unrepresentedVramPinsObserver;
    public Action<Tms9918VramSignal>? VramPinsChanged
    {
        get => _vramPinsObserver;
        set
        {
            if (value is not null) WithdrawProjectedRead();
            _vramPinsObserver = value; UpdateVramPinObservation();
        }
    }
    public Action<Tms9918VramSignal>? UnrepresentedVramPinsChanged
    {
        get => _unrepresentedVramPinsObserver;
        set { _unrepresentedVramPinsObserver = value; UpdateVramPinObservation(); }
    }
    public Tms9918VramReadCycleReceiver? VramReadCycleProduced { get; set; }
    public Action<long>? VramReadCycleCancelled { get; set; }
    private bool _vramReadWaveRouted;
    private bool _projectedRead;
    private long _projectedReadCycles, _readPhaseVisitsEliminated;
    public long ProjectedReadCycles => _projectedReadCycles;
    public long ReadPhaseVisitsEliminated => _readPhaseVisitsEliminated;

    private int ProjectedReadStep => _time - _vramStarted < V.RasAssert ? 1
        : _time - _vramStarted < V.CasAssert ? 2
        : _time - _vramStarted < V.RasRelease ? 6 : 7;
    private long ObservedVramDue => !_projectedRead ? _vramDue : checked(_vramStarted
        + (ProjectedReadStep switch { 1 => V.RasAssert, 2 => V.CasAssert, 6 => V.RasRelease, _ => V.CasRelease }));

    private void MaterializeProjectedReadPins()
    {
        if (!_projectedRead) return;
        int step = ProjectedReadStep;
        int offset = step switch { 1 => 0, 2 => V.RasAssert, 6 => V.CasAssert, _ => V.RasRelease };
        _vramStep = step;
        _vramBus.MaterializeReadPrefix(checked(_vramStarted + offset), new(
            step < 6 ? _vramRow : _vramColumn, _vramAddressKnown,
            step is 1 or 7, step < 6, true));
    }

    private void WithdrawProjectedRead()
    {
        if (!_projectedRead) return;
        MaterializeProjectedReadPins();
        long next = ObservedVramDue;
        _readPhaseVisitsEliminated += _vramStep switch { 1 => 0, 2 => 1, 6 => 3, _ => 4 };
        _projectedRead = false;
        SetVramDeadline(next);
        // No scalar deadline listener could coexist with this representation.
        // A newly attached listener starts from the current physical deadline.
        _announcedDeadline = _nextDue;
    }
    private readonly Tms9918VramReadInput[] _vramReadInputs = new Tms9918VramReadInput[48];
    private int _vramReadInputCount, _vramReadInputIndex;
    private long _vramReadInputDue = long.MaxValue;

    /// <summary>Replaces the unconsumed wire suffix. The connected producer
    /// owns its validity and must withdraw it before any input that changes
    /// its predictions. No event list is built inside the raster loop.</summary>
    public void ReceiveVramReadProgram(ReadOnlySpan<Tms9918VramReadInput> inputs)
    {
        if (inputs.Length > _vramReadInputs.Length) throw new ArgumentOutOfRangeException(nameof(inputs));
        long previous = _time;
        foreach (ref readonly var input in inputs)
        {
            if (input.TimeNanoseconds < previous) throw new ArgumentException("VRAM input dates must be ordered.", nameof(inputs));
            previous = input.TimeNanoseconds;
        }
        ResolveVramReadInputs(_time);
        inputs.CopyTo(_vramReadInputs);
        _vramReadInputCount = inputs.Length;
        _vramReadInputIndex = 0;
        _vramReadInputDue = inputs.IsEmpty ? long.MaxValue : inputs[0].TimeNanoseconds;
    }

    public void WithdrawVramReadProgram()
    {
        ResolveVramReadInputs(_time);
        _vramReadInputCount = _vramReadInputIndex = 0;
        _vramReadInputDue = long.MaxValue;
    }

    // Received DOUT words change only the input latch. They do not schedule
    // another master-clock visit. Resolve their exact dates before sampling,
    // replacing the received suffix, or observing the component.
    private void ResolveVramReadInputs(long boundary, bool inclusive = true)
    {
        while (_vramReadInputIndex < _vramReadInputCount
            && (_vramReadInputDue < boundary || inclusive && _vramReadInputDue == boundary))
        {
            var input = _vramReadInputs[_vramReadInputIndex++];
            _vramReadInputDue = _vramReadInputIndex < _vramReadInputCount
                ? _vramReadInputs[_vramReadInputIndex].TimeNanoseconds : long.MaxValue;
            if (_time < input.TimeNanoseconds) _time = input.TimeNanoseconds;
            _vramBus.SetInputData(input.TimeNanoseconds, input.Data, input.Known, input.IntermediateTransition);
        }
    }

    private void UpdateVramPinObservation()
    {
        if (_vramBus is null) return;
        _vramBus.PinsChanged = _vramPinsObserver is not null
            || !_vramReadWaveRouted && _unrepresentedVramPinsObserver is not null ? _vramPinDispatch : null;
    }

    private void PublishVramPins(Tms9918VramPins pins)
    {
        bool wasPublishing = _publishing;
        _publishing = true;
        try
        {
            var signal = new Tms9918VramSignal(_time, pins);
            _vramPinsObserver?.Invoke(signal);
            // The first observer may change subscriptions or withdraw a
            // represented waveform at this date. Read the actual binding now.
            if (!_vramReadWaveRouted) _unrepresentedVramPinsObserver?.Invoke(signal);
        }
        finally { _publishing = wasPublishing; }
    }

    public void SetVramReadData(long timeNanoseconds, byte data, bool known)
    {
        AdvanceTo(timeNanoseconds);
        _vramBus.SetInputData(timeNanoseconds, data, known);
    }

    /// <summary>Receives the final word of coincident input transitions. The
    /// producer must retain any real intermediate transition, including an
    /// excursion returning to the same final data or knowledge. No observation
    /// or other VDP interaction may occur inside the condensed sequence.</summary>
    public void SetVramReadData(long timeNanoseconds, byte data, bool known, bool intermediateTransition)
    {
        AdvanceTo(timeNanoseconds);
        _vramBus.SetInputData(timeNanoseconds, data, known, intermediateTransition);
    }

    private void ArmCpuVramRequest() => _cpuReadyHalfEdge = checked(_clock.HalfEdges + V.CpuPreparationHalfEdges);

    private void SelectVramSlot(int dot, int line)
    {
        int nextLine = line == 261 ? 0 : line + 1;
        var slot = Tms9918VramSchedule.At(dot, _registers.Mode, false);
        // Préparer aussi les sprites des premières lignes : leur scan commence
        // deux lignes avant la sortie, puis les quatre canaux sont chargés.
        // Garder le calendrier existant ; la phase verticale est nominale,
        // pas une nouvelle caractérisation des broches pendant la bordure.
        bool spritePreparation = line == 261 && slot.Kind is >= K.SpriteVisibility and <= K.SpritePatternRight
            || line == 260 && dot >= 316 && slot.Kind == K.SpriteVisibility;
        bool refresh = !_registers.DisplayEnabled || (line >= 192
            && !spritePreparation && !(nextLine == 0 && dot >= 332));
        if (refresh) slot = Tms9918VramSchedule.At(dot, _registers.Mode, true);
        Tms9918FetchAddress address;
        bool write = false;
        byte data = 0;
        int targetLine = dot >= (_registers.UsesTextTiming ? 338 : 332) ? nextLine : line;
        switch (slot.Kind)
        {
            case K.Cpu:
                if (!_memory.HasPendingRequest || _cpuVramInService || _clock.HalfEdges < _cpuReadyHalfEdge) return;
                var request = _memory.CurrentRequest;
                address = new(request.Address ?? 0, request.Address.HasValue && request.Qualified);
                write = request.Operation == Tms9918CpuVramOperation.Write;
                data = request.Data;
                break;
            case K.Refresh:
                address = new(_refreshRow, true);
                _refreshRow = (byte)((_refreshRow + 1) & 127);
                break;
            case K.BackgroundName:
                if (targetLine >= 192) return;
                address = Tms9918BackgroundAddresses.Name(_registers, targetLine, slot.Index);
                break;
            case K.BackgroundColour:
                if (targetLine >= 192) return;
                address = Tms9918BackgroundAddresses.Colour(_registers, targetLine, _background.Name, _background.NameKnown);
                break;
            case K.BackgroundPattern:
                if (targetLine >= 192) return;
                address = Tms9918BackgroundAddresses.Pattern(_registers, targetLine, _background.Name, _background.NameKnown);
                break;
            default:
                if (!SelectSpriteAddress(slot, line, out address)) return;
                break;
        }
        BeginVramCycle(slot, address, write, data);
    }

    private void BeginVramCycle(Tms9918VramSlot slot, Tms9918FetchAddress address, bool write, byte data)
    {
        if (_vramActive) { _qualified = false; return; } // source hors cadence du profil en cours
        ushort value = address.Address;
        if (_registers.SixteenKilobyteVram)
        { _vramRow = (byte)(value & 127); _vramColumn = (byte)(value >> 7); }
        else
        { _vramRow = (byte)((value & 63) | ((value >> 6) & 64)); _vramColumn = (byte)(((value >> 6) & 63) | ((value >> 7) & 64)); }
        _vramSlot = slot;
        if (slot.Kind == K.Cpu) _cpuVramInService = true;
        _vramAddressKnown = address.Qualified;
        _vramWrite = write;
        _vramWriteData = data;
        _vramActive = true;
        _vramStarted = _time;
        _vramStartedHalfEdge = _clock.HalfEdges;
        _vramStep = 1;
        SetVramDeadline(checked(_time + V.RasAssert));
        _vramReadWaveRouted = false;
        if (!write && _vramWriteEnd < 0 && VramPins.RasHigh && VramPins.CasHigh && VramPins.ReadWriteHigh
            && VramReadCycleProduced is { } receiver)
        {
            var cycle = new Tms9918VramReadCycle(_time, checked(_time + V.RasAssert),
                checked(_time + V.CasAssert), checked(_time + V.RasRelease),
                checked(_time + V.CasRelease), _vramRow, _vramColumn, address.Qualified);
            bool wasPublishing = _publishing;
            _publishing = true;
            try { _vramReadWaveRouted = receiver(in cycle); }
            finally { _publishing = wasPublishing; }
        }
        UpdateVramPinObservation();
        _vramBus.ApplyOutputs(_time, VramPins with
            { AddressData = _vramRow, AddressDataKnown = address.Qualified });
        if (_vramReadWaveRouted && _vramPinsObserver is null && _deadlineObserver is null)
        {
            _projectedRead = true;
            _projectedReadCycles++;
            SetVramDeadline(checked(_vramStarted + V.CasRelease));
        }
        PublishDeadline();
    }

    private void AdvanceVramSequence()
    {
        if (_projectedRead)
        {
            MaterializeProjectedReadPins();
            _readPhaseVisitsEliminated += 4;
            _projectedRead = false;
            _vramStep = 7;
        }
        var pins = VramPins;
        int nextOffset;
        switch (_vramStep++)
        {
            case 1:
                pins = pins with { RasHigh = false };
                nextOffset = V.CasAssert;
                break;
            case 2:
                pins = pins with { CasHigh = false, AddressData = _vramColumn, AddressDataKnown = _vramAddressKnown };
                nextOffset = V.Column;
                break;
            case 3:
                pins = pins with { AddressData = _vramColumn, AddressDataKnown = _vramAddressKnown };
                nextOffset = _vramWrite ? V.WriteData : V.RasRelease;
                if (!_vramWrite) _vramStep = 6;
                break;
            case 4:
                // Pas de second bufferCPU capturé à la sélection d'adresse :
                // présenter le registre partagé à la vraie phase de données.
                if (_vramSlot.Kind == K.Cpu)
                {
                    _vramWriteData = _memory.Data;
                    _vramAddressKnown &= _memory.CurrentRequest.Qualified && _memory.DataKnown;
                }
                pins = pins with { AddressData = _vramWriteData, AddressDataKnown = _vramAddressKnown };
                nextOffset = V.WriteAssert;
                break;
            case 5:
                pins = pins with { ReadWriteHigh = false };
                nextOffset = V.RasRelease;
                break;
            case 6:
                pins = pins with { RasHigh = true };
                nextOffset = V.CasRelease;
                break;
            case 7:
                _vramDue = -1;
                if (_vramWrite)
                {
                    QueueRamLoad(0, _vramAddressKnown, true);
                    _vramWriteEnd = checked(_vramStarted + V.WriteRelease);
                }
                RefreshNextDeadline();
                _vramBus.ApplyOutputs(_time, pins with { CasHigh = true });
                _vramReadWaveRouted = false;
                UpdateVramPinObservation();
                _vramActive = false;
                return;
            default: throw new InvalidOperationException("Invalid VDP VRAM sequencer phase.");
        }
        SetVramDeadline(checked(_vramStarted + nextOffset));
        _vramBus.ApplyOutputs(_time, pins);
    }

    private void CaptureVramRead(Tms9918VramRead read)
    {
        if (!_vramActive || _vramWrite || _vramSlot.Kind == K.Refresh) return;
        QueueRamLoad(read.Data, read.DataKnown && _vramAddressKnown, false);
    }

    private void QueueRamLoad(byte data, bool known, bool cpuWrite)
    {
        if (_ramLoadActive) throw new InvalidOperationException("VDP RAM latch was not consumed at its phase.");
        _ramSlot = _vramSlot; _ramData = data; _ramDataKnown = known; _ramCpuWrite = cpuWrite;
        _ramLoadHalfEdge = checked(_vramStartedHalfEdge + V.RamLatchHalfEdges);
        _ramLoadActive = true;
    }

    private void ApplyRamLoad()
    {
        _ramLoadActive = false;
        switch (_ramSlot.Kind)
        {
            case K.Cpu:
                _cpuVramInService = false;
                if (_ramCpuWrite) _memory.CompleteWrite();
                else _memory.CompleteRead(_ramData, _ramDataKnown);
                break;
            case K.BackgroundName: _background.LoadName(_ramData, _ramDataKnown); break;
            case K.BackgroundColour: _background.LoadColour(_ramData, _ramDataKnown); break;
            case K.BackgroundPattern: _background.LoadPattern(_ramData, _ramDataKnown); break;
            default: LoadSpriteByte(_ramSlot, _ramData, _ramDataKnown); break;
        }
    }

    private void WithdrawVramReadWave()
    {
        WithdrawProjectedRead();
        if (_vramReadWaveRouted)
        {
            bool wasPublishing = _publishing;
            _publishing = true;
            try { VramReadCycleCancelled?.Invoke(_time); }
            finally { _publishing = wasPublishing; }
            _vramReadWaveRouted = false;
            UpdateVramPinObservation();
        }
    }

    private void CancelVramOnReset()
    {
        WithdrawVramReadWave();
        _vramDue = _vramWriteEnd = -1;
        RefreshNextDeadline();
        _vramActive = _ramLoadActive = _cpuVramInService = false;
        var state = _background.Capture();
        _background = new(state with { NameKnown = false, PatternKnown = false, ColourKnown = false,
            ActivePatternKnown = false, ActiveColourKnown = false });
        _vramBus.ApplyOutputs(_time, VramPins with
            { RasHigh = true, CasHigh = true, ReadWriteHigh = true, AddressDataKnown = false });
    }
}

/// <summary>A dated word on the connected VRAM data wires. Intermediate
/// transitions retain data/knowledge excursions at a single instant.</summary>
public readonly record struct Tms9918VramReadInput
{
    public readonly long TimeNanoseconds;
    public readonly byte Data;
    public readonly bool Known, IntermediateTransition;

    public Tms9918VramReadInput(long timeNanoseconds, byte data, bool known, bool intermediateTransition = false)
    {
        TimeNanoseconds = timeNanoseconds; Data = data;
        Known = known; IntermediateTransition = intermediateTransition;
    }
}
