using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Enums;
using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Interfaces;
using C = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000BusConstants;

namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

/// <summary>Port parallèle de la carte ACB-4000 : sélection, phases et quatre fronts REQ/ACK.</summary>
/// <remarks>Le moteur de commandes demande chaque octet et récupère sa fin après retrait d'ACK.
/// Une phase reste stable jusque-là. Tick représente la cadence fournie par le propriétaire.
/// La réponse de sélection nominale utilise le délai de stabilisation ; le timeout nominal est le minimum
/// constructeur. Ce ne sont pas des mesures du firmware interne. Aucun arbitrage ou reconnect n'est ajouté.</remarks>
public sealed class Acb4000Bus : IAcb4000Bus
{
    private readonly byte _targetMask;
    private readonly int _settleTicks;
    private readonly int _deskewTicks;
    private readonly int _timeoutTicks;
    private byte _transferredData;
    private bool _select;
    private bool _acknowledge;
    private bool _selectPending;
    private bool _transferPending;
    private bool _waitingAckLow;
    private int _delay;
    private int _timeout;
    private bool _protocolBusy;
    private bool _manualBusy;
    private bool _manualSelect;
    private bool _manualDataEnabled;
    private byte _manualData;
    private byte _protocolData;
    private byte _initiatorData;
    private bool _dmaControlsData;

    public Acb4000BusPhase Phase { get; private set; } = Acb4000BusPhase.BusFree;
    public bool Busy => _protocolBusy || _manualBusy;
    public bool Select => _select || _manualSelect;
    internal bool ProtocolBusy => _protocolBusy;
    public bool Request { get; private set; }
    public bool InputOutput => IsInformationPhase && ((byte)Phase & C.InputOutputMask) != 0;
    public bool ControlData => IsInformationPhase && ((byte)Phase & C.ControlDataMask) != 0;
    public bool Message => IsInformationPhase && ((byte)Phase & C.MessageMask) != 0;
    public bool Attention { get; private set; }
    public bool ResetAsserted { get; private set; }
    public bool TimedOut { get; private set; }
    private bool ProtocolDrivesData => _protocolBusy && InputOutput && !_dmaControlsData;
    public byte OutputData => _manualDataEnabled
        ? (byte)(_manualData | (ProtocolDrivesData ? _protocolData : 0))
        : _dmaControlsData ? (byte)0 : _protocolData;
    public bool TransferCompleted { get; private set; }
    private bool IsInformationPhase => (byte)Phase <= C.InformationPhaseMask;

    public Acb4000Bus(int ticksPerSecond, byte targetId = C.DefaultTargetId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ticksPerSecond);
        if (targetId > C.MaximumTargetId) throw new ArgumentOutOfRangeException(nameof(targetId));
        _targetMask = (byte)(1 << targetId);
        _settleTicks = ToTicks(C.BusSettleNanoseconds, ticksPerSecond);
        _deskewTicks = ToTicks(C.DeskewNanoseconds, ticksPerSecond);
        _timeoutTicks = ToTicks(C.MinimumRequestTimeoutNanoseconds, ticksPerSecond);
    }

    public void SetInitiator(byte data, bool select, bool acknowledge, bool reset, bool attention = false)
    {
        _initiatorData = data;
        _select = select;
        _acknowledge = acknowledge;
        Attention = attention;
        ResetAsserted = reset;
        if (reset)
        {
            _manualBusy = _manualSelect = _manualDataEnabled = _dmaControlsData = false;
            Release();
            TimedOut = false;
            return;
        }
        if (!_protocolBusy)
        {
            if (_manualBusy || _manualSelect || !select || (data & _targetMask) == 0)
            {
                _selectPending = false;
                _delay = 0;
            }
            else if (!_selectPending)
            {
                _selectPending = true;
                _delay = _settleTicks;
                TimedOut = false;
            }
            return;
        }
        if (!_transferPending) return;
        if (Request && acknowledge)
        {
            // The initiator holds its data until REQ drops; sample it at ACK assertion.
            _transferredData = SampleHostData();
            Request = false;
            _waitingAckLow = true;
            _timeout = 0;
        }
        else if (_waitingAckLow && !acknowledge)
        {
            _waitingAckLow = false;
            _transferPending = false;
            TransferCompleted = true;
        }
    }

    /// <summary>Drivers externes de la carte ; ils ne créent pas une connexion de commande.</summary>
    internal void SetManualOutputs(bool select, bool busy, bool dataEnabled, byte data, bool dmaControlsData = false)
    {
        _manualSelect = !ResetAsserted && select;
        _manualBusy = !ResetAsserted && busy;
        _manualDataEnabled = !ResetAsserted && dataEnabled;
        _manualData = data;
        _dmaControlsData = !ResetAsserted && dmaControlsData;
        if (!_protocolBusy && (_manualSelect || _manualBusy))
        {
            _selectPending = false;
            _delay = 0;
        }
    }

    /// <summary>Octets logiques assertés sur les lignes collecteur ouvert du bus huit bits.</summary>
    internal byte SampleHostData() => (byte)((InputOutput ? 0 : _initiatorData)
        | (ProtocolDrivesData ? _protocolData : 0)
        | (_manualDataEnabled ? _manualData : 0));

    internal byte ResolveInitiatorData(byte data) => (byte)(data | (_manualDataEnabled ? _manualData : 0));

    public bool TryBeginTransfer(Acb4000BusPhase phase, byte data = 0)
    {
        if (phase is not (Acb4000BusPhase.Command or Acb4000BusPhase.DataOut or Acb4000BusPhase.DataIn
            or Acb4000BusPhase.Status or Acb4000BusPhase.MessageIn or Acb4000BusPhase.MessageOut))
            throw new ArgumentOutOfRangeException(nameof(phase));
        if (!_protocolBusy || ResetAsserted || Select || _acknowledge || _transferPending || TransferCompleted) return false;
        _delay = Phase == phase ? _deskewTicks : _settleTicks;
        Phase = phase;
        _protocolData = data;
        _transferPending = true;
        return true;
    }

    public bool TryTakeTransferredByte(out byte data)
    {
        data = _transferredData;
        if (!TransferCompleted) return false;
        TransferCompleted = false;
        return true;
    }

    public bool TryReleaseBus()
    {
        if (_transferPending || TransferCompleted || _acknowledge || _select) return false;
        Release();
        return true;
    }

    public void Tick()
    {
        if (_selectPending)
        {
            if (--_delay != 0) return;
            _selectPending = false;
            _protocolBusy = true;
            Phase = Acb4000BusPhase.Selected;
            return;
        }
        if (!_transferPending || _waitingAckLow) return;
        if (Request)
        {
            if (--_timeout != 0) return;
            Release();
            TimedOut = true;
            return;
        }
        if (_delay > 0) _delay--;
        // An early/held ACK is not the response to a request which has not yet been issued.
        if (_delay != 0 || _acknowledge) return;
        Request = true;
        _timeout = _timeoutTicks;
    }

    private void Release()
    {
        _protocolBusy = Request = _selectPending = _transferPending = _waitingAckLow = TransferCompleted = false;
        Phase = Acb4000BusPhase.BusFree;
        _delay = _timeout = 0;
    }

    private static int ToTicks(int nanoseconds, int ticksPerSecond) => checked((int)
        (((long)nanoseconds * ticksPerSecond + C.NanosecondsPerSecond - 1) / C.NanosecondsPerSecond));
}
