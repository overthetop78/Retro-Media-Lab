using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Vidc.Enums;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.Archimedes.Common.Enums;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Vidc.Constants.ArchimedesVidcConstants;
using Board = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.Archimedes.Common.Constants.ArchimedesBoardConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Vidc.Services;

/// <summary>VIDC1A : balayage programmable et FIFOs servis par le bus DMA de MEMC.</summary>
internal sealed partial class ArchimedesVidc
{
    private readonly uint[] _registers = new uint[C.RegisterCount];
    private readonly int[] _palette = new int[C.PaletteAndCursorEntries];
    private readonly int[] _extendedPalette = new int[C.ExtendedPaletteEntries];
    private readonly ArchimedesVidcFifo _videoFifo = new(C.VideoFifoWords);
    private readonly ArchimedesVidcFifo _cursorFifo = new(C.CursorFifoWords);
    private readonly ArchimedesVidcFifo _soundFifo = new(C.SoundFifoWords);
    private int _pixelClockPhase;
    private bool _horizontalProgrammed;
    private bool _verticalProgrammed;

    internal ArchimedesVidc() => Reset();

    internal uint Control { get; private set; }
    internal int BitsPerPixel { get; private set; }
    private int Depth { get; set; }
    private bool Interlaced { get; set; }
    internal bool VideoDmaRequest { get; private set; }
    internal bool CursorDmaRequest { get; private set; }
    internal bool SoundDmaRequest { get; private set; }
    internal bool Flyback { get; private set; }
    internal bool HighResolutionMonochrome { get; set; }
    internal Action<bool>? FlybackChanged { get; set; }
    internal Action? VideoFrameStarted { get; set; }
    internal Action? CursorFrameStarted { get; set; }
    internal long Frame { get; private set; }
    internal int Raster => _line;
    internal int HorizontalPixel => _x;
    internal int? FrameMasterClocks => _horizontalProgrammed && _verticalProgrammed
        ? (FieldLength * PixelDivisor * (Interlaced ? C.InterlaceFieldsPerFrame : 1)
            + C.MasterHalfClocks - 1) / C.MasterHalfClocks : null;
    internal double? FramesPerSecond => FrameMasterClocks is { } clocks
        ? (double)Board.DefaultVideoClockHz / clocks : null;

    private uint Register(VidcRegister register) => _registers[(int)register >> C.RegisterIndexShift];
    private int Timing(VidcRegister register) => (int)((Register(register) >> C.TimingShift) & C.TimingMask);
    private int HorizontalPeriod { get; set; }
    private int VerticalPeriod { get; set; }
    private int HorizontalDisplayStart { get; set; }
    private int HorizontalDisplayEnd { get; set; }
    private int HorizontalBorderStart { get; set; }
    private int HorizontalBorderEnd { get; set; }
    private int VerticalDisplayStart { get; set; }
    private int VerticalDisplayEnd { get; set; }
    private int VerticalBorderStart { get; set; }
    private int VerticalBorderEnd { get; set; }
    private int VerticalCursorStart { get; set; }
    private int VerticalCursorEnd { get; set; }
    private int CursorStart { get; set; }
    private int HorizontalSyncEnd { get; set; }
    private int FieldLength { get; set; }
    private int VideoWordsPerRaster { get; set; }
    private int DmaTrigger { get; set; }
    private int PixelDivisor { get; set; }
    private uint PixelMask { get; set; }
    private bool DisplayRaster => _line >= VerticalDisplayStart && _line < VerticalDisplayEnd;
    private bool CursorRaster => _line >= VerticalCursorStart && _line < VerticalCursorEnd;

    // Register decoding is independent of the current beam position. Refresh on
    // each timing write so changes during an active raster take effect immediately.
    private void RefreshTimings()
    {
        Control = Register(VidcRegister.Control);
        Depth = (int)((Control >> C.DepthShift) & C.DepthMask);
        BitsPerPixel = 1 << Depth;
        PixelMask = (1u << BitsPerPixel) - 1;
        Interlaced = (Control & C.InterlaceEnable) != 0;
        PixelDivisor = C.PixelHalfClocks[(int)(Control & C.PixelClockMask)];
        DmaTrigger = (int)((Control >> C.DmaRequestShift) & C.DmaRequestMask);
        HorizontalPeriod = (Timing(VidcRegister.HorizontalCycle) + 1) * C.HorizontalUnits;
        VerticalPeriod = Timing(VidcRegister.VerticalCycle) + 1;
        HorizontalSyncEnd = (Timing(VidcRegister.HorizontalSync) + 1) * C.HorizontalUnits;
        HorizontalDisplayStart = Timing(VidcRegister.HorizontalDisplayStart) * C.HorizontalUnits + C.DisplayOffsets[Depth];
        HorizontalDisplayEnd = Timing(VidcRegister.HorizontalDisplayEnd) * C.HorizontalUnits + C.DisplayOffsets[Depth];
        HorizontalBorderStart = Timing(VidcRegister.HorizontalBorderStart) * C.HorizontalUnits + 1;
        HorizontalBorderEnd = Timing(VidcRegister.HorizontalBorderEnd) * C.HorizontalUnits + 1;
        VerticalDisplayStart = Timing(VidcRegister.VerticalDisplayStart) + 1;
        VerticalDisplayEnd = Timing(VidcRegister.VerticalDisplayEnd) + 1;
        VerticalBorderStart = Timing(VidcRegister.VerticalBorderStart) + 1;
        VerticalBorderEnd = Timing(VidcRegister.VerticalBorderEnd) + 1;
        VerticalCursorStart = Timing(VidcRegister.VerticalCursorStart) + 1;
        VerticalCursorEnd = Timing(VidcRegister.VerticalCursorEnd) + 1;
        CursorStart = (int)((Register(VidcRegister.HorizontalCursor) >> C.CursorShift) & C.CursorMask) + C.CursorOffset;
        FieldLength = HorizontalPeriod * VerticalPeriod;
        if (Interlaced) FieldLength += Timing(VidcRegister.HorizontalInterlace) * C.HorizontalUnits;
        VideoWordsPerRaster = (Math.Max(0, HorizontalDisplayEnd - HorizontalDisplayStart) * BitsPerPixel + C.WordBits - 1) / C.WordBits;
    }

