namespace RetroMediaLab.Chips.IO.Acorn.Tube.ULA.Interfaces;
/// <summary>Contrat matériel du composant TubeUla ; aucune ressource hôte n'est possédée par ce contrat.</summary>
public interface ITubeUla
{
    Action? ResetParasite { get; set; }

    bool ResetHeld { get; }

    bool HostIrq { get; }

    bool ParasiteIrq { get; }

    bool ParasiteNmi { get; }
    Action<bool>? HostIrqChanged { get; set; }
    Action<bool>? ParasiteIrqChanged { get; set; }
    Action<bool>? ParasiteNmiChanged { get; set; }
    Action<bool>? ParasiteResetChanged { get; set; }
    Action<bool>? DmaRequestChanged { get; set; }
    bool DmaRequest { get; }

    void Reset(bool clearControl = true);
    void SetHostReset(bool asserted);
    void SetHostBus(bool selected, bool phase2High, bool read, int register, byte data);
    void SetParasiteBus(bool selected, bool readStrobeHigh, bool writeStrobeHigh, bool dmaAcknowledged, int register, byte data);
    byte HostData { get; }
    byte ParasiteData { get; }
    bool HostDataEnabled { get; }
    bool ParasiteDataEnabled { get; }
    byte HostRead(int register);
    byte ParasiteRead(int register);
    void HostWrite(int register, byte value);
    void ParasiteWrite(int register, byte value);
}
