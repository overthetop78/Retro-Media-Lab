using RetroMediaLab.Chips.Controllers.Acorn.Winchester.Scsi.Services;
using RetroMediaLab.Chips.Controllers.Acorn.Winchester.Scsi.Enums;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;
using H = RetroMediaLab.Chips.Controllers.Acorn.Winchester.Scsi.Constants.ScsiHostAdapterConstants;
using System.Reflection;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services;
using RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Services;
using RetroMediaLab.Chips.IO.Motorola.M6800.Chip6850.Services;
using C = RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Constants.Via6522Constants;

namespace RetroMediaLab.Tests.Emulation;

public sealed class AcornEmuBbcInterruptWiringTests
{
    [Theory]
    [InlineData(false, 0, false)]
    [InlineData(true, 0, false)]
    [InlineData(true, 65536, false)]
    [InlineData(true, 131072, false)]
    [InlineData(true, 0, true)]
    public void SimultaneousSourcesRetainIrqUntilEveryCauseIsAcknowledged(bool expanded, int plusMemory, bool master)
    {
        var board = new BbcMicroMachine(new byte[16384], new byte[16384], memoryBytes: 32768,
            expandedIo: expanded, modelBPlusMemory: plusMemory, master: master, winchesterInterface: true);
        var via = Field<Via6522>(board, "_via");
        var acia = Field<Acia6850>(board, "_acia");
        var winchester = Field<Winchester>(board, "_winchester");
        var userVia = expanded ? Field<Via6522>(board, "_userVia") : null;
        Assert.False(board.Irq);
        via.Write(C.Enable, C.InterruptSummary | C.Ca1Interrupt);
        via.SetCa1(true);
        via.SetCa1(false);
        Assert.True(board.Irq);
        acia.Write(false, 0x94);
        acia.SetDcd(true);
        acia.ClockReceive(true);
        via.Read(C.PortA);
        Assert.True(board.Irq);
        if (userVia is not null)
        {
            userVia.Write(C.Enable, C.InterruptSummary | C.Ca1Interrupt);
            userVia.SetCa1(true);
            userVia.SetCa1(false);
        }
        acia.Read(false);
        acia.Read(true);
        Assert.Equal(expanded, board.Irq);
        userVia?.Read(C.PortA);
        Assert.False(board.Irq);
        via.SetCa1(true);
        via.SetCa1(false);
        board.Reset(true);
        Assert.False(board.Irq);
        Assert.False(winchester.Irq);
        var controller = Field<WinchesterController>(winchester, "_controller");
        var host = Field<ScsiHostAdapter>(controller, "host");
        host.Write(H.InterruptEnableRegister, H.InterruptEnableMask);
        host.SetTarget(ScsiBusSignals.Request, 0);
        Assert.True(board.Irq);
        host.SetTarget(ScsiBusSignals.None, 0);
        Assert.True(board.Irq);
        host.Write(H.InterruptEnableRegister, 0);
        Assert.False(board.Irq);
    }

    private static T Field<T>(object owner, string name) =>
        (T)owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner)!;
}
