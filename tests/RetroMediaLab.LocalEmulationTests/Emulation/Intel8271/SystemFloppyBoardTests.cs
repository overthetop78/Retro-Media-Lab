using Fdc8271 = RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Services.Intel8271;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Services;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Enums;
using H = RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Constants.Intel8271Constants;

namespace RetroMediaLab.Tests.Emulation;

public sealed class SystemFloppyBoardTests
{
    [Fact]
    public void UnconnectedDecoderResponsesDoNotBlockTheBusAndConnectedResponsesKeepTheirDates()
    {
        var chip = new Fdc8271(2_000_000); var referenceChip = new Fdc8271(2_000_000);
        using var card = new SystemFloppyBoard(chip); using var reference = new SystemFloppyBoard(referenceChip);
        reference.GroupDecoderDeadlines = false;
        var actualClock = new ClockWire(card, periodic: true); var expectedClock = new ClockWire(reference, periodic: true);
        actualClock.AdvanceTo(1_000); expectedClock.AdvanceTo(1_000);
        card.SetBusAddress(0x0e01); reference.SetBusAddress(0x0e01);
        Assert.True(card.PrivateBusPinsSettled);
        Assert.False(reference.PrivateBusPinsSettled);
        long time = 1_000;
        foreach (var address in new ushort[] { 0xe000, 0x0a00, 0x0a04, 0x0a7f, 0x0a80, 0x0e01, 0xe001, 0x0a01 })
        {
            time += 100;
            actualClock.AdvanceTo(time); expectedClock.AdvanceTo(time);
            card.SetBusAddress(address); reference.SetBusAddress(address);
            for (var step = 1; step <= 80; step++)
            {
                actualClock.AdvanceTo(time + step); expectedClock.AdvanceTo(time + step);
                Assert.Equal(reference.ChipSelected, card.ChipSelected);
                Assert.Equal(reference.DataAcknowledged, card.DataAcknowledged);
                Assert.Equal(reference.Ready0, card.Ready0); Assert.Equal(reference.Ready1, card.Ready1);
                Assert.Equal(reference.Nmi, card.Nmi);
                Assert.Equal(referenceChip.InputClockCount, chip.InputClockCount);
            }
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void DescribedDividerQdPreservesReadyTimeoutControllerClocksAndDeselection(int drive)
    {
        var chip = new Fdc8271(2_000_000); var referenceChip = new Fdc8271(2_000_000);
        using var card = new SystemFloppyBoard(chip); using var reference = new SystemFloppyBoard(referenceChip);
        Divider(reference).SetQdPeriodicClockReceiver(0, null);
        var actualClock = new ClockWire(card, periodic: true); var expectedClock = new ClockWire(reference, periodic: true);
        void Advance(long time)
        {
            actualClock.AdvanceTo(time); expectedClock.AdvanceTo(time);
            Divider(card).AdvanceTo(time); Divider(reference).AdvanceTo(time);
            Assert.Equal(reference.Ready0, card.Ready0); Assert.Equal(reference.Ready1, card.Ready1);
            Assert.Equal(reference.Nmi, card.Nmi); Assert.Equal(referenceChip.InputClockCount, chip.InputClockCount);
            for (var channel = 0; channel < 2; channel++)
            {
                Assert.Equal(Divider(reference).GetOutputs(channel), Divider(card).GetOutputs(channel));
                Assert.Equal(Divider(reference).GetInputClockCount(channel), Divider(card).GetInputClockCount(channel));
                Assert.Equal(Divider(reference).GetQaRisingClockCount(channel), Divider(card).GetQaRisingClockCount(channel));
            }
        }
        Advance(4_000);
        var selected = (byte)((drive == 0 ? H.Drive0Select : H.Drive1Select) | H.OutputLoadHead);
        WriteSpecial(chip, H.DriveOutput, selected); WriteSpecial(referenceChip, H.DriveOutput, selected);
        Advance(6_000);
        foreach (var (time, level) in new (long, bool)[] { (10_000, false), (20_000, true), (30_000, false), (333_000_000, true), (333_001_000, false) })
        {
            Advance(time); card.SetIndexLevel(level); reference.SetIndexLevel(level); Advance(time + 100);
        }
        WriteSpecial(chip, H.DriveOutput, 0); WriteSpecial(referenceChip, H.DriveOutput, 0);
        Advance(333_003_000);
        chip.Write(0, H.DriveStatus); referenceChip.Write(0, H.DriveStatus);
        Advance(333_005_000);
        Assert.Equal(referenceChip.Read(0), chip.Read(0)); Assert.Equal(referenceChip.Read(1), chip.Read(1));
        Assert.False(drive == 0 ? card.Ready0 : card.Ready1);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(6)]
    [InlineData(26)]
    [InlineData(30)]
    [InlineData(108)]
    [InlineData(500)]
    [InlineData(1700)]
    public void LatchedSeparatorDeferredInputsPreserveEveryWindowDateAcrossPulsePhases(int width)
    {
        for (var phase = 0; phase < 16_000; phase += 37)
        {
            var chip = new Fdc8271(2_000_000); var referenceChip = new Fdc8271(2_000_000);
            using var card = new SystemFloppyBoard(chip);
            using var reference = new SystemFloppyBoard(referenceChip, groupStableSeparator: false);
            var actual = new List<(long, bool?)>(); var expected = new List<(long, bool?)>();
            static void Capture(SystemFloppyBoard board, List<(long, bool?)> trace)
            {
                var latch = (RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS00.Services.Sn74Ls00)
                    typeof(SystemFloppyBoard).GetField("_ic12", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(board)!;
                var wire = latch.OutputChanged;
                latch.OutputChanged = (gate, high) =>
                {
                    if (gate == 2) trace.Add((board.TimeNanoseconds, high));
                    wire?.Invoke(gate, high);
                };
            }
            Capture(card, actual); Capture(reference, expected);
            var clock = new ClockWire(card, periodic: true); var scalar = new ClockWire(reference, periodic: true);
            void Advance(long at)
            {
                try { clock.AdvanceTo(at); }
                catch (Exception error) { throw new InvalidOperationException($"candidate width={width}, phase={phase}, at={at}", error); }
                try { scalar.AdvanceTo(at); }
                catch (Exception error) { throw new InvalidOperationException($"reference width={width}, phase={phase}, at={at}", error); }
            }
            foreach (var (time, high) in new (long, bool)[]
                { (30_000 + phase, false), (30_000 + phase + width, true), (60_000 + phase, false), (60_500 + phase, true) })
            {
                Advance(time);
                card.SetReadDataLevel(high); reference.SetReadDataLevel(high);
                if (!high)
                {
                    static byte Pins(SystemFloppyBoard board) => ((RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS93.Services.Sn74Ls93)
                        typeof(SystemFloppyBoard).GetField("_ic7", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(board)!).Outputs;
                    Assert.True((Pins(card) & 10) == (Pins(reference) & 10), $"Counter QB/QD differ: phase={phase}, time={time}, actual={Pins(card)}, expected={Pins(reference)}");
                }
            }
            Advance(90_000);
            card.SetResetLine(false); reference.SetResetLine(false);
            Advance(90_100);
            card.SetResetLine(true); reference.SetResetLine(true);
            Advance(100_000);
            Assert.True(expected.SequenceEqual(actual), $"width={width}, phase={phase}: expected {string.Join(';', expected)}, actual {string.Join(';', actual)}");
            Assert.Equal(reference.DataWindow, card.DataWindow);
            Assert.Equal(referenceChip.InputClockCount, chip.InputClockCount);
            Assert.Equal(referenceChip.Nmi, chip.Nmi);
        }
    }

    [Fact]
    public void IndependentCpuQualificationTracksTheControllersNotifiedTimingState()
    {
        var controller = new Fdc8271(2_000_000);
        using var card = new SystemFloppyBoard(controller);
        var clock = new ClockWire(card, periodic: true);
        Assert.Equal(!controller.NextClockEvent.HasValue, card.CpuClockCanRunIndependently);
        clock.AdvanceTo(10_000);
        Assert.Equal(!controller.NextClockEvent.HasValue, card.CpuClockCanRunIndependently);
        controller.Write(0, 0x52); controller.Write(1, 0); controller.Write(1, 1);
        Assert.Equal(!controller.NextClockEvent.HasValue, card.CpuClockCanRunIndependently);
        for (var time = 10_500L; time <= 50_000; time += 500)
        {
            clock.AdvanceTo(time);
            Assert.Equal(!controller.NextClockEvent.HasValue, card.CpuClockCanRunIndependently);
        }
        Assert.Equal(long.MaxValue, card.NextDriveInteractionNanoseconds);
    }

    [Fact]
    public void DescribedSeparatorClockPreservesAllDownstreamEdgesAndResetDates()
    {
        var chip = new Fdc8271(2_000_000); var referenceChip = new Fdc8271(2_000_000);
        // This diagnostic observes the internal QB/QD wires as well as the
        // external latch. Retain their full transport for both clock sources.
        using var card = new SystemFloppyBoard(chip, groupStableSeparator: false);
        using var reference = new SystemFloppyBoard(referenceChip, groupStableSeparator: false);
        var actual = new List<(long, byte, byte)>(); var expected = new List<(long, byte, byte)>();
        var actualNmi = new List<(long, bool?)>(); var expectedNmi = new List<(long, bool?)>();
        static RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS93.Services.Sn74Ls93 Counter(SystemFloppyBoard owner, string name)
            => (RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS93.Services.Sn74Ls93)typeof(SystemFloppyBoard)
                .GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(owner)!;
        void Capture(SystemFloppyBoard owner, List<(long, byte, byte)> trace)
        {
            var counter = Counter(owner, "_ic7"); var receiver = counter.OutputsChanged;
            counter.OutputsChanged = (levels, changed) =>
            {
                // QA is represented by its physical external wire. Compare
                // every scalar output still connected to the NAND latch.
                if ((changed & 10) != 0) trace.Add((owner.TimeNanoseconds, (byte)(levels & 10), (byte)(changed & 10)));
                receiver?.Invoke(levels, changed);
            };
        }
        Capture(card, actual); Capture(reference, expected);
        card.NmiChanged = high => actualNmi.Add((card.TimeNanoseconds, high));
        reference.NmiChanged = high => expectedNmi.Add((reference.TimeNanoseconds, high));
        var periodic = new ClockWire(card, periodic: true); var scalar = new ClockWire(reference);
        for (long time = 1; time <= 30_000; time++)
        {
            periodic.AdvanceTo(time); scalar.AdvanceTo(time);
            if (time is 2_001 or 2_109 or 5_253 or 5_259 or 11_500 or 11_526)
            {
                var high = time is 2_109 or 5_259 or 11_526;
                card.SetReadDataLevel(high); reference.SetReadDataLevel(high);
            }
            if (time is 17_001 or 19_013)
            {
                card.SetResetLine(time == 19_013); reference.SetResetLine(time == 19_013);
            }
            Assert.Equal(reference.DataWindow, card.DataWindow);
            Assert.Equal(referenceChip.InputClockCount, chip.InputClockCount);
        }
        Assert.Equal(expected, actual); Assert.Equal(expectedNmi, actualNmi);
        foreach (var name in new[] { "_ic14", "_ic7" })
        {
            var first = Counter(card, name); var second = Counter(reference, name);
            first.AdvanceTo(30_000); second.AdvanceTo(30_000);
            Assert.Equal(second.Outputs, first.Outputs);
            Assert.Equal(second.GetInputClockCount(0), first.GetInputClockCount(0));
            Assert.Equal(second.GetInputClockCount(1), first.GetInputClockCount(1));
        }
    }

    [Fact]
    public void IdleDividerRetainsAllQaClocksWithFarFewerConnectedWakeups()
    {
        var chip = new Fdc8271(2_000_000); var reference = new Fdc8271(2_000_000);
        using var card = new SystemFloppyBoard(chip); using var scalar = new SystemFloppyBoard(reference);
        UseScalarControllerClock(scalar, reference);
        var divider = Divider(card); var scalarDivider = Divider(scalar);
        var connected = 0; var internalEvents = 0;
        var next = divider.NextOutputEventNanosecondsChanged;
        var scalarNext = scalarDivider.NextEventNanosecondsChanged;
        divider.NextOutputEventNanosecondsChanged = at => { if (at.HasValue) connected++; next?.Invoke(at); };
        scalarDivider.NextEventNanosecondsChanged = at => { if (at.HasValue) internalEvents++; scalarNext?.Invoke(at); };
        new ClockWire(card).AdvanceTo(24_000); new ClockWire(scalar).AdvanceTo(24_000);
        Assert.Equal(reference.InputClockCount, chip.InputClockCount);
        Assert.Equal(scalarDivider.GetOutputs(0), divider.GetOutputs(0));
        Assert.Equal(scalarDivider.GetOutputs(1), divider.GetOutputs(1));
        Assert.Equal(scalarDivider.GetQaRisingClockCount(0), divider.GetQaRisingClockCount(0));
        Assert.True(connected * 4 < internalEvents, $"connected={connected}, internal={internalEvents}");
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(0, 2)]
    [InlineData(1, 2)]
    public void PhysicalDriveHeadsDetermineWhetherOp0SelectsTheOtherFluxSurface(int drive, int heads)
    {
        // A two-surface medium does not turn a one-head physical drive into a two-head drive.
        using var disk = new RetroMediaLab.Tests.Emulation.Intel8271.MemoryTrackDisk { Heads = 2 };
        var seed = disk.Sectors.Single();
        disk.WriteTrackWindow(0, 0, "iso.fm", 4000, 200_000_000, 0, 30_000, [2000, 8000, 16000]);
        // The fixture shares its decoded sector list. Restore a seed for the
        // second surface before replacing its raw window independently.
        disk.Sectors.Clear(); disk.Sectors.Add(seed with { Head = 1 });
        disk.WriteTrackWindow(0, 1, "iso.fm", 4000, 200_000_000, 0, 30_000, [6000, 20000]);
        var chip = new Fdc8271(2_000_000);
        var controller = System.Reflection.DispatchProxy.Create<
            RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Interfaces.IIntel8271, ReadInputProbe>();
        var probe = (ReadInputProbe)(object)controller;
        probe.Target = chip;
        using var card = new SystemFloppyBoard(controller);
        probe.Time = () => card.TimeNanoseconds;
        using var drives = new SystemFloppyDrive(card, drive == 0 ? [heads, 1] : [1, heads]);
        drives.Attach(drive, disk);
        var clock = new ClockWire(card);
        clock.AdvanceTo(1000);
        var select = drive == 0 ? H.Drive0Select : H.Drive1Select;
        WriteSpecial(chip, H.DriveOutput, (byte)(select | H.OutputLoadHead));
        clock.AdvanceTo(4000);
        WriteSpecial(chip, H.DriveOutput, (byte)(select | H.OutputLoadHead | H.OptionalOutput));
        clock.AdvanceTo(10_000);
        Assert.Equal(new long[] { 3011, heads == 2 ? 7011 : 9011 },
            probe.Edges.Where(edge => edge.Time > 2000 && edge.Asserted).Select(edge => edge.Time));
        clock.AdvanceTo(11000);
        WriteSpecial(chip, H.DriveOutput, (byte)(select | H.OutputLoadHead));
        clock.AdvanceTo(22000);
        Assert.Equal(new long[] { 3011, heads == 2 ? 7011 : 9011, 17011 },
            probe.Edges.Where(edge => edge.Time > 2000 && edge.Asserted).Select(edge => edge.Time));
        drives.Attach(drive, null);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void ChangingSurfaceWhileWritingClosesTheOldHeadBeforeOpeningTheOther(int drive)
    {
        using var disk = new RetroMediaLab.Tests.Emulation.Intel8271.MemoryTrackDisk { Heads = 2 };
        var chip = new Fdc8271(2_000_000);
        using var card = new SystemFloppyBoard(chip);
        using var drives = new SystemFloppyDrive(card, [2, 2]);
        drives.Attach(drive, disk);
        long motorTime = 0, sideTime = 0, gateTime = 0;
        card.DriveLinesChanged += (levels, _, changed) =>
        {
            var motor = (byte)SystemFloppyDriveLines.Motor;
            if ((changed & motor) != 0 && (levels & motor) == 0) motorTime = card.TimeNanoseconds;
            var side = (byte)SystemFloppyDriveLines.FaultReset;
            if ((changed & side) != 0 && (levels & side) == 0) sideTime = card.TimeNanoseconds;
            var gate = (byte)SystemFloppyDriveLines.WriteEnable;
            if ((changed & gate) != 0) gateTime = card.TimeNanoseconds;
        };
        var clock = new ClockWire(card);
        clock.AdvanceTo(1000);
        var select = drive == 0 ? H.Drive0Select : H.Drive1Select;
        var control = (byte)(select | H.OutputLoadHead | H.OutputWriteEnable);
        WriteSpecial(chip, H.DriveOutput, control);
        clock.AdvanceTo(2000);
        var writeStart = gateTime;
        clock.AdvanceTo(3000);
        WriteSpecial(chip, H.DriveOutput, (byte)(control | H.OptionalOutput));
        clock.AdvanceTo(4000);
        Assert.True(sideTime > writeStart);
        Assert.Equal(1, disk.WindowWrites);
        Assert.Equal(0, disk.LastWriteHead);
        Assert.Equal((uint)(sideTime - writeStart), disk.LastWriteDuration);
        Assert.True(drives.Writing(drive));
        WriteSpecial(chip, H.DriveOutput, (byte)(select | H.OutputLoadHead | H.OptionalOutput));
        clock.AdvanceTo(5000);
        Assert.Equal(2, disk.WindowWrites);
        Assert.Equal(1, disk.LastWriteHead);
        Assert.Equal((uint)(sideTime - motorTime), disk.LastWriteStart);
        Assert.Equal((uint)(gateTime - sideTime), disk.LastWriteDuration);
        Assert.Empty(disk.LastWritePulses);
        drives.Attach(drive, null);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void ContinuousWriteSpansIndexWithoutLosingTheFollowingWindow(int drive)
    {
        const uint revolution = 200_000_000;
        using var disk = new RetroMediaLab.Tests.Emulation.Intel8271.MemoryTrackDisk();
        var chip = new Fdc8271(2_000_000);
        using var card = new SystemFloppyBoard(chip);
        using var drives = new SystemFloppyDrive(card);
        drives.Attach(drive, disk);
        long motorTime = 0, gateTime = 0;
        card.DriveLinesChanged += (levels, _, changed) =>
        {
            var motor = (byte)SystemFloppyDriveLines.Motor;
            if ((changed & motor) != 0 && (levels & motor) == 0) motorTime = card.TimeNanoseconds;
            var gate = (byte)SystemFloppyDriveLines.WriteEnable;
            if ((changed & gate) != 0) gateTime = card.TimeNanoseconds;
        };
        var clock = new ClockWire(card);
        clock.AdvanceTo(1000);
        var select = drive == 0 ? H.Drive0Select : H.Drive1Select;
        WriteSpecial(chip, H.DriveOutput, (byte)(select | H.OutputLoadHead | H.OutputWriteEnable));
        clock.AdvanceTo(2000);
        Assert.True(drives.Writing(drive));
        Assert.Equal(motorTime, gateTime);
        var writeStart = gateTime;
        clock.AdvanceTo(motorTime + revolution + 1000);
        Assert.Equal(1, disk.WindowWrites);
        Assert.Equal(0, disk.LastWriteCylinder);
        Assert.Equal(0u, disk.LastWriteStart);
        Assert.Equal(revolution, disk.LastWriteDuration);
        Assert.Empty(disk.LastWritePulses);
        Assert.True(drives.Writing(drive));
        WriteSpecial(chip, H.DriveOutput, (byte)(select | H.OutputLoadHead));
        clock.AdvanceTo(motorTime + revolution + 2000);
        Assert.False(drives.Writing(drive));
        Assert.Equal(2, disk.WindowWrites);
        Assert.Equal(0u, disk.LastWriteStart);
        Assert.Equal((uint)(gateTime - writeStart - revolution), disk.LastWriteDuration);
        Assert.Empty(disk.LastWritePulses);
        drives.Attach(drive, null);
        Assert.Equal(2, disk.WindowWrites);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void StepDuringWriteClosesTheOldTrackAndContinuesAtTheSamePhase(int drive)
    {
        using var disk = new RetroMediaLab.Tests.Emulation.Intel8271.MemoryTrackDisk { Cylinders = 2 };
        var chip = new Fdc8271(2_000_000);
        using var card = new SystemFloppyBoard(chip);
        using var drives = new SystemFloppyDrive(card);
        drives.Attach(drive, disk);
        long motorTime = 0, gateTime = 0, stepTime = 0;
        card.DriveLinesChanged += (levels, _, changed) =>
        {
            var motor = (byte)SystemFloppyDriveLines.Motor;
            if ((changed & motor) != 0 && (levels & motor) == 0) motorTime = card.TimeNanoseconds;
            var gate = (byte)SystemFloppyDriveLines.WriteEnable;
            if ((changed & gate) != 0) gateTime = card.TimeNanoseconds;
            var step = (byte)SystemFloppyDriveLines.Step;
            if ((changed & step) != 0 && (levels & step) == 0) stepTime = card.TimeNanoseconds;
        };
        var clock = new ClockWire(card);
        clock.AdvanceTo(1000);
        var select = drive == 0 ? H.Drive0Select : H.Drive1Select;
        var control = (byte)(select | H.OutputLoadHead | H.OutputWriteEnable | H.OutputDirection);
        WriteSpecial(chip, H.DriveOutput, control);
        clock.AdvanceTo(2000);
        var writeStart = gateTime;
        Assert.True(drives.Writing(drive));
        clock.AdvanceTo(20_000); // Direction established before the STEP input changes.
        WriteSpecial(chip, H.DriveOutput, (byte)(control | H.OutputStep));
        clock.AdvanceTo(21_000);
        Assert.True(stepTime > writeStart);
        Assert.Equal(1, disk.WindowWrites);
        Assert.Equal(0, disk.LastWriteCylinder);
        Assert.Equal(0u, disk.LastWriteStart);
        Assert.Equal((uint)(stepTime - writeStart), disk.LastWriteDuration);
        Assert.Empty(disk.LastWritePulses);
        Assert.True(drives.Writing(drive));
        WriteSpecial(chip, H.DriveOutput, control);
        clock.AdvanceTo(22_000);
        Assert.Equal(1, disk.WindowWrites); // Releasing STEP does not move a second time.
        WriteSpecial(chip, H.DriveOutput, (byte)(control & ~H.OutputWriteEnable));
        clock.AdvanceTo(23_000);
        Assert.False(drives.Writing(drive));
        Assert.Equal(2, disk.WindowWrites);
        Assert.Equal(1, disk.LastWriteCylinder);
        Assert.Equal((uint)(stepTime - motorTime), disk.LastWriteStart);
        Assert.Equal((uint)(gateTime - stepTime), disk.LastWriteDuration);
        Assert.Empty(disk.LastWritePulses);
        drives.Attach(drive, null);
        Assert.Equal(2, disk.WindowWrites);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void DisposingTheDriveReleasesItsReadPulseBeforeAnotherRelayIsConnected(int drive)
    {
        using var disk = new RetroMediaLab.Tests.Emulation.Intel8271.MemoryTrackDisk();
        disk.WriteTrackWindow(0, 0, "iso.fm", 4000, 200_000_000, 0, 10_000, [2000]);
        var chip = new Fdc8271(2_000_000);
        var controller = System.Reflection.DispatchProxy.Create<
            RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Interfaces.IIntel8271, ReadInputProbe>();
        var probe = (ReadInputProbe)(object)controller;
        probe.Target = chip;
        using var card = new SystemFloppyBoard(controller);
        probe.Time = () => card.TimeNanoseconds;
        using var drives = new SystemFloppyDrive(card);
        drives.Attach(drive, disk);
        var clock = new ClockWire(card);
        clock.AdvanceTo(1000);
        var select = drive == 0 ? H.Drive0Select : H.Drive1Select;
        WriteSpecial(chip, H.DriveOutput, (byte)(select | H.OutputLoadHead));
        chip.Write(4, 0x5a);
        clock.AdvanceTo(3200);
        Assert.True(probe.Edges[^1].Asserted);
        drives.Dispose();
        Assert.Equal((3200L, false), probe.Edges[^1]);
        Assert.Equal(0x5a, chip.Read(4));
        using var replacement = new SystemFloppyDrive(card);
        replacement.Attach(drive, disk);
        clock.AdvanceTo(5200);
        Assert.Equal((5200L, true), probe.Edges[^1]);
        drives.Dispose(); // An old owner cannot release the new owner's pulse.
        Assert.Equal((5200L, true), probe.Edges[^1]);
        replacement.Attach(drive, null);
    }

    [Theory]
    [InlineData(250, 2)]
    [InlineData(500, 2)]
    [InlineData(750, 6)]
    public void ReadPulsesPublishOnlyTheResolvedPhysicalLevel(int spacing, int expectedEdges)
    {
        using var disk = new RetroMediaLab.Tests.Emulation.Intel8271.MemoryTrackDisk();
        disk.WriteTrackWindow(0, 0, "iso.fm", 4000, 200_000_000, 0, 10_000,
            [2000, (uint)(2000 + spacing), (uint)(2000 + 2 * spacing)]);
        var chip = new Fdc8271(2_000_000);
        var controller = System.Reflection.DispatchProxy.Create<
            RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Interfaces.IIntel8271, ReadInputProbe>();
        var probe = (ReadInputProbe)(object)controller;
        probe.Target = chip;
        using var card = new SystemFloppyBoard(controller);
        probe.Time = () => card.TimeNanoseconds;
        using var drives = new SystemFloppyDrive(card);
        drives.Attach(0, disk);
        var clock = new ClockWire(card);
        clock.AdvanceTo(1000);
        WriteSpecial(chip, H.DriveOutput, (byte)(H.Drive0Select | H.OutputLoadHead));
        clock.AdvanceTo(8000);
        var edges = probe.Edges.Where(edge => edge.Time > 2000).ToArray();
        Assert.Equal(expectedEdges, edges.Length);
        Assert.True(edges[0].Asserted);
        Assert.False(edges[^1].Asserted);
        Assert.Equal(2 * spacing + 500, edges[^1].Time - edges[0].Time);
        if (expectedEdges == 6)
            for (var pulse = 0; pulse < 3; pulse++)
            {
                Assert.True(edges[pulse * 2].Asserted);
                Assert.False(edges[pulse * 2 + 1].Asserted);
                Assert.Equal(500, edges[pulse * 2 + 1].Time - edges[pulse * 2].Time);
            }
        drives.Attach(0, null);
    }

    public class ReadInputProbe : System.Reflection.DispatchProxy
    {
        internal Fdc8271 Target = null!;
        internal Func<long> Time = static () => 0;
        internal List<(long Time, bool Asserted)> Edges { get; } = [];
        private bool? _level;
        protected override object? Invoke(System.Reflection.MethodInfo? method, object?[]? arguments)
        {
            if (method!.Name == "SetUnseparatedData")
            {
                var asserted = (bool)arguments![0]!;
                if (_level != asserted) { _level = asserted; Edges.Add((Time(), asserted)); }
            }
            return method.Invoke(Target, arguments);
        }
    }

    [Theory]
    [InlineData(0, false, false, false)]
    [InlineData(1, false, false, false)]
    [InlineData(0, false, false, true)]
    [InlineData(1, false, false, true)]
    [InlineData(0, true, true, false)]
    [InlineData(1, true, true, false)]
    [InlineData(0, true, false, false)]
    [InlineData(1, true, false, false)]
    public void MediaReceivesAnAlreadyAssertedWriteGateWithoutWaitingForAnotherWireChange(
        int drive, bool hasOldDisk, bool oldProtected, bool newProtected)
    {
        using var oldDisk = hasOldDisk ? new RetroMediaLab.Tests.Emulation.Intel8271.MemoryTrackDisk { IsReadOnly = oldProtected } : null;
        using var newDisk = new RetroMediaLab.Tests.Emulation.Intel8271.MemoryTrackDisk { IsReadOnly = newProtected };
        var chip = new Fdc8271(2_000_000);
        using var card = new SystemFloppyBoard(chip);
        using var drives = new SystemFloppyDrive(card);
        if (oldDisk is not null) drives.Attach(drive, oldDisk);
        var clock = new ClockWire(card);
        clock.AdvanceTo(1000);
        var select = drive == 0 ? H.Drive0Select : H.Drive1Select;
        WriteSpecial(chip, H.DriveOutput, (byte)(select | H.OutputLoadHead | H.OutputWriteEnable));
        clock.AdvanceTo(2000);
        Assert.True(drives.Selected(drive));
        Assert.True(drives.Motor(drive));
        Assert.Equal(hasOldDisk && !oldProtected, drives.Writing(drive));
        var insertionTime = card.TimeNanoseconds;
        drives.Attach(drive, newDisk);
        Assert.Equal(!newProtected, drives.Writing(drive));
        if (oldDisk is not null) Assert.Equal(oldProtected ? 0 : 1, oldDisk.WindowWrites);
        long releaseTime = 0;
        card.DriveLinesChanged += (levels, _, changed) =>
        {
            var gate = (byte)SystemFloppyDriveLines.WriteEnable;
            if ((changed & gate) != 0 && (levels & gate) != 0) releaseTime = card.TimeNanoseconds;
        };
        clock.AdvanceTo(3000);
        WriteSpecial(chip, H.DriveOutput, (byte)(select | H.OutputLoadHead));
        clock.AdvanceTo(4000);
        Assert.True(releaseTime > insertionTime);
        Assert.False(drives.Writing(drive));
        Assert.Equal(newProtected ? 0 : 1, newDisk.WindowWrites);
        if (!newProtected)
        {
            Assert.Equal((uint)(releaseTime - insertionTime), newDisk.LastWriteDuration);
            Assert.Empty(newDisk.LastWritePulses); // No data pulse was needed to begin this window.
        }
        drives.Attach(drive, null);
    }

    [Fact]
    public void ReceivedDeadlinesPreserveTheSelectedTieUntilAnOldMinimumIsRenewed()
    {
        using var card = new SystemFloppyBoard(new Fdc8271(2_000_000));
        card.AdvanceTo(1000);
        Assert.Null(card.NextEventNanoseconds);
        var notify = typeof(SystemFloppyBoard).GetMethod("RememberDeadline",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .CreateDelegate<Action<int, long?>>(card);
        var selected = typeof(SystemFloppyBoard).GetField("_nextPart",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        notify(14, 1200);
        notify(7, 1200);
        Assert.Equal(14, selected.GetValue(card)); // A newly armed equal date does not preempt.
        notify(8, 1300);
        notify(14, 1400);
        Assert.Equal(7, selected.GetValue(card));
        notify(14, null);
        Assert.Equal(1200, card.NextEventNanoseconds);
        notify(7, null);
        Assert.Equal(8, selected.GetValue(card));
        notify(8, null);
        Assert.Null(card.NextEventNanoseconds);

        // Independent reference of received owners, including equal dates,
        // cancellation, duplicate notifications and replacement of a non-head.
        var received = new Dictionary<int, long>();
        long? expected = null;
        var expectedPart = 0;
        var actual = new List<long?>();
        var published = new List<long?>();
        card.NextEventNanosecondsChanged = actual.Add;
        var random = new Random(74193);
        int[] parts = [2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 14, 15];
        for (var step = 0; step < 10000; step++)
        {
            var part = parts[random.Next(parts.Length)];
            var previous = received.GetValueOrDefault(part);
            var oldMinimum = expected;
            long? next = random.Next(4) == 0 ? null : 1100 + random.Next(8);
            if (next.HasValue) received[part] = next.Value;
            else received.Remove(part);
            if (next.HasValue && (!expected.HasValue || next < expected))
            { expected = next; expectedPart = part; }
            if (previous != 0 && previous == oldMinimum)
            {
                var minimum = received.OrderBy(pair => pair.Value).ThenBy(pair => pair.Key).FirstOrDefault();
                expected = minimum.Key == 0 ? null : minimum.Value;
                expectedPart = minimum.Key;
            }
            if (oldMinimum != expected) published.Add(expected);
            notify(part, next);
            Assert.Equal(expected, card.NextEventNanoseconds);
            Assert.Equal(expectedPart, selected.GetValue(card));
        }
        Assert.Equal(published, actual);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void ReceivedDriveDeadlinesKeepTheSeparateWireChronology(int drive)
    {
        using var disk = new RetroMediaLab.Tests.Emulation.Intel8271.MemoryTrackDisk(fill: 0x5a);
        using var referenceDisk = new RetroMediaLab.Tests.Emulation.Intel8271.MemoryTrackDisk(fill: 0x5a);
        var chip = new Fdc8271(2_000_000); var referenceChip = new Fdc8271(2_000_000);
        using var card = new SystemFloppyBoard(chip);
        using var referenceCard = new SystemFloppyBoard(referenceChip);
        UseScalarControllerClock(referenceCard, referenceChip);
        using var drives = new SystemFloppyDrive(card);
        using var referenceDrives = new SystemFloppyDrive(referenceCard);
        // Reproduce the previous external wiring loop as an independent reference.
        referenceCard.DisconnectDriveTiming(referenceDrives);
        drives.Attach(drive, disk); referenceDrives.Attach(drive, referenceDisk);
        var clock = new ClockWire(card);
        var referenceClock = new ClockWire(referenceCard, referenceDrives);
        var edges = ObserveWindow(card); var referenceEdges = ObserveWindow(referenceCard);
        var nmi = new List<(long Time, bool? Level)>(); var referenceNmi = new List<(long Time, bool? Level)>();
        card.NmiChanged = level => nmi.Add((card.TimeNanoseconds, level));
        referenceCard.NmiChanged = level => referenceNmi.Add((referenceCard.TimeNanoseconds, level));
        var select = drive == 0 ? H.Drive0Select : H.Drive1Select;
        Advance(1000);
        WriteSpecial(chip, H.DriveOutput, (byte)(select | H.OutputLoadHead));
        WriteSpecial(referenceChip, H.DriveOutput, (byte)(select | H.OutputLoadHead));
        Advance(401_000_000);
        Assert.True(drive == 0 ? card.Ready0 : card.Ready1);
        foreach (var controller in new[] { chip, referenceChip })
        {
            controller.Write(0, (byte)(H.DriveStatus | select)); _ = controller.Read(1);
            controller.Write(0, 0x35);
            foreach (var parameter in new byte[] { 0x0d, 1, 0, 0x10 }) controller.Write(1, parameter);
            controller.Write(0, (byte)(H.ReadSingle | select)); controller.Write(1, 0); controller.Write(1, 1);
        }
        var count = 0;
        for (var time = card.TimeNanoseconds + 500; time < 650_000_000; time += 500)
        {
            Advance(time);
            Assert.Equal(referenceChip.Drq, chip.Drq);
            if (chip.Drq) { Assert.Equal(referenceChip.Read(4), chip.Read(4)); count++; }
            Assert.Equal(referenceChip.Read(0), chip.Read(0));
            if ((chip.Read(0) & H.ResultFull) != 0) break;
        }
        Assert.Equal(128, count);
        Assert.Equal(0, chip.Read(1)); Assert.Equal(0, referenceChip.Read(1));
        Assert.Equal(referenceEdges, edges); Assert.Equal(referenceNmi, nmi);
        drives.Attach(drive, null); referenceDrives.Attach(drive, null);
        Advance(card.TimeNanoseconds + 13_579);
        Assert.Equal(referenceCard.Ready0, card.Ready0); Assert.Equal(referenceCard.Ready1, card.Ready1);
        Assert.Equal(referenceChip.InputClockCount, chip.InputClockCount);

        void Advance(long target) { clock.AdvanceTo(target); referenceClock.AdvanceTo(target); }
        static List<(long Time, bool? Level)> ObserveWindow(SystemFloppyBoard board)
        {
            var latch = (RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS00.Services.Sn74Ls00)
                typeof(SystemFloppyBoard).GetField("_ic12", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(board)!;
            var previous = latch.OutputChanged;
            var trace = new List<(long Time, bool? Level)>();
            latch.OutputChanged = (gate, level) =>
            {
                previous!(gate, level);
                if (gate == 2) trace.Add((board.TimeNanoseconds, level));
            };
            return trace;
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DriveConnectorProducesReadyAndReadsAWholeSectorThroughTheRealSeparator(bool periodic)
    {
        using var disk = new RetroMediaLab.Tests.Emulation.Intel8271.MemoryTrackDisk(fill: 0x5a);
        var chip = new Fdc8271(2_000_000);
        using var card = new SystemFloppyBoard(chip);
        using var drives = new SystemFloppyDrive(card);
        drives.Attach(0, disk);
        var clock = new ClockWire(card, periodic: periodic);
        clock.AdvanceTo(1000);
        WriteSpecial(chip, H.DriveOutput, (byte)(H.Drive0Select | H.OutputLoadHead));
        clock.AdvanceTo(401_000_000);
        Assert.True(card.Ready0);
        chip.Write(0, 0x6c); _ = chip.Read(1); // Clear the power-on not-ready latch as the OS does.
        chip.Write(0, 0x35);
        foreach (var parameter in new byte[] { 0x0d, 1, 0, 0x10 }) chip.Write(1, parameter);
        chip.Write(0, 0x52); chip.Write(1, 0); chip.Write(1, 1);
        var bytes = new List<byte>();
        for (var time = card.TimeNanoseconds + 500; time < 650_000_000; time += 500)
        {
            clock.AdvanceTo(time);
            if (chip.Drq) bytes.Add(chip.Read(4));
            if ((chip.Read(0) & H.ResultFull) != 0) break;
        }
        var result = chip.Read(1);
        Assert.True(bytes.Count == 128, $"Read {bytes.Count} bytes; result={result:X2}, time={card.TimeNanoseconds}, ready={card.Ready0}");
        Assert.All(bytes, value => Assert.Equal(0x5a, value));
        Assert.Equal(0, result);
        chip.Write(0, 0x4a); chip.Write(1, 0); chip.Write(1, 1);
        var written = 0;
        var limit = card.TimeNanoseconds + 250_000_000;
        for (var time = card.TimeNanoseconds + 500; time < limit; time += 500)
        {
            clock.AdvanceTo(time);
            if (chip.Drq) { chip.Write(4, 0xa6); written++; }
            if ((chip.Read(0) & H.ResultFull) != 0) break;
        }
        Assert.Equal(128, written);
        Assert.Equal(0, chip.Read(1));
        Assert.True(disk.WindowWrites > 0);
        chip.Write(0, 0x52); chip.Write(1, 0); chip.Write(1, 1);
        bytes.Clear(); limit = card.TimeNanoseconds + 250_000_000;
        for (var time = card.TimeNanoseconds + 500; time < limit; time += 500)
        {
            clock.AdvanceTo(time);
            if (chip.Drq) bytes.Add(chip.Read(4));
            if ((chip.Read(0) & H.ResultFull) != 0) break;
        }
        Assert.Equal(0, chip.Read(1));
        Assert.Equal(128, bytes.Count);
        Assert.All(bytes, value => Assert.Equal(0xa6, value));
        chip.Write(4, 0xa5);
        drives.Attach(0, null);
        Assert.False(chip.ResetAsserted);
        Assert.Equal(0xa5, chip.Read(4));
    }

    [Fact]
    public void FastMachineCpuUsesTheDecodedDataMirrorsAndThePhysicalBusStrobes()
    {
        var rom = new byte[8192]; rom[^3] = 2; rom[^1] = 2;
        using var machine = new AcornSystemMachine(
            RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Enums.SystemCpuCard.Fast6502A,
            rom, new byte[2048], [], true);
        byte[] program = [0xa9, 0x5a, 0x8d, 0x47, 0x0a, 0xad, 0x44, 0x0a, 0x8d, 0x00, 0x03, 0x4c, 0x0b, 0x02];
        for (var index = 0; index < program.Length; index++) machine.Write((ushort)(0x200 + index), program[index]);
        for (var cycle = 0; cycle < 50; cycle++) machine.Tick();
        Assert.Equal(0x5a, machine.Read(0x300));
    }

    [Fact]
    public void RealBufferLevelsAndDeselectionSettleWithoutAnInventedControllerClock()
    {
        var chip = new Fdc8271(2_000_000);
        using var card = new SystemFloppyBoard(chip);
        card.AdvanceTo(1000);
        Assert.Equal(byte.MaxValue, card.DriveKnown);
        Assert.Equal(byte.MaxValue, card.DriveLevels);
        Assert.False(card.Nmi); Assert.False(card.Ready0); Assert.False(card.Ready1);
        Assert.False(chip.ResetAsserted);
        Assert.Equal(0, chip.InputClockCount); // An elapsed timestamp is not an oscillator.
    }

    [Fact]
    public void ExternalFourMhzReceivesTheDividerAndKeepsClockingDuringHardwareReset()
    {
        var chip = new Fdc8271(2_000_000);
        using var card = new SystemFloppyBoard(chip);
        var clock = new ClockWire(card);
        clock.AdvanceTo(4000);
        Assert.Equal(8, chip.InputClockCount);
        card.SetResetLine(false);
        clock.AdvanceTo(4008); Assert.False(chip.ResetAsserted);
        clock.AdvanceTo(4009); Assert.True(chip.ResetAsserted);
        clock.AdvanceTo(10_000);
        Assert.Equal(20, chip.InputClockCount);
        card.SetResetLine(true);
        clock.AdvanceTo(10_009); Assert.True(chip.ResetAsserted);
        clock.AdvanceTo(10_010); Assert.False(chip.ResetAsserted);
    }

    [Fact]
    public void DecoderAndDackCoverTheWholePhysicalWindowAndItsRegisterMirrors()
    {
        var chip = new Fdc8271(2_000_000);
        using var card = new SystemFloppyBoard(chip);
        for (var address = 0; address < 0x1000; address++)
        {
            card.SetBusAddress((ushort)address);
            card.AdvanceTo(card.TimeNanoseconds + 100);
            var selected = address is >= 0x0a00 and <= 0x0a7f;
            Assert.Equal(selected, card.ChipSelected);
            Assert.Equal(selected && (address & 4) != 0, card.DataAcknowledged);
        }
        foreach (var address in new ushort[] { 0x1a00, 0xea00, 0xfa04 })
        {
            card.SetBusAddress(address); card.AdvanceTo(card.TimeNanoseconds + 100);
            Assert.False(card.ChipSelected); Assert.False(card.DataAcknowledged);
        }
    }

    [Fact]
    public void MirroredRegisterWriteUsesTheBusBufferAndThePhysicalClock()
    {
        var chip = new Fdc8271(2_000_000);
        using var card = new SystemFloppyBoard(chip);
        var clock = new ClockWire(card);
        card.SetBusAddress(0x0a20, H.DriveStatus);
        clock.AdvanceTo(100);
        card.SetBusStrobes(true, false);
        card.SetBusStrobes(true, true);
        Assert.NotEqual(0, chip.Read(0) & H.CommandFull);
        clock.AdvanceTo(1000);
        Assert.Equal(0, chip.Read(0) & H.CommandFull);
    }

    [Fact]
    public void DackIgnoresBothLowAddressBitsForTheFourDataAliases()
    {
        var chip = new Fdc8271(2_000_000);
        using var card = new SystemFloppyBoard(chip);
        card.SetBusAddress(0x0a47, 0x5a); card.AdvanceTo(100);
        card.SetBusStrobes(true, false); card.SetBusStrobes(true, true);
        foreach (var address in new ushort[] { 0x0a04, 0x0a05, 0x0a06, 0x0a07, 0x0a7f })
        {
            card.SetBusAddress(address); card.AdvanceTo(card.TimeNanoseconds + 100);
            card.SetBusStrobes(false, true);
            Assert.Equal((byte?)0x5a, card.BusData);
            card.SetBusStrobes(true, true);
            Assert.Null(card.BusData);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void ReadyUsesTwoIndexFrontsAndDeselectionTravelsThroughBothFlipFlops(int drive)
    {
        var chip = new Fdc8271(2_000_000);
        using var card = new SystemFloppyBoard(chip);
        var clock = new ClockWire(card);
        clock.AdvanceTo(1000);
        WriteSpecial(chip, H.DriveOutput, (byte)((drive == 0 ? H.Drive0Select : H.Drive1Select) | H.OutputLoadHead));
        clock.AdvanceTo(2000);
        Assert.False(drive == 0 ? card.Ready0 : card.Ready1);
        card.SetIndexLevel(false); clock.AdvanceTo(3000);
        Assert.False(drive == 0 ? card.Ready0 : card.Ready1); // First capture releases the second RESET.
        card.SetIndexLevel(true); clock.AdvanceTo(4000);
        card.SetIndexLevel(false); clock.AdvanceTo(5000);
        Assert.True(drive == 0 ? card.Ready0 : card.Ready1);
        Assert.False(drive == 0 ? card.Ready1 : card.Ready0);
        WriteSpecial(chip, H.DriveOutput, 0);
        clock.AdvanceTo(6000);
        Assert.False(drive == 0 ? card.Ready0 : card.Ready1);
    }

    [Fact]
    public void TimeoutIsCountedByTheActualDividerAndSampledOnlyAtTheNextIndexFront()
    {
        var chip = new Fdc8271(2_000_000);
        using var card = new SystemFloppyBoard(chip);
        var clock = new ClockWire(card);
        clock.AdvanceTo(1000);
        WriteSpecial(chip, H.DriveOutput, (byte)(H.Drive0Select | H.OutputLoadHead));
        clock.AdvanceTo(2000);
        card.SetIndexLevel(false); clock.AdvanceTo(3000);
        card.SetIndexLevel(true); clock.AdvanceTo(4000);
        card.SetIndexLevel(false); clock.AdvanceTo(5000);
        Assert.True(card.Ready0);
        card.SetIndexLevel(true);
        clock.AdvanceTo(263_005_000);
        Assert.True(card.Ready0); // Counter changed D, not the retained Q output.
        card.SetIndexLevel(false);
        clock.AdvanceTo(263_005_300); Assert.True(card.Ready0);
        clock.AdvanceTo(263_006_000); Assert.False(card.Ready0);
    }

    [Fact]
    public void InterruptTravelsThroughTheOpenCollectorBufferBeforeReachingNmi()
    {
        var chip = new Fdc8271(2_000_000);
        using var card = new SystemFloppyBoard(chip);
        card.AdvanceTo(1000);
        chip.Write(0, 0x3f); // Unsupported command completes with a real controller interrupt.
        Assert.True(chip.Nmi);
        card.AdvanceTo(1010); Assert.False(card.Nmi);
        card.AdvanceTo(1011); Assert.True(card.Nmi);
        chip.Read(1);
        card.AdvanceTo(1024); Assert.True(card.Nmi);
        card.AdvanceTo(1025); Assert.False(card.Nmi);
    }

    [Fact]
    public void FaultResetIsAnOutputAndDoesNotDriveTheSeparatePulledUpFaultInput()
    {
        var chip = new Fdc8271(2_000_000);
        using var card = new SystemFloppyBoard(chip);
        card.AdvanceTo(1000);
        card.SetDriveSensorLevels(false, false);
        WriteSpecial(chip, H.DriveOutput, (byte)H.OptionalOutput);
        card.AdvanceTo(1011);
        Assert.Equal(0, card.DriveLevels & 128);
        chip.Write(0, H.ReadSpecial); chip.Write(1, H.DriveInput);
        var inputs = chip.Read(1);
        Assert.NotEqual(0, inputs & H.InputTrackZero);
        Assert.NotEqual(0, inputs & H.InputWriteProtect);
        Assert.Equal(0, inputs & H.InputFault);
    }

    [Fact]
    public void AContinuousLowIndexDoesNotResetTheControllerOrEraseItsData()
    {
        var chip = new Fdc8271(2_000_000);
        using var card = new SystemFloppyBoard(chip);
        var clock = new ClockWire(card);
        clock.AdvanceTo(1000);
        chip.Write(4, 0x5a);
        card.SetIndexLevel(false); // Actual no-disc level, not a reset command.
        clock.AdvanceTo(10_000);
        Assert.False(chip.ResetAsserted);
        Assert.Equal(0x5a, chip.Read(4));
        Assert.Equal(20, chip.InputClockCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DataPulseResetsTheRealCountersAndTheWindowLatchesAfterTheFiveMicrosecondBoundary(bool periodic)
    {
        var chip = new Fdc8271(2_000_000);
        using var card = new SystemFloppyBoard(chip);
        var clock = new ClockWire(card, periodic: periodic);
        clock.AdvanceTo(10_000);
        Assert.True(card.DataWindow);
        card.SetReadDataLevel(false);
        clock.AdvanceTo(10_018); Assert.True(card.DataWindow);
        clock.AdvanceTo(10_019); Assert.False(card.DataWindow);
        clock.AdvanceTo(10_100); card.SetReadDataLevel(true);
        clock.AdvanceTo(14_500); Assert.False(card.DataWindow);
        clock.AdvanceTo(16_000); Assert.True(card.DataWindow);
        clock.AdvanceTo(30_000); Assert.True(card.DataWindow); // Latch retains it beyond counter wrap.
        card.SetReadDataLevel(false);
        clock.AdvanceTo(30_100); Assert.False(card.DataWindow);
    }

    [Fact]
    public void DisposalDetachesOnlyOwnedConnectionsAndLeavesTheBorrowedControllerUsable()
    {
        var chip = new Fdc8271(2_000_000);
        var originalCalls = 0;
        Action<bool> original = _ => originalCalls++;
        chip.NmiChanged = original;
        var card = new SystemFloppyBoard(chip);
        try
        {
            card.AdvanceTo(1000);
            card.Dispose(); card.Dispose();
            Assert.Same(original, chip.NmiChanged);
            chip.NmiChanged!(true); Assert.Equal(1, originalCalls);
            chip.Write(4, 0xa5); Assert.Equal(0xa5, chip.Read(4));
            Assert.Throws<ObjectDisposedException>(() => card.AdvanceTo(2000));
        }
        finally { card.Dispose(); }
    }

    private static void WriteSpecial(Fdc8271 chip, byte register, byte value)
    {
        chip.Write(0, H.WriteSpecial); chip.Write(1, register); chip.Write(1, value);
    }

    private static RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS393.Services.Sn74Ls393 Divider(SystemFloppyBoard card)
        => (RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS393.Services.Sn74Ls393)typeof(SystemFloppyBoard)
            .GetField("_ic8", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(card)!;

    // Restore the preceding scalar transport as a reference without changing
    // any chip, input waveform or physical delay. Delegates are bound once.
    private static void UseScalarControllerClock(SystemFloppyBoard card, Fdc8271 chip)
    {
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        var clockChanged = typeof(SystemFloppyBoard).GetMethod("ControllerClockEventChanged", flags)!.CreateDelegate<Action<long?>>(card);
        chip.NextClockEventChanged -= clockChanged;
        var divider = Divider(card);
        divider.FirstQaClockReached = null; divider.RequestQaClock(0, null);
        divider.NextOutputEventNanosecondsChanged = null;
        var remember = typeof(SystemFloppyBoard).GetMethod("RememberDeadline", flags)!.CreateDelegate<Action<int, long?>>(card);
        divider.NextEventNanosecondsChanged = next => remember(8, next);
        var pins = divider.FirstOutputsChanged;
        divider.SetObservedOutputs(0, 9);
        divider.FirstOutputsChanged = (levels, changed) =>
        {
            pins?.Invoke(levels, changed);
            if ((changed & 1) != 0 && (levels & 1) != 0) chip.AdvanceTo(chip.InputClockCount + 1);
        };
    }

    /// <summary>External test source: its phase is independent of every chip's state.</summary>
    private sealed class ClockWire(SystemFloppyBoard card, SystemFloppyDrive? drives = null, bool periodic = false)
    {
        private long _next = card.TimeNanoseconds;
        private bool _high;
        private bool _connected;
        internal void AdvanceTo(long target)
        {
            if (periodic)
            {
                if (!_connected)
                {
                    card.SetClock4MHz(true);
                    card.ConnectClock4MHz(card.TimeNanoseconds + 125);
                    _connected = true;
                }
                AdvanceReceivers(target);
                return;
            }
            while (_next <= target)
            {
                AdvanceReceivers(_next);
                _high = !_high;
                card.SetClock4MHz(_high);
                _next += 125;
            }
            AdvanceReceivers(target);
        }

        private void AdvanceReceivers(long target)
        {
            if (drives is not null)
                while (Math.Min(card.NextEventNanoseconds ?? long.MaxValue, drives.NextEventNanoseconds) <= target)
                {
                    var next = Math.Min(card.NextEventNanoseconds ?? long.MaxValue, drives.NextEventNanoseconds);
                    card.AdvanceTo(next); drives.AdvanceTo(next);
                }
            card.AdvanceTo(target); drives?.AdvanceTo(target);
        }
    }
}
