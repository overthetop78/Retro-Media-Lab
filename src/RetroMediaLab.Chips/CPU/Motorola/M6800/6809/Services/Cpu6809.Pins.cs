using RetroMediaLab.Chips.CPU.Common.Interfaces;
using RetroMediaLab.Chips.CPU.Motorola.M6800.Chip6809.Interfaces;
using C = RetroMediaLab.Chips.CPU.Motorola.M6800.Chip6809.Constants.Cpu6809Constants;

namespace RetroMediaLab.Chips.CPU.Motorola.M6800.Chip6809.Services;

public sealed partial class Cpu6809
{
    private bool _haltInput;
    private bool _haltAcknowledged;
    private bool _resetInput;
    private bool _resetLatched;
    private long _resetStarted;
    private bool _dmaInput;
    private bool _dmaPending;
    private bool _memoryReady = true;
    private int _dmaRefreshCounter;
    private int _dmaInactiveCycles = C.DmaCounterClearCycles;
    private long _processorCycles;
    private readonly ICpu6809ClockBus? _clockBus;
    private readonly ICpuBusCyclePins? _cyclePins;
    // The legacy bus advances at the interrupt-sampling quarter, as before.
    private int _sourceClockPhase = 1;
    private bool _busAvailable;
    private bool _busStatus;
    private bool _nextBusAvailable;
    private bool _nextBusStatus;
    private bool _busStatusPending;
    private bool _busDriversReleased;
    private bool _clockE;
    private bool _clockQ;
    private ushort? _busAddress;
    private bool? _busRead;
    private byte? _busOutputData;
    private ushort _nextBusAddress = ushort.MaxValue;
    private bool _nextBusRead = true;
    private byte? _nextBusOutputData;

    /// <summary>Nombre de périodes EXTAL : le MC6809 divise cette source par quatre.</summary>
    public long InputClockCount { get; private set; }
    public bool ClockE => _clockE;
    public bool ClockQ => _clockQ;
    public bool BusAvailable => _busAvailable;
    public bool BusStatus => _busStatus;
    public long ProcessorCycles => _processorCycles;
    public ushort? BusAddress => _busAddress;
    public bool? BusRead => _busRead;
    public byte? BusOutputData => _busOutputData;
    /// <summary>Adresse, R/W et données en sortie ; null représente la haute impédance.</summary>
    public Action<ushort?, bool?, byte?>? BusPinsChanged { get; set; }
    /// <summary>Niveaux E/Q après chaque changement ; Q précède E d'un quart de cycle.</summary>
    public Action<bool, bool>? ClockOutputsChanged { get; set; }
    /// <summary>Niveaux BA/BS établis au retour E bas, valides à Q montant ; aucun appel sans changement.</summary>
    public Action<bool, bool>? BusStatusChanged { get; set; }

    /// <summary>Assertion logique de /HALT ; prend effet à la fin de l'instruction en cours.</summary>
    public void SetHalt(bool asserted) => _haltInput = asserted;

    public void SetDmaRequest(bool asserted) => _dmaInput = asserted;
    public void SetMemoryReady(bool high) => _memoryReady = high;

    /// <summary>Assertion logique de /RESET ; la largeur minimale reste celle de Motorola.</summary>
    public void SetReset(bool asserted)
    {
        if (_resetInput == asserted) return;
        if (asserted)
        {
            _resetStarted = InputClockCount;
            _interruptsInhibited = true;
        }
        else
        {
            QualifyReset();
            if (!_resetLatched && !_resetSequence) _interruptsInhibited = false;
        }
        _resetInput = asserted;
    }

    private void SetBusStatus(bool available, bool status)
    {
        _nextBusAvailable = available;
        _nextBusStatus = status;
        _busStatusPending = _busAvailable != available || _busStatus != status;
        if (_busStatusPending && !_clockE && !_clockQ)
        {
            PublishBusStatus();
            PublishBusPins(false);
        }
    }

    private void PublishBusStatus()
    {
        _busStatusPending = false;
        var released = !_busAvailable && _nextBusAvailable;
        _busAvailable = _nextBusAvailable;
        _busStatus = _nextBusStatus;
        if (released)
        {
            _busDriversReleased = true;
            _cyclePins?.ReleaseBus();
        }
        BusStatusChanged?.Invoke(_busAvailable, _busStatus);
    }

    private void PublishBusPins(bool memoryCycle)
    {
        var released = _busAvailable || _busDriversReleased;
        ushort? address = released ? null : _nextBusAddress;
        bool? read = released ? null : _nextBusRead;
        byte? data = released ? null : _nextBusOutputData;
        var changed = BusPinsChanged;
        if (changed is null)
        {
            _busAddress = address;
            _busRead = read;
            _busOutputData = data;
        }
        else if (_busAddress != address || _busRead != read || _busOutputData != data)
        {
            _busAddress = address;
            _busRead = read;
            _busOutputData = data;
            changed(address, read, data);
        }
        if (!memoryCycle || _busAvailable || _cyclePins is null) return;
        // A new transfer may use identical pin levels: preparation is a bus
        // transaction, distinct from the change-only physical notification.
        if (_nextBusRead) _cyclePins.PrepareRead(_nextBusAddress);
        else _cyclePins.PrepareWrite(_nextBusAddress, _nextBusOutputData!.Value);
    }

    private void SetClockOutputs(bool e, bool q)
    {
        _clockE = e;
        _clockQ = q;
        ClockOutputsChanged?.Invoke(e, q);
    }

