using RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Enums;
using C = RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Constants.Upd765Constants;

namespace RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Services;

public sealed partial class Upd765
{
    private bool _resetAsserted;
    private bool _busReading;
    private bool _busWriting;
    private bool _busDma;
    private bool _busDataRegister;
    private byte _busWriteValue;
    private byte _busReadValue;
    private int _busReadOffset;
    private Upd765BusAccess _busReadAccess;

    /// <summary>DB0..7 pendant RD ; null signifie haute impédance.</summary>
    public byte? BusData => !_busReading || _resetAsserted ? null
        : _busReadAccess == Upd765BusAccess.Status ? ReadMainStatus() : _busReadValue;

    /// <summary>Niveaux logiques actifs des strobes. A0 haut sélectionne DATA ; DACK force DATA.</summary>
    /// <remarks>Les broches /CS, /RD, /WR et /DACK sont actives à zéro sur le boîtier.
    /// Cette API reçoit true lorsqu'elles sont actives. Les accès aux registres sont
    /// acquittés à la fin du strobe, sans dupliquer l'opération si celui-ci est étiré.</remarks>
    public void SetBus(bool selected, bool read, bool write, bool dataRegister,
        bool dmaAcknowledge = false, byte value = C.OpenBus)
    {
        if (_resetAsserted) { ResetBus(); return; }
        if (dmaAcknowledge && !_dmaAcknowledged) AcknowledgeDma();
        var previousDrq = Drq;
        _dmaAcknowledged = dmaAcknowledge;
        if (!dmaAcknowledge) _dmaSuppressed = false;
        if (previousDrq != Drq) DataRequestChanged?.Invoke(Drq);
        if (_resetAsserted) { ResetBus(); return; }
        var addressed = selected || dmaAcknowledge;
        var reading = addressed && read && !write;
        var writing = addressed && write && !read && (dataRegister || dmaAcknowledge || _model == Upd765Model.Upd72065B);

        if (_busReading && !reading)
        {
            _busReading = false;
            if (!read && !write && addressed) CompleteBusRead();
            _busReadAccess = Upd765BusAccess.None;
        }
        if (_busWriting && !writing)
        {
            _busWriting = false;
            if (!read && !write && addressed)
            {
                if (_busDma) WriteDma(_busWriteValue);
                else if (_busDataRegister) WriteFifo(_busWriteValue);
                else WriteAuxiliaryCommand(_busWriteValue);
            }
        }
        // Une notification d'acquittement peut avoir activé RESET.
        if (_resetAsserted) { ResetBus(); return; }

        if (reading && !_busReading)
        {
            _busDma = dmaAcknowledge;
            _busDataRegister = dataRegister || dmaAcknowledge;
            BeginBusRead();
        }
        _busReading = reading;
        if (!writing) return;
        _busWriting = true;
        _busDma = dmaAcknowledge;
        _busDataRegister = dataRegister || dmaAcknowledge;
        _busWriteValue = value;
    }

    /// <summary>Maintient RESET actif ; les paramètres SRT/HUT/HLT sont conservés.</summary>
    public void SetReset(bool asserted)
    {
        if (_resetAsserted == asserted) return;
        _resetAsserted = asserted;
        if (asserted) Reset();
        else
        {
            ResetBus();
            _drives.ReleaseReset();
        }
    }

    private void BeginBusRead()
    {
        _busReadValue = C.OpenBus;
        _busReadAccess = Upd765BusAccess.None;
        if (!_busDataRegister) { _busReadAccess = Upd765BusAccess.Status; return; }
        if (PowerClockStopped) return;
        if (_phase == Upd765Phase.Result && _delay == 0)
        {
            _busReadAccess = Upd765BusAccess.Result;
            _busReadOffset = _resultOffset;
            _busReadValue = _results[_resultOffset];
        }
        else if (_phase == Upd765Phase.Reading && _request && _busDma != _drives.NonDma)
        {
            _busReadAccess = Upd765BusAccess.Data;
            _busReadOffset = _hostOffset;
            _busReadValue = _dataLatch;
        }
    }

    private void CompleteBusRead()
    {
        if (_busReadAccess == Upd765BusAccess.Result && _phase == Upd765Phase.Result
            && _delay == 0 && _resultOffset == _busReadOffset) ReadFifo();
        else if (_busReadAccess == Upd765BusAccess.Data && _phase == Upd765Phase.Reading
            && _request && _hostOffset == _busReadOffset) ConsumeRead();
    }

    private void ResetBus()
    {
        _busReading = _busWriting = _busDma = _busDataRegister = false;
        _busReadAccess = Upd765BusAccess.None;
        _busReadValue = _busWriteValue = C.OpenBus;
        _busReadOffset = 0;
    }
}
