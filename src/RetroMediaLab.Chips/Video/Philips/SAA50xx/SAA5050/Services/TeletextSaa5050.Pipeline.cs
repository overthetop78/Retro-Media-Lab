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
    private int _serialForeground;
    private int _serialBackground;
    private int _serialPixel = C.HalfDotsPerCharacter;
    private int _serialRow;
    private bool _serialBox;
    private (int Rgb, bool Monochrome, bool Blanking) _serialOn;
    private (int Rgb, bool Monochrome, bool Blanking) _serialOff;

    public int Rgb { get; private set; }
    public bool Red => (Rgb & C.RedBit) != 0;
    public bool Green => (Rgb & C.GreenBit) != 0;
    public bool Blue => (Rgb & C.BlueBit) != 0;
    public bool Monochrome { get; private set; }
    public bool Blanking { get; private set; }

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
        _serialOn = ResolveVideo(true, _serialForeground, _serialBackground, _outputWindow, _serialBox, _serialRow);
        _serialOff = ResolveVideo(false, _serialForeground, _serialBackground, _outputWindow, _serialBox, _serialRow);
        RenderSerialOutputs();
    }

    private void RenderSerialOutputs()
    {
        var valid = _outputWindow && _serialPixel < C.HalfDotsPerCharacter;
        var dot = valid && (_serialMask & (1 << (C.HalfDotsPerCharacter - 1 - _serialPixel))) != 0;
        var output = dot ? _serialOn : _serialOff;
        Rgb = output.Rgb;
        Monochrome = output.Monochrome;
        Blanking = output.Blanking;
    }

    private void ResetPipeline()
    {
        Array.Clear(_pipeline);
        Array.Clear(_windowPipeline);
        _pipelineHead = _pipelineCount = _windowHead = _windowCount = 0;
        _halfDotTime = 0;
        _outputWindow = false;
        _serialMask = _serialForeground = _serialBackground = _serialRow = 0;
        _serialPixel = C.HalfDotsPerCharacter;
        _serialBox = false;
        UpdateSerialOutputs();
    }
}
