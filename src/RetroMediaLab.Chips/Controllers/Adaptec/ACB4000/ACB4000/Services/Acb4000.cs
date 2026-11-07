using System.IO;
using RetroMediaLab.Chips.Controllers.Acorn.Winchester.Scsi.Interfaces;
using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Enums;
using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Interfaces;
using C = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000Constants;
using F = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000FormatConstants;
using T = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000SearchTimingConstants;
using D = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000DiagnosticConstants;

namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

/// <summary>Moteur de carte ACB-4000 relié à son port, avec stockage emprunté et tampon dual-port de 1 Kio.</summary>
/// <remarks>La carte hôte et le mécanisme sont raccordés par le propriétaire de la machine.
/// Les diagnostics utilisent les registres possédés par la carte. Les lectures non pilotées du bus
/// de diagnostic suivent la convention numérique explicitée dans la fiche du composant.</remarks>
public sealed partial class Acb4000 : IAcb4000
{
    private readonly Func<int, IScsiStorage?> _drives;
    private readonly Acb4000Positioning[] _positioners;
    private readonly Acb4000Bus _bus;
    private readonly Acb4000ReadChannel _readChannel;
    private readonly Acb4000FormatChannel _formatChannel;
    private readonly Acb4000WriteChannel _writeChannel;
    private readonly Acb4000ParameterChannel _parameterChannel;
    private readonly Acb4000SearchBudget _searchBudget = new();
    private readonly Acb4000SearchDeadline _searchDeadline;
    private readonly IAcb4000RegisterBus? _registerBus;
    private IScsiStorage? _writeDrive;
    private readonly byte[] _formatMap = new byte[F.SmallBlockInterleavedSectors];
    private readonly byte[] _command = new byte[C.LongCdbLength];
    private readonly byte[] _buffer = new byte[C.BufferSize];
    private readonly byte[] _sense = new byte[C.AddressableUnitCount];
    private readonly uint[] _senseAddress = new uint[C.AddressableUnitCount];
    private readonly bool[] _senseAddressValid = new bool[C.AddressableUnitCount];
    private readonly int[] _blockSizes = [C.DefaultBlockSize, C.DefaultBlockSize];
    private readonly IScsiStorage?[] _parameterStorage = new IScsiStorage?[C.DriveCount];
    private readonly bool[] _parametersLoaded = new bool[C.DriveCount];
    private readonly bool[] _hasFormat = new bool[C.DriveCount];
    private readonly bool[] _modeSelected = new bool[C.DriveCount];
    private readonly bool[] _sequentialLayout = new bool[C.DriveCount];
    private readonly IScsiStorage?[] _invalidFormatStorage = new IScsiStorage?[C.DriveCount];
    private readonly Acb4000DriveParameters[] _activeParameters = new Acb4000DriveParameters[C.DriveCount];
    private readonly Acb4000DriveParameters[] _modeParameters = new Acb4000DriveParameters[C.DriveCount];
    private Acb4000Stage _stage;
    private Acb4000CommandCode _code;
    private int _commandCount;
    private int _commandLength;
    private int _offset;
    private int _length;
    private uint _address;
    private uint _firstAddress;
    private int _commandBlocks;
    private byte _status;
    private byte _readError;
    private bool _readingDisk;
    private bool _writingDisk;
    private bool _matchingReadId;
    private bool _verifying;
    private int _readAttempts;
    private uint _readIdSequence;
    private bool _driveWriteFault;
    private bool _hostAckInput;

