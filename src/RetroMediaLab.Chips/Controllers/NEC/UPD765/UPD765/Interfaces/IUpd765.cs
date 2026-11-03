using RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Enums;
using C = RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Constants.Upd765Constants;

namespace RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Interfaces;
/// <summary>Contrat matériel du composant Upd765 ; aucune ressource hôte n'est possédée par ce contrat.</summary>
public interface IUpd765
{
    Action<bool>? InterruptChanged { get; set; }

    Action<bool>? DataRequestChanged { get; set; }

    bool Irq { get; }

    bool Drq { get; }

    int Unit { get; }
    int Head { get; }
    bool Mfm { get; }
    bool Sync { get; }
    bool LowCurrent { get; }
    Action? DriveSignalsChanged { get; set; }
    bool SeekMode { get; }
    bool Step { get; }
    bool Direction { get; }
    bool FaultResetStep { get; }
    bool LowCurrentDirection { get; }
    bool FaultReset { get; }
    Action<bool>? FaultResetChanged { get; set; }
    bool HeadLoaded { get; }
    bool WriteEnable { get; }
    bool WriteData { get; }
    Upd765Precompensation Precompensation { get; }
    bool Preshift0 { get; }
    bool Preshift1 { get; }
    Action<ulong, Upd765Precompensation>? PrecompensationChanged { get; set; }
    Action<ulong, bool>? WriteDataChanged { get; set; }
    Action<ulong, bool>? WriteEnableChanged { get; set; }
    void SetIndex(bool high);
    void SetReadSignals(bool window, bool readData);
    void SetWriteClock(bool high);

    long NextEventTicks { get; }

    bool Active { get; }

    bool Writing { get; }

    bool Reading { get; }

    long ReadBytes(int unit);
    long WrittenBytes(int unit);
    long ByteTicks { get; }

    Upd765DataRate DataRate { get; set; }
    Upd765PowerState PowerState { get; }
    void WriteAuxiliaryCommand(byte value);

    byte? BusData { get; }

    void SetBus(bool selected, bool read, bool write, bool dataRegister,
        bool dmaAcknowledge = false, byte value = C.OpenBus);
    void SetReset(bool asserted);
    void Reset();
    byte ReadMainStatus();
    byte ReadFifo();
    void WriteFifo(byte value);
    byte ReadDma();
    void WriteDma(byte value);
    void SetTerminalCount(bool high);
    void Advance(int ticks);
}
