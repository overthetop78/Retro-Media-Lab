using RetroMediaLab.Chips.Audio.Common.Resampling;
using RetroMediaLab.Chips.Common.Memory;
using RetroMediaLab.Chips.Controllers.Sega.CD;
using RetroMediaLab.Chips.CPU.Samsung.SSP1601;
using RetroMediaLab.Chips.Memory.Flash.Cfi;
using RetroMediaLab.Chips.Memory.Serial.I2c;

namespace RetroMediaLab.Chips.Tests.SegaEmu;

public sealed class PeripheralChipTests
{
    [Fact]
    public void Ssp1601MultipliesAndWritesAnExternalPortWithoutCartridgeStorage()
    {
        DspBus bus = new();
        ushort[] program = [0x0810,3,0x0820,4,0x0037,0x00bf];
        program.CopyTo(bus.Program, 0);
        Ssp1601 cpu = new(bus);
        try
        {
            cpu.Reset(); cpu.Run(100);
            Assert.Equal(24u, bus.Value); Assert.Equal(11, bus.Register);
            Assert.Equal(6, cpu.ProgramCounter);
            byte[] state = new byte[Ssp1601.ContextSize]; cpu.SaveContext(state);
            cpu.Reset(100); cpu.LoadContext(state);
            Assert.Equal(6, cpu.ProgramCounter);
        }
        finally { bus.ShouldYield = true; }
    }

    [Fact]
    public void FlashProgramsOnlyClearedBitsAndRoutesEraseToItsStorageBus()
    {
        FlashBus bus = new(); FlashCfi flash = new(bus); flash.Initialize(FlashCfi.M29W320Eb, []);
        Unlock(flash); flash.Write(0x555,0xa0); flash.Write(0x10000,0x1234);
        Assert.Equal(0x1234u, flash.Read(0x10000));
        Unlock(flash); flash.Write(0x555,0xa0); flash.Write(0x10000,0xffff);
        Assert.Equal(0x1234u, flash.Read(0x10000));
        Unlock(flash); flash.Write(0x555,0x80); Unlock(flash); flash.Write(0x10000,0x30);
        Assert.Equal(2u, bus.ErasedBlock);
        flash.Write(0x55,0x98);
        Assert.Equal((uint)'Q', flash.Read(0x10)); Assert.Equal((uint)'R', flash.Read(0x11)); Assert.Equal((uint)'Y', flash.Read(0x12));
    }

    [Fact]
    public void FlashSnapshotRetainsAnIncompleteUnlockSequence()
    {
        FlashBus bus = new(); FlashCfi flash = new(bus); flash.Initialize(0, []);
        flash.Write(0x555,0xaa);
        byte[] state = new byte[FlashCfi.ContextSize]; flash.SaveContext(state);
        FlashCfi restored = new(bus); restored.Initialize(0, []); restored.LoadContext(state);
        restored.Write(0x2aa,0x55); restored.Write(0x555,0xa0); restored.Write(0x10000,0x3456);
        Assert.Equal(0x3456u, restored.Read(0x10000));
    }

    [Fact]
    public void I2cWritesAnAddressedByteThroughClockAndDataLines()
    {
        byte[] memory = new byte[256]; EepromI2c chip = new(memory); chip.Configure(3);
        chip.SetLines(true,true); chip.SetLines(true,false);
        SendByte(chip,0xa0); SendByte(chip,0x2a); SendByte(chip,0x5c);
        chip.SetLines(false,false); chip.SetLines(true,false); chip.SetLines(true,true);
        Assert.Equal(0x5c, memory[0x2a]);
        Assert.Equal(0, memory[0x29]);
    }

    [Fact]
    public void CdReadsSectorsThroughBorrowedMediaAndPreservesItsOwner()
    {
        CdBus bus = new(); FakeDisc media = new(); BandLimitedBuffer output = new(4096);
        Cdd drive = new(bus, output, CancellationToken.None);
        try
        {
            drive.Initialize(44100); drive.Mount(media); drive.Lba = 10;
            byte[] data = new byte[2048]; drive.ReadData(data, []);
            Assert.Equal(10, media.LastDataLba); Assert.All(data, value => Assert.Equal(0x5a, value));
            drive.Status = 1; drive.Update();
            Assert.Equal(11, drive.Lba); Assert.NotEqual(0u, bus.DecoderHeader);
            drive.Unload(); Assert.False(media.Disposed);
        }
        finally { try { drive.Dispose(); } finally { media.Dispose(); } }
    }