    public IAcb4000Bus Bus => _bus;
    public bool IsIdle => !_bus.Busy && _stage == Acb4000Stage.Idle
        && _aic.Sequencer.Stopped && !_sequencerBuffer.HostTransferEnabled
        && !_sequencerBuffer.PeripheralCyclePending && !_sequencerBuffer.HostCyclePending
        && !_aic.Clocks.TransferPending
        && !_sequencerBuffer.HostArbitrationPending
        && !_driveMoving[0] && !_driveMoving[1] && !_positioners[0].Busy && !_positioners[1].Busy;
    public int RemainingBlocks { get; private set; }
    internal bool ReadingParameterFields => _stage == Acb4000Stage.Parameters;
    internal bool LoadingParameters => ReadingParameterFields || _stage == Acb4000Stage.Positioning
        && _positioningPurpose == Acb4000PositioningPurpose.Parameters;
    public bool ReadGate => _readGate;
    public bool WriteGate => _writeGate;
    public void SetWriteFault(bool active)
    {
        if (_driveWriteFault == active) return;
        _driveWriteFault = active;
        UpdateDriveGates();
    }
    private int BlockSize => _blockSizes[Unit];
    private IScsiStorage? Drive => Unit < C.DriveCount ? _drives(Unit) : null;

    public Acb4000(Func<int, IScsiStorage?> drives, int ticksPerSecond,
        int sequencerTStatesPerSecond = T.RevisionBSequencerClock, IAcb4000RegisterBus? registerBus = null)
    {
        ArgumentNullException.ThrowIfNull(drives);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ticksPerSecond);
        _drives = drives;
        _registerBus = registerBus;
        _aicOwnerTicksPerSecond = ticksPerSecond;
        _aicSystemHalfCyclesPerSecond = (long)sequencerTStatesPerSecond
            * Constants.Acb4000ClockConstants.EdgesPerCycle;
        _aicSystemEdgesPerTick = (int)Math.DivRem(_aicSystemHalfCyclesPerSecond, ticksPerSecond,
            out _aicSystemEdgeRemainder);
        _sequencerBuffer = new(_buffer);
        _aic = new(new Acb4000EccRegisters(), _sequencerBuffer);
        InitializeAic();
        _readChannel = new(_aic);
        _parameterChannel = new(_aic);
        _writeChannel = new(_aic);
        _formatChannel = new(_aic);
        InitializeMemory();
        _bus = new Acb4000Bus(ticksPerSecond);
        _searchDeadline = new(ticksPerSecond, sequencerTStatesPerSecond);
        _positioners = [new(ticksPerSecond), new(ticksPerSecond)];
        _aic.Ecc.ResetChanged += _ => UpdateDriveGates();
        _writeChannel.WriteGateChanged += _ => UpdateDriveGates();
        _formatChannel.WriteGateChanged += _ => UpdateDriveGates();
        UpdateDriveGates();
    }

    public void SetInitiator(byte data, bool select, bool acknowledge, bool reset, bool attention = false)
    {
        _hostAckInput = acknowledge;
        if (_hostBufferWritePending)
        {
            _hostBufferInitiatorData = data;
            _hostBufferInitiatorSelect = select;
            _hostBufferInitiatorAck = acknowledge;
            _hostBufferInitiatorAttention = attention;
        }
        if (!reset && _hostBufferDma && _stage == Acb4000Stage.DataOut && _bus.Request && acknowledge)
        {
            if (!_sequencerBuffer.HostTransferPending)
            {
                if (!_sequencerBuffer.HostCyclePending && !_sequencerBuffer.RequestHostWrite(_bus.ResolveInitiatorData(data)))
                    throw new InvalidOperationException();
                _hostBufferWritePending = true;
                _hostBufferInitiatorData = data;
                _hostBufferInitiatorSelect = select;
                _hostBufferInitiatorAck = acknowledge;
                _hostBufferInitiatorAttention = attention;
            }
        }
        // The RAM write must complete before the target acknowledges the host's ACK by dropping REQ.
        _bus.SetInitiator(data, select, acknowledge && !_hostBufferWritePending, reset, attention);
        if (reset) ResetCommandState();
        SynchronizeHostInterface();
    }

    public void Reset()
    {
        SetInitiator(0, false, false, true);
        SetInitiator(0, false, false, false);
    }

    public void Tick()
    {
        TickAicSystemClock();
        SynchronizeHostInterface();
        CompleteHostBufferRead();
        CompleteHostBufferWrite();
        if (_eccRegisters.ShiftPending) _eccRegisters.TryClockReference(ReadGate, WriteGate);
        MaintainNativeReadCounter();
        TickPositioners();
        _bus.Tick();
        SynchronizeHostInterface();
        if (!_bus.ProtocolBusy)
        {
            AbortHostBufferDma();
            SetStage(Acb4000Stage.Idle);
            return;
        }
        if (_stage == Acb4000Stage.Idle)
        {
            if (!_bus.TryBeginTransfer(Acb4000BusPhase.Command)) return;
            _commandCount = 0;
            _commandLength = C.ShortCdbLength;
            SetStage(Acb4000Stage.Command);
            return;
        }
        if ((_stage is Acb4000Stage.Read or Acb4000Stage.Verify or Acb4000Stage.Format
            or Acb4000Stage.WriteSearch or Acb4000Stage.Write or Acb4000Stage.Parameters) && !_driveReady[Unit])
        {
            Complete(C.NotReady);
            return;
        }
        if (TickSearchTiming()) return;
        if (_stage == Acb4000Stage.Verify)
        {
            VerifyBlock();
            return;
        }
        if (_stage == Acb4000Stage.Format)
        {
            TickFormat();
            return;
        }
        if (_stage == Acb4000Stage.SearchData)
        {
            if (CheckSearchDataDrive()) ReadBlock();
            return;
        }
        if (_stage == Acb4000Stage.Read)
        {
            if (_translating) CheckTranslationDrive();
            if (_searchingData) CheckSearchDataDrive();
            return;
        }
        if (_stage == Acb4000Stage.Parameters)
        {
            if (!ReferenceEquals(Drive, _parameterStorage[Unit])) { _parameterChannel.Reset(); Complete(C.NotReady); }
            return;
        }
        if (_stage is Acb4000Stage.WriteSearch or Acb4000Stage.Write)
        {
            CheckWriteDrive();
            return;
        }
        if (_stage == Acb4000Stage.Positioning)
        {
            TickPositioningCommand();
            return;
        }
        if (_hostBufferDma && _stage == Acb4000Stage.DataOut)
        {
            if (_sequencerBuffer.HostCyclePending) return;
            _sequencerBuffer.TryTakeHostCycle(out _);
        }
        if (!_bus.TryTakeTransferredByte(out var data)) return;
        switch (_stage)
        {
            case Acb4000Stage.Command:
                _command[_commandCount++] = data;
                if (_commandCount == 1)
                    _commandLength = (data >> C.CommandGroupShift) == C.LongCommandGroup ? C.LongCdbLength : C.ShortCdbLength;
                if (_commandCount == _commandLength) { StoreCommandMemory(); ExecuteCommand(); }
                else Request(Acb4000BusPhase.Command);
                break;
            case Acb4000Stage.DataIn:
                if (_hostBufferDma) _sequencerBuffer.CompleteHostAcknowledgement();
                if (++_offset == _length) EndReadTransfer();
                else Request(Acb4000BusPhase.DataIn, _diagnosticReading ? DiagnosticReadByte(_offset)
                    : _hostBufferDma ? (byte)0 : _buffer[_offset]);
                break;
            case Acb4000Stage.DataOut:
                if (_hostBufferDma) _sequencerBuffer.CompleteHostAcknowledgement();
                if (_diagnosticPatching) { WriteDiagnosticByte(data); break; }
                if (!_hostBufferDma) _buffer[_offset] = data;
                _offset++;
                if (_offset == _length) EndWriteTransfer();
                else Request(Acb4000BusPhase.DataOut);
                break;
            case Acb4000Stage.Status:
                SetStage(Acb4000Stage.Message);
                Request(Acb4000BusPhase.MessageIn, C.CommandCompleteMessage);
                break;
            case Acb4000Stage.Message:
                if (!_bus.TryReleaseBus()) throw new InvalidOperationException();
                SetStage(Acb4000Stage.Idle);
                break;
        }
        SynchronizeHostInterface();
    }

    private void Request(Acb4000BusPhase phase, byte data = 0)
    {
        if (phase == Acb4000BusPhase.DataIn && _hostBufferDma)
        {
            BeginHostBufferRead();
            return;
        }
        if (phase is Acb4000BusPhase.DataIn or Acb4000BusPhase.DataOut
            && _code is Acb4000CommandCode.SendDiagnostic or Acb4000CommandCode.ReceiveDiagnostic)
            DiagnosticBytesRemaining--;
        if (!_bus.TryBeginTransfer(phase, data)) throw new InvalidOperationException();
    }

    private void Complete(byte error = C.NoSense, bool addressValid = false)
    {
        if (_reinitializing && error != C.NoSense) error = D.ReinitializeFailure;
        if (error != C.NoSense)
        {
            _sense[Unit] = error;
            _senseAddress[Unit] = _address;
            _senseAddressValid[Unit] = addressValid && _address <= C.MaximumShortAddress;
        }
        CompleteStatus(error == C.NoSense ? (byte)0 : C.CheckStatus);
    }

    private void CompleteStatus(byte status)
    {
        EndNativeRead();
        EndHostBufferDma();
        _searchDeadline.Reset();
        _diagnosticReading = _diagnosticPatching = false;
        SaveDiagnosticFlags();
        _status = status;
        SetStage(Acb4000Stage.Status);
        Request(Acb4000BusPhase.Status, _status);
    }

    private void Reply(int length)
    {
        _searchDeadline.Reset();
        _diagnosticReading = _diagnosticPatching = false;
        _offset = 0;
        _length = length;
        if (length == 0) { Complete(); return; }
        BeginHostBufferDma(length, outgoing: true);
        SetStage(Acb4000Stage.DataIn);
        Request(Acb4000BusPhase.DataIn, _hostBufferDma ? (byte)0 : _buffer[0]);
    }

    private void Receive(int length)
    {
        _searchDeadline.Reset();
        _diagnosticReading = _diagnosticPatching = false;
        _offset = 0;
        _length = length;
        BeginHostBufferDma(length, outgoing: false);
        SetStage(Acb4000Stage.DataOut);
        Request(Acb4000BusPhase.DataOut);
    }

    private void ResetCommandState()
    {
        InitializeAic();
        _hostBufferDma = false;
        _hostBufferReadPending = false;
        _hostBufferWritePending = false;
        _diagnosticReading = _diagnosticPatching = false;
        _diagnosticAddress = _diagnosticReadAddress = 0;
        _diagnosticPatchRemaining = 0;
        _searchDeadline.Reset();
        SetStage(Acb4000Stage.Idle);
        _commandCount = _offset = _length = Unit = RemainingBlocks = 0;
        _address = _firstAddress = 0;
        _commandBlocks = 0;
        _readingDisk = _writingDisk = false;
        _matchingReadId = _verifying = false;
        _translating = _translationIndexLevel = _translationIndexSeen = false;
        _translationDrive = null;
        _searchingData = _receivingSearchPattern = _searchInverted = false;
        _searchDataDrive = null;
        _reinitializing = _completeAfterParameterAcquisition = false;
        _translationOrdinal = 0;
        _readAttempts = 0;
        _readChannel.Reset();
        _searchBudget.Reset();
        _formatChannel.Reset();
        _writeChannel.Reset();
        _parameterChannel.Reset();
        _writeDrive = null;
        _formatTrackPending = false;
        _formatDefectLength = 0;
        FormatCylinder = FormatHead = 0;
        SelectedHead = 0;
        _readError = _status = 0;
        _formatDrive = null;
        _formatHeaderReceived = false;
        ResetPositioners();
        ResetStatistics();
        Array.Clear(_sense);
        Array.Clear(_senseAddress);
        Array.Clear(_senseAddressValid);
        Array.Clear(_parametersLoaded);
        Array.Clear(_parameterStorage);
        Array.Clear(_hasFormat);
        Array.Clear(_modeSelected);
        Array.Clear(_sequentialLayout);
    }

    private static bool StorageFailure(Exception error) => error is IOException or UnauthorizedAccessException
        or ArgumentException or NotSupportedException;
}
