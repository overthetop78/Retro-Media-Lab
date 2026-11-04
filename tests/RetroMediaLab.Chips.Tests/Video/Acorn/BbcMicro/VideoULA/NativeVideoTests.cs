using BbcVideo = RetroMediaLab.Chips.Video.Acorn.BbcMicro.VideoULA.Services.Video;
using Xunit;

namespace RetroMediaLab.Chips.Tests.Video.Acorn.BbcMicro.VideoULA;

public sealed class NativeVideoTests
{
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
