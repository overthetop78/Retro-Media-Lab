using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Enums;
using RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Contracts;
using RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Enums;
using RetroMediaLab.Emulation.Acorn.Common.Services;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Factories;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Functions;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Constants.UserPortPointerConstants;
using M = RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Constants.MouseInputConstants;
using Input = RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Functions.InputSnapshotFunctions;

namespace RetroMediaLab.Tests.Emulation;

public sealed class AcornEmuPointerInputTests
{
    [Theory]
    [InlineData(MachineModel.BbcMicroModelB)]
    [InlineData(MachineModel.BbcMicroModelBPlus)]
    [InlineData(MachineModel.BbcMaster128)]
    [InlineData(MachineModel.BbcMasterCompact)]
    public void MouseEditorProvidesPhysicalButtonDefaultsBeforeConnection(MachineModel model)
    {
        var settings = new Engine().DescribeInputSettings(new MachineConfiguration(model, core: Emulator.AcornEmu));
        var mouse = Assert.IsType<EmulationInputBindingSet>(settings.Mouse);
        Assert.Collection(mouse.Definitions,
            item => Assert.Equal((M.LeftAction, M.LeftResource, M.LeftSource), (item.Id, item.DisplayResourceKey, item.DefaultBinding)),
            item => Assert.Equal((M.RightAction, M.RightResource, M.RightSource), (item.Id, item.DisplayResourceKey, item.DefaultBinding)),
            item => Assert.Equal((M.MiddleAction, M.MiddleResource, M.MiddleSource), (item.Id, item.DisplayResourceKey, item.DefaultBinding)));
        Assert.Equal(M.LeftSource, mouse.Values[M.LeftAction]);
        Assert.Equal(M.RightSource, mouse.Values[M.RightAction]);
        Assert.Equal(M.MiddleSource, mouse.Values[M.MiddleAction]);
    }

    [Fact]
    public void MouseBindingsPersistAndUsePhysicalInputsWithoutChangingMotion()
    {
        var engine = new Engine();
        var configuration = new MachineConfiguration(MachineModel.BbcMaster128, core: Emulator.AcornEmu);
        var settings = engine.DescribeInputSettings(configuration);
        var bindings = new Dictionary<string, string>
        {
            [M.LeftAction] = M.RightSource,
            [M.RightAction] = "Keyboard:A",
            [M.MiddleAction] = string.Empty
        };
        configuration = (MachineConfiguration)engine.ApplyInputSettings(configuration,
            settings with { Mouse = settings.Mouse! with { Values = bindings } });
        var restored = System.Text.Json.JsonSerializer.Deserialize<InputConfiguration>(
            System.Text.Json.JsonSerializer.Serialize(configuration.Input))!;
        configuration = configuration with { Input = restored };
        var physical = new EmulationInputSnapshot(new HashSet<EmulationKey> { EmulationKey.A },
            new(7, -3, 4, true, false, true, true, false, -2), []);
        var mapped = Input.Apply(physical, configuration, new AcornEmuMachineFactory());
        Assert.Equal(physical.Pointer with { Left = false, Right = true, Middle = false }, mapped.Pointer);
        Assert.True(physical.Pointer.Left);
        Assert.True(physical.Pointer.Middle);
        Assert.Equal(string.Empty, engine.DescribeInputSettings(configuration).Mouse!.Values[M.MiddleAction]);
        configuration = (MachineConfiguration)engine.ApplyInputSettings(configuration, settings with { Mouse = null });
        Assert.Equal(bindings.OrderBy(item => item.Key), configuration.Input.MouseBindings!.OrderBy(item => item.Key));
    }

