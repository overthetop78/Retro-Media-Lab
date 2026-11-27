using System.Diagnostics;
using System.IO;
using RetroMediaLab.Chips.CPU.Hitachi.SH4;
using RetroMediaLab.Chips.Dreamcast.Holly;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2;

using static PvrRegisters;

internal static unsafe partial class PvrScanGenerator
{
    private static uint _elapsedScanlineCycles;
    private static uint _scanlineCount = 512;
    private static uint _currentScanline = uint.MaxValue;

#if DEBUG || DEBUGFAST
    private static uint _vblankCount;
    private static ulong _lastFps;
    private static int _lastFrameCount;
#endif

    private const int PixelClock = 27 * 1000 * 1000;
    private static uint _lineCycles;
    private static uint _frameCycles;
    internal static int RenderEndScheduleId;
    internal static int VblankScheduleId;

    private static readonly double[] RealTimes = new double[4];
    private static readonly ulong[] CpuCycles = new ulong[4];
    private static uint _cpuTimeIndex;
    internal static bool SH4FastEnough;
    internal static uint FrameSkip;

    private static uint _lightgunLine = 0xffff;
    private static uint _lightgunHPosition;
    private static bool _mapleInterruptPending;
    private static uint _lightgunFlip;

    private static Sh4Scheduler Scheduler => Sh4Context.Active.Scheduler;

    internal static partial void CalculateSync()
    {
        uint pixelClock = (uint)(PixelClock / (FB_R_CTRL.vclk_div != 0 ? 1 : 2));

        _scanlineCount = SPG_LOAD.vcount + 1;

        _lineCycles = (uint)((ulong)Sh4Scheduler.MainClock * (SPG_LOAD.hcount + 1) / pixelClock);
        if (SPG_CONTROL.interlace != 0)
            _lineCycles /= 2;

        _frameCycles = _scanlineCount * _lineCycles;
        _currentScanline = 0;
        _elapsedScanlineCycles = 0;

        Scheduler.Request(VblankScheduleId, (int)_lineCycles);

#if LIBRETRO
        PvrPlatform.SetRetroFps(Sh4Scheduler.MainClock / ((double)_lineCycles * _scanlineCount));
#endif
    }

    private static int GetNextSpgInterrupt()
    {
        if (SPG_HBLANK_INT.hblank_int_mode == 2)
            return (int)_lineCycles;

        uint minimumScanline = _currentScanline + 1;
        uint minimumActive = _scanlineCount;

        if (minimumScanline <= SPG_VBLANK_INT.vblank_in_interrupt_line_number)
            minimumActive = Math.Min(minimumActive, SPG_VBLANK_INT.vblank_in_interrupt_line_number);

        if (minimumScanline <= SPG_VBLANK_INT.vblank_out_interrupt_line_number)
            minimumActive = Math.Min(minimumActive, SPG_VBLANK_INT.vblank_out_interrupt_line_number);

        if (minimumScanline <= SPG_VBLANK.vstart)
            minimumActive = Math.Min(minimumActive, SPG_VBLANK.vstart);

        if (minimumScanline <= SPG_VBLANK.vbend)
            minimumActive = Math.Min(minimumActive, SPG_VBLANK.vbend);

        if (_lightgunLine != 0xffff && minimumScanline <= _lightgunLine)
            minimumActive = Math.Min(minimumActive, _lightgunLine);

        if (SPG_HBLANK_INT.hblank_int_mode == 0 && minimumScanline <= SPG_HBLANK_INT.line_comp_val)
            minimumActive = Math.Min(minimumActive, SPG_HBLANK_INT.line_comp_val);

        minimumActive = Math.Max(minimumActive, minimumScanline);

        return (int)((minimumActive - _currentScanline) * _lineCycles);
    }

    internal static partial void Reschedule()
    {
        Scheduler.Request(VblankScheduleId, GetNextSpgInterrupt());
    }

