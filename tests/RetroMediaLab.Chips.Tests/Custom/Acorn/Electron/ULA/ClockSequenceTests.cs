using RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Interfaces;
using RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Services;

namespace RetroMediaLab.Chips.Tests.Custom.Acorn.Electron.ULA;

public sealed class ClockSequenceTests
{
    private sealed class Pins : IElectronRamPins
    {
        internal ElectronVideo? Video;
        internal Action? Mutate;
        internal int Trigger;
        internal readonly List<(byte Address, bool Ras, bool Cas, bool We, byte Data, bool Phi, int Color)> Trace = [];
        public byte Data { get; private set; }
        public void Drive(byte address, bool rowStrobeHigh, bool columnStrobeHigh, bool writeEnableHigh, byte data)
        {
            Trace.Add((address, rowStrobeHigh, columnStrobeHigh, writeEnableHigh, data, Video?.CpuClockHigh ?? true, Video?.PixelColor ?? 0));
            Data = (byte)((address ^ (address >> 4)) & 15);
            if (Trace.Count == Trigger)
            {
                var mutation = Mutate;
                Mutate = null;
                mutation?.Invoke();
            }
        }
    }

    public static TheoryData<int, int, int> Cases
    {
        get
        {
            var result = new TheoryData<int, int, int>();
            for (var mode = 0; mode < 8; mode++)
            for (var mutation = 0; mutation < 3; mutation++)
            foreach (var strobe in new[] { 3, 5 }) result.Add(mode, mutation, strobe);
            return result;
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void DirectClockMatchesPointSteppingThroughPinSideEffects(int mode, int mutation, int strobe)
    {
        var expected = Run(false, mode, mutation, strobe);
        var actual = Run(true, mode, mutation, strobe);
        Assert.Equal(expected.Trace, actual.Trace);
        Assert.Equal(expected.Phi, actual.Phi);
        Assert.Equal(expected.State, actual.State);
    }

    private static (object[] Trace, (bool High, int Dot)[] Phi, (int Mode, int Address, byte VideoData, int Color, byte CpuData, bool Pending) State)
        Run(bool grouped, int mode, int mutation, int strobe)
    {
        var pins = new Pins();
        var video = new ElectronVideo(pins);
        pins.Video = video;
        try
        {
            video.Reset(); video.SetMode((byte)(mode << 3));
            for (var register = 0; register < 8; register++) video.WritePalette(register, 0);
            video.Tick(); video.Tick();
            pins.Trace.Clear();
            if (mutation == 2)
            {
                video.BeginCpuRam(0x0200, true, 0xa5);
                video.ConfigureCpuRam(true, true, false);
            }
            pins.Trigger = strobe;
            pins.Mutate = mutation switch
            {
                1 => () => video.SetMode((byte)(((mode + 1) & 7) << 3)),
                2 => video.CancelCpuRam,
                _ => null
            };
            List<(bool High, int Dot)> phi = [];
            video.CpuClockChanged = (high, dot) => phi.Add((high, dot));
            if (grouped) { video.Tick(); video.Tick(); }
            else for (var point = 0; point < 16; point++) video.TickPixel();
            return (pins.Trace.Cast<object>().ToArray(), phi.ToArray(),
                (video.Mode, video.VideoAddress, video.VideoData, video.PixelColor, video.CpuRamData, video.CpuRamPending));
        }
        finally
        {
            pins.Mutate = null;
            video.CpuClockChanged = null;
            video.Reset();
            pins.Video = null;
        }
    }
}
