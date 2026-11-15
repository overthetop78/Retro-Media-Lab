using System.Reflection;
using RetroMediaLab.Chips.Controllers.Sega.Input;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Enums;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Boards;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Contracts;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Factories;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Services;

namespace RetroMediaLab.Tests.Emulation.Sega.SegaEmu;

public sealed class PointerSensitivityTests
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    [Fact]
    public void MouseTabAndConditionalPortFieldsDescribeEditableHundredths()
    {
        var configuration = RetroMediaLab.Emulation.Sega.Emulators.Common.Interop.Functions.CoreSettingsFunctions.Configure(new("Pico", "segaemu"));
        var fields = new MachineFactory().DescribeSettings(configuration).SelectMany(block => block.Fields).ToArray();
        var global = fields.Single(field => field.Id == "segaemu_pointer_speed");
        Assert.Equal(EmulationMachineTab.Mouse, global.Tab);
        Assert.Equal(0.10, global.NumericRange!.Step); Assert.Equal(0.01, global.NumericRange.FineStep);
        Assert.True(global.NumericRange.ShowNumericInput);
        for (int port = 1; port <= 2; port++)
        {
            var field = fields.Single(item => item.Id == $"segaemu_pointer_port{port}_speed");
            Assert.Equal(port, field.ControllerPortNumber); Assert.Equal(EmulationMachineTab.Controllers, field.Tab);
            Assert.Equal("auto", field.AutomaticValue); Assert.False(field.RequiresRestart);
            Assert.Contains("SegaMegaMouse", field.AppliesToControllerIds!);
            Assert.DoesNotContain("SegaMegaDriveSixButton", field.AppliesToControllerIds!);
        }
    }

    [Fact]
    public void PortOverridesAndAutomaticInheritanceRemainIndependent()
    {
        WithCore(3, (core, _) =>
        {
            core.SetOption("segaemu_pointer_port2_speed", "1.23");
            Move(core, 100, -100);
            Assert.Equal(200, Read(core, 2, 0)); Assert.Equal(123, Read(core, 2, 0, 1));
            Assert.Equal(-123, Read(core, 2, 1, 1));
            core.SetOption("segaemu_pointer_speed", "3.14");
            Move(core, 100, 0);
            Assert.Equal(314, Read(core, 2, 0)); Assert.Equal(123, Read(core, 2, 0, 1));
            core.SetOption("segaemu_pointer_port2_speed", "auto");
            Move(core, 100, 0); Assert.Equal(314, Read(core, 2, 0, 1));
            Assert.Throws<ArgumentException>(() => core.SetOption("segaemu_pointer_speed", "1.234"));
            Assert.Throws<ArgumentException>(() => core.SetOption("segaemu_pointer_speed", "0.09"));
            Assert.Throws<ArgumentException>(() => core.SetOption("segaemu_pointer_speed", "8.01"));
        });
    }

    [Fact]
    public void AbsoluteFractionsPreserveSmallMovesAtMinimumSpeed()
    {
        WithCore(8, (core, _) =>
        {
            core.SetOption("segaemu_pointer_speed", "0.10");
            for (int i = 0; i < 10; i++) Move(core, 1, -1);
            Assert.Equal(129, Read(core, 6, 0)); Assert.Equal(-127, Read(core, 6, 1));
        });
    }

    [Theory]
    [InlineData(3)] // Mega Mouse
    [InlineData(4)] // Light Phaser / Menacer
    [InlineData(5)] // Paddle Control
    [InlineData(6)] // Sports Pad
    [InlineData(7)] // Drawing tablet
    [InlineData(8)] // Pico pen
    [InlineData(9)] // Graphic board
    public void GainReachesRelativeAndAbsoluteDevices(byte device)
    {
        WithCore(device, (core, board) =>
        {
            core.SetOption("segaemu_pointer_speed", "1.00");
            Move(core, 20, 10);
            short first = board.Input.AnalogInputs[0];
            Assert.Equal(2561, Read(core, 6, 0));
            Assert.Equal(2561, Read(core, 4, 13));
            core.SetOption("segaemu_pointer_speed", "2.00");
            ((int[])typeof(ManagedCore).GetField("_pointerX", Private)!.GetValue(core)!)[0] = 1;
            ((int[])typeof(ManagedCore).GetField("_pointerY", Private)!.GetValue(core)!)[0] = 1;
            Move(core, 20, 10);
            Assert.Equal(5121, Read(core, 6, 0));
            Assert.Equal(2561, Read(core, 6, 1));
            Assert.True(board.Input.AnalogInputs[0] > first);
            Assert.Equal(1, Read(core, 2, 2)); // Mouse buttons are preserved.
        });
    }

    [Theory]
    [InlineData(5)]
    [InlineData(6)]
    public void AnalogPointingDevicesKeepTheirControllerAxes(byte device)
    {
        WithCore(device, (core, board) =>
        {
            Move(core, 20, 10);
            short mouse = board.Input.AnalogInputs[0];
            Assert.True(mouse > 128);
            core.SetInput(new(new HashSet<EmulationKey>(), new(0, 0, 0, false, false, false),
                [EmulationControllerState.Empty with { LeftX = -32767, LeftY = 32767 }]));
            Poll(core);
            Assert.Equal(0, board.Input.AnalogInputs[0]);
            if (device == 6) Assert.Equal(255, board.Input.AnalogInputs[1]);
        });
    }

    [Theory]
    [InlineData("0.25", 1)]
    [InlineData("0.50", 2)]
    [InlineData("1.00", 4)]
    [InlineData("1.50", 6)]
    [InlineData("2.00", 8)]
    [InlineData("8.00", 32)]
    public void RelativeMouseUsesSelectedMultiplier(string value, short expected)
    {
        WithCore(3, (core, _) =>
        {
            core.SetOption("segaemu_pointer_speed", value);
            Move(core, 4, -4);
            Assert.Equal(expected, Read(core, 2, 0));
            Assert.Equal(-expected, Read(core, 2, 1));
            Poll(core);
            Assert.Equal(0, Read(core, 2, 0));
            Assert.Equal(0, Read(core, 2, 1));
        });
    }

    [Fact]
    public void SmallRelativeMovementsRetainFractionsAndCancelInOppositeDirection()
    {
        WithCore(3, (core, _) =>
        {
            core.SetOption("segaemu_pointer_speed", "0.25");
            for (int i = 0; i < 4; i++)
            {
                Move(core, 1, -1);
                Assert.Equal(i == 3 ? 1 : 0, Read(core, 2, 0));
                Assert.Equal(i == 3 ? -1 : 0, Read(core, 2, 1));
            }
            Move(core, 1, 0); Move(core, -1, 0); Move(core, 3, 0);
            Assert.Equal(0, Read(core, 2, 0));
            Move(core, 1, 0); Assert.Equal(1, Read(core, 2, 0));
        });
    }

    [Fact]
    public void DefaultIsFasterAndLargeMovementsSaturateWithoutWrappingOrRepeating()
    {
        WithCore(8, (core, board) =>
        {
            Move(core, 1, -1);
            Assert.Equal(257, Read(core, 6, 0));
            Assert.Equal(-255, Read(core, 6, 1));
            core.SetOption("segaemu_pointer_speed", "8.00");
            Move(core, int.MaxValue, int.MinValue);
            Assert.Equal(short.MaxValue, Read(core, 2, 0));
            Assert.Equal(short.MinValue, Read(core, 2, 1));
            Assert.Equal(32767, Read(core, 6, 0)); Assert.Equal(-32767, Read(core, 6, 1));
            Assert.InRange(board.Input.AnalogInputs[0], (short)60, (short)380);
            Poll(core); Assert.Equal(0, Read(core, 2, 0)); Assert.Equal(32767, Read(core, 6, 0));
            Move(core, -1, 1); Assert.Equal(31743, Read(core, 6, 0)); Assert.Equal(-31743, Read(core, 6, 1));
        });
    }

    [Fact]
    public void LiveChangePreservesMachinePageAndPointerPosition()
    {
        WithCore(8, (core, board) =>
        {
            board.PicoPage = 3; Move(core, 5, 0);
            short before = Read(core, 6, 0);
            core.SetOption("segaemu_pointer_speed", "4.00");
            Assert.Same(board, typeof(ManagedCore).GetField("_system", Private)!.GetValue(core));
            Assert.Equal(3, board.PicoPage); Assert.Equal(before, Read(core, 6, 0));
            Move(core, 5, 0); Assert.Equal(before + 2560, Read(core, 6, 0));
            Assert.False(core.Options.Single(option => option.Key == "segaemu_pointer_speed").RequiresRestart);
        });
    }

    private static void Move(ManagedCore core, int x, int y)
    {
        core.SetInput(new(new HashSet<EmulationKey>(), new(x, y, 0, true, true, false), []));
        Poll(core);
    }
    private static void Poll(ManagedCore core) => typeof(ManagedCore).GetMethod("PollInputs", Private)!.Invoke(core, null);
    private static short Read(ManagedCore core, uint device, uint id, uint port = 0) =>
        (short)typeof(ManagedCore).GetMethod("ReadHostInput", Private)!.Invoke(core, [port, device, 0u, id])!;

    private static void WithCore(byte device, Action<ManagedCore, SegaBoard> test)
    {
        ManagedCore? core = null; SegaBoard? board = null; bool transferred = false;
        try
        {
            core = new();
            var configuration = RetroMediaLab.Emulation.Sega.Emulators.Common.Interop.Functions.CoreSettingsFunctions.Configure(new("Pico", "segaemu"));
            typeof(ManagedCore).GetField("_configuration", Private)!.SetValue(core, configuration);
            var settings = (SystemConfiguration)typeof(ManagedCore).GetMethod("CreateSettings", Private)!.Invoke(core, null)!;
            typeof(ManagedCore).GetField("_settings", Private)!.SetValue(core, settings);
            byte[] bitmap = (byte[])typeof(ManagedCore).GetField("_bitmap", Private)!.GetValue(core)!;
            board = BoardFactory.Create("Pico", new(settings, bitmap, 1024, 626, 4096, 44100, _ => null, (_, _) => 0, CancellationToken.None));
            Assert.True(board.Load("synthetic.md", MachineIntegrationTests.Program(0x82)));
            board.RefreshConfiguration(); board.Input.Initialize(board.Hardware, new(PrimaryDevice: device));
            typeof(ManagedCore).GetField("_system", Private)!.SetValue(core, board); transferred = true;
            test(core, board);
        }
        finally { try { core?.Dispose(); } finally { if (!transferred) board?.Dispose(); } }
    }
}
