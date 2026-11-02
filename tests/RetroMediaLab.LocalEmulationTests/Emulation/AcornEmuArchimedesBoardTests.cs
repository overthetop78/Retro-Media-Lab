using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Arm26.Enums;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Memc.Enums;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Vidc.Enums;
using System.Buffers.Binary;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Enums;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Interfaces;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Enums;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.Archimedes.Common.Services;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.Archimedes.Common.Enums;

namespace RetroMediaLab.Tests.Emulation;

public sealed class AcornEmuArchimedesBoardTests
{
    [Fact]
    public void UnfittedSerialUpgradeLeavesItsRegistersAbsentAcrossResets()
    {
        var machine = new ArchimedesMachine(Rom(), 1 << 20, serialPort: false, floppyDrives: 1);
        Assert.Equal(1, machine.FloppyDriveCount);
        Assert.Equal(0, machine.HardDiskDriveCount);
        Assert.Throws<ArgumentOutOfRangeException>(() => machine.AttachFloppy(1, null));
        foreach (var cold in new[] { false, true })
        {
            machine.Reset(cold);
            Write(machine, 0x03230008, 0x09);
            Write(machine, 0x0323000c, 0x1f);
            Assert.Equal(0xffu, Read(machine, 0x03230004));
            Assert.Equal(0xffu, Read(machine, 0x03230008));
            Assert.Equal(0xffu, Read(machine, 0x0323000c));
        }
        machine.RunFrame();
        Assert.False(machine.Irq);
    }

    [Fact]
    public void CommonMachineContractExposesTheRealVideoStereoAndNoCassette()
    {
        IAcornMachine machine = Board();
        Assert.Null(machine.CassetteTransport);
        Assert.False(machine.CassetteHasChanges);
        machine.ConfigureCassette(new EmulationCassetteSettings());
        machine.MarkCassetteSaved();
        machine.EjectCassette();
        Assert.Throws<NotSupportedException>(() => machine.InsertCassette(new EmulationCassetteSignal(new short[1], 44100)));
        Assert.Throws<NotSupportedException>(() => machine.RecordedCassette);
        Assert.Equal(EmulationAudioChannelLayout.Stereo, machine.AudioChannelLayout);
        var frame = machine.RunFrame();
        Assert.Equal(machine.VideoWidth * machine.VideoHeight * 4, frame.Length);
        var audio = machine.DrainAudio();
        var chunk = new AudioChunk(audio, 44100, audio.Length / 2, 1, TimeSpan.Zero, machine.AudioChannelLayout);
        Assert.True(chunk.HasValidLength);
        Assert.NotEmpty(audio);
        Assert.Empty(machine.DrainAudio());
    }

    [Fact]
    public void CommonMachineContractRetainsCmosAcrossBothResetsAndRestoration()
    {
        IAcornMachine machine = Board();
        var saved = machine.ReadNonvolatileMemory()!;
        saved[0x80] = 0x37;
        machine.WriteNonvolatileMemory(saved);
        machine.Reset(false);
        Assert.Equal(0x37, machine.ReadNonvolatileMemory()![0x80]);
        machine.Reset(true);
        Assert.Equal(0x37, machine.ReadNonvolatileMemory()![0x80]);
        IAcornMachine restored = Board();
        restored.WriteNonvolatileMemory(machine.ReadNonvolatileMemory()!);
        Assert.Equal(0x37, restored.ReadNonvolatileMemory()![0x80]);
        Assert.Equal("archimedes-cmos.bin", restored.NonvolatileMemoryFile);
    }

    [Fact]
    public void St506IsAbsentUnlessInstalledAndDoesNotOwnTheAttachedDisks()
    {
        var machine = Board();
        using var disk = new AcornEmuHd63463Tests.Disk();
        Assert.Equal(uint.MaxValue, Read(machine, 0x03250020));
        Assert.Throws<NotSupportedException>(() => machine.AttachHardDisk(0, disk));
        Assert.False(machine.HardDiskMotor(0));
        Assert.False(machine.HardDiskActive(0));
        var installed = new ArchimedesMachine(Rom(), 4 << 20, st506: true);
        installed.AttachHardDisk(0, disk);
        Assert.True(installed.HardDiskMotor(0));
        installed.Reset(false);
        Assert.False(installed.HardDiskActive(0));
        Assert.False(disk.Disposed);
        installed.AttachHardDisk(0, null);
        Assert.False(installed.HardDiskMotor(0));
        Assert.False(disk.Disposed);
    }

