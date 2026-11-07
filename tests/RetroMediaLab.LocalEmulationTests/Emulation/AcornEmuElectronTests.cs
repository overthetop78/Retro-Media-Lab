using RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Services;
using RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Contracts;
using RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Enums;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Functions;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornElectron.Common.Services;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Enums;

namespace RetroMediaLab.Tests.Emulation;

public sealed class AcornEmuElectronTests
{
    [Fact]
    public void TapeOnlyBoardDoesNotRequestUnsupportedDiskConfiguration()
    {
        RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Interfaces.IAcornMachine machine =
            new ElectronMachine(new byte[16384], new byte[16384]);
        try
        {
            Assert.Equal(0, machine.FloppyDriveCount);
            Assert.Equal(0, machine.HardDiskDriveCount);
            machine.RunFrame();
            Assert.NotEmpty(machine.DrainAudio());
        }
        finally { machine.EjectTape(); }
    }

    [Fact]
    public void KeyboardCombinesOnlySelectedLinesAndBothModifierKeys()
    {
        var keyboard = new ElectronKeyboard();
        keyboard.SetInput(EmulationInputSnapshot.Empty with { Keys = new HashSet<EmulationKey> { EmulationKey.Q, EmulationKey.X, EmulationKey.RightShift } });
        Assert.Equal(0, keyboard.Read(0x3fff));
        Assert.Equal(2, keyboard.Read(0x3fff ^ (1 << 12)));
        Assert.Equal(8, keyboard.Read(0x3fff ^ (1 << 11)));
        Assert.Equal(10, keyboard.Read(0x3fff ^ (1 << 11) ^ (1 << 12)));
        Assert.Equal(8, keyboard.Read(0x1fff));
    }

    [Fact]
    public void PowerOnStatusIsReadOnceAndClockIrqRequiresItsMask()
    {
        var ula = new ElectronUla(new ElectronVideo(_ => 0), new AnalogTape(2000000));
        Assert.Equal(0x82, ula.Read(0));
        Assert.Equal(0x80, ula.Read(0));
        for (var tick = 0; tick < 12800; tick++) ula.Tick();
        Assert.False(ula.Irq);
        ula.Write(0, 8);
        Assert.True(ula.Irq);
        Assert.Equal(0x89, ula.Read(0));
        ula.Write(5, 0x20);
        Assert.False(ula.Irq);
    }

    [Fact]
    public void TransmitterBecomesReadyAtStopRatherThanOnDataRegisterWrite()
    {
        var ula = new ElectronUla(new ElectronVideo(_ => 0), new AnalogTape(2000000));
        ula.Write(7, 4);
        ula.Tick();
        Assert.NotEqual(0, ula.Read(0) & 0x20);
        ula.Write(4, 0x55);
        ula.Tick();
        Assert.Equal(0, ula.Read(0) & 0x20);
        // FE04 is written after the initial boundary: start waits for 1665, stop begins at 16641.
        for (var tick = 2; tick < 16640; tick++) ula.Tick();
        Assert.Equal(0, ula.Read(0) & 0x20);
        ula.Tick();
        Assert.NotEqual(0, ula.Read(0) & 0x20);
    }

