using RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Enums;
using C = RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Constants.ElectronVideoConstants;
using T = RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Constants.ElectronVideoTimingConstants;

namespace RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Services;

public sealed partial class ElectronVideo
{
    private byte _nextVideoData;
    private ushort _cpuRamAddress;
    private byte _cpuRamWriteData;
    private bool _cpuRamWrite;
    private bool _cpuEligible;
    private bool _cpuPriority;
    private bool _cpuTransfer;
    private bool _videoTransfer;

    internal bool CpuRamPending { get; private set; }
    internal byte CpuRamData { get; private set; }

    internal void BeginCpuRam(ushort address, bool write, byte data)
    {
        if (CpuRamPending) throw new InvalidOperationException();
        _cpuRamAddress = address;
        _cpuRamWrite = write;
        _cpuRamWriteData = data;
        CpuRamPending = true;
    }

    internal void ConfigureCpuRam(bool eligible, bool priority, bool clockHeld)
    {
        _cpuEligible = eligible;
        _cpuPriority = priority;
        _cpuClockDirty |= _cpuClockHeld != clockHeld;
        _cpuClockHeld = clockHeld;
    }

    internal void CancelCpuRam()
    {
        if (_cpuTransfer && _ram.Busy) _ram.Cancel();
        CpuRamPending = _cpuEligible = _cpuTransfer = false;
    }

    private void ResetRam()
    {
        _ram.Cancel();
        _nextVideoData = CpuRamData = 0;
        CpuRamPending = _cpuEligible = _cpuPriority = _cpuTransfer = _videoTransfer = false;
        _cpuClockHeld = _cpuClockDirty = false;
        CpuClockHigh = true;
    }

    private void TickRam(int dot)
    {
        if (_cpuClockDirty || (dot & T.CpuClockTransitionMask) == 0) TickCpuClock(dot);
        if ((dot & C.CpuPointMask) == 0) BeginRamClock(dot);
        if (_ram.Tick() == ElectronRamEvent.TransferComplete) CompleteRamClock();
    }

    private void BeginRamClock(int dot)
    {
        _videoTransfer = NextVideoAddress(dot, out var address);
        _cpuTransfer = CpuRamPending && _cpuEligible && (!_videoTransfer || _cpuPriority);
        if (_cpuTransfer)
            _ram.Begin(_cpuRamAddress, cpu: true, _cpuRamWrite, _cpuRamWriteData);
        else if (_videoTransfer)
            _ram.Begin(address, cpu: false);
        else
            // Blanking masks pixels, not the recurrent DRAM transactions.
            _ram.Begin((ushort)(_byteAddress | (_raster & C.RasterAddressMask)), cpu: false);
    }

    private void CompleteRamClock()
    {
        if (_cpuTransfer)
        {
            CpuRamData = _ram.Data;
            CpuRamPending = false;
        }
        if (_videoTransfer) _nextVideoData = _ram.Data;
    }

    private bool NextVideoAddress(int dot, out ushort address)
    {
        address = 0;
        var width = _bytePoints;
        // Low bandwidth reserves the first half of each 1 MHz period for the CPU.
        // Its VDU fetch in the second half completes before the next 16-point byte.
        var fetchDot = LowBandwidth ? dot - T.LowBandwidthFetchOffset : dot;
        if (fetchDot < 0) return false;
        if (fetchDot == T.ClocksPerLine - width)
        {
            var fieldLines = _timing.LongField ? T.LongFieldLines : T.ShortFieldLines;
            if (_line + 1 == fieldLines)
            {
                address = (ushort)FieldStart;
                return true;
            }
            if (_line + 1 >= _modeHeight || _raster >= C.ActiveRasters) return false;
            address = (ushort)(_byteAddress | (_raster & C.RasterAddressMask));
            return true;
        }
        if (fetchDot >= C.Width - width || (fetchDot & (width - 1)) != 0
            || _line >= _modeHeight || _raster >= C.ActiveRasters) return false;
        address = (ushort)(Wrap(_byteAddress + C.ActiveRasters) | (_raster & C.RasterAddressMask));
        return true;
    }
}
