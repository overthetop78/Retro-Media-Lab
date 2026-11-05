using System.Buffers.Binary;
using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Enums;
using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Interfaces;
using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;
using B = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000BufferConstants;

namespace RetroMediaLab.Chips.Tests.Controllers.Adaptec.ACB4000.ACB4000;

public sealed partial class Acb4000CommandTests
{
    [Fact]
    public void NativeDmaWriteStoresTheResolvedBusByteWhenBoeIsManuallyForced()
    {
        var target = new Acb4000(_ => null, 2_000_000);
        Assert.Equal(0, Run(target, DiagnosticCdb(0x1d, 5), [0x63, 0x30, B.HostData, 1, 0xa5]).Status);
        SendCommand(target, Short(0x13));
        WaitRequest(target);
        target.BufferController.TryWrite(B.HostInterfaceControl, B.HostOutputEnable);
        target.Tick();
        target.SetInitiator(0x30, false, true, false);
        WaitRequestCleared(target);
        target.SetInitiator(0x30, false, false, false);
        target.Tick();
        target.Reset();
        var result = Run(target, Short(0x14));
        Assert.Equal(0, result.Status);
        Assert.Equal(0xb5, result.Data[0]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NativeDmaBoeFollowsAcknowledgeImmediatelyUnlessManuallyForced(bool forced)
    {
        var target = new Acb4000(_ => null, 2_000_000);
        var contents = Enumerable.Repeat((byte)0xa5, 1024).ToArray();
        Assert.Equal(0, Run(target, Short(0x13), contents).Status);
        SendCommand(target, Short(0x14));
        WaitRequest(target);
        Assert.Equal(0xa5, target.Bus.OutputData);
        if (forced) target.BufferController.TryWrite(B.HostInterfaceControl, B.HostOutputEnable);
        target.SetInitiator(0, false, true, false);
        Assert.False(target.Bus.Request);
        Assert.Equal(forced, target.BufferController.HostOutputEnabled);
        Assert.Equal(forced ? 0xa5 : 0, target.Bus.OutputData);
        target.SetInitiator(0, false, false, false);
        Assert.Equal(forced, target.BufferController.HostOutputEnabled);
        Assert.Equal(forced ? 0xa5 : 0, target.Bus.OutputData);
        target.Reset();
        Assert.False(target.Bus.Busy);
        Assert.False(target.BufferController.HostOutputEnabled);
        Assert.Equal(contents, Run(target, Short(0x14)).Data);
    }

    [Fact]
    public void NativeHostOutputLatchIsWrittenThroughPatchAndSurvivesAsicReset()
    {
        var target = new Acb4000(_ => null, 2_000_000);
        Assert.Equal(0, Run(target, DiagnosticCdb(0x1d, 5), [0x63, 0x30, B.HostData, 1, 0xa5]).Status);
        target.BufferController.TryWrite(B.HostInterfaceControl, B.HostOutputEnable);
        target.Tick();
        Assert.Equal(0xa5, target.Bus.OutputData);
        target.Reset();
        Assert.False(target.BufferController.HostOutputEnabled);
        target.BufferController.TryWrite(B.HostInterfaceControl, B.HostOutputEnable);
        target.Tick();
        Assert.Equal(0xa5, target.Bus.OutputData);
        target.BufferController.TryWrite(B.HostInterfaceControl, 0);
        target.Tick();
        var contents = Enumerable.Repeat((byte)0x6c, 1024).ToArray();
        Assert.Equal(0, Run(target, Short(0x13), contents).Status);
        Assert.Equal(contents, Run(target, Short(0x14)).Data);
        target.BufferController.TryWrite(B.HostInterfaceControl, B.HostOutputEnable);
        target.Tick();
        Assert.Equal(0x6c, target.Bus.OutputData);
    }

    [Fact]
    public void NativeArbitrationWaitsForTheDiagnosticConnectionToReleaseTheBus()
    {
        var target = new Acb4000(_ => null, 2_000_000);
        Assert.Equal(0, Run(target, DiagnosticCdb(0x1d, 5), [0x63, 0x30, B.HostInterfaceControl, 1, B.HostArbitrate]).Status);
        Assert.True(target.BufferController.HostArbitrationPending);
        Assert.False(target.IsIdle);
        target.Tick();
        Assert.False(target.BufferController.HostArbitrationPending);
        Assert.True(target.Bus.Busy);
        Assert.True(target.BufferController.HostOutputEnabled);
        target.BufferController.TryRead(B.HostInterfaceControl, out var control);
        Assert.Equal(B.HostBusyInput | B.HostBusyOutput | B.HostOutputEnable | B.HostArbitrate, control);
        target.Reset();
        Assert.False(target.Bus.Busy);
    }

    [Fact]
    public void NativeHostDataReadUsesTheLiveBusOnlyWhenTheReceiverIsEnabled()
    {
        var target = new Acb4000(_ => null, 2_000_000);
        Assert.Equal(0, Run(target, DiagnosticCdb(0x1d, 9), [0x64, 0x30, 0x98, 5, B.HostData, 0x40, 0, 0, 4]).Status);
        var floating = Run(target, DiagnosticCdb(0x1c, 5));
        Assert.Equal(0, floating.Status);
        Assert.Equal(4, floating.Data[4]); // Last driven diagnostic RAM write, not a host receiver value.
        Assert.Equal(0, Run(target, DiagnosticCdb(0x1d, 5), [0x63, 0x30, B.HostInterfaceControl, 1, B.HostInputEnable]).Status);
        // While the diagnostic samples the bus, it still drives the final header byte (address low).
        Assert.Equal(new byte[] { B.HostData }, DumpNativeRegisters(target, B.HostData, 1));
        // No upper-byte latch is fitted to the eight-bit board.
        Assert.Equal(0, Run(target, DiagnosticCdb(0x1d, 5), [0x63, 0x30, 0x51, 1, 0xa5]).Status);
    }

    [Fact]
    public void NativeManualSelectAndBusyArePhysicalPinsWithoutACommandPhase()
    {
        var target = new Acb4000(_ => null, 2_000_000);
        target.BufferController.TryWrite(B.HostInterfaceControl, B.HostSelectOutput | B.HostBusyOutput);
        target.Tick();
        Assert.True(target.Bus.Select);
        Assert.True(target.Bus.Busy);
        Assert.False(target.Bus.Request);
        Assert.Equal(Acb4000BusPhase.BusFree, target.Bus.Phase);
        target.BufferController.TryRead(B.HostInterfaceControl, out var control);
        Assert.Equal(B.HostSelectInput | B.HostBusyInput | B.HostSelectOutput | B.HostBusyOutput | B.HostArbitrate, control);
        target.Reset();
        Assert.False(target.Bus.Select);
        Assert.False(target.Bus.Busy);
    }

    [Fact]
    public void ResetBeforeHostRamCompletionDoesNotAcknowledgeOrStoreThePendingByte()
    {
        var target = new Acb4000(_ => null, 2_000_000);
        var contents = Enumerable.Repeat((byte)0x6c, 1024).ToArray();
        Assert.Equal(0, Run(target, Short(0x13), contents).Status);
        SendCommand(target, Short(0x13));
        WaitRequest(target);
        target.SetInitiator(0xa5, false, true, false);
        Assert.True(target.Bus.Request);
        Assert.True(target.BufferController.HostCyclePending);
        Assert.False(target.BufferController.HostTransferPending);
        target.Reset();
        Assert.False(target.BufferController.HostCyclePending);
        var result = Run(target, Short(0x14));
        Assert.Equal(0, result.Status);
        Assert.Equal(contents, result.Data);
    }

    [Theory]
    [InlineData(0x6e, false, 1)]
    [InlineData(0x6f, false, 3)]
    [InlineData(0x6e, true, 0)]
    [InlineData(0x6f, true, 2)]
    public void NativeDiagnosticAccessPulsesTheSelectedExternalPin(byte register, bool writing, int pin)
    {
        var memory = new DiagnosticRegisters();
        var target = new Acb4000(_ => null, 2_000_000, registerBus: memory);
        Assert.Equal(0, Run(target, DiagnosticCdb(0x1d, 5), [0x63, 0x30, 0x7d, 1, 0xff]).Status);
        var observed = 0;
        memory.Access = (address, write) =>
        {
            if (address != 0x4000 + register || write != writing) return;
            observed++;
            Assert.Equal(0x0f, target.Aic.PinOutputEnable);
            Assert.Equal(0x0f ^ (1 << pin), target.Aic.PinOutputLevels);
        };
        if (writing)
            Assert.Equal(0, Run(target, DiagnosticCdb(0x1d, 5), [0x63, 0x30, register, 1, 0xa9]).Status);
        else Assert.Equal(memory.Bytes[0x4000 + register], Assert.Single(DumpNativeRegisters(target, register, 1)));
        Assert.Equal(1, observed);
        Assert.Equal(0x0f, target.Aic.PinOutputLevels);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExternalAccessFailureDoesNotLeaveTheDecodePinAsserted(bool write)
    {
        var memory = new DiagnosticRegisters();
        var target = new Acb4000(_ => null, 2_000_000, registerBus: memory);
        Assert.Equal(0, Run(target, DiagnosticCdb(0x1d, 5), [0x63, 0x30, 0x7d, 1, 0xff]).Status);
        memory.Access = (address, writing) =>
        {
            if (address == 0x406e && writing == write) throw new InvalidOperationException("Peripheral access failure");
        };
        try
        {
            if (write)
                Assert.Throws<InvalidOperationException>(() => Run(target, DiagnosticCdb(0x1d, 5), [0x63, 0x30, 0x6e, 1, 0xa9]));
            else Assert.Throws<InvalidOperationException>(() => DumpNativeRegisters(target, 0x6e, 1));
            Assert.Equal(0x0f, target.Aic.PinOutputLevels);
        }
        finally { target.Reset(); }
    }

    [Theory]
    [InlineData(256)]
    [InlineData(512)]
    [InlineData(1024)]
    public void PhysicalWriteReceivesItsBlockThroughTheNativeHostDma(ushort size)
    {
        var disk = new Disk(size);
        var target = CreateTarget(_ => disk, 2_000_000);
        var payload = Enumerable.Range(0, size).Select(index => (byte)(index ^ 0x5a)).ToArray();
        SendCommand(target, Short(0x0a, count: 1));
        Assert.Equal(size - 1, target.BufferController.StopPointer);
        for (var index = 0; index < payload.Length; index++)
        {
            WaitRequest(target);
            Assert.True(target.BufferController.HostTransferEnabled);
            target.SetInitiator(payload[index], false, true, false);
            Assert.True(target.Bus.Request);
            WaitRequestCleared(target);
            Assert.True(target.BufferController.HostTransferPending);
            Assert.Equal((index + 1) % 1024, target.BufferController.WritePointer);
            target.BufferController.TryRead(0x53, out var beforeAck);
            Assert.Equal(0, beforeAck & 0x20);
            target.SetInitiator(payload[index], false, false, false);
            Tick(target);
            Assert.False(target.BufferController.HostTransferPending);
        }
        target.BufferController.TryRead(0x53, out var done);
        Assert.Equal(0x20, done & 0x20);
        Assert.False(target.BufferController.HostTransferEnabled);
        WaitRequest(target);
        Assert.Equal(0, target.Bus.OutputData);
        Transfer(target, 0);
        WaitRequest(target);
        Transfer(target, 0);
        Assert.False(target.Bus.Busy);
        Assert.Equal(payload, disk.Bytes.AsSpan(0, size).ToArray());
    }

    [Fact]
    public void ResetDuringPhysicalWriteReceptionKeepsOnlyTheAcknowledgedBufferBytes()
    {
        var disk = new Disk();
        var original = disk.Bytes.ToArray();
        var target = CreateTarget(_ => disk, 2_000_000);
        SendCommand(target, Short(0x0a, count: 1));
        WaitRequest(target);
        Transfer(target, 0x83);
        WaitRequest(target);
        target.SetInitiator(0xa9, false, true, false);
        WaitRequestCleared(target);
        Assert.True(target.BufferController.HostTransferPending);
        target.Reset();
        Assert.False(target.BufferController.HostTransferPending);
        Assert.Equal(original, disk.Bytes);
        Assert.Equal(0, disk.Writes);
        var result = Run(target, Short(0x14));
        Assert.Equal(0, result.Status);
        Assert.Equal(new byte[] { 0x83, 0xa9 }, result.Data.AsSpan(0, 2).ToArray());
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(false, 1)]
    [InlineData(false, 4)]
    [InlineData(false, 5)]
    [InlineData(false, 259)]
    [InlineData(false, 260)]
    [InlineData(false, 1024)]
    [InlineData(true, 0)]
    [InlineData(true, 1)]
    [InlineData(true, 4)]
    [InlineData(true, 5)]
    [InlineData(true, 259)]
    [InlineData(true, 260)]
    [InlineData(true, 1024)]
    public void MemoryDumpReadsOnlyTheAllocatedPrefixFromTheSelectedAddressSpace(bool ram, int allocation)
    {
        var memory = new DiagnosticRegisters();
        var target = new Acb4000(_ => null, 2_000_000, registerBus: memory);
        if (ram)
        {
            memory.Bytes[0x80af] = 0; // Keep unit 0 selected while seeding the diagnostic fixture.
            var seed = new byte[] { 0x64, 0x30, 0, 0 }.Concat(memory.Bytes.Skip(0x8000).Take(256)).ToArray();
            Assert.Equal(0, Run(target, DiagnosticCdb(0x1d, seed.Length), seed).Status);
        }
        Assert.Equal(0, Run(target, DiagnosticCdb(0x1d, 4), [(byte)(ram ? 0x62 : 0x61), 0x30, 0, 0]).Status);
        Assert.Empty(memory.Reads);
        var result = Run(target, DiagnosticCdb(0x1c, allocation));
        var address = ram ? 0x8000 : 0x4000;
        var contents = memory.Bytes.Skip(address).Take(255).ToArray();
        if (ram)
        {
            contents[0x1c] = (byte)(contents[0x9c] | 4);
            contents[0x80] = 0x1c;
            contents[0x81] = contents[0x82] = 0;
            contents[0x83] = (byte)(allocation >> 8);
            // MOV A,M precedes the decrement of 8084/8085 for this outgoing byte.
            contents[0x84] = unchecked((byte)(allocation - 4 - 0x84));
            contents[0x85] = unchecked((byte)((allocation - 4 - 0x85) >> 8));
            contents[0x98] = 0;
            contents[0x99] = 0x80;
            contents[0x9c] &= 0xfb;
        }
        var expected = new byte[] { 1, 4, (byte)(address >> 8), 0 }
            .Concat(contents).Take(allocation).ToArray();
        Assert.Equal(0, result.Status);
        Assert.Equal(expected, result.Data);
        Assert.Equal(ram ? Array.Empty<ushort>() : Enumerable.Range(address, Math.Max(0, expected.Length - 4)).Select(value => (ushort)value), memory.Reads);
        Assert.Equal(2, Run(target, DiagnosticCdb(0x1c, allocation)).Status);
    }

    [Fact]
    public void HardwareReadsOccurAfterHeaderAcknowledgementsWithoutSpeculativeRegisterReads()
    {
        var memory = new DiagnosticRegisters();
        var target = new Acb4000(_ => null, 2_000_000, registerBus: memory);
        Run(target, DiagnosticCdb(0x1d, 4), [0x61, 0x30, 0, 0]);
        SendCommand(target, DiagnosticCdb(0x1c, 6));
        for (var header = 0; header < 4; header++)
        {
            WaitRequest(target);
            Assert.Empty(memory.Reads);
            Transfer(target, 0);
        }
        Assert.Equal(new ushort[] { 0x4000 }, memory.Reads);
        memory.Bytes[0x4001] = 0xe7;
        WaitRequest(target);
        Transfer(target, 0);
        Assert.Equal(new ushort[] { 0x4000, 0x4001 }, memory.Reads);
        WaitRequest(target);
        Assert.Equal(0xe7, target.Bus.OutputData);
        target.Reset();
        Assert.Equal(2, Run(target, DiagnosticCdb(0x1c, 6)).Status);
    }

    [Theory]
    [InlineData(false, 0xff, 4, 4)]
    [InlineData(true, 0xfe, 0, 256)]
    [InlineData(true, 0xfd, 8, 3)]
    [InlineData(false, 0x20, 8, 0)]
    public void PatchHonoursCountCdbLengthAndSixteenBitAddressIncrement(bool ram, byte start, byte count, int supplied)
    {
        var memory = new DiagnosticRegisters();
        var target = new Acb4000(_ => null, 2_000_000, registerBus: memory);
        var data = Enumerable.Range(0, supplied).Select(index => (byte)(index ^ 0xa6)).ToArray();
        var packet = new byte[] { (byte)(ram ? 0x64 : 0x63), 0x30, start, count }.Concat(data).ToArray();
        Assert.Equal(0, Run(target, DiagnosticCdb(0x1d, packet.Length), packet).Status);
        var address = (ram ? 0x8000 : 0x4000) + start;
        var owned = ram ? Math.Min(supplied, 0x8100 - address) : 0;
        Assert.Equal(Enumerable.Range(address + owned, supplied - owned).Select(value => (ushort)value), memory.Writes);
        Assert.Equal(data.AsSpan(owned).ToArray(), memory.Bytes.AsSpan(address + owned, supplied - owned).ToArray());
        Assert.Empty(memory.Reads);
        if (ram && owned > 0)
        {
            Run(target, DiagnosticCdb(0x1d, 4), [0x62, 0x30, 0, 0]);
            var dump = Run(target, DiagnosticCdb(0x1c, 259)).Data;
            Assert.Equal(data[0], dump[4 + start]);
        }
    }

    [Fact]
    public void ResetDuringPatchKeepsOnlyBytesAlreadyAcceptedOnTheBus()
    {
        var memory = new DiagnosticRegisters();
        var target = new Acb4000(_ => null, 2_000_000, registerBus: memory);
        var untouched = memory.Bytes[0x80ff];
        SendCommand(target, DiagnosticCdb(0x1d, 8));
        foreach (var value in new byte[] { 0x64, 0x30, 0xfe, 4, 0xaa })
        {
            WaitRequest(target);
            Transfer(target, value);
        }
        target.Reset();
        Assert.Empty(memory.Writes);
        Run(target, DiagnosticCdb(0x1d, 4), [0x62, 0x30, 0, 0]);
        Assert.Equal(0xaa, Run(target, DiagnosticCdb(0x1c, 259)).Data[4 + 0xfe]);
        Assert.Equal(untouched, memory.Bytes[0x80ff]);
        Assert.False(target.Bus.Busy);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SameUnitCommandConsumesThePendingDumpButBusResetPreservesIt(bool reset)
    {
        var memory = new DiagnosticRegisters();
        var target = new Acb4000(_ => null, 2_000_000, registerBus: memory);
        Run(target, DiagnosticCdb(0x1d, 4), [0x62, 0x30, 0, 0]);
        if (reset) target.Reset();
        else Run(target, Short(3, count: 4));
        Assert.Equal(reset ? 0 : 2, Run(target, DiagnosticCdb(0x1c, 260)).Status);
        Assert.Empty(memory.Reads);
    }

    [Theory]
    [InlineData(0x61)]
    [InlineData(0x62)]
    [InlineData(0x63)]
    [InlineData(0x64)]
    public void DiagnosticsRequireTheFirmwareQualifierButNotAnExternalRegisterBus(byte option)
    {
        var memory = new DiagnosticRegisters();
        var target = new Acb4000(_ => null, 2_000_000, registerBus: memory);
        Assert.Equal(2, Run(target, DiagnosticCdb(0x1d, 4), [option, 0x31, 0, 0]).Status);
        Assert.Equal(0x24, Run(target, Short(3, count: 4)).Data[0]);
        Assert.Empty(memory.Reads);
        Assert.Empty(memory.Writes);
        var disconnected = new Acb4000(_ => null, 2_000_000);
        var status = Run(disconnected, DiagnosticCdb(0x1d, 4), [option, 0x30, 0, 0]).Status;
        Assert.Equal(0, status);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(83)]
    [InlineData(126)]
    [InlineData(259)]
    [InlineData(260)]
    [InlineData(1024)]
    public void NativeHardwareDumpTransfersAllocatedPrefixWithoutAnInventedRegisterArray(int allocation)
    {
        var target = new Acb4000(_ => null, 2_000_000);
        // This unpopulated port has no register state. It only drives the digital board data bus.
        Assert.Equal(0, Run(target, DiagnosticCdb(0x1d, 5), [0x63, 0x30, 0x40, 1, 0xa6]).Status);
        target.Aic.TryRead(0x80, out var firstMicroinstruction);
        Assert.Equal(0, Run(target, DiagnosticCdb(0x1d, 4), [0x61, 0x30, 0, 0]).Status);
        var result = Run(target, DiagnosticCdb(0x1c, allocation));
        Assert.Equal(0, result.Status);
        Assert.Equal(Math.Min(allocation, 259), result.Data.Length);
        Assert.Equal(new byte[] { 1, 4, 0x40, 0 }.Take(allocation), result.Data.Take(Math.Min(allocation, 4)));
        if (result.Data.Length > 4) Assert.Equal(0xa6, result.Data[4]);
        if (result.Data.Length > 4 + 0x52) Assert.Equal(B.HostBusyInput, result.Data[4 + 0x52]);
        if (result.Data.Length > 4 + 0x53) Assert.Equal(0x40, result.Data[4 + 0x53]);
        if (result.Data.Length > 4 + 0x80) Assert.Equal(firstMicroinstruction, result.Data[4 + 0x80]);
        Assert.Equal(2, Run(target, DiagnosticCdb(0x1c, allocation)).Status);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NativeHardwareDumpAcknowledgesStatusOnlyIfThatRegisterIsActuallyRead(bool full)
    {
        var target = new Acb4000(_ => null, 2_000_000);
        Assert.Equal(0, Run(target, DiagnosticCdb(0x1d, 5), [0x63, 0x30, 0x80, 1, 0x40]).Status);
        Assert.Equal(0, Run(target, DiagnosticCdb(0x1d, 5), [0x63, 0x30, 0x79, 1, 0]).Status);
        target.SetIndex(true);
        Assert.Equal(0, Run(target, DiagnosticCdb(0x1d, 4), [0x61, 0x30, 0, 0]).Status);
        var result = Run(target, DiagnosticCdb(0x1c, full ? 259 : 4));
        Assert.Equal(0, result.Status);
        if (full) Assert.NotEqual(0, result.Data[4 + 0x79] & 0x20);
        target.Aic.TryRead(0x79, out var status);
        Assert.Equal(full ? 0 : 0x20, status & 0x20);
    }

    [Fact]
    public void NativeHardwareDumpPendingAcrossResetUsesLiveRegistersAndKeepsFloatingBusConvention()
    {
        var target = new Acb4000(_ => null, 2_000_000);
        Assert.Equal(0, Run(target, DiagnosticCdb(0x1d, 5), [0x63, 0x30, 0x40, 1, 0xa6]).Status);
        Assert.Equal(0, Run(target, DiagnosticCdb(0x1d, 4), [0x61, 0x30, 0, 0]).Status);
        target.Reset();
        var result = Run(target, DiagnosticCdb(0x1c, 259));
        Assert.Equal(0, result.Status);
        Assert.Equal(259, result.Data.Length);
        Assert.Equal(0xa6, result.Data[4]);
        Assert.Equal(B.HostBusyInput, result.Data[4 + 0x52]);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 2)]
    [InlineData(2, 2)]
    [InlineData(3, 2)]
    public void PatchingWorkingRamChangesThePolicyOfActualEccReads(byte mode, byte expectedStatus)
    {
        var target = CreateManualTarget();
        Assert.Equal(0, Run(target, DiagnosticCdb(0x1d, 5), [0x64, 0x30, 0x9a, 1, (byte)(mode << 3)]).Status);
        SendCommand(target, Short(8, count: 1));
        var payload = Enumerable.Range(0, 256).Select(index => (byte)(index * 13 + 7)).ToArray();
        var damaged = PhysicalData(payload);
        damaged[2] ^= 0x80;
        for (var attempt = 0; attempt < ((mode & 1) != 0 ? 1 : 6); attempt++)
            PhysicalPair(target, PhysicalId(0), damaged);
        var data = DrainRead(target, out var status);
        Assert.Equal(expectedStatus, status);
        if ((mode & 1) != 0) Assert.Empty(data);
        else Assert.Equal(payload, data);
    }

    [Fact]
    public void UnitSwitchSavesAndLoadsTheSamePolicyBytesThatRamPatchUses()
    {
        var target = new Acb4000(_ => null, 2_000_000);
        SetReadPolicy(target, 1);
        SetReadPolicy(target, 2, unit: 1);
        // Unit 0's context was saved at 801A while unit 1's working policy is 809A.
        var patch = DiagnosticCdb(0x1d, 5);
        patch[1] = 0x20;
        Assert.Equal(0, Run(target, patch, [0x64, 0x30, 0x1a, 1, 0]).Status);
        Assert.Equal(0x10, DumpRam(target, 1)[0x9a]);
        Assert.Equal(0, DumpRam(target, 0)[0x9a]);
        Assert.Equal(0x10, DumpRam(target, 1)[0x9a]);
    }

    [Fact]
    public void PolicyCommandPreservesOtherContextBitsAndBusResetPreservesRam()
    {
        var target = new Acb4000(_ => null, 2_000_000);
        Assert.Equal(0, Run(target, DiagnosticCdb(0x1d, 5), [0x64, 0x30, 0x9a, 1, 0xe7]).Status);
        SetReadPolicy(target, 2);
        Assert.Equal(0xf7, DumpRam(target, 0)[0x9a]);
        target.Reset();
        Assert.Equal(0xf7, DumpRam(target, 0)[0x9a]);
    }

    [Fact]
    public void NativeRamPatchDoesNotInventRegistersOutsideItsDecodedPage()
    {
        var target = new Acb4000(_ => null, 2_000_000);
        Assert.Equal(2, Run(target, DiagnosticCdb(0x1d, 6), [0x64, 0x30, 0xff, 2]).Status);
        Assert.Equal(0x20, Run(target, Short(3, count: 4)).Data[0]);
    }

    [Fact]
    public void NativeHardwarePatchChangesTheEccUsedForPhysicalReadsAndResetReloadsFirmwareTaps()
    {
        var target = CreateManualTarget();
        // Acquire the real drive parameters before changing the field polynomial.
        SendCommand(target, Short(8, count: 1));
        PhysicalPair(target, PhysicalId(0), PhysicalData(new byte[256]));
        Assert.Equal(new byte[256], DrainRead(target, out var initialStatus));
        Assert.Equal(0, initialStatus);
        const uint polynomial = 0x04c11db7;
        var taps = polynomial >> 1;
        Assert.Equal(0, Run(target, DiagnosticCdb(0x1d, 8),
            [0x63, 0x30, 0x74, 4, (byte)taps, (byte)(taps >> 8), (byte)(taps >> 16), (byte)(taps >> 24)]).Status);
        var payload = Enumerable.Range(0, 256).Select(index => (byte)(index * 7)).ToArray();
        var id = new byte[10];
        var data = new byte[262];
        Acb4000FieldCodec.WriteId(id, 0, 0, polynomial: polynomial);
        Acb4000FieldCodec.WriteData(data, payload, polynomial);
        SendCommand(target, Short(8, count: 1));
        PhysicalPair(target, id, data);
        Assert.Equal(payload, DrainRead(target, out var status));
        Assert.Equal(0, status);
        target.Reset();
        SendCommand(target, Short(8, count: 1));
        PhysicalPair(target, PhysicalId(0), PhysicalData(payload));
        Assert.Equal(payload, DrainRead(target, out status));
        Assert.Equal(0, status);
    }

    [Fact]
    public void NativeEccResetControlSuppressesReadGateUntilBoardFirmwareRestartsTheAsic()
    {
        var target = CreateManualTarget();
        Assert.Equal(0, Run(target, DiagnosticCdb(0x1d, 5), [0x63, 0x30, 0x71, 1, 0xe0]).Status);
        SendCommand(target, Short(8, count: 1), waitForParameters: false);
        for (var tick = 0; tick < 20; tick++) Tick(target);
        Assert.False(target.ReadGate);
        Assert.False(target.WriteGate);
        target.Reset();
        SendCommand(target, Short(8, count: 1));
        PhysicalPair(target, PhysicalId(0), PhysicalData(new byte[256]));
        Assert.Equal(new byte[256], DrainRead(target, out var status));
        Assert.Equal(0, status);
    }

    [Fact]
    public void OtherUnitCommandDoesNotConsumeThePreparedRamDump()
    {
        var target = new Acb4000(_ => null, 2_000_000);
        Assert.Equal(0, Run(target, DiagnosticCdb(0x1d, 4), [0x62, 0x30, 0, 0]).Status);
        SetReadPolicy(target, 2, unit: 1);
        Assert.Equal(0, Run(target, DiagnosticCdb(0x1c, 6)).Status);
    }

    [Fact]
    public void PatchingTheLiveRemainingCounterCanExtendTheDeclaredTransfer()
    {
        var target = new Acb4000(_ => null, 2_000_000);
        // Only one payload byte was declared. Writing 03 into 8084 supplies three more.
        Assert.Equal(0, Run(target, DiagnosticCdb(0x1d, 5), [0x64, 0x30, 0x84, 4, 3, 0, 0xaa, 0xbb]).Status);
        var ram = DumpRam(target, 0);
        Assert.Equal(0xaa, ram[0x86]);
        Assert.Equal(0xbb, ram[0x87]);
    }

    [Fact]
    public void NativeRamPointerAndFlagPatchExposeTheActualSerialEccRegisters()
    {
        var target = new Acb4000(_ => null, 2_000_000);
        Run(target, DiagnosticCdb(0x1d, 5), [0x63, 0x30, 0x71, 1, 0xc8]);
        const uint value = 0x12345678;
        for (var bit = 31; bit >= 0; bit--)
            Assert.Equal(0, Run(target, DiagnosticCdb(0x1d, 5), [0x63, 0x30, 0x71, 1, (byte)(0xc6 | ((value >> bit) & 1))]).Status);
        Assert.Equal(0, Run(target, DiagnosticCdb(0x1d, 9), [0x64, 0x30, 0x98, 5, 0x72, 0x40, 0, 0, 4]).Status);
        var result = Run(target, DiagnosticCdb(0x1c, 6));
        Assert.Equal(0, result.Status);
        Assert.Equal(new byte[] { 1, 4, 0x40, 0x72, 0x35, 0x12 }, result.Data);
    }

    [Fact]
    public void NativeMicroprogramPatchUsesTheOwnedAicAndResetReloadsTheFirmwareTable()
    {
        var target = new Acb4000(_ => null, 2_000_000);
        Assert.Equal(0, Run(target, DiagnosticCdb(0x1d, 7), [0x63, 0x30, 0x80, 3, 0x1f, 0x1e, 0x1d]).Status);
        var dump = DumpNativeRegisters(target, 0x80, 3);
        Assert.Equal(new byte[] { 0x1f, 0x1e, 0x1d }, dump);
        Assert.True(target.Aic.Sequencer.TryReadProgram(0x80, out var value));
        Assert.Equal(0x1f, value);
        target.Reset();
        Assert.Equal(new byte[] { 1, 2, 3 }, DumpNativeRegisters(target, 0x80, 3));
        Assert.Equal(0x10, target.Aic.WriteAddressMarkControl);
        Assert.Equal(0xa1, target.Aic.DetectAddressMarkControl);
    }

    [Fact]
    public void NativeIndexInputBranchesThePatchedSequencerAndStatusReadAcknowledgesIt()
    {
        var target = new Acb4000(_ => null, 2_000_000);
        Assert.Equal(0, Run(target, DiagnosticCdb(0x1d, 5), [0x63, 0x30, 0x80, 1, 0x40]).Status);
        Assert.Equal(0, Run(target, DiagnosticCdb(0x1d, 5), [0x63, 0x30, 0x79, 1, 0]).Status);
        Assert.False(target.Aic.Sequencer.Stopped);
        target.SetIndex(true);
        Assert.True(target.Aic.Sequencer.Stopped);
        Assert.NotEqual(0, DumpNativeRegisters(target, 0x79, 1)[0] & 0x20);
        Assert.Equal(0, DumpNativeRegisters(target, 0x79, 1)[0] & 0x20);
        Assert.NotEqual(0, DumpNativeRegisters(target, 0x7a, 1)[0] & 1);
        target.SetIndex(false);
        Assert.NotEqual(0, DumpNativeRegisters(target, 0x7a, 1)[0] & 1);
        Assert.Equal(0, DumpNativeRegisters(target, 0x7a, 1)[0] & 1);
    }

    [Fact]
    public void NativeSequencerWritesTheSameBufferReturnedByTheScsiBufferCommand()
    {
        var target = new Acb4000(_ => null, 2_000_000);
        Assert.Equal(0, Run(target, DiagnosticCdb(0x1d, 5), [0x63, 0x30, 0x80, 1, 0x1f]).Status);
        Assert.Equal(0, Run(target, DiagnosticCdb(0x1d, 5), [0x63, 0x30, 0xa0, 1, 0x41]).Status);
        Assert.Equal(0, Run(target, DiagnosticCdb(0x1d, 5), [0x63, 0x30, 0xc0, 1, 0]).Status);
        Assert.Equal(0, Run(target, DiagnosticCdb(0x1d, 5), [0x63, 0x30, 0x79, 1, 0]).Status);
        for (var bit = 7; bit >= 0; bit--) target.Aic.Sequencer.ClockBit((0xa6 & (1 << bit)) != 0);
        Assert.True(target.BufferController.PeripheralCyclePending);
        Assert.False(target.IsIdle);
        for (var tick = 0; tick < 8; tick++) target.Tick();
        Assert.False(target.BufferController.PeripheralCyclePending);
        Assert.Equal(1ul, target.Aic.Sequencer.TransferByteCount);
        var result = Run(target, [0x14, 0, 0, 0, 0, 0]);
        Assert.Equal(0, result.Status);
        Assert.Equal(0xa6, result.Data[0]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NativeCpuBufferPatchUsesTheProgrammedPointerWithoutIncrement(bool rop)
    {
        var target = new Acb4000(_ => null, 2_000_000);
        var pointer = rop ? (byte)0x5c : (byte)0x5a;
        Assert.Equal(0, Run(target, DiagnosticCdb(0x1d, 5), [0x63, 0x30, 0x53, 1, (byte)(rop ? 0xd0 : 0xc0)]).Status);
        Assert.Equal(0, Run(target, DiagnosticCdb(0x1d, 6), [0x63, 0x30, pointer, 2, 0x34, 0x02]).Status);
        Assert.Equal(0, Run(target, DiagnosticCdb(0x1d, 5), [0x63, 0x30, 0x70, 1, 0xa9]).Status);
        Assert.Equal(new byte[] { 0x34, 0x02 }, DumpNativeRegisters(target, pointer, 2));
        Assert.Equal(new byte[] { 0xa9 }, DumpNativeRegisters(target, 0x70, 1));
        var contents = Run(target, [0x14, 0, 0, 0, 0, 0]);
        Assert.Equal(0, contents.Status);
        Assert.Equal(0xa9, contents.Data[0x234]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NativeBufferDmaDoneWaitsForTheLastAckToFall(bool read)
    {
        var target = new Acb4000(_ => null, 2_000_000);
        SendCommand(target, [(byte)(read ? 0x14 : 0x13), 0, 0, 0, 0, 0]);
        for (var index = 0; index < 1023; index++)
        {
            WaitRequest(target);
            Transfer(target, (byte)index);
        }
        WaitRequest(target);
        target.BufferController.TryRead(0x53, out var status);
        Assert.Equal(0, status & 0x20);
        target.SetInitiator(0xa9, false, true, false);
        for (var index = 0; index < 10; index++) target.Tick();
        Assert.True(target.BufferController.HostTransferPending);
        target.BufferController.TryRead(0x53, out status);
        Assert.Equal(0, status & 0x20);
        Assert.Equal(0, read ? target.BufferController.ReadPointer : target.BufferController.WritePointer);
        target.SetInitiator(0xa9, false, false, false);
        target.Tick();
        Assert.False(target.BufferController.HostTransferPending);
        target.BufferController.TryRead(0x53, out status);
        Assert.Equal(0xe0, status);
        Assert.False(target.IsIdle); // Status/message phases still own the SCSI bus.
        target.Reset();
        Assert.True(target.IsIdle);
    }

    [Fact]
    public void NativeResetDuringBufferDmaKeepsAcknowledgedRamButClearsPendingState()
    {
        var target = new Acb4000(_ => null, 2_000_000);
        SendCommand(target, [0x13, 0, 0, 0, 0, 0]);
        WaitRequest(target);
        target.SetInitiator(0x83, false, true, false);
        WaitRequestCleared(target);
        Assert.True(target.BufferController.HostTransferPending);
        target.Reset();
        Assert.False(target.BufferController.HostTransferPending);
        Assert.Equal(0, target.BufferController.WritePointer);
        var result = Run(target, [0x14, 0, 0, 0, 0, 0]);
        Assert.Equal(0, result.Status);
        Assert.Equal(1024, result.Data.Length);
        Assert.Equal(0x83, result.Data[0]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NativeRequestTimeoutReleasesDmaAndAllowsAnotherSelection(bool read)
    {
        var target = new Acb4000(_ => null, 2_000_000);
        SendCommand(target, [(byte)(read ? 0x14 : 0x13), 0, 0, 0, 0, 0]);
        WaitRequest(target);
        Transfer(target, 0x83);
        WaitRequest(target);
        for (var ticks = 0; ticks < 1_000_000 && target.Bus.Busy; ticks++) target.Tick();
        Assert.False(target.Bus.Busy);
        Assert.True(target.Bus.TimedOut);
        Assert.True(target.IsIdle);
        Assert.False(target.BufferController.HostTransferPending);
        Assert.False(target.BufferController.HostTransferEnabled);
        target.BufferController.TryRead(0x53, out var status);
        Assert.Equal(0xc0, status);
        Assert.Equal(read ? 2 : 1, read ? target.BufferController.ReadPointer : target.BufferController.WritePointer);
        var result = Run(target, [0x14, 0, 0, 0, 0, 0]);
        Assert.Equal(0, result.Status);
        Assert.Equal(1024, result.Data.Length);
        if (!read) Assert.Equal(0x83, result.Data[0]);
    }

    [Fact]
    public void PhysicalReadDependsOnTheProgrammedIdStackControls()
    {
        var target = CreateDiskTarget();
        Assert.Equal(0, Run(target, DiagnosticCdb(0x1d, 5), [0x63, 0x30, 0xa7, 1, 0x02]).Status);
        var result = Run(target, Short(0x08, count: 1));
        Assert.Equal(2, result.Status);
        Assert.Empty(result.Data);
    }

    [Fact]
    public void ShortenedMicroprogramDataCountCannotBeReplacedByTheDecodedSectorCopy()
    {
        var disk = new Disk();
        var target = CreateTarget(_ => disk, 2_000_000);
        Assert.Equal(0, Run(target, DiagnosticCdb(0x1d, 5), [0x63, 0x30, 0xc4, 1, 0]).Status);
        var result = Run(target, Short(0x08, count: 1));
        Assert.Equal(2, result.Status);
        Assert.Equal(256, result.Data.Length);
        Assert.False(disk.Bytes.AsSpan(0, 256).SequenceEqual(result.Data));
    }

    private static byte[] DumpNativeRegisters(Acb4000 target, byte address, byte length)
    {
        Assert.Equal(0, Run(target, DiagnosticCdb(0x1d, 9), [0x64, 0x30, 0x98, 5, address, 0x40, 0, 0, 4]).Status);
        var result = Run(target, DiagnosticCdb(0x1c, 4 + length));
        Assert.Equal(0, result.Status);
        return result.Data.AsSpan(4).ToArray();
    }

    private static byte[] DumpRam(Acb4000 target, int unit)
    {
        var prepare = DiagnosticCdb(0x1d, 4);
        prepare[1] = (byte)(unit << 5);
        Assert.Equal(0, Run(target, prepare, [0x62, 0x30, 0, 0]).Status);
        var receive = DiagnosticCdb(0x1c, 259);
        receive[1] = prepare[1];
        var result = Run(target, receive);
        Assert.Equal(0, result.Status);
        return result.Data.AsSpan(4).ToArray();
    }

    private static byte[] DiagnosticCdb(byte opcode, int length)
    {
        var cdb = new byte[] { opcode, 0, 0, 0, 0, 0 };
        BinaryPrimitives.WriteUInt16BigEndian(cdb.AsSpan(3), checked((ushort)length));
        return cdb;
    }

    private sealed class DiagnosticRegisters : IAcb4000RegisterBus
    {
        internal Action<ushort, bool>? Access { get; set; }
        internal byte[] Bytes { get; } = Enumerable.Range(0, ushort.MaxValue + 1).Select(index => (byte)(index * 37 + 11)).ToArray();
        internal List<ushort> Reads { get; } = [];
        internal List<ushort> Writes { get; } = [];
        public byte Read(ushort address) { Access?.Invoke(address, false); Reads.Add(address); return Bytes[address]; }
        public void Write(ushort address, byte value) { Access?.Invoke(address, true); Writes.Add(address); Bytes[address] = value; }
    }
}
