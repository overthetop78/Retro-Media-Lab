using RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Interfaces;
using Phase = RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Enums.Intel8271Phase;
using SerialPhase = RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Enums.Intel8271SerialPhase;
using C = RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Constants.Intel8271Constants;

namespace RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Services;

/// <summary>Contrôleur disquette 8271 : registres, séquence de commandes et demandes de transfert non DMA.</summary>
/// <remarks>Le propriétaire de la carte avance l’horloge et raccorde les broches du lecteur.
/// Le composant ne connaît ni les images, ni les conteneurs, ni leur mécanique.</remarks>
public sealed partial class Intel8271 : IIntel8271
{
    private readonly int _clock;
    private readonly int _chipClock;
    private readonly byte[] _special = new byte[C.SpecialRegisterMask + 1];
    private readonly int _transferDeadlineTicks;
    private int _unloadCount;
    private bool _resetAsserted;
    private bool _dataRequest;
    private readonly byte[] _parameters = new byte[C.ParameterCapacity], _tracks = new byte[2];
    private readonly long[] _seekSteps = new long[2];
    private readonly long[] _readBytes = new long[2], _writtenBytes = new long[2];
    public long SeekSteps(int drive) => _seekSteps[drive];
    public long ReadBytes(int drive) => _readBytes[drive];
    public long WrittenBytes(int drive) => _writtenBytes[drive];
    private byte _status, _result, _data, _command, _driveOutput, _mode;
    private int _parameterCount, _requiredParameters, _drive, _sector, _remaining, _sectorSize, _offset;
    private int _delay, _stepTicks, _settleTicks, _loadTicks;
    private byte[] _buffer = [];
    private bool _awaitingData;
    private byte _deletedResult;
    private Phase _phase;
    public bool Drq => _dataRequest && (_mode & C.NonDmaMode) == 0;
    public bool ResetAsserted => _resetAsserted;
    public byte DriveControl => _driveOutput;
    public bool Nmi => (_status & C.Interrupt) != 0;
    public bool Motor => (_driveOutput & C.OutputLoadHead) != 0;
    public int ActiveDrive => _drive;
    public bool Reading => _phase is Phase.Read or Phase.Id or Phase.Verify or Phase.Scan or Phase.ScanTail;
    public bool Writing => _phase is Phase.Write or Phase.Format;
    public bool Active => _phase != Phase.Idle;
    /// <summary>Accélère les délais mécaniques ; le transfert non DMA conserve son horloge matérielle.</summary>
    private int _mechanicalSpeedPercent = C.PercentageScale;
    public int MechanicalSpeedPercent
    {
        get => _mechanicalSpeedPercent;
        set { ArgumentOutOfRangeException.ThrowIfNegative(value); _mechanicalSpeedPercent = value; }
    }

    /// <summary>Contrôleur physique : le propriétaire fournit les niveaux et impulsions du lecteur.</summary>
    public Intel8271(int ticksPerSecond = C.DefaultOwnerClock, int chipClock = C.MiniClock)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(ticksPerSecond, C.FmBytesPerSecond);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(chipClock);
        _clock = ticksPerSecond;
        _chipClock = chipClock;
        _writeGateDelayCycles = Math.Max(1, checked((int)((long)chipClock * C.WriteGateDelayMicroseconds / C.MicrosecondsPerSecond)));
        _transferDeadlineTicks = Math.Max(1, checked((int)((long)_clock * C.DmaDeadlineChipCycles / chipClock)));
        Reset();
    }

    /// <summary>Réinitialise le contrôleur sans modifier les niveaux externes fournis par la carte.</summary>
    public void Reset()
    {
        _resetAsserted = false;
        ResetSerialRead();
        _status = _result = _data = _command = _driveOutput = 0;
        ResetBus();
        _mode = C.ModeFixed;
        _dataRequest = false;
        Array.Clear(_special);
        _special[C.Mode] = _mode;
        _special[C.StepRate] = C.DefaultStepRate;
        _special[C.SettleTime] = C.DefaultSettleTime;
        _special[C.LoadUnload] = C.DefaultLoadUnload;
        _special[C.BadTrack00] = _special[C.BadTrack01] = _special[C.BadTrack10] = _special[C.BadTrack11] = byte.MaxValue;
        _parameterCount = _requiredParameters = _drive = _sector = _remaining = _offset = _delay = 0;
        _seekRemaining = 0;
        _externalCount = false;
        Array.Clear(_notReadyLatched);
        UpdateTiming();
        Array.Clear(_tracks); Array.Clear(_parameters); _buffer = []; _awaitingData = false; _phase = Phase.Idle;
    }

    public byte Read(int register)
    {
        switch ((register & 4) != 0 ? 4 : register & C.RegisterMask)
        {
            case 0: return (byte)(_status | (_commandPending ? C.CommandFull : 0) | (_parameterPending ? C.ParameterFull : 0));
            case 1: _status &= unchecked((byte)~(C.ResultFull | C.Interrupt)); return _result;
            case 4:
                if (_awaitingData && Reading) _readBytes[_drive]++;
                AcknowledgeData();
                return _data;
            default: return 0;
        }
    }

    public void Write(int register, byte value)
    {
        if (_resetAsserted && (register & C.RegisterMask) != 2) return;
        switch ((register & 4) != 0 ? 4 : register & C.RegisterMask)
        {
            case 0: Command(value); break;
            case 1:
                if ((_status & C.Busy) == 0 || _parameterCount >= _requiredParameters) return;
                _parameters[_parameterCount++] = value;
                if (_parameterCount == _requiredParameters) BeginCommand();
                break;
            case 2: SetReset((value & 1) != 0); break;
            case 4:
                _data = value;
                if (_awaitingData) CaptureSerialWrite(value);
                if (_awaitingData && _phase == Phase.Scan) CompareScanByte(value);
                AcknowledgeData();
                break;
        }
    }

    /// <summary>Avance positionnement et échéances des octets ; un octet non acquitté produit Late DMA.</summary>
    public void Tick()
    {
        if (_resetAsserted) return;
        if (_commandPending || _parameterPending) TickBusBuffers();
        if (_phase == Phase.Idle) return;
        if (_serialPhase != SerialPhase.Inactive) { TickSerialRead(); return; }
        if (_delay > 0 && --_delay > 0) return;
        if (AdvancePositioning()) return;
    }

    private void Complete(byte result, bool interrupt = true)
    {
        StopSerialRead();
        _driveOutput &= unchecked((byte)~(C.OutputWriteEnable | C.OutputStep));
        _result = result; _status = (byte)(C.ResultFull | (interrupt ? C.Interrupt : 0));
        _phase = Phase.Idle; _awaitingData = _dataRequest = false; _buffer = []; _delay = 0;
        _unloadCount = _special[C.LoadUnload] >> 4;
        if (_unloadCount == 0 && Motor) UnloadHead();
    }
}
