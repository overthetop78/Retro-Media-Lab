using RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Interfaces;

namespace RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip65C12.Interfaces;

public interface ICpu65C12 : ICpu6502
{
    bool DataBusEnabled { get; }
    void SetDataBusEnable(bool high);
    bool InputClockHigh { get; }
    Action<bool>? ClockOutputChanged { get; set; }
    void SetClockInput(bool high);
}
