namespace RetroMediaLab.Chips.Controllers.Acorn.SystemKeyboard.Interfaces;

/// <summary>400,013 output path receiving already decoded, validated key codes.</summary>
public interface ISystemKeyboardOutput
{
    int ClockFrequency { get; }
    long ClockCount { get; }
    byte Data { get; }
    bool Strobe { get; }
    long? NextClockEvent { get; }
    Action<byte>? DataChanged { get; set; }
    Action<bool>? StrobeChanged { get; set; }
    Action<long?>? NextClockEventChanged { get; set; }
    void SetInputs(byte? encodedData, bool anyKeyDown, bool repeat, bool newKey);
    void AdvanceTo(long clocks);
    void Reset();
}
