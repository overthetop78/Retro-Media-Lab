using C = RetroMediaLab.Chips.Video.Philips.SAA50xx.SAA5050.Constants.Saa5050Constants;

namespace RetroMediaLab.Chips.Video.Philips.SAA50xx.SAA5050.Services;

public sealed partial class TeletextSaa5050
{
    private byte _data;
    private bool _f1;
    private bool _tr6;
    private bool _lose;
    private bool _dew;
    private bool _remoteClock;
    private bool _generalLineResetHigh = true;
    private bool _remoteHalf;
    private int _remoteBit;
    private byte _remoteCommand;
    private int _scanline;
    private int _characterRow;
    private int _flashField;

    public byte Data { get => _data; set => _data = (byte)(value & C.CharacterDataMask); }
    public bool CharacterRoundingSelect { get; set; } = true;
    public bool BigCharacterSelectHigh { get; set; } = true;
    public bool RemoteData { get; set; } = true;
    public int CharacterRow => _characterRow;
    public int CharacterScanline => _scanline;
    public int FlashField => _flashField;
    public bool TransmittedLargeCharacterHigh => !_rowDouble;

    /// <summary>F1 : données D1–D7 prises au front descendant (figure 4).</summary>
    public void SetCharacterClock(bool level)
    {
        if (_f1 == level) return;
        _f1 = level;
        if (level) return;
        ClockRemoteTimer();
        var raster = _scanline * C.RasterParities + (CharacterRoundingSelect ? 0 : 1);
        QueueInput(_data, raster, _flashField, _characterRow);
    }

    /// <summary>TR6 : chaque transition représente un demi-point de la sortie arrondie.</summary>
    public void SetDotClock(bool level)
    {
        if (_tr6 == level) return;
        _tr6 = level;
        AdvanceHalfDot();
    }

    public void SetLoadOutputShiftEnable(bool level)
    {
        if (_lose == level) return;
        _lose = level;
        QueueWindow(level && !_dew);
        if (level)
        {
            if (!_dew) BeginLine(_characterRow);
        }
        else if (!_dew)
        {
            if (++_scanline == (BigCharacterSelectHigh ? C.CharacterRows : C.CharacterRows * C.RasterParities))
            {
                _scanline = 0;
                _characterRow++;
            }
        }
    }

    /// <summary>DEW maintient le compteur de ligne au début du champ ; un front suffit au flash.</summary>
    public void SetDataEntryWindow(bool level)
    {
        if (_dew == level) return;
        _dew = level;
        if (level)
        {
            _scanline = 0;
            _characterRow = 0;
            _row = -1;
            _rowDouble = _bottom = false;
        }
        else _flashField = (_flashField + 1) % C.FlashFields;
    }

    /// <summary>GLR est actif bas et resynchronise le diviseur de réception DLIM.</summary>
    public void SetGeneralLineReset(bool level)
    {
        _generalLineResetHigh = level;
        if (!level) _remoteHalf = false;
    }

    /// <summary>Deux fronts montants DLIM par bit, ordre de transmission b7,b1,...,b6.</summary>
    public void SetRemoteClock(bool level)
    {
        if (_remoteClock == level) return;
        _remoteClock = level;
        if (!level || !_generalLineResetHigh) return;
        _remoteHalf = !_remoteHalf;
        if (_remoteHalf) return;
        var bit = _remoteBit == 0 ? C.RemoteFirstBit : _remoteBit - 1;
        if (RemoteData) _remoteCommand |= (byte)(1 << bit);
        if (++_remoteBit != C.RemoteCommandBits) return;
        ReceiveRemoteCommand(_remoteCommand);
        _remoteBit = 0;
        _remoteCommand = 0;
    }

    private void ResetSignals()
    {
        _data = 0;
        _f1 = _tr6 = _lose = _dew = _remoteClock = false;
        _remoteHalf = false;
        _remoteBit = 0;
        _remoteCommand = 0;
        _generalLineResetHigh = true;
        _scanline = _characterRow = _flashField = 0;
        ResetPipeline();
    }
}
