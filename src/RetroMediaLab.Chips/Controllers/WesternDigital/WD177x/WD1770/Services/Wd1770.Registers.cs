using C = RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Constants.Wd1770Constants;

namespace RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Services;

public sealed partial class Wd1770
{
    /// <summary>Accès atomique d'une carte ne détaillant pas les fronts du bus.</summary>
    public byte Read(int register)
    {
        register &= C.RegisterMask;
        var value = PeekRegister(register);
        AcknowledgeRead(register);
        return value;
    }

    private byte PeekRegister(int register)
    {
        switch (register)
        {
            case C.StatusCommandRegister:
                var status = (byte)(_errors | (Active ? C.Busy : 0) | (Motor ? C.Motor : 0));
                if (_typeOne)
                {
                    if (_trackZero) status |= C.TrackZeroStatus;
                    if (_index) status |= C.IndexStatus;
                    if (_spinComplete) status |= C.SpinUpStatus;
                    if (_writeProtected) status |= C.WriteProtected;
                }
                else if (_drq) status |= C.DataRequest;
                return status;
            case C.TrackRegister: return _track;
            case C.SectorRegister: return _sector;
            default: return _data;
        }
    }

    private void AcknowledgeRead(int register)
    {
        if (register == C.StatusCommandRegister)
        {
            if (!_forceImmediate) _irq = false;
        }
        else if (register == C.DataRegister)
        {
            if (_drq && Reading) DataBytesRead++;
            _drq = false;
        }
    }

    public void Write(int register, byte value)
    {
        switch (register & C.RegisterMask)
        {
            case C.StatusCommandRegister: Command(value); break;
            case C.TrackRegister: if (!Active) _track = value; break;
            case C.SectorRegister: if (!Active) _sector = value; break;
            case C.DataRegister:
                _data = value;
                if (Writing)
                {
                    if (_drq) DataBytesWritten++;
                    _writeLatch = value;
                    _writeLatchFull = true;
                    if (_phase == Enums.Wd1770Phase.WriteTrack) _writer.SetLookAhead(FormatLookAhead(value));
                    else if (_phase == Enums.Wd1770Phase.WriteData && _remaining > 0) _writer.SetLookAhead(value);
                }
                _drq = false;
                break;
        }
    }
}
