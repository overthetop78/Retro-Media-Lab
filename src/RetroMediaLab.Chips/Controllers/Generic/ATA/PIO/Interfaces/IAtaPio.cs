using RetroMediaLab.Chips.Controllers.Generic.ATA.PIO.Enums;
using C = RetroMediaLab.Chips.Controllers.Generic.ATA.PIO.Constants.AtaConstants;
using S = RetroMediaLab.Chips.Controllers.Generic.ATA.PIO.Constants.AtaTimingConstants;
using System.Buffers.Binary;
using System.IO;

namespace RetroMediaLab.Chips.Controllers.Generic.ATA.PIO.Interfaces;
/// <summary>Contrat matériel du composant AtaPio ; aucune ressource hôte n'est possédée par ce contrat.</summary>
public interface IAtaPio
{
    Action<bool>? InterruptChanged { get; set; }

    bool Irq { get; }

    bool Busy { get; }

    bool Drq { get; }

    int Unit { get; }

    long ReadBlocks(int unit);
    long WrittenBlocks(int unit);
    long SeekCount(int unit);
    bool Active(int unit);
    bool Writing { get; }

    void Configure(int unit, int readLatency, int writeLatency, int speedPercent);
    void Reset();
    ushort ReadCommand(int register);
    void WriteCommand(int register, ushort value);
    byte ReadControl(int register);
    void WriteControl(int register, byte value);
    void Advance(int ticks);
}
