using RetroMediaLab.Chips.Video.Motorola.M6845.Chip6845.Enums;
using RetroMediaLab.Chips.Video.Motorola.M6845.Chip6845.Services;
using RetroMediaLab.Chips.Video.Philips.SAA50xx.SAA5050.Services;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Constants.SystemVideoConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Services;

/// <summary>Cartes VDU System 40 colonnes SAA5050 et 80 colonnes à ROM de caractères.</summary>
/// <remarks>L’assemblage fournit l’horloge des caractères. Les adresses proviennent
/// du CRTC existant ; aucune position de curseur n’est inventée à partir du texte.</remarks>
internal sealed partial class SystemVideo
{
    private readonly Crtc6845 _crtc = new(Crtc6845Model.Hd6845S);
    private readonly bool _eightyColumns;
    private readonly byte[] _ram;
    private readonly TeletextSaa5050? _teletext;
    private readonly int _ramStart;
    private readonly int _crtcStart;
    private byte[] _drawing = new byte[C.FrameBytes];
    private byte[] _completed = new byte[C.FrameBytes];
    private long _characterClocks;
    private long _fieldStartClock;
    private bool _drawingInterlaced;
    private int _drawingParity;
    private EmulationVideoScanMode _scanMode;

    internal SystemVideo(bool eightyColumns, ReadOnlySpan<byte> characters, bool relocated = false, bool inverted = false)
    {
        _eightyColumns = eightyColumns;
        if (eightyColumns && characters.Length != C.CharacterRomBytes) throw new ArgumentException(nameof(characters));
        _ram = new byte[eightyColumns ? C.MonochromeRamBytes : C.TeletextRamBytes];
        _ramStart = eightyColumns ? relocated ? C.RelocatedMonochromeRamStart : C.MonochromeRamStart : C.TeletextRamStart;
        _crtcStart = eightyColumns ? relocated ? C.RelocatedMonochromeCrtcStart : C.MonochromeCrtcStart : C.TeletextCrtcStart;
        if (!eightyColumns)
        {
            _teletext = new TeletextSaa5050(characters);
            _crtc.VSyncChanged = _teletext.SetDataEntryWindow;
            _crtc.HSyncChanged = level => _teletext.SetGeneralLineReset(!level);
            _crtc.RasterAddressChanged = SetTeletextRasterAddress;
            ResetTeletextWiring();
        }
        else ConfigureMonochrome(characters, inverted);
    }

    internal long Frame => _crtc.Frame;
    internal bool IsProgrammed => _crtc.IsProgrammed;
    internal int ClocksToLineBoundary => _crtc.ClocksToLineBoundary;
    internal long CompletedFieldClocks { get; private set; }
    internal EmulationVideoScanMode ScanMode => _scanMode;
    internal byte[] CompletedFrame => _completed;

    internal bool Handles(int address) => InRam(address) || CrtcPort(address) >= 0;

    internal byte Read(int address)
    {
        if (InRam(address)) return _ram[address - _ramStart];
        return CrtcPort(address) switch
        {
            0 => _crtc.VSync ? C.VerticalBlankStatus : (byte)0,
            1 => _crtc.Read(),
            _ => byte.MaxValue
        };
    }

    internal void Write(int address, byte value)
    {
        if (InRam(address))
        {
            var index = address - _ramStart;
            _ram[index] = value;
            if (_monochrome is not null && index == _monochromeRamIndex)
                _monochrome.SetCharacterData(value);
        }
        else if (CrtcPort(address) == 0) _crtc.Select(value);
        else if (CrtcPort(address) == 1) _crtc.Write(value);
    }

    internal void Reset()
    {
        _crtc.Reset();
        _teletext?.Reset();
        ResetTeletextWiring();
        _characterClocks = _fieldStartClock = CompletedFieldClocks = 0;
        _drawingInterlaced = false;
        _drawingParity = 0;
        _scanMode = EmulationVideoScanMode.Progressive;
        Array.Clear(_drawing);
        Array.Clear(_completed);
    }

    internal bool TickCharacter()
    {
        _characterClocks++;
        if (!_eightyColumns) ClockTeletext();
        var frameStarted = _crtc.Tick();
        if (_crtc.Column == 0) _teletextLineHalfDot = 0;
        if (!frameStarted) return false;
        (_drawing, _completed) = (_completed, _drawing);
        CompletedFieldClocks = _characterClocks - _fieldStartClock;
        _fieldStartClock = _characterClocks;
        _scanMode = !_drawingInterlaced ? EmulationVideoScanMode.Progressive
            : _drawingParity == 0 ? EmulationVideoScanMode.InterlacedEvenField : EmulationVideoScanMode.InterlacedOddField;
        _drawingInterlaced = _crtc.InterlaceVideo;
        _drawingParity = _crtc.RasterAddress & 1;
        Array.Clear(_drawing);
        return true;
    }

    private bool InRam(int address) => address >= _ramStart && address < _ramStart + _ram.Length;

    private int CrtcPort(int address)
    {
        var decoded = address & ~(_eightyColumns ? C.MonochromeCrtcMirror : C.TeletextCrtcMirror);
        return decoded == _crtcStart ? 0 : decoded == _crtcStart + 1 ? 1 : -1;
    }

}
