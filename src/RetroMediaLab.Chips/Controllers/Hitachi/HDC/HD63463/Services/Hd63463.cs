using System.Buffers.Binary;
using C = RetroMediaLab.Chips.Controllers.Hitachi.HDC.HD63463.Constants.Hd63463Constants;
using Command = RetroMediaLab.Chips.Controllers.Hitachi.HDC.HD63463.Enums.Hd63463Command;
using Phase = RetroMediaLab.Chips.Controllers.Hitachi.HDC.HD63463.Enums.Hd63463Phase;

namespace RetroMediaLab.Chips.Controllers.Hitachi.HDC.HD63463.Services;

/// <summary>Hitachi HD63463 : port 16 bits, bloc de paramètres et deux buffers de secteur.
/// Le propriétaire avance l'horloge ; les images restent empruntées et ne sont jamais fermées ici.</summary>
public sealed partial class Hd63463
{
    private readonly Func<int, IDiskImage?> _disk;
    private readonly int _clock;
    private readonly byte[] _parameters = new byte[C.ParameterBytes];
    private readonly byte[] _specification = new byte[C.ParameterBytes];
    private readonly byte[][] _buffers = [new byte[C.SectorBytes], new byte[C.SectorBytes]];
    private readonly int[] _cylinders = new int[C.UnitCount];
    private readonly int[] _heads = new int[C.UnitCount];
    private readonly int[] _sectors = new int[C.UnitCount];
    private readonly int[] _recordLengths = new int[C.UnitCount];
    private readonly int[] _tracks = new int[C.UnitCount];
    private readonly int[] _seekDelays = new int[C.UnitCount];
    private byte _validUnits;
    private byte _completedSeeks;
    private readonly bool[] _specified = new bool[C.UnitCount];
    private readonly long[] _seeks = new long[C.UnitCount];
    private readonly long[] _reads = new long[C.UnitCount];
    private readonly long[] _writes = new long[C.UnitCount];
    private readonly int[] _latencies = Enumerable.Repeat(C.DefaultLatencyMilliseconds, C.UnitCount).ToArray();
    private readonly int[] _writeLatencies = Enumerable.Repeat(C.DefaultLatencyMilliseconds, C.UnitCount).ToArray();
    private readonly int[] _speeds = Enumerable.Repeat(C.DefaultSpeedPercent, C.UnitCount).ToArray();
    private readonly int[] _rotationsPerMinute = Enumerable.Repeat(C.DefaultRotationsPerMinute, C.UnitCount).ToArray();
    private byte _status;
    private byte _interruptMode;
    private Command _command;
    private Phase _phase;
    private int _parameterRead;
    private int _parameterWrite;
    private int _buffer;
    private int _bufferOffset;
    private int _unit;
    private int _cylinder;
    private int _head;
    private int _sector;
    private int _remaining;
    private int _delay;
    private int _activity;
    private bool _drq;
    private bool _irq;
    private bool _polling;
    private bool _selected;
    private bool _initialized;
    private bool _testMode;