    internal void Reset()
    {
        Array.Clear(_registers);
        Array.Clear(_palette);
        Array.Clear(_extendedPalette);
        _videoFifo.Clear();
        _cursorFifo.Clear();
        _soundFifo.Clear();
        _horizontalProgrammed = _verticalProgrammed = false;
        _pixelClockPhase = 0;
        _x = _line = _fieldPixels = _fieldParity = 0;
        _videoBits = _cursorBits = _videoWordsFetched = _videoWordsTaken = 0;
        _videoWord = _cursorWord = 0;
        Frame = 0;
        Flyback = true;
        VideoDmaRequest = CursorDmaRequest = false;
        SoundDmaRequest = true;
        _drawingWidth = _completedWidth = C.DefaultWidth;
        _drawingHeight = _completedHeight = C.DefaultHeight;
        _drawing = new byte[_drawingWidth * _drawingHeight * C.PixelBytes];
        _completed = new byte[_drawing.Length];
        RefreshTimings();
        ResetSound();
    }

    internal void Write(uint value)
    {
        var register = (VidcRegister)((value >> C.RegisterAddressShift) & C.RegisterAddressMask);
        _registers[(int)register >> C.RegisterIndexShift] = value;
        if (register <= VidcRegister.CursorLast)
        {
            var index = (int)register >> C.RegisterIndexShift;
            _palette[index] = Rgb(value);
            if (index < C.PaletteEntries) RefreshExtendedPalette(index);
        }
        else if (register is >= VidcRegister.StereoFirst and <= VidcRegister.StereoLast)
        {
            var channel = (((int)register >> C.RegisterIndexShift) - 1) & C.StereoPositionMask;
            _stereo[channel] = (byte)(value & C.StereoPositionMask);
        }
        else if (register == VidcRegister.HorizontalCycle) _horizontalProgrammed = true;
        else if (register == VidcRegister.VerticalCycle) _verticalProgrammed = true;
        else if (register == VidcRegister.SoundFrequency) _soundRemaining = SoundPeriod;
        if (register is >= VidcRegister.HorizontalCycle and <= VidcRegister.VerticalCursorEnd or VidcRegister.Control)
            RefreshTimings();
    }

    private static int Rgb(uint value) =>
        ((int)(value & C.PaletteComponentMask) * C.PaletteScale << C.BlueOutputShift) |
        ((int)((value >> C.PaletteGreenShift) & C.PaletteComponentMask) * C.PaletteScale << C.GreenOutputShift) |
        (int)((value >> C.PaletteBlueShift) & C.PaletteComponentMask) * C.PaletteScale;

    private void RefreshExtendedPalette(int index)
    {
        var value = _registers[index];
        for (var pen = index; pen < C.ExtendedPaletteEntries; pen += C.PaletteEntries)
        {
            var red = (int)(value & C.ExtendedRedMask) | ((pen >> C.ExtendedRedSourceShift) & C.ExtendedHighComponent);
            var green = (int)((value >> C.PaletteGreenShift) & C.ExtendedGreenMask) | ((pen >> C.ExtendedGreenSourceShift) & C.ExtendedGreenHighMask);
            var blue = (int)((value >> C.PaletteBlueShift) & C.ExtendedBlueMask) | ((pen >> C.ExtendedBlueSourceShift) & C.ExtendedHighComponent);
            _extendedPalette[pen] = (red * C.PaletteScale << C.BlueOutputShift) |
                (green * C.PaletteScale << C.GreenOutputShift) | blue * C.PaletteScale;
        }
    }

    internal void SupplyVideoBurst(ReadOnlySpan<uint> words)
    {
        ValidateBurst(words);
        _videoFifo.Supply(words);
        _videoWordsFetched += C.DmaWords;
        VideoDmaRequest = false;
        RequestVideoData();
    }

    internal void SupplyCursorBurst(ReadOnlySpan<uint> words)
    {
        ValidateBurst(words);
        _cursorFifo.Supply(words);
        CursorDmaRequest = false;
    }

    internal void SupplySoundBurst(ReadOnlySpan<uint> words)
    {
        ValidateBurst(words);
        _soundFifo.Supply(words);
        SoundDmaRequest = false;
    }

    private static void ValidateBurst(ReadOnlySpan<uint> words)
    {
        if (words.Length != C.DmaWords) throw new ArgumentException(nameof(words));
    }

    internal void AdvanceMasterClocks(int clocks)
    {
        if (clocks <= 0) return;
        AdvanceSound(clocks);
        if (!_horizontalProgrammed || !_verticalProgrammed) return;
        _pixelClockPhase += clocks * C.MasterHalfClocks;
        var divisor = PixelDivisor;
        while (_pixelClockPhase >= divisor)
        {
            _pixelClockPhase -= divisor;
            PixelClock();
        }
    }
}
