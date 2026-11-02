using RetroMediaLab.Chips.CPU.ARM.ARMv2.ARM2.Enums;
using RetroMediaLab.Chips.CPU.ARM.ARMv2.ARM2.Services;
namespace RetroMediaLab.Chips.Tests.CPU.ARM.ARMv2.ARM2;

public sealed partial class Arm26Tests
{
    [Theory]
    [InlineData(0u, 2)]
    [InlineData(4u, 3)]
    [InlineData(8u, 4)]
    [InlineData(12u, 4)]
    public void Arm3ResumesAtTheCriticalWordAndOverlapsTheLoadInternalCycle(uint offset, int reads)
    {
        var (cpu, bus) = CachedLoad(offset);
        cpu.Step();
        Assert.Equal(offset / 4 + 1, cpu.GetRegister(3));
        Assert.Equal(Enumerable.Range(0, reads).Select(word => 256u + (uint)word * 4),
            DataReads(bus));
        cpu.Step();
        Assert.Equal(new uint[] { 256, 260, 264, 268 }, DataReads(bus));
    }

    [Fact]
    public void Arm3FirstWordAbortStopsTheBurstAndAbortsTheRequestedLoad()
    {
        var (cpu, bus) = CachedLoad(8);
        bus.AbortRead = 256;
        cpu.Step();
        Assert.Equal(new uint[] { 256 }, DataReads(bus));
        Assert.Equal(0u, cpu.GetRegister(3));
        Assert.Equal(16u, cpu.PC);
    }

    [Fact]
    public void Arm3IgnoresAbortAfterTheFirstWordOfAnAcceptedLine()
    {
        var (cpu, bus) = CachedLoad(0, 0xe5924004);
        bus.AbortRead = 260;
        cpu.Step();
        Assert.Equal(1u, cpu.GetRegister(3));
        cpu.Step();
        Assert.Equal(2u, cpu.GetRegister(4));
        Assert.Equal(new uint[] { 256, 260, 264, 268 }, DataReads(bus));
    }

    [Fact]
    public void Arm3ResetCancelsTheOutstandingBurstWithoutReadingItsTail()
    {
        var (cpu, bus) = CachedLoad(0);
        cpu.Step();
        Assert.Equal(new uint[] { 256, 260 }, DataReads(bus));
        bus.Accesses.Clear();
        cpu.Reset();
        Assert.Empty(DataReads(bus));
        bus.Words[256] = 77;
        Run(cpu, 6);
        Assert.Equal(77u, cpu.GetRegister(3));
    }

    [Fact]
    public void Arm3FinishesTheInstructionLineBeforeIssuingAStore()
    {
        var (cpu, bus) = CachedLoad(0, 0xe3a0304d, 0xe5823000);
        Run(cpu, 2);
        bus.BeforeWrite = address =>
        {
            Assert.Equal(256u, address);
            Assert.Equal(new uint[] { 32, 36, 40, 44 }, bus.Accesses
                .Where(access => access.Instruction && access.Address >= 32 && access.Address < 48)
                .Select(access => access.Address));
        };
        cpu.Step();
        Assert.Equal(77u, bus.Words[256]);
    }

    [Fact]
    public void Arm3FlushCannotPublishTheOldPendingLineAfterInvalidation()
    {
        var (cpu, bus) = CachedLoad(0, 0xe1a00000, 0xee011f10, 0xe5924000);
        cpu.Step();
        bus.Words[256] = 91;
        Run(cpu, 3);
        Assert.Equal(91u, cpu.GetRegister(4));
        Assert.Equal(2, DataReads(bus).Count(address => address == 256));
    }

    private static uint[] DataReads(ArmBus bus) => bus.Accesses
        .Where(access => !access.Instruction && access.Address >= 256 && access.Address < 272)
        .Select(access => access.Address).ToArray();

    private static (CpuArm26 Cpu, ArmBus Bus) CachedLoad(uint offset, params uint[] following)
    {
        var instructions = new uint[] { 0xe3a01001, 0xee031f10, 0xe3a01003,
            0xee021f10, 0xe3a02c01, 0xe5923000u | offset }
            .Concat(following).Concat(Enumerable.Repeat(0xe1a00000u, 8)).ToArray();
        var (cpu, bus) = Machine(Arm26Model.Arm3, instructions);
        for (var word = 0; word < 4; word++) bus.Words[256u + (uint)word * 4] = (uint)word + 1;
        Run(cpu, 5);
        bus.Accesses.Clear();
        return (cpu, bus);
    }
}