    private static int LineScheduleCallback(int tag, int cycles, int jitter, nint argument)
    {
        _elapsedScanlineCycles += unchecked((uint)(cycles + jitter));

        while (_elapsedScanlineCycles >= _lineCycles)
        {
            _currentScanline = (_currentScanline + 1) % _scanlineCount;
            _elapsedScanlineCycles -= _lineCycles;

            if (SPG_VBLANK_INT.vblank_in_interrupt_line_number == _currentScanline)
            {
                if (_mapleInterruptPending)
                {
                    _mapleInterruptPending = false;
                    HollySystemBus.SB_MDST = 0;
                }
                HollyInterruptController.RaiseInterrupt(HollyInterruptId.ScanInt1);
            }

            if (SPG_VBLANK_INT.vblank_out_interrupt_line_number == _currentScanline)
            {
                MapleBus.Vblank();
                HollyInterruptController.RaiseInterrupt(HollyInterruptId.ScanInt2);
            }

            if (SPG_VBLANK.vstart == _currentScanline)
                SPG_STATUS.vsync = 1;

            if (SPG_VBLANK.vbend == _currentScanline)
                SPG_STATUS.vsync = 0;

            SPG_STATUS.scanline = _currentScanline;

            switch (SPG_HBLANK_INT.hblank_int_mode)
            {
                case 0:
                    if (_currentScanline == SPG_HBLANK_INT.line_comp_val)
                        HollyInterruptController.RaiseInterrupt(HollyInterruptId.HBlank);
                    break;
                case 2:
                    HollyInterruptController.RaiseInterrupt(HollyInterruptId.HBlank);
                    break;
                case 1:
                    Trace.TraceWarning("Unimplemented HBLANK INT mode");
                    break;
                default:
                    Trace.TraceInformation("Invalid HBLANK INT mode");
                    break;
            }

            if (_currentScanline == 0)
            {
                if (SPG_CONTROL.interlace != 0)
                    SPG_STATUS.fieldnum = ~SPG_STATUS.fieldnum;
                else
                    SPG_STATUS.fieldnum = 0;

                PvrRenderer.Vblank();

                ulong now = PvrPlatform.GetTimeMs();
                _cpuTimeIndex = (_cpuTimeIndex + 1) % (uint)CpuCycles.Length;
                if (CpuCycles[_cpuTimeIndex] != 0)
                {
                    uint cycleSpan = (uint)(Scheduler.Now64() - CpuCycles[_cpuTimeIndex]);
                    ulong timeSpan = (ulong)(now - RealTimes[_cpuTimeIndex]);
                    float cpuSpeed = ((float)cycleSpan / timeSpan) / (Sh4Scheduler.MainClock / 100000);
                    SH4FastEnough = cpuSpeed >= 85f;
                }
                else
                {
                    SH4FastEnough = false;
                }
                CpuCycles[_cpuTimeIndex] = Scheduler.Now64();
                RealTimes[_cpuTimeIndex] = now;

#if TEST_AUTOMATION
                PvrInput.ReplayInput();
#endif

#if DEBUG || DEBUGFAST
                _vblankCount++;
                if (PvrPlatform.GetTimeMs() - _lastFps >= 2000)
                {
                    double seconds = ((double)PvrPlatform.GetTimeMs() - _lastFps) / 1000.0;
                    double framesPerSecond = unchecked((uint)PvrRenderer.FrameCount - (uint)_lastFrameCount) / seconds;
                    double vblanksPerSecond = _vblankCount / seconds;
                    double cpuSpeed = vblanksPerSecond * _frameCycles;
                    cpuSpeed /= 1000000.0;
                    double fullVblanks = (vblanksPerSecond / cpuSpeed) * 200.0;

                    _lastFrameCount = (int)PvrRenderer.FrameCount;
                    _vblankCount = 0;

                    string mode;
                    string resolution = SPG_CONTROL.interlace != 0 ? "480i" : "240p";

                    if (SPG_CONTROL.IsPAL())
                        mode = "PAL";
                    else if (SPG_CONTROL.IsNTSC())
                        mode = "NTSC";
                    else
                    {
                        resolution = SPG_CONTROL.interlace != 0 ? "480i" : "480p";
                        mode = "VGA";
                    }

                    double framesDone = cpuSpeed / 2;
                    double millisecondsPerDrawnFrame = 1 / framesDone * 1000;

                    double fullRenderRate = framesPerSecond + FrameSkip / seconds;

                    Trace.TraceInformation($"SPG - {millisecondsPerDrawnFrame:F2} - {cpuSpeed * 100 / 200:F2} - V: {vblanksPerSecond:F2} ({vblanksPerSecond / fullRenderRate:F2}, {mode}{resolution}{fullVblanks:F2}) R: {framesPerSecond:F2}+{FrameSkip / seconds:F2}");

                    FrameSkip = 0;
                    _lastFps = PvrPlatform.GetTimeMs();
                }
#endif
            }
            if (_lightgunLine != 0xffff && _lightgunLine == _currentScanline)
            {
                _mapleInterruptPending = false;
                SPG_TRIGGER_POS = ((_lightgunLine & 0x3ff) << 16) | (_lightgunHPosition & 0x3ff);
                HollySystemBus.SB_MDST = 0;
                _lightgunLine = 0xffff;
            }
        }

        return GetNextSpgInterrupt();
    }

