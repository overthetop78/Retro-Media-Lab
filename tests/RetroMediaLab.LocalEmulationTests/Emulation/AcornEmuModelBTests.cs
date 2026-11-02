using System.Net.Http;
using RetroMediaLab.Emulation.Interfaces;
using RetroMediaLab.Emulation.Acorn.Modules;
using RetroMediaLab.Emulation.Acorn.Common.Services;
using RetroMediaLab.Emulation;
using RetroMediaLab.Emulation.Constants;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Enums;
using RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Contracts;
using RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Enums;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Factories;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services;
using Io = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Constants.BbcMicroIoConstants;
using ModelCatalog = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Dictionaries.ModelCatalog;
using Settings = RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Constants.SettingsConstants;

namespace RetroMediaLab.Tests.Emulation;

public sealed class AcornEmuModelBTests
{
    [Fact]
    public void ModelBIsRegisteredWithItsOwnFixedMainMemoryAndOptionalExpansions()
    {
        var configuration = new MachineConfiguration(MachineModel.BbcMicroModelB, core: Emulator.AcornEmu);
        var factory = new AcornEmuMachineFactory();
        Assert.Equal(MachineModel.BbcMicroModelB, ModelCatalog.Get(configuration.Model).Model);
        Assert.Equal("32", configuration.Options[Settings.MainMemory]);
        Assert.Equal(32m, factory.GetMemoryKib(configuration));
        var field = factory.DescribeSettings(configuration).SelectMany(block => block.Fields).Single(item => item.Id == Settings.MainMemory);
        Assert.False(field.IsEnabled);
        Assert.Equal("32", Assert.Single(field.Choices!).Id);
        Assert.Empty(factory.DescribeMediaDevices(configuration));
        Assert.DoesNotContain(factory.GetFirmwareSlots(configuration), slot => slot.IsRequired && slot.FieldId is ("acornemu.dfs" or "acornemu.nfs" or "acornemu.floppy.rom8271"));
        Assert.Contains(factory.GetFirmwareSlots(configuration), slot => slot.FieldId == "acornemu.characters");
        Assert.Throws<ArgumentException>(() => factory.ValidateOptions(configuration with
        {
            Options = new Dictionary<string, string>(configuration.Options) { [Settings.MainMemory] = "16" }
        }));
        using var core = new EmulatorCore(MachineModel.BbcMicroModelB);
        Assert.Equal(Emulator.AcornEmu, core.Emulator);
    }

    [Fact]
    public void ModelBHasIndependentUpperRamAndUserViaTimerInterrupts()
    {
        var board = Board();
        board.Write(0, 0x12); board.Write(0x4000, 0x34);
        Assert.Equal(0x12, board.Read(0)); Assert.Equal(0x34, board.Read(0x4000));
        board.Write(0xfe6e, 0xc0);
        board.Write(0xfe64, 2); board.Write(0xfe65, 0);
        for (var tick = 0; tick < 8; tick++) board.Tick();
        Assert.True(board.Irq);
        Assert.Equal(0xc0, board.Read(0xfe6d) & 0xc0);
        board.Tick(); board.Tick(); // Une horloge VIA après le cycle de déclenchement.
        board.Read(0xfe64);
        Assert.False(board.Irq);
        var modelA = new BbcMicroMachine(new byte[16384], new byte[16384]);
        modelA.Write(0xfe6e, 0xc0); modelA.Write(0xfe65, 0);
        for (var tick = 0; tick < 8; tick++) modelA.Tick();
        Assert.False(modelA.Irq);
        Assert.Equal(Io.UnconnectedBus, modelA.Read(0xfe6d));
        Assert.Equal(Io.UnconnectedBus, modelA.Read(Io.AdcStart));
    }

