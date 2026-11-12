using System.Text.Json;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Enums;
using RetroMediaLab.Emulation.Sega.Common.Machines.Common.Contracts;
using RetroMediaLab.Emulation.Sega.Common.Machines.Common.Enums;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Contracts;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Services;

namespace RetroMediaLab.Tests.Emulation.Sega;

public sealed class SegaEmuCoreTests
{
    private static readonly MachineConfiguration Configuration = new("Sg1000", "segaemu");

    [Fact]
    public void BuiltInAdapterUsesCommonSettingsFirmwareStorageAndPauseBindings()
    {
        var engine = new RetroMediaLab.Emulation.Sega.Common.Services.Engine();
        var adapter = engine.Adapter(Configuration);
        Assert.Contains(RetroMediaLab.Emulation.Sega.Common.Dictionaries.EmulatorCatalog.GetAll("Sg1000"), definition => definition.Id == "segaemu");
        Assert.True(adapter.SupportsNoContent);
        Assert.Empty(adapter.GetFirmwareSlots(Configuration));
        Assert.Equal(bool.FalseString, adapter.GetDefaultOptions(Configuration)["segaemu.internalGame.enabled"]);
        Assert.Single(adapter.DescribeStorage(Configuration).ConfiguredSlots);
        var description = engine.Describe("Sg1000", Configuration);
        Assert.Contains(description.Blocks.SelectMany(block => block.Fields), field => field.Id == "segaemu.internalGame.enabled");
        var configured = (MachineConfiguration)engine.ApplySettings(Configuration, new Dictionary<string, string?>
        {
            ["segaemu.internalGame.enabled"] = bool.TrueString,
            ["segaemu.internalGame.rom"] = "chosen-internal-game.bin"
        });
        Assert.Equal("chosen-internal-game.bin", configured.FirmwarePaths!["segaemu.internalGame.rom"]);
        Assert.True(Assert.Single(adapter.GetFirmwareSlots(configured)).IsRequired);
        Assert.True(adapter.RequiresOptionRestart(configured, "segaemu.internalGame.enabled"));
        Assert.True(adapter.RequiresOptionRestart(configured, "segaemu.internalGame.rom"));
        Assert.True(adapter.RequiresOptionRestart(configured, "segaemu.cartridge.layout"));
        Assert.Contains(engine.Describe("Sg1000", configured).Blocks.SelectMany(block => block.Fields), field => field.Id == "segaemu.internalGame.rom");
        var input = RetroMediaLab.Emulation.Sega.Common.Machines.Common.Functions.InputSettingsFunctions.Describe(Configuration);
        Assert.Contains(input.ControllerPorts[0].Bindings.Definitions, binding => binding.Id == "Start");
        Assert.DoesNotContain(input.ControllerPorts[1].Bindings.Definitions, binding => binding.Id == "Start");
    }

