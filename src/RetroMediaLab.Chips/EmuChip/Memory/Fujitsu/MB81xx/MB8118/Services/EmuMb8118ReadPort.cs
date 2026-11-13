using RetroMediaLab.Chips.EmuChip.Common.Contracts;
using System.Numerics;
using RetroMediaLab.Chips.Logic.Common.Enums;
using RetroMediaLab.Chips.Memory.Fujitsu.MB81xx.MB8118.Contracts;

namespace RetroMediaLab.Chips.EmuChip.Memory.Fujitsu.MB81xx.MB8118.Services;

/// <summary>A wired byte port over eight distinct one-bit memories. The
/// shared object contains received input history and output projections only;
/// cells, retention and physical states remain owned by the eight memories.</summary>
public sealed class EmuMb8118ReadPort : IDisposable
{
    private readonly EmuMb8118[] _owners;
    private readonly Mb8118ReadView[] _views = new Mb8118ReadView[8];
    private readonly long[] _refreshes = new long[128], _minimum = new long[128], _dataChanges = new long[8];
    private Mb8118ReadProgram _current, _completed;
    private bool _active, _hasCurrent, _hasCompleted, _withdrawing, _disposed;
    private byte _currentRowData, _currentRowKnown, _currentColumnData, _currentColumnKnown;
    private byte _completedColumnData, _completedColumnKnown;
    private byte _currentLevels, _currentKnown, _completedLevels, _completedKnown;
    private byte _address, _data, _known;
    private bool _addressKnown;
    private long _addressChanged = -1;
    private long _projectedReads, _canonicalReceives, _epochs, _byteReadPrograms, _laneOutputRecordsEliminated;
    public long ByteReadPrograms => _byteReadPrograms;
    public long LaneOutputRecordsEliminated => _laneOutputRecordsEliminated;
    public long ProjectedReads => _projectedReads;
    public long CanonicalReceives => _canonicalReceives;
    public long Epochs => _epochs;
    internal long CurrentRowTime => _current.RowTime;
    internal bool HasCompletedRead => _hasCompleted;
    internal Mb8118ReadProgram CompletedRead => _completed;
    internal Mb8118ReadProgram CurrentRead => _current;
    internal ReadOnlySpan<long> CompletedRefreshes => _refreshes;
    internal ReadOnlySpan<long> CompletedDataChanges => _dataChanges;
    internal long AddressChanged => _addressChanged;
    internal bool? CompletedColumnData(int lane) => Data(_completedColumnData, _completedColumnKnown, lane);
    internal bool? CurrentRowData(int lane) => Data(_currentRowData, _currentRowKnown, lane);
    internal bool? CurrentColumnData(int lane) => Data(_currentColumnData, _currentColumnKnown, lane);
    internal LogicPinState CompletedValue(int lane) => Value(_completedLevels, _completedKnown, lane);

    public EmuMb8118ReadPort(ReadOnlySpan<EmuMb8118> owners)
    {
        if (owners.Length != 8) throw new ArgumentException("A byte port requires eight distinct one-bit owners.", nameof(owners));
        _owners = owners.ToArray();
        var unique = new HashSet<EmuMb8118>(ReferenceEqualityComparer.Instance);
        foreach (var owner in _owners)
        {
            ArgumentNullException.ThrowIfNull(owner);
            if (!unique.Add(owner) || !ReferenceEquals(owner.ReadTimingIdentity, _owners[0].ReadTimingIdentity))
                throw new ArgumentException("The input grid requires distinct owners with the same timing profile.", nameof(owners));
        }
    }

    public bool ReceiveReadProgram(in Mb8118ReadProgram input, byte rowData, byte rowKnown,
        byte columnData, byte columnKnown, Span<Mb8118ReadOutputProgram> outputs)
        => ReceiveReadCore(in input, rowData, rowKnown, columnData, columnKnown, outputs, false, out _);

    public bool ReceiveByteReadProgram(in Mb8118ReadProgram input, byte rowData, byte rowKnown,
        byte columnData, byte columnKnown, Span<Mb8118ReadOutputProgram> fallback,
        out EmuReleasedByteRead? byteRead)
        => ReceiveReadCore(in input, rowData, rowKnown, columnData, columnKnown, fallback, true, out byteRead);

    private bool ReceiveReadCore(in Mb8118ReadProgram input, byte rowData, byte rowKnown,
        byte columnData, byte columnKnown, Span<Mb8118ReadOutputProgram> outputs,
        bool useByteResponse, out EmuReleasedByteRead? byteRead)
    {
        byteRead = null;
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_withdrawing) throw new InvalidOperationException("Cannot replace a read stream during materialisation.");
        if (outputs.Length < 8 || !ReferenceEquals(input.Profile, _owners[0].ReadTimingIdentity))
            throw new ArgumentException("The received read grid does not match the byte port.");
        if (_active) ArgumentOutOfRangeException.ThrowIfLessThan(input.RowTime, _current.RowTime);
        else foreach (var owner in _owners)
            ArgumentOutOfRangeException.ThrowIfLessThan(input.RowTime, owner.ReadInputTime);
        if (_active && input.RowTime < _current.CasRise)
            throw new InvalidOperationException("The preceding read inputs have not released CAS.");

        bool grammar = input.Predictable && input.CompleteReadTimingQualified && input.ReadAddressKnown;
        if (_active && (!grammar || !_owners[0].FollowsRead(in _current, in input) || !Fresh(in input))) Withdraw();
        if (!_active && grammar) TryBegin(in input);
        if (!_active)
        {
            bool accepted = true;
            for (int lane = 0; lane < 8; lane++)
            {
                _canonicalReceives++;
                accepted &= _owners[lane].ReceiveReadProgram(in input, Data(rowData,rowKnown,lane),
                    Data(columnData,columnKnown,lane), out outputs[lane]);
            }
            return accepted;
        }