    [Fact]
    public void St506DmaAndCompletionShareTheBoardInterruptWithoutConfusingDataLanes()
    {
        using var disk = new AcornEmuHd63463Tests.Disk(0x31);
        var machine = new ArchimedesMachine(Rom(), 4 << 20, st506: true);
        machine.AttachHardDisk(0, disk);
        machine.ConfigureHardDisk(0, 0, 0);
        WriteIoc(machine, 0x28, 8);
        HdcCommand(machine, 0xe8, [0x1f, 0xa0, 0, 2, 16, 2, 1, 3, 0x21, 10, 13, 12, 0, 0, 0, 0]);
        HdcCommand(machine, 0x40, [1, 0, 0, 0, 0, 1, 0, 2]);
        for (var cycle = 0; cycle < 1000 && (Read(machine, 0x03200020) & 8) == 0; cycle++) machine.InternalCycle();
        Assert.Equal(0xc000u, Read(machine, 0x03250020));
        Assert.Equal(8u, Read(machine, 0x03200020) & 8);
        Assert.True(machine.Irq); // DRQ est présent malgré le masquage CED du HD63463.
        var bytes = new byte[512];
        for (var word = 0; word < 256; word++)
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(word * 2), (ushort)Read(machine,
                word % 2 == 0 ? 0x03250008u : 0x033d000cu));
        for (var cycle = 0; cycle < 8; cycle++) machine.InternalCycle();
        Assert.Equal(disk.Block(1).Concat(disk.Block(2)), bytes);
        Assert.Equal(0x6000u, Read(machine, 0x03250020));
        Assert.False(machine.Irq);
        Assert.Equal(2, machine.HardDiskReadBlocks(0));
        Write(machine, 0x03250000, 8);
        WaitHdcControl(machine);
        Assert.Equal(0u, Read(machine, 0x03250020));
    }

    [Fact]
    public void DisconnectingSt506DuringDrqCancelsTheCommandAndKeepsTheImageBorrowed()
    {
        using var disk = new AcornEmuHd63463Tests.Disk();
        var machine = new ArchimedesMachine(Rom(), 4 << 20, st506: true);
        machine.AttachHardDisk(0, disk);
        machine.ConfigureHardDisk(0, 0, 0);
        HdcCommand(machine, 0xe8, [0x1f, 0x80, 0, 2, 16, 2, 1, 3, 0x21, 10, 13, 12, 0, 0, 0, 0]);
        HdcCommand(machine, 0x87, [1, 0, 0, 0, 0, 0, 0, 1]);
        for (var cycle = 0; cycle < 8; cycle++) machine.InternalCycle();
        machine.AttachHardDisk(0, null);
        Assert.Equal(0x6400u, Read(machine, 0x03250020));
        Assert.Equal(0x20u, Read(machine, 0x03250024));
        Assert.False(machine.HardDiskMotor(0));
        Assert.False(disk.Disposed);
        Assert.False(disk.HasChanges);
    }

    private static void HdcCommand(ArchimedesMachine machine, byte command, byte[] parameters)
    {
        Write(machine, 0x03250000, 8);
        WaitHdcControl(machine);
        for (var offset = 0; offset < parameters.Length; offset += 2)
            Write(machine, 0x03250004, BinaryPrimitives.ReadUInt16BigEndian(parameters.AsSpan(offset)));
        Write(machine, 0x03250000, command);
        if (command == 0xe8) WaitHdcControl(machine);
    }

    private static void WaitHdcControl(ArchimedesMachine machine)
    {
        for (var attempt = 0; attempt < 1000; attempt++)
            if ((Read(machine, 0x03250020) & 0x8000) == 0) return;
        Assert.Fail("The HD63463 control command did not complete.");
    }

    [Fact]
    public void SerialBankHasItsOwnRegistersAndMirrorsWithoutChangingIocDataLanes()
    {
        var machine = Board();
        Assert.Equal(0x60u, Read(machine, 0x03230004));
        Write(machine, 0x03230008, 0x09);
        Write(machine, 0x0323000c, 0x1f);
        Assert.Equal(0x09u, Read(machine, 0x033b0008));
        Assert.Equal(0x1fu, Read(machine, 0x033b000c));
        Write(machine, 0x03230004, 0);
        Assert.Equal(0u, Read(machine, 0x03230008));
        Assert.Equal(0x1fu, Read(machine, 0x0323000c));
        machine.Reset(false);
        Assert.Equal(0u, Read(machine, 0x0323000c));
    }

    [Fact]
    public void MemcForcesNonSequentialCyclesAtFourWordBoundaries()
    {
        var machine = Board();
        int[] expectedTicks = [6, 3, 3, 3, 6, 3, 3, 3];
        for (var word = 0; word < expectedTicks.Length; word++)
        {
            var before = machine.MasterTicks;
            Assert.True(machine.TryReadWord(0x02000000 + (uint)(word * 4), true, false, false, out _));
            Assert.Equal(expectedTicks[word], machine.MasterTicks - before);
        }
        Write(machine, 0x036e00c0, 0); // ROM avec mode nibble : le cycle N réouvre chaque groupe de quatre mots.
        for (var word = 0; word < expectedTicks.Length; word++)
        {
            var before = machine.MasterTicks;
            Assert.True(machine.TryReadWord(0x03800000 + (uint)(word * 4), true, false, true, out _));
            Assert.Equal(expectedTicks[word], machine.MasterTicks - before);
        }
    }

    [Theory]
    [InlineData(1u, 16)]
    [InlineData(2u, 64)]
    [InlineData(3u, 80)]
    public void ArmWordStoresKeepTheCamAddressPinsThatSelectPhysicalPages(uint lowPins, int page)
    {
        var rom = Rom();
        Put(rom, 0, 0xe59ff000);
        Put(rom, 8, 0x03800010);
        Put(rom, 0x10, 0xe59f0018);
        Put(rom, 0x14, 0xe3a01000);
        Put(rom, 0x18, 0xe5801000);
        Put(rom, 0x1c, 0xe3a00000);
        Put(rom, 0x20, 0xe5902000);
        Put(rom, 0x30, 0x03800000 | lowPins);
        var machine = new ArchimedesMachine(rom, 4 << 20);
        machine.Step(); // Le branchement ROM libère d'abord le miroir de démarrage.
        Write(machine, 0x036e000c, 0);
        Write(machine, 0x02000000 + (uint)(page << 15), 0x12345678);
        for (var instruction = 0; instruction < 10; instruction++) machine.Step();
        Assert.Equal(0x12345678u, Read(machine, 0));
        Assert.InRange(machine.ProgramCounter, 0x03800024u, 0x0380002cu);
    }

    [Theory]
    [InlineData(0, 8000000)]
    [InlineData(1, 12000000)]
    [InlineData(2, 25000000)]
    public void ArmExecutesResetRomAndWritesPhysicalRam(int processor, int clock)
    {
        var rom = Rom();
        Put(rom, 0, 0xe59ff000); // LDR PC,[PC] : quitter le miroir de démarrage.
        Put(rom, 8, 0x03800010);
        Put(rom, 0x10, 0xe59f0018); // LDR R0,[PC,#24].
        Put(rom, 0x14, 0xe3a0105a);
        Put(rom, 0x18, 0xe5801000);
        Put(rom, 0x30, 0x02000000);
        var machine = new ArchimedesMachine(rom, 4 << 20, (Arm26Model)processor, clock);
        for (var instruction = 0; instruction < 10; instruction++) machine.Step();
        Assert.Equal(0x5au, Read(machine, 0x02000000));
        Assert.InRange(machine.ProgramCounter, 0x0380001cu, 0x03800024u);
        Assert.True(machine.MasterTicks > 0);
    }

    [Fact]
    public void UnmappedUserMemoryAbortsWithoutChangingPhysicalRamOrIo()
    {
        var machine = Board();
        Read(machine, 0x03800000);
        Assert.False(machine.TryReadWord(0, false, true, false, out _));
        Assert.False(machine.TryWriteWord(0x02000000, 123, false, true));
        Assert.False(machine.TryWriteWord(0x03200018, uint.MaxValue, false, true));
        Assert.True(machine.TryReadWord(0x03800000, false, true, true, out _));
        Assert.Equal(0u, Read(machine, 0x02000000));
        Assert.False(machine.Irq);
    }

    [Fact]
    public void IocWordWritesUseThePhysicalDataByteAndTimersFollowTheBoardClock()
    {
        var machine = Board();
        WriteIoc(machine, 0x18, 0x20);
        WriteIoc(machine, 0x40, 3);
        WriteIoc(machine, 0x44, 0);
        WriteIoc(machine, 0x48, 0);
        for (var cycle = 0; cycle < 40; cycle++) machine.InternalCycle();
        Assert.True(machine.Irq);
        WriteIoc(machine, 0x18, 0);
        Assert.False(machine.Irq);
    }

    [Fact]
    public void CmosCanBeWrittenThroughIocGpioAndSurvivesColdComputerReset()
    {
        var machine = Board();
        Lines(machine, true, true);
        Lines(machine, true, false);
        Lines(machine, false, false);
        Assert.True(Send(machine, 0xa0));
        Assert.True(Send(machine, 0x80));
        Assert.True(Send(machine, 0x5a));
        Lines(machine, false, false);
        Lines(machine, true, false);
        Lines(machine, true, true);
        Assert.Equal(0x5a, machine.ReadNonvolatileMemory()[0x80]);
        machine.Reset(true);
        Assert.Equal(0x5a, machine.ReadNonvolatileMemory()[0x80]);
    }

    [Fact]
    public void FloppyLatchesSelectAllFourDrivesAndRouteIntrqToFiq()
    {
        var machine = Board();
        WriteIoc(machine, 0x38, 2);
        Write(machine, 0x03250018, 8);
        for (var drive = 0; drive < 4; drive++)
        {
            machine.ConfigureFloppySpeed(drive, 0);
            Write(machine, 0x03250040, (uint)(0xdf & ~(1 << drive)));
            Assert.True(machine.FloppyMotor(drive));
            for (var other = 0; other < 4; other++)
                if (other != drive) Assert.False(machine.FloppyMotor(other));
            Write(machine, 0x03210000, 0x58);
            for (var cycle = 0; cycle < 5; cycle++) machine.InternalCycle();
            Assert.Equal(1, machine.FloppySeekSteps(drive));
            Assert.True(machine.Fiq);
            Read(machine, 0x03210000);
            Assert.False(machine.Fiq);
        }
        Write(machine, 0x03250040, 0xff);
        Assert.DoesNotContain(Enumerable.Range(0, 4), machine.FloppyMotor);
    }

    [Fact]
    public void LockedBusDefersSoundDmaAndUnlockRestoresTheProgrammedStereoImage()
    {
        var machine = Board();
        machine.SetBusLock(true);
        for (uint address = 0x02000000; address < 0x02000010; address += 4)
            Write(machine, address, 0xfcfcfcfc);
        for (uint stereo = 0x60; stereo <= 0x7c; stereo += 4)
            Write(machine, 0x03400000, stereo << 24 | 1);
        Write(machine, 0x03400000, 0xc0000010);
        Write(machine, 0x03680000, 0);
        Write(machine, 0x036a0000, 0);
        Write(machine, 0x036c0000, 0);
        Write(machine, 0x036e0800, 0);
        for (var cycle = 0; cycle < 3000; cycle++) machine.InternalCycle();
        Assert.All(machine.DrainAudio(), sample => Assert.Equal(0, sample));
        machine.SetBusLock(false);
        for (var cycle = 0; cycle < 3000; cycle++) machine.InternalCycle();
        var audio = machine.DrainAudio();
        Assert.Contains(audio.Where((_, index) => index % 2 == 0), sample => sample > 0);
        Assert.All(audio.Where((_, index) => index % 2 == 1), sample => Assert.Equal(0, sample));
    }

    [Fact]
    public void RunFrameIsBoundedBeforeTheRomProgramsVideoAndProducesStereoFrames()
    {
        var machine = Board();
        var before = machine.MasterTicks;
        var frame = machine.RunFrame();
        Assert.Equal(machine.Width * machine.Height * 4, frame.Length);
        Assert.InRange(machine.MasterTicks - before, 480000, 480100);
        var samples = machine.DrainAudio();
        Assert.InRange(samples.Length, 1760, 1770);
        Assert.Equal(0, samples.Length % 2);
        Assert.Empty(machine.DrainAudio());
    }

    private static ArchimedesMachine Board() => new(Rom(), 4 << 20, initialTime: new DateTime(2024, 1, 1));

    [Theory]
    [InlineData((int)ArchimedesMemcRevision.Memc1, 0x02000004u, 27)]
    [InlineData((int)ArchimedesMemcRevision.Memc1, 0x0200000cu, 30)]
    [InlineData((int)ArchimedesMemcRevision.Memc1A, 0x02000004u, 15)]
    [InlineData((int)ArchimedesMemcRevision.Memc1A, 0x0200000cu, 15)]
    [InlineData((int)ArchimedesMemcRevision.Memc1, 0x03800004u, 51)]
    [InlineData((int)ArchimedesMemcRevision.Memc1A, 0x03800004u, 21)]
    public void MultiplyRepeatsMemoryCyclesOnlyOnMemc1(int revision, uint nextAddress, int expected)
    {
        var machine = new ArchimedesMachine(Rom(), 4 << 20, memoryRevision: (ArchimedesMemcRevision)revision);
        Read(machine, 0x02000000);
        var before = machine.MasterTicks;
        machine.MultiplyCycles(nextAddress, 4);
        Assert.True(machine.TryReadWord(nextAddress, false, false, true, out _));
        Assert.Equal(expected, machine.MasterTicks - before);
    }

    [Theory]
    [InlineData((int)ArchimedesMemcRevision.Memc1, 0x02000004u, 3, 3)]
    [InlineData((int)ArchimedesMemcRevision.Memc1, 0x0200000cu, 6, 6)]
    [InlineData((int)ArchimedesMemcRevision.Memc1A, 0x02000004u, 3, 3)]
    [InlineData((int)ArchimedesMemcRevision.Memc1A, 0x0200000cu, 3, 3)]
    [InlineData((int)ArchimedesMemcRevision.Memc1, 0x03800004u, 9, 12)]
    [InlineData((int)ArchimedesMemcRevision.Memc1, 0x0380000cu, 12, 12)]
    [InlineData((int)ArchimedesMemcRevision.Memc1A, 0x03800004u, 9, 3)]
    public void MergedFetchSharesOneClockExceptAtTheMemc1Boundary(int revision, uint nextAddress, int fetchTicks, int followingInternalTicks)
    {
        var machine = new ArchimedesMachine(Rom(), 4 << 20, memoryRevision: (ArchimedesMemcRevision)revision);
        Read(machine, 0x02000000);
        machine.MergeInternalCycle();
        var start = machine.MasterTicks;
        Assert.True(machine.TryReadWord(nextAddress, true, false, true, out _));
        Assert.Equal(fetchTicks, machine.MasterTicks - start);
        start = machine.MasterTicks;
        machine.InternalCycle();
        Assert.Equal(followingInternalTicks, machine.MasterTicks - start);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NonInstructionAccessAndIndependentCycleCancelPendingMerge(bool independentCycle)
    {
        var machine = Board();
        Read(machine, 0x02000000);
        machine.MergeInternalCycle();
        if (independentCycle) machine.InternalCycle();
        else Read(machine, 0x02000008);
        var before = machine.MasterTicks;
        Assert.True(machine.TryReadWord(0x02000004, false, false, true, out _));
        Assert.Equal(6, machine.MasterTicks - before);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ResetDoesNotRetainAMergedFetch(bool cold)
    {
        var machine = Board();
        machine.MergeInternalCycle();
        machine.Reset(cold);
        var before = machine.MasterTicks;
        Assert.True(machine.TryReadWord(0x02000004, false, false, true, out _));
        Assert.Equal(6, machine.MasterTicks - before);
    }

    [Theory]
    [InlineData((int)ArchimedesMemcRevision.Memc1, 0x02000000u, false, 6)]
    [InlineData((int)ArchimedesMemcRevision.Memc1, 0x02000004u, true, 3)]
    [InlineData((int)ArchimedesMemcRevision.Memc1, 0x03800000u, false, 12)]
    [InlineData((int)ArchimedesMemcRevision.Memc1, 0x03800004u, true, 12)]
    [InlineData((int)ArchimedesMemcRevision.Memc1A, 0x02000000u, false, 3)]
    [InlineData((int)ArchimedesMemcRevision.Memc1A, 0x02000004u, true, 3)]
    [InlineData((int)ArchimedesMemcRevision.Memc1A, 0x03800000u, false, 3)]
    [InlineData((int)ArchimedesMemcRevision.Memc1A, 0x03800004u, true, 3)]
    public void MemoryRevisionControlsArm2InternalClockLength(int revision, uint address, bool sequential, int expected)
    {
        var machine = new ArchimedesMachine(Rom(), 4 << 20, memoryRevision: (ArchimedesMemcRevision)revision);
        Assert.True(machine.TryReadWord(address, sequential, false, false, out _));
        for (var cycle = 0; cycle < 3; cycle++)
        {
            var before = machine.MasterTicks;
            machine.InternalCycle();
            Assert.Equal(expected, machine.MasterTicks - before);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ResetRetainsMemoryRevisionAndReplacesThePreviousBusCycle(bool cold)
    {
        foreach (var revision in new[] { ArchimedesMemcRevision.Memc1, ArchimedesMemcRevision.Memc1A })
        {
            var machine = new ArchimedesMachine(Rom(), 4 << 20, memoryRevision: revision);
            Assert.True(machine.TryReadWord(0x02000004, true, false, false, out _));
            machine.Reset(cold);
            var before = machine.MasterTicks;
            machine.InternalCycle();
            Assert.Equal(revision == ArchimedesMemcRevision.Memc1 ? 12 : 3, machine.MasterTicks - before);
        }
    }

    [Theory]
    [InlineData((int)ArchimedesMemcRevision.Memc1)]
    [InlineData((int)ArchimedesMemcRevision.Memc1A)]
    public void Arm3FastInternalClockDoesNotInheritTheArm2MemoryStretch(int revision)
    {
        var machine = new ArchimedesMachine(Rom(), 4 << 20, processor: Arm26Model.Arm3,
            cpuClockHz: 25000000, memoryRevision: (ArchimedesMemcRevision)revision);
        Read(machine, 0x03800000);
        var before = machine.MasterTicks;
        for (var cycle = 0; cycle < 25; cycle++) machine.InternalCycle();
        Assert.Equal(24, machine.MasterTicks - before);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SlowVideoRunsUntilACompleteFrameAndReportsActualElapsedTime(bool interlace)
    {
        var machine = Board();
        IAcornMachine host = machine;
        Assert.Null(host.VideoFramesPerSecond);
        Assert.Null(host.VideoFrameDurationSeconds);
        void Timing(VidcRegister register, uint value) => Write(machine, 0x03400000, ((uint)register << 24) | (value << 14));
        Timing(VidcRegister.HorizontalCycle, 511);
        Timing(VidcRegister.VerticalCycle, 311);
        Timing(VidcRegister.HorizontalBorderStart, 0);
        Timing(VidcRegister.HorizontalBorderEnd, 400);
        Timing(VidcRegister.VerticalBorderStart, 0);
        Timing(VidcRegister.VerticalBorderEnd, 256);
        Write(machine, 0x03400000, interlace ? 0xe0000040u : 0xe0000000u);
        var clocksPerFrame = 1024 * 312 * 3 * (interlace ? 2 : 1);
        Assert.Equal(24000000d / clocksPerFrame, host.VideoFramesPerSecond!.Value, 8);
        for (var frame = 0; frame < 2; frame++)
        {
            var start = machine.MasterTicks;
            host.RunFrame();
            var elapsed = machine.MasterTicks - start;
            Assert.InRange(elapsed, clocksPerFrame - 200, clocksPerFrame + 100);
            Assert.Equal(elapsed / 24000000d, host.VideoFrameDurationSeconds!.Value, 10);
        }
        Assert.Equal(800, host.VideoWidth);
        Assert.Equal(interlace ? 512 : 256, host.VideoHeight);
        machine.Reset(true);
        Assert.Null(host.VideoFramesPerSecond);
        Assert.Null(host.VideoFrameDurationSeconds);
    }
    private static byte[] Rom()
    {
        var rom = new byte[256];
        for (var offset = 0; offset < rom.Length; offset += 4) Put(rom, offset, 0xeafffffe);
        return rom;
    }
    private static void Put(byte[] rom, int offset, uint word) => BinaryPrimitives.WriteUInt32LittleEndian(rom.AsSpan(offset), word);
    private static uint Read(ArchimedesMachine machine, uint address)
    {
        Assert.True(machine.TryReadWord(address, false, false, false, out var value));
        return value;
    }
    private static void Write(ArchimedesMachine machine, uint address, uint value) => Assert.True(machine.TryWriteWord(address, value, false, false));
    private static void WriteIoc(ArchimedesMachine machine, uint register, byte value) => Write(machine, 0x03200000 | register, (uint)value << 16);
    private static void Lines(ArchimedesMachine machine, bool scl, bool sda) => WriteIoc(machine, 0, (byte)(0xfc | (scl ? 2 : 0) | (sda ? 1 : 0)));
    private static bool Send(ArchimedesMachine machine, byte value)
    {
        for (var bit = 7; bit >= 0; bit--)
        {
            var high = (value & (1 << bit)) != 0;
            Lines(machine, false, high);
            Lines(machine, true, high);
            Lines(machine, false, high);
        }
        Lines(machine, false, true);
        Lines(machine, true, true);
        var acknowledged = (Read(machine, 0x03200000) & 1) == 0;
        Lines(machine, false, true);
        return acknowledged;
    }
}
