namespace RetroMediaLab.Chips.Controllers.Acorn.SystemCpuClock.Interfaces;

/// <summary>IC5 WAIT/READY circuit on Acorn CPU card 400,005, after the clock-selection link.</summary>
public interface ISystemCpuClockControl
{
    long ClockFrequency { get; }
    long InputClockCount { get; }
    long CpuClockCount { get; }
    bool SelectedClockHigh { get; }
    bool CpuClockHigh { get; }
    bool Ready { get; }
    bool BusGrantHigh { get; }
    bool BusBuffersEnabled { get; }
    Action<bool>? CpuClockChanged { get; set; }
    /// <summary>Q1 physically drives both RDY and NBGt; its complement disables the buffers.</summary>
    Action<bool>? ReadyChanged { get; set; }
    void SetSelectedClock(bool high);
    void SetMemoryReady(bool high);
    void SetBusRequest(bool high);
    void SetCpuRead(bool read);
}
