using R = RetroMediaLab.Chips.Timers.Hitachi.HD1468xx.HD146818.Constants.Hd146818Constants;
using RetroMediaLab.Chips.Timers.Hitachi.HD1468xx.HD146818.Enums;
using RetroMediaLab.Chips.Timers.Hitachi.HD1468xx.HD146818.Interfaces;

namespace RetroMediaLab.Chips.Timers.Hitachi.HD1468xx.HD146818.Services;

/// <summary>Registres, diviseurs et calendrier Hitachi. Aucune horloge de l'hôte dans le circuit.</summary>
public sealed partial class Hd146818 : IHd146818
{
    private readonly byte[] _registers = new byte[R.RegisterCount];
    private bool _reset;
    private bool _powerSense;
    private bool _irq;
    private bool _squareWave;
    private bool _clockOut;
    private bool _clockFrequencySelect;
    private Action<bool>? _squareWaveChanged;
    private Action<bool>? _clockOutChanged;
    private bool _updateInProgress;
    private long _nextUip;
    private long _updateEnd = long.MaxValue;

    public Hd146818(long referenceFrequency = R.LowFrequency, int oscillatorFrequency = R.LowFrequency,
        Hd146818Variant variant = Hd146818Variant.Hd146818)
    {
        if (referenceFrequency <= 0) throw new ArgumentOutOfRangeException(nameof(referenceFrequency));
        if (oscillatorFrequency <= 0 || oscillatorFrequency > int.MaxValue / R.HalfClocksPerCycle)
            throw new ArgumentOutOfRangeException(nameof(oscillatorFrequency));
        if (!Enum.IsDefined(variant)) throw new ArgumentOutOfRangeException(nameof(variant));
        ReferenceFrequency = referenceFrequency;
        ClockFrequency = oscillatorFrequency;
        Variant = variant;
        RestartUpdatePhase();
    }

    public long ReferenceFrequency { get; }
    public int ClockFrequency { get; }
    public Hd146818Variant Variant { get; }
    public bool Irq => _irq;
    public bool SquareWave => _squareWave;
    public bool ClockOut => _clockOut;
    public bool OscillatorHigh => (_halfClocks & 1) != 0;
    public bool CalendarDataAvailable => _updateEnd == long.MaxValue || _halfClocks < _updateEnd - UpdateHalfClocks;
    public Action<bool>? IrqChanged { get; set; }
    public Action<bool>? SquareWaveChanged
    {
        get => _squareWaveChanged;
        set { _squareWaveChanged = value; PublishNextClockEvent(); }
    }
    public Action<bool>? ClockOutChanged
    {
        get => _clockOutChanged;
        set { _clockOutChanged = value; PublishNextClockEvent(); }
    }
    public Action<long?>? NextClockEventChanged { get; set; }
    private int Divider => (_registers[R.StatusA] & R.DividerMask) >> R.DividerShift;
    private bool Running => Divider <= R.LowFrequencyDivider;
    private bool UpdatesEnabled => Running && (_registers[R.StatusB] & R.StopUpdates) == 0;
    private int DividerPeriod => Divider switch { 0 => R.HighFrequency, 1 => R.MediumFrequency, _ => R.LowFrequency };
    private int WarningHalfClocks => DividerPeriod * R.HalfClocksPerCycle / R.WarningDivisor;
    private int UpdateHalfClocks => R.HalfClocksPerCycle * (Divider switch
    {
        0 => R.HighUpdateCycles,
        1 => R.MediumUpdateCycles,
        _ => R.LowUpdateCycles
    });
    private long PeriodicHalfPeriod
    {
        get
        {
            var rate = _registers[R.StatusA] & R.LowNibble;
            if (rate == 0 || !Running) return 0;
            if (Divider == R.LowFrequencyDivider && rate <= 2) rate += 7;
            return (long)DividerPeriod * R.HalfClocksPerCycle * (1L << (rate - 1)) / R.LowFrequency;
        }
    }

