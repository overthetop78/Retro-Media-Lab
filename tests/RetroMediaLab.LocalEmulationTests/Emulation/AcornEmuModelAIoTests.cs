using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Enums;
using RetroMediaLab.Emulation.Acorn.Common.Interfaces;
using RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Contracts;
using RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Enums;
using RetroMediaLab.Emulation.Acorn.Common.Services;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Factories;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Functions;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.ModelA.Constants.ModelAIoConstants;
using P = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Constants.UserPortPointerConstants;
using Io = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Constants.BbcMicroIoConstants;

namespace RetroMediaLab.Tests.Emulation;

public sealed class AcornEmuModelAIoTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void OptionalEquipmentControlsDescriptionsAndIndependentCircuits(bool userPort, bool analog)
    {
        var configuration = Configuration(userPort, analog);
        var factory = new AcornEmuMachineFactory();
        var engine = new Engine();
        Assert.Equal(userPort, PointerInputFunctions.HasConnector(configuration));
        Assert.Equal(analog ? 2 : 0, factory.GetControllerPortCount(configuration));
        var input = engine.DescribeInputSettings(configuration);
        Assert.Equal(analog ? 2 : 0, input.ControllerPorts.Count);
        Assert.Equal(userPort, input.Mouse is not null);
        var settings = engine.Describe(configuration.Model.ToString(), configuration);
        Assert.Equal(analog, settings.Visibility.Tabs[EmulationMachineTab.Controllers]);
        var group = factory.DescribeSettings(configuration).Single(item => item.Id == C.Group);
        Assert.Equal(2, group.Fields.Count);
        Assert.All(group.Fields, field => Assert.True(field.RequiresRestart));

        var board = new BbcMicroMachine(new byte[16384], new byte[16384],
            expandedIo: userPort, analogInterface: analog);
        try
        {
            board.Write(0xfe62, 255);
            board.Write(0xfe60, 0x5a);
            Assert.Equal(userPort ? 0x5a : Io.UnconnectedBus, board.Read(0xfe60));
            board.SetInput(EmulationInputSnapshot.Empty with { Controllers =
                [EmulationControllerState.Empty with { LeftX = short.MaxValue },
                 EmulationControllerState.Empty with { LeftX = short.MinValue }] });
            board.Write(0xfec0, 8);
            if (analog)
            {
                Assert.Equal(0x80, board.Read(0xfec0) & 0x80);
                for (var clock = 0; clock < 20000; clock++) board.Tick();
                Assert.Equal(0, board.Read(0xfec1));
                board.Write(0xfec0, 10);
                for (var clock = 0; clock < 20000; clock++) board.Tick();
                Assert.Equal(255, board.Read(0xfec1));
            }
            else Assert.Equal(Io.UnconnectedBus, board.Read(0xfec0));
        }
        finally { board.SetInput(EmulationInputSnapshot.Empty); board.EjectTape(); }
    }

    [Fact]
    public void ConfiguredAnalogPortsSurviveSerializationAndValidation()
    {
        var configuration = Configuration(false, true);
        configuration = configuration with { Input = configuration.Input with { Controllers =
            [new(0, PeripheralCategory.AnalogJoystick), new(1, PeripheralCategory.AnalogJoystick)] } };
        var json = System.Text.Json.JsonSerializer.Serialize(configuration);
        var restored = System.Text.Json.JsonSerializer.Deserialize<MachineConfiguration>(json)!;
        Assert.Equal(2, new Engine().DescribeInputSettings(restored).ControllerPorts.Count);
    }

    [Fact]
    public void RemovingEquipmentAlsoRemovesItsImpossibleConnections()
    {
        var engine = new Engine();
        var configuration = Configuration(true, true);
        configuration = (MachineConfiguration)engine.ApplySettings(configuration,
            new Dictionary<string, string?> { [P.ConnectedOption] = bool.TrueString });
        configuration = configuration with { Input = configuration.Input with { Controllers =
            [new(0, PeripheralCategory.AnalogJoystick), new(1, PeripheralCategory.AnalogJoystick)] } };
        var result = (MachineConfiguration)engine.ApplySettings(configuration, new Dictionary<string, string?>
            { [C.UserPort] = bool.FalseString, [C.AnalogPort] = bool.FalseString });
        Assert.Empty(result.Input.Controllers!);
        Assert.False(result.Input.CaptureMouse);
        Assert.False(PointerInputFunctions.Connected(result));
        Assert.Empty(engine.DescribeInputSettings(result).ControllerPorts);
    }

    [Fact]
    public void PointerNeedsTheUserViaAndOtherModelsDoNotAcceptModelAUpgrades()
    {
        var configuration = Configuration(false, false);
        configuration = configuration with { Options = new Dictionary<string, string>(configuration.Options) { [P.ConnectedOption] = bool.TrueString } };
        Assert.Throws<ArgumentException>(() => PointerInputFunctions.Validate(configuration));
        var modelB = new MachineConfiguration(MachineModel.BbcMicroModelB, core: Emulator.AcornEmu);
        modelB = modelB with { Options = new Dictionary<string, string>(modelB.Options) { [C.UserPort] = bool.TrueString } };
        Assert.Throws<ArgumentException>(() => ModelAIoFunctions.Validate(modelB));
        IEmulatorControllerAdapter adapter = new AcornEmuMachineFactory();
        Assert.Equal(2, adapter.GetControllerPortCount(modelB));
    }

    private static MachineConfiguration Configuration(bool userPort, bool analog) =>
        new(MachineModel.BbcMicroModelA, core: Emulator.AcornEmu, options: new Dictionary<string, string>
        { [C.UserPort] = userPort.ToString(), [C.AnalogPort] = analog.ToString() });
}
