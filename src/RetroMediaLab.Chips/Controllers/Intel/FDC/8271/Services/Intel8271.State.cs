using RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Contracts;
using C = RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Constants.Intel8271Constants;

namespace RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Services;

public sealed partial class Intel8271
{
    /// <summary>Capture à l'instant courant sans acquitter DRQ, IRQ ou le bus.</summary>
    public Intel8271State CaptureState() => new()
    {
        FormatVersion = C.StateFormatVersion,
        InputClock = _clock,
        ChipClock = _chipClock,
        BusData = BusData,
        Buffer = (byte[])_buffer.Clone(),
        Special = (byte[])_special.Clone(),
        Parameters = (byte[])_parameters.Clone(),
        Tracks = (byte[])_tracks.Clone(),
        SeekSteps = (long[])_seekSteps.Clone(),
        ReadBytes = (long[])_readBytes.Clone(),
        WrittenBytes = (long[])_writtenBytes.Clone(),
        ReadyInputs = (bool?[])_readyInputs.Clone(),
        ProtectInputs = (bool?[])_protectInputs.Clone(),
        TrackZeroInputs = (bool?[])_trackZeroInputs.Clone(),
        IndexInputs = (bool?[])_indexInputs.Clone(),
        FaultInputs = (bool[])_faultInputs.Clone(),
        NotReadyLatched = (bool[])_notReadyLatched.Clone(),
        PhysicalTracks = (int[])_physicalTracks.Clone(),
        SerialHeader = (byte[])_serialHeader.Clone(),
        CommandPending = _commandPending,
        ParameterPending = _parameterPending,
        BufferedCommand = _bufferedCommand,
        BufferedParameter = _bufferedParameter,
        RegisterClockFraction = _registerClockFraction,
        BusReading = _busReading,
        BusWriting = _busWriting,
        BusSelected = _busSelected,
        BusDack = _busDack,
        BusRegister = _busRegister,
        BusWriteValue = _busWriteValue,
        InputClockCount = _inputClockCount,
        PublishedClockEvent = _publishedClockEvent,
        UnloadCount = _unloadCount,
        ResetAsserted = _resetAsserted,
        DataRequest = _dataRequest,
        Status = _status,
        Result = _result,
        Data = _data,
        Command = _command,
        DriveOutput = _driveOutput,
        Mode = _mode,
        ParameterCount = _parameterCount,
        RequiredParameters = _requiredParameters,
        Drive = _drive,
        Sector = _sector,
        Remaining = _remaining,
        SectorSize = _sectorSize,
        Offset = _offset,
        Delay = _delay,
        StepTicks = _stepTicks,
        SettleTicks = _settleTicks,
        LoadTicks = _loadTicks,
        AwaitingData = _awaitingData,
        DeletedResult = _deletedResult,
        Phase = _phase,
        MechanicalSpeedPercent = _mechanicalSpeedPercent,
        ExternalCount = _externalCount,
        Recalibrating = _recalibrating,
        DidStep = _didStep,
        SeekRemaining = _seekRemaining,
        SeekTarget = _seekTarget,
        SeekDirection = _seekDirection,
        ScanMatches = _scanMatches,
        ScanEqual = _scanEqual,
        ScanFieldOffset = _scanFieldOffset,
        ScanResult = _scanResult,
        SerialPhase = _serialPhase,
        ReadDataLevel = _readDataLevel,
        ClockWindow = _clockWindow,
        PhaseLockedSeparator = _phaseLockedSeparator,
        PreviousPulseWasClock = _previousPulseWasClock,
        ReadPulseSeen = _readPulseSeen,
        SeparatorSynchronized = _separatorSynchronized,
        PloClockLevel = _ploClockLevel,
        ReadPulseGap = _readPulseGap,
        ReadFullCellRun = _readFullCellRun,
        ClockPulse = _clockPulse,
        DataPulse = _dataPulse,
        SerialClockStarted = _serialClockStarted,
        SerialFraction = _serialFraction,
        SerialDataShift = _serialDataShift,
        SerialClockShift = _serialClockShift,
        SerialBits = _serialBits,
        SerialZeroBits = _serialZeroBits,
        SerialSyncArmed = _serialSyncArmed,
        InSync = _inSync,
        SerialFieldOffset = _serialFieldOffset,
        SerialCrc = _serialCrc,
        SerialPayloadOffset = _serialPayloadOffset,
        SerialSkipData = _serialSkipData,
        SerialIndexCount = _serialIndexCount,
        SerialRequestTicks = _serialRequestTicks,
        SeekRetryCount = _seekRetryCount,
        WritePhase = _writePhase,
        WriteGapBits = _writeGapBits,
        WriteRun = _writeRun,
        WriteWord = _writeWord,
        WriteHalfBit = _writeHalfBit,
        WriteHalfDelay = _writeHalfDelay,
        WriteFraction = _writeFraction,
        WriteCrc = _writeCrc,
        WriteFieldOffset = _writeFieldOffset,
        WriteLatch = _writeLatch,
        WriteLatchFull = _writeLatchFull,
        WriteData = _writeData,
        WritePulseAge = _writePulseAge,
        WriteStopping = _writeStopping,
        WriteFinishRequested = _writeFinishRequested,
        WriteFinishingFormat = _writeFinishingFormat,
    };

