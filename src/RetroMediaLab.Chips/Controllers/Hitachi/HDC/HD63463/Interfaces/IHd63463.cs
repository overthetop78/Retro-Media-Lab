using System.Buffers.Binary;
using C = RetroMediaLab.Chips.Controllers.Hitachi.HDC.HD63463.Constants.Hd63463Constants;
using Command = RetroMediaLab.Chips.Controllers.Hitachi.HDC.HD63463.Enums.Hd63463Command;
using Phase = RetroMediaLab.Chips.Controllers.Hitachi.HDC.HD63463.Enums.Hd63463Phase;
using E = RetroMediaLab.Chips.Controllers.Hitachi.HDC.HD63463.Constants.Hd63463ErrorCodeConstants;
using Codes = RetroMediaLab.Chips.Controllers.Hitachi.HDC.HD63463.Functions.Hd63463ErrorCodeFunctions;
using Stage = RetroMediaLab.Chips.Controllers.Hitachi.HDC.HD63463.Enums.Hd63463RecordStage;
using System.IO;

namespace RetroMediaLab.Chips.Controllers.Hitachi.HDC.HD63463.Interfaces;
/// <summary>Contrat matériel du composant Hd63463 ; aucune ressource hôte n'est possédée par ce contrat.</summary>
public interface IHd63463
{
    void DmaDone();
    void WriteCommand(byte value);
    Action<bool>? InterruptChanged { get; set; }

    Action<bool>? DataRequestChanged { get; set; }

    bool DriveOutputsEnabled { get; }

    bool Irq { get; }

    bool Drq { get; }

    bool Busy { get; }

    int Unit { get; }

    byte Status { get; }

    bool Writing { get; }

    bool Active(int unit);
    long SeekCount(int unit);
    long ReadBlocks(int unit);
    long WrittenBlocks(int unit);
    void MediaChanged(int unit);
    void Configure(int unit, int latencyMilliseconds, int speedPercent, int? writeLatencyMilliseconds = null, int rotationsPerMinute = C.DefaultRotationsPerMinute);
    void Reset();
    void WriteParameter(ushort value);
    ushort ReadParameter();
    ushort ReadDma(bool done = false);
    void WriteDma(ushort value, bool done = false);
    void Advance(int ticks);
}