    [Fact]
    public void CdSnapshotResumesAtTheSamePcmSampleOffset()
    {
        CdBus bus = new(); FakeDisc media = new(); BandLimitedBuffer output = new(4096);
        Cdd drive = new(bus, output, CancellationToken.None);
        try
        {
            drive.Initialize(44100); drive.Mount(media); drive.Index = 1; drive.Lba = 75;
            drive.SeekAudio(1,75); bus.Registers[0x36 >> 1].High = 0;
            drive.ReadAudio(64); Assert.Equal(0L, media.LastAudioOffset);
            byte[] state = new byte[Cdd.ContextSize]; drive.SaveContext(state);
            drive.ReadAudio(32); Assert.Equal(64L, media.LastAudioOffset);
            drive.ReadAudio(32); Assert.Equal(96L, media.LastAudioOffset);
            drive.LoadContext(state); drive.ReadAudio(32);
            Assert.Equal(64L, media.LastAudioOffset);
        }
        finally { try { drive.Dispose(); } finally { media.Dispose(); } }
    }

    private static void Unlock(FlashCfi flash) { flash.Write(0x555,0xaa); flash.Write(0x2aa,0x55); }
    private static void SendByte(EepromI2c chip, byte value)
    {
        for (int bit = 7; bit >= 0; bit--)
        {
            bool data = (value & (1 << bit)) != 0;
            chip.SetLines(false,data); chip.SetLines(true,data);
        }
        chip.SetLines(false,true); Assert.False(chip.DataOutput);
        chip.SetLines(true,true); chip.SetLines(false,true);
    }
    private sealed class DspBus : ISsp1601Bus
    {
        public ushort[] Program { get; } = new ushort[65536];
        public uint Value { get; private set; }
        public int Register { get; private set; }
        public bool ShouldYield { get; set; }
        public ushort ReadProgramWord(ushort address) => Program[address];
        public uint ReadExternalRegister(int register) => 0;
        public void WriteExternalRegister(int register, uint value) { Register = register; Value = value; ShouldYield = true; }
    }
    private sealed class FlashBus : IFlashCfiBus
    {
        private ushort _word = 0xffff;
        public uint ErasedBlock { get; private set; } = uint.MaxValue;
        public ushort ReadWord(uint address) => _word;
        public void ProgramWord(uint address, ushort value) => _word = value;
        public void EraseChip() => _word = 0xffff;
        public void EraseBlock(uint block) { ErasedBlock = block; _word = 0xffff; }
    }
    private sealed class CdBus : ICddBus
    {
        public Register16[] Registers { get; } = new Register16[256];
        public byte PendingInterrupts { get; set; }
        public uint DecoderHeader { get; private set; }
        public bool MegaSdEnabled => false;
        public void UpdateSubIrq(uint mask) { }
        public void UpdateDecoder(uint header) => DecoderHeader = header;
        public void UpdateMegaSdCdda(uint samples) { }
    }
    private sealed class FakeDisc : ICdDriveMedia, IDisposable
    {
        public IReadOnlyList<CdTrack> Tracks { get; } = [new(0,75,1),new(75,150,0)];
        public int LeadOutLba => 150;
        public bool HasSubchannel => false;
        public bool Disposed { get; private set; }
        public int LastDataLba { get; private set; }
        public long LastAudioOffset { get; private set; }
        public void ReadData(int lba, Span<byte> destination, Span<byte> subheader) { LastDataLba = lba; destination.Fill(0x5a); }
        public int ReadAudio(int trackIndex, long sampleFrame, Span<short> destination)
        {
            LastAudioOffset = sampleFrame;
            for (int i = 0; i < destination.Length; i++) destination[i] = (short)(1000 + sampleFrame + i);
            return destination.Length / 2;
        }
        public void ReadSubchannel(int lba, Span<byte> destination) => destination.Clear();
        public void Dispose() => Disposed = true;
    }
}
