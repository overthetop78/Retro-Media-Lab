using RetroMediaLab.Chips.Controllers.Acorn.Vdu80CharacterGenerator.Interfaces;
using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS165.Services;
using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS374.Services;
using RetroMediaLab.Chips.Memory.NationalSemiconductor.MM52116.Services;
using C = RetroMediaLab.Chips.Controllers.Acorn.Vdu80CharacterGenerator.Constants.Vdu80CharacterConstants;

namespace RetroMediaLab.Chips.Controllers.Acorn.Vdu80CharacterGenerator.Services;

/// <summary>400,019 character latch, mask ROM, load pulse and video serializer.</summary>
/// <remarks>External bus clocks; no independent oscillator or framebuffer. Propagation
/// uses the documented digital profile (NL typical, ROM maximums), not measured analog delays.</remarks>
public sealed partial class Vdu80CharacterGenerator : IVdu80CharacterGenerator
{
    private readonly Sn74Ls374 _latch = new(C.CharacterFrequency);
    private readonly Sn74Ls165 _serializer = new(C.DotFrequency);
    private readonly Mm52116 _rom;
    private readonly Vdu80CursorCircuit _cursorCircuit;
    private byte _characterData;
    private byte _rasterAddress;
    private byte? _latchedData;
    private bool _displayInput;
    private bool _delayedDisplay;
    private bool _displayKnown;
    private bool _displayHigh;
    private bool _serialKnown;
    private bool _serialHigh;
    private bool? _videoWhite;
    private bool _cursor;
    private bool _inverted;

    public Vdu80CharacterGenerator(ReadOnlySpan<byte> characters)
        : this(characters, C.CursorSwitchingFraction) { }

    public Vdu80CharacterGenerator(ReadOnlySpan<byte> characters, double cursorSwitchingFraction)
    {
        _cursorCircuit = new Vdu80CursorCircuit(C.CursorSeriesOhms, C.CursorShuntOhms,
            C.CursorCapacitancePicofarads, cursorSwitchingFraction, C.InitialCursorCharge);
        _cursorCircuit.OutputChanged = high =>
        {
            _cursor = high;
            PublishVideo();
        };
        _cursorCircuit.NextEventNanosecondsChanged = next =>
        {
            _cursorDeadline = next;
            UpdateDeadline();
        };
        _rom = new Mm52116(characters, C.RomSelects, C.RomAddressNanoseconds,
            C.RomSelectNanoseconds, C.RomDisableNanoseconds);
        _latch.OutputsChanged = LatchChanged;
        _rom.OutputsChanged = RomChanged;
        _rom.NextEventNanosecondsChanged = next =>
        {
            _romDeadline = next;
            UpdateDeadline();
        };
        _serializer.OutputsChanged = (high, _) =>
        {
            _serialKnown = high.HasValue;
            _serialHigh = high.GetValueOrDefault();
            PublishVideo();
        };
        _serializer.SetSerialInput(false);
        _serializer.SetParallelData(0, C.GroundedInputs);
        _rom.SetChipSelects(C.RomSelects);
        _romDeadline = _rom.NextEventNanoseconds;
        UpdateDeadline();
    }

    public long TimeNanoseconds => _timeNanoseconds;
    public long CharacterClockCount => _latch.InputClockCount;
    public long DotClockCount => _serializer.InputClockCount;
    public bool? VideoWhite => _videoWhite;
    public Action<bool?>? VideoChanged { get; set; }

    public void SetCharacterData(byte data) => _characterData = (byte)(data & 0x7f);

    public void SetRasterAddress(int address)
    {
        var raster = (byte)(address & 15);
        if (_rasterAddress == raster) return;
        _rasterAddress = raster;
        UpdateRomAddress();
    }

    public void SetDisplayEnabled(bool high)
    {
        if (_displayInput == high) return;
        _displayInput = high;
        // Functional one-character delay specified by the handbook's RC circuit.
        // A transition back to the retained level cancels the pending change.
        _displayDeadline = high == _delayedDisplay ? null
            : checked(_timeNanoseconds + C.DisplayDelayNanoseconds);
        UpdateDeadline();
    }

    public void SetCursor(bool high)
    {
        if (_cursorCircuit.InputHigh == high) return;
        _cursorCircuit.AdvanceTo(_timeNanoseconds);
        _cursorCircuit.SetInput(high);
    }

    public void SetInverted(bool high)
    {
        if (_inverted == high) return;
        _inverted = high;
        PublishVideo();
    }

    private void LatchChanged(byte? data, bool driven)
    {
        _latchedData = driven ? data : null;
        _displayKnown = _latchedData.HasValue;
        _displayHigh = (_latchedData.GetValueOrDefault() & 0x80) != 0;
        UpdateRomAddress();
        PublishVideo();
    }

    private void UpdateRomAddress()
    {
        if (_latchedData is not { } data) return;
        _rom.AdvanceTo(_timeNanoseconds); // A physical address transaction, not periodic polling.
        _rom.SetAddress((ushort)(((data & 0x7f) << 4) | _rasterAddress));
    }

    private void RomChanged(byte? data, bool driven) => _serializer.SetParallelData(
        data is { } value && driven ? (byte)((value >> 1) & C.GlyphInputs) : (byte)0,
        data.HasValue && driven ? byte.MaxValue : C.GroundedInputs);

    private void PublishVideo()
    {
        // Resolve the received levels once, including a known-low AND input masking
        // an unknown one. Do not repeatedly decode nullable inputs for each dot.
        var known = (_displayKnown && !_displayHigh) || (_serialKnown && !_serialHigh)
            || (_displayKnown && _serialKnown);
        bool? white = known ? (_displayHigh && _serialHigh) ^ _cursor ^ _inverted : null;
        if (_videoWhite == white) return;
        _videoWhite = white;
        VideoChanged?.Invoke(white);
    }
}
