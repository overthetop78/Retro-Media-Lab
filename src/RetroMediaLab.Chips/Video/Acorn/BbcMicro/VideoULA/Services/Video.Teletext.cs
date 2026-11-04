using System.Runtime.InteropServices;
using VideoConstants = RetroMediaLab.Chips.Video.Acorn.BbcMicro.VideoULA.Constants.VideoConstants;

namespace RetroMediaLab.Chips.Video.Acorn.BbcMicro.VideoULA.Services;

public sealed partial class Video
{
    private byte _teletextLatchedData = VideoConstants.TeletextSpace;
    private int _teletextLineHalfDot;
    private bool _teletextDotLevel;

    private void ConfigureTeletextPins()
    {
        // IC5, schéma BBC : DE/BCS/DATA/DLIM au +5 V ; PO/SI/GLR à la masse.
        _teletext.DisplayEnabled = true;
        _teletext.BigCharacterSelectHigh = true;
        _teletext.RemoteData = true;
        _teletext.PictureOn = false;
        _teletext.SuperimposeInputHigh = false;
        _teletext.SetGeneralLineReset(false);
        _teletext.SetRemoteClock(true);
    }

    private void ResetTeletextWiring()
    {
        _teletextLatchedData = VideoConstants.TeletextSpace;
        _teletextLineHalfDot = 0;
        _teletextDotLevel = false;
        ConfigureTeletextPins();
    }

    private void ClockTeletext()
    {
        _teletext.SetDataEntryWindow(_crtc.VSync);
        _teletext.CharacterRoundingSelect = (_crtc.RasterAddress & 1) == 0;
        if (_phase == 0)
        {
            // Le SAA5050 prend le mot de l'IC15 avant sa mise à jour suivante.
            _teletext.Data = _teletextLatchedData;
            _teletext.SetCharacterClock(false);
            _teletext.SetLoadOutputShiftEnable(_crtc.DisplayEnabled);
            var address = _crtc.MemoryAddress;
            if ((_control & VideoConstants.TeletextControlBit) != 0)
                address = VideoConstants.TeletextRamBase | (address & VideoConstants.TeletextRamMask);
            else
            {
                address = (address << 3) | _crtc.RasterAddress;
                if (address >= 0x8000) address = (address + ScreenWrap) & 0x7fff;
            }
            var offset = address & (_ram.Length - 1);
            _teletextLatchedData = ReadMemory?.Invoke(offset) ?? _ram[offset];
        }
        else _teletext.SetCharacterClock(true);

        var draw = (_control & VideoConstants.TeletextControlBit) != 0 && _crtc.IsProgrammed && _crtc.Scanline < Height;
        var pixels = MemoryMarshal.Cast<byte, uint>(_drawing.AsSpan());
        var colours = Monochrome ? VideoConstants.PackedMonochrome : VideoConstants.PackedColours;
        for (var halfDot = 0; halfDot < VideoConstants.TeletextHalfDotsPerCpuClock; halfDot++)
        {
            _teletextDotLevel = !_teletextDotLevel;
            _teletext.SetDotClock(_teletextDotLevel);
            var position = ++_teletextLineHalfDot - VideoConstants.TeletextViewportHalfDots;
            if (!draw || position < 0) continue;
            var first = (position * VideoConstants.TeletextPixelNumerator + VideoConstants.TeletextPixelDenominator - 1) / VideoConstants.TeletextPixelDenominator;
            if (first >= VideoConstants.Width) continue;
            var last = Math.Min(VideoConstants.Width,
                ((position + 1) * VideoConstants.TeletextPixelNumerator + VideoConstants.TeletextPixelDenominator - 1) / VideoConstants.TeletextPixelDenominator);
            var lineAddress = (_crtc.MemoryAddress - _crtc.Column) & VideoConstants.CrtcMemoryMask;
            for (var x = first; x < last; x++)
            {
                // Le curseur reste fourni par le chemin logique de l'ULA existante ;
                // il n'est pas un attribut ni une sortie du SAA5050.
                var cursor = _crtc.CursorForCell((lineAddress + x / VideoConstants.TeletextCellWidth) & VideoConstants.CrtcMemoryMask);
                pixels[_crtc.Scanline * VideoConstants.Width + x] = colours[_teletext.Rgb ^ (cursor ? 7 : 0)];
            }
        }
    }
}
