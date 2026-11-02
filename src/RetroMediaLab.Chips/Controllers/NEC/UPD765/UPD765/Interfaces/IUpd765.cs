using RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Enums;
using C = RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Constants.Upd765Constants;
using System.IO;

namespace RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Interfaces;
/// <summary>Contrat matériel du composant Upd765 ; aucune ressource hôte n'est possédée par ce contrat.</summary>
public interface IUpd765
{
    Action<bool>? InterruptChanged { get; set; }

    Action<bool>? DataRequestChanged { get; set; }

    bool Irq { get; }

    bool Drq { get; }

    int Unit { get; }

    long NextEventTicks { get; }

    bool Active { get; }

    bool Writing { get; }

    bool Reading { get; }

    long ReadBytes(int unit);
    long WrittenBytes(int unit);
    long ByteTicks { get; }

    Upd765DataRate DataRate { get; set; }

    void Reset();
    byte ReadMainStatus();
    byte ReadFifo();
    void WriteFifo(byte value);
    byte ReadDma();
    void WriteDma(byte value);
    void SetTerminalCount(bool high);
    void Advance(int ticks);
}
