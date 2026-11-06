using RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Contracts;

namespace RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Interfaces;
/// <summary>Contrat matériel du composant Intel8271 ; aucune ressource hôte n'est possédée par ce contrat.</summary>
public interface IIntel8271
{
    Intel8271State CaptureState();
    void RestoreState(Intel8271State state);
    long SeekSteps(int drive);
    long ReadBytes(int drive);
    long WrittenBytes(int drive);
    bool Nmi { get; }
    int InputClockFrequency { get; }
    int ControllerClockFrequency { get; }
    long InputClockCount { get; }
    long? NextClockEvent { get; }
    event Action<long?>? NextClockEventChanged;
    void AdvanceTo(long inputClockCount);
    Action<bool>? NmiChanged { get; set; }
    bool Drq { get; }
    bool InSync { get; }
    bool WriteData { get; }
    event Action<bool>? WriteDataChanged;
    event Action? DriveSignalsChanged;
    void SetDataWindow(bool clockWindow);
    void SetPhaseLockedSeparator(bool enabled);
    void SetUnseparatedData(bool asserted);
    bool ResetAsserted { get; }
    byte DriveControl { get; }
    void SetReset(bool asserted);
    void SetReady(int drive, bool? ready);
    void SetWriteProtected(int drive, bool? writeProtected);
    void SetWriteFault(int drive, bool fault);
    void SetTrackZero(int drive, bool? atTrackZero);
    void SetIndex(int drive, bool? asserted);
    void SetCount(bool asserted);
    byte? ReadBus(bool selected, bool dmaAcknowledge, int register);
    void WriteBus(bool selected, bool dmaAcknowledge, int register, byte value);
    byte? BusData { get; }
    void SetBus(bool selected, bool read, bool write, bool dmaAcknowledge, int register, byte value = 0);

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
