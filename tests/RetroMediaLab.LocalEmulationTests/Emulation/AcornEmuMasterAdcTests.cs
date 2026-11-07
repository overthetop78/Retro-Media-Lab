using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services;
using Io = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Constants.BbcMicroIoConstants;
using Master = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMaster.Common.Constants.MasterConstants;

namespace RetroMediaLab.Tests.Emulation;

public sealed class AcornEmuMasterAdcTests
{
    [Theory]
    [InlineData(false, 0, 0)]
    [InlineData(false, 37, 0)]
    [InlineData(false, 25001, 4)]
    [InlineData(true, 0, 0)]
    [InlineData(true, 37, 4)]
    [InlineData(true, 25001, 0)]
    public void ConversionStartsAtTheCurrentBusTimeAfterIdle(bool longMode, int idle, int mirror) => Run(board =>
    {
        var address = (ushort)(Master.AdcStart + mirror);
        var duration = longMode ? Io.TwelveBitCycles : Io.EightBitCycles;
        for (var tick = 0; tick < idle; tick++) board.Tick();
        board.Write(0xfe4e, 0x90);
        board.Read(0xfe40);
        board.Write(address, longMode ? Io.TwelveBitMode : (byte)0);
        Assert.Equal(Io.Busy, board.Read(address) & Io.Busy);
        for (var tick = 0; tick < duration - 1; tick++) board.Tick();
        Assert.Equal(Io.Busy, board.Read(address) & Io.Busy);
        Assert.False(board.Irq);
        board.Tick();
        Assert.Equal(Io.Complete, board.Read(address) & (Io.Busy | Io.Complete));
        Assert.True(board.Irq);
        board.Read(0xfe40);
        Assert.False(board.Irq);
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RestartDuringConversionReceivesItsOwnFullDuration(bool longMode) => Run(board =>
    {
        var duration = longMode ? Io.TwelveBitCycles : Io.EightBitCycles;
        board.Write(Master.AdcStart, Io.TwelveBitMode);
        for (var tick = 0; tick < 2713; tick++) board.Tick();
        board.Write(Master.AdcStart, longMode ? Io.TwelveBitMode : (byte)0);
        for (var tick = 0; tick < duration - 1; tick++) board.Tick();
        Assert.Equal(Io.Busy, board.Read(Master.AdcStart) & Io.Busy);
        board.Tick();
        Assert.Equal(Io.Complete, board.Read(Master.AdcStart) & (Io.Busy | Io.Complete));
    });

    private static void Run(Action<BbcMicroMachine> test)
    {
        var board = new BbcMicroMachine(new byte[16384], new byte[16384], 32768, expandedIo: true, master: true);
        try { test(board); }
        finally
        {
            board.SetInput(EmulationInputSnapshot.Empty);
            board.NetworkFrameTransmitted = null;
            board.NetworkPoll = null;
            board.EjectTape();
        }
    }
}
