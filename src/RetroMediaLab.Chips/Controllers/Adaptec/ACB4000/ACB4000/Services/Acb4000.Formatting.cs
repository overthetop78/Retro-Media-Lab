using RetroMediaLab.Chips.Controllers.Acorn.Winchester.Scsi.Interfaces;
using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Enums;
using C = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000Constants;
using F = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000FormatConstants;
using P = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000ParameterConstants;
using R = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000PhysicalParameterConstants;

namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

public sealed partial class Acb4000
{
    private Acb4000FormatRequest _formatRequest;
    private Acb4000DriveParameters _formatParameters;
    private IScsiStorage? _formatDrive;
    private bool _formatHeaderReceived;
    private int _formatDefectLength;
    private int _formatTrackLogicalBlocks;
    private int _formatPrecedingDefects;
    private bool _formatTrackPending;

    private void BeginFormat()
    {
        if (!_hasFormat[Unit] && !_modeSelected[Unit]) { Complete(C.BlownFormat); return; }
        if (Drive!.IsReadOnly || _driveWriteFault) { Complete(C.WriteFault); return; }
        _formatParameters = _modeParameters[Unit];
        if (!Acb4000FormatRequest.TryDecode(_command.AsSpan(0, _commandLength), _formatParameters,
            out _formatRequest, ExpandedCommandsEnabled))
        { Complete(C.BadArgument); return; }
        _formatHeaderReceived = false;
        _formatDefectLength = 0;
        if (_formatRequest.HasDefectList) Receive(F.DefectHeaderLength);
        else PrepareFormat();
    }

    private void ContinueFormatDefects()
    {
        if (!_formatHeaderReceived)
        {
            if (!Acb4000FormatDefects.TryReadLength(_buffer.AsSpan(0, _length), out var total))
            { Complete(C.BadArgument); return; }
            _formatHeaderReceived = true;
            if (total > F.DefectHeaderLength)
            {
                _length = total;
                Request(Acb4000BusPhase.DataOut);
                return;
            }
        }
        if (!Acb4000FormatDefects.TryDecode(_buffer.AsSpan(0, _length), _formatParameters, out var defects))
        { Complete(C.BadArgument); return; }
        _formatDefectLength = _length;
        PrepareFormat();
    }

    private void PrepareFormat()
    {
        var drive = Drive!;
        if (!TryValidateFormatTracks(out var logicalBlocks)) { Complete(C.BadArgument); return; }
        try
        {
            drive.PrepareFormat(_formatParameters.Cylinders, _formatParameters.Heads,
                _formatRequest.Geometry.SectorsPerTrack, _formatParameters.BlockSize);
            _formatDrive = drive;
            _invalidFormatStorage[Unit] = drive;
            _hasFormat[Unit] = false;
            _blockSizes[Unit] = _formatParameters.BlockSize;
            _address = 0;
            RemainingBlocks = logicalBlocks;
            FormatCylinder = FormatHead = 0;
            _formatPrecedingDefects = 0;
            _formatChannel.Reset();
            _formatTrackPending = true;
            _stage = Acb4000Stage.Format;
        }
        catch (Exception error) when (StorageFailure(error)) { Complete(C.WriteFault); }
    }

    private void TickFormat()
    {
        if (!ReferenceEquals(Drive, _formatDrive)) { Complete(C.NotReady); return; }
        if (_driveWriteFault) { Complete(C.WriteFault, true); return; }
        if (!_formatTrackPending) return;
        try
        {
            var defects = FormatDefects();
            var geometry = _formatRequest.Geometry;
            if (!Acb4000TrackLayout.TryBuild(_formatParameters, geometry, FormatCylinder, FormatHead,
                defects, _formatMap, out _formatTrackLogicalBlocks)) { Complete(C.BadArgument); return; }
            var physicalBlock = ((long)FormatCylinder * _formatParameters.Heads + FormatHead) * geometry.SectorsPerTrack;
            BeginPositioning(physicalBlock * BlockSize, Acb4000PositioningPurpose.Format);
        }
        catch (Exception error) when (StorageFailure(error)) { Complete(C.WriteFault, true); }
    }

