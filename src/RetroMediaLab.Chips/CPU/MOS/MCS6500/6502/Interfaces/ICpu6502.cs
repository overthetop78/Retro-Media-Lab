using C = RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Constants.Cpu6502Constants;
using RetroMediaLab.Chips.CPU.Common.Interfaces;
using RetroMediaLab.Chips.CPU.Common.Enums;
using RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Enums;
using Phase = RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Enums.Cpu6502BusPhase;
using Mode = RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Enums.Cpu6502AddressMode;
using Op = RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Enums.Cpu6502Operation;

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

    void CompleteHostCall(byte result);
    void Reset();
    void Step();
    bool AtInstructionBoundary { get; }
}
