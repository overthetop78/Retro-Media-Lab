namespace RetroMediaLab.Chips.Controllers.Acorn.SystemKeyboard.Interfaces;

/// <summary>AKD/REPEAT inputs and active-low NSTROBE of keyboard board 400,013.</summary>
public interface ISystemKeyboardRepeat
{
    int ClockFrequency { get; }
    long ClockCount { get; }
    bool Strobe { get; }
    long? NextClockEvent { get; }
    Action<bool>? StrobeChanged { get; set; }
    Action<long?>? NextClockEventChanged { get; set; }
    void SetInputs(bool anyKeyDown, bool repeat);
    void AdvanceTo(long clockCount);
    void Reset();
}
