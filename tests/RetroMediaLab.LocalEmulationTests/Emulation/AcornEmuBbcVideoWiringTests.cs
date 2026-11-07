using BbcVideo = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services.Video;
using RetroMediaLab.Chips.Video.Motorola.M6845.Chip6845.Services;
using System.Reflection;
using Xunit;
using RetroMediaLab.App.Functions.Rendering.Emulation;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Enums;

namespace RetroMediaLab.Tests.Emulation;

public sealed class AcornEmuBbcVideoWiringTests
{
    [Theory]
    [InlineData(200)]
    [InlineData(256)]
    public void InterlacedFieldsRetainTheirOwnLinesAndInvalidateHistoryOnModeChange(int height)
    {
        var video = new BbcVideo(Enumerable.Repeat(byte.MaxValue, 32768).ToArray(), ReadOnlySpan<byte>.Empty, height);
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var crtc = (Crtc6845)typeof(BbcVideo).GetField("_crtc", flags)!.GetValue(video)!;
        var display = new EmulationVideoFieldComposer();
        VideoFrame Field(byte[] pixels, long sequence) => new(pixels, 640, height, 640 * 4,
            EmulationPixelFormat.Xrgb8888, 4f / 3f, sequence, TimeSpan.FromMilliseconds(sequence * 20)) { ScanMode = video.ScanMode };
        try
        {
            Set(video, 0, 7); Set(video, 1, 6); Set(video, 2, 6); Set(video, 3, 0x11);
            Set(video, 4, 3); Set(video, 6, 2); Set(video, 7, 3); Set(video, 9, 2);
            Set(video, 10, 32); Set(video, 8, 3);
            video.WriteUla(0, 8);
            FinishFrame(video, 1);
            var redParity = crtc.RasterAddress & 1;
            video.WriteUla(1, 0xf6);
            FinishFrame(video, 2);
            Assert.Equal(height, video.OutputHeight);
            var red = video.Render();
            Assert.Equal(640 * height * 4, red.Length);
            Assert.Equal(redParity == 0 ? EmulationVideoScanMode.InterlacedEvenField : EmulationVideoScanMode.InterlacedOddField, video.ScanMode);
            Assert.Equal(255, red[2]);
            display.Receive(Field(red, 2));
            Assert.Equal(0, display.Compose()!.Pixels.Span[(redParity ^ 1) * 640 * 4 + 2]);

            video.WriteUla(1, 0xf3);
            FinishFrame(video, 3);
            var native = video.Render();
            Assert.Equal(redParity == 0 ? EmulationVideoScanMode.InterlacedOddField : EmulationVideoScanMode.InterlacedEvenField, video.ScanMode);
            Assert.Equal(255, native[0]);
            Assert.Equal(0, native[2]);
            display.Receive(Field(native, 3));
            var woven = display.Compose()!.Pixels.ToArray();
            Assert.Equal(255, woven[redParity * 640 * 4 + 2]);
            Assert.Equal(0, woven[redParity * 640 * 4]);
            Assert.Equal(255, woven[(redParity ^ 1) * 640 * 4]);
            Assert.Equal(0, woven[(redParity ^ 1) * 640 * 4 + 2]);
            Assert.Equal(255, red[2]);

            FinishFrame(video, 4);
            var blue = video.Render();
            Assert.Equal(255, blue[0]);
            Assert.Equal(255, blue[640 * 4]);
            Set(video, 8, 0);
            FinishFrame(video, 5);
            FinishFrame(video, 6);
            Assert.Equal(height, video.OutputHeight);
            Assert.Equal(640 * height * 4, video.Render().Length);
            Assert.Equal(EmulationVideoScanMode.Progressive, video.ScanMode);
            display.Receive(Field(video.Render(), 6));
            Set(video, 8, 3);
            FinishFrame(video, 7);
            FinishFrame(video, 8);
            var freshParity = crtc.RasterAddress & 1 ^ 1;
            display.Receive(Field(video.Render(), 8));
            var fresh = display.Compose()!.Pixels.ToArray();
            Assert.Equal(0, fresh[(freshParity ^ 1) * 640 * 4]);
            video.Reset();
            Assert.Equal(height, video.OutputHeight);
            Assert.All(video.Render(), value => Assert.Equal(0, value));
        }
        finally { video.Reset(); }
    }

