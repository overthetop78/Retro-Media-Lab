using System.Text.Json;
using RetroMediaLab.Chips.Video.Motorola.M6845.Chip6845.Contracts;
using RetroMediaLab.Chips.Video.Motorola.M6845.Chip6845.Enums;
using RetroMediaLab.Chips.Video.Motorola.M6845.Chip6845.Services;
using static RetroMediaLab.Chips.Tests.Video.Motorola.M6845.Chip6845.CrtcTestFixture;

namespace RetroMediaLab.Chips.Tests.Video.Motorola.M6845.Chip6845;

public sealed class StateTests
{
    [Theory]
    [InlineData(Crtc6845Model.Mc6845)]
    [InlineData(Crtc6845Model.Hd6845S)]
    public void ScanoutBusLightPenAndSyncContinueAtTheSameCharacter(Crtc6845Model model)
    {
        foreach (byte mode in new byte[] { 0, 1, 3, 0x51, 0xa3 })
        foreach (var instant in new[] { 0, 1, 7, 15, 16, 31, 127, 255, 300 })
        {
            var trace = new List<bool>();
            var crtc = Create(model, mode: mode, adjust: 2);
            crtc.VSyncChanged = trace.Add;
            for (var clock = 0; clock < instant; clock++) Advance(crtc, clock);
            var state = JsonSerializer.Deserialize<Crtc6845State>(Serialize(crtc))!;
            var restoredTrace = new List<bool>();
            var restored = new Crtc6845(model) { VSyncChanged = restoredTrace.Add };
            restored.RestoreState(state);
            Assert.Empty(restoredTrace);
            trace.Clear();
            for (var clock = instant; clock < instant + 1200; clock++)
            {
                Assert.Equal(Advance(crtc, clock), Advance(restored, clock));
                Assert.Equal(crtc.MemoryAddress, restored.MemoryAddress);
                Assert.Equal(crtc.Address, restored.Address);
                Assert.Equal(crtc.DisplayEnabled, restored.DisplayEnabled);
                Assert.Equal(crtc.Cursor, restored.Cursor);
                Assert.Equal(crtc.HSync, restored.HSync);
                Assert.Equal(crtc.VSync, restored.VSync);
                Assert.Equal(crtc.DataBusValue, restored.DataBusValue);
            }
            Assert.Equal(trace, restoredTrace);
            Assert.Equal(Serialize(crtc), Serialize(restored));
        }
    }

    [Fact]
    public void RegistersAreCopiedAndWrongVariantIsRejectedBeforeMutation()
    {
        var crtc = Create();
        var state = crtc.CaptureState();
        crtc.RestoreState(state);
        var expected = Serialize(crtc);
        state.Registers[0] = 0;
        Assert.Equal(expected, Serialize(crtc));
        Assert.Throws<ArgumentException>(() => crtc.RestoreState(state with { Model = Crtc6845Model.Mc6845 }));
        Assert.Equal(expected, Serialize(crtc));
        Assert.Throws<ArgumentException>(() => crtc.RestoreState(state with { Registers = [] }));
        Assert.Equal(expected, Serialize(crtc));
    }

    private static byte[] Serialize(Crtc6845 crtc) => JsonSerializer.SerializeToUtf8Bytes(crtc.CaptureState());

    private static bool Advance(Crtc6845 crtc, int clock)
    {
        if (clock == 31) crtc.SetLightPen(true);
        if (clock == 34) crtc.SetLightPen(false);
        if (clock == 127) crtc.SetBus(true, false, false, true, 1);
        if (clock == 128) crtc.SetBus(true, false, false, false, 1);
        if (clock == 129) crtc.SetBus(true, true, false, true, 6);
        if (clock == 130) crtc.SetBus(true, true, false, false, 6);
        if (clock == 255) Write(crtc, 13, 0x70);
        if (clock == 600) crtc.SetReset(true);
        if (clock == 603) crtc.SetReset(false);
        return crtc.Tick();
    }
}
