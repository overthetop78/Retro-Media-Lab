using RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Enums;
using C = RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Constants.Upd765Constants;
namespace RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Services;
public sealed partial class Upd765
{
    private bool _faultReset;
    private bool _indexPin;
    private long _indexSkipClocks;
    private void SetFaultReset(bool high)
    {
        if (_faultReset == high) return;
        _faultReset = high;
        FaultResetChanged?.Invoke(high);
    }
    /// <summary>Index fourni par le lecteur ; aucun index ou temps de rotation synthétique.</summary>
    public void SetIndex(bool high)
    {
        var rising = high && !_indexPin;
        _indexPin = high;
        if (!rising || _resetAsserted || PowerClockStopped) return;
        _indexSkipClocks = _model == Upd765Model.Upd72065B
            ? C.CmosIndexSkipClocks : C.IndexSkipClocks;
        if (!Active) return;
        if (_serialPhase == Upd765SerialPhase.AwaitIndex)
        {
            _indexCount = 0;
            if (Formatting) BeginFormat();
            else _serialPhase = Upd765SerialPhase.SearchId;
            return;
        }
        if (Formatting && _serialPhase == Upd765SerialPhase.Format)
        {
            _record++;
            FinishData();
        }
        else if (_serialPhase is Upd765SerialPhase.SearchId or Upd765SerialPhase.Id or Upd765SerialPhase.SearchData)
        {
            if (++_indexCount < C.SearchRevolutions) return;
            if (_serialPhase == Upd765SerialPhase.SearchData) FinishData(C.MissingAddressMark, C.MissingDataMark);
            else FinishData((byte)(C.NoData | (_sawId ? 0 : C.MissingAddressMark)), _wrongCylinder);
        }
    }
    public void SetReadSignals(bool window, bool readData)
    {
        if (!_resetAsserted && !PowerClockStopped && Active && HeadLoaded && !_writer.Gate
            && !(_serialPhase == Upd765SerialPhase.SearchId && _indexSkipClocks > 0))
            _reader.SetInputs(window, readData);
    }
    public void SetWriteClock(bool high)
    {
        if (_resetAsserted || PowerClockStopped) return;
        if (high && !_writeClockPin && _syncGapCells > 0) _syncGapCells--;
        if (high && !_writeClockPin && _writeGapCells > 0)
        {
            _writeGapCells--;
            _writeClockPin = high;
            _writer.SetClock(high);
            if (_writeGapCells == 0) _writer.SetGate(true);
            return;
        }
        _writeClockPin = high;
        _writer.SetClock(high);
    }
}
