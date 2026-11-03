using RetroMediaLab.Chips.Network.Motorola.M6800.Chip6854.Enums;
using C = RetroMediaLab.Chips.Network.Motorola.M6800.Chip6854.Constants.Mc6854Constants;

namespace RetroMediaLab.Chips.Network.Motorola.M6800.Chip6854.Services;

public sealed partial class Mc6854
{
    /// <summary>Plus aucun événement numérique ne peut survenir sur ces bits de mark idle constants.</summary>
    private bool CanAdvanceStableIdle()
    {
        if (!_lineIdle || _packetBits is not null || _transmitCount != 0 || _transmitting ||
            _transmitBits != 0 || _transmitPhase != AdlcTransmitPhase.Idle ||
            _abortRequested || _finishPending || _markIdlePrefix || _loopPrefixZero ||
            (_control2 & C.FlagIdle) != 0 || (_control3 & (C.Loop | C.Test)) != 0 ||
            (_rtsAsserted && (_control2 & C.Rts) == 0) || _flagOutput)
            return false;
        var receiveBlocked = _resetAsserted || (_control1 & C.ReceiveReset) != 0 || !_carrierPresent || _dcdRaised;
        if (!receiveBlocked && _rawOnes != C.IdleOnes)
            return false;
        var incoming = (_control4 & C.Nrzi) == 0 || _packetLineLevel;
        return _txData == _nrziLevel && _rxData == incoming && _rxLastLevel == incoming;
    }
}
