using System.Text.Json;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Enums;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Contracts;
using RetroMediaLab.Chips.IO.Motorola.M6800.Chip6850.Services;
using BbcSerial = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services.Serial;
using BbcTape = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services.Tape;

namespace RetroMediaLab.LocalEmulationTests.Emulation;

public sealed class AcornEmuTapeStateTests
{
    [Theory]
    [InlineData(EmulationTapeCommand.Play)]
    [InlineData(EmulationTapeCommand.Pause)]
    [InlineData(EmulationTapeCommand.Rewind)]
    [InlineData(EmulationTapeCommand.Record)]
    public void TransportAndSerialElectronicsResumeAtTheSameInstant(EmulationTapeCommand operation)
    {
        foreach (var instant in new[] { 20_001, 20_017, 20_417 })
        {
            var serial = new BbcSerial(new Acia6850(2_000_000, 0));
            var tape = new BbcTape(serial);
            tape.Insert(new EmulationTapeSignal(
                Enumerable.Range(0, 30_000).Select(index => index % 20 < 10 ? (short)12000 : (short)-12000).ToArray(),
                44_100, [0, 10_000, 20_000]), writable: true);
            tape.WriteUla(0x89);
            for (var tick = 0; tick < instant; tick++) Advance(tape, tick, operation);
            var tapeState = JsonSerializer.Deserialize<TapeState>(Serialize(tape.CaptureState()))!;
            var serialState = JsonSerializer.Deserialize<SerialState>(Serialize(serial.CaptureState()))!;
            var restoredSerial = new BbcSerial(new Acia6850(2_000_000, 0));
            var restoredTape = new BbcTape(restoredSerial);
            var notifications = 0;
            restoredTape.ActivityChanged += () => notifications++;
            restoredTape.ClockRequiredChanged = _ => notifications++;
            restoredSerial.RestoreState(serialState);
            restoredTape.RestoreState(tapeState);
            Assert.Equal(0, notifications);
            for (var tick = instant; tick < instant + 5000; tick++)
            {
                Advance(tape, tick, operation);
                Advance(restoredTape, tick, operation);
                Assert.Equal(tape.Position, restoredTape.Position);
                Assert.Equal(tape.TapeState, restoredTape.TapeState);
                Assert.Equal(tape.TapeActive, restoredTape.TapeActive);
                Assert.Equal(tape.TapeOperation, restoredTape.TapeOperation);
                Assert.Equal(tape.HasChanges, restoredTape.HasChanges);
            }
            Assert.Equal(Serialize(tape.CaptureState()), Serialize(restoredTape.CaptureState()));
            Assert.Equal(Serialize(serial.CaptureState()), Serialize(restoredSerial.CaptureState()));
        }
    }

    private static byte[] Serialize<T>(T value) => JsonSerializer.SerializeToUtf8Bytes(value);

    private static void Advance(BbcTape tape, int tick, EmulationTapeCommand operation)
    {
        if (tick == 20_000) tape.ExecuteTape(operation);
        if (tick == 21_000) tape.ExecuteTape(EmulationTapeCommand.Play);
        if (tick == 22_000) tape.WriteUla(0x88);
        if (tick == 23_000) tape.ExecuteTape(EmulationTapeCommand.Stop);
        if (tick == 23_030) tape.ExecuteTape(EmulationTapeCommand.Play);
        tape.Tick();
    }
}
