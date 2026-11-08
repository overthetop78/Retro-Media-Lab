namespace RetroMediaLab.Chips.CPU.Motorola.M6800.Chip6809.Interfaces;
/// <summary>Contrat matériel du composant Cpu6809 ; aucune ressource hôte n'est possédée par ce contrat.</summary>
public interface ICpu6809
{
    byte A { get; }

    byte B { get; }

    byte DP { get; }

    byte CC { get; }

    ushort X { get; }

    ushort Y { get; }

    ushort U { get; }

    ushort S { get; }

    ushort PC { get; }

    long Cycles { get; }

    void SetIrq(bool active);
    void SetNmi(bool active);
    void SetFirq(bool active);

    void SetHalt(bool asserted);
    void SetReset(bool asserted);
    void SetDmaRequest(bool asserted);
    void SetMemoryReady(bool high);
    long InputClockCount { get; }
    long ProcessorCycles { get; }
    bool ClockE { get; }
    bool ClockQ { get; }
    bool BusAvailable { get; }
    bool BusStatus { get; }
    ushort? BusAddress { get; }
    bool? BusRead { get; }
    byte? BusOutputData { get; }
    Action<ushort?, bool?, byte?>? BusPinsChanged { get; set; }
    Action<bool, bool>? ClockOutputsChanged { get; set; }
    Action<bool, bool>? BusStatusChanged { get; set; }

    void Reset();
    void Step();
}
