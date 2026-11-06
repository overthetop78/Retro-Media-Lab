using RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Interfaces;

namespace RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip65C102.Interfaces;

public interface ICpu65C102 : ICpu6502
{
    bool BusEnabled { get; }
    bool MemoryLocked { get; }
    Action<bool>? BusEnableChanged { get; set; }
    Action<bool>? MemoryLockChanged { get; set; }
    void SetBusEnable(bool high);
    bool Phase2High { get; }
    bool Phase4High { get; }
    Action<bool, bool>? ClockOutputsChanged { get; set; }
    bool InputClockHigh { get; }
    void SetClockInput(bool high);
}
