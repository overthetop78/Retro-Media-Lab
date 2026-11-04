using C = RetroMediaLab.Chips.Video.Acorn.BbcMicro.VideoULA.Constants.VideoConstants;

namespace RetroMediaLab.Chips.Video.Acorn.BbcMicro.VideoULA.Services;

public sealed partial class Video
{
    private byte _teletextLatchedData = C.TeletextSpace;
    private bool _teletextDotLevel;

    private void ConfigureTeletextPins()
    {
        // IC5 : DE/BCS/DATA/DLIM au +5 V ; PO/SI/GLR à la masse.
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
        _teletextLatchedData = C.TeletextSpace;
        _teletextDotLevel = false;
        ConfigureTeletextPins();
    }

    private void PrepareTeletext()
    {
        _teletext.SetDataEntryWindow(_crtc.VSync);
        _teletext.CharacterRoundingSelect = (_crtc.RasterAddress & C.CpuPhaseMask) == 0;
        if (_phase == 0)
        {
            _teletext.Data = _teletextLatchedData;
            _teletext.SetCharacterClock(false);
            _teletext.SetLoadOutputShiftEnable(_crtc.DisplayEnabled);
            // IC15 n'est chargé comme caractère télétexte que lorsque MA13 sélectionne cette voie.
            _teletextLatchedData = (_crtc.MemoryAddress & C.MemoryTeletextSelect) != 0
                ? ReadVideoMemory(_crtc.MemoryAddress, _crtc.RasterAddress) : (byte)0;
        }
        else _teletext.SetCharacterClock(true);
    }

}
