using Io = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Constants.BbcMicroIoConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services;

internal sealed partial class BbcMicroMachine
{
    private bool _viaTrailingEdgePending;

    public byte Read(ushort address)
    {
        try { return ReadBus(address); }
        finally { CompleteViaBusCycle(); }
    }

    public void Write(ushort address, byte value)
    {
        try { WriteBus(address, value); }
        finally { CompleteViaBusCycle(); }
    }

    private void ClockVia()
    {
        if ((_ticks & Io.ViaPhaseMask) == Io.ViaTrailingPhase)
            _viaTrailingEdgePending = true;
        else
        {
            _via.Phi2LeadingEdge();
            _userVia?.Phi2LeadingEdge();
        }
    }

    private void CompleteViaBusCycle()
    {
        if (!_viaTrailingEdgePending) return;
        _viaTrailingEdgePending = false;
        _via.Phi2TrailingEdge();
        _userVia?.Phi2TrailingEdge();
    }
}
