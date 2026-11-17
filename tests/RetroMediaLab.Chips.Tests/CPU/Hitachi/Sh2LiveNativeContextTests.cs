using System.Buffers.Binary;
using System.Runtime.InteropServices;
using RetroMediaLab.Chips.CPU.Hitachi.SH2;
using RetroMediaLab.Chips.CPU.Hitachi.SH2.Compilation;
using RetroMediaLab.Chips.CPU.Hitachi.SH2.Contracts;

namespace RetroMediaLab.Chips.Tests.CPU.Hitachi;

public sealed class Sh2LiveNativeContextTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InstructionFamiliesUseClrFieldLayoutWithoutChangingSnapshots(bool specialized)
    {
        if (!OperatingSystem.IsWindows() || RuntimeInformation.ProcessArchitecture != Architecture.X64) return;
        var buses = new[] { new Bus(), new Bus(), new Bus() };
        var cpus = buses.Select(b => specialized ? Sh2<Bus>.CreateNative(b) : new Sh2<Bus>(b)).ToArray();
        try
        {
            Select(cpus);
            ushort[] fieldInstructions = [0x0008,0x0018,0x0019,0x0028,0x000b,0x002b,0x001b,0xc300,
                0x400e,0x401e,0x402e,0x400a,0x401a,0x402a,0x4007,0x4017,0x4027,0x4006,0x4016,0x4026,
                0x0002,0x0012,0x0022,0x000a,0x001a,0x002a,0x000f,0x400f,0x3004,0x2007,
                0x6100,0x6101,0x6102,0x2100,0x2101,0x2102,0x8bfe,0x89fe,0xaffe,0xbffe,0x402b,0x400b];
            var random = new Random(170011);
            var instructions = fieldInstructions.Concat(Enumerable.Range(0,1024).Select(_=>(ushort)random.Next(65536)));
            Span<uint> state=stackalloc uint[Sh2X64Context.Words];
            foreach (ushort opcode in instructions)
            {
                for (int i=0;i<cpus.Length;i++)
                {
                    Array.Clear(buses[i].Memory);
                    BinaryPrimitives.WriteUInt16BigEndian(buses[i].Memory.AsSpan(0x1000),opcode);
                    BinaryPrimitives.WriteUInt16BigEndian(buses[i].Memory.AsSpan(0x1002),0x0009);
                    cpus[i].Reset(); cpus[i].AccelerateIdleLoops=false;
                    cpus[i].CaptureX64State(state);
                    state[Sh2X64Context.Status]=0x3f3; state[Sh2X64Context.GlobalBase]=0x2400;
                    state[Sh2X64Context.VectorBase]=0x400;state[Sh2X64Context.Procedure]=0x1080;
                    state[Sh2X64Context.MacLow]=0x89abcdef;state[Sh2X64Context.MacHigh]=0xfedcba98;
                    state[Sh2X64Context.Pc]=0x1004;state[Sh2X64Context.PreviousPc]=0x1002;
                    state[Sh2X64Context.CyclesLow]=0xfffffff0;state[Sh2X64Context.CyclesHigh]=1;
                    cpus[i].RestoreX64State(state);
                    for(int r=0;r<16;r++)cpus[i].R[r]=(uint)(0x8000+r*16);
                }
                int expected=cpus[0].Execute(1);
                Assert.Equal(expected,cpus[1].Execute(1));Assert.Equal(expected,cpus[2].Execute(1));
                Equal(cpus,buses);
            }
        }
        finally { foreach(var cpu in cpus)cpu.ClearCodeCache(); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CallbacksSeeAndChangeLiveCpuFieldsAcrossGcFaultsAndInterrupts(bool fault)
    {
        if (!OperatingSystem.IsWindows() || RuntimeInformation.ProcessArchitecture != Architecture.X64) return;
        var buses=new[]{new Bus(),new Bus(),new Bus()};
        var cpus=buses.Select(b=>Sh2<Bus>.CreateNative(b)).ToArray();
        try
        {
            Select(cpus);
            for(int i=0;i<cpus.Length;i++)
            {
                var cpu=cpus[i];var bus=buses[i];
                ushort[] words=[0x6102,0x7001,0xaffe,0x0009];
                for(int j=0;j<words.Length;j++)BinaryPrimitives.WriteUInt16BigEndian(bus.Memory.AsSpan(0x1000+j*2),words[j]);
                cpu.Reset();cpu.ProgramCounter=0x1000;cpu.R[0]=0x2000;cpu.R[15]=0x9000;
                bool called=false;
                bus.OnRead=()=>
                {
                    if(called)return;called=true;
                    Assert.Equal(0x1000u,cpu.ProgramCounter);
                    Assert.True(cpu.Cycles>0);
                    cpu.StatusRegister=0x20;cpu.GlobalBase=0x3300;cpu.VectorBase=0x400;
                    cpu.ProcedureRegister=0x12345678;cpu.R[2]=0x98765432;
                    cpu.SetInterrupt(3,16);cpu.ClearCodeCache();
                    GC.Collect(GC.MaxGeneration,GCCollectionMode.Forced,true,true);
                    if(fault)throw new InvalidOperationException("live fault");
                };
            }
            if(fault)
            {
                foreach(var cpu in cpus)Assert.Throws<InvalidOperationException>(()=>cpu.Execute(17));
                Equal(cpus,buses);foreach(var bus in buses)bus.OnRead=null;
            }
            foreach(int budget in new[]{1,2,3,17,64,127,511})
            {
                int cycles=cpus[0].Execute(budget);
                Assert.Equal(cycles,cpus[1].Execute(budget));Assert.Equal(cycles,cpus[2].Execute(budget));
                Equal(cpus,buses);
            }
            // Selecting the original context clears derived blocks and leaves
            // the actual CPU registers/clock intact for subsequent execution.
            cpus[2].UseLiveX64Context=false;
            Assert.Equal(cpus[0].Execute(128),cpus[1].Execute(128));cpus[2].Execute(128);Equal(cpus,buses);
        }
        finally { foreach(var bus in buses)bus.OnRead=null;foreach(var cpu in cpus)cpu.ClearCodeCache(); }
    }

    private static void Select(Sh2<Bus>[] cpus)
    {
        cpus[1].UseCompiledBlocks=cpus[1].UseFullX64Blocks=cpus[1].UseMultiInstructionX64Blocks=true;
        cpus[2].UseCompiledBlocks=cpus[2].UseFullX64Blocks=cpus[2].UseMultiInstructionX64Blocks=true;
        cpus[2].UseLiveX64Context=true;
    }
    private static void Equal(Sh2<Bus>[] cpus,Bus[] buses)
    {
        uint[] expected=new uint[Sh2X64Context.Words];cpus[0].CaptureX64State(expected);
        for(int i=1;i<cpus.Length;i++)
        {
            uint[] actual=new uint[Sh2X64Context.Words];cpus[i].CaptureX64State(actual);
            Assert.Equal(expected,actual);Assert.Equal(cpus[0].R,cpus[i].R);Assert.Equal(buses[0].Memory,buses[i].Memory);
        }
    }
    private sealed class Bus:ISh2Bus
    {
        internal readonly byte[] Memory=new byte[65536];internal Action? OnRead;
        public byte Read8(uint a){OnRead?.Invoke();return Memory[a&65535];}
        public ushort Read16(uint a){OnRead?.Invoke();return (ushort)((Memory[a&65535]<<8)|Memory[(a+1)&65535]);}
        public uint Read32(uint a){OnRead?.Invoke();return (uint)Memory[a&65535]<<24|(uint)Memory[(a+1)&65535]<<16|(uint)Memory[(a+2)&65535]<<8|Memory[(a+3)&65535];}
        public ushort Fetch16(uint a)=>(ushort)((Memory[a&65535]<<8)|Memory[(a+1)&65535]);
        public void Write8(uint a,byte v)=>Memory[a&65535]=v;
        public void Write16(uint a,ushort v){Memory[a&65535]=(byte)(v>>8);Memory[(a+1)&65535]=(byte)v;}
        public void Write32(uint a,uint v){Write16(a,(ushort)(v>>16));Write16(a+2,(ushort)v);}
        public bool TryMapFetch(uint address,out ReadOnlyMemory<byte> memory,out uint start,out int waitStates)
        {memory=Memory;start=address&0xffff0000;waitStates=0;return true;}
        public long CodeVersion(uint address)=>0;
        public bool DeferClockUpdates=>true;
        public bool IsTimedAccess(uint a)=>false;
    }
}
