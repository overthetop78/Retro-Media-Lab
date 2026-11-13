using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;
using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Enums;

namespace RetroMediaLab.Chips.EmuChip.Video.TexasInstruments.TMS99xx.TMS9918A.Services;

/// <summary>Chemin CPU vers l'arbitre VRAM : TI MP010A §2.1.3/2.1.5 et figure 2-1.
/// Aucun accès à une mémoire externe ni délai arbitraire ne se produit ici.
/// L'arbitre du VDP appelle CompleteRead/CompleteWrite au service physique réel.</summary>
internal sealed class Tms9918CpuMemoryPort
{
    private ushort? _address;
    private byte _data;
    private bool _dataKnown;
    private bool _requestKnown;
    private Tms9918CpuVramOperation _pending;

    internal Tms9918CpuMemoryPort(Tms9918CpuMemoryState initial)
    {
        if (initial.Address > 0x3fff || !Enum.IsDefined(initial.PendingOperation))
            throw new ArgumentException("Invalid TMS9918 CPU memory state.");
        _address = initial.Address; _data = initial.Data; _pending = initial.PendingOperation;
        _dataKnown = initial.DataKnown;
        _requestKnown = initial.RequestKnown;
    }

    internal bool HasPendingRequest => _pending != Tms9918CpuVramOperation.None;
    internal byte Data => _data;
    internal bool DataKnown => _dataKnown;
    internal Tms9918CpuMemoryState Capture() => new(_address, _data, _pending, _dataKnown, _requestKnown);
    internal Tms9918CpuVramRequest CurrentRequest => HasPendingRequest
        ? new(_address, _data, _pending, _requestKnown) : throw new InvalidOperationException("No CPU VRAM request.");

    internal void SetAddress(ushort address, bool prefetch)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(address, (ushort)0x3fff);
        _requestKnown = !HasPendingRequest;
        _address = address;
        if (prefetch) _pending = Tms9918CpuVramOperation.Read;
    }

    // Relevé matériel CanonV20 : le premier octet atteint immédiatement
    // le compteur, même si une lecture status abandonne ensuite le couple.
    internal void AddressLowAccepted(byte value)
    {
        if (HasPendingRequest) _requestKnown = false;
        if (_address is { } address) _address = (ushort)((address & 0x3f00) | value);
    }

    internal void AddressHighAccepted(ushort commandAddress, bool prefetch)
    {
        // Le haut ne réécrit pas un bas déjà modifié par le compteur.
        // Après une adresse non qualifiée, le premier octet de la commande
        // complète fournit la partie basse pour la séquence normale.
        ushort low = (ushort)((_address ?? commandAddress) & 0xff);
        SetAddress((ushort)((commandAddress & 0x3f00) | low), prefetch);
    }

    internal void RegisterWriteAccepted()
    {
        if (HasPendingRequest) _requestKnown = false;
        _address = null; // §2.1.2 : adresse détruite, pas une adresse zéro inventée.
    }

    internal void DataWriteAccepted(byte value)
    {
        // Une captureCPU n'est pas annulée parce que la demande précédente
        // attend encore. Le registre et la requête restent uniques ; le cas
        // de conflit est nominal, explicitement non caractérisé sur silicium.
        _requestKnown = !HasPendingRequest;
        _data = value;
        _dataKnown = true;
        _pending = Tms9918CpuVramOperation.Write;
    }

    internal byte DataReadAccepted()
    {
        _requestKnown = !HasPendingRequest;
        _pending = Tms9918CpuVramOperation.Read;
        return _data; // Donnée préchargée, pas celle d'une lecture immédiate de RAM.
    }

    internal void CompleteRead(byte value, bool known = true)
    {
        if (!HasPendingRequest)
            throw new InvalidOperationException("No CPU VRAM read to complete.");
        _data = value;
        _dataKnown = known && _requestKnown && _pending == Tms9918CpuVramOperation.Read;
        Complete();
    }

    internal void CompleteWrite()
    {
        if (!HasPendingRequest)
            throw new InvalidOperationException("No CPU VRAM write to complete.");
        _dataKnown &= _requestKnown && _pending == Tms9918CpuVramOperation.Write;
        Complete();
    }

    // Le brevet TI US4243984A décrit le même registre de données dans les deux
    // directions et l'incrément après service. Ce n'est pas un incrément à la demande.
    private void Complete()
    {
        if (!_requestKnown) _address = null;
        else if (_address is { } address) _address = (ushort)((address + 1) & 0x3fff);
        _pending = Tms9918CpuVramOperation.None;
        _requestKnown = true;
    }
}