    [Fact]
    public void BoardVideoSelectionFollowsEveryControlPaletteWriteAndReset()
    {
        var video = Create(new byte[32768]);
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var ula = (RetroMediaLab.Chips.Video.Acorn.BbcMicro.VideoULA.Services.BbcVideoUla)
            typeof(BbcVideo).GetField("_ula", flags)!.GetValue(video)!;
        try
        {
            for (var control = 0; control <= byte.MaxValue; control++)
            {
                video.WriteUla(0, (byte)control);
                Check(control);
                for (var palette = 0; palette <= byte.MaxValue; palette++)
                {
                    video.WriteUla(1, (byte)palette);
                    video.Tick();
                    Check(control);
                }
                video.Reset();
                Check(0);
            }
        }
        finally { video.Reset(); }

        void Check(int control)
        {
            Assert.Equal((control & 16) != 0, ula.FastCrtc);
            Assert.Equal((control & 2) != 0, ula.TeletextSelected);
        }
    }

    [Fact]
    public void CompactScanoutPreservesClippingOverwritesAndPaletteAtWriteTime()
    {
        var video = Create(new byte[32768]);
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var write = typeof(BbcVideo).GetMethod("WriteScanout", flags)!;
        var drawing = (uint[])typeof(BbcVideo).GetField("_drawing", flags)!.GetValue(video)!;
        typeof(BbcVideo).GetField("_completed", flags)!.SetValue(video, drawing);
        var expected = new uint[640 * 256];
        uint[] colours = [0xff000000, 0xffff0000, 0xff00ff00, 0xffffff00, 0xff0000ff, 0xffff00ff, 0xff00ffff, 0xffffffff];
        uint[] monochrome = [0xff000000, 0xff4c4c4c, 0xff959595, 0xffe1e1e1, 0xff1d1d1d, 0xff696969, 0xffb2b2b2, 0xffffffff];
        try
        {
            for (var pattern = 0; pattern < 4096; pattern++)
            {
                foreach (var mono in new[] { false, true })
                {
                    video.Monochrome = mono;
                    var left = (pattern * 17 + (mono ? 3 : 0)) % 660 - 10;
                    var line = new[] { 0, 1, 255 }[pattern % 3];
                    var output = (uint)(pattern | ((4095 - pattern) << 12));
                    write.Invoke(video, [left, line, output]);
                    for (var dot = 0; dot < 8; dot++)
                    {
                        var x = left + dot;
                        if ((uint)x < 640)
                            expected[line * 640 + x] = (mono ? monochrome : colours)[(output >> (dot * 3)) & 7];
                    }
                }
                if ((pattern & 127) == 127)
                {
                    var actual = video.Render();
                    Assert.Equal(System.Runtime.InteropServices.MemoryMarshal.AsBytes(expected.AsSpan()).ToArray(), actual);
                }
            }
        }
        finally { video.Reset(); }
    }

    [Theory]
    [InlineData(200)]
    [InlineData(256)]
    public void ScanoutRetainsEveryAlignmentEdgeAndUnwrittenPixel(int height)
    {
        var video = new BbcVideo(new byte[32768], new byte[960], height);
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var write = (Action<int, int, uint>)typeof(BbcVideo).GetMethod("WriteScanout", flags)!
            .CreateDelegate(typeof(Action<int, int, uint>), video);
        var drawing = (uint[])typeof(BbcVideo).GetField("_drawing", flags)!.GetValue(video)!;
        typeof(BbcVideo).GetField("_completed", flags)!.SetValue(video, drawing);
        var expected = new uint[640 * height];
        uint[] colours = [0xff000000, 0xffff0000, 0xff00ff00, 0xffffff00, 0xff0000ff, 0xffff00ff, 0xff00ffff, 0xffffffff];
        uint[] monochrome = [0xff000000, 0xff4c4c4c, 0xff959595, 0xffe1e1e1, 0xff1d1d1d, 0xff696969, 0xffb2b2b2, 0xffffffff];
        try
        {
            foreach (var mono in new[] { false, true })
            foreach (var line in new[] { 0, height - 1 })
            foreach (var left in Enumerable.Range(-8, 24).Concat(Enumerable.Range(624, 17)))
            {
                video.Monochrome = mono;
                var output = (uint)((left * 7919) & 0xffffff);
                write(left, line, output);
                for (var dot = 0; dot < 8; dot++)
                {
                    var x = left + dot;
                    if ((uint)x < 640) expected[line * 640 + x] = (mono ? monochrome : colours)[(output >> (dot * 3)) & 7];
                }
                Assert.Equal(System.Runtime.InteropServices.MemoryMarshal.AsBytes(expected.AsSpan()).ToArray(), video.Render());
            }
        }
        finally { video.Reset(); }
    }

