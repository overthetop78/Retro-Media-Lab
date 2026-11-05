using C = RetroMediaLab.Chips.Video.Philips.SAA50xx.SAA5050.Constants.Saa5050Constants;

namespace RetroMediaLab.Chips.Video.Philips.SAA50xx.SAA5050.Services;

public sealed partial class TeletextSaa5050
{
    private readonly (ulong Due, byte Data, int Raster, int Frame, int Row)[]
        _pipeline = new (ulong, byte, int, int, int)[C.PipelineCapacity];
    private readonly (ulong Due, bool Level)[] _windowPipeline = new (ulong, bool)[C.PipelineCapacity];
    private int _pipelineHead;
    private int _pipelineCount;
    private int _windowHead;
    private int _windowCount;
    private ulong _halfDotTime;
    private bool _outputWindow;
    private int _serialMask;
    private int _serialReversedMask;
    private int _serialForeground;
    private int _serialBackground;
    private int _serialPixel = C.HalfDotsPerCharacter;
    private int _serialRow;
    private bool _serialBox;
    private (int Rgb, bool Monochrome, bool Blanking) _serialOn;
    private (int Rgb, bool Monochrome, bool Blanking) _serialOff;

    private int _rgb;
    private bool _monochrome;
    private bool _blanking;
    public int Rgb => _rgb;
    public bool Red => (_rgb & C.RedBit) != 0;
    public bool Green => (_rgb & C.GreenBit) != 0;
    public bool Blue => (_rgb & C.BlueBit) != 0;
    public bool Monochrome => _monochrome;
    public bool Blanking => _blanking;

    /// <summary>Avance entre les échéances du pipeline ; conserve chaque RGB et les sorties finales.</summary>
    public ulong ClockHalfDots(int count)
    {
        if ((uint)count > C.MaximumPackedHalfDots) throw new ArgumentOutOfRangeException(nameof(count));
        if (!_outputWindow && _windowCount == 0)
        {
            // Sans front LOSE en vol, toutes les entrées échues seraient ignorées.
            // Les entrées futures restent en file pour une éventuelle réouverture.
            _halfDotTime += (uint)count;
            if (_serialPixel < C.HalfDotsPerCharacter)
            {
                _serialPixel += count;
                if (_serialPixel > C.HalfDotsPerCharacter) _serialPixel = C.HalfDotsPerCharacter;
            }
            while (_pipelineCount > 0 && unchecked((long)(_pipeline[_pipelineHead].Due - _halfDotTime)) <= 0)
            {
                _pipelineHead = (_pipelineHead + 1) & C.PipelineIndexMask;
                _pipelineCount--;
            }
            if ((count & 1) != 0) _tr6 = !_tr6;
            // La fermeture et les changements de commandes publient déjà _serialOff.
            // Aucun état de sortie ne change dans cet intervalle sans front LOSE.
            return (C.RepeatedColour * (uint)_serialOff.Rgb) & ((1UL << (count * C.ColourBits)) - 1);
        }
        ulong samples = 0;
        var completed = 0;
        while (completed < count)
        {
            var run = count - completed;
            if (_windowCount > 0)
            {
                var remaining = unchecked((long)(_windowPipeline[_windowHead].Due - _halfDotTime));
                if (remaining <= run) run = remaining > 1 ? (int)remaining - 1 : 0;
            }
            if (_pipelineCount > 0)
            {
                var remaining = unchecked((long)(_pipeline[_pipelineHead].Due - _halfDotTime));
                if (remaining <= run) run = remaining > 1 ? (int)remaining - 1 : 0;
            }
            if (run == 0)
            {
                AdvanceHalfDot();
                samples |= (ulong)_rgb << (completed++ * C.ColourBits);
                continue;
            }
            _halfDotTime += (uint)run;
            if (_serialMask == 0 || _serialOn.Rgb == _serialOff.Rgb || !_outputWindow
                || _serialPixel >= C.HalfDotsPerCharacter)
            {
                // Le paquet expose les RGB intermédiaires. Même si les couleurs
                // coïncident, les broches monochrome/blanking finales sont résolues
                // après l'avancement exact du décalage et de tous les événements.
                samples |= ((C.RepeatedColour * (uint)_serialOff.Rgb)
                    & ((1UL << (run * C.ColourBits)) - 1)) << (completed * C.ColourBits);
                _serialPixel += run;
                if (_serialPixel > C.HalfDotsPerCharacter) _serialPixel = C.HalfDotsPerCharacter;
                completed += run;
                continue;
            }
            samples |= PackSerialColours(_serialReversedMask >> (_serialPixel + 1), run,
                _serialOn.Rgb, _serialOff.Rgb) << (completed * C.ColourBits);
            _serialPixel += run;
            if (_serialPixel > C.HalfDotsPerCharacter) _serialPixel = C.HalfDotsPerCharacter;
            completed += run;
        }
        if ((count & 1) != 0) _tr6 = !_tr6;
        if (count != 0) RenderSerialOutputs();
        return samples;
    }

