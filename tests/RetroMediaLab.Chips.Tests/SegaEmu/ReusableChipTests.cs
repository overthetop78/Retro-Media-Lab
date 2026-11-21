using System.Buffers.Binary;
using RetroMediaLab.Chips.Audio.Common.Resampling;
using RetroMediaLab.Chips.Audio.Ricoh.RF5C164;
using RetroMediaLab.Chips.Controllers.Sega.Input;
using RetroMediaLab.Chips.CPU.Motorola.M68000.Execution;

namespace RetroMediaLab.Chips.Tests.SegaEmu;

public sealed class ReusableChipTests
{
    [Fact]
    public void CpuClockScalePreservesExecutedInstructions()
    {
        Bus a = new(), b = new();
        Cpu68000<Bus> neutral = new(a), scaled = new(b, new(ClockMultiplier: 7));
        try
        {
            neutral.Reset(); scaled.Reset();
            neutral.Run(1000); scaled.Run(7000);
            Assert.Equal(neutral.Registers.ToArray(), scaled.Registers.ToArray());
            Assert.Equal(neutral.State.cycles * 7, scaled.State.cycles);
            Assert.Equal(0, neutral.Configuration.RefreshPeriod);
        }
        finally { neutral.Halt(); scaled.Halt(); }
    }

    [Fact]
    public void PcmTransferWrapsInsideSelectedWaveWindow()
    {
        Rf5c164 pcm = new(new BandLimitedBuffer(4096));
        pcm.Reset();
        pcm.Write(7, 3, 0); // Select wave bank 3, without enabling playback.
        pcm.WriteWaveRam(0xffe, new byte[] { 1, 2, 3, 4 });
        Assert.Equal(new byte[] { 1, 2 }, pcm.WaveRam.Span.Slice(0x3ffe, 2).ToArray());
        Assert.Equal(new byte[] { 3, 4 }, pcm.WaveRam.Span.Slice(0x3000, 2).ToArray());
        Assert.Equal(0, pcm.WaveRam.Span[0x4000]);
    }

    [Fact]
    public void SixButtonHandshakeContinuesAfterContextRestore()
    {
        InputHardware input = new(new InputBus());
        input.PortSystems[0] = 1;
        input.ConfiguredPadTypes[0] = 1;
        input.Initialize(0x80, new()); input.InitializeIo(); input.Reset();
        input.DigitalInputs[0] = 0x0f00;
        input.gamepad_write(0, 0, 64); input.gamepad_write(0, 64, 64);
        input.gamepad_write(0, 0, 64); input.gamepad_write(0, 64, 64);
        byte[] saved = new byte[InputHardware.ContextSize]; input.SaveContext(saved);
        byte[] expected = Continue(input);
        input.Reset(); input.LoadContext(saved);
        Assert.Equal(expected, Continue(input));
        Assert.True(expected.Distinct().Count() > 1);
    }

    [Fact]
    public void MousePacketResumesDuringAcknowledgementDelay()
    {
        InputHardware input = new(new InputBus());
        input.PortSystems[0] = 2;
        input.Initialize(0x80, new()); input.InitializeIo(); input.Reset();
        input.AnalogInputs[0] = -23; input.AnalogInputs[1] = 49;
        input.DigitalInputs[0] = 0x20;
        input.mouse_write(0x20, 0x60);
        input.mouse_write(0, 0x20);
        input.mouse_read();
        byte[] saved = new byte[InputHardware.ContextSize]; input.SaveContext(saved);
        byte[] expected = ContinueMouse(input);
        input.Reset(); input.LoadContext(saved);
        Assert.Equal(expected, ContinueMouse(input));
        Assert.Contains(expected, value => (value & 15) == 9); // Low nibble of -23.
    }

    private static byte[] ContinueMouse(InputHardware input)
    {
        List<byte> reads = [input.mouse_read(), input.mouse_read()];
        for (int i = 0; i < 8; i++)
        {
            input.mouse_write((byte)(i % 2 == 0 ? 0x20 : 0), 0x20);
            for (int ack = 0; ack < 3; ack++) reads.Add(input.mouse_read());
        }
        return reads.ToArray();
    }

    private static byte[] Continue(InputHardware input)
    {
        List<byte> reads = [];
        for (int i = 0; i < 4; i++)
        {
            input.gamepad_write(0, 0, 64); reads.Add(input.gamepad_read(0));
            input.gamepad_write(0, 64, 64); reads.Add(input.gamepad_read(0));
        }
        return reads.ToArray();
    }

    private sealed class Bus : ICpu68000Bus
    {
        private readonly byte[] memory = new byte[65536];
        public Bus()
        {
            BinaryPrimitives.WriteUInt32BigEndian(memory, 0x8000);
            BinaryPrimitives.WriteUInt32BigEndian(memory.AsSpan(4), 0x100);
            new byte[] { 0x52, 0x80, 0x60, 0xfc }.CopyTo(memory, 0x100);
        }
        public uint Read8(uint address) => memory[address & 65535];
        public uint Read16(uint address) => BinaryPrimitives.ReadUInt16BigEndian(memory.AsSpan((int)(address & 65535), 2));
        public uint ReadMapped8(uint address) => Read8(address);
        public uint ReadMapped16(uint address) => Read16(address);
        public void Write8(uint address, uint value) => memory[address & 65535] = (byte)value;
        public void Write16(uint address, uint value) => BinaryPrimitives.WriteUInt16BigEndian(memory.AsSpan((int)(address & 65535), 2), (ushort)value);
        public void AcknowledgeInterrupt(uint level) { }
    }

    private sealed class InputBus : IInputBus
    {
        public uint MainCycles => 0;
        public uint Z80Cycles => 0;
        public byte Region => 0x80;
        public int ViewportWidth => 320;
        public int ViewportHeight => 224;
        public ushort VerticalCounter => 0;
        public uint HvcLatch { get; set; }
        public byte VersionPins => 0;
        public bool BootRomMapped => false;
        public ReadOnlyMemory<byte> VdpRegisters { get; } = new byte[32];
        public ReadOnlyMemory<byte> HorizontalCounterTable { get; } = new byte[3420];
        public void SetPsgPanning(uint cycles, uint panning) { }
        public void SwitchSmsCartridge(byte mode) { }
        public void UpdateMainIrq(uint level) { }
    }
}
