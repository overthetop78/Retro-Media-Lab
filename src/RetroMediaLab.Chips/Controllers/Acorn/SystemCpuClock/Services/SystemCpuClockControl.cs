using RetroMediaLab.Chips.Controllers.Acorn.SystemCpuClock.Interfaces;
using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS74A.Services;
using C = RetroMediaLab.Chips.Controllers.Acorn.SystemCpuClock.Constants.SystemCpuClockConstants;

namespace RetroMediaLab.Chips.Controllers.Acorn.SystemCpuClock.Services;

/// <summary>Two positive-edge bistables and gates of Acorn 400,005 §2.4/2.5, figure 21.</summary>
/// <remarks>The initial Q values select a deterministic running phase, not a guaranteed
/// power-on state. PRE/CLR remain inactive as wired; CPU reset is not a reset of IC5.</remarks>
public sealed class SystemCpuClockControl : ISystemCpuClockControl
{
    private readonly Sn74Ls74 _ic5;
    private bool _waitHigh;
    private bool _busRequestHigh = true;
    private bool _cpuRead = true;
    private bool _memoryReady = true;

    public SystemCpuClockControl(long clockFrequency)
    {
        if (clockFrequency is not C.LinkTwoFrequency and not C.LinkThreeFrequency and not C.LinkOneFrequency)
            throw new ArgumentOutOfRangeException(nameof(clockFrequency));
        ClockFrequency = clockFrequency;
        _ic5 = new Sn74Ls74(clockFrequency, clockFrequency, true, true);
        _ic5.SetData(C.ReadyChannel, true);
        _ic5.SetData(C.WaitChannel, true);
        _ic5.SecondOutputsChanged = (_, notQ) =>
        {
            _waitHigh = notQ!.Value;
            ResolveCpuClock();
        };
        _ic5.FirstOutputsChanged = (q, _) =>
        {
            Ready = q!.Value;
            ReadyChanged?.Invoke(Ready);
        };
    }

    public long ClockFrequency { get; }
    public long InputClockCount => _ic5.GetInputClockCount(C.WaitChannel);
    public long CpuClockCount => _ic5.GetInputClockCount(C.ReadyChannel);
    public bool SelectedClockHigh { get; private set; }
    public bool CpuClockHigh { get; private set; }
    public bool Ready { get; private set; } = true;
    public bool BusGrantHigh => Ready;
    public bool BusBuffersEnabled => Ready;
    public Action<bool>? CpuClockChanged { get; set; }
    public Action<bool>? ReadyChanged { get; set; }

    public void SetMemoryReady(bool high)
    {
        _memoryReady = high;
        _ic5.SetData(C.WaitChannel, high);
    }

    /// <summary>The receiver can represent complete normal PH0 periods only
    /// while these received inputs and resolved bistables keep the path normal.</summary>
    public bool CanRepresentClockPeriods => _memoryReady && _busRequestHigh && Ready
        && !_waitHigh && !SelectedClockHigh && !CpuClockHigh;

    /// <summary>Account for selected periods whose PH0 effects the CPU receiver
    /// has already consumed. No clock callback is replayed; the two real
    /// bistables retain their counters and resolved stable states.</summary>
    public void AdvanceRepresentedClockPeriods(long periods)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(periods);
        if (periods == 0) return;
        if (!CanRepresentClockPeriods) throw new InvalidOperationException("WAIT/READY is not qualified for represented periods.");
        _ = checked(InputClockCount + periods);
        _ = checked(CpuClockCount + periods);
        _ic5.AdvanceClocks(C.WaitChannel, periods);
        _ic5.AdvanceClocks(C.ReadyChannel, periods);
    }

    public void SetBusRequest(bool high)
    {
        if (_busRequestHigh == high) return;
        _busRequestHigh = high;
        _ic5.SetData(C.ReadyChannel, high || !_cpuRead);
    }

    public void SetCpuRead(bool read)
    {
        if (_cpuRead == read) return;
        _cpuRead = read;
        _ic5.SetData(C.ReadyChannel, _busRequestHigh || !read);
    }

    public void SetSelectedClock(bool high)
    {
        if (SelectedClockHigh == high) return;
        SelectedClockHigh = high;
        _ic5.SetClock(C.WaitChannel, high);
        ResolveCpuClock();
    }

    private void ResolveCpuClock()
    {
        var high = SelectedClockHigh || _waitHigh;
        if (CpuClockHigh == high) return;
        CpuClockHigh = high;
        // READY captures on this very PH0 edge, before PH0 receivers observe it.
        _ic5.SetClock(C.ReadyChannel, high);
        CpuClockChanged?.Invoke(high);
    }
}