    public byte ReadRegister(int address)
    {
        address &= R.AddressMask;
        switch (address)
        {
            case R.StatusA:
                return (byte)(_registers[address] | (_updateInProgress ? R.UpdateInProgress : 0));
            case R.StatusC:
                var causes = _registers[address];
                _registers[address] = 0;
                UpdateIrq();
                PublishNextClockEvent();
                return causes;
            case R.StatusD:
                if (_powerSense) _registers[address] = R.ValidRam;
                return _registers[address];
            default:
                // During the disconnected-calendar interval these data are unspecified by Hitachi.
                // Returning the stored bytes is one deterministic value, not a guaranteed stable read.
                return _registers[address];
        }
    }

    public void WriteRegister(int address, byte value)
    {
        address &= R.AddressMask;
        if (address is R.StatusC or R.StatusD || address < R.StatusA && !CalendarDataAvailable) return;
        var previous = _registers[address];
        if (address is R.Seconds or R.StatusA) value &= R.SevenBits;
        if (address == R.StatusB)
        {
            if ((value & R.StopUpdates) != 0 && Variant != Hd146818Variant.Hd146818A)
                value &= unchecked((byte)~R.UpdateInterruptEnable);
            if (_reset) value &= unchecked((byte)~R.ResetClearedEnables);
        }
        _registers[address] = value;
        if (address == R.StatusA)
        {
            var previousDivider = (previous & R.DividerMask) >> R.DividerShift;
            if (Divider != previousDivider)
            {
                _dividerOrigin = _halfClocks;
                AbortUpdate();
                RestartUpdatePhase();
            }
            PublishClockOutputs();
            PublishNextClockEvent();
        }
        if (address == R.StatusB)
        {
            if ((value & R.StopUpdates) != 0) AbortUpdate();
            if ((previous & R.StopUpdates) != 0 && (value & R.StopUpdates) == 0)
            {
                PrepareCalendarInitialization();
                RestartUpdatePhase();
            }
            UpdateIrq();
            PublishClockOutputs();
            PublishNextClockEvent();
        }
    }

    private void RestartUpdatePhase()
    {
        // Figure 10/15: first UIP at 500 ms minus one half-period of the 32768-Hz stage (15.26 us).
        _nextUip = _halfClocks + DividerPeriod - DividerPeriod / R.LowFrequency;
    }

    private void AbortUpdate()
    {
        _updateInProgress = false;
        _updateEnd = long.MaxValue;
    }

    private void CompleteUpdate()
    {
        IncrementCalendar();
        if (!_reset)
        {
            _registers[R.StatusC] |= R.UpdateFlag;
            if (AlarmMatches()) _registers[R.StatusC] |= R.AlarmFlag;
        }
        AbortUpdate();
        _nextUip += (long)DividerPeriod * R.HalfClocksPerCycle;
    }

    private void UpdateIrq()
    {
        var active = !_reset && (_registers[R.StatusC] & _registers[R.StatusB] & R.InterruptCauses) != 0;
        _registers[R.StatusC] = (byte)((_registers[R.StatusC] & R.InterruptCauses) | (active ? R.InterruptFlag : 0));
        if (active == _irq) return;
        _irq = active;
        IrqChanged?.Invoke(active);
    }

    public void SetReset(bool asserted)
    {
        if (_reset == asserted) return;
        _reset = asserted;
        if (asserted)
        {
            _registers[R.StatusB] &= unchecked((byte)~R.ResetClearedEnables);
            _registers[R.StatusC] = 0;
        }
        UpdateIrq();
        PublishClockOutputs();
        PublishNextClockEvent();
    }

    public void SetPowerSense(bool high)
    {
        _powerSense = high;
        if (!high) _registers[R.StatusD] = 0;
    }

    public byte[] Export() => _registers.ToArray();

    public void Import(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != R.RegisterCount) throw new ArgumentException(nameof(bytes));
        for (var address = 0; address < R.RegisterCount; address++) WriteRegister(address, bytes[address]);
    }
}
