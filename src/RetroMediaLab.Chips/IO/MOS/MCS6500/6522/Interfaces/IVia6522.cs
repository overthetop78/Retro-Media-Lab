using RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Enums;
using RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Contracts;

namespace RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Interfaces;
/// <summary>Contrat matériel du composant Via6522 ; aucune ressource hôte n'est possédée par ce contrat.</summary>
public interface IVia6522
{
    Func<byte>? ReadPortA { get; set; }

    Func<byte>? ReadPortB { get; set; }

    Action<byte>? WritePortA { get; set; }

    Action<byte>? WritePortB { get; set; }

    Action<byte>? PortAPinsChanged { get; set; }

    Action<byte>? PortBPinsChanged { get; set; }

    byte OutputA { get; }

    bool Irq { get; }
    Action<bool>? IrqChanged { get; set; }

    void Reset();
    Via6522State CaptureState();
    void RestoreState(Via6522State state);
    void Tick();
    void Phi2LeadingEdge();
    void Phi2TrailingEdge();
    /// <summary>Reçoit le niveau PH2 réellement livré. Les périodes sans effet sont
    /// conservées localement jusqu'à la prochaine observation, entrée ou frontière.
    /// Les changements d'entrée doivent passer par les méthodes du composant ;
    /// un remplacement de fournisseur ou un passage aux fronts historiques exige
    /// SynchronizePhi2Input au préalable, sans changement du niveau physique.</summary>
    void SetPhi2Input(bool high);
    /// <summary>Résout uniquement les périodes PH2 déjà reçues, sans avancer le temps.
    /// Les accès et nouvelles entrées le font automatiquement ; utile avant une
    /// observation d'ensemble ou le remplacement d'un raccordement historique.</summary>
    void SynchronizePhi2Input();
    /// <summary>Représente un préfixe de périodes PH2 complètes sans effet extérieur.
    /// Retourne le nombre accepté, éventuellement zéro, avant la première frontière.
    /// Les entrées doivent rester stables ; le câblage livre toute observation ou nouvelle
    /// entrée avant le prochain appel et traite les fronts de frontière à leur vraie date.</summary>
    long TryAdvanceStableClocks(long clocks);
    /// <summary>Même préfixe ; false dans retryAfterBoundary demande de garder les
    /// fronts ordinaires pour le reste de l'intervalle d'entrées stables, sans retry
    /// à chaque période. Une nouvelle entrée/observation permet une nouvelle demande.</summary>
    long TryAdvanceStableClocks(long clocks, out bool retryAfterBoundary);
    byte Read(int register);
    void Write(int register, byte value);
    Action<bool>? Ca2PinsChanged { get; set; }

    Action<bool>? Cb2PinsChanged { get; set; }

    bool Ca2 { get; }

    bool Cb2 { get; }

    ViaControlOutputs ControlOutputs { get; }
    Action<ViaControlOutputs>? ControlOutputsChanged { get; set; }

    void SetPb6(bool level);
    void VSync();
    void SetCa1(bool level);
    void SetKeyboardLine(bool level);
    void SetCa2(bool level);
    void SetCb1(bool level);
    void SetCb2(bool level);
    Action<bool>? Cb1PinsChanged { get; set; }

    bool Cb1 { get; }
}
