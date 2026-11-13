using RetroMediaLab.Chips.Logic.Common.Enums;
using RetroMediaLab.Chips.Memory.Fujitsu.MB81xx.MB8118.Contracts;

namespace RetroMediaLab.Chips.EmuChip.Memory.Fujitsu.MB81xx.MB8118.Services;

internal readonly struct Mb8118ReadView(EmuMb8118 owner, ReadOnlyMemory<byte> cells,
    ReadOnlyMemory<long> refreshed, byte address, bool addressKnown, bool? data,
    LogicPinState output, long release)
{
    internal readonly EmuMb8118 Owner = owner;
    internal readonly ReadOnlyMemory<byte> Cells = cells;
    internal readonly ReadOnlyMemory<long> Refreshed = refreshed;
    internal readonly byte Address = address;
    internal readonly bool AddressKnown = addressKnown;
    internal readonly bool? Data = data;
    internal readonly LogicPinState Output = output;
    internal readonly long Release = release;
}

public sealed partial class EmuMb8118
{
    private EmuMb8118ReadPort? _readPort;
    internal long ReadInputTime => _readPort?.CurrentRowTime ?? _time;
    internal object ReadTimingIdentity => _timingProfile;

    internal bool PrepareReadView(in Mb8118ReadProgram input, out Mb8118ReadView view)
    {
        view = default;
        if (_publishing || _outputChanged is not null || _deadlineChanged is not null
            || !input.Predictable || !input.CompleteReadTimingQualified || !input.ReadAddressKnown
            || _startupCycles < 8 || input.RasFall < Startup || _writeHoldUntil > input.RowTime
            || (_hasReadCycle && (!_outputsRepresented || _receivedCasRise > input.RowTime))
            || (_outputDue >= 0 && !_outputsRepresented)) return false;
        AdvanceCore(input.RowTime, true);
        if (_rowActive || !_pins.RasHigh || !_pins.CasHigh || !_pins.WriteHigh
            || (_outputDue >= 0 && (_target != LogicPinState.HighImpedance || _outputDue > input.CasFall))
            || (_rasRise >= 0 && input.RasFall - _rasRise < _timing.RasPrecharge)
            || (_rasFall >= 0 && input.RasFall - _rasFall < (_readCycle && _cycleWrite ? _timing.ReadWriteCycle : _timing.Cycle))
            || (_casRise >= 0 && input.CasFall - _casRise < _timing.CasPrecharge)
            || (_casFall >= 0 && input.RowTime - _casFall < 15)
            || (_rasFall >= 0 && input.RowTime - _rasFall < 15)) return false;
        view = new(this, _cells, _refreshed, _pins.Address, _pins.AddressKnown, _pins.Data, _output, _outputDue);
        return true;
    }

    internal bool FollowsRead(in Mb8118ReadProgram previous, in Mb8118ReadProgram next)
        => next.RowTime >= previous.CasRise && next.RowTime - previous.CasFall >= 15
            && next.RowTime - previous.RasFall >= 15
            && next.RasFall - previous.RasRise >= _timing.RasPrecharge
            && next.RasFall - previous.RasFall >= _timing.Cycle
            && next.CasFall - previous.CasRise >= _timing.CasPrecharge
            && previous.ReleaseTime <= next.CasFall;

    internal void AttachReadPort(EmuMb8118ReadPort port)
    {
        if (_readPort is not null) throw new InvalidOperationException("The memory already owns a received read stream.");
        _readPort = port;
        PublishUnrepresentedDeadline();
    }
    internal void DetachReadPort(EmuMb8118ReadPort port)
    {
        if (ReferenceEquals(_readPort, port)) _readPort = null;
    }

    internal void MaterializeReadPort(EmuMb8118ReadPort port, int lane)
    {
        // These rows were certified fresh for every RAS. No cell contents or
        // write state changed inside the received read-only stream.
        if (port.HasCompletedRead)
        {
            var last = port.CompletedRead;
            var refreshed = port.CompletedRefreshes;
            for (int row = 0; row < 128; row++)
                if (refreshed[row] >= 0) _refreshed[row] = refreshed[row];
            if (port.AddressChanged >= 0) _addressChanged = port.AddressChanged;
            long changed = port.CompletedDataChanges[lane];
            if (changed >= 0) _dataChanged = changed;
            _pins.Address = last.Column; _pins.AddressKnown = true;
            _pins.RasHigh = _pins.CasHigh = _pins.WriteHigh = true;
            _pins.Data = port.CompletedColumnData(lane);
            _row = last.Row; _column = last.Column;
            _rowKnown = _columnKnown = true;
            _rowActive = _cycleWrite = false; _readCycle = true;
            _rasFall = last.RasFall; _casFall = last.CasFall;
            _rasRise = last.RasRise; _casRise = last.CasRise;
            _hasReadCycle = false; _readInputStep = 4;
            _output = port.CompletedValue(lane); _target = LogicPinState.HighImpedance;
            _outputDue = _nextResponseTime = last.ReleaseTime;
            _outputsRepresented = true; _representedOutputUntil = last.ReleaseTime;
            _time = last.CasRise;
        }
        var current = port.CurrentRead;
        // Only the latest unfinished read needs the progressive machinery.
        // Its previous latched release is retained by the canonical receiver.
        ReceiveReadProgram(in current, port.CurrentRowData(lane), port.CurrentColumnData(lane), out _);
    }
}
