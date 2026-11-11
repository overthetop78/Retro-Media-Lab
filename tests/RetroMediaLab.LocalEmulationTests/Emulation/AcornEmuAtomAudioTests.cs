using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Enums;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornAtom.Common.Services;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornAtom.Common.Constants.AtomConstants;

namespace RetroMediaLab.Tests.Emulation;

public sealed class AcornEmuAtomAudioTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TapeDataDoesNotDriveTheSpeakerButPc2StillDoes(bool recording)
    {
        var board = new AtomMachine(new byte[C.CombinedBasicOsBytes], new byte[280], C.RamBlockBytes, C.RamBlockBytes);
        try
        {
            board.Write(0xb003, 0x8a); // Mode0, C lower outputs, upper inputs.
            board.Write(0xb002, 0);
            var samples = Enumerable.Range(0, 48000).Select(index => (short)((index / 10 & 1) == 0 ? 16000 : -16000)).ToArray();
            board.InsertTape(new EmulationTapeSignal(samples, 48000), writable: recording);
            board.TapeTransport.ExecuteTape(recording ? EmulationTapeCommand.Record : EmulationTapeCommand.Play);
            var inputEdges = 0;
            var previous = board.Read(0xb002) & 0x20;
            for (var cycle = 0; cycle < 100000; cycle++)
            {
                if (recording && cycle % 1000 == 0) board.Write(0xb002, (byte)((cycle / 1000 & 1) == 0 ? 1 : 3));
                board.Tick();
                var current = board.Read(0xb002) & 0x20;
                if (current != previous) inputEdges++;
                previous = current;
            }
            var audio = board.DrainAudio();
            Assert.NotEmpty(audio);
            Assert.All(audio, sample => Assert.Equal((short)0, sample));
            if (recording) Assert.True(board.TapeHasChanges);
            else Assert.True(inputEdges > 100); // CAS IN remains active on PC5.
            board.Write(0xb003, 0x05); // BSR: set PC2, independent of CAS IN/OUT.
            for (var cycle = 0; cycle < 1000; cycle++) board.Tick();
            Assert.Contains(board.DrainAudio(), sample => sample != 0);
        }
        finally
        {
            board.EjectTape();
            board.Reset(true);
            board.DrainAudio();
        }
    }
}
