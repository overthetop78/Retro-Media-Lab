using System.Reflection;
using RetroMediaLab.Chips.CPU.Motorola.M68000.Execution;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Boards;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Factories;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Contracts;

namespace RetroMediaLab.Tests.Emulation.Sega.SegaEmu;

public sealed class PicoIntegrationTests
{
    [Theory]
    [InlineData(1, 0x60)]
    [InlineData(2, 0x20)]
    [InlineData(3, 0x40)]
    [InlineData(4, 0x00)]
    public void RegionRegisterUsesPicoWiring(byte region, uint expected)
    {
        SegaBoard? board = null;
        try
        {
            var resources = MachineIntegrationTests.Resources() with { Configuration = new SystemConfiguration { region_detect = region } };
            board = BoardFactory.Create("Pico", resources);
            Assert.True(board.Load("synthetic.md", MachineIntegrationTests.Program(0x82)));
            object cpu = typeof(SegaBoard).GetField("_mainCpuComponent", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(board)!;
            var bus = (ICpu68000Bus)cpu.GetType().GetField("_bus", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(cpu)!;
            Assert.Equal(expected, bus.Read8(0x800001));
        }
        finally { board?.Dispose(); }
    }

    [Theory]
    [InlineData("EF", 0xc0)]
    [InlineData("8F", 0xc0)]
    [InlineData("5", 0x80)]
    [InlineData("J", 0)]
    public void AutomaticRegionUsesPicoHeaderPriority(string header, byte expected)
    {
        SegaBoard? board = null;
        try
        {
            byte[] rom = MachineIntegrationTests.Program(0x82);
            rom.AsSpan(0x1f0, 16).Fill(32);
            System.Text.Encoding.ASCII.GetBytes(header).CopyTo(rom, 0x1f0);
            board = BoardFactory.Create("Pico", MachineIntegrationTests.Resources());
            Assert.True(board.Load("synthetic.md", rom)); Assert.Equal(expected, board.Region);
        }
        finally { board?.Dispose(); }
    }

    [Fact]
    public void HorizontalVideoInterruptIsWiredToLevelFive()
    {
        SegaBoard? board = null;
        try
        {
            board = BoardFactory.Create("Pico", MachineIntegrationTests.Resources());
            Assert.True(board.Load("synthetic.md", MachineIntegrationTests.Program(0x82)));
            ((RetroMediaLab.Chips.Video.Sega.VDP.IVdpBus)board).SetMainIrq(4);
            object cpu = typeof(SegaBoard).GetField("_mainCpuComponent", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(board)!;
            var state = (Cpu68000State)cpu.GetType().GetProperty("State")!.GetValue(cpu)!;
            Assert.Equal(5u, state.int_level >> 8);
        }
        finally { board?.Dispose(); }
    }

    [Fact]
    public void HostPointerReachesPadBookAndPageRegisters()
    {
        RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Services.ManagedCore? core = null;
        SegaBoard? board = null;
        bool transferred = false;
        try
        {
            core = new();
            var type = core.GetType();
            var configuration = RetroMediaLab.Emulation.Sega.Emulators.Common.Interop.Functions.CoreSettingsFunctions.Configure(new("Pico", "segaemu"));
            type.GetField("_configuration", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(core, configuration);
            var settings = (SystemConfiguration)type.GetMethod("CreateSettings", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(core, null)!;
            type.GetField("_settings", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(core, settings);
            byte[] bitmap = (byte[])type.GetField("_bitmap", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(core)!;
            board = BoardFactory.Create("Pico", new(settings, bitmap, 1024, 626, 4096, 44100, _ => null, (_, _) => 0, CancellationToken.None));
            Assert.True(board.Load("synthetic.md", MachineIntegrationTests.Program(0x82)));
            board.RefreshConfiguration();
            board.PollInput = type.GetMethod("PollInputs", BindingFlags.Instance | BindingFlags.NonPublic)!.CreateDelegate<Action>(core);
            type.GetField("_system", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(core, board); transferred = true;
            core.SetInput(new(new HashSet<RetroMediaLab.Emulation.Enums.EmulationKey>(), new(0, 0, 0, true, false, false), []));
            core.RunFrame(); Assert.InRange(board.Input.AnalogInputs[1], (short)508, (short)759);
            core.SetInput(new(new HashSet<RetroMediaLab.Emulation.Enums.EmulationKey>(), new(0, 0, -1, true, false, true), []));
            core.RunFrame(); Assert.InRange(board.Input.AnalogInputs[1], (short)760, (short)1011);
            Assert.Equal(1, board.PicoPage); Assert.NotEqual(0, board.Input.DigitalInputs[0] & 128);
            core.RunFrame(); Assert.Equal(1, board.PicoPage); // A wheel event is consumed once.
        }
        finally { try { core?.Dispose(); } finally { if (!transferred) board?.Dispose(); } }
    }

    [Fact]
    public void SavedPageResumesAndCorruptPageIsRejectedBeforeMutation()
    {
        SegaBoard? board = null;
        try
        {
            board = BoardFactory.Create("Pico", MachineIntegrationTests.Resources());
            Assert.True(board.Load("synthetic.md", MachineIntegrationTests.Program(0x82)));
            board.PicoPage = 3;
            byte[] saved = new byte[board.StateSize];
            Assert.Equal(saved.Length, board.SaveState(saved));
            Assert.True(saved.AsSpan(0, 16).SequenceEqual("SEGAEMU 00000004"u8));
            board.PicoPage = 5;
            Assert.Equal(saved.Length, board.LoadState(saved));
            Assert.Equal(3, board.PicoPage);
            byte[] invalid = saved.ToArray(); invalid[invalid.Length - 193] = 8;
            Assert.Equal(0, board.LoadState(invalid));
            Assert.Equal(3, board.PicoPage);
            byte[] after = new byte[board.StateSize]; board.SaveState(after);
            Assert.Equal(saved, after);
            byte[] old = saved[..^193]; old[15] = (byte)'3';
            Assert.Equal(old.Length, board.LoadState(old));
            Assert.Equal(0, board.PicoPage);
        }
        finally { board?.Dispose(); }
    }
}