    [Fact]
    public void InternalRomPagesMustBeEscapedViaAnExternalHighPage()
    {
        var ula = new ElectronUla(new ElectronVideo(_ => 0), new AnalogTape(2000000));
        ula.Write(5, 10);
        ula.Write(5, 1);
        Assert.Equal(10, ula.RomPage);
        ula.Write(5, 12);
        ula.Write(5, 1);
        Assert.Equal(1, ula.RomPage);
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    [InlineData(4)] [InlineData(5)] [InlineData(6)] [InlineData(7)]
    public void VideoCompletesAFrameAndLeavesSpacedRastersBlack(int mode)
    {
        var video = new ElectronVideo(_ => 255);
        video.SetMode((byte)(mode << 3));
        for (var register = 0; register < 8; register++) video.WritePalette(register, 0);
        for (var tick = 0; tick < 39936; tick++) video.Tick();
        Assert.Equal(1, video.Frame);
        Assert.Equal(255, video.CompletedFrame[0]);
        var spaced = mode is 3 or 6;
        Assert.Equal(spaced ? 0 : 255, video.CompletedFrame[8 * 640 * 4]);
    }

    [Fact]
    public void MotorStopsTapePositionWithoutBlockingManualRewind()
    {
        var tape = new AnalogTape(2000000);
        tape.Insert(new(new short[4800], 48000), false);
        try
        {
            tape.ExecuteTape(EmulationTapeCommand.Play);
            for (var tick = 0; tick < 20000; tick++) tape.Tick(false);
            Assert.Equal(0, tape.Position);
            for (var tick = 0; tick < 20000; tick++) tape.Tick();
            Assert.Equal(480, tape.Position);
            tape.ExecuteTape(EmulationTapeCommand.Rewind);
            for (var tick = 0; tick < 20000; tick++) tape.Tick(false);
            Assert.Equal(0, tape.Position);
        }
        finally { tape.Eject(); }
    }

    [Fact]
    public void ModelUsesItsOwnHardwareProfileAndOnlyItsInstalledStorage()
    {
        var configuration = new MachineConfiguration(MachineModel.AcornElectron);
        Assert.Equal(Emulator.AcornEmu, configuration.Core);
        Assert.Equal(MachineFamily.AcornElectron, configuration.Family);
        Assert.Equal(32, ElectronFunctions.MemoryKib(configuration));
        Assert.Equal(EmulationMediaSlot.Tape0, Assert.Single(ElectronFunctions.Devices(configuration)).Slot);
        Assert.Equal(2, ElectronFunctions.Slots(configuration).Count);
        Assert.Equal(3, ElectronFunctions.Describe(configuration).Count);
    }
    private static T Chip<T>(ElectronMachine machine, string name) =>
        (T)typeof(ElectronMachine).GetField(name,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(machine)!;

    private static long Clocks(ElectronMachine machine) => Chip<long>(machine, "_ticks");

    private static long CpuRead(ElectronMachine machine, ushort address)
    {
        var before = Clocks(machine);
        machine.PrepareRead(address);
        machine.Tick();
        machine.Read(address);
        return Clocks(machine) - before;
    }

    private static long CpuWrite(ElectronMachine machine, ushort address, byte data)
    {
        var before = Clocks(machine);
        machine.PrepareWrite(address, data);
        machine.Tick();
        machine.Write(address, data);
        return Clocks(machine) - before;
    }

    [Fact]
    public void NativePreparedRamWriteOccursDuringItsFirstPhiPeriodAndIsNotRepeatedAtCompletion()
    {
        var machine = new ElectronMachine(new byte[16384], new byte[16384]);
        var ula = Chip<ElectronUla>(machine, "_ula");
        try
        {
            machine.Write(0xfe07, 0x20);
            while ((Clocks(machine) & 1) != 0) machine.Tick();
            var storage = Chip<byte[]>(machine, "_ram");
            List<(bool High, byte Value)> edges = [];
            ula.CpuClockChanged = (high, _) => edges.Add((high, storage[0x1234]));
            machine.PrepareWrite(0x1234, 0xff);
            machine.Tick();
            Assert.Equal(new[] { (false, (byte)0), (true, (byte)0x55) }, edges);
            Assert.Equal(0xff, storage[0x1234]);
            var before = Clocks(machine);
            machine.Write(0x1234, 0xff);
            Assert.Equal(before + 1, Clocks(machine));
            Assert.Equal(2, edges.Count);
            Assert.False(ula.CpuWaiting);
            machine.Tick();
            Assert.Equal((false, (byte)0xff), edges[2]);
        }
        finally
        {
            ula.CpuClockChanged = null;
            machine.EjectTape();
            machine.Reset(true);
        }
    }

    [Fact]
    public void NativeResetInhibitsAPreparedRamWriteBeforeItsFirstStrobe()
    {
        var machine = new ElectronMachine(new byte[16384], new byte[16384]);
        try
        {
            machine.Write(0xfe07, 0x20);
            var storage = Chip<byte[]>(machine, "_ram");
            machine.PrepareWrite(0x1234, 0xff);
            machine.SetInput(EmulationInputSnapshot.Empty with { Keys = new HashSet<EmulationKey> { EmulationKey.Break } });
            machine.Tick();
            Assert.Equal(0, storage[0x1234]);
            Assert.False(Chip<ElectronUla>(machine, "_ula").CpuWaiting);
            machine.Tick();
            Assert.Equal(0, storage[0x1234]);
        }
        finally
        {
            machine.EjectTape();
            machine.Reset(true);
        }
    }

    [Fact]
    public void NativeRamBusCoversEveryByteAndPreservesTheUnwrittenGroupAndNeighbours()
    {
        var machine = new ElectronMachine(new byte[16384], new byte[16384]);
        try
        {
            machine.Write(0xfe07, 0x20);
            var ula = Chip<ElectronUla>(machine, "_ula");
            var storage = Chip<byte[]>(machine, "_ram");
            ula.Nmi = true;
            for (var address = 0; address < 32768; address++)
                CpuWrite(machine, (ushort)address, (byte)(address ^ (address >> 8)));
            for (var address = 0; address < 32768; address++)
            {
                var expected = (byte)(address ^ (address >> 8));
                Assert.Equal(expected, storage[address]);
                machine.Tick();
                Assert.Equal(expected, machine.Read((ushort)address));
            }
            foreach (var address in new[] { 1, 0x0123, 0x5367, 0x7ffe })
            {
                var before = storage[address - 1];
                var after = storage[address + 1];
                for (var value = 0; value < 256; value++)
                {
                    CpuWrite(machine, (ushort)address, (byte)value);
                    machine.Tick();
                    Assert.Equal(value, machine.Read((ushort)address));
                    Assert.Equal(before, storage[address - 1]);
                    Assert.Equal(after, storage[address + 1]);
                }
            }
        }
        finally { machine.EjectTape(); machine.Reset(true); }
    }

    [Fact]
    public void NativeSlowAccessTakesTwoOrThreeReferenceClocksAndRomKeepsOne()
    {
        var machine = new ElectronMachine(new byte[16384], new byte[16384]);
        try
        {
            machine.Write(0xfe07, 0x20);
            while ((Clocks(machine) & 1) != 0) machine.Tick();
            Assert.Equal(2, CpuRead(machine, 0x0200));
            Assert.Equal(2, CpuRead(machine, 0x0201));
            Assert.Equal(1, CpuRead(machine, 0xc000));
            Assert.Equal(3, CpuRead(machine, 0x0202));
            Assert.Equal(2, CpuRead(machine, 0xfc00));
            Assert.Equal(1, CpuRead(machine, 0xc001));
            Assert.Equal(3, CpuWrite(machine, 0x0203, 42));
            Assert.Equal(2, CpuWrite(machine, 0x0204, 43));
            Assert.Equal(42, machine.Read(0x0203));
            Assert.Equal(43, machine.Read(0x0204));
        }
        finally { machine.EjectTape(); machine.Reset(true); }
    }

    [Fact]
    public void NativeNmiPriorityPersistsUntilFe05AndLeavesVideoRunning()
    {
        var machine = new ElectronMachine(new byte[16384], new byte[16384]);
        try
        {
            var ula = Chip<ElectronUla>(machine, "_ula");
            var video = Chip<ElectronVideo>(machine, "_video");
            var remaining = 40000;
            while ((video.Clock != 0 || video.Line != 0) && --remaining > 0) machine.Tick();
            Assert.True(remaining > 0);
            ula.Nmi = true;
            Assert.True(machine.Nmi);
            Assert.InRange(CpuRead(machine, 0x0200), 2L, 3L);
            ula.Nmi = false;
            Assert.False(machine.Nmi);
            Assert.InRange(CpuRead(machine, 0x0201), 2L, 3L);
            Assert.True(video.DisplayFetching);
            machine.Write(0xfe05, 0x80);
            Assert.False(ula.CpuRamPriority);
            Assert.True(CpuRead(machine, 0x0202) > 3);
            Assert.Equal(Clocks(machine) % 128, (long)video.Clock);
            Assert.False(video.DisplayFetching);
        }
        finally { machine.EjectTape(); machine.Reset(true); }
    }

    [Fact]
    public void NativeUlaUsesMeasuredIrqPositionsAndAlternatingFields()
    {
        var machine = new ElectronMachine(new byte[16384], new byte[16384]);
        try
        {
            var video = Chip<ElectronVideo>(machine, "_video");
            machine.Write(0xfe00, 0x0c);
            machine.Read(0xfe00);
            foreach (var (tick, cause) in new[] { (12670, 8), (32736, 4), (52670, 8), (72672, 4) })
            {
                while (Clocks(machine) < tick - 1) machine.Tick();
                Assert.False(machine.Irq);
                machine.Tick();
                Assert.True(machine.Irq);
                Assert.Equal(cause, machine.Read(0xfe00) & 0x0c);
                machine.Write(0xfe05, 0x30);
                Assert.False(machine.Irq);
            }
            while (Clocks(machine) < 80000) machine.Tick();
            Assert.Equal(2, video.Frame);
            Assert.Equal(0, video.Line);
            Assert.Equal(0, video.Clock);
        }
        finally { machine.EjectTape(); machine.Reset(true); }
    }


    [Fact]
    public void NativeBreakHoldsCpuUntilReleaseWithoutStoppingVideoOrTape()
    {
        var rom = new byte[16384];
        // INC $0200 / JMP $C000. No MOS register writes during reset or restart.
        new byte[] { 0xee, 0x00, 0x02, 0x4c, 0x00, 0xc0 }.CopyTo(rom, 0);
        rom[0x3ffc] = 0x00;
        rom[0x3ffd] = 0xc0;
        var machine = new ElectronMachine(rom, rom);
        try
        {
            machine.InsertTape(new EmulationTapeSignal(new short[48000], 48000));
            machine.TapeTransport.ExecuteTape(EmulationTapeCommand.Play);
            machine.Write(0xfe07, 0x62);
            machine.RunFrame();
            var cpu = Chip<RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Services.Cpu6502>(machine, "_cpu");
            var video = Chip<ElectronVideo>(machine, "_video");
            var tape = Chip<AnalogTape>(machine, "_tape");
            var program = cpu.PC;
            var stack = cpu.S;
            var data = machine.Read(0x0200);
            var position = tape.Position;
            var frame = video.Frame;
            machine.SetInput(EmulationInputSnapshot.Empty with { Keys = new HashSet<EmulationKey> { EmulationKey.Break } });
            Assert.True(machine.ResetRequested);
            machine.RunFrame();
            // Repeating a held snapshot must not execute another reset sequence.
            machine.SetInput(EmulationInputSnapshot.Empty with { Keys = new HashSet<EmulationKey> { EmulationKey.Break } });
            machine.RunFrame();
            Assert.Equal(program, cpu.PC);
            Assert.Equal(stack, cpu.S);
            Assert.Equal(data, machine.Read(0x0200));
            Assert.Equal(frame + 2, video.Frame);
            Assert.True(tape.Position > position);
            Assert.Equal(4, video.Mode);
            Assert.NotEmpty(machine.DrainAudio());
            machine.SetInput(EmulationInputSnapshot.Empty);
            Assert.False(machine.ResetRequested);
            cpu.Step();
            Assert.Equal(0xc000, cpu.PC);
            Assert.Equal((byte)(stack - 3), cpu.S);
            cpu.Step();
            Assert.Equal((byte)(data + 1), machine.Read(0x0200));
        }
        finally { machine.EjectTape(); machine.Reset(true); }
    }

    [Fact]
    public void NativeBreakPreservesUlaAndRecordedWaveformWhileCpuResetClocksContinue()
    {
        // NOP ROM: no MOS writes can conceal a hardware reset of the ULA.
        var rom = Enumerable.Repeat((byte)0xea, 16384).ToArray();
        var machine = new ElectronMachine(rom, rom);
        var reference = new ElectronMachine(rom, rom);
        try
        {
            foreach (var board in new[] { machine, reference })
            {
                board.InsertTape(new EmulationTapeSignal(ReadOnlyMemory<short>.Empty, 48000), true);
                board.TapeTransport.ExecuteTape(EmulationTapeCommand.Record);
                board.Write(0xfe00, 0x7c);
                board.Read(0xfe00);
                board.Write(0xfe05, 12);
                board.Write(0xfe05, 5);
                board.Write(0xfe07, 0xe4);
                board.Write(0xfe04, 0xa5);
                for (var clock = 0; clock < 4321; clock++) board.Tick();
            }
            machine.Reset(false);
            while (Clocks(reference) < Clocks(machine)) reference.Tick();
            var actualUla = Chip<ElectronUla>(machine, "_ula");
            var referenceUla = Chip<ElectronUla>(reference, "_ula");
            Assert.Equal(5, actualUla.RomPage);
            Assert.True(actualUla.Motor);
            Assert.True(actualUla.CapsLock);
            for (var clock = 0; clock < 20000; clock++)
            {
                machine.Tick();
                reference.Tick();
                Assert.Equal(referenceUla.Read(0), actualUla.Read(0));
            }
            Assert.Equal(reference.RecordedTape.Samples.ToArray(), machine.RecordedTape.Samples.ToArray());
            Assert.Equal(reference.DrainAudio(), machine.DrainAudio());
        }
        finally
        {
            machine.EjectTape();
            machine.Reset(true);
            reference.EjectTape();
            reference.Reset(true);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NativeTapeRecordsUlaWaveformAndReceivesItsPcmAgain(bool zeroCrossings)
    {
        var machine = new ElectronMachine(new byte[16384], new byte[16384]);
        try
        {
            machine.InsertTape(new EmulationTapeSignal(ReadOnlyMemory<short>.Empty, 48000), true);
            machine.TapeTransport.ExecuteTape(EmulationTapeCommand.Record);
            machine.Write(0xfe07, 0x44);
            for (var clock = 0; clock < 20 * 1664; clock++) machine.Tick();
            machine.Write(0xfe04, 0xa5);
            for (var clock = 0; clock < 22 * 1664; clock++) machine.Tick();
            machine.TapeTransport.ExecuteTape(EmulationTapeCommand.Stop);
            Assert.True(machine.TapeHasChanges);
            var signal = machine.RecordedTape;
            Assert.True(signal.Samples.Length > 1000);
            Assert.Contains((short)16000, signal.Samples.ToArray());
            Assert.Contains((short)-16000, signal.Samples.ToArray());
            if (zeroCrossings)
            {
                var samples = signal.Samples.ToArray();
                for (var sample = samples.Length - 1; sample > 0; sample--)
                    if (samples[sample] != samples[sample - 1]) samples[sample] = 0;
                signal = new EmulationTapeSignal(samples, signal.SampleRate);
            }
            machine.InsertTape(signal);
            machine.Write(0xfe07, 0x40);
            machine.Read(0xfe04);
            machine.TapeTransport.ExecuteTape(EmulationTapeCommand.Play);
            var remaining = signal.Samples.Length * 2000000L / signal.SampleRate + 100;
            // RX can already be partway through a frame after output mode. Synchronize
            // on the recorded carrier before treating RX Full as the recorded byte.
            while ((machine.Read(0xfe00) & 0x40) == 0 && --remaining > 0) machine.Tick();
            Assert.True(remaining > 0);
            machine.Read(0xfe04);
            while ((machine.Read(0xfe00) & 0x10) == 0 && --remaining > 0) machine.Tick();
            Assert.True(remaining > 0);
            Assert.Equal(0xa5, machine.Read(0xfe04));
        }
        finally { machine.EjectTape(); machine.Reset(true); }
    }

}
