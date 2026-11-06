using RetroMediaLab.Chips.CPU.Common.Interfaces;
using RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Enums;
using RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Services;
using RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip65C12.Interfaces;

namespace RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip65C12.Services;

/// <summary>GTE 65SC12 : entrée PHI2 et DBE, sans RDY ni oscillateur intégré.</summary>
public sealed class Cpu65C12(ICpuBus bus, long clockFrequency)
    : Cpu6502(bus, Cpu6502Variant.Gte65Sc12, clockFrequency), ICpu65C12
{
    public bool DataBusEnabled => DirectDataBusEnabled;
    public void SetDataBusEnable(bool high) => SetDirectDataBusEnable(high);
    public bool InputClockHigh => ExternalClockInputHigh;
    public Action<bool>? ClockOutputChanged { get => DirectClockOutputChanged; set => DirectClockOutputChanged = value; }
    public void SetClockInput(bool high) => SetCmosClockInput(high);
}
