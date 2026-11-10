using RetroMediaLab.Chips.Controllers.Acorn.Vdu80CharacterGenerator.Services;
using RetroMediaLab.Chips.Controllers.Acorn.SystemClock.Interfaces;
using System.Runtime.InteropServices;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Constants.SystemVideoConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Services;

internal sealed partial class SystemVideo : IIndependentClockPeriodReceiver
{
    private Vdu80CharacterGenerator? _monochrome;
    private int _monochromeRamIndex;
    // Presentation coordinates only. The real clocks, latches and shift count live in Chips.
    private int _monochromeX;
    private int _monochromeY;
    internal bool GroupSettledCharacters { get; set; } = true;
    internal long RepresentedSettledCharacters { get; private set; }

    /// <summary>The CPU has consumed only private-memory cycles. No VDU input
    /// changed during this prefix; replay its clocks independently, preserving
    /// dot-before-character ordering at coincident source edges.</summary>
    internal void AdvanceIndependentCharacterPeriods(int periods, bool dotHigh)
    {
        var dots = (IIndependentClockPeriodReceiver)this;
        for (var period = 0; period < periods; period++)
        {
            if (GroupSettledCharacters && _monochrome!.TryAdvanceSettledCharacterPeriods(1, out var white))
            {
                DrawConstantMonochromeDots(C.MonochromeCellPixels / 2, white);
                PositionMonochromeCharacterPixels();
                DrawConstantMonochromeDots(C.MonochromeCellPixels / 2, white);
                TickCharacter();
                RepresentedSettledCharacters++;
                continue;
            }
            dots.AdvancePeriods(C.MonochromeCellPixels / 2, dotHigh);
            SetCharacterClock(true);
            dots.AdvancePeriods(C.MonochromeCellPixels / 2, dotHigh);
            SetCharacterClock(false);
            TickCharacter();
        }
    }

    internal bool StableClockCandidate => _monochrome?.StableClockCandidate == true;
    internal Action<bool>? StableClockCandidateChanged
    {
        get => _monochrome?.StableClockCandidateChanged;
        set { if (_monochrome is { } circuit) circuit.StableClockCandidateChanged = value; }
    }

    void IStableClockPeriodReceiver.SetClock(bool high) => SetDotClock(high);

    int IStableClockPeriodReceiver.AdvanceStablePeriods(int maximumPeriods)
        => AdvanceMonochromePeriods(maximumPeriods, false);

    int IStableClockPeriodReceiver.AdvanceStablePeriods(int maximumPeriods, bool startHigh)
        => AdvanceMonochromePeriods(maximumPeriods, startHigh);

    void IIndependentClockPeriodReceiver.AdvancePeriods(int periods, bool startHigh)
    {
        // Neither dot output nor presentation changes CPU/disk/source inputs.
        // The source stops at their next possible character-clock interaction.
        // Preserve every varying dot; only already-proven stable portions may
        // use arithmetic advancement and a presentation span fill.
        while (periods > 0)
        {
            if (StableClockCandidate)
            {
                var stable = AdvanceMonochromePeriods(periods, startHigh);
                if (stable != 0) { periods -= stable; continue; }
            }
            SetDotClock(!startHigh);
            SetDotClock(startHigh);
            periods--;
        }
    }

    private int AdvanceMonochromePeriods(int maximumPeriods, bool startHigh)
    {
        if (_monochrome is null) return 0;
        var white = _monochrome.VideoWhite == true;
        var count = startHigh ? _monochrome.AdvanceStableDotClockPeriods(maximumPeriods)
            : _monochrome.AdvanceStableDotClocks(maximumPeriods);
        if (count == 0) return 0;
        DrawConstantMonochromeDots(count, white);
        return count;
    }

    private void DrawConstantMonochromeDots(int count, bool white)
    {
        var first = _monochromeX;
        _monochromeX += count;
        if ((uint)_monochromeY >= C.Height) return;
        var start = Math.Max(0, first);
        var end = Math.Min(C.Width, _monochromeX);
        if (end <= start) return;
        // Monochrome frames are cleared to zero, including alpha. Fill the same
        // native RGB bytes as explicit dots, preserving the zero alpha channel.
        var pixel = white ? (uint)C.PixelLevel * 0x010101u : 0u;
        if (!BitConverter.IsLittleEndian) pixel <<= 8;
        var bytes = _drawing.AsSpan((_monochromeY * C.Width + start) * C.PixelBytes,
            (end - start) * C.PixelBytes);
        MemoryMarshal.Cast<byte, uint>(bytes).Fill(pixel);
    }

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
            PositionMonochromeCharacterPixels();
        }
        _monochrome.SetCharacterClock(high);
    }

    private void PositionMonochromeCharacterPixels()
    {
        _monochromeX = (_crtc.Column - 1) * C.MonochromeCellPixels;
        _monochromeY = _crtc.Scanline;
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
