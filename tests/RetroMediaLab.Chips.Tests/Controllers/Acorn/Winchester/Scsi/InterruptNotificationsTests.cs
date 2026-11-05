using RetroMediaLab.Chips.Controllers.Acorn.Winchester.Scsi.Enums;
using RetroMediaLab.Chips.Controllers.Acorn.Winchester.Scsi.Services;
using C = RetroMediaLab.Chips.Controllers.Acorn.Winchester.Scsi.Constants.ScsiHostAdapterConstants;

namespace RetroMediaLab.Chips.Tests.Controllers.Acorn.Winchester.Scsi;

public sealed class InterruptNotificationsTests
{
    [Fact]
    public void RequestLatchesIrqUntilDisableOrResetWithoutDuplicateNotifications()
    {
        var host = new ScsiHostAdapter();
        var levels = new List<bool>();
        host.IrqChanged = level => { Assert.Equal(host.Irq, level); levels.Add(level); };
        host.Write(C.InterruptEnableRegister, C.InterruptEnableMask);
        host.SetTarget(ScsiBusSignals.Request, 0);
        host.SetTarget(ScsiBusSignals.Request, 0);
        host.SetTarget(ScsiBusSignals.None, 0);
        Assert.Equal(new[] { true }, levels);
        host.Write(C.InterruptEnableRegister, 0);
        Assert.Equal(new[] { true, false }, levels);
        host.SetTarget(ScsiBusSignals.Request, 0);
        host.Write(C.InterruptEnableRegister, C.InterruptEnableMask);
        host.SetReset(true);
        host.SetReset(false);
        Assert.Equal(new[] { true, false, true, false }, levels);
    }
}
