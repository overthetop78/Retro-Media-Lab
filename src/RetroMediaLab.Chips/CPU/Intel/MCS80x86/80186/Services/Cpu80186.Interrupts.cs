using I = RetroMediaLab.Chips.CPU.Intel.MCS80x86.Chip80186.Constants.Interrupt80186Constants;
using P = RetroMediaLab.Chips.CPU.Intel.MCS80x86.Chip80186.Constants.Peripheral80186Constants;

namespace RetroMediaLab.Chips.CPU.Intel.MCS80x86.Chip80186.Services;

public sealed partial class Cpu80186
{
    private readonly ushort[] _interruptControls = new ushort[I.SourceCount];
    private ushort _interruptRequest;
    private ushort _interruptStatus;
    private ushort _interruptInService;
    private ushort _interruptPriorityMask;
    private byte _interruptVectorBase;
    private byte _lastPollVector;
    private byte _externalInterruptLevels;
    public Func<int, byte>? CascadeAcknowledge { get; set; }
    public Func<byte>? ExternalInterruptAcknowledge { get; set; }
    private bool SlaveInterruptMode => (_peripheralRelocation & P.SlaveInterruptMode) != 0;

    private void ResetInterruptController()
    {
        Array.Fill(_interruptControls, I.ControlBits);
        _interruptRequest = _interruptStatus = _interruptInService = 0;
        _interruptPriorityMask = I.PriorityBits;
        _interruptVectorBase = _lastPollVector = _externalInterruptLevels = 0;
    }

    public void SetExternalInterrupt(int pin, bool asserted)
    {
        var level = 1 << pin;
        var previous = (_externalInterruptLevels & level) != 0;
        _externalInterruptLevels = (byte)(asserted ? _externalInterruptLevels | level : _externalInterruptLevels & ~level);
        if (SlaveInterruptMode || previous == asserted) return;
        var mask = I.FirstExternalRequest << pin;
        if (asserted) _interruptRequest |= (ushort)mask;
        else _interruptRequest &= (ushort)~mask;
    }

    private int InterruptSourceAt(int ordinal) => SlaveInterruptMode
        ? ordinal switch { 0 => I.TimerSource, 1 => I.FirstExternalSource, 2 => I.FirstExternalSource + 1, _ => ordinal - 2 }
        : ordinal;

    private int InterruptSourceCount => SlaveInterruptMode ? I.SlaveSourceCount : I.SourceCount;

    private ushort InterruptSourceMask(int source) => source == I.TimerSource ? I.TimerRequest
        : source < I.FirstExternalSource ? (ushort)(I.FirstDmaRequest << (SlaveInterruptMode ? 2 - source : source - I.FirstDmaSource))
        : (ushort)(I.FirstExternalRequest << (source - I.FirstExternalSource));

    private int InterruptTimer(int source) => source == I.TimerSource ? 0 : source - I.FirstExternalSource + 1;

    private bool IsTimerInterruptSource(int source) => source == I.TimerSource || SlaveInterruptMode && source >= I.FirstExternalSource;

    private bool InterruptSourceRequested(int source)
    {
        if (IsTimerInterruptSource(source)) return (_interruptStatus & (SlaveInterruptMode ? 1 << InterruptTimer(source) : I.TimerStatusBits)) != 0;
        return (_interruptRequest & InterruptSourceMask(source)) != 0;
    }

    private byte SourceVector(int source, int priority)
    {
        if (SlaveInterruptMode) return (byte)(_interruptVectorBase | priority);
        if (source == I.TimerSource) return (_interruptStatus & 1) != 0 ? I.Timer0Vector
            : (_interruptStatus & 2) != 0 ? I.Timer1Vector : I.Timer2Vector;
        return source < I.FirstExternalSource ? (byte)(I.FirstDmaVector + source - I.FirstDmaSource)
            : (byte)(I.FirstExternalVector + source - I.FirstExternalSource);
    }

    private int SelectIntegratedInterrupt(out byte vector)
    {
        vector = _lastPollVector;
        if ((_interruptRequest | (_interruptStatus & I.TimerStatusBits) | _interruptInService) == 0) return -1;
        for (var priority = 0; priority <= _interruptPriorityMask; priority++)
        {
            for (var ordinal = 0; ordinal < InterruptSourceCount; ordinal++)
            {
                var source = InterruptSourceAt(ordinal);
                var control = _interruptControls[source];
                if ((control & I.ControlBits) != priority) continue;
                var servicing = (_interruptInService & InterruptSourceMask(source)) != 0;
                var nestedCascade = !SlaveInterruptMode && source >= I.FirstExternalSource && (control & I.SpecialFullyNested) != 0;
                if (servicing && !nestedCascade) return -1;
                if (InterruptSourceRequested(source))
                {
                    var external = source - I.FirstExternalSource;
                    if (!SlaveInterruptMode && external >= I.CascadeCount &&
                        (_interruptControls[source - I.CascadeCount] & I.Cascade) != 0) continue;
                    vector = SourceVector(source, priority);
                    return source;
                }
                if (servicing) return -1;
            }
        }
        return -1;
    }

