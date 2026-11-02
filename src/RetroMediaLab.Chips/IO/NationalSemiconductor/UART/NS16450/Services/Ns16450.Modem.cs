using C = RetroMediaLab.Chips.IO.NationalSemiconductor.UART.NS16450.Constants.Ns16450Constants;

namespace RetroMediaLab.Chips.IO.NationalSemiconductor.UART.NS16450.Services;

public sealed partial class Ns16450
{
    private bool _cts = true;
    private bool _dsr = true;
    private bool _ring = true;
    private bool _dcd = true;
    private bool _txd = true;
    private bool _dtr = true;
    private bool _rts = true;
    private bool _out1 = true;
    private bool _out2 = true;
    private byte _modemStatus;
    private byte _modemDelta;

    public void SetCts(bool high)
    {
        if (_cts == high) return;
        _cts = high;
        RefreshModem();
    }

    public void SetDsr(bool high)
    {
        if (_dsr == high) return;
        _dsr = high;
        RefreshModem();
    }

    public void SetRing(bool high)
    {
        if (_ring == high) return;
        _ring = high;
        RefreshModem();
    }

    public void SetDcd(bool high)
    {
        if (_dcd == high) return;
        _dcd = high;
        RefreshModem();
    }

    private byte ExternalModemStatus() => (byte)((_cts ? 0 : C.StatusCts)
        | (_dsr ? 0 : C.StatusDsr) | (_ring ? 0 : C.StatusRing) | (_dcd ? 0 : C.StatusDcd));

    private void RefreshModem()
    {
        var status = Loopback ? (byte)(((_modemControl & C.Rts) != 0 ? C.StatusCts : 0)
            | ((_modemControl & C.Dtr) != 0 ? C.StatusDsr : 0)
            | ((_modemControl & C.Out1) != 0 ? C.StatusRing : 0)
            | ((_modemControl & C.Out2) != 0 ? C.StatusDcd : 0)) : ExternalModemStatus();
        var changed = _modemStatus ^ status;
        if ((changed & C.StatusCts) != 0) _modemDelta |= C.DeltaCts;
        if ((changed & C.StatusDsr) != 0) _modemDelta |= C.DeltaDsr;
        if ((changed & C.StatusDcd) != 0) _modemDelta |= C.DeltaDcd;
        if ((_modemStatus & C.StatusRing) != 0 && (status & C.StatusRing) == 0)
            _modemDelta |= C.TrailingRing;
        _modemStatus = status;
        UpdateInterrupt();
    }

    private void RefreshOutputs()
    {
        PublishPin(ref _txd, Loopback || (_transmitLine && (_lineControl & C.Break) == 0), TransmitChanged);
        PublishPin(ref _dtr, Loopback || (_modemControl & C.Dtr) == 0, DtrChanged);
        PublishPin(ref _rts, Loopback || (_modemControl & C.Rts) == 0, RtsChanged);
        PublishPin(ref _out1, Loopback || (_modemControl & C.Out1) == 0, Out1Changed);
        PublishPin(ref _out2, Loopback || (_modemControl & C.Out2) == 0, Out2Changed);
    }

    private static void PublishPin(ref bool pin, bool high, Action<bool>? changed)
    {
        if (pin == high) return;
        pin = high;
        changed?.Invoke(high);
    }
}
