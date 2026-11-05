using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Constants.VideoConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services;

internal sealed partial class Video
{
    private uint[] _previousField;
    private bool _drawingInterlaced;
    private bool _completedInterlaced;
    private bool _previousFieldValid;
    private int _drawingParity;
    private int _completedParity;

    public int OutputHeight => _completedInterlaced ? Height * C.FieldsPerInterlacedImage : Height;

    private void ResetFields()
    {
        Array.Clear(_previousField);
        _drawingInterlaced = _completedInterlaced = _previousFieldValid = false;
        _drawingParity = _completedParity = 0;
    }

    private void CompleteField()
    {
        // Le CRTC vient de commencer le champ suivant. Conserver séparément la
        // parité du champ terminé : RA0 courant appartient déjà au suivant.
        _previousFieldValid = _drawingInterlaced && _completedInterlaced
            && _drawingParity != _completedParity;
        (_previousField, _completed, _drawing) = (_completed, _drawing, _previousField);
        _completedInterlaced = _drawingInterlaced;
        _completedParity = _drawingParity;
        _drawingInterlaced = _crtc.InterlaceVideo;
        _drawingParity = _crtc.RasterAddress & C.FieldParityMask;
        Array.Clear(_drawing);
    }
}