    internal static partial void ReadLightgunPosition(int x, int y)
    {
        _mapleInterruptPending = true;
        if (y < 0 || y >= 480 || x < 0 || x >= 640)
        {
            _lightgunLine = 0xffff;
        }
        else
        {
            _lightgunLine = (uint)(y / (SPG_CONTROL.interlace != 0 ? 2 : 1)) + SPG_VBLANK_INT.vblank_out_interrupt_line_number;
            _lightgunLine ^= (_lightgunFlip >> 1) & 1;
            _lightgunHPosition = (uint)(x + 286) ^ (_lightgunFlip & 1);
            _lightgunFlip++;
        }
    }

    internal static partial bool Init()
    {
        RenderEndScheduleId = Scheduler.Register(0, PvrRenderer.EndRender);
        VblankScheduleId = Scheduler.Register(0, LineScheduleCallback);

        return true;
    }

    internal static partial void Term()
    {
        Scheduler.Unregister(RenderEndScheduleId);
        RenderEndScheduleId = -1;
        Scheduler.Unregister(VblankScheduleId);
        VblankScheduleId = -1;
    }

    internal static partial void Reset(bool hard)
    {
        CalculateSync();

        SH4FastEnough = false;
        _cpuTimeIndex = 0;
        Array.Clear(CpuCycles);
        Array.Clear(RealTimes);
        _mapleInterruptPending = false;
        _lightgunLine = 0xffff;
        _lightgunHPosition = 0;
    }

    internal static partial void ScheduleRenderDone(TaContext? context)
    {
        int cycles = 4096;
        if (context is not null)
        {
            if (Sh4Context.Active.Platform.IsNaomi2)
            {
                cycles = 1500000;
            }
            else
            {
                int size = 0;
                for (TaContext? current = context; current is not null; current = current.NextContext)
                    size += (int)(current.Tad.ThdData - current.Tad.ThdRoot);
                cycles = Math.Min(450000 + size * 100, 1500000);
            }
        }
        Scheduler.Request(RenderEndScheduleId, cycles);
    }

    private const int SaveVersion30 = 825;

    internal static partial void Serialize(BinaryWriter writer)
    {
        writer.Write(_elapsedScanlineCycles);
        writer.Write(_mapleInterruptPending);
        writer.Write(_scanlineCount);
        writer.Write(_currentScanline);
        writer.Write(_lineCycles);
        writer.Write(_frameCycles);
        writer.Write(_lightgunLine);
        writer.Write(_lightgunHPosition);
    }

    internal static partial void Deserialize(BinaryReader reader, int version)
    {
        if (version < SaveVersion30)
            reader.ReadUInt32();
        _elapsedScanlineCycles = reader.ReadUInt32();
        _mapleInterruptPending = reader.ReadBoolean();
        _scanlineCount = reader.ReadUInt32();
        _currentScanline = reader.ReadUInt32();
        _lineCycles = reader.ReadUInt32();
        _frameCycles = reader.ReadUInt32();
        _lightgunLine = reader.ReadUInt32();
        _lightgunHPosition = reader.ReadUInt32();
    }
}