    private byte AcknowledgeIntegratedInterrupt(int source, byte vector)
    {
        var mask = InterruptSourceMask(source);
        _interruptInService |= mask;
        if (IsTimerInterruptSource(source))
        {
            var timer = SlaveInterruptMode ? InterruptTimer(source) : vector == I.Timer0Vector ? 0 : vector == I.Timer1Vector ? 1 : 2;
            _interruptStatus &= (ushort)~(1 << timer);
        }
        else if (source < I.FirstExternalSource || (_interruptControls[source] & I.LevelTriggered) == 0)
            _interruptRequest &= (ushort)~mask;
        _lastPollVector = vector;
        if (!SlaveInterruptMode && source >= I.FirstExternalSource &&
            (_interruptControls[source] & I.Cascade) != 0)
            return CascadeAcknowledge?.Invoke(source - I.FirstExternalSource) ?? byte.MaxValue;
        return vector;
    }

    private bool TryAcknowledgeMaskableInterrupt(out byte vector)
    {
        var source = SelectIntegratedInterrupt(out vector);
        if (source >= 0)
        {
            vector = AcknowledgeIntegratedInterrupt(source, vector);
            return true;
        }
        if (SlaveInterruptMode && (_externalInterruptLevels & 1) != 0)
        {
            vector = ExternalInterruptAcknowledge?.Invoke() ?? InterruptVector;
            return true;
        }
        vector = InterruptVector;
        return InterruptPending;
    }

    private void IntegratedEndOfInterrupt(ushort value)
    {
        var specific = !SlaveInterruptMode && (value & I.NonSpecificEoi) == 0;
        for (var priority = 0; priority <= I.PriorityBits; priority++)
        {
            for (var ordinal = 0; ordinal < InterruptSourceCount; ordinal++)
            {
                var source = InterruptSourceAt(ordinal);
                if ((_interruptControls[source] & I.PriorityBits) != priority || (_interruptInService & InterruptSourceMask(source)) == 0) continue;
                if (SlaveInterruptMode && priority != (value & I.PriorityBits)) continue;
                if (specific)
                {
                    var requestedVector = value & I.EoiVectorMask;
                    var matches = source == I.TimerSource
                        ? requestedVector is I.Timer0Vector or I.Timer1Vector or I.Timer2Vector
                        : requestedVector == SourceVector(source, priority);
                    if (!matches) continue;
                }
                _interruptInService &= (ushort)~InterruptSourceMask(source);
                return;
            }
        }
    }

    private ushort ReadInterruptRegister(int offset)
    {
        if (offset >= I.TimerControl)
        {
            var source = (offset - I.TimerControl) / I.RegisterStride;
            return SlaveInterruptMode && source >= I.FirstExternalSource + I.CascadeCount ? (ushort)0 : _interruptControls[source];
        }
        switch (offset)
        {
            case I.Vector: return _interruptVectorBase;
            case I.Poll:
            case I.PollStatus:
                var source = SelectIntegratedInterrupt(out var vector);
                var result = source < 0 ? vector : (ushort)(I.PollPending | vector);
                if (source >= 0 && offset == I.Poll) AcknowledgeIntegratedInterrupt(source, vector);
                return result;
            case I.Mask:
                ushort mask = 0;
                for (var ordinal = 0; ordinal < InterruptSourceCount; ordinal++)
                {
                    var index = InterruptSourceAt(ordinal);
                    if ((_interruptControls[index] & I.MaskBit) != 0) mask |= InterruptSourceMask(index);
                }
                return mask;
            case I.PriorityMask: return _interruptPriorityMask;
            case I.InService: return _interruptInService;
            case I.Request: return (ushort)(_interruptRequest | ((_interruptStatus & I.TimerStatusBits) != 0 ? I.TimerRequest : 0));
            case I.Status: return _interruptStatus;
            default: return 0;
        }
    }

    private void WriteInterruptRegister(int offset, ushort value)
    {
        if (offset >= I.TimerControl)
        {
            var source = (offset - I.TimerControl) / I.RegisterStride;
            if (SlaveInterruptMode && source >= I.FirstExternalSource + I.CascadeCount) return;
            _interruptControls[source] = (ushort)(value & (SlaveInterruptMode || source < I.FirstExternalSource ? I.ControlBits
                : source < I.FirstExternalSource + I.CascadeCount ? I.CascadedControlBits : I.ExternalControlBits));
            return;
        }
        switch (offset)
        {
            case I.Vector: _interruptVectorBase = (byte)(value & I.VectorBaseMask); break;
            case I.EndOfInterrupt: IntegratedEndOfInterrupt(value); break;
            case I.Mask:
                for (var ordinal = 0; ordinal < InterruptSourceCount; ordinal++)
                {
                    var source = InterruptSourceAt(ordinal);
                    _interruptControls[source] = (ushort)((_interruptControls[source] & ~I.MaskBit) | ((value & InterruptSourceMask(source)) != 0 ? I.MaskBit : 0));
                }
                break;
            case I.PriorityMask: _interruptPriorityMask = (ushort)(value & I.PriorityBits); break;
            case I.InService: _interruptInService = (ushort)(value & I.SourceBits); break;
            case I.Request: _interruptRequest = (ushort)((_interruptRequest & ~I.DmaRequests) | (value & I.DmaRequests)); break;
            case I.Status: _interruptStatus = (ushort)((_interruptStatus & ~I.WritableStatus) | (value & I.WritableStatus)); break;
        }
    }
}
