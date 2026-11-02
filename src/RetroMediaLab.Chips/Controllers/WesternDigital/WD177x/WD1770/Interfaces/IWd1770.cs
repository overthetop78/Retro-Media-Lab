using Phase = RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Enums.Wd1770Phase;
using C = RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Constants.Wd1770Constants;
using RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Enums;
using System.IO;

namespace RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Interfaces;
/// <summary>Contrat matériel du composant Wd1770 ; aucune ressource hôte n'est possédée par ce contrat.</summary>
public interface IWd1770
{
    long SeekSteps(int drive);
    long ReadBytes(int drive);
    long WrittenBytes(int drive);
    int Drive { get; set; }

    int Side { get; set; }

    bool Selected { get; set; }

    bool DoubleDensity { get; set; }

    int MechanicalSpeedPercent { get; set; }

    bool Irq { get; }

    bool Drq { get; }

    bool Nmi { get; }

    bool Motor { get; }

    bool Active { get; }

    bool Writing { get; }

    bool Reading { get; }

    void Reset();
    byte Read(int register);
    void Write(int register, byte value);
    void Tick();
    void Advance(int ticks);
}
