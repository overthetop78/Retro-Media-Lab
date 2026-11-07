using RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Contracts;

namespace RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Interfaces;

/// <summary>Broches et registres du WD1770/1772 ; le composant ne possède aucun lecteur ni média.</summary>
public interface IWd1770
{
    Wd1770State CaptureState();
    void RestoreState(Wd1770State state);
    int ClockFrequency { get; }
    ulong ClockCount { get; }
    void AdvanceTo(ulong clockCount);
    ulong HalfClocks { get; }
    long StepPulses { get; }
    long DataBytesRead { get; }
    long DataBytesWritten { get; }
    bool DoubleDensity { get; set; }
    bool Irq { get; }
    bool Drq { get; }
    bool Nmi { get; }
    Action<bool>? NmiChanged { get; set; }
    bool Motor { get; }
    bool Step { get; }
    bool Direction { get; }
    bool WriteGate { get; }
    bool WriteData { get; }
    bool Active { get; }
    bool Writing { get; }
    bool Reading { get; }
    byte? BusData { get; }
    event Action<ulong, bool>? WriteDataChanged;
    /// <summary>MO, STEP, DIRC, WG ou activité de commande modifiés ; les niveaux sont lisibles après l'appel courant.</summary>
    event Action? DriveSignalsChanged;
    void SetBus(bool selected, bool read, int register, byte value = 0);
    void SetReadData(bool active);
    void SetTrackZero(bool active);
    void SetWriteProtected(bool active);
    void SetIndex(bool active);
    void SetReset(bool active);
    void Reset();
    byte Read(int register);
    void Write(int register, byte value);
    void Tick();
    void Advance(int clocks);
    /// <summary>Consomme les clocks à entrées stables précédant une échéance interne ; zéro si le prochain Tick doit la traiter.</summary>
    int AdvanceWithStableInputs(int clocks);
}