    private void QueueInput(byte data, int raster, int frame, int row)
    {
        // Le débit légal (F1 = TR6/6) utilise au plus trois caractères en vol.
        // Les cadences qui dépassent cette capacité sortent des limites de la fiche.
        if (_pipelineCount == C.PipelineCapacity)
        {
            _pipelineHead = (_pipelineHead + 1) & C.PipelineIndexMask;
            _pipelineCount--;
        }
        var tail = (_pipelineHead + _pipelineCount) & C.PipelineIndexMask;
        _pipeline[tail] = (_halfDotTime + C.GraphicsDelayHalfClocks, data, raster, frame, row);
        _pipelineCount++;
    }

    private void QueueWindow(bool level)
    {
        if (_windowCount == C.PipelineCapacity)
        {
            _windowHead = (_windowHead + 1) & C.PipelineIndexMask;
            _windowCount--;
        }
        var tail = (_windowHead + _windowCount) & C.PipelineIndexMask;
        _windowPipeline[tail] = (_halfDotTime + C.GraphicsDelayHalfClocks, level);
        _windowCount++;
    }

    private void AdvanceHalfDot()
    {
        var gatesChanged = false;
        _halfDotTime++;
        if (_serialPixel < C.HalfDotsPerCharacter) _serialPixel++;
        while (_windowCount > 0 && IsDue(_windowPipeline[_windowHead].Due))
        {
            _outputWindow = _windowPipeline[_windowHead].Level;
            gatesChanged = true;
            _windowHead = (_windowHead + 1) & C.PipelineIndexMask;
            _windowCount--;
        }
        while (_pipelineCount > 0 && IsDue(_pipeline[_pipelineHead].Due))
        {
            var input = _pipeline[_pipelineHead];
            _pipelineHead = (_pipelineHead + 1) & C.PipelineIndexMask;
            _pipelineCount--;
            if (!_outputWindow) continue;
            var cell = Character(input.Data, input.Raster, input.Frame);
            _serialMask = cell.Mask;
            _serialReversedMask = ReverseSerialMask(cell.Mask);
            _serialForeground = cell.Foreground;
            _serialBackground = cell.Background;
            _serialBox = _boxed;
            _serialRow = input.Row;
            _serialPixel = 0;
            gatesChanged = true;
        }
        if (gatesChanged) UpdateSerialOutputs();
        else RenderSerialOutputs();
    }

    private bool IsDue(ulong deadline) => unchecked((long)(deadline - _halfDotTime)) <= 0;

    private void UpdateSerialOutputs()
    {
        // Les portes sont identiques pour les deux valeurs du point série.
        // Character a déjà appliqué tous les attributs et effets de contrôle.
        if (!_outputWindow)
            _serialOn = _serialOff = (C.Black, false, !_pictureOn);
        else if (_pictureOn && !_textMode && _superimposeInputHigh && _serialRow != 0)
            _serialOn = _serialOff = (C.Black, false, false);
        else
        {
            var blanking = !_pictureOn || (_displayEnabled && _serialBox);
            var enabled = _displayEnabled && (!_pictureOn || _superimpose || _serialBox);
            _serialOn = (enabled ? _serialForeground & C.ColourMask : C.Black, enabled, blanking);
            _serialOff = (enabled && blanking ? _serialBackground & C.ColourMask : C.Black, false, blanking);
        }
        RenderSerialOutputs();
    }


    private void RenderSerialOutputs()
    {
        var valid = _outputWindow && _serialPixel < C.HalfDotsPerCharacter;
        var dot = valid && (_serialMask & (1 << (C.HalfDotsPerCharacter - 1 - _serialPixel))) != 0;
        var output = dot ? _serialOn : _serialOff;
        _rgb = output.Rgb;
        _monochrome = output.Monochrome;
        _blanking = output.Blanking;
    }

    private void ResetPipeline()
    {
        Array.Clear(_pipeline);
        Array.Clear(_windowPipeline);
        _pipelineHead = _pipelineCount = _windowHead = _windowCount = 0;
        _halfDotTime = 0;
        _outputWindow = false;
        _serialMask = _serialReversedMask = _serialForeground = _serialBackground = _serialRow = 0;
        _serialPixel = C.HalfDotsPerCharacter;
        _serialBox = false;
        UpdateSerialOutputs();
    }
}
