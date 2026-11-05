using RetroMediaLab.Chips.Controllers.Acorn.Winchester.Scsi.Interfaces;
using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Enums;
using C = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000Constants;
using F = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000FormatConstants;
using P = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000ParameterConstants;

namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

public sealed partial class Acb4000
{
    private bool _completeAfterParameterAcquisition;

    private bool LoadParameters(IScsiStorage storage)
    {
        if (!ReferenceEquals(_parameterStorage[Unit], storage))
        {
            _parameterStorage[Unit] = storage;
            _parametersLoaded[Unit] = false;
            _modeSelected[Unit] = _hasFormat[Unit] = false;
            _sequentialLayout[Unit] = false;
            _activeParameters[Unit] = _modeParameters[Unit] = Acb4000DriveParameters.Default;
            _blockSizes[Unit] = Acb4000DriveParameters.Default.BlockSize;
        }
        if (_parametersLoaded[Unit]) return true;
        // MODE SELECT can recover a medium whose parameter fields cannot be read.
        // REZERO itself must not recursively start another calibration for this acquisition.
        if (_code is Acb4000CommandCode.ModeSelect or Acb4000CommandCode.Rezero)
        {
            return true;
        }
        if (_code == Acb4000CommandCode.Format && _modeSelected[Unit]) return true;
        _positioningOffset = 0;
        _positioningPurpose = Acb4000PositioningPurpose.Parameters;
        _positioningDrive = storage;
        _positioningExpired = false;
        _positioners[Unit].BeginRezero(_modeParameters[Unit].StepRate);
        _driveMoving[Unit] = true;
        _stage = Acb4000Stage.Positioning;
        return false;
    }

    public void SetIndex(bool asserted)
    {
        _aic.Sequencer.SetIndex(asserted);
        TranslationIndex(asserted);
        if (_stage == Acb4000Stage.Parameters)
        {
            var waiting = _parameterChannel.WaitingForIndex;
            _parameterChannel.SetIndex(asserted);
            if (waiting && !_parameterChannel.WaitingForIndex) BeginFirstFieldSearch();
        }
    }

    public void ExpireParameterSearch()
    {
        if (_stage != Acb4000Stage.Parameters) return;
        _parameterChannel.Expire();
        FinishParameterAcquisition();
    }

    private void StartParameterAcquisition()
    {
        _nativeReadOperation = true;
        // Calibration has completed with SEEK COMPLETE sampled after its last edge.
        // This internal read is not a new host command competing with a moving unit.
        _driveMoving[Unit] = false;
        SelectedHead = 0;
        _parameterChannel.Begin(Acb4000DriveParameters.Default);
        _stage = Acb4000Stage.Parameters;
        _searchBudget.Begin(F.SmallBlockSequentialSectors);
        BeginIndexSearch();
    }

    private void FinishParameterAcquisition()
    {
        if (!_parameterChannel.Complete) return;
        _searchDeadline.Reset();
        if (!ReferenceEquals(Drive, _parameterStorage[Unit]) || !_driveReady[Unit])
        { _parameterChannel.Reset(); Complete(C.NotReady); return; }
        _parametersLoaded[Unit] = true;
        _hasFormat[Unit] = _parameterChannel.FormatValid && !ReferenceEquals(Drive, _invalidFormatStorage[Unit]);
        if (_hasFormat[Unit])
        {
            _activeParameters[Unit] = _parameterChannel.Parameters;
            _sequentialLayout[Unit] = _parameterChannel.SequentialLayout;
            if (!_modeSelected[Unit]) _modeParameters[Unit] = _parameterChannel.Parameters;
            _blockSizes[Unit] = _parameterChannel.Parameters.BlockSize;
        }
        if (_completeAfterParameterAcquisition)
        {
            if (_hasFormat[Unit])
            {
                _modeParameters[Unit] = _activeParameters[Unit];
                _modeSelected[Unit] = false;
            }
            Complete(_hasFormat[Unit] ? C.NoSense : C.BlownFormat);
        }
        else ExecuteCommand();
    }

    private void BeginModeSelect()
    {
        var length = _command[C.ShortCountOffset];
        if (!ReservedShort(allocation: true) || length < P.ExtentParameterLength
            || length is > P.ExtentParameterLength and < P.DriveParameterLength)
        { Complete(C.BadArgument); return; }
        // This changes controller RAM only. Persisting the selected parameters belongs to FORMAT.
        Receive(length);
    }

    private void EndModeSelect()
    {
        if (!Acb4000DriveParameters.TryDecode(_buffer.AsSpan(0, _length), _modeParameters[Unit], out var selected))
        { Complete(C.BadArgument); return; }
        _modeParameters[Unit] = selected;
        _modeSelected[Unit] = true;
        Complete();
    }

    private void ModeSenseReply()
    {
        var length = _command[C.ShortCountOffset];
        if (!ReservedShort(allocation: true) || length < P.ExtentParameterLength)
        { Complete(C.BadArgument); return; }
        if (!_hasFormat[Unit]) { Complete(C.BlownFormat); return; }
        _buffer.AsSpan(0, length).Clear();
        // A buffer ending inside the optional list receives only that prefix of the complete record.
        _modeParameters[Unit].Write(_buffer, includeDriveParameters: true);
        Reply(length);
    }
}
