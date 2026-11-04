using RetroMediaLab.Chips.CPU.Common.Interfaces;
using RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Services;

namespace RetroMediaLab.Chips.Tests.CPU.MOS.MCS6500.Chip6502;

public sealed class BusCyclePinsTests
{
    private enum Signal { PrepareRead, PrepareWrite, Tick, Read, Write, Undriven }
    private readonly record struct Edge(Signal Signal, ushort Address = 0, byte Data = 0);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OptionalPinsPreserveBusTraceCyclesRegistersAndMemory(bool cmos)
    {
        var ordinary = new Bus();
        var prepared = new PreparedBus();
        byte[] program = [0xa9, 0xa5, 0x8d, 0, 0x20, 0xee, 0, 0x20, 0xad, 0, 0x20, 0xea];
        program.CopyTo(ordinary.Memory, 0x200);
        program.CopyTo(prepared.Memory, 0x200);
        var first = new Cpu6502(ordinary, cmos);
        var second = new Cpu6502(prepared, cmos);
        first.Reset();
        second.Reset();
        for (var instruction = 0; instruction < 5; instruction++)
        {
            first.Step();
            second.Step();
        }

        Assert.Equal(ordinary.Edges, prepared.Edges.Where(edge =>
            edge.Signal is not (Signal.PrepareRead or Signal.PrepareWrite)));
        Assert.Equal(ordinary.Memory, prepared.Memory);
        Assert.Equal((first.A, first.X, first.Y, first.S, first.P, first.PC, first.Cycles),
            (second.A, second.X, second.Y, second.S, second.P, second.PC, second.Cycles));
        Assert.Equal(0xa6, second.A);
        AssertPreparationOrder(prepared.Edges);
    }

    [Fact]
    public void ReadyRepeatsAddressPreparationBeforeEachHeldRead()
    {
        var bus = new PreparedBus();
        bus.Memory[0x200] = 0xea;
        var cpu = new Cpu6502(bus);
        cpu.Reset();
        bus.Edges.Clear();
        bus.Ready = false;
        cpu.Step();
        cpu.Step();
        Assert.Equal(new Edge[]
        {
            new(Signal.PrepareRead, 0x200), new(Signal.Tick), new(Signal.Read, 0x200, 0xea),
            new(Signal.PrepareRead, 0x200), new(Signal.Tick), new(Signal.Read, 0x200, 0xea)
        }, bus.Edges);
        bus.Ready = true;
        cpu.Step();
        Assert.Equal(0x201, cpu.PC);
        AssertPreparationOrder(bus.Edges);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DataEnableIsSampledAfterPreparationAndTick(bool dataEnabled)
    {
        var bus = new PreparedBus();
        byte[] program = [0xa9, 0x57, 0x8d, 0, 0x20];
        program.CopyTo(bus.Memory, 0x200);
        var cpu = new Cpu6502(bus);
        cpu.Reset();
        cpu.Step();
        bus.Edges.Clear();
        // Change DBE in Tick, after the write address and proposed data have been presented.
        bus.OnTick = () => bus.DataBusEnabled = dataEnabled;
        cpu.Step();
        Assert.Equal(new Edge(Signal.PrepareWrite, 0x2000, 0x57), bus.Edges[^3]);
        Assert.Equal(new Edge(Signal.Tick), bus.Edges[^2]);
        Assert.Equal(dataEnabled ? new Edge(Signal.Write, 0x2000, 0x57)
            : new Edge(Signal.Undriven, 0x2000), bus.Edges[^1]);
        Assert.Equal(dataEnabled ? 0x57 : 0, bus.Memory[0x2000]);
        AssertPreparationOrder(bus.Edges);
    }

    [Fact]
    public void ResetAssertedDuringTickCancelsPreparedTransfer()
    {
        var bus = new PreparedBus();
        var cpu = new Cpu6502(bus);
        cpu.Reset();
        bus.Edges.Clear();
        bus.OnTick = () => bus.ResetRequested = true;
        cpu.Step();
        Assert.Equal(new Edge[] { new(Signal.PrepareRead, 0x200), new(Signal.Tick) }, bus.Edges);
        bus.Edges.Clear();
        cpu.Step();
        Assert.Equal(new Edge[] { new(Signal.Tick) }, bus.Edges);
    }

    private static void AssertPreparationOrder(List<Edge> edges)
    {
        for (var index = 0; index < edges.Count; index++)
        {
            var transfer = edges[index];
            if (transfer.Signal is not (Signal.Read or Signal.Write or Signal.Undriven)) continue;
            Assert.True(index >= 2);
            Assert.Equal(Signal.Tick, edges[index - 1].Signal);
            var preparation = edges[index - 2];
            Assert.Equal(transfer.Signal == Signal.Read ? Signal.PrepareRead : Signal.PrepareWrite,
                preparation.Signal);
            Assert.Equal(transfer.Address, preparation.Address);
            if (transfer.Signal == Signal.Write) Assert.Equal(transfer.Data, preparation.Data);
        }
    }

    private class Bus : ICpuBus
    {
        public byte[] Memory { get; } = new byte[ushort.MaxValue + 1];
        public List<Edge> Edges { get; } = [];
        public bool Irq => false;
        public bool Nmi => false;
        public bool Ready { get; set; } = true;
        public bool ResetRequested { get; set; }
        public bool DataBusEnabled { get; set; } = true;
        public Action? OnTick { get; set; }

        public Bus() => Memory[0xfffd] = 2;
        public byte Read(ushort address)
        {
            var data = Memory[address];
            Edges.Add(new(Signal.Read, address, data));
            return data;
        }
        public void Write(ushort address, byte data)
        {
            Edges.Add(new(Signal.Write, address, data));
            Memory[address] = data;
        }
        public void WriteUndriven(ushort address) => Edges.Add(new(Signal.Undriven, address));
        public void Tick()
        {
            Edges.Add(new(Signal.Tick));
            OnTick?.Invoke();
        }
    }

    private sealed class PreparedBus : Bus, ICpuBusCyclePins
    {
        public void PrepareRead(ushort address) => Edges.Add(new(Signal.PrepareRead, address));
        public void PrepareWrite(ushort address, byte data) => Edges.Add(new(Signal.PrepareWrite, address, data));
    }
}