    [Fact]
    public void PreparedMemoryAddressPreservesEveryReadAcrossClocksWritesAndReset()
    {
        foreach (var highBank in new[] { false, true })
        foreach (var size in new[] { 16384, 32768 })
        {
            var video = Create(new byte[size], highBankOnly: highBank);
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var crtc = (Crtc6845)typeof(BbcVideo).GetField("_crtc", flags)!.GetValue(video)!;
            var phase = typeof(BbcVideo).GetField("_phase", flags)!;
            var reads = 0;
            video.ReadMemory = address =>
            {
                var ma = crtc.MemoryAddress;
                var high = (ma >> 8) & 15;
                if ((ma & 0x1000) != 0) high = (high + (video.ScreenWrap >> 11)) & 15;
                var expected = (ma & 0x2000) != 0
                    ? 0x3c00 | (ma & 0x3ff) | ((highBank ? 8 : high & 8) << 11)
                    : ((ma & 255) << 3) | (high << 11) | (crtc.RasterAddress & 7);
                Assert.Equal(expected & (size - 1), address);
                return (byte)++reads;
            };
            try
            {
                for (var cycle = 0; cycle < 8192; cycle++)
                {
                    if (cycle % 503 == 0) video.Reset();
                    if (cycle % 11 == 0) video.ScreenWrap = new[] { 0x4000, 0x6000, 0x3000, 0x5800 }[(cycle / 11) & 3];
                    if (cycle % 19 == 0) Set(video, (byte)(cycle % 18), (byte)(cycle >> 2));
                    var fast = (cycle & 32) != 0;
                    video.WriteUla(0, (byte)(fast ? 16 : 0));
                    var nextPhase = (int)phase.GetValue(video)! ^ 1;
                    var expectedReads = fast || nextPhase == 0 ? 1 : 0;
                    var before = reads;
                    video.Tick();
                    Assert.Equal(expectedReads, reads - before);
                }
            }
            finally { video.ReadMemory = null; video.Reset(); }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void VideoLatchAndVidprocReceiveTheSameRamTransfer(bool fast)
    {
        var video = Create(new byte[32768], 0x2000);
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var crtc = (Crtc6845)typeof(BbcVideo).GetField("_crtc", flags)!.GetValue(video)!;
        var ula = (RetroMediaLab.Chips.Video.Acorn.BbcMicro.VideoULA.Services.BbcVideoUla)
            typeof(BbcVideo).GetField("_ula", flags)!.GetValue(video)!;
        var teletext = (RetroMediaLab.Chips.Video.Philips.SAA50xx.SAA5050.Services.TeletextSaa5050)
            typeof(BbcVideo).GetField("_teletext", flags)!.GetValue(video)!;
        var latch = typeof(BbcVideo).GetField("_teletextLatchedData", flags)!;
        var phase = typeof(BbcVideo).GetField("_phase", flags)!;
        var reads = 0;
        byte busData = 0;
        video.ReadMemory = _ => busData = (byte)++reads;
        try
        {
            video.WriteUla(0, (byte)(fast ? 16 : 0));
            for (var tick = 0; tick < 2048; tick++)
            {
                var nextPhase = (int)phase.GetValue(video)! ^ 1;
                var previousLatch = (byte)latch.GetValue(video)!;
                var teletextAddress = (crtc.MemoryAddress & 0x2000) != 0;
                var before = reads;
                video.Tick();
                Assert.Equal(fast || nextPhase == 0 ? 1 : 0, reads - before);
                if (fast || nextPhase == 0) Assert.Equal(busData, ula.Data);
                if (nextPhase == 0)
                {
                    Assert.Equal(previousLatch & 0x7f, teletext.Data);
                    Assert.Equal(teletextAddress ? busData : (byte)0, (byte)latch.GetValue(video)!);
                }
                else Assert.Equal(previousLatch, (byte)latch.GetValue(video)!);
            }
        }
        finally { video.ReadMemory = null; video.Reset(); }
    }

    [Fact]
    public void BoardVideoClocksContinueWithoutInstructionsAndCpuWritesWaitForTheNextVideoSlot()
    {
        var board = new RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services.BbcMicroMachine(
            new byte[16384], new byte[16384], 32768, expandedIo: true);
        var video = board.Video;
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var ula = (RetroMediaLab.Chips.Video.Acorn.BbcMicro.VideoULA.Services.BbcVideoUla)
            typeof(BbcVideo).GetField("_ula", flags)!.GetValue(video)!;
        try
        {
            video.Reset();
            Set(video, 0, 7); Set(video, 1, 6); Set(video, 2, 6); Set(video, 3, 0x11);
            Set(video, 4, 3); Set(video, 6, 2); Set(video, 7, 3); Set(video, 9, 2);
            video.WriteUla(0, 8);
            board.Write(0, 0x35);
            var cycles = board.Cpu.Cycles;
            board.Tick();
            Assert.Equal(0x35, ula.Data);
            board.Write(8, 0x5a);
            Assert.Equal(0x35, ula.Data);
            board.Tick();
            Assert.Equal(0x35, ula.Data);
            board.Tick();
            Assert.Equal(0x5a, ula.Data);
            var frame = video.Frame;
            for (var tick = 0; tick < 1024; tick++) board.Tick();
            Assert.True(video.Frame > frame);
            Assert.Equal(cycles, board.Cpu.Cycles);
        }
        finally { video.Reset(); }
    }

    [Fact]
    public void BoardCrtcSignalsFollowEveryClockRegisterWriteAndReset()
    {
        var video = Create(new byte[32768]);
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var type = typeof(BbcVideo);
        var crtc = (Crtc6845)type.GetField("_crtc", flags)!.GetValue(video)!;
        var pins = new (string Property, string Field)[]
        {
            (nameof(Crtc6845.VSync), "_crtcVSync"),
            (nameof(Crtc6845.RasterAddress), "_crtcRaster"),
            (nameof(Crtc6845.MemoryAddress), "_crtcMemoryAddress"),
            (nameof(Crtc6845.DisplayEnabled), "_crtcDisplayEnabled"),
            (nameof(Crtc6845.Cursor), "_crtcCursor"),
            (nameof(Crtc6845.DisplayColumn), "_crtcDisplayColumn"),
            (nameof(Crtc6845.Scanline), "_crtcScanline"),
            (nameof(Crtc6845.IsProgrammed), "_crtcProgrammed")
        }.Select(pin => (Property: typeof(Crtc6845).GetProperty(pin.Property)!, Field: type.GetField(pin.Field, flags)!)).ToArray();
        try
        {
            for (var cycle = 0; cycle < 4096; cycle++)
            {
                if (cycle % 211 == 0) { video.Reset(); CheckPins(); }
                if (cycle % 37 == 0) { Set(video, (byte)(cycle % 18), (byte)cycle); CheckPins(); }
                video.WriteUla(0, (byte)cycle);
                video.Tick();
                CheckPins();
            }
        }
        finally { video.Reset(); }

        void CheckPins()
        {
            foreach (var pin in pins) Assert.Equal(pin.Property.GetValue(crtc), pin.Field.GetValue(video));
            Assert.Equal(crtc.VSync, video.VSync);
        }
    }

    private static void Set(BbcVideo video, byte index, byte value) { video.Select(index); video.WriteRegister(value); }
    private static BbcVideo Create(byte[] ram, int address = 0, bool highBankOnly = false)
    {
        var video = new BbcVideo(ram, ReadOnlySpan<byte>.Empty, 256, teletextHighBankOnly: highBankOnly);
        Set(video,0,7); Set(video,1,6); Set(video,2,6); Set(video,3,0x11);
        Set(video,4,3); Set(video,6,2); Set(video,7,3); Set(video,9,1); Set(video,10,32);
        Set(video,12,(byte)(address >> 8)); Set(video,13,(byte)address);
        video.WriteUla(0,8);
        return video;
    }
    private static void FinishFrame(BbcVideo video, long frame)
    {
        var limit = 10000;
        while (video.Frame < frame && --limit > 0) video.Tick();
        Assert.Equal(frame, video.Frame);
    }

    [Fact]
    public void PaletteWriteBetweenCpuCyclesDoesNotRepaintTheFirstHalfOfASlowCell()
    {
        var video = Create(Enumerable.Repeat((byte)255,32768).ToArray());
        try
        {
            video.WriteUla(1,0xf6);
            FinishFrame(video,1);
            video.Tick();
            video.WriteUla(1,0xf3);
            video.Tick();
            FinishFrame(video,2);
            var image=video.Render();
            for(var x=0;x<16;x++)
            {
                Assert.Equal(x<8?255:0,image[x*4+2]);
                Assert.Equal(x<8?0:255,image[x*4]);
            }
        }
        finally { video.Reset(); }
    }

    [Fact]
    public void NativeCursorRetainsAllEightSegmentPatterns()
    {
        for(var pattern=0;pattern<8;pattern++)
        {
            var video=Create(new byte[32768]);
            try
            {
                Set(video,10,0); Set(video,11,0);
                video.WriteUla(0,(byte)(8|(pattern<<5)));
                FinishFrame(video,2);
                var image=video.Render();
                for(var x=0;x<80;x++)
                {
                    var cell=x/16;
                    var active=cell switch {0=>(pattern&4)!=0,1=>(pattern&2)!=0,2 or 3=>(pattern&1)!=0,_=>false};
                    Assert.Equal(active?255:0,image[x*4+2]);
                    Assert.Equal(active?255:0,image[x*4+1]);
                    Assert.Equal(active?255:0,image[x*4]);
                }
            }
            finally { video.Reset(); }
        }
    }

    [Fact]
    public void NativeBlankingUsesTheRa3WireAndDisplaysAgainWhenRa4AloneIsHigh()
    {
        var video = Create(Enumerable.Repeat((byte)255, 32768).ToArray());
        try
        {
            Set(video,9,31);
            video.WriteUla(1,0xf6);
            FinishFrame(video,2);
            var image=video.Render();
            for(var line=0;line<32;line++)
            {
                var expected=(line&8)==0?255:0;
                Assert.Equal(expected,image[(line*640)*4+2]);
            }
        }
        finally {video.Reset();}
    }

    [Fact]
    public void DisplaySkewDoesNotChangeTheAddressDrivenOnTheRamBus()
    {
        for (var skew = 0; skew < 4; skew++)
        {
            var video = Create(new byte[32768]);
            try
            {
                Set(video, 8, (byte)(skew << 4));
                FinishFrame(video, 1);
                var addresses = new List<int>();
                video.ReadMemory = address => { addresses.Add(address); return 0; };
                for (var cycle = 0; cycle < 6; cycle++) video.Tick();
                Assert.Equal(new[] { 0, 8, 16 }, addresses);
            }
            finally { video.Reset(); }
        }
    }

    [Fact]
    public void AddressSelectionFollowsTheBoardNandAdderAndMa13RatherThanTheUlaMode()
    {
        foreach(var ma in new[]{0,0x800,0xfff,0x1000,0x17ff,0x1fff,0x2000,0x2400,0x2800,0x2fff,0x3000,0x37ff,0x3fff})
        foreach(var pins in Enumerable.Range(0,4))
        foreach(var ramSize in new[]{16384,32768})
        foreach(var highBank in new[]{false,true})
        {
            var video=Create(new byte[ramSize],ma,highBank);
            try
            {
                var wraps=new[]{0x4000,0x6000,0x3000,0x5800};
                video.ScreenWrap=wraps[pins];
                FinishFrame(video,1);
                int? first=null;
                video.ReadMemory=address=>{first??=address;return 0;};
                video.Tick();video.Tick();
                var c0=pins&1;var c1=pins>>1;var m12=(ma>>12)&1;
                var b1=1-(c0&c1&m12);var b3=1-(c0&m12);
                var b2=1-(c1&b3&m12);var b4=1-(b3&m12);
                var high=((ma>>8)+(b1|b2<<1|b3<<2|b4<<3)+1)&15;
                var expected=(ma&0x2000)!=0
                    ? 0x3c00|(ma&0x3ff)|((highBank?8:high&8)<<11)
                    : (ma&255)*8|(high<<11);
                Assert.Equal(expected&(ramSize-1),first);
            }
            finally { video.Reset(); }
        }
    }
}