    [Fact]
    public void ModelBAnalogueConversionSamplesAtStartAndInterruptsOnCompletion()
    {
        var board = Board();
        var fire = 1u << EmulationSdk.InputProcessing.ControllerButtonIndex(EmulationControllerCommandIds.A);
        board.SetInput(EmulationInputSnapshot.Empty with { Controllers = [EmulationControllerState.Empty with { LeftX = short.MaxValue, LeftY = short.MinValue, Buttons = fire }] });
        Assert.Equal(0, board.Read(0xfe40) & Io.FirstFireLine);
        board.Write(0xfe4e, 0x90);
        board.Write(Io.AdcStart, 0);
        Assert.Equal(Io.Busy, board.Read(Io.AdcStart) & Io.Busy);
        board.SetInput(EmulationInputSnapshot.Empty);
        for (var tick = 0; tick < Io.EightBitCycles - 1; tick++) board.Tick();
        Assert.False(board.Irq);
        Assert.Equal(Io.Busy, board.Read(Io.AdcStart) & Io.Busy);
        board.Tick();
        Assert.True(board.Irq);
        Assert.Equal(Io.Complete, board.Read(Io.AdcStart) & (Io.Busy | Io.Complete));
        Assert.Equal(0, board.Read(Io.AdcStart + Io.HighRegister));
        Assert.Equal(0, board.Read(Io.AdcStart + Io.LowRegister));
        board.Read(0xfe40);
        Assert.False(board.Irq);
    }

    [Theory]
    [InlineData(0, 0xff)]
    [InlineData(1, 0x00)]
    [InlineData(2, 0x00)]
    [InlineData(3, 0xff)]
    public void FourAnalogueChannelsUseBothControllerPorts(int channel, int expectedHigh)
    {
        var board = Board();
        board.SetInput(EmulationInputSnapshot.Empty with { Controllers =
        [
            EmulationControllerState.Empty with { LeftX = short.MinValue, LeftY = short.MaxValue },
            EmulationControllerState.Empty with { LeftX = short.MaxValue, LeftY = short.MinValue }
        ] });
        board.Write(Io.AdcStart, (byte)(channel | Io.TwelveBitMode));
        for (var tick = 0; tick < Io.TwelveBitCycles; tick++) board.Tick();
        Assert.Equal(expectedHigh, board.Read(Io.AdcStart + Io.HighRegister));
        Assert.Equal(Io.Complete, board.Read(Io.AdcStart) & (Io.Busy | Io.Complete));
    }

    [Theory]
    [InlineData(Emulator.AcornEmu, "acornemu")]
    [InlineData(Emulator.B2, "b2")]
    public async Task PublicManagerKeepsAllCompatibleChoicesAndUsesTheSelectedCore(Emulator emulator, string expectedId)
    {
        using var http = new HttpClient();
        IEmulationEmulatorManager manager = new AcornEmulationModule("unused-config", "unused-data", http, "unused-core");
        var configuration = new MachineConfiguration(MachineModel.BbcMicroModelB, core: emulator);
        var choices = await manager.GetEmulatorInstallationsAsync(configuration);
        Assert.Contains(choices, choice => choice.EmulatorId == "acornemu");
        Assert.Contains(choices, choice => choice.EmulatorId == "b2");
        Assert.Equal(expectedId, (await manager.GetEmulatorInstallationAsync(configuration)).EmulatorId);
        var selected = await manager.UseEmulatorAsync(configuration, expectedId);
        Assert.Equal(emulator, ((MachineConfiguration)selected).Core);
        Assert.Equal(expectedId, (await manager.GetEmulatorInstallationAsync(selected)).EmulatorId);
        Assert.Equal(choices.Select(item => item.EmulatorId), (await manager.GetEmulatorInstallationsAsync(selected)).Select(item => item.EmulatorId));
    }

    [Fact]
    public async Task RuntimeUsesAcornEmuDespiteOtherCompatibleCores()
    {
        using var http = new HttpClient();
        var engine = new Engine("unused-config", "unused-data", http, "unused-core");
        var configuration = new MachineConfiguration(MachineModel.BbcMicroModelB, core: Emulator.AcornEmu);
        var services = new EmulationRuntimeServices("unused-sessions", "unused-states", "unused-converted", "unused-host", (_, _) => throw new InvalidOperationException("No physical audio may open"));
        var runtime = await engine.CreateRuntimeAsync(configuration, services);
        Assert.Equal(Emulator.AcornEmu, ((MachineConfiguration)runtime.Configuration).Core);
        Assert.Equal("acornemu", (await engine.GetEmulatorInstallationAsync(configuration)).EmulatorId);
        var releases = await engine.FindEmulatorReleasesAsync(configuration);
        Assert.Single(releases);
    }

    private static BbcMicroMachine Board() => new(new byte[16384], new byte[16384], 32768, expandedIo: true);
}
