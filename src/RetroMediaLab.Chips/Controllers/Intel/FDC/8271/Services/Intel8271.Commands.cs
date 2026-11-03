using Phase = RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Enums.Intel8271Phase;
using C = RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Constants.Intel8271Constants;

namespace RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Services;

/// <summary>Décodage des commandes et registres de configuration du 8271.</summary>
public sealed partial class Intel8271
{
    private void Command(byte value)
    {
        if ((_status & C.Busy) != 0) return;
        AcceptCommand(value);
    }

    private void AcceptCommand(byte value)
    {
        _seekRetryCount = 0;
        _command = (byte)(value & C.CommandMask); _drive = (value & C.Drive1Select) != 0 ? 1 : 0;
        Array.Clear(_parameters); _parameterCount = 0; _awaitingData = _dataRequest = false; _status = C.Busy; _deletedResult = 0;
        _requiredParameters = _command switch
        {
            C.DriveStatus => 0,
            C.Seek or C.ReadSpecial => 1,
            C.ReadSingle or C.ReadIncludingDeletedSingle or C.WriteSingle or C.WriteDeletedSingle or C.VerifySingle or C.WriteSpecial => 2,
            C.ReadMultiple or C.ReadIncludingDeletedMultiple or C.WriteMultiple or C.WriteDeletedMultiple or C.VerifyMultiple or C.ReadId => 3,
            C.Specify => 4,
            C.Format or C.ScanData or C.ScanIncludingDeleted => 5,
            _ => -1
        };
        if (_requiredParameters < 0) Complete(C.NotFound);
        else if (_requiredParameters == 0) BeginCommand();
    }

    private void BeginCommand()
    {
        switch (_command)
        {
            case C.DriveStatus:
                Array.Clear(_notReadyLatched);
                Complete(DriveInputs(), false); return;
            case C.Specify:
                Specify();
                _status = 0;
                return;
            case C.WriteSpecial:
                WriteSpecial(_parameters[0], _parameters[1]);
                _status = 0;
                return;
            case C.ReadSpecial:
                Complete(ReadSpecial(_parameters[0]), false);
                return;
        }
        SelectCommandDrive();
        if (!CheckDrive()) return;
        _remaining = _command is C.ReadSingle or C.ReadIncludingDeletedSingle or C.WriteSingle or C.WriteDeletedSingle or C.VerifySingle ? 1 : (_parameters[2] & 31) == 0 ? 32 : _parameters[2] & 31;
        _sectorSize = _command is C.ReadSingle or C.ReadIncludingDeletedSingle or C.WriteSingle or C.WriteDeletedSingle or C.VerifySingle ? 128 : 128 << (_parameters[2] >> 5);
        _sector = _command == C.ReadId ? 0 : _parameters[1];
        if (_command is C.ScanData or C.ScanIncludingDeleted) BeginScan();
        BeginPositioning();
    }

    private void Positioned()
    {
        if (!CheckDrive()) return;
        if (_command == C.Seek) { Complete(0); return; }
        if (_command == C.Format) StartSerialFormat();
        else StartSerialRead();
    }
}
