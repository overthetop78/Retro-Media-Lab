using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Constants.VideoConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services;

internal sealed partial class Video
{
    private byte _teletextLatchedData = C.TeletextSpace;
    private bool _teletextWindow;
    private int _teletextRasterParity = -1;

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
        _teletextWindow = false;
        _teletextRasterParity = -1;
        ConfigureTeletextPins();
    }

    private byte PrepareTeletext(bool fetch)
    {
        var window = _crtcVSync;
        if (_teletextWindow != window)
        {
            _teletextWindow = window;
            _teletext.SetDataEntryWindow(window);
        }
        var parity = _crtcRaster & C.CpuPhaseMask;
        if (_teletextRasterParity != parity)
        {
            _teletextRasterParity = parity;
            _teletext.CharacterRoundingSelect = parity == 0;
        }
        if (_phase == 0)
        {
            _teletext.Data = _teletextLatchedData;
            _teletext.SetCharacterClock(false);
            _teletext.SetLoadOutputShiftEnable(_crtcDisplayEnabled);
            // Le créneau RAM produit un seul octet sur le bus vidéo. IC15 et
            // VIDPROC voient cette même donnée ; F1 a capturé le latch précédent.
            var data = ReadVideoMemory(_crtcMemoryAddress, _crtcRaster);
            // IC15 n'est chargé comme caractère télétexte que lorsque MA13 sélectionne cette voie.
            _teletextLatchedData = (_crtcMemoryAddress & C.MemoryTeletextSelect) != 0
                ? data : (byte)0;
            return data;
        }
        _teletext.SetCharacterClock(true);
        return fetch ? ReadVideoMemory(_crtcMemoryAddress, _crtcRaster) : (byte)0;
    }

}