    private void AdvanceSourceClock()
    {
        InputClockCount++;
        if (++_sourceClockPhase == C.InputClockDivider)
        {
            _sourceClockPhase = 0;
            if (_clockBus is null) _bus.Tick();
        }
        _clockBus?.TickInputClock();
        QualifyReset();
    }

    private void QualifyReset()
    {
        if (_resetInput && InputClockCount - _resetStarted > C.InputClockDivider)
            _resetLatched = true;
    }

    private bool StretchClock(bool memoryCycle) => !_memoryReady && !_resetLatched
        && (memoryCycle || _busAvailable && _busStatus);

    private void ClockBusCycle(bool memoryCycle = false, bool dmaCycle = false,
        ushort idleAddress = ushort.MaxValue)
    {
        if (!memoryCycle)
        {
            _nextBusAddress = idleAddress;
            _nextBusRead = true;
            _nextBusOutputData = null;
        }
        if (ClockOutputsChanged is null && _clockBus is null && _memoryReady && _sourceClockPhase == 1)
        {
            // No recipient observes the intermediate clock edges. A bus callback
            // still sees the same E-high/Q-high phase as on the connected path.
            InputClockCount++;
            _clockQ = true;
            if (_busStatusPending) PublishBusStatus();
            PublishBusPins(memoryCycle);
            InputClockCount++;
            _clockE = true;
            InputClockCount++;
            _bus.Tick();
            _clockQ = false;
            if (_interruptClockStages != 0 && !_interruptsInhibited) SampleInterruptInputs();
            InputClockCount++;
            while (StretchClock(memoryCycle)) AdvanceSourceClock();
            _clockE = false;
            QualifyReset();
        }
        else
        {
            AdvanceSourceClock();
            _clockE = false;
            _clockQ = true;
            if (_busStatusPending) PublishBusStatus();
            PublishBusPins(memoryCycle);
            ClockOutputsChanged?.Invoke(false, true);
            AdvanceSourceClock();
            SetClockOutputs(true, true);
            AdvanceSourceClock();
            SetClockOutputs(true, false);
            if (_interruptClockStages != 0 && !_interruptsInhibited) SampleInterruptInputs();
            AdvanceSourceClock();
            // Figure 12: Q has fallen; MRDY extends E high / Q low in complete
            // EXTAL periods. The pending memory transfer is not performed twice.
            while (StretchClock(memoryCycle)) AdvanceSourceClock();
            SetClockOutputs(false, false);
        }
        Cycles++;
        _dmaPending = _dmaInput;
        if (_dmaInput) _dmaInactiveCycles = 0;
        else if (_dmaInactiveCycles < C.DmaCounterClearCycles) _dmaInactiveCycles++;
        if (_dmaInactiveCycles == C.DmaCounterClearCycles) _dmaRefreshCounter = 0;
        else if (dmaCycle) _dmaRefreshCounter = (_dmaRefreshCounter + 1) & C.DmaRefreshMask;
    }

    private void SampleInterruptInputs()
    {
        _irqSampled = _irqClocked;
        _firqSampled = _firqClocked;
        _irqClocked = _irqInput;
        _firqClocked = _firqInput;
        var nmi = _nmiClocked;
        if (nmi && !_nmiLevel) _nmiPending = true;
        _nmiLevel = nmi;
        _nmiClocked = _nmiInput;
        --_interruptClockStages;
    }

    private bool HoldAtInstructionBoundary()
    {
        if (_haltInput)
        {
            if (!_haltAcknowledged) { _haltAcknowledged = true; SetBusStatus(true, true); }
            ClockBusCycle();
            return true;
        }
        if (!_haltAcknowledged) return false;
        _haltAcknowledged = false;
        SetBusStatus(_sync, false);
        // Motorola: a dead cycle follows BA's falling edge before memory access.
        ClockBusCycle();
        return true;
    }

    private void GrantDma()
    {
        if (!_dmaPending || _dmaRefreshCounter == C.DmaRefreshMask) return;
        var available = _nextBusAvailable;
        var status = _nextBusStatus;
        SetBusStatus(true, true);
        ClockBusCycle(dmaCycle: true); // Dead cycle: CPU drivers are released before the grant.
        while (_dmaPending && _dmaRefreshCounter != C.DmaRefreshMask && !_resetLatched)
            ClockBusCycle(dmaCycle: true);
        SetBusStatus(false, false);
        ClockBusCycle(dmaCycle: true); // Dead cycle before the CPU may drive the bus again.
        SetBusStatus(available, status);
    }

    private void ProcessorCycle(bool memoryCycle = false, ushort address = ushort.MaxValue,
        bool read = true, byte? data = null)
    {
        if (!_resetSequence) GrantDma();
        if (_resetLatched) throw new ResetAcceptedException();
        _busDriversReleased = false;
        _nextBusAddress = address;
        _nextBusRead = read;
        _nextBusOutputData = data;
        ClockBusCycle(memoryCycle, idleAddress: address);
        _processorCycles++;
        if (_resetLatched) throw new ResetAcceptedException();
    }

    private bool HoldResetAtBoundary()
    {
        if (!_resetInput && !_resetLatched) return false;
        if (_resetInput || _haltInput)
        {
            if (_haltInput) _haltAcknowledged = true;
            SetBusStatus(_haltInput, _haltInput);
            if (!_haltInput) _busDriversReleased = false;
            ClockBusCycle(idleAddress: C.ResetVector);
        }
        else Reset();
        return true;
    }

    private sealed class ResetAcceptedException : Exception { }
}
