using C = RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Constants.Upd765Constants;

namespace RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Interfaces;
/// <summary>Contrat matériel du composant Upd765DriveControl ; aucune ressource hôte n'est possédée par ce contrat.</summary>
public interface IUpd765DriveControl
{
    Action<bool>? InterruptChanged { get; set; }

    Action? OutputsChanged { get; set; }
    bool SignalsBusy { get; }
    int SelectedUnit { get; }
    int SelectedHead { get; }
    bool SeekMode { get; }
    bool Step { get; }
    bool Direction { get; }
    long ControllerClocks { get; }
    long NextSignalTicks { get; }
    bool Irq { get; }

    byte BusyMask { get; }

    bool NonDma { get; }

    long HeadLoadTicks { get; }

    long HeadUnloadTicks { get; }

    bool PollingEnabled { get; set; }

    byte PresentCylinder(int unit);
    long NextEventTicks { get; }

    void Reset();
    void ReleaseReset();
    void Specify(byte rates, byte load);
    void SenseDriveStatus(byte unitHead);
    Action<byte>? DriveStatusCompleted { get; set; }
    bool Seek(byte unitHead, byte cylinder);
    bool Recalibrate(byte unit);
    bool TrySenseInterrupt(out byte status, out byte cylinder);
    void Advance(int ticks);
}
