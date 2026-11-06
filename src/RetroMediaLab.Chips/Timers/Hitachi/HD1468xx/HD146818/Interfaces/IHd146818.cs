using RetroMediaLab.Chips.Timers.Hitachi.HD1468xx.HD146818.Enums;

namespace RetroMediaLab.Chips.Timers.Hitachi.HD1468xx.HD146818.Interfaces;
/// <summary>Contrat matériel du composant Hd146818 ; aucune ressource hôte n'est possédée par ce contrat.</summary>
public interface IHd146818
{
    long ReferenceFrequency { get; }
    int ClockFrequency { get; }
    long ClockCount { get; }
    long ClockHalfCount { get; }
    Hd146818Variant Variant { get; }
    bool Irq { get; }
    bool SquareWave { get; }
    bool ClockOut { get; }
    bool OscillatorHigh { get; }
    bool CalendarDataAvailable { get; }
    Action<bool>? IrqChanged { get; set; }
    Action<bool>? SquareWaveChanged { get; set; }
    Action<bool>? ClockOutChanged { get; set; }
    Action<long?>? NextClockEventChanged { get; set; }
    long? NextClockEvent { get; }
    void AdvanceTo(long referenceClock);
    void AdvanceClocks(long clocks);
    void AdvanceHalfClocks(long halfClocks);
    void SetOscillator(bool high);
    void SetClockFrequencySelect(bool high);
    void Clock();
    void SetReset(bool asserted);
    void SetPowerSense(bool high);
    bool DrivingDataBus { get; }
    byte DataBus { get; }
    void SetBus(byte data, bool chipEnabled, bool addressStrobe, bool dataStrobe, bool readWriteHigh);
    void SetStandby(bool asserted);
    void SetMotorolaMode(bool high);
    byte ReadRegister(int address);
    void WriteRegister(int address, byte value);
    byte[] Export();
    void Import(ReadOnlySpan<byte> bytes);
}
