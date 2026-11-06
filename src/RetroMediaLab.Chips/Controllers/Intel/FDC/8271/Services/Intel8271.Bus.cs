using C = RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Constants.Intel8271Constants;

namespace RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Services;

public sealed partial class Intel8271
{
    private bool _commandPending;
    private bool _parameterPending;
    private byte _bufferedCommand;
    private byte _bufferedParameter;
    private long _registerClockFraction;
    private bool _busReading;
    private bool _busWriting;
    private bool _busSelected;
    private bool _busDack;
    private int _busRegister;
    private byte _busWriteValue;

    /// <summary>Valeur pilotée pendant le cycle RD sélectionné ; null signifie haute impédance.</summary>
    public byte? BusData { get; private set; }

    /// <summary>Niveaux logiques actifs de CS/RD/WR/DACK. Une écriture est échantillonnée au relâchement de WR.</summary>
    public void SetBus(bool selected, bool read, bool write, bool dmaAcknowledge, int register, byte value = 0)
    {
        var addressed = selected || dmaAcknowledge;
        var reading = addressed && read && !write;
        var writing = addressed && write && !read;
        if (_busWriting && !write && !read)
            WriteBus(_busSelected, _busDack, _busRegister, _busWriteValue);

        if (reading && !_busReading) BusData = ReadBus(selected, dmaAcknowledge, register);
        else if (!reading) BusData = null;
        _busReading = reading;
        _busWriting = writing;
        if (!writing) return;
        _busSelected = selected;
        _busDack = dmaAcknowledge;
        _busRegister = register;
        _busWriteValue = value;
    }

    private void ResetBus()
    {
        _commandPending = _parameterPending = _busReading = _busWriting = false;
        _bufferedCommand = _bufferedParameter = _busWriteValue = 0;
        _registerClockFraction = 0;
        _busRegister = 0;
        _busDack = _busSelected = false;
        BusData = null;
    }

    private void BufferBusWrite(int register, byte value)
    {
        if (_resetAsserted && register != 2) return;
        switch (register)
        {
            case 0:
                if ((_status & C.Busy) != 0) return; // Une commande pendant Busy est illégale selon Intel.
                if (!_parameterPending) _registerClockFraction = 0;
                _bufferedCommand = value;
                _commandPending = true;
                SetStatus(C.Busy);
                break;
            case 1:
                if (!_commandPending && !_parameterPending) _registerClockFraction = 0;
                _bufferedParameter = value; // Intel : écrire avec Parameter Full écrase le paramètre précédent.
                _parameterPending = true;
                break;
            default:
                Write(register, value);
                break;
        }
        PublishClockEvent();
    }

    private void TickBusBuffers()
    {
        // Frontière nominale d'un cycle de contrôleur. Intel ne publie pas la latence interne du microprogramme.
        // Read/Write restent les accès atomiques pour les cartes qui ne modélisent pas ces cycles de bus.
        _registerClockFraction += _chipClock;
        while (_registerClockFraction >= _clock && (_commandPending || _parameterPending))
        {
            _registerClockFraction -= _clock;
            if (_commandPending)
            {
                _commandPending = false;
                AcceptCommand(_bufferedCommand);
            }
            else
            {
                _parameterPending = false;
                Write(1, _bufferedParameter);
            }
        }
    }
}
