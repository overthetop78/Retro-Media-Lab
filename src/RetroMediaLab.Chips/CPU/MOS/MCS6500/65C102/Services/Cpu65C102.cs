using RetroMediaLab.Chips.CPU.Common.Interfaces;
using RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Enums;
using RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Services;
using RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip65C102.Interfaces;

namespace RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip65C102.Services;

/// <summary>GTE 65SC102 : oscillateur divisé par quatre, RDY, BE et ML.</summary>
public sealed class Cpu65C102(ICpuBus bus, long clockFrequency)
    : Cpu6502(bus, Cpu6502Variant.Gte65Sc102, clockFrequency), ICpu65C102
{
    public bool BusEnabled => BusPinsEnabled;
    public bool MemoryLocked => MemoryLockAsserted;
    public Action<bool>? BusEnableChanged { get => BusPinsEnableChanged; set => BusPinsEnableChanged = value; }
    public Action<bool>? MemoryLockChanged { get => MemoryLockOutputChanged; set => MemoryLockOutputChanged = value; }
    public void SetBusEnable(bool high) => SetBusPinsEnable(high);
    public bool Phase2High => AdvancedPhase2High;
    public bool Phase4High => AdvancedPhase4High;
    public Action<bool, bool>? ClockOutputsChanged { get => AdvancedClockOutputsChanged; set => AdvancedClockOutputsChanged = value; }
    public bool InputClockHigh => ExternalClockInputHigh;
    public void SetClockInput(bool high) => SetCmosClockInput(high);
}
