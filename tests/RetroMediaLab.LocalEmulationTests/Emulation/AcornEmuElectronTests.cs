using RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Contracts;
using RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Enums;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Functions;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornElectron.Common.Services;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Enums;

namespace RetroMediaLab.Tests.Emulation;

public sealed class AcornEmuElectronTests
{
    [Fact]
    public void CassetteOnlyBoardDoesNotRequestUnsupportedDiskConfiguration()
    {
        RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Interfaces.IAcornMachine machine =
            new ElectronMachine(new byte[16384], new byte[16384]);
        try
        {
            Assert.Equal(0, machine.FloppyDriveCount);
            Assert.Equal(0, machine.HardDiskDriveCount);
            machine.RunFrame();
            Assert.NotEmpty(machine.DrainAudio());
        }
        finally { machine.EjectCassette(); }
    }

    [Fact]
    public void KeyboardCombinesOnlySelectedLinesAndBothModifierKeys()
    {
        var keyboard = new ElectronKeyboard();
        keyboard.SetInput(EmulationInputSnapshot.Empty with { Keys = new HashSet<EmulationKey> { EmulationKey.Q, EmulationKey.X, EmulationKey.RightShift } });
        Assert.Equal(0, keyboard.Read(0x3fff));
        Assert.Equal(2, keyboard.Read(0x3fff ^ (1 << 12)));
        Assert.Equal(8, keyboard.Read(0x3fff ^ (1 << 11)));
        Assert.Equal(10, keyboard.Read(0x3fff ^ (1 << 11) ^ (1 << 12)));
        Assert.Equal(8, keyboard.Read(0x1fff));
    }

    [Fact]
    public void PowerOnStatusIsReadOnceAndClockIrqRequiresItsMask()
    {
        var ula = new ElectronUla(new ElectronVideo(_ => 0), new AnalogCassette(2000000));
        Assert.Equal(0x82, ula.Read(0));
        Assert.Equal(0x80, ula.Read(0));
        for (var tick = 0; tick < 12800; tick++) ula.Tick();
        Assert.False(ula.Irq);
        ula.Write(0, 8);
        Assert.True(ula.Irq);
        Assert.Equal(0x89, ula.Read(0));
        ula.Write(5, 0x20);
        Assert.False(ula.Irq);
    }

    [Fact]
    public void TransmitterBecomesReadyAtStopRatherThanOnDataRegisterWrite()
    {
        var ula = new ElectronUla(new ElectronVideo(_ => 0), new AnalogCassette(2000000));
        ula.Write(7, 4);
        ula.Tick();
        Assert.NotEqual(0, ula.Read(0) & 0x20);
        ula.Write(4, 0x55);
        ula.Tick();
        Assert.Equal(0, ula.Read(0) & 0x20);
        for (var tick = 0; tick < 14998; tick++) ula.Tick();
        Assert.Equal(0, ula.Read(0) & 0x20);
        ula.Tick();
        Assert.NotEqual(0, ula.Read(0) & 0x20);
    }

    [Fact]
    public void InternalRomPagesMustBeEscapedViaAnExternalHighPage()
    {
        var ula = new ElectronUla(new ElectronVideo(_ => 0), new AnalogCassette(2000000));
        ula.Write(5, 10);
        ula.Write(5, 1);
        Assert.Equal(10, ula.RomPage);
        ula.Write(5, 12);
        ula.Write(5, 1);
        Assert.Equal(1, ula.RomPage);
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    [InlineData(4)] [InlineData(5)] [InlineData(6)] [InlineData(7)]
    public void VideoCompletesAFrameAndLeavesSpacedRastersBlack(int mode)
    {
        var video = new ElectronVideo(_ => 255);
        video.SetMode((byte)(mode << 3));
        for (var register = 0; register < 8; register++) video.WritePalette(register, 0);
        for (var tick = 0; tick < 39936; tick++) video.Tick();
        Assert.Equal(1, video.Frame);
        Assert.Equal(255, video.CompletedFrame[0]);
        var spaced = mode is 3 or 6 or 7;
        Assert.Equal(spaced ? 0 : 255, video.CompletedFrame[8 * 640 * 4]);
    }

    [Fact]
    public void MotorStopsTapePositionWithoutBlockingManualRewind()
    {
        var tape = new AnalogCassette(2000000);
        tape.Insert(new(new short[4800], 48000), false);
        try
        {
            tape.ExecuteCassette(EmulationCassetteCommand.Play);
            for (var tick = 0; tick < 20000; tick++) tape.Tick(false);
            Assert.Equal(0, tape.Position);
            for (var tick = 0; tick < 20000; tick++) tape.Tick();
            Assert.Equal(480, tape.Position);
            tape.ExecuteCassette(EmulationCassetteCommand.Rewind);
            for (var tick = 0; tick < 20000; tick++) tape.Tick(false);
            Assert.Equal(0, tape.Position);
        }
        finally { tape.Eject(); }
    }

    [Fact]
    public void ModelUsesItsOwnHardwareProfileAndOnlyItsInstalledStorage()
    {
        var configuration = new MachineConfiguration(MachineModel.AcornElectron);
        Assert.Equal(Emulator.AcornEmu, configuration.Core);
        Assert.Equal(MachineFamily.AcornElectron, configuration.Family);
        Assert.Equal(32, ElectronFunctions.MemoryKib(configuration));
        Assert.Equal(EmulationMediaSlot.Cassette0, Assert.Single(ElectronFunctions.Devices(configuration)).Slot);
        Assert.Equal(2, ElectronFunctions.Slots(configuration).Count);
        Assert.Equal(3, ElectronFunctions.Describe(configuration).Count);
    }
}
