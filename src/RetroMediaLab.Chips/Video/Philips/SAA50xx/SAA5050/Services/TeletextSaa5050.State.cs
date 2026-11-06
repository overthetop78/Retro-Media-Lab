using RetroMediaLab.Chips.Video.Philips.SAA50xx.SAA5050.Contracts;
using C = RetroMediaLab.Chips.Video.Philips.SAA50xx.SAA5050.Constants.Saa5050Constants;

namespace RetroMediaLab.Chips.Video.Philips.SAA50xx.SAA5050.Services;

public sealed partial class TeletextSaa5050
{
    /// <summary>Conserve aussi les événements graphiques dont le délai de propagation n'est pas écoulé.</summary>
    public TeletextSaa5050State CaptureState() => new()
    {
        FormatVersion = C.StateFormatVersion,
        CharacterRom = (byte[])_characters.Clone(),
        Pipeline = Array.ConvertAll(_pipeline, item => new TeletextCharacterState(item.Due, item.Data, item.Raster, item.Frame, item.Row)),
        WindowPipeline = Array.ConvertAll(_windowPipeline, item => new TeletextWindowState(item.Due, item.Level)),
        SerialOn = new(_serialOn.Rgb, _serialOn.Monochrome, _serialOn.Blanking),
        SerialOff = new(_serialOff.Rgb, _serialOff.Monochrome, _serialOff.Blanking),
        Foreground = _foreground,
        Background = _background,
        Held = _held,
        Row = _row,
        Graphics = _graphics,
        Flash = _flash,
        Conceal = _conceal,
        Separated = _separated,
        Hold = _hold,
        Double = _double,
        Bottom = _bottom,
        RowDouble = _rowDouble,
        HeldSeparated = _heldSeparated,
        Boxed = _boxed,
        PreviousCode = _previousCode,
        PictureOn = _pictureOn,
        DisplayEnabled = _displayEnabled,
        SuperimposeInputHigh = _superimposeInputHigh,
        PipelineHead = _pipelineHead,
        PipelineCount = _pipelineCount,
        WindowHead = _windowHead,
        WindowCount = _windowCount,
        HalfDotTime = _halfDotTime,
        OutputWindow = _outputWindow,
        SerialMask = _serialMask,
        SerialReversedMask = _serialReversedMask,
        SerialForeground = _serialForeground,
        SerialBackground = _serialBackground,
        SerialPixel = _serialPixel,
        SerialRow = _serialRow,
        SerialBox = _serialBox,
        Rgb = _rgb,
        Monochrome = _monochrome,
        Blanking = _blanking,
        TextMode = _textMode,
        Superimpose = _superimpose,
        Reveal = _reveal,
        RevealClocks = _revealClocks,
        Data = _data,
        F1 = _f1,
        Tr6 = _tr6,
        Lose = _lose,
        Dew = _dew,
        RemoteClock = _remoteClock,
        GeneralLineResetHigh = _generalLineResetHigh,
        RemoteHalf = _remoteHalf,
        RemoteBit = _remoteBit,
        RemoteCommand = _remoteCommand,
        Scanline = _scanline,
        CharacterRow = _characterRow,
        FlashField = _flashField,
        CharacterRoundingSelect = _characterRoundingSelect,
        BigCharacterSelectHigh = _bigCharacterSelectHigh,
        RemoteData = _remoteData,
    };

    /// <summary>Restaure sans avancer F1/TR6 et sans redécoder les caractères en cours.</summary>
    public void RestoreState(TeletextSaa5050State state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.FormatVersion != C.StateFormatVersion || state.CharacterRom is null
            || !_characters.AsSpan().SequenceEqual(state.CharacterRom)
            || state.Pipeline?.Length != C.PipelineCapacity || state.WindowPipeline?.Length != C.PipelineCapacity
            || (uint)state.PipelineHead >= C.PipelineCapacity || (uint)state.WindowHead >= C.PipelineCapacity
            || (uint)state.PipelineCount > C.PipelineCapacity || (uint)state.WindowCount > C.PipelineCapacity)
            throw new ArgumentException(nameof(state));
        for (var index = 0; index < C.PipelineCapacity; index++)
        {
            var character = state.Pipeline[index];
            _pipeline[index] = (character.Due, character.Data, character.Raster, character.Frame, character.Row);
            var window = state.WindowPipeline[index];
            _windowPipeline[index] = (window.Due, window.Level);
        }
        _serialOn = (state.SerialOn.Rgb, state.SerialOn.Monochrome, state.SerialOn.Blanking);
        _serialOff = (state.SerialOff.Rgb, state.SerialOff.Monochrome, state.SerialOff.Blanking);
        _foreground = state.Foreground;
        _background = state.Background;
        _held = state.Held;
        _row = state.Row;
        _graphics = state.Graphics;
        _flash = state.Flash;
        _conceal = state.Conceal;
        _separated = state.Separated;
        _hold = state.Hold;
        _double = state.Double;
        _bottom = state.Bottom;
        _rowDouble = state.RowDouble;
        _heldSeparated = state.HeldSeparated;
        _boxed = state.Boxed;
        _previousCode = state.PreviousCode;
        _pictureOn = state.PictureOn;
        _displayEnabled = state.DisplayEnabled;
        _superimposeInputHigh = state.SuperimposeInputHigh;
        _pipelineHead = state.PipelineHead;
        _pipelineCount = state.PipelineCount;
        _windowHead = state.WindowHead;
        _windowCount = state.WindowCount;
        _halfDotTime = state.HalfDotTime;
        _outputWindow = state.OutputWindow;
        _serialMask = state.SerialMask;
        _serialReversedMask = state.SerialReversedMask;
        _serialForeground = state.SerialForeground;
        _serialBackground = state.SerialBackground;
        _serialPixel = state.SerialPixel;
        _serialRow = state.SerialRow;
        _serialBox = state.SerialBox;
        _rgb = state.Rgb;
        _monochrome = state.Monochrome;
        _blanking = state.Blanking;
        _textMode = state.TextMode;
        _superimpose = state.Superimpose;
        _reveal = state.Reveal;
        _revealClocks = state.RevealClocks;
        _data = state.Data;
        _f1 = state.F1;
        _tr6 = state.Tr6;
        _lose = state.Lose;
        _dew = state.Dew;
        _remoteClock = state.RemoteClock;
        _generalLineResetHigh = state.GeneralLineResetHigh;
        _remoteHalf = state.RemoteHalf;
        _remoteBit = state.RemoteBit;
        _remoteCommand = state.RemoteCommand;
        _scanline = state.Scanline;
        _characterRow = state.CharacterRow;
        _flashField = state.FlashField;
        _characterRoundingSelect = state.CharacterRoundingSelect;
        _bigCharacterSelectHigh = state.BigCharacterSelectHigh;
        _remoteData = state.RemoteData;
    }
}