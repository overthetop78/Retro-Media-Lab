using C = RetroMediaLab.Chips.IO.Acorn.Tube.ULA.Constants.TubeConstants;

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

    void Reset(bool clearControl = true);
    byte HostRead(int register);
    byte ParasiteRead(int register);
    void HostWrite(int register, byte value);
    void ParasiteWrite(int register, byte value);
}
