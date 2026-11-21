using System.Buffers.Binary;
using System.Reflection;
using RetroMediaLab.Chips.CPU.Motorola.M68000.Execution;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Boards;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Buses;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Cartridge;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Factories;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.ThirtyTwoX.Hardware;

namespace RetroMediaLab.Tests.Emulation.Sega.SegaEmu;

[CollectionDefinition("Sega code windows", DisableParallelization = true)]
public sealed class SegaCodeWindowsCollection { }

[Collection("Sega code windows")]
public sealed class Mega32XCodeWindowTests
{
    [Fact]
    public void OptionalFetchViewKeepsDataHandlersAndInvalidatesOnSelection()
    {
        var banks=Enumerable.Range(0,256).Select(_=>new CartridgeMemoryBank()).ToArray();
        byte[] data=new byte[65536];BinaryPrimitives.WriteUInt16LittleEndian(data,0x1234);
        var bank=banks[0];bank.Data=data;bank.MappedReadsUseHandlers=true;bank.read8=_=>0xaa;bank.read16=_=>0xbbbb;
        var bus=new TestBus(banks);Assert.False(bus.TryGetCodeMemory(0,out _,out _));
        long prior=bus.CodeMemoryVersion;
        CartridgeCodeWindow view=(uint a,out ReadOnlyMemory<byte> m,out uint start)=>{m=data;start=0;return true;};
        bank.CodeWindow=view;Assert.True(bus.CodeMemoryVersion>prior);
        Assert.Equal(0xaau,bus.Read8(0));Assert.Equal(0xbbbbu,bus.Read16(0));
        Assert.Equal(0x12u,bus.ReadMapped8(0));Assert.Equal(0x34u,bus.ReadMapped8(1));Assert.Equal(0x1234u,bus.ReadMapped16(0));
        Assert.True(bus.TryGetCodeMemory(0,out var memory,out uint logical));Assert.Equal(0u,logical);Assert.True(memory.Span.SequenceEqual(data));
        prior=bus.CodeMemoryVersion;bank.CodeWindow=view;Assert.Equal(prior,bus.CodeMemoryVersion);
        bank.CodeWindow=null;Assert.True(bus.CodeMemoryVersion>prior);Assert.Equal(0xbbbbu,bus.ReadMapped16(0));
    }

    [Theory]
    [InlineData(0x8000)]
    [InlineData(0x300000)]
    [InlineData(0x400000)]
    public void RomViewsMatchDataAcrossBanksMirrorsAndBoundaries(int length)
    {
        SegaBoard? board=null;
        try
        {
            byte[] rom=Mega32XIntegrationTests.Program();Array.Resize(ref rom,length);
            for(int i=0x1000;i<rom.Length;i++)rom[i]=(byte)(i^(i>>16));
            board=BoardFactory.Create("ThirtyTwoX",MachineIntegrationTests.Resources());Assert.True(board.Load("mapped.32x",rom));
            var bus=Bus(board);var code=(ICpu68000CodeBus)bus;
            Assert.False(code.TryGetCodeMemory(0x881000,out _,out _));
            board.UseMapped32XRomFetch=true;Assert.False(code.TryGetCodeMemory(0x881000,out _,out _));bus.Write16(0xa15100,3);
            foreach(uint address in new uint[]{0,0xfe,0x840000,0x87fffe,0xa15120,0xa15180})Assert.False(code.TryGetCodeMemory(address,out _,out _));
            Assert.True(code.TryGetCodeMemory(0x200,out _,out _));
            for(int bank=0;bank<4;bank++)
            {
                bus.Write16(0xa15104,(uint)bank);
                foreach(uint address in new uint[]{0x881000,0x887ffe,0x88fffe,0x890000,0x8ffffe,0x901000,0x90fffe,0x9ffffe})
                {
                    Assert.True(code.TryGetCodeMemory(address,out var window,out uint start));
                    Assert.InRange(window.Length,2,65536);Assert.True(address>=start && address-start+2<=window.Length);
                    ushort word=BinaryPrimitives.ReadUInt16LittleEndian(window.Span[(int)(address-start)..]);
                    Assert.Equal(bus.Read16(address),(uint)word);Assert.Equal((uint)word,bus.ReadMapped16(address));
                    Assert.Equal(bus.Read8(address),bus.ReadMapped8(address));Assert.Equal(bus.Read8(address+1),bus.ReadMapped8(address+1));
                }
            }
            board.UseMapped32XRomFetch=false;Assert.False(code.TryGetCodeMemory(0x881000,out _,out _));
        }
        finally{board?.Dispose();}
    }

