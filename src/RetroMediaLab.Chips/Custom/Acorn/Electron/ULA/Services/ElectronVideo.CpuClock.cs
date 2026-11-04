using C = RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Constants.ElectronVideoConstants;
using T = RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Constants.ElectronVideoTimingConstants;

namespace RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Services;

public sealed partial class ElectronVideo
{
    private bool _cpuClockHeld;
    private bool _cpuClockDirty;
    internal bool CpuClockHigh { get; private set; } = true;
    internal Action<bool, int>? CpuClockChanged { get; set; }

    private void TickCpuClock(int dot)
    {
        // Clear before notification: a callback may change the hold input for the next point.
        _cpuClockDirty = false;
        var high = _cpuClockHeld || (dot & C.CpuPointMask) >= T.CpuClockLowPoints;
        if (high == CpuClockHigh) return;
        CpuClockHigh = high;
        CpuClockChanged?.Invoke(high, dot);
    }
}
