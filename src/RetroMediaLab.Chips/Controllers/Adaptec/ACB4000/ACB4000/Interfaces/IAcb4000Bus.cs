using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Enums;

namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Interfaces;

/// <summary>Broches logiques du port cible, sans carte hôte, mémoire de média ou ressource possédée.</summary>
public interface IAcb4000Bus
{
    Acb4000BusPhase Phase { get; }
    bool Busy { get; }
    bool Select { get; }
    bool Request { get; }
    bool InputOutput { get; }
    bool ControlData { get; }
    bool Message { get; }
    bool Attention { get; }
    bool ResetAsserted { get; }
    bool TimedOut { get; }
    byte OutputData { get; }
    bool TransferCompleted { get; }
    void SetInitiator(byte data, bool select, bool acknowledge, bool reset, bool attention = false);
    bool TryBeginTransfer(Acb4000BusPhase phase, byte data = 0);
    bool TryTakeTransferredByte(out byte data);
    bool TryReleaseBus();
    void Tick();
}