    public Hd63463(Func<int, IDiskImage?> disk, int ticksPerSecond = C.DefaultClockHz)
    {
        ArgumentNullException.ThrowIfNull(disk);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ticksPerSecond);
        _disk = disk;
        _clock = ticksPerSecond;
        Reset();
    }

    public Action<bool>? InterruptChanged { get; set; }
    public Action<bool>? DataRequestChanged { get; set; }
    public bool DriveOutputsEnabled => !_testMode;
    public bool Irq => _irq;
    public bool Drq => _drq;
    public bool Busy => (_status & C.Busy) != 0;
    public int Unit => _unit;
    public byte Status => _status;
    public bool Writing => _phase is Phase.Writing or Phase.Formatting or Phase.ReceivingFormatIdentifiers;
    public bool Active(int unit) => _seekDelays[CheckedUnit(unit)] > 0 || (_selected && unit == _unit && (_activity > 0 || Busy));
    public long SeekCount(int unit) => _seeks[CheckedUnit(unit)];
    public long ReadBlocks(int unit) => _reads[CheckedUnit(unit)];
    public long WrittenBlocks(int unit) => _writes[CheckedUnit(unit)];
    private IDiskImage? Image => _disk(_unit);

    public void MediaChanged(int unit)
    {
        CheckedUnit(unit);
        if (_disk(unit) is not null) return;
        _validUnits &= (byte)~UnitBit(unit);
        _completedSeeks &= (byte)~UnitBit(unit);
        _seekDelays[unit] = 0;
        if (_polling && _specified[unit]) EndPolling(unit, C.NotReady);
        else if (_selected && _unit == unit && Busy) Finish(C.NotReady);
    }

    private static int CheckedUnit(int unit)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(unit);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(unit, C.UnitCount);
        return unit;
    }

    public void Configure(int unit, int latencyMilliseconds, int speedPercent, int? writeLatencyMilliseconds = null, int rotationsPerMinute = C.DefaultRotationsPerMinute)
    {
        CheckedUnit(unit);
        ArgumentOutOfRangeException.ThrowIfNegative(latencyMilliseconds);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(latencyMilliseconds, C.MaximumLatencyMilliseconds);
        ArgumentOutOfRangeException.ThrowIfNegative(speedPercent);
        var writeLatency = writeLatencyMilliseconds ?? latencyMilliseconds;
        ArgumentOutOfRangeException.ThrowIfNegative(writeLatency);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(writeLatency, C.MaximumLatencyMilliseconds);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rotationsPerMinute);
        _latencies[unit] = latencyMilliseconds;
        _writeLatencies[unit] = writeLatency;
        _speeds[unit] = speedPercent;
        _rotationsPerMinute[unit] = rotationsPerMinute;
        _rotationRates[unit] = (long)rotationsPerMinute * speedPercent;
    }

    public void Reset()
    {
        _status = _interruptMode = 0;
        _command = 0;
        _phase = Phase.Idle;
        _parameterRead = _parameterWrite = _buffer = _bufferOffset = 0;
        _unit = _cylinder = _head = _sector = _remaining = _delay = _activity = 0;
        _polling = _selected = _initialized = _testMode = false;
        _timeoutClockPhase = 0;
        _identifierIndex = _identifierDmaBytes = _identifierSectors = 0;
        _formatDmaBytes = _formatRecordCount = 0;
        Array.Clear(_parameters);
        Array.Clear(_specification);
        Array.Clear(_specified);
        Array.Clear(_tracks);
        Array.Fill(_recordLengths, C.SectorBytes);
        ResetRecord();
        ResetErrorCode();
        Array.Clear(_seekDelays);
        _validUnits = _completedSeeks = 0;
        foreach (var buffer in _buffers) Array.Clear(buffer);
        _phase = Phase.Resetting;
        _status = C.Busy;
        _delay = ControllerCycles(C.ResetCycles);
        SetDrq(false);
        UpdateInterrupt();
    }

    /// <summary>Les paramètres utilisent l'octet de poids fort avant l'octet de poids faible.</summary>
    public void WriteParameter(ushort value)
    {
        if (_testMode) return;
        if (_phase == Phase.OpenWriteBuffer)
        {
            WriteBufferWord(value);
            return;
        }
        if (Busy || _phase == Phase.OpenReadBuffer || _parameterWrite > C.ParameterBytes - C.ParameterWordBytes)
        {
            _status |= C.ParameterReject;
            return;
        }
        BinaryPrimitives.WriteUInt16BigEndian(_parameters.AsSpan(_parameterWrite), value);
        _parameterWrite += C.ParameterWordBytes;
        if (_parameterWrite == C.ParameterBytes) _status |= C.ParameterReject;
    }

    public ushort ReadParameter()
    {
        if (_phase == Phase.OpenReadBuffer) return ReadBufferWord();
        if (_phase == Phase.OpenWriteBuffer) return ushort.MaxValue;
        if (_parameterRead > C.ParameterBytes - C.ParameterWordBytes) return ushort.MaxValue;
        var value = BinaryPrimitives.ReadUInt16BigEndian(_parameters.AsSpan(_parameterRead));
        _parameterRead += C.ParameterWordBytes;
        return value;
    }

    public ushort ReadDma(bool done = false)
    {
        if (!_drq || _phase is not (Phase.Reading or Phase.ReadBuffer or Phase.ReadIdentifierBuffer)) return ushort.MaxValue;
        var cycleSteal = (_specification[C.ModeParameter] & C.DmaBurst) == 0;
        if (cycleSteal) SetDrq(false);
        try
        {
            if (_phase == Phase.ReadIdentifierBuffer)
            {
                var identifier = ReadIdentifierWord();
                if (done && Busy) Finish();
                return identifier;
            }
            if (_phase == Phase.ReadBuffer)
            {
                var buffered = ReadBufferWord();
                if (done) CompleteBufferDma();
                return buffered;
            }
            return ReadRecordWord(done);
        }
        finally
        {
            if (cycleSteal && DmaRequestPending) SetDrq(true);
        }
    }

    public void WriteDma(ushort value, bool done = false)
    {
        if (!_drq || _phase is not (Phase.Writing or Phase.Comparing or Phase.ReceivingFormatIdentifiers or Phase.WriteBuffer)) return;
        var cycleSteal = (_specification[C.ModeParameter] & C.DmaBurst) == 0;
        if (cycleSteal) SetDrq(false);
        try
        {
            if (_phase == Phase.ReceivingFormatIdentifiers)
            {
                WriteFormatIdentifierWord(value, done);
                return;
            }
            if (_phase == Phase.WriteBuffer)
            {
                WriteBufferWord(value);
                if (done) CompleteBufferDma();
                return;
            }
            BinaryPrimitives.WriteUInt16LittleEndian(_buffers[_buffer].AsSpan(_bufferOffset), value);
            _bufferOffset += C.ParameterWordBytes;
            if (_bufferOffset == C.SectorBytes || done) QueueWriteBuffer(done);
        }
        finally
        {
            if (cycleSteal && DmaRequestPending) SetDrq(true);
        }
    }

    private void SetDrq(bool value)
    {
        if (_drq == value) return;
        _drq = value;
        DataRequestChanged?.Invoke(value);
    }

    private void UpdateInterrupt()
    {
        var value = (_status & ~_interruptMode & C.InterruptMask) != 0;
        if (_irq == value) return;
        _irq = value;
        InterruptChanged?.Invoke(value);
    }

    /// <summary>Avance directement jusqu'aux événements, sans boucler sur chaque cycle de bus.</summary>
    public void Advance(int ticks)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(ticks);
        _activity = Math.Max(0, _activity - ticks);
        Span<bool> completed = stackalloc bool[C.UnitCount];
        while (ticks > 0)
        {
            var elapsed = _delay > 0 ? Math.Min(ticks, _delay) : ticks;
            if (_searchTimeout > 0) elapsed = Math.Min(elapsed, _searchTimeout);
            for (var unit = 0; unit < C.UnitCount; unit++)
                if (_seekDelays[unit] > 0) elapsed = Math.Min(elapsed, _seekDelays[unit]);
            var commandEnded = _delay > 0 && _delay == elapsed;
            var searchExpired = _searchTimeout > 0 && _searchTimeout == elapsed;
            if (_delay > 0) _delay -= elapsed;
            if (_searchTimeout > 0) _searchTimeout -= elapsed;
            for (var unit = 0; unit < C.UnitCount; unit++)
            {
                completed[unit] = _seekDelays[unit] > 0 && _seekDelays[unit] == elapsed;
                if (_seekDelays[unit] > 0) _seekDelays[unit] -= elapsed;
            }
            ticks -= elapsed;
            AdvanceRotation(elapsed);
            AdvanceSearchClock(elapsed);
            if (searchExpired) Finish(C.Timeout);
            else if (commandEnded) ExecuteEvent();
            for (var unit = 0; unit < C.UnitCount; unit++)
                if (completed[unit]) DriveSeekCompleted(unit);
        }
    }

    private int ControllerCycles(int cycles) =>
        (int)Math.Clamp(((long)cycles * _clock + C.ControllerClockHz - 1) / C.ControllerClockHz, 1, int.MaxValue);

    private int Delay(int microseconds)
    {
        var speed = _speeds[_unit];
        return speed == 0 ? 1 : (int)Math.Clamp((long)_clock * microseconds * C.DefaultSpeedPercent
            / C.MicrosecondsPerSecond / speed, 1, int.MaxValue);
    }
}
