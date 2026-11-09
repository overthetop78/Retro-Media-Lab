using RetroMediaLab.Chips.Controllers.Acorn.SystemClock.Services;
using RetroMediaLab.Chips.Controllers.Acorn.Vdu80CharacterGenerator.Services;
using RetroMediaLab.Chips.Video.Motorola.M6845.Chip6845.Services;
using System.Reflection;
using RetroMediaLab.Emulation.Enums;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Enums;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Services;

namespace RetroMediaLab.Tests.Emulation;

public sealed class AcornEmuSystemMonochromeTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void StableIntervalsPreserveCompleteFieldsAndSerialTransitions(bool inverted, bool interlaced)
    {
        var rom = new byte[2048];
        for (var raster = 0; raster < 16; raster++) rom[16 + raster] = (byte)(raster * 37);
        var unitVideo = new SystemVideo(true, rom, inverted: inverted);
        var fastVideo = new SystemVideo(true, rom, inverted: inverted);
        var unit = Connect(unitVideo);
        var fast = Connect(fastVideo);
        var unitCircuit = Field<Vdu80CharacterGenerator>(unitVideo, "_monochrome");
        var fastCircuit = Field<Vdu80CharacterGenerator>(fastVideo, "_monochrome");
        var unitEvents = new List<(long Time, bool? White)>();
        var fastEvents = new List<(long Time, bool? White)>();
        unitCircuit.VideoChanged += RecordUnit;
        fastCircuit.VideoChanged += RecordFast;
        try
        {
            fast.Clock12MHzChanged = null;
            fast.Clock12MHzReceiver = fastVideo;
            fastVideo.StableClockCandidateChanged = high => fast.Clock12MHzCanGroup = high;
            fast.Clock12MHzCanGroup = fastVideo.StableClockCandidate;
            Program(unitVideo, interlaced ? (byte)0x43 : (byte)0x40, 3, true);
            Program(fastVideo, interlaced ? (byte)0x43 : (byte)0x40, 3, true);
            for (var field = 0; field < 8; field++)
            {
                for (var cell = 0; cell < 80; cell++)
                {
                    var character = (byte)((cell + field) % 5 == 0 ? 1 : 0);
                    unitVideo.Write(0x1000 + cell, character);
                    fastVideo.Write(0x1000 + cell, character);
                }
                RunToFrame(unitVideo, unit, field + 1);
                RunToFrame(fastVideo, fast, field + 1);
                Assert.Equal(unitVideo.CompletedFrame, fastVideo.CompletedFrame);
                Assert.Equal(unitVideo.ScanMode, fastVideo.ScanMode);
                Assert.Equal(unit.InputClockCount, fast.InputClockCount);
                Assert.Equal(unitCircuit.TimeNanoseconds, fastCircuit.TimeNanoseconds);
                Assert.Equal(unitCircuit.DotClockCount, fastCircuit.DotClockCount);
                Assert.Equal(unitCircuit.CharacterClockCount, fastCircuit.CharacterClockCount);
                Assert.Equal(unitEvents, fastEvents);
                unitEvents.Clear(); fastEvents.Clear();
            }
        }
        finally
        {
            unitCircuit.VideoChanged -= RecordUnit;
            fastCircuit.VideoChanged -= RecordFast;
            fastVideo.StableClockCandidateChanged = null;
            fast.Clock12MHzReceiver = null;
            Disconnect(unitVideo, unit);
            Disconnect(fastVideo, fast);
        }
        void RecordUnit(bool? white) => unitEvents.Add((unitCircuit.TimeNanoseconds, white));
        void RecordFast(bool? white) => fastEvents.Add((fastCircuit.TimeNanoseconds, white));
    }

    [Fact]
    public void ProgrammedCursorPulsesReachTheRcCircuitAtTheirCalculatedPhases()
    {
        var video = new SystemVideo(true, new byte[2048]);
        var source = Connect(video);
        var generator = Field<Vdu80CharacterGenerator>(video, "_monochrome");
        var rc = Field<Vdu80CursorCircuit>(generator, "_cursorCircuit");
        var crtc = Field<Crtc6845>(video, "_crtc");
        var raw = new List<(long Time, bool High)>();
        var delayed = new List<(long Time, bool High)>();
        Action<bool> rawObserver = high => raw.Add((generator.TimeNanoseconds, high));
        Action<bool> delayedObserver = high => delayed.Add((rc.TimeNanoseconds, high));
        crtc.CursorChanged += rawObserver;
        rc.OutputChanged += delayedObserver;
        try
        {
            Program(video, mode: 0x40, cursor: true);
            RunToFrame(video, source, 3);
            raw.Clear(); delayed.Clear();
            RunToFrame(video, source, 4);
            Assert.Equal(4, raw.Count); // Two scanlines, rising and falling CU on each.
            Assert.Equal(raw.Count, delayed.Count);
            for (var i = 0; i < raw.Count; i++)
            {
                Assert.Equal(raw[i].High, delayed[i].High);
                Assert.Equal(raw[i].High ? 167 : 135, delayed[i].Time - raw[i].Time);
            }
        }
        finally
        {
            crtc.CursorChanged -= rawObserver;
            rc.OutputChanged -= delayedObserver;
            Disconnect(video, source);
        }
    }

    [Fact]
    public void CpuResetKeepsTheCharacterAndDotClocksRunningInTheirComponents()
    {
        var rom = new byte[8192];
        rom[^3] = rom[^1] = 2;
        var board = new AcornSystemMachine(SystemCpuCard.Fast6502A, rom, new byte[2048], [], false);
        try
        {
            board.Write(0x200, 0x4c); board.Write(0x201, 0); board.Write(0x202, 2);
            for (var i = 0; i < 11; i++) board.Tick();
            var video = Field<SystemVideo>(board, "_video");
            var circuit = Field<Vdu80CharacterGenerator>(video, "_monochrome");
            var source = Field<SystemClockGenerator>(board, "_fastClock");
            var dots = circuit.DotClockCount;
            var characters = circuit.CharacterClockCount;
            var time = circuit.TimeNanoseconds;
            var sourceClocks = source.InputClockCount;
            board.Reset(false);
            Assert.Equal(sourceClocks + 108, source.InputClockCount);
            Assert.Equal(dots + 54, circuit.DotClockCount);
            Assert.Equal(characters + 9, circuit.CharacterClockCount);
            Assert.Equal(time + 4500, circuit.TimeNanoseconds);
        }
        finally { board.Reset(true); }
    }

    [Fact]
    public void NativeSerialVideoPreservesBothEdgesOfTheEightyCharacterViewport()
    {
        var rom = new byte[2048];
        rom[16] = 0xa8;
        rom[17] = 0x50;
        var video = new SystemVideo(true, rom);
        var source = Connect(video);
        try
        {
            Program(video);
            for (var address = 0x1000; address < 0x1050; address++) video.Write(address, 1);
            RunToFrame(video, source, 4);
            foreach (var cell in new[] { 0, 1, 79 })
            {
                Assert.Equal(new byte[] { 0, 255, 0, 255, 0, 255 }, Row(video, cell * 6, 0));
                Assert.Equal(new byte[] { 0, 0, 255, 0, 255, 0 }, Row(video, cell * 6, 1));
            }
            Assert.Equal(480 * 250 * 4, video.CompletedFrame.Length);
        }
        finally { Disconnect(video, source); }
    }

    [Fact]
    public void RamWritesReachTheSelectedCharacterAndAppearInTheNextField()
    {
        var rom = new byte[2048];
        rom[16] = rom[17] = 0xf8;
        var video = new SystemVideo(true, rom);
        var source = Connect(video);
        try
        {
            Program(video);
            RunToFrame(video, source, 3);
            Assert.Equal(new byte[6], Row(video, 0, 0));
            video.Write(0x1000, 0x81); // D7 is not a character address pin.
            RunToFrame(video, source, 5);
            Assert.Equal(new byte[] { 0, 255, 255, 255, 255, 255 }, Row(video, 0, 0));
            Assert.Equal(new byte[6], Row(video, 6, 0));
        }
        finally { Disconnect(video, source); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CursorXorsTheSerializedPatternAndTheInversionLink(bool inverted)
    {
        var rom = new byte[2048];
        rom[16] = rom[17] = 0xa8;
        var video = new SystemVideo(true, rom, inverted: inverted);
        var source = Connect(video);
        try
        {
            Program(video, mode: 0x40, cursor: true); // CU skew one character, as the ROM pipeline.
            video.Write(0x1000, 1);
            video.Write(0x1001, 1);
            RunToFrame(video, source, 4);
            // R12||R13 and C6 give tau=240.8108 ns. For a 500 ns CU pulse,
            // the 50% reference threshold falls 135 ns after its input falls.
            // The last dot at +208 ns is therefore outside CU. A full six-dot
            // inversion was not a valid expectation for this electrical profile.
            Assert.Equal(inverted ? new byte[] { 0, 255, 0, 255, 0, 0 }
                : new byte[] { 255, 0, 255, 0, 255, 255 }, Row(video, 0, 0));
            Assert.Equal(inverted ? new byte[] { 255, 0, 255, 0, 255, 0 }
                : new byte[] { 0, 255, 0, 255, 0, 255 }, Row(video, 6, 0));
        }
        finally { Disconnect(video, source); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NativeFieldsKeepTheirParityAndSampleTheMatchingRomRaster(bool interlaced)
    {
        var rom = new byte[2048];
        for (var raster = 0; raster < 16; raster++) rom[16 + raster] = (raster & 1) == 0 ? (byte)0xf8 : (byte)0x08;
        var video = new SystemVideo(true, rom);
        var source = Connect(video);
        try
        {
            Program(video, mode: interlaced ? (byte)3 : (byte)0, raster: interlaced ? (byte)3 : (byte)1);
            for (var address = 0x1000; address < 0x1050; address++) video.Write(address, 1);
            RunToFrame(video, source, 3);
            var modes = new List<EmulationVideoScanMode>();
            for (var field = 0; field < 4; field++)
            {
                var start = source.InputClockCount;
                RunToFrame(video, source, video.Frame + 1);
                modes.Add(video.ScanMode);
                Assert.Equal(video.CompletedFieldClocks * 12, source.InputClockCount - start);
                var expected = video.ScanMode == EmulationVideoScanMode.InterlacedOddField ? (byte)0 : (byte)255;
                Assert.Equal(expected, Row(video, 0, 0)[1]);
                Assert.Equal(255, Row(video, 0, 0)[5]);
            }
            if (interlaced)
            {
                Assert.DoesNotContain(EmulationVideoScanMode.Progressive, modes);
                for (var i = 1; i < modes.Count; i++) Assert.NotEqual(modes[i - 1], modes[i]);
            }
            else Assert.All(modes, mode => Assert.Equal(EmulationVideoScanMode.Progressive, mode));
        }
        finally { Disconnect(video, source); }
    }

    private static SystemClockGenerator Connect(SystemVideo video)
    {
        var source = new SystemClockGenerator();
        source.Clock12MHzChanged = video.SetDotClock;
        source.Clock2MHzChanged = high =>
        {
            video.SetCharacterClock(high);
            if (!high) video.TickCharacter();
        };
        return source;
    }

    private static void Disconnect(SystemVideo video, SystemClockGenerator source)
    {
        source.Clock12MHzChanged = source.Clock2MHzChanged = null;
        video.Reset();
    }

    private static void Program(SystemVideo video, byte mode = 0, byte raster = 1, bool cursor = false)
    {
        byte[] registers = [83, 80, 81, 0x11, 3, 0, 1, 2, mode, raster, cursor ? (byte)0 : (byte)0x20, raster, 0, 0, 0, 0];
        for (byte register = 0; register < registers.Length; register++)
        {
            video.Write(0x1840, register);
            video.Write(0x1841, registers[register]);
        }
    }

    private static void RunToFrame(SystemVideo video, SystemClockGenerator source, long target)
    {
        var limit = source.InputClockCount + 1_000_000;
        while (video.Frame < target && source.InputClockCount < limit) source.AdvanceClocks(12);
        Assert.Equal(target, video.Frame);
    }

    private static byte[] Row(SystemVideo video, int x, int y) =>
        Enumerable.Range(0, 6).Select(dot => video.CompletedFrame[(y * 480 + x + dot) * 4]).ToArray();

    private static T Field<T>(object owner, string name) =>
        (T)owner.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(owner)!;
}