    [Fact]
    public void BankActivationAndRestorationInvalidateBorrowedWindows()
    {
        SegaBoard? board=null;
        try
        {
            byte[] rom=Mega32XIntegrationTests.Program();Array.Resize(ref rom,0x400000);
            for(int bank=0;bank<4;bank++)BinaryPrimitives.WriteUInt16BigEndian(rom.AsSpan(bank*0x100000+0x1000),(ushort)(0xa000+bank));
            board=BoardFactory.Create("ThirtyTwoX",MachineIntegrationTests.Resources());Assert.True(board.Load("mapping.32x",rom));
            board.UseMapped32XRomFetch=true;var bus=Bus(board);var versions=(ICpu68000VersionedCodeBus)bus;
            long prior=versions.CodeMemoryVersion;bus.Write16(0xa15100,3);Assert.True(versions.CodeMemoryVersion>prior);
            prior=versions.CodeMemoryVersion;bus.Write16(0xa15104,1);Assert.True(versions.CodeMemoryVersion>prior);Assert.Equal(0xa001u,bus.ReadMapped16(0x901000));
            byte[] state=new byte[Mega32X.ContextSize];board.Adapter32X!.SaveContext(state);
            bus.Write16(0xa15104,3);Assert.Equal(0xa003u,bus.ReadMapped16(0x901000));prior=versions.CodeMemoryVersion;
            board.Adapter32X.LoadContext(state);Assert.True(versions.CodeMemoryVersion>prior);Assert.Equal(0xa001u,bus.ReadMapped16(0x901000));
            prior=versions.CodeMemoryVersion;board.Adapter32X.Reset();Assert.True(versions.CodeMemoryVersion>prior);Assert.False(((ICpu68000CodeBus)bus).TryGetCodeMemory(0x901000,out _,out _));
        }
        finally{board?.Dispose();}
    }

    [Fact]
    public void RomFetchDefersSynchronizationWhileDataReadStillAdvancesTheAdapter()
    {
        SegaBoard? board=null;
        try
        {
            board=BoardFactory.Create("ThirtyTwoX",MachineIntegrationTests.Resources());Assert.True(board.Load("sync.32x",Mega32XIntegrationTests.Program()));
            board.SynchronizeMapped32XRomFetch=false;
            board.UseMapped32XRomFetch=true;var bus=Bus(board);bus.Write16(0xa15100,3);
            object cpu=Cpu(board);var state=Field(cpu,"_state")!;int cycles=(int)state.GetType().GetField("cycles")!.GetValue(state)!;
            cpu.GetType().GetMethod("Run")!.Invoke(cpu,[(uint)(cycles+2000)]);
            ulong before=(ulong)Field(board.Adapter32X!,"_lastMaster")!;
            bus.ReadMapped16(0x881000);Assert.Equal(before,(ulong)Field(board.Adapter32X!,"_lastMaster")!);
            bus.Read16(0x881000);Assert.True((ulong)Field(board.Adapter32X!,"_lastMaster")!>before);
        }
        finally{board?.Dispose();}
    }

