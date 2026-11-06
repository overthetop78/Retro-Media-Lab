using C = RetroMediaLab.Chips.IO.Acorn.Tube.ULA.Constants.TubeConstants;

namespace RetroMediaLab.Chips.IO.Acorn.Tube.ULA.Services;

public sealed partial class TubeUla
{
    private bool _hostPhase2High;
    private bool _hostTransferActive;
    private bool _hostReading;
    private int _hostRegister;
    private byte _hostWriteData;
    private byte _hostData;
    private bool _parasiteReadStrobeHigh = true;
    private bool _parasiteWriteStrobeHigh = true;
    private bool _parasiteSelected;
    private bool _parasiteDmaAcknowledged;
    private bool _parasiteReading;
    private bool _parasiteWriting;
    private bool _parasiteReadDma;
    private bool _parasiteWriteDma;
    private int _parasiteReadRegister;
    private int _parasiteWriteRegister;
    private byte _parasiteWriteData;
    private byte _parasiteData;

    public bool HostDataEnabled => _hostTransferActive && _hostReading && !_hostReset;
    public bool ParasiteDataEnabled => !_hostReset && (_parasiteSelected && !_parasiteReadStrobeHigh
        || _parasiteDmaAcknowledged && !_parasiteWriteStrobeHigh);
    public byte HostData => HostDataEnabled
        ? Read(true, _hostRegister, consume: false) : _hostData;
    public byte ParasiteData => _parasiteReading && ParasiteDataEnabled
        ? Read(false, _parasiteReadRegister, consume: false) : _parasiteData;

    /// <summary>HPHI2 valide le bus hôte ; la fin du cycle acquitte une seule transaction.</summary>
    public void SetHostBus(bool selected, bool phase2High, bool read, int register, byte data)
    {
        if (_hostPhase2High && !phase2High && _hostTransferActive)
        {
            _hostTransferActive = false;
            if (_hostReading) _hostData = HostRead(_hostRegister);
            else HostWrite(_hostRegister, _hostWriteData);
        }
        _hostPhase2High = phase2High;
        _hostTransferActive = phase2High && selected && !_hostReset;
        if (!_hostTransferActive) return;
        _hostReading = read;
        _hostRegister = register & C.RegisterMask;
        _hostWriteData = data;
    }

    /// <summary>PNRDS/PNWDS sont actifs bas. DACK sélectionne R3 avec le sens inversé du cycle mémoire.</summary>
    public void SetParasiteBus(bool selected, bool readStrobeHigh, bool writeStrobeHigh,
        bool dmaAcknowledged, int register, byte data)
    {
        if (_parasiteReading && (_parasiteReadDma ? writeStrobeHigh : readStrobeHigh))
        {
            _parasiteReading = false;
            _parasiteData = ParasiteRead(_parasiteReadRegister);
        }
        if (_parasiteWriting && (_parasiteWriteDma ? readStrobeHigh : writeStrobeHigh))
        {
            _parasiteWriting = false;
            ParasiteWrite(_parasiteWriteRegister, _parasiteWriteData);
        }
        if (!_hostReset && (selected || dmaAcknowledged))
        {
            if (!_parasiteReading && (dmaAcknowledged
                ? _parasiteWriteStrobeHigh && !writeStrobeHigh
                : _parasiteReadStrobeHigh && !readStrobeHigh))
            {
                _parasiteReading = true;
                _parasiteReadDma = dmaAcknowledged;
                _parasiteReadRegister = dmaAcknowledged ? C.DmaDataRegister : register & C.RegisterMask;
            }
            if (!_parasiteWriting && (dmaAcknowledged
                ? _parasiteReadStrobeHigh && !readStrobeHigh
                : _parasiteWriteStrobeHigh && !writeStrobeHigh))
            {
                _parasiteWriting = true;
                _parasiteWriteDma = dmaAcknowledged;
                _parasiteWriteRegister = dmaAcknowledged ? C.DmaDataRegister : register & C.RegisterMask;
            }
            if (_parasiteWriting) _parasiteWriteData = data;
        }
        _parasiteReadStrobeHigh = readStrobeHigh;
        _parasiteWriteStrobeHigh = writeStrobeHigh;
        _parasiteSelected = selected;
        _parasiteDmaAcknowledged = dmaAcknowledged;
    }

    private void CancelBusTransfers()
    {
        _hostTransferActive = false;
        _parasiteReading = _parasiteWriting = false;
    }
}
