using Q = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Intel80186.Constants.Prefetch80186Constants;
using static RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Intel80186.Constants.Cpu80186Constants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Intel80186.Services;

internal sealed partial class Cpu80186
{
    private readonly byte[] _prefetch = new byte[Q.Capacity];
    private int _prefetchHead;
    private int _prefetchCount;
    private ushort _prefetchSegment;
    private ushort _prefetchHeadOffset;
    private ushort _prefetchFillOffset;
    private bool _prefetchPending;
    private bool _prefetchDiscardPending;
    private int _prefetchPendingAddress;
    private int _prefetchPendingBytes;
    private int _prefetchPendingClocks;
    private int _prefetchStalls;
    private int _prefetchAdvancedClocks;
    private int _executionBusClocks;
    private int _executionWaitClocks;
    internal bool IsInstructionFetch { get; private set; }

    private void ResetPrefetch()
    {
        _prefetchPending = false;
        _prefetchPendingClocks = 0;
        IsInstructionFetch = false;
        FlushPrefetch();
    }

    private void FlushPrefetch()
    {
        _prefetchHead = _prefetchCount = 0;
        _prefetchSegment = _segments[Cs];
        _prefetchHeadOffset = _prefetchFillOffset = IP;
        _prefetchDiscardPending = _prefetchPending;
    }

    private void TransferControl(ushort offset, ushort? segment = null)
    {
        if (segment.HasValue) _segments[Cs] = segment.Value;
        IP = offset;
        FlushPrefetch();
    }

    private bool StartPrefetch()
    {
        if (_prefetchPending || _prefetchCount > Q.Capacity - Q.WordBytes) return false;
        _prefetchPendingAddress = ((_prefetchSegment << 4) + _prefetchFillOffset) & AddressMask;
        _prefetchPendingBytes = (_prefetchFillOffset & 1) != 0 ? 1 : Q.WordBytes;
        _prefetchPendingClocks = Q.BusCycles + ChipSelectWaits(SelectBusTargets(_prefetchPendingAddress, true));
        _prefetchPending = true;
        _prefetchDiscardPending = false;
        return true;
    }

    private void CompletePrefetch()
    {
        ushort value;
        IsInstructionFetch = true;
        try
        {
            value = _prefetchPendingBytes == Q.WordBytes ? ReadExternalWord(_prefetchPendingAddress, true)
                : ReadExternalByte(_prefetchPendingAddress, true);
        }
        finally { IsInstructionFetch = false; }
        _prefetchPending = false;
        _prefetchPendingClocks = 0;
        if (_prefetchDiscardPending) return;
        for (var index = 0; index < _prefetchPendingBytes; index++)
        {
            _prefetch[(_prefetchHead + _prefetchCount++) % Q.Capacity] = (byte)(value >> (index * Q.ByteShift));
            _prefetchFillOffset++;
        }
    }

    private void FinishPendingPrefetch()
    {
        if (!_prefetchPending) return;
        _prefetchStalls += _prefetchPendingClocks;
        CompletePrefetch();
    }

    private byte FetchPrefetched()
    {
        if (_prefetchSegment != _segments[Cs] || _prefetchHeadOffset != IP) FlushPrefetch();
        while (_prefetchCount == 0)
        {
            if (!_prefetchPending) StartPrefetch();
            FinishPendingPrefetch();
        }
        var value = _prefetch[_prefetchHead];
        _prefetchHead = (_prefetchHead + 1) % Q.Capacity;
        _prefetchCount--;
        _prefetchHeadOffset++;
        IP++;
        return value;
    }

    private void AdvancePrefetch(int clocks)
    {
        while (clocks > 0)
        {
            if (!_prefetchPending && !StartPrefetch()) return;
            var elapsed = Math.Min(clocks, _prefetchPendingClocks);
            _prefetchPendingClocks -= elapsed;
            clocks -= elapsed;
            if (_prefetchPendingClocks == 0) CompletePrefetch();
        }
    }

    private void PrefetchDuringPrefix(int clocks)
    {
        _prefetchAdvancedClocks += clocks;
        AdvancePrefetch(clocks);
    }

    private void PrefetchDuringExecution(int executionClocks)
    {
        var available = executionClocks + _busPenalty - _executionBusClocks - _executionWaitClocks - _prefetchAdvancedClocks;
        if (!_halted && available > 0) AdvancePrefetch(available);
    }

    private void PrepareExecutionBus()
    {
        if (IsInstructionFetch) return;
        FinishPendingPrefetch();
        _executionBusClocks += Q.BusCycles;
    }
}
