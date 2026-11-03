using Phase = RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Enums.Intel8271Phase;
using C = RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Constants.Intel8271Constants;

namespace RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Services;

public sealed partial class Intel8271
{
    private bool _scanMatches;
    private bool _scanEqual;
    private int _scanFieldOffset;
    private byte _scanResult;

    private void BeginScan()
    {
        _scanResult = 0;
        _scanFieldOffset = 0;
        _special[C.ScanCountLow] = _special[C.ScanCountHigh] = 0;
    }

    private void ResetScanSector()
    {
        _scanMatches = _scanEqual = true;
        _scanFieldOffset = 0;
        UpdateScanCount();
    }

    private void UpdateScanCount()
    {
        var remaining = _sectorSize - _offset;
        _special[C.ScanCountHigh] = (byte)((remaining - 1) / C.MinimumSectorSize);
        _special[C.ScanCountLow] = (byte)(remaining == 0 ? 0 : (remaining - 1) % C.MinimumSectorSize + 1);
    }

    private void CompareScanByte(byte key)
    {
        var value = _buffer[_offset++];
        if (key != C.ScanWildcard)
        {
            var type = _parameters[3] >> C.ScanTypeShift;
            _scanMatches &= type switch { 0 => value == key, 1 => value >= key, 2 => value <= key, _ => false };
            _scanEqual &= value == key;
        }
        UpdateScanCount();
        if (++_scanFieldOffset != (_parameters[4] == 0 ? 256 : _parameters[4])) return;
        if (_scanMatches)
        {
            _scanResult = _scanEqual ? C.ScanEqual : C.ScanNotEqual;
            // Le verdict est rendu après le reste du secteur et son CRC, sans nouveau DMA pour les octets restants.
            _phase = Phase.ScanTail;
        }
        _scanFieldOffset = 0;
        _scanMatches = _scanEqual = true;
    }
}
