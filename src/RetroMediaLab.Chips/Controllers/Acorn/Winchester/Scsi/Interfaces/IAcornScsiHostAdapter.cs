using RetroMediaLab.Chips.Controllers.Acorn.Winchester.Scsi.Enums;

namespace RetroMediaLab.Chips.Controllers.Acorn.Winchester.Scsi.Interfaces;

/// <summary>Câblage numérique des verrous de la carte hôte, indépendant de la cible et de son stockage.</summary>
public interface IAcornScsiHostAdapter
{
    byte OutputData { get; }
    bool DrivesData { get; }
    bool Select { get; }
    bool Acknowledge { get; }
    bool IrqEnabled { get; }
    bool Irq { get; }
    Action<bool>? IrqChanged { get; set; }
    event Action? OutputsChanged;
    bool ResetAsserted { get; }
    ScsiBusSignals TargetSignals { get; }
    void SetTarget(ScsiBusSignals signals, byte data);
    void SetReset(bool asserted);
    byte Read(int register);
    void Write(int register, byte value);
}