    private void StartFormatTrack()
    {
        Span<byte> physicalParameters = stackalloc byte[R.FlagCount];
        var firstTrack = FormatCylinder == 0 && FormatHead == 0;
        if (firstTrack) Acb4000PhysicalParameters.Write(physicalParameters, _formatParameters);
        _formatChannel.BeginTrack(_formatMap.AsSpan(0, _formatRequest.Geometry.SectorsPerTrack), _address, BlockSize,
            _formatRequest.Geometry.Interleave == F.SequentialInterleave, _formatRequest.Fill,
            checked((byte)_formatPrecedingDefects), firstTrack ? physicalParameters : default, _eccRegisters.Polynomial);
        _formatTrackPending = false;
        // Firmware 14A9-14AC waits for INDEX without the bounded search loop at 15BF.
        // Keep FORMAT waiting while the drive is ready; RESET/faults can still abort it.
        _stage = Acb4000Stage.Format;
    }

    public bool ClockWriteCell(bool index)
    {
        if (_stage == Acb4000Stage.Write && _bus.Busy) return ClockBlockWrite();
        if (_stage != Acb4000Stage.Format || _formatTrackPending || !_bus.Busy) return false;
        if (_driveWriteFault) { Complete(C.WriteFault, true); return false; }
        var cell = _formatChannel.ClockCell(index);
        if (_formatChannel.Complete) FinishFormatTrack();
        return cell;
    }

    private void FinishFormatTrack()
    {
        _emittedBlocks[Unit] += _formatRequest.Geometry.SectorsPerTrack;
        _address += checked((uint)_formatTrackLogicalBlocks);
        RemainingBlocks -= _formatTrackLogicalBlocks;
        _formatPrecedingDefects += _formatRequest.Geometry.SectorsPerTrack - _formatTrackLogicalBlocks;
        if (++FormatHead == _formatParameters.Heads)
        {
            FormatHead = 0;
            FormatCylinder++;
        }
        if (FormatCylinder != _formatParameters.Cylinders)
        {
            _formatTrackPending = true;
            return;
        }
        try
        {
            Span<byte> parameters = stackalloc byte[P.DriveParameterLength];
            _formatParameters.Write(parameters);
            _formatDrive!.WriteParameters(parameters);
            _activeParameters[Unit] = _modeParameters[Unit] = _formatParameters;
            _modeSelected[Unit] = false;
            _hasFormat[Unit] = true;
            _parametersLoaded[Unit] = true;
            _sequentialLayout[Unit] = _formatRequest.Geometry.Interleave == F.SequentialInterleave;
            _invalidFormatStorage[Unit] = null;
            _formatDrive = null;
            Complete();
        }
        catch (Exception error) when (StorageFailure(error)) { Complete(C.WriteFault, true); }
    }

    private Acb4000FormatDefects FormatDefects()
    {
        if (_formatDefectLength == 0) return default;
        if (!Acb4000FormatDefects.TryDecode(_buffer.AsSpan(0, _formatDefectLength), _formatParameters, out var defects))
            throw new InvalidOperationException();
        return defects;
    }

    private bool TryValidateFormatTracks(out int logicalBlocks)
    {
        var geometry = _formatRequest.Geometry;
        logicalBlocks = checked((int)geometry.BlockCount);
        var defects = FormatDefects();
        var previousCylinder = -1;
        var previousHead = -1;
        // Only affected tracks need validation before touching the drive; duplicates are counted once by the map.
        for (var index = 0; index < defects.Count; index++)
        {
            var defect = defects[index];
            if (defect.Cylinder == previousCylinder && defect.Head == previousHead) continue;
            previousCylinder = defect.Cylinder;
            previousHead = defect.Head;
            if (!Acb4000TrackLayout.TryBuild(_formatParameters, geometry, defect.Cylinder, defect.Head,
                defects, _formatMap, out var count)) return false;
            logicalBlocks -= geometry.SectorsPerTrack - count;
        }
        return true;
    }
}
