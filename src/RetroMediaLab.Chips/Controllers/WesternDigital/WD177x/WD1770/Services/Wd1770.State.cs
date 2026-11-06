using RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Contracts;
using C = RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Constants.Wd1770Constants;

namespace RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Services;

public sealed partial class Wd1770
{
    public Wd1770State CaptureState() => new()
    {
        FormatVersion = C.StateFormatVersion,
        Model = _model,
        Clock = _clock,
        Reader = _reader.CaptureState(),
        Writer = _writer.CaptureState(),
        Id = (byte[])_id.Clone(),
        BusData = BusData,
        StepPulses = StepPulses,
        DataBytesRead = DataBytesRead,
        DataBytesWritten = DataBytesWritten,
        PublishedDriveOutputs = new(_publishedDriveOutputs.Motor, _publishedDriveOutputs.Step,
            _publishedDriveOutputs.Direction, _publishedDriveOutputs.WriteGate, _publishedDriveOutputs.Active,
            _publishedDriveOutputs.Reading, _publishedDriveOutputs.Writing, _publishedDriveOutputs.DoubleDensity),
        BusSelected = _busSelected,
        BusRead = _busRead,
        BusRegister = _busRegister,
        BusWriteData = _busWriteData,
        Command = _command,
        Track = _track,
        Sector = _sector,
        Data = _data,
        Errors = _errors,
        SeekTarget = _seekTarget,
        WriteLatch = _writeLatch,
        Drq = _drq,
        Irq = _irq,
        TypeOne = _typeOne,
        ForceIndex = _forceIndex,
        ForceImmediate = _forceImmediate,
        Index = _index,
        TrackZero = _trackZero,
        WriteProtected = _writeProtected,
        ReadData = _readData,
        Reset = _reset,
        Motor = _motor,
        SpinComplete = _spinComplete,
        Step = _step,
        Direction = _direction,
        Verify = _verify,
        WriteLatchFull = _writeLatchFull,
        FormatCrc = _formatCrc,
        FormatCrcRequested = _formatCrcRequested,
        Timer = _timer,
        MotorIndexes = _motorIndexes,
        IdleIndexes = _idleIndexes,
        SpinIndexes = _spinIndexes,
        SearchIndexes = _searchIndexes,
        RestoreSteps = _restoreSteps,
        IdOffset = _idOffset,
        Remaining = _remaining,
        SearchBytes = _searchBytes,
        WriteRun = _writeRun,
        WriteGapBytes = _writeGapBytes,
        Crc = _crc,
        Phase = _phase,
    };

    public void RestoreState(Wd1770State state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.FormatVersion != C.StateFormatVersion || state.Model != _model || state.Clock != _clock
            || state.Reader is null || state.Writer is null || state.Id?.Length != _id.Length || !Enum.IsDefined(state.Phase))
            throw new ArgumentException(nameof(state));
        _reader.RestoreState(state.Reader);
        _writer.RestoreState(state.Writer);
        state.Id.CopyTo(_id, 0);
        BusData = state.BusData;
        StepPulses = state.StepPulses;
        DataBytesRead = state.DataBytesRead;
        DataBytesWritten = state.DataBytesWritten;
        var outputs = state.PublishedDriveOutputs;
        _publishedDriveOutputs = new(outputs.Motor, outputs.Step, outputs.Direction, outputs.WriteGate,
            outputs.Active, outputs.Reading, outputs.Writing, outputs.DoubleDensity);
        _busSelected = state.BusSelected;
        _busRead = state.BusRead;
        _busRegister = state.BusRegister;
        _busWriteData = state.BusWriteData;
        _command = state.Command;
        _track = state.Track;
        _sector = state.Sector;
        _data = state.Data;
        _errors = state.Errors;
        _seekTarget = state.SeekTarget;
        _writeLatch = state.WriteLatch;
        _drq = state.Drq;
        _irq = state.Irq;
        _typeOne = state.TypeOne;
        _forceIndex = state.ForceIndex;
        _forceImmediate = state.ForceImmediate;
        _index = state.Index;
        _trackZero = state.TrackZero;
        _writeProtected = state.WriteProtected;
        _readData = state.ReadData;
        _reset = state.Reset;
        _motor = state.Motor;
        _spinComplete = state.SpinComplete;
        _step = state.Step;
        _direction = state.Direction;
        _verify = state.Verify;
        _writeLatchFull = state.WriteLatchFull;
        _formatCrc = state.FormatCrc;
        _formatCrcRequested = state.FormatCrcRequested;
        _timer = state.Timer;
        _motorIndexes = state.MotorIndexes;
        _idleIndexes = state.IdleIndexes;
        _spinIndexes = state.SpinIndexes;
        _searchIndexes = state.SearchIndexes;
        _restoreSteps = state.RestoreSteps;
        _idOffset = state.IdOffset;
        _remaining = state.Remaining;
        _searchBytes = state.SearchBytes;
        _writeRun = state.WriteRun;
        _writeGapBytes = state.WriteGapBytes;
        _crc = state.Crc;
        _phase = state.Phase;
    }
}