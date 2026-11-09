using RetroMediaLab.Chips.Controllers.Acorn.Vdu80CharacterGenerator.Services;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Constants.SystemVideoConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Services;

internal sealed partial class SystemVideo
{
    private Vdu80CharacterGenerator? _monochrome;
    private int _monochromeRamIndex;
    // Presentation coordinates only. The real clocks, latches and shift count live in Chips.
    private int _monochromeX;
    private int _monochromeY;

    private void ConfigureMonochrome(ReadOnlySpan<byte> characters, bool inverted)
    {
        var circuit = _monochrome = new Vdu80CharacterGenerator(characters);
        _crtc.MemoryAddressChanged = SetMonochromeAddress;
        _crtc.RasterAddressChanged = circuit.SetRasterAddress;
        _crtc.DisplayEnabledChanged = circuit.SetDisplayEnabled;
        _crtc.CursorChanged = circuit.SetCursor;
        SetMonochromeAddress(_crtc.MemoryAddress);
        circuit.SetRasterAddress(_crtc.RasterAddress);
        circuit.SetDisplayEnabled(_crtc.DisplayEnabled);
        circuit.SetCursor(_crtc.Cursor);
        circuit.SetInverted(inverted);
    }

    private void SetMonochromeAddress(int address)
    {
        _monochromeRamIndex = address & (_ram.Length - 1);
        _monochrome!.SetCharacterData(_ram[_monochromeRamIndex]);
    }

    internal void SetCharacterClock(bool high)
    {
        if (_monochrome is null) return;
        if (high)
        {
            // Crop the native active area after the one-character ROM pipeline.
            // Do not undo CRTC skew: its physical MA/DE/CU already reach the circuit.
            _monochromeX = (_crtc.Column - 1) * C.MonochromeCellPixels;
            _monochromeY = _crtc.Scanline;
        }
        _monochrome.SetCharacterClock(high);
    }

    internal void SetDotClock(bool high)
    {
        var sample = _monochrome!.SetDotClock(high);
        if (!high) return;
        var x = _monochromeX++;
        if ((uint)x >= C.Width || (uint)_monochromeY >= C.Height) return;
        var offset = (_monochromeY * C.Width + x) * C.PixelBytes;
        // Unknown physical levels remain unknown in the circuit. The presenter
        // uses black for an unresolved sample; no known level is fed back.
        var level = sample == true ? (byte)C.PixelLevel : (byte)0;
        _drawing[offset] = _drawing[offset + 1] = _drawing[offset + 2] = level;
    }
}
