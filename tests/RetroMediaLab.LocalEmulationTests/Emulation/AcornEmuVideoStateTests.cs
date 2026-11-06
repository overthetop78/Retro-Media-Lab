using System.Text.Json;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Contracts;
using BbcVideo = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services.Video;

namespace RetroMediaLab.LocalEmulationTests.Emulation;

public sealed class AcornEmuVideoStateTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InProgressFieldsResumeWithTheSamePixelsAndSync(bool interlaced)
    {
        foreach (var instant in new[] { 0, 1, 7, 31, 127, 359 })
        {
            var ram = Enumerable.Range(0, 32768).Select(index => (byte)(index * 37)).ToArray();
            var video = new BbcVideo(ram, []);
            Set(video, 0, 7); Set(video, 1, 6); Set(video, 2, 6); Set(video, 3, 0x11);
            Set(video, 4, 3); Set(video, 6, 2); Set(video, 7, 3); Set(video, 9, 2);
            Set(video, 10, 32); Set(video, 8, interlaced ? (byte)3 : (byte)0);
            video.WriteUla(0, 8);
            video.WriteUla(1, 0xf6);
            for (var tick = 0; tick < instant; tick++) Advance(video, ram, tick);
            var state = JsonSerializer.Deserialize<VideoState>(JsonSerializer.SerializeToUtf8Bytes(video.CaptureState()))!;
            var restoredRam = (byte[])ram.Clone();
            var restored = new BbcVideo(restoredRam, []);
            var trace = new List<bool>();
            var restoredTrace = new List<bool>();
            video.VSyncChanged = trace.Add;
            restored.VSyncChanged = restoredTrace.Add;
            restored.RestoreState(state);
            Assert.Empty(restoredTrace);
            Assert.Equal(video.Render(), restored.Render());
            for (var tick = instant; tick < instant + 2500; tick++)
            {
                Advance(video, ram, tick);
                Advance(restored, restoredRam, tick);
                Assert.Equal(video.Frame, restored.Frame);
                Assert.Equal(video.VSync, restored.VSync);
                Assert.Equal(video.OutputHeight, restored.OutputHeight);
                if (tick % 503 == 0) Assert.Equal(video.Render(), restored.Render());
            }
            Assert.Equal(trace, restoredTrace);
            Assert.Equal(video.Render(), restored.Render());
            Assert.Equal(JsonSerializer.SerializeToUtf8Bytes(video.CaptureState()), JsonSerializer.SerializeToUtf8Bytes(restored.CaptureState()));
        }
    }

    private static void Set(BbcVideo video, byte register, byte value)
    {
        video.Select(register);
        video.WriteRegister(value);
    }

    private static void Advance(BbcVideo video, byte[] ram, int tick)
    {
        if (tick % 97 == 0) ram[tick % ram.Length] ^= 0xff;
        if (tick == 501) video.WriteUla(1, 0xf3);
        if (tick == 701) video.WriteUla(0, 2);
        if (tick == 911) video.WriteUla(0, 8);
        if (tick == 1101) video.Monochrome = true;
        video.Tick();
    }
}
