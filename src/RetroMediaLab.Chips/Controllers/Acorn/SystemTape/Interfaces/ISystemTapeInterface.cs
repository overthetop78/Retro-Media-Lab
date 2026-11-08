namespace RetroMediaLab.Chips.Controllers.Acorn.SystemTape.Interfaces;

/// <summary>Digital pins and own clock domain of cassette board 200.001/100.001.</summary>
public interface ISystemTapeInterface
{
    int ClockFrequency { get; }
    long ClockCount { get; }
    long NextClockEvent { get; }
    bool TapeInput { get; }
    bool TapeOutput { get; }
    byte InputPins { get; }
    Action<bool>? TapeOutputChanged { get; set; }
    Action<byte>? InputPinsChanged { get; set; }
    Action<long>? NextClockEventChanged { get; set; }
    void Reset(long clock = 0);
    void SetOutputBit(bool value, long clock);
    void SetComparatorInput(bool high, long clock);
    void AdvanceTo(long clock);
}
