using System.Runtime.InteropServices;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Constants.SystemVideoConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Services;

internal sealed partial class SystemVideo
{
    private int _teletextLineHalfDot;
    private bool _teletextDotLevel;
    private int _teletextRasterParity = -1;

    private void ConfigureTeletextPins()
    {
        if (_teletext is null) return;
        // Configuration VDU autonome normale déjà proposée par ce propriétaire.
        // Les entrées de commande TV distantes du connecteur ne sont pas reliées à un récepteur.
        _teletext.DisplayEnabled = true;
        _teletext.PictureOn = false;
        _teletext.SuperimposeInputHigh = false;
        _teletext.BigCharacterSelectHigh = true;
        _teletext.RemoteData = true;
        _teletext.SetRemoteClock(true);
    }

    private void ResetTeletextWiring()
    {
        _teletextLineHalfDot = 0;
        _teletextDotLevel = false;
        _teletextRasterParity = -1;
        ConfigureTeletextPins();
        _teletext?.SetDataEntryWindow(_crtc.VSync);
        _teletext?.SetGeneralLineReset(!_crtc.HSync);
        if (_teletext is not null) SetTeletextRasterAddress(_crtc.RasterAddress);
    }

    private void SetTeletextRasterAddress(int address)
    {
        var parity = address & 1;
        if (_teletextRasterParity == parity) return;
        _teletextRasterParity = parity;
        _teletext!.CharacterRoundingSelect = parity == 0;
    }

    private void ClockTeletext()
    {
        var chip = _teletext!;
        chip.Data = _ram[_crtc.MemoryAddress & (_ram.Length - 1)];
        chip.SetLoadOutputShiftEnable(_crtc.DisplayEnabled);
        chip.SetCharacterClock(true);
        chip.SetCharacterClock(false);
        var pixels = MemoryMarshal.Cast<byte, uint>(_drawing.AsSpan());
        var line = _crtc.Scanline;
        var lineAddress = _crtc.MemoryAddress - _crtc.Column;
        for (var halfDot = 0; halfDot < C.TeletextHalfDotsPerCharacter; halfDot++)
        {
            _teletextDotLevel = !_teletextDotLevel;
            chip.SetDotClock(_teletextDotLevel);
            var x = ++_teletextLineHalfDot - C.TeletextViewportHalfDots;
            if ((uint)x >= C.Width || (uint)line >= C.Height) continue;
            var cursor = _crtc.CursorForCell(lineAddress + x / C.TeletextCellPixels);
            pixels[line * C.Width + x] = C.TeletextColours[chip.Rgb ^ (cursor ? C.White : 0)];
        }
    }
}
