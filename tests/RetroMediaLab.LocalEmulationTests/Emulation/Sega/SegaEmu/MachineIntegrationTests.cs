using System.Buffers.Binary;
using System.IO;
using System.Reflection;
using RetroMediaLab.Chips.CPU.Zilog.Z80.Instruction;
using RetroMediaLab.Emulation.Sega.Common.Machines.Common.Constants;
using RetroMediaLab.Emulation.Sega.Common.Machines.Common.Contracts;
using RetroMediaLab.Emulation.Sega.Emulators.Common.Interop.Functions;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Common;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Constants;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Factories;

namespace RetroMediaLab.Tests.Emulation.Sega.SegaEmu;

public sealed class MachineIntegrationTests
{
    public static TheoryData<string, string, byte, string> Machines => new()
    {
        { ModelConstants.Sg1000, "Sg1000Machine", 1, ".sg" },
        { ModelConstants.Sc3000, "Sc3000Machine", 1, ".sg" },
        { ModelConstants.MarkIII, "MarkIIIMachine", 0x10, ".sms" },
        { ModelConstants.MasterSystem, "MasterSystemMachine", 0x21, ".sms" },
        { ModelConstants.GameGear, "GameGearMachine", 0x40, ".gg" },
        { ModelConstants.MegaDrive, "MegaDriveMachine", 0x80, ".md" },
        { ModelConstants.Pico, "PicoMachine", 0x82, ".md" },
    };

    [Theory, MemberData(nameof(Machines))]
    public void MachineRunsAndRestoresItsInMemoryProgram(string model, string type, byte hardware, string extension)
    {
        SegaBoard? board = null;
        try
        {
            board = BoardFactory.Create(model, Resources());
            Assert.Equal(type, board.GetType().Name);
            Assert.True(board.Load("synthetic" + extension, Program(hardware)));
            Assert.Equal(hardware, board.Hardware);
            board.RefreshConfiguration();
            short[] audio = new short[4096];
            for (int frame = 0; frame < 3; frame++) Assert.InRange(board.RunFrame(audio), 700, 900);
            byte[] saved = new byte[board.StateSize];
            Assert.Equal(saved.Length, board.SaveState(saved));
            board.RunFrame(audio);
            byte[] expected = new byte[board.StateSize]; board.SaveState(expected);
            Assert.Equal(saved.Length, board.LoadState(saved));
            board.RunFrame(audio);
            byte[] replay = new byte[board.StateSize]; board.SaveState(replay);
            Assert.Equal(expected, replay);
            if (hardware < 0x80)
            {
                foreach (string field in new[] { "_mainCpuComponent", "_mdComponent", "_cdComponent", "_cdCartridgeComponent", "_megaSdComponent", "_yx5200Component" })
                    Assert.Null(typeof(SegaBoard).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(board));
            }
        }
        finally { board?.Dispose(); }
    }

    [Fact]
    public void MegaCdHasItsOwnBoardWithoutOpeningAnImage()
    {
        SegaBoard? board = null;
        try { board = BoardFactory.Create(ModelConstants.MegaCd, Resources()); Assert.Equal("MegaCdMachine", board.GetType().Name); Assert.Equal(0x84, board.Hardware); }
        finally { board?.Dispose(); }
    }

    [Fact]
    public void InvalidSavedStateDoesNotMutateOrCloseTheRunningMachine()
    {
        SegaBoard? board = null;
        try
        {
            board = BoardFactory.Create(ModelConstants.Sg1000, Resources());
            Assert.True(board.Load("synthetic.sg",Program(1)));
            short[] audio = new short[4096]; board.RunFrame(audio);
            byte[] state = new byte[board.StateSize]; board.SaveState(state);
            Assert.Equal(0,board.LoadState(state.AsSpan(0,32)));
            byte[] after = new byte[board.StateSize]; board.SaveState(after);
            Assert.Equal(state,after);
            byte[] incompatible = state.ToArray(); incompatible[16] = 0x80;
            Assert.Equal(0,board.LoadState(incompatible));
            Assert.InRange(board.RunFrame(audio),700,900);
        }
        finally { board?.Dispose(); }
    }

    [Fact]
    public void MasterSystemJoypadReachesTheHardwarePort()
    {
        SegaBoard? board = null;
        try
        {
            board = BoardFactory.Create(ModelConstants.MasterSystem, Resources());
            Assert.True(board.Load("synthetic.sms", Program(0x21)));
            board.ConfigureInputPort(0, 1, 2);
            ICpuZ80Bus bus = board;
            board.Input.DigitalInputs[0] = 0;
            byte released = bus.ReadPort(0xdc);
            board.Input.DigitalInputs[0] = 16;
            byte pressed = bus.ReadPort(0xdc);
            Assert.Equal(0x10, (released ^ pressed) & 0x10);
            Assert.Equal(0, pressed & 0x10);
        }
        finally { board?.Dispose(); }
    }

    [Theory]
    [InlineData(ModelConstants.Sg1000, "segaemu_sg1000_model", "sg-1000 II")]
    [InlineData(ModelConstants.MasterSystem, "segaemu_master_system_model", "master system II")]
    public void MachineVariantsRemainVisibleAndEditable(string model, string key, string value)
    {
        MachineConfiguration configuration = new(model, CoreConstants.Id, new Dictionary<string,string> { [key] = value });
        MachineConfiguration configured = CoreSettingsFunctions.Configure(configuration);
        Assert.Equal(value, configured.Options![key]);
        Assert.Contains(CoreSettingsFunctions.Blocks(configured).SelectMany(block => block.Fields), field => field.Id == key);
        Assert.DoesNotContain(CoreSettingsFunctions.Blocks(configured).SelectMany(block => block.Fields), field => field.Id.Contains("system_hw", StringComparison.Ordinal));
    }

    internal static MachineResources Resources() => new(new(), new byte[1024 * 626 * 4], 1024, 626, 4096,
        44100, _ => null, (_, _) => 0, CancellationToken.None);

    internal static byte[] Program(byte hardware)
    {
        byte[] rom = new byte[0x8000];
        if (hardware < 0x80) new byte[] { 0x21,0x00,0xc0,0x34,0xc3,0x03,0x00 }.CopyTo(rom,0);
        else
        {
            BinaryPrimitives.WriteUInt32BigEndian(rom, 0x00ff1000);
            BinaryPrimitives.WriteUInt32BigEndian(rom.AsSpan(4), 0x200);
            (hardware == 0x82 ? "SEGA PICO       "u8 : "SEGA MEGA DRIVE "u8).CopyTo(rom.AsSpan(0x100));
            "U"u8.CopyTo(rom.AsSpan(0x1f0));
            new byte[] { 0x52,0x39,0x00,0xff,0x00,0x00,0x60,0xf8 }.CopyTo(rom,0x200);
        }
        return rom;
    }
}
