using System.Reflection;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Interfaces;
using RetroMediaLab.Emulation.Acorn.Common.Interfaces;
using RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Enums;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Interfaces;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;
using BbcMachine = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services.BbcMicroMachine;
using Plus = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.ModelBPlus.Constants.ModelBPlusConstants;

namespace RetroMediaLab.LocalEmulationTests.Emulation;

public sealed class AcornEmuStateTests
{
    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 0)]
    [InlineData(true, Plus.BaseBytes)]
    [InlineData(true, Plus.ExpandedBytes)]
    public void BbcBoardResumesItsRamVideoAudioAndCpuThroughTheCommonCore(bool expandedIo, int plusMemory)
    {
        var mos = Enumerable.Repeat((byte)0xea, 16384).ToArray();
        byte[] program = [0xee, 0x00, 0x10, 0x4c, 0x00, 0xc0];
        program.CopyTo(mos, 0);
        mos[0x3ffc] = 0;
        mos[0x3ffd] = 0xc0;
        var board = new BbcMachine(mos, new byte[16384], memoryBytes: 32768,
            diskInterface: true, wd1770: plusMemory != 0, expandedIo: expandedIo, econetInterface: true,
            modelBPlusMemory: plusMemory);
        var video = board.Video;
        byte[] registers = [7, 6, 6, 0x11, 3, 0, 2, 3, 0, 2, 32];
        for (byte register = 0; register < registers.Length; register++)
        {
            video.Select(register);
            video.WriteRegister(registers[register]);
        }
        video.WriteUla(0, 8);
        video.WriteUla(1, 0xf6);
        for (var address = 0; address < 32768; address++) board.Write((ushort)address, (byte)(address * 37));
        var model = plusMemory != 0 ? MachineModel.BbcMicroModelBPlus
            : expandedIo ? MachineModel.BbcMicroModelB : MachineModel.BbcMicroModelA;
        var core = new EmulatorCore(model);
        typeof(EmulatorCore).GetField("_machine", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(core, board);
        try
        {
            core.RunFrame();
            for (var instruction = 0; instruction < 17; instruction++) board.Cpu.Step();
            var saved = core.SaveState();
            core.RunFrame();
            var expectedMachine = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(board.CaptureState());
            var expectedPixels = core.LatestVideoFrame!.Pixels.ToArray();
            var expectedAudio = core.LatestAudioChunk!.InterleavedSamples.ToArray();
            core.RunFrame();
            board.Write(0x1000, 0xff);
            core.LoadState(saved);
            core.RunFrame();
            Assert.Equal(expectedMachine, System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(board.CaptureState()));
            Assert.Equal(expectedPixels, core.LatestVideoFrame!.Pixels.ToArray());
            Assert.Equal(expectedAudio, core.LatestAudioChunk!.InterleavedSamples.ToArray());
        }
        finally { core.Dispose(); }
    }

    [Theory]
    [InlineData(MachineModel.BbcMicroModelA)]
    [InlineData(MachineModel.BbcMicroModelB)]
    public void CommonCorePathRestoresMemoryAndOutputCountersWithoutExecutingTheMachine(MachineModel model)
    {
        var core = new EmulatorCore(model);
        var machine = new MemoryMachine();
        typeof(EmulatorCore).GetField("_machine", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(core, machine);
        try
        {
            Assert.True(core.SupportsSaveStates);
            core.RunFrame();
            var saved = core.SaveState();
            var savedPixels = core.LatestVideoFrame!.Pixels.ToArray();
            var savedFrames = core.BufferedAudioFrames;
            core.RunFrame();
            var expectedPixels = core.LatestVideoFrame!.Pixels.ToArray();
            var expectedSequence = core.LatestVideoFrame.Sequence;
            var expectedTimestamp = core.LatestVideoFrame.Timestamp;
            core.RunFrame();
            var calls = machine.RunCalls;
            core.LoadState(saved);
            Assert.Equal(calls, machine.RunCalls);
            Assert.Equal(savedPixels, core.LatestVideoFrame!.Pixels.ToArray());
            Assert.Equal(savedFrames, core.BufferedAudioFrames);
            core.RunFrame();
            Assert.Equal(expectedPixels, core.LatestVideoFrame!.Pixels.ToArray());
            Assert.Equal(expectedSequence, core.LatestVideoFrame.Sequence);
            Assert.Equal(expectedTimestamp, core.LatestVideoFrame.Timestamp);
            Assert.Equal(0, machine.ResetCalls);
        }
        finally { core.Dispose(); }
    }

    public sealed record MemoryState(byte[] Ram, int Counter);

    private sealed class MemoryMachine : IAcornMachine
    {
        private readonly byte[] _ram = new byte[16];
        private int _counter;
        internal int RunCalls { get; private set; }
        internal int ResetCalls { get; private set; }
        public Type StateType => typeof(MemoryState);
        public object CaptureState() => new MemoryState((byte[])_ram.Clone(), _counter);
        public void RestoreState(object state)
        {
            var saved = (MemoryState)state;
            saved.Ram.CopyTo(_ram, 0);
            _counter = saved.Counter;
        }
        public int? VideoWidth => 1;
        public int? VideoHeight => 1;
        public byte[] RunFrame()
        {
            RunCalls++;
            _ram[0] += 3;
            return [_ram[0], (byte)++_counter, 0, 255];
        }
        public short[] DrainAudio() => [(short)_counter, (short)_ram[0]];
        public IEmulatorCassetteTransport? CassetteTransport => null;
        public bool CassetteHasChanges => false;
        public EmulationCassetteSignal RecordedCassette => throw new NotSupportedException();
        public void InsertCassette(EmulationCassetteSignal signal, bool writable = false) => throw new NotSupportedException();
        public void MarkCassetteSaved() { }
        public void RefreshCassetteIndex(EmulationCassetteSignal signal) => throw new NotSupportedException();
        public void ConfigureCassette(EmulationCassetteSettings settings) { }
        public void EjectCassette() { }
        public void AttachFloppy(int index, IEmulationDiskImage? image) => throw new NotSupportedException();
        public bool FloppyActive(int index) => false;
        public bool FloppyMotor(int index) => false;
        public bool FloppyReading(int index) => false;
        public bool FloppyWriting(int index) => false;
        public long FloppySeekSteps(int index) => 0;
        public long FloppyReadBytes(int index) => 0;
        public long FloppyWrittenBytes(int index) => 0;
        public void ConfigureFloppySpeed(int index, int percent) { }
        public void AttachHardDisk(int unit, IEmulationDiskImage? image) => throw new NotSupportedException();
        public bool HardDiskActive(int unit) => false;
        public bool HardDiskMotor(int unit) => false;
        public bool HardDiskWriting(int unit) => false;
        public long HardDiskSeekCount(int unit) => 0;
        public long HardDiskReadBlocks(int unit) => 0;
        public long HardDiskWrittenBlocks(int unit) => 0;
        public void ConfigureHardDisk(int unit, int latencyMilliseconds, int speedPercent, int? writeLatencyMilliseconds = null) { }
        public Action<ReadOnlyMemory<byte>>? NetworkFrameTransmitted { get; set; }
        public Action<int>? NetworkPoll { get; set; }
        public bool ReceiveNetworkFrame(ReadOnlySpan<byte> frame, bool validFcs = true) => false;
        public void SetNetworkLine(bool clockPresent, bool clearToSend = true, bool idle = true) { }
        public void SetInput(EmulationInputSnapshot snapshot) { }
        public byte[]? ReadNonvolatileMemory() => null;
        public void WriteNonvolatileMemory(ReadOnlySpan<byte> bytes) { }
        public void Reset(bool cold) => ResetCalls++;
        public void SetOption(string key, string value) { }
    }
}
