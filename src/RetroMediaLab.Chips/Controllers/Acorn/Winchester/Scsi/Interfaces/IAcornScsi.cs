using Phase = RetroMediaLab.Chips.Controllers.Acorn.Winchester.Scsi.Enums.AcornScsiPhase;
using System.Buffers.Binary;
using System.Text;
using C = RetroMediaLab.Chips.Controllers.Acorn.Winchester.Scsi.Constants.AcornScsiConstants;
using System.IO;

namespace RetroMediaLab.Chips.Controllers.Acorn.Winchester.Scsi.Interfaces;
/// <summary>Contrat matériel du composant AcornScsi ; aucune ressource hôte n'est possédée par ce contrat.</summary>
public interface IAcornScsi
{
    long SeekCount(int unit);
    long ReadBlocks(int unit);
    long WrittenBlocks(int unit);
    bool Irq { get; }

    bool Busy { get; }

    int Unit { get; }

    bool Writing { get; }

    bool Active(int unit);
    void Configure(int unit, int latencyMilliseconds, int speedPercent, int? writeLatencyMilliseconds = null);
    void Reset();
    byte Read(int register);
    void Write(int register, byte value);
    void Tick();
}
