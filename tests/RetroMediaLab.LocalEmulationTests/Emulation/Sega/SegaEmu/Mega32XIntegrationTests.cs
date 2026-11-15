using System.Buffers.Binary;
using System.Reflection;
using RetroMediaLab.Chips.CPU.Motorola.M68000.Execution;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Boards;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Factories;

namespace RetroMediaLab.Tests.Emulation.Sega.SegaEmu;

public sealed class Mega32XIntegrationTests
{
    [Fact]
    public void CartridgeReadsKeepBytesWordsLongsMirrorsAndFetchWaits()
    {
        SegaBoard? board = null;
        try
        {
            byte[] rom = Program(); Array.Resize(ref rom, 0x300000);
            for (int i = 0x1000; i < rom.Length; i++) rom[i] = (byte)(i ^ i >> 16);
            board = BoardFactory.Create("ThirtyTwoX", MachineIntegrationTests.Resources()); Assert.True(board.Load("mirror.32x", rom));
            var soc = board.Adapter32X!.Master;
            byte ByteAt(uint offset) => rom[(offset & 0x3fffff) % (uint)rom.Length];
            foreach (uint offset in new uint[] { 0, 0x1000, 0x2ffffe, 0x300000, 0x3ffffc, 0x3ffffe })
            {
                uint address = 0x02000000 + offset;
                Assert.Equal(ByteAt(offset), soc.Read8(address));
                Assert.Equal((ushort)(ByteAt(offset) << 8 | ByteAt(offset + 1)), soc.Read16(address));
                uint value = (uint)ByteAt(offset) << 24 | (uint)ByteAt(offset + 1) << 16 | (uint)ByteAt(offset + 2) << 8 | ByteAt(offset + 3);
                Assert.Equal(value, soc.Read32(address));
                Assert.Equal(soc.Read16(address), soc.Fetch16(address, out int waits)); Assert.Equal(6, waits);
            }
            var bus = new RetroMediaLab.Chips.Controllers.Hitachi.SH7095.Sh7095.CpuBus(soc);
            Assert.True(bus.TryMapFetch(0x02401234, out var memory, out uint start, out int mappedWaits));
            Assert.Equal(0x02400000u, start); Assert.Equal(0x400000, memory.Length); Assert.Equal(6, mappedWaits);
            Assert.Equal(ByteAt(0x1234), memory.Span[0x1234]);
        }
        finally { board?.Dispose(); }
    }

    [Fact]
    public void CartridgeBankWindowsPreserveAllFourSelections()
    {
        SegaBoard? board = null;
        try
        {
            byte[] rom = Program(); Array.Resize(ref rom, 0x400000);
            BinaryPrimitives.WriteUInt16BigEndian(rom.AsSpan(0x8000), 0xbeef);
            for (int bank = 0; bank < 4; bank++)
                BinaryPrimitives.WriteUInt16BigEndian(rom.AsSpan(bank * 0x100000 + 0x1000), (ushort)(0xa000 + bank));
            board = BoardFactory.Create("ThirtyTwoX", MachineIntegrationTests.Resources()); Assert.True(board.Load("banks.32x", rom));
            var bus = Bus(board); bus.Write16(0xa15100, 3);
            for (int bank = 0; bank < 4; bank++)
            {
                bus.Write16(0xa15104, (uint)bank);
                Assert.Equal((uint)(0xa000 + bank), bus.ReadMapped16(0x901000));
                Assert.Equal(0xbeefu, bus.ReadMapped16(0x888000));
            }

        }
        finally { board?.Dispose(); }
    }

