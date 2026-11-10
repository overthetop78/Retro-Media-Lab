namespace RetroMediaLab.Chips.Controllers.Acorn.SystemClock.Services;

public sealed partial class SystemClockGenerator
{
    // Pending computation of an already received source span, not another
    // physical counter or a queue of per-edge work. Every 163 retains its state.
    private long _deferredNetworkClocks;
    private bool _deferRepresentedResolution = true;

    internal bool DeferRepresentedResolution
    {
        get => _deferRepresentedResolution;
        set
        {
            if (_deferRepresentedResolution == value) return;
            ResolveDeferredClockNetwork();
            _deferRepresentedResolution = value;
        }
    }

    private bool ReadResolvedClock(byte pin)
    {
        ResolveDeferredClockNetwork();
        return (_levels & pin) != 0;
    }

    private void ResolveDeferredClockNetwork()
    {
        if (_deferredNetworkClocks == 0) return;
        ResolveRepresentedClockNetwork(_deferredNetworkClocks, _inputClockCount);
        _deferredNetworkClocks = 0;
    }
}