    [Fact]
    public void NativeBankSwitchLeavesTheOldRomBlockBeforeExecutingItsTail()
    {
        SegaBoard? board=null;
        try
        {
            byte[] rom=Mega32XIntegrationTests.Program();Array.Resize(ref rom,0x400000);
            ushort[] words=[0x33fc,0x0001,0x00a1,0x5104,0x7011,0x4e71,0x60fe];
            for(int i=0;i<words.Length;i++)BinaryPrimitives.WriteUInt16BigEndian(rom.AsSpan(0x1000+i*2),words[i]);
            BinaryPrimitives.WriteUInt16BigEndian(rom.AsSpan(0x101008),0x4e71);BinaryPrimitives.WriteUInt16BigEndian(rom.AsSpan(0x10100a),0x702a);BinaryPrimitives.WriteUInt16BigEndian(rom.AsSpan(0x10100c),0x60fe);
            board=BoardFactory.Create("ThirtyTwoX",MachineIntegrationTests.Resources());Assert.True(board.Load("switch.32x",rom));
            board.UseMapped32XRomFetch=true;Bus(board).Write16(0xa15100,3);object cpu=Cpu(board);
            cpu.GetType().GetMethod("SetRegister")!.Invoke(cpu,[Cpu68000Register.PC,0x901000u]);
            var state=Field(cpu,"_state")!;int cycles=(int)state.GetType().GetField("cycles")!.GetValue(state)!;
            cpu.GetType().GetMethod("RunNative")!.Invoke(cpu,[(uint)(cycles+3000)]);
            Assert.Equal(42u,(uint)cpu.GetType().GetMethod("GetRegister")!.Invoke(cpu,[Cpu68000Register.D0])!);
            Assert.True((long)cpu.GetType().GetProperty("NativeBlockEntryCount")!.GetValue(cpu)!>0);
        }
        finally{board?.Dispose();}
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void ObservedNativeFetchKeepsInterpreterClockTransitions(bool registers, bool extensions)
    {
        var boards=new List<SegaBoard>();
        var clocks=new[]{new List<int>(),new List<int>()};
        try
        {
            byte[] rom=Mega32XIntegrationTests.Program();
            ushort[] words=extensions?[0x4e71,0x5240,0x323c,0x1234,0x60f6]:[0x4e71,0x5240,0x60fa];
            for(int i=0;i<words.Length;i++)BinaryPrimitives.WriteUInt16BigEndian(rom.AsSpan(0x1000+i*2),words[i]);
            for(int route=0;route<2;route++)
            {
                int index=route;
                var board=BoardFactory.Create("ThirtyTwoX",MachineIntegrationTests.Resources());boards.Add(board);
                Assert.True(board.Load("observed.32x",rom));Bus(board).Write16(0xa15100,3);
                var cpu=(Cpu68000<MegaDriveCpuBus>)Cpu(board);cpu.UseNativeGuestRegisters=registers;cpu.SetRegister(Cpu68000Register.PC,0x881000);
                var bank=((CartridgeAddressSpace)Field(board,"_addresses")!).Main[0x88];
                if(route==0)
                {
                    var original=bank.read16!;
                    bank.read16=a=>{RecordClock();return original(a);};
                }
                else
                {
                    board.UseMapped32XRomFetch=true;
                    var observer=bank.CodeFetchObserver!;
                    bank.CodeFetchObserver=a=>{RecordClock();observer(a);};
                    var versioned=(ICpu68000VersionedCodeBus)Bus(board);
                    long prior=versioned.CodeMemoryVersion;
                    board.SynchronizeMapped32XRomFetch=false;Assert.True(versioned.CodeMemoryVersion>prior);
                    Assert.False(((ICpu68000ObservedCodeBus)Bus(board)).RequiresCodeFetchObservation(0x881000));
                    board.SynchronizeMapped32XRomFetch=true;
                    Assert.True(((ICpu68000ObservedCodeBus)Bus(board)).RequiresCodeFetchObservation(0x881000));
                    // The selection restores its owned observer; wrap it again for this trace.
                    observer=bank.CodeFetchObserver!;
                    bank.CodeFetchObserver=a=>{RecordClock();observer(a);};
                }
                void RecordClock()
                {
                    int now=(int)Field(Field(cpu,"_state")!,"cycles")!;
                    if(clocks[index].Count==0 || clocks[index][^1]!=now)clocks[index].Add(now);
                }
            }
            for(int route=0;route<2;route++)
            {
                var cpu=(Cpu68000<MegaDriveCpuBus>)Cpu(boards[route]);
                uint target=unchecked((uint)(int)Field(Field(cpu,"_state")!,"cycles")!)+4000;
                if(route==0)cpu.Run(target);else cpu.RunNative(target);
            }
            Assert.Equal(clocks[0],clocks[1]);
            var reference=(Cpu68000<MegaDriveCpuBus>)Cpu(boards[0]);var native=(Cpu68000<MegaDriveCpuBus>)Cpu(boards[1]);
            foreach(var register in new[]{Cpu68000Register.PC,Cpu68000Register.D0,Cpu68000Register.D1,Cpu68000Register.SR})
                Assert.Equal(reference.GetRegister(register),native.GetRegister(register));
            Assert.Equal(Field(Field(reference,"_state")!,"cycles"),Field(Field(native,"_state")!,"cycles"));
            Assert.True(native.NativeBlockEntryCount>0);
        }
        finally{for(int i=boards.Count-1;i>=0;i--)boards[i].Dispose();}
    }
    private static object Cpu(SegaBoard b)=>Field(b,"_mainCpuComponent")!;
    private static ICpu68000Bus Bus(SegaBoard b)=>(ICpu68000Bus)Field(Cpu(b),"_bus")!;
    private static object? Field(object o,string name)
    {for(Type? t=o.GetType();t!=null;t=t.BaseType)if(t.GetField(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly)is{}f)return f.GetValue(o);return null;}
    private sealed class TestBus(CartridgeMemoryBank[] banks):MappedCpuBus(banks)
    {public override void AcknowledgeInterrupt(uint level){} }
}

