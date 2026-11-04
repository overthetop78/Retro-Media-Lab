using C = RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Constants.ElectronUlaConstants;

namespace RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Services;

public sealed partial class ElectronUla
{
    private bool _nmi;
    private bool _ramPriority;
    private bool _cpuSlowAccess;
    private bool _cpuRamAccess;
    private int _cpuPhase;
    private int _cpuDelay;
    private bool _cpuFirstPeriod;

    /// <summary>Entrée NMI active ; sa mémorisation pour la RAM est distincte de son niveau CPU.</summary>
    public bool Nmi
    {
        get => _nmi;
        set
        {
            _nmi = value;
            if (value) _ramPriority = true;
        }
    }

    public bool CpuRamPriority => _ramPriority;
    public bool CpuClockHigh => _video.CpuClockHigh;
    public Action<bool, int>? CpuClockChanged
    {
        get => _video.CpuClockChanged;
        set => _video.CpuClockChanged = value;
    }
    public byte CpuRamData => _video.CpuRamData;
    public bool CpuWaiting => _cpuSlowAccess && (_cpuDelay != 0 || _cpuPhase != 0
        || _cpuRamAccess && _video.CpuRamPending);

    public void BeginCpuCycle(ushort address, bool write, byte data)
    {
        _cpuFirstPeriod = true;
        SelectCpuAccess(address);
        _cpuDelay = !_cpuSlowAccess ? 0 : _cpuPhase == 0 ? C.AlignedSlowWait : C.MisalignedSlowCycle;
        if (_cpuRamAccess) _video.BeginCpuRam(address, write, data);
    }

    public void EndCpuCycle()
    {
        _cpuSlowAccess = _cpuRamAccess = _cpuFirstPeriod = false;
        _cpuDelay = 0;
        _video.CancelCpuRam();
    }

    private void SelectCpuAccess(ushort address)
    {
        _cpuRamAccess = address < C.RamEndAddress;
        var keyboard = address >= C.RamEndAddress && address < C.PagedRomEndAddress
            && (RomPage & C.KeyboardPageMask) == C.KeyboardPage;
        var externalIo = address >= C.ExternalIoStartAddress && address < C.ExternalIoEndAddress;
        _cpuSlowAccess = _cpuRamAccess || keyboard || externalIo;
    }

    private void TickBus()
    {
        var held = CpuWaiting && !_cpuFirstPeriod;
        _cpuFirstPeriod = false;
        _cpuPhase = (_cpuPhase + 1) & C.SlowClockPhaseMask;
        if (_cpuDelay != 0) _cpuDelay--;
        // CPU half of the 1 MHz RAM cycle (dot 0 modulo 16), independently of completion delay.
        _video.ConfigureCpuRam(_cpuPhase != 0, CpuRamPriority, held);
    }

    private void ClearRamPriority() => _ramPriority = _nmi;

    private void ResetBus(bool cold)
    {
        EndCpuCycle();
        _video.ConfigureCpuRam(false, false, false);
        if (!cold) return;
        _cpuPhase = 0;
        ClearRamPriority();
    }
}