    [Fact]
    public void NativeFrameAudioInputsAndSnapshotResumeThroughTheCommonContract()
    {
        EmulatorCore? original = null, restored = null;
        try
        {
            var image = BuildCartridge();
            original = new(); restored = new();
            original.Initialize(Configuration, image); restored.Initialize(Configuration, image);
            var input = EmulationInputSnapshot.Empty with
            {
                Controllers = [EmulationControllerState.Empty with { Buttons = (1U << 4) | (1U << 8) },
                    EmulationControllerState.Empty with { Buttons = (1U << 5) | (1U << 6) | 1U }]
            };
            original.SetInput(input);
            original.RunFrame();
            Assert.Equal(0x5e, original.Board.CaptureState().Ram.Cells[0x11]);
            Assert.Equal(0xfa, original.Board.CaptureState().Ram.Cells[0x12]);
            Assert.Equal(0, original.Board.Vdp.RasterState.Dot);
            Assert.Equal(0, original.Board.Vdp.RasterState.Line);
            var frame = Assert.IsType<VideoFrame>(original.LatestVideoFrame);
            Assert.Equal((256, 192, 1024, EmulationPixelFormat.Xrgb8888), (frame.Width, frame.Height, frame.Pitch, frame.PixelFormat));
            Assert.Equal(256 * 192 * 4, frame.Pixels.Length);
            Assert.InRange(original.FramesPerSecond, 59.92, 59.93);
            Assert.Equal(3_579_545, original.ProcessorClockHz);
            Assert.True(original.LatestAudioChunk!.HasValidLength);
            Assert.Equal(EmulationAudioChannelLayout.Mono, original.LatestAudioChunk.ChannelLayout);
            Assert.Contains(original.LatestAudioChunk.InterleavedSamples.ToArray(), sample => sample != 0);
            byte[] before = original.SaveState();
            restored.LoadState(before);
            Assert.Equal(before, restored.SaveState());
            var pause = input with { Controllers = [input.Controllers[0] with { Buttons = 1U << 3 }, input.Controllers[1]] };
            original.SetInput(pause); restored.SetInput(pause);
            original.RunFrame(); restored.RunFrame();
            Assert.Equal(0x42, original.Board.CaptureState().Ram.Cells[0x13]);
            Assert.Equal(original.SaveState(), restored.SaveState());
            Assert.Equal(original.LatestVideoFrame!.Pixels.ToArray(), restored.LatestVideoFrame!.Pixels.ToArray());
            var timestamp = original.LatestVideoFrame.Timestamp;
            original.SetInput(input); original.RunFrame();
            Assert.InRange((original.LatestVideoFrame!.Timestamp - timestamp).TotalSeconds, 1 / original.FramesPerSecond - 0.0000002, 1 / original.FramesPerSecond + 0.0000002);
            int count = 0;
            while (restored.TryDequeueAudio(out var chunk)) { Assert.True(chunk!.HasValidLength); count += chunk.FrameCount; }
            Assert.True(count > 0); Assert.Equal(0, restored.BufferedAudioFrames);
            Assert.False(restored.TryDequeueAudio(out _));
        }
        finally { try { original?.Dispose(); } finally { restored?.Dispose(); } }
    }

    [Fact]
    public void InvalidSnapshotsOptionsAndControllerChangesAreRejectedWithoutChangingTheMachine()
    {
        EmulatorCore? core = null;
        try
        {
            core = new(); core.Initialize(Configuration, BuildCartridge()); core.RunFrame();
            byte[] before = core.SaveState();
            var saved = JsonSerializer.Deserialize<EmulatorState>(before)!;
            foreach (var invalid in new[] { saved with { Version = 9 }, saved with { Controllers = [] },
                saved with { Board = saved.Board with { CartridgeHash = [] } },
                saved with { Audio = [saved.LatestAudio! with { FrameCount = 1 }] } })
            {
                Assert.Throws<ArgumentException>(() => core.LoadState(JsonSerializer.SerializeToUtf8Bytes(invalid)));
                Assert.Equal(before, core.SaveState());
            }
            Assert.Throws<NotSupportedException>(() => core.SetControllerPortDevice(0, PeripheralCategory.SegaMegaMouse));
            Assert.Throws<NotSupportedException>(() => core.SoftReset());
            Assert.Throws<NotSupportedException>(() => core.SelectDisk(0));
            Assert.Equal(before, core.SaveState());
            core.SetControllerPortDevice(1, PeripheralCategory.None);
            core.SetInput(EmulationInputSnapshot.Empty with { Controllers = [EmulationControllerState.Empty, EmulationControllerState.Empty with { Buttons = uint.MaxValue }] });
            Assert.Equal(0, core.Board.CaptureState().Pressed >> 6);
            core.HardReset(); Assert.Null(core.LatestVideoFrame); Assert.Equal(0, core.BufferedAudioFrames);
            core.RunFrame(); Assert.NotNull(core.LatestVideoFrame);
            core.Stop();
            Assert.Throws<ObjectDisposedException>(() => core.RunFrame());
        }
        finally { core?.Dispose(); }
    }

    private static byte[] BuildCartridge()
    {
        byte[] image = Enumerable.Repeat((byte)0xff, 8192).ToArray();
        new byte[] { 0xc3, 0x00, 0x01 }.CopyTo(image, 0);
        new byte[] { 0x3e, 0x42, 0x32, 0x13, 0xc0, 0xed, 0x45 }.CopyTo(image, 0x66);
        byte[] code = [0xf3, 0x31, 0xff, 0xc3, 0xdb, 0xdc, 0x32, 0x11, 0xc0,
            0xdb, 0xdd, 0x32, 0x12, 0xc0, 0x3e, 0x84, 0xd3, 0x7f,
            0x3e, 0x00, 0xd3, 0x7f, 0x3e, 0x90, 0xd3, 0x7f,
            0x76, 0xc3, 0x1a, 0x01];
        code.CopyTo(image, 0x100);
        return image;
    }
}
