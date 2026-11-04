using C = RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Constants.ElectronVideoConstants;
using T = RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Constants.ElectronVideoTimingConstants;
using RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Interfaces;

namespace RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Services;

/// <summary>Electron ULA pixel and memory counters, stepped at 16 MHz or in CPU periods.</summary>
public sealed partial class ElectronVideo : IElectronVideo
{
    private readonly ElectronRamSequencer _ram;
    private readonly ElectronVideoTiming _timing = new();
    private readonly byte[] _registers = new byte[C.PaletteRegisters];
    private readonly int[] _packedPalette = new int[C.PaletteEntries];
    private byte[] _drawing = new byte[C.FrameBytes];
    private byte[] _completed = new byte[C.FrameBytes];
    private int _line;
    private int _raster;
    private int _start = C.DefaultStart;
    private int _rowAddress = C.DefaultStart;
    private int _byteAddress = C.DefaultStart;
    private byte _data;
    private int _modeHeight = C.Height;
    private int _modeRasters = C.ActiveRasters;
    private int _bytePoints = C.ByteBits;
    private int _pixelShift;
    private int _penMask = C.TwoColorMask;

    public ElectronVideo(Func<int, byte> read) : this(new ElectronReadOnlyRamPins(read)) { }

    public ElectronVideo(IElectronRamPins ram) => _ram = new(ram);

    public int Mode { get; private set; }
    public int Line => _line;
    public int Dot => _timing.Dot;
    public int Clock => Dot / C.PixelsPerCycle;
    public long Frame { get; private set; }
    public byte[] CompletedFrame => _completed;
    public bool LowBandwidth => _bytePoints == C.LowBandwidthBytePoints;
    public bool HorizontalSyncActive => _timing.HorizontalSync;
    public bool CompositeSyncActive => _timing.CompositeSync;
    public bool ClockInterruptEdge { get; private set; }
    public bool DisplayEndInterruptEdge { get; private set; }
    public bool DisplayFetching => _line < _modeHeight && _raster < C.ActiveRasters && Dot < C.Width;
    public int VideoAddress { get; private set; } = C.DefaultStart;
    public byte VideoData => _data;
    public Action<int>? LineStarted { get; set; }

    public void Reset()
    {
        _line = _raster = 0;
        _timing.Reset();
        _start = _rowAddress = _byteAddress = VideoAddress = C.DefaultStart;
        SetMode(0);
        Frame = 0;
        _data = 0;
        ResetRam();
        _pixelColor = 0;
        ClockInterruptEdge = DisplayEndInterruptEdge = false;
        Array.Clear(_registers);
        Array.Clear(_packedPalette);
        Array.Clear(_drawing);
        Array.Clear(_completed);
    }

    public void SetMode(byte control)
    {
        Mode = (control >> C.ModeShift) & C.ModeMask;
        _modeHeight = C.ModeHeight[Mode];
        _modeRasters = C.ModeRasters[Mode];
        _bytePoints = (Mode & C.LowBandwidthMask) != 0 ? C.LowBandwidthBytePoints : C.ByteBits;
        _pixelShift = C.ModePixelShifts[Mode];
        var bits = C.ModeBits[Mode];
        _penMask = bits == C.OneBitPixel ? C.TwoColorMask
            : bits == C.TwoBitPixel ? C.FourColorMask : C.SixteenColorMask;
        if (_modeRasters == C.ActiveRasters) _raster &= C.RasterAddressMask;
    }

    public void WriteStart(bool high, byte value) => _start = high
        ? (_start & C.StartRetainedLowMask) | ((value & C.StartHighMask) << C.StartHighShift)
        : (_start & C.StartRetainedHighMask) | ((value & C.StartLowMask) << C.StartLowShift);

    public bool TickPixel()
    {
        var previousDot = Dot;
        TickRam(previousDot);
        RenderPoint(previousDot);
        _timing.Tick(_modeRasters == C.SpacedRasters);
        return FinishClock(previousDot);
    }

    public bool Tick()
    {
        // Aligned CPU periods never cross a line or a data-byte boundary internally.
        // Render the eight actual points, but update the equivalent timing counters once.
        var previousDot = Dot;
        if ((previousDot & C.CpuPointMask) == 0) return TickAlignedClock(previousDot);

        var field = false;
        var rtc = false;
        var display = false;
        for (var point = 0; point < C.PixelsPerCycle; point++)
        {
            field |= TickPixel();
            rtc |= ClockInterruptEdge;
            display |= DisplayEndInterruptEdge;
        }
        ClockInterruptEdge = rtc;
        DisplayEndInterruptEdge = display;
        return field;
    }

    private bool FinishClock(int previousDot)
    {
        ClockInterruptEdge = _timing.RtcEdge;
        DisplayEndInterruptEdge = _timing.DisplayEndEdge;
        AdvanceMemory(previousDot, Dot == 0 ? T.ClocksPerLine : Dot);
        _line = _timing.Line;
        if (_timing.FieldEnded)
        {
            _raster = 0;
            _rowAddress = _byteAddress = FieldStart;
            Frame++;
            (_drawing, _completed) = (_completed, _drawing);
        }
        if (Dot == 0) LineStarted?.Invoke(_line);
        return _timing.FieldEnded;
    }
}
