using System.Text.Json;
using RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Contracts;
using RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.Intel.FDC.Chip8271;

public sealed class StateTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FmTransfersResumeWithTheSameRequestsBitsAndDeadlines(bool writing)
    {
        var inputs = BuildInputs(writing);
        foreach (var instant in new[] { 0, 1, 37, 1023, 4097, 7001 })
        {
            var trace = new List<string>();
            var chip = Create(trace);
            Command(chip, writing ? (byte)0x4a : (byte)0x52, 0, 1);
            for (var tick = 0; tick < instant; tick++) Advance(chip, inputs[tick], writing, trace);
            var state = JsonSerializer.Deserialize<Intel8271State>(Serialize(chip))!;
            var restoredTrace = new List<string>();
            var restored = Create(restoredTrace);
            restoredTrace.Clear();
            restored.RestoreState(state);
            Assert.Empty(restoredTrace);
            trace.Clear();
            for (var tick = instant; tick < inputs.Count; tick++)
            {
                Advance(chip, inputs[tick], writing, trace);
                Advance(restored, inputs[tick], writing, restoredTrace);
                Assert.Equal(chip.Read(0), restored.Read(0));
                Assert.Equal(chip.Drq, restored.Drq);
                Assert.Equal(chip.NextClockEvent, restored.NextClockEvent);
                Assert.Equal(chip.DriveControl, restored.DriveControl);
                Assert.Equal(chip.WriteData, restored.WriteData);
            }
            Assert.False(chip.Active);
            Assert.Equal(128L, writing ? chip.WrittenBytes(0) : chip.ReadBytes(0));
            Assert.Equal(trace, restoredTrace);
            Assert.Equal(Serialize(chip), Serialize(restored));
            Assert.Equal(0, chip.Read(1));
            Assert.Equal(0, restored.Read(1));
        }
    }

    [Fact]
    public void PendingBusCommandAndSelectedParametersResumeWithoutAcknowledgement()
    {
        var chip = Create([]);
        chip.WriteBus(true, false, 0, 0x3d);
        chip.WriteBus(true, false, 1, 0x17);
        var state = chip.CaptureState();
        var restored = Create([]);
        restored.RestoreState(state);
        for (var tick = 0; tick < 8; tick++)
        {
            chip.Tick();
            restored.Tick();
            Assert.Equal(Serialize(chip), Serialize(restored));
        }
        Assert.Equal(chip.Read(1), restored.Read(1));
        var expected = Serialize(chip);
        Assert.Throws<ArgumentException>(() => chip.RestoreState(state with { ChipClock = 1 }));
        Assert.Equal(expected, Serialize(chip));
        Assert.Throws<ArgumentException>(() => chip.RestoreState(state with { Parameters = [] }));
        Assert.Equal(expected, Serialize(chip));
    }

    private readonly record struct Input(bool ClockWindow, bool Pulse);

    private static Intel8271 Create(List<string> trace)
    {
        var chip = new Intel8271();
        chip.SetPhaseLockedSeparator(true);
        chip.SetReady(0, true);
        chip.SetTrackZero(0, true);
        chip.SetIndex(0, false);
        Command(chip, 0x35, 0x0d, 1, 0, 0x10);
        Command(chip, 0x3a, 0x17, 0);
        chip.NmiChanged = value => trace.Add($"N:{chip.InputClockCount}:{value}");
        chip.WriteDataChanged += value => trace.Add($"W:{chip.InputClockCount}:{value}");
        chip.DriveSignalsChanged += () => trace.Add($"D:{chip.InputClockCount}:{chip.DriveControl}:{chip.ActiveDrive}");
        chip.NextClockEventChanged += value => trace.Add($"T:{chip.InputClockCount}:{value}");
        return chip;
    }

    private static void Command(Intel8271 chip, byte command, params byte[] parameters)
    {
        chip.Write(0, command);
        foreach (var value in parameters) chip.Write(1, value);
        chip.Tick();
    }

    private static void Advance(Intel8271 chip, Input input, bool writing, List<string> trace)
    {
        chip.SetDataWindow(input.ClockWindow);
        if (input.Pulse)
        {
            chip.SetUnseparatedData(true);
            chip.SetUnseparatedData(false);
        }
        chip.Tick();
        if (!chip.Drq) return;
        if (writing) chip.Write(4, (byte)chip.WrittenBytes(0));
        else trace.Add($"R:{chip.InputClockCount}:{chip.Read(4)}");
    }

    private static List<Input> BuildInputs(bool writing)
    {
        var inputs = new List<Input>();
        Field(inputs, 0xfe, [0, 0, 1, 0]);
        if (writing)
        {
            for (var gap = 0; gap < 12; gap++) Byte(inputs, 0xff, 0xff);
            for (var tick = 0; tick < 22_000; tick++) inputs.Add(new(false, false));
        }
        else Field(inputs, 0xfb, Enumerable.Range(0, 128).Select(value => (byte)value).ToArray());
        return inputs;
    }

    private static void Field(List<Input> inputs, byte mark, byte[] data)
    {
        Byte(inputs, 0xff, 0xff);
        Byte(inputs, 0, 0xff); Byte(inputs, 0, 0xff);
        Byte(inputs, mark, 0xc7);
        ushort crc = 0xffff;
        foreach (var value in new[] { mark }.Concat(data))
        {
            crc ^= (ushort)(value << 8);
            for (var bit = 0; bit < 8; bit++) crc = (ushort)((crc & 0x8000) != 0 ? (crc << 1) ^ 0x1021 : crc << 1);
        }
        foreach (var value in data) Byte(inputs, value, 0xff);
        Byte(inputs, (byte)(crc >> 8), 0xff);
        Byte(inputs, (byte)crc, 0xff);
    }

    private static void Byte(List<Input> inputs, byte data, byte clocks)
    {
        for (var bit = 7; bit >= 0; bit--)
        {
            for (var tick = 0; tick < 8; tick++) inputs.Add(new(true, tick == 0 && (clocks & 1 << bit) != 0));
            for (var tick = 0; tick < 8; tick++) inputs.Add(new(false, tick == 0 && (data & 1 << bit) != 0));
        }
    }

    private static byte[] Serialize(Intel8271 chip) => JsonSerializer.SerializeToUtf8Bytes(chip.CaptureState());
}