    internal static byte[] Program()
    {
        byte[] rom = MachineIntegrationTests.Program(0x80); "SEGA 32X        "u8.CopyTo(rom.AsSpan(0x100));
        void Put(int offset, uint value) => BinaryPrimitives.WriteUInt32BigEndian(rom.AsSpan(offset), value);
        Put(0x3d4, 0x800); Put(0x3d8, 0x200); Put(0x3dc, 32); Put(0x3e0, 0x06000200); Put(0x3e4, 0x06000210);
        Put(0x3e8, 0x06000000); Put(0x3ec, 0x06000000);
        new byte[] { 0xe0, 0x11, 0xc2, 0x0a, 0xaf, 0xfe, 0x00, 0x09 }.CopyTo(rom, 0x800);
        new byte[] { 0xe0, 0x22, 0xc2, 0x0b, 0xaf, 0xfe, 0x00, 0x09 }.CopyTo(rom, 0x810);
        return rom;
    }
    private static ICpu68000Bus Bus(SegaBoard board)
    {
        object cpu = typeof(SegaBoard).GetField("_mainCpuComponent", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(board)!;
        return (ICpu68000Bus)cpu.GetType().GetField("_bus", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(cpu)!;
    }
    [Fact]
    public void PollingAdjacentWordsAtDifferentInstructionsSleepsAndWakesOnWrite()
    {
        SegaBoard? board = null;
        try
        {
            board = BoardFactory.Create("ThirtyTwoX", MachineIntegrationTests.Resources()); Assert.True(board.Load("synthetic.32x", Program()));
            var bus = Bus(board); bus.Write16(0xa15100, 3); bus.Write16(0xa15120, 0x1111); bus.Write16(0xa15122, 0x2222);
            var adapter = board.Adapter32X!; var cpu = adapter.Master.Cpu;
            for (int i = 0; i < 12; i++)
            {
                cpu.ProgramCounter = 0x06000100; cpu.Idle(6); Assert.Equal(0x1111, adapter.Master.Read16(0x20004020));
                cpu.ProgramCounter = 0x06000102; cpu.Idle(6); Assert.Equal(0x2222, adapter.Master.Read16(0x20004022));
            }
            var polling = (bool[])adapter.GetType().GetField("_polling", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(adapter)!;
            Assert.True(polling[0]); bus.Write16(0xa15122, 0x1234); Assert.False(polling[0]);
        }
        finally { board?.Dispose(); }
    }

    [Fact]
    public void CommandInterruptRequestsCanBeWithdrawnIndependently()
    {
        SegaBoard? board = null;
        try
        {
            board = BoardFactory.Create("ThirtyTwoX", MachineIntegrationTests.Resources()); Assert.True(board.Load("synthetic.32x", Program()));
            var bus = Bus(board);
            bus.Write16(0xa15100, 3);
            var adapter = board.Adapter32X!;
            adapter.Master.Write16(0x20004000, 2); adapter.Slave.Write16(0x20004000, 2);
            adapter.Master.Cpu.StatusRegister = adapter.Slave.Cpu.StatusRegister = 0;
            bus.Write16(0xa15102, 3); Assert.Equal(3u, bus.Read16(0xa15102));
            Assert.True(adapter.Master.Cpu.InterruptReady); Assert.True(adapter.Slave.Cpu.InterruptReady);
            bus.Write16(0xa15102, 0); Assert.Equal(0u, bus.Read16(0xa15102));
            Assert.False(adapter.Master.Cpu.InterruptReady); Assert.False(adapter.Slave.Cpu.InterruptReady);
            bus.Write16(0xa15102, 2); Assert.Equal(2u, bus.Read16(0xa15102));
            Assert.False(adapter.Master.Cpu.InterruptReady); Assert.True(adapter.Slave.Cpu.InterruptReady);
            bus.Write8(0xa15103, 1); Assert.Equal(1u, bus.Read16(0xa15102));
            bus.Write8(0xa15102, 0); Assert.Equal(1u, bus.Read16(0xa15102));
            bus.Write8(0xa15103, 0); Assert.Equal(0u, bus.Read16(0xa15102));
        }
        finally { board?.Dispose(); }
    }

    [Fact]
    public void AdapterRegistersAndBothProcessorsWorkThroughTheMachineBus()
    {
        SegaBoard? board = null;
        try
        {
            board = BoardFactory.Create("ThirtyTwoX", MachineIntegrationTests.Resources()); Assert.True(board.Load("synthetic.32x", Program()));
            Assert.True(board.Has32X); var bus = Bus(board); Assert.Equal(0x4d41u, bus.Read16(0xa130ec)); Assert.Equal(0x5253u, bus.Read16(0xa130ee));
            bus.Write16(0xa15100, 3); Assert.Equal(3u, bus.Read16(0xa15100) & 3);
            Assert.Equal(0xe011u, bus.ReadMapped16(0x880800));
            short[] audio = new short[4096]; for (int i = 0; i < 5; i++) board.RunFrame(audio);
            Assert.Equal(0x11u, bus.Read16(0xa1512a)); Assert.Equal(0x22u, bus.Read16(0xa1512e));
            Assert.InRange(board.Adapter32X!.Master.Cpu.ProgramCounter, 0x06000200u, 0x06000208u);
            Assert.InRange(board.Adapter32X.Slave.Cpu.ProgramCounter, 0x06000210u, 0x06000218u);
            bus.Write8(0xa10009, 0x40); bus.Write8(0xa10003, 0x40); board.Input.DigitalInputs[0] = 0;
            uint released = bus.Read8(0xa10003); board.Input.DigitalInputs[0] = 16; uint pressed = bus.Read8(0xa10003);
            Assert.NotEqual(released, pressed);
        }
        finally { board?.Dispose(); }
    }
    [Fact]
    public void ReplacementBootstrapUsesTheRegistersWithoutACartridge()
    {
        SegaBoard? board = null;
        try
        {
            board = BoardFactory.Create("ThirtyTwoX", MachineIntegrationTests.Resources()); Assert.True(board.Load(""));
            var bus = Bus(board); uint vector = bus.Read16(0); bus.Write16(0xa15100, 3);
            Assert.Equal(vector, bus.Read16(0)); Assert.Equal(vector, bus.ReadMapped16(0));
            Assert.Equal(0x20004000u, board.Adapter32X!.Master.Cpu.GlobalBase);
            Assert.Equal(0x20004000u, board.Adapter32X.Slave.Cpu.GlobalBase);
        }
        finally { board?.Dispose(); }
    }
    [Fact]
    public void MachineStateIncludesThe32XAndRejectsCorruptionBeforeMutation()
    {
        SegaBoard? board = null;
        try
        {
            board = BoardFactory.Create("ThirtyTwoX", MachineIntegrationTests.Resources()); Assert.True(board.Load("synthetic.32x", Program()));
            var bus = Bus(board); bus.Write16(0xa15100, 3); short[] audio = new short[4096];
            for (int frame = 0; frame < 3; frame++) board.RunFrame(audio);
            byte[] saved = new byte[board.StateSize]; Assert.Equal(saved.Length, board.SaveState(saved));
            board.RunFrame(audio); byte[] expected = new byte[board.StateSize]; board.SaveState(expected);
            Assert.Equal(saved.Length, board.LoadState(saved)); board.RunFrame(audio);
            byte[] replay = new byte[board.StateSize]; board.SaveState(replay); Assert.Equal(expected, replay);
            byte[] corrupt = saved.ToArray();
            int offset = saved.Length - RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.ThirtyTwoX.Hardware.Mega32X.ContextSize;
            corrupt[offset] ^= 0xff; Assert.Equal(0, board.LoadState(corrupt));
            byte[] after = new byte[board.StateSize]; board.SaveState(after); Assert.Equal(replay, after);
        }
        finally { board?.Dispose(); }
    }
}

