using RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Enums;
using RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Contracts;

namespace RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Interfaces;
/// <summary>Contrat matériel du composant Cpu6502 ; aucune ressource hôte n'est possédée par ce contrat.</summary>
public interface ICpu6502
{
    byte A { get; }

    byte X { get; }

    byte Y { get; }

    byte S { get; }

    byte P { get; }

    ushort PC { get; }

    ushort InstructionAddress { get; }

    long Cycles { get; }
    Cpu6502Variant Variant { get; }
    long ClockFrequency { get; }
    long InputClockFrequency { get; }
    long InputClocks { get; }
    int ClockDivider { get; }

    /// <summary>Entrée PH0 du NMOS ; les variantes CMOS possèdent leur propre contrat de broches.</summary>
    void SetPhaseZeroInput(bool high);
    bool PhaseOneHigh { get; }
    bool PhaseTwoHigh { get; }
    Action<bool, bool>? PhaseOutputsChanged { get; set; }

    void CompleteHostCall(byte result);
    void Reset();
    void Step();
    /// <summary>Raccorde le niveau IRQ conservé par le CPU, sans consultation ultérieure du bus.</summary>
    void SetIrq(bool asserted);
    /// <summary>Raccorde NMI ; les fronts reçus sont mémorisés puis échantillonnés à la phase CPU prévue.</summary>
    void SetNmi(bool asserted);
    void SetReset(bool asserted);
    /// <summary>RDY est absent du G65SC12 ; cette variante refuse le raccordement.</summary>
    void SetReady(bool high);
    void SetOverflowInput(bool asserted);
    /// <summary>Avance d'un cycle matériel, y compris quand RDY retient le séquenceur.</summary>
    void Clock();
    /// <summary>Avance l'oscillateur d'entrée ; son diviseur et sa phase restent dans la puce.</summary>
    void AdvanceInputClocks(long clocks);
    bool AtInstructionBoundary { get; }
    Cpu6502State CaptureState();
    void RestoreState(Cpu6502State state);
}
