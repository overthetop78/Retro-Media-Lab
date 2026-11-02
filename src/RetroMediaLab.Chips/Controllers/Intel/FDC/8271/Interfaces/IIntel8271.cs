using Phase = RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Enums.Intel8271Phase;
using C = RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Constants.Intel8271Constants;
using System.IO;

namespace RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Interfaces;
/// <summary>Contrat matériel du composant Intel8271 ; aucune ressource hôte n'est possédée par ce contrat.</summary>
public interface IIntel8271
{
    long SeekSteps(int drive);
    long ReadBytes(int drive);
    long WrittenBytes(int drive);
    bool Nmi { get; }

    bool Motor { get; }

    int ActiveDrive { get; }

    bool Reading { get; }

    bool Writing { get; }

    bool Active { get; }

    int MechanicalSpeedPercent { get; set; }

    void Reset();
    byte Read(int register);
    void Write(int register, byte value);
    void Tick();
}