    [Fact]
    public void MouseBindingCanUseAnExistingControllerButton()
    {
        var configuration = new MachineConfiguration(MachineModel.BbcMicroModelB, core: Emulator.AcornEmu);
        configuration = configuration with { Input = configuration.Input with
        {
            MouseBindings = new Dictionary<string, string> { [M.LeftAction] = "Controller:A" }
        } };
        var fire = 1u << RetroMediaLab.Emulation.EmulationSdk.InputProcessing.ControllerButtonIndex(
            RetroMediaLab.Emulation.Constants.EmulationControllerCommandIds.A);
        var physical = new EmulationInputSnapshot(new HashSet<EmulationKey>(),
            new(0, 0, 0, false, true, false), [new(fire, 0, 0, 0, 0, 0, 0)]);
        var mapped = Input.Apply(physical, configuration, new AcornEmuMachineFactory());
        Assert.True(mapped.Pointer.Left);
        Assert.True(mapped.Pointer.Right);
        Assert.False(mapped.Pointer.Middle);
        Assert.False(Input.Apply(physical with { Controllers = [] }, configuration).Pointer.Left);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BothAnalogPortsReachEveryAdcChannelAndFireLine(bool master)
    {
        var board = new BbcMicroMachine(new byte[16384], new byte[16384], 32768, expandedIo: true, master: master);
        try
        {
            var fire = 1u << RetroMediaLab.Emulation.EmulationSdk.InputProcessing.ControllerButtonIndex(
                RetroMediaLab.Emulation.Constants.EmulationControllerCommandIds.A);
            board.SetInput(new(new HashSet<EmulationKey>(), EmulationInputSnapshot.Empty.Pointer,
            [new(fire, short.MaxValue, short.MinValue, 0, 0, 0, 0),
                new(0, short.MinValue, short.MaxValue, 0, 0, 0, 0)]));
            Assert.Equal(0x20, board.Read(0xfe40) & 0x30);
            var address = (ushort)(master ? 0xfe18 : 0xfec0);
            var expected = new byte[] { 0, 255, 255, 0 };
            for (var channel = 0; channel < expected.Length; channel++)
            {
                board.Write(address, (byte)(channel | 8));
                for (var cycle = 0; cycle < 20000; cycle++) board.Tick();
                Assert.Equal(expected[channel], board.Read((ushort)(address + 1)));
            }
        }
        finally { board.SetInput(EmulationInputSnapshot.Empty); board.Reset(true); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CompactConnectorUsesOnlyItsSelectedElectricalConnection(bool pointerConnected)
    {
        var board = new BbcMicroMachine(new byte[16384], new byte[16384], 32768,
            expandedIo: true, master: true, compact: true, userPortPointer: pointerConnected);
        try
        {
            var fire = 1u << RetroMediaLab.Emulation.EmulationSdk.InputProcessing.ControllerButtonIndex(
                RetroMediaLab.Emulation.Constants.EmulationControllerCommandIds.A);
            board.SetInput(new(new HashSet<EmulationKey>(), new(0, 0, 0, false, false, true),
                [new(fire, short.MaxValue, short.MinValue, 0, 0, 0, 0)]));
            Assert.Equal(pointerConnected ? 0x1d : 0x06, board.Read(0xfe60) & 0x1f);
            Assert.Equal(pointerConnected, board.SupportsPointerCapture);
        }
        finally { board.SetInput(EmulationInputSnapshot.Empty); board.Reset(true); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BothConnectorsKeepButtonPinsAndQuadratureDirections(bool compact)
    {
        UserPortPointer? pointer = null;
        var transitions = new List<(long Time, char Axis, bool Level, byte Port)>();
        pointer = new(compact, high => transitions.Add((pointer!.Time, 'X', high, pointer.PortB)),
            high => transitions.Add((pointer!.Time, 'Y', high, pointer.PortB)));
        var xMask = compact ? C.CompactX : C.UserX;
        var yMask = compact ? C.CompactY : C.UserY;
        var left = compact ? C.CompactLeft : C.UserLeft;
        var middle = compact ? C.CompactMiddle : C.UserMiddle;
        var right = compact ? C.CompactRight : C.UserRight;
        var allButtons = left | middle | right;
        pointer.SetInput(new(1, 1, 0, true, false, true), 0);
        Assert.Equal(right, pointer.PortB & allButtons);
        Assert.Equal(C.InitialHostSampleCycles, pointer.NextSignal);
        Assert.Empty(transitions);
        pointer.AdvanceTo(C.InitialHostSampleCycles - 1);
        Assert.Empty(transitions);
        pointer.AdvanceTo(C.InitialHostSampleCycles);
        Assert.Collection(transitions,
            x => { Assert.Equal('X', x.Axis); Assert.True(x.Level); Assert.Equal(0, x.Port & xMask); },
            y => { Assert.Equal('Y', y.Axis); Assert.True(y.Level); Assert.Equal(yMask, y.Port & yMask); });
        Assert.Null(pointer.NextSignal);
        var positive = pointer.PortB;
        transitions.Clear();
        pointer.SetInput(new(-1, -1, 0, false, true, false), C.InitialHostSampleCycles);
        Assert.Equal(left | middle, pointer.PortB & allButtons);
        pointer.AdvanceTo(C.InitialHostSampleCycles * 2);
        Assert.Equal(2, transitions.Count);
        Assert.All(transitions, value => Assert.False(value.Level));
        Assert.Equal(positive & (xMask | yMask), pointer.PortB & (xMask | yMask));
        transitions.Clear();
        pointer.AdvanceTo(C.InitialHostSampleCycles * 100);
        Assert.Empty(transitions);
        Assert.Null(pointer.NextSignal);
    }

    [Fact]
    public void GroupedInputAdvanceMatchesEveryDeadlineAndNeverQueriesTheReceiver()
    {
        var grouped = Run(grouped: true);
        Assert.Equal(Run(grouped: false), grouped);
        Assert.Equal(12, grouped.Count); // Sept fronts X et cinq fronts Y ; aucun front au repos.

        static List<(long Time, char Axis, bool High, byte Port)> Run(bool grouped)
        {
            var result = new List<(long, char, bool, byte)>();
            UserPortPointer? pointer = null;
            pointer = new(false, high => result.Add((pointer!.Time, 'X', high, pointer.PortB)),
                high => result.Add((pointer!.Time, 'Y', high, pointer.PortB)));
            pointer.SetInput(new(7, -5, 0, false, false, false), 13);
            if (grouped) pointer.AdvanceTo(C.InitialHostSampleCycles * 20);
            else for (var time = 14; time <= C.InitialHostSampleCycles * 20; time++) pointer.AdvanceTo(time);
            Assert.Null(pointer.NextSignal);
            return result;
        }
    }

    [Theory]
    [InlineData(false, 40, -20)]
    [InlineData(true, 40, -20)]
    [InlineData(false, 1200, -600)]
    [InlineData(true, 1200, -600)]
    public void HostMotionFinishesWithinItsSampleWithoutAConstantRateBacklog(bool compact, int dx, int dy)
    {
        const long sampleCycles = 40000;
        var events = new List<(long Time, char Axis)>();
        UserPortPointer? pointer = null;
        pointer = new(compact, _ => events.Add((pointer!.Time, 'X')),
            _ => events.Add((pointer!.Time, 'Y')));
        pointer.SetInput(EmulationInputSnapshot.Empty.Pointer, 0);
        pointer.SetInput(new(dx, dy, 0, false, false, false), sampleCycles);
        pointer.AdvanceTo(2 * sampleCycles);
        Assert.Equal(dx, events.Count(e => e.Axis == 'X'));
        Assert.Equal(-dy, events.Count(e => e.Axis == 'Y'));
        Assert.Equal(2 * sampleCycles, events.Last(e => e.Axis == 'X').Time);
        Assert.Equal(2 * sampleCycles, events.Last(e => e.Axis == 'Y').Time);
        Assert.All(events, e => Assert.InRange(e.Time, sampleCycles + 1, 2 * sampleCycles));
        Assert.Null(pointer.NextSignal);
        pointer.SetInput(EmulationInputSnapshot.Empty.Pointer, 2 * sampleCycles);
        pointer.AdvanceTo(4 * sampleCycles);
        Assert.Equal(dx - dy, events.Count);
        Assert.Null(pointer.NextSignal);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BoardRoutesPointerPinsThroughTheRealUserVia(bool compact)
    {
        var board = new BbcMicroMachine(new byte[16384], new byte[16384], 32768,
            expandedIo: true, master: compact, compact: compact, userPortPointer: true);
        try
        {
            board.Write(0xfe62, 0); // PB en entrée.
            board.Write(0xfe6c, 0x50); // CB1/CB2 sensibles aux fronts montants.
            board.Write(0xfe6d, 0x7f);
            board.Write(0xfe6e, 0x98);
            board.SetInput(new(new HashSet<EmulationKey>(), new(1, 1, 0, true, false, false), []));
            var left = compact ? C.CompactLeft : C.UserLeft;
            Assert.Equal(0, board.Read(0xfe60) & left);
            Assert.False(board.Irq);
            // La première fenêtre commence à l'entrée hôte, après les cycles de RESET.
            // La souris n'est pas asservie à une grille périodique globale de 2048 cycles.
            var remaining = C.InitialHostSampleCycles;
            for (var cycle = 0; cycle < remaining - 1; cycle++) board.Tick();
            Assert.False(board.Irq);
            board.Tick();
            Assert.Equal(0x98, board.Read(0xfe6d) & 0x98);
            Assert.True(board.Irq);
            _ = board.Read(0xfe60);
            Assert.False(board.Irq);
            board.Write(0xfe62, left); // Les sorties du VIA restent prioritaires sur ses entrées.
            board.Write(0xfe60, left);
            Assert.Equal(left, board.Read(0xfe60) & left);
            board.Reset(true);
            Assert.Equal(0, board.Read(0xfe60) & left); // Le reset du CPU ne relâche pas le bouton hôte.
        }
        finally { board.SetInput(EmulationInputSnapshot.Empty); board.Reset(true); }
    }

    [Theory]
    [InlineData(MachineModel.BbcMicroModelB)]
    [InlineData(MachineModel.BbcMicroModelBPlus)]
    [InlineData(MachineModel.BbcMaster128)]
    [InlineData(MachineModel.BbcMasterCompact)]
    public void ConnectorCanBeEnabledThroughTheExistingSettingsAndCaptureContracts(MachineModel model)
    {
        var factory = new AcornEmuMachineFactory();
        var configuration = new MachineConfiguration(model, core: Emulator.AcornEmu);
        Assert.False(factory.SupportsPointerCapture(configuration));
        var field = Assert.Single(factory.DescribeSettings(configuration).Where(block => block.Tab == EmulationMachineTab.Mouse)
            .SelectMany(block => block.Fields));
        Assert.Equal(C.ConnectedOption, field.Id);
        Assert.True(field.RequiresRestart);
        var engine = new Engine();
        configuration = (MachineConfiguration)engine.ApplySettings(configuration,
            new Dictionary<string, string?> { [field.Id] = bool.TrueString });
        Assert.True(factory.SupportsPointer(configuration));
        Assert.True(factory.SupportsPointerCapture(configuration));
        Assert.True(configuration.Input.CaptureMouse);
        Assert.True(new Engine().Describe(configuration.MachineId, configuration).Visibility.Tabs[EmulationMachineTab.Mouse]);
        PointerInputFunctions.Validate(configuration);
        var manualCapture = configuration with { Input = configuration.Input with { CaptureMouse = false } };
        var unchanged = (MachineConfiguration)engine.ApplySettings(manualCapture,
            new Dictionary<string, string?>());
        Assert.False(unchanged.Input.CaptureMouse);
        configuration = (MachineConfiguration)engine.ApplySettings(configuration,
            new Dictionary<string, string?> { [field.Id] = bool.FalseString });
        Assert.False(configuration.Input.CaptureMouse);
        Assert.False(factory.SupportsPointerCapture(configuration));
    }

    [Theory]
    [InlineData(MachineModel.BbcMicroModelA)]
    [InlineData(MachineModel.BbcMasterET)]
    public void ModelsWithoutTheConnectorDoNotAdvertiseOrCreateIt(MachineModel model)
    {
        var configuration = new MachineConfiguration(model, core: Emulator.AcornEmu);
        var factory = new AcornEmuMachineFactory();
        Assert.False(factory.SupportsPointerCapture(configuration));
        Assert.Null(new Engine().DescribeInputSettings(configuration).Mouse);
        Assert.DoesNotContain(factory.DescribeSettings(configuration), block => block.Tab == EmulationMachineTab.Mouse);
        configuration = configuration with { Options = new Dictionary<string, string>(configuration.Options) { [C.ConnectedOption] = bool.TrueString } };
        Assert.Throws<ArgumentException>(() => PointerInputFunctions.Validate(configuration));
    }
}