    /// <summary>Réinjecte l'état sans horloge, notification ni accès à un lecteur.</summary>
    public void RestoreState(Intel8271State state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.FormatVersion != C.StateFormatVersion || state.InputClock != _clock || state.ChipClock != _chipClock
            || state.Buffer is null || !Enum.IsDefined(state.Phase) || !Enum.IsDefined(state.SerialPhase) || !Enum.IsDefined(state.WritePhase)
            || state.Special?.Length != _special.Length
            || state.Parameters?.Length != _parameters.Length
            || state.Tracks?.Length != _tracks.Length
            || state.SeekSteps?.Length != _seekSteps.Length
            || state.ReadBytes?.Length != _readBytes.Length
            || state.WrittenBytes?.Length != _writtenBytes.Length
            || state.ReadyInputs?.Length != _readyInputs.Length
            || state.ProtectInputs?.Length != _protectInputs.Length
            || state.TrackZeroInputs?.Length != _trackZeroInputs.Length
            || state.IndexInputs?.Length != _indexInputs.Length
            || state.FaultInputs?.Length != _faultInputs.Length
            || state.NotReadyLatched?.Length != _notReadyLatched.Length
            || state.PhysicalTracks?.Length != _physicalTracks.Length
            || state.SerialHeader?.Length != _serialHeader.Length
            ) throw new ArgumentException(nameof(state));
        BusData = state.BusData;
        _buffer = (byte[])state.Buffer.Clone();
        state.Special.CopyTo(_special, 0);
        state.Parameters.CopyTo(_parameters, 0);
        state.Tracks.CopyTo(_tracks, 0);
        state.SeekSteps.CopyTo(_seekSteps, 0);
        state.ReadBytes.CopyTo(_readBytes, 0);
        state.WrittenBytes.CopyTo(_writtenBytes, 0);
        state.ReadyInputs.CopyTo(_readyInputs, 0);
        state.ProtectInputs.CopyTo(_protectInputs, 0);
        state.TrackZeroInputs.CopyTo(_trackZeroInputs, 0);
        state.IndexInputs.CopyTo(_indexInputs, 0);
        state.FaultInputs.CopyTo(_faultInputs, 0);
        state.NotReadyLatched.CopyTo(_notReadyLatched, 0);
        state.PhysicalTracks.CopyTo(_physicalTracks, 0);
        state.SerialHeader.CopyTo(_serialHeader, 0);
        _commandPending = state.CommandPending;
        _parameterPending = state.ParameterPending;
        _bufferedCommand = state.BufferedCommand;
        _bufferedParameter = state.BufferedParameter;
        _registerClockFraction = state.RegisterClockFraction;
        _busReading = state.BusReading;
        _busWriting = state.BusWriting;
        _busSelected = state.BusSelected;
        _busDack = state.BusDack;
        _busRegister = state.BusRegister;
        _busWriteValue = state.BusWriteValue;
        _inputClockCount = state.InputClockCount;
        _publishedClockEvent = state.PublishedClockEvent;
        _unloadCount = state.UnloadCount;
        _resetAsserted = state.ResetAsserted;
        _dataRequest = state.DataRequest;
        _status = state.Status;
        _result = state.Result;
        _data = state.Data;
        _command = state.Command;
        _driveOutput = state.DriveOutput;
        _mode = state.Mode;
        _parameterCount = state.ParameterCount;
        _requiredParameters = state.RequiredParameters;
        _drive = state.Drive;
        _sector = state.Sector;
        _remaining = state.Remaining;
        _sectorSize = state.SectorSize;
        _offset = state.Offset;
        _delay = state.Delay;
        _stepTicks = state.StepTicks;
        _settleTicks = state.SettleTicks;
        _loadTicks = state.LoadTicks;
        _awaitingData = state.AwaitingData;
        _deletedResult = state.DeletedResult;
        _phase = state.Phase;
        _mechanicalSpeedPercent = state.MechanicalSpeedPercent;
        _externalCount = state.ExternalCount;
        _recalibrating = state.Recalibrating;
        _didStep = state.DidStep;
        _seekRemaining = state.SeekRemaining;
        _seekTarget = state.SeekTarget;
        _seekDirection = state.SeekDirection;
        _scanMatches = state.ScanMatches;
        _scanEqual = state.ScanEqual;
        _scanFieldOffset = state.ScanFieldOffset;
        _scanResult = state.ScanResult;
        _serialPhase = state.SerialPhase;
        _readDataLevel = state.ReadDataLevel;
        _clockWindow = state.ClockWindow;
        _phaseLockedSeparator = state.PhaseLockedSeparator;
        _previousPulseWasClock = state.PreviousPulseWasClock;
        _readPulseSeen = state.ReadPulseSeen;
        _separatorSynchronized = state.SeparatorSynchronized;
        _ploClockLevel = state.PloClockLevel;
        _readPulseGap = state.ReadPulseGap;
        _readFullCellRun = state.ReadFullCellRun;
        _clockPulse = state.ClockPulse;
        _dataPulse = state.DataPulse;
        _serialClockStarted = state.SerialClockStarted;
        _serialFraction = state.SerialFraction;
        _serialDataShift = state.SerialDataShift;
        _serialClockShift = state.SerialClockShift;
        _serialBits = state.SerialBits;
        _serialZeroBits = state.SerialZeroBits;
        _serialSyncArmed = state.SerialSyncArmed;
        _inSync = state.InSync;
        _serialFieldOffset = state.SerialFieldOffset;
        _serialCrc = state.SerialCrc;
        _serialPayloadOffset = state.SerialPayloadOffset;
        _serialSkipData = state.SerialSkipData;
        _serialIndexCount = state.SerialIndexCount;
        _serialRequestTicks = state.SerialRequestTicks;
        _seekRetryCount = state.SeekRetryCount;
        _writePhase = state.WritePhase;
        _writeGapBits = state.WriteGapBits;
        _writeRun = state.WriteRun;
        _writeWord = state.WriteWord;
        _writeHalfBit = state.WriteHalfBit;
        _writeHalfDelay = state.WriteHalfDelay;
        _writeFraction = state.WriteFraction;
        _writeCrc = state.WriteCrc;
        _writeFieldOffset = state.WriteFieldOffset;
        _writeLatch = state.WriteLatch;
        _writeLatchFull = state.WriteLatchFull;
        _writeData = state.WriteData;
        _writePulseAge = state.WritePulseAge;
        _writeStopping = state.WriteStopping;
        _writeFinishRequested = state.WriteFinishRequested;
        _writeFinishingFormat = state.WriteFinishingFormat;
    }
}