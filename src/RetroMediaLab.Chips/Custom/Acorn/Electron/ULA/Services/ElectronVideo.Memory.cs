using C = RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Constants.ElectronVideoConstants;
using T = RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Constants.ElectronVideoTimingConstants;

namespace RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Services;

public sealed partial class ElectronVideo
{
    private void AdvanceMemory(int previousDot, int nextDot)
    {
        if (previousDot < T.HorizontalSyncStart && nextDot >= T.HorizontalSyncStart)
        {
            if (_raster == _modeRasters - 1)
                _rowAddress = _byteAddress & C.RowAddressMask;
            else
                _byteAddress = _rowAddress;
        }
        if (previousDot < T.HorizontalSyncEnd && nextDot >= T.HorizontalSyncEnd)
        {
            if (++_raster >= _modeRasters) _raster = 0;
        }
    }

    private int FieldStart => Mode == C.LowStartObservedMode && _start < C.LowStartLimit
        && (_start & C.LowStartLinearBit) == 0
            ? C.DefaultStart | (_start & C.LowStartOffsetMask)
            : _start;

    private int Wrap(int address)
    {
        if (Mode == C.LowStartObservedMode && address == C.LowStartCarryAddress) return C.DefaultStart;
        return address >= C.VideoWrapAddress ? address - C.ModeBytes[Mode] : address;
    }
}
