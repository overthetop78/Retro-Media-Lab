using RetroMediaLab.Chips.Memory.Toshiba.TMM20xx.TMM2016.Contracts;
using C = RetroMediaLab.Chips.Memory.Toshiba.TMM20xx.TMM2016.Constants.Tmm2016Constants;

namespace RetroMediaLab.Chips.Memory.Toshiba.TMM20xx.TMM2016.Services;

public sealed partial class Tmm2016
{
    private static long? Optional(long time) => time < 0 ? null : time;

    public Tmm2016State CaptureState()
    {
        if (_publishing) throw new InvalidOperationException("Cannot snapshot a partial SRAM publication.");
        return new(C.StateVersion,_timing,_time,(byte[])_cells.Clone(),(byte[])_known.Clone(),
            UnpackPins(_pins),_addressChanged,_dataChanged,_csStarted,_oeStarted,_writeStarted,_writeAddress,
            _writeAddressKnown,_writing,_qualified,_postWriteUnknown,_lastWrite,_lastWriteAddress,
            _lastWriteAddressStarted,_lastWriteAddressKnown,_readData,_readKnown,_targetData,_targetKnown,
            _driven,_driveTarget,Optional(_dataDue),Optional(_invalidateDue),Optional(_driveDue),
            Optional(_writeEnableDue),Optional(_writeDisableDue),_writeOutputSelected,_supplyMillivolts,_supplyRecoveryEnd);
    }
    public void RestoreState(Tmm2016State state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (_publishing) throw new InvalidOperationException("Cannot restore during SRAM publication.");
        if (state.Version != C.StateVersion || state.Timing != _timing || state.Time < 0
            || state.Cells?.Length != _timing.Capacity || state.Known?.Length != _timing.Capacity
            || state.Pins.Address >= _timing.Capacity || state.WriteAddress >= _timing.Capacity || state.LastWriteAddress >= _timing.Capacity
            || state.AddressChanged < 0 || state.AddressChanged > state.Time || state.DataChanged < 0 || state.DataChanged > state.Time
            || new[] { state.CsStarted,state.OeStarted,state.WriteStarted,state.LastWrite }.Any(value => value < -1 || value > state.Time)
            || state.LastWriteAddressStarted < 0 || state.LastWriteAddressStarted > state.Time
            || state.DataDue <= state.Time || state.InvalidateDue <= state.Time || state.DriveDue <= state.Time
            || state.WriteEnableDue <= state.Time || state.WriteDisableDue <= state.Time
            || ((state.WriteEnableDue is not null || state.WriteDisableDue is not null) && !_timing.SelectionDuringWriteCanDrive)
            || (state.WriteEnableDue is not null && (state.WriteDisableDue is null
                || state.WriteEnableDue >= state.WriteDisableDue || !state.Writing || state.Pins.OeHigh))
            || state.Writing != (IsSelected(PackPins(state.Pins)) && !state.Pins.WeHigh)
            || state.DriveTarget != (IsSelected(PackPins(state.Pins)) && !state.Pins.OeHigh && state.Pins.WeHigh)
            || (state.DriveDue is not null && state.Driven == state.DriveTarget)
            || (state.DriveDue is null && state.Driven != state.DriveTarget
                && !(state.WriteDisableDue is not null && !state.DriveTarget))
            || state.SupplyMillivolts < 0 || state.SupplyRecoveryEnd < 0
            || state.SupplyRecoveryEnd > (Int128)state.Time + _timing.Access
            || (_timing.MinimumRetentionMillivolts == 0 && (state.SupplyMillivolts != C.DefaultSupplyMillivolts || state.SupplyRecoveryEnd != 0))
            || (_timing.MinimumRetentionMillivolts > 0 && state.SupplyMillivolts < _timing.MinimumRetentionMillivolts
                && state.Known!.Any(value => value != 0))
            || (state.ReadData & ~state.ReadKnown) != 0 || (state.TargetData & ~state.TargetKnown) != 0
            || (state.Pins.Data & ~state.Pins.DataKnown) != 0)
            throw new ArgumentException(nameof(state));
        // Unknown bits in cells may retain their last physical value; only Known makes a read meaningful.
        state.Cells.CopyTo(_cells,0); state.Known.CopyTo(_known,0);
        _time = state.Time; _pins = PackPins(state.Pins); _addressChanged = state.AddressChanged;
        _dataChanged = state.DataChanged; _csStarted = state.CsStarted; _oeStarted = state.OeStarted;
        _writeStarted = state.WriteStarted; _writeAddress = state.WriteAddress; _writeAddressKnown = state.WriteAddressKnown;
        _writing = state.Writing; _qualified = state.Qualified; _postWriteUnknown = state.PostWriteUnknown;
        _lastWrite = state.LastWrite; _lastWriteAddress = state.LastWriteAddress;
        _lastWriteAddressStarted = state.LastWriteAddressStarted; _lastWriteAddressKnown = state.LastWriteAddressKnown;
        _readData = state.ReadData; _readKnown = state.ReadKnown; _targetData = state.TargetData; _targetKnown = state.TargetKnown;
        _driven = state.Driven; _driveTarget = state.DriveTarget;
        _dataDue = state.DataDue ?? -1; _invalidateDue = state.InvalidateDue ?? -1; _driveDue = state.DriveDue ?? -1;
        _writeEnableDue = state.WriteEnableDue ?? -1; _writeDisableDue = state.WriteDisableDue ?? -1;
        _writeOutputSelected = state.WriteOutputSelected;
        _supplyMillivolts = state.SupplyMillivolts; _supplyRecoveryEnd = state.SupplyRecoveryEnd;
        FindNext(); _announced = _next; _published = OutputBits;
    }
}