        bool releasedByte = useByteResponse && _hasCurrent && input.RowTime >= _current.ReleaseTime;
        byte levels = 0, known = 0;
        for (int lane = 0; lane < 8; lane++)
        {
            var value = (LogicPinState)_views[lane].Cells.Span[input.ReadAddress];
            if (value == LogicPinState.High) levels |= (byte)(1 << lane);
            if (value is LogicPinState.Low or LogicPinState.High) known |= (byte)(1 << lane);
            if (releasedByte) continue;
            LogicPinState initial;
            long release;
            if (_hasCurrent)
            {
                initial = input.RowTime >= _current.ReleaseTime ? LogicPinState.HighImpedance : Value(_currentLevels,_currentKnown,lane);
                release = initial == LogicPinState.HighImpedance ? -1 : _current.ReleaseTime;
            }
            else { initial = _views[lane].Output; release = _views[lane].Release; }
            outputs[lane] = new(initial, release, LogicPinState.HighImpedance,
                input.ResponseTime, value, input.ReleaseTime);
        }
        if (releasedByte)
        {
            byteRead = new EmuReleasedByteRead(input.ResponseTime, input.ReleaseTime, levels, known);
            _byteReadPrograms++; _laneOutputRecordsEliminated += 8;
        }
        if (_hasCurrent) CommitCurrent();
        _current = input; _currentRowData = rowData; _currentRowKnown = rowKnown;
        _currentColumnData = columnData; _currentColumnKnown = columnKnown;
        _currentLevels = levels; _currentKnown = known; _hasCurrent = true;
        _projectedReads++;
        if (_epochsJustOpened)
            for (int lane = 0; lane < 8; lane++) _owners[lane].AttachReadPort(this);
        _epochsJustOpened = false;
        return true;
    }

    private bool _epochsJustOpened;
    private void TryBegin(in Mb8118ReadProgram input)
    {
        for (int lane = 0; lane < 8; lane++)
        {
            if (!_owners[lane].PrepareReadView(in input, out _views[lane])) return;
            if (lane != 0 && (_views[lane].Address != _views[0].Address || _views[lane].AddressKnown != _views[0].AddressKnown)) return;
        }
        Array.Fill(_refreshes, -1); Array.Fill(_minimum, long.MinValue); Array.Fill(_dataChanges, -1);
        _hasCurrent = _hasCompleted = false;
        if (!Fresh(in input)) return;
        _address = _views[0].Address; _addressKnown = _views[0].AddressKnown;
        _addressChanged = -1; _data = _known = 0;
        for (int lane = 0; lane < 8; lane++)
        {
            if (_views[lane].Data.HasValue) _known |= (byte)(1 << lane);
            if (_views[lane].Data == true) _data |= (byte)(1 << lane);
        }
        _active = true; _epochsJustOpened = true; _epochs++;
    }

    private bool Fresh(in Mb8118ReadProgram input)
    {
        int row = input.Row;
        long latest = _hasCurrent && _current.Row == row ? _current.RasFall : _refreshes[row];
        if (latest < 0)
        {
            latest = _minimum[row];
            if (latest == long.MinValue)
            {
                latest = long.MaxValue;
                for (int lane = 0; lane < 8; lane++) latest = Math.Min(latest, _views[lane].Refreshed.Span[row]);
                _minimum[row] = latest;
            }
        }
        return latest >= 0 && input.RasFall - latest <= 2_000_000;
    }

    private void CommitCurrent()
    {
        _refreshes[_current.Row] = _current.RasFall;
        TrackInput(_current.Row, _currentRowData, _currentRowKnown, _current.RowTime);
        TrackInput(_current.Column, _currentColumnData, _currentColumnKnown, _current.CasFall);
        _completed = _current; _completedLevels = _currentLevels; _completedKnown = _currentKnown;
        _completedColumnData = _currentColumnData; _completedColumnKnown = _currentColumnKnown;
        _hasCompleted = true;
    }

    private void TrackInput(byte address, byte data, byte known, long time)
    {
        if (!_addressKnown || _address != address) _addressChanged = time;
        _address = address; _addressKnown = true;
        data &= known;
        uint changed = (uint)((_data ^ data) | (_known ^ known));
        while (changed != 0)
        {
            int lane = BitOperations.TrailingZeroCount(changed); changed &= changed - 1;
            _dataChanges[lane] = time;
        }
        _data = data; _known = known;
    }

    public void Withdraw()
    {
        if (!_active || _withdrawing) return;
        _withdrawing = true; _active = false;
        foreach (var owner in _owners) owner.DetachReadPort(this);
        try
        {
            for (int lane = 0; lane < 8; lane++)
            {
                _owners[lane].MaterializeReadPort(this, lane);
                _canonicalReceives++;
            }
        }
        finally { _withdrawing = false; _hasCurrent = _hasCompleted = _epochsJustOpened = false; Array.Clear(_views); }
    }

    private static bool? Data(byte levels, byte known, int lane)
        => (known & (1 << lane)) == 0 ? null : (levels & (1 << lane)) != 0;
    private static LogicPinState Value(byte levels, byte known, int lane)
        => (known & (1 << lane)) == 0 ? LogicPinState.Unknown
            : (levels & (1 << lane)) != 0 ? LogicPinState.High : LogicPinState.Low;

    public void Dispose()
    {
        if (_disposed) return;
        try { Withdraw(); }
        finally { _disposed = true; Array.Clear(_views); }
    }
}
