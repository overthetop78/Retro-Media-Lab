using RetroMediaLab.Chips.CPU.ARM.ARMv2.ARM2.Enums;
using C = RetroMediaLab.Chips.CPU.ARM.ARMv2.ARM2.Constants.Arm26Constants;
using RetroMediaLab.Chips.CPU.ARM.ARMv2.ARM2.Interfaces;
using System.Numerics;

namespace RetroMediaLab.Chips.CPU.ARM.ARMv2.ARM2.Interfaces;
/// <summary>Contrat matériel du composant CpuArm26 ; aucune ressource hôte n'est possédée par ce contrat.</summary>
public interface ICpuArm26
{
    uint PC { get; }

    uint Status { get; }

    Arm26Mode Mode { get; }

    uint InstructionAddress { get; }

    long Cycles { get; }

    long Instructions { get; }

    void Reset();
    void Step();
    uint GetRegister(int register);
}
