using C = RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Constants.ElectronVideoConstants;

namespace RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Services;

/// <summary>Prélèvements RAM et pixels de l’ULA Electron, à son horloge de 2 MHz.</summary>
public sealed class ElectronVideo(Func<int, byte> read)
:RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Interfaces.IElectronVideo{
    private readonly byte[] _registers = new byte[C.PaletteRegisters];
    private readonly int[] _palette = new int[C.PaletteEntries];
    private byte[] _drawing = new byte[C.FrameBytes];
    private byte[] _completed = new byte[C.FrameBytes];
    private int _clock;
    private int _line;
    private int _raster;
    private int _start = C.DefaultStart;
    private int _address = C.DefaultStart;
    private byte _data;

    public int Mode { get; private set; }
    public int Line => _line;
    public int Clock => _clock;
    public long Frame { get; private set; }
    public byte[] CompletedFrame => _completed;
    public bool LowBandwidth => (Mode & C.LowBandwidthMask) != 0;
    public bool DisplayFetching => _line < C.ModeHeight[Mode] && _raster < C.ActiveRasters && _clock < C.ActiveCycles;
    public Action<int>? LineStarted { get; set; }

    public void Reset()
    {
        _clock = _line = _raster = 0;
        _start = _address = C.DefaultStart;
        Mode = 0;
        Frame = 0;
        _data = 0;
        Array.Clear(_registers);
        Array.Clear(_palette);
        Array.Clear(_drawing);
        Array.Clear(_completed);
    }

    public void SetMode(byte control) => Mode = (control >> C.ModeShift) & C.ModeMask;

    public void WriteStart(bool high, byte value) => _start = high
        ? (_start & C.StartRetainedLowMask) | ((value & C.StartHighMask) << C.StartHighShift)
        : (_start & C.StartRetainedHighMask) | ((value & C.StartLowMask) << C.StartLowShift);

    public void WritePalette(int register, byte value)
    {
        _registers[register] = (byte)(value ^ C.ByteMask);
        var pair = register & C.PalettePairMask;
        var even = _registers[pair];
        var odd = _registers[pair + 1];
        var first = C.PaletteBases[pair >> C.PalettePairShift];
        for (var entry = 0; entry < 4; entry++)
        {
            var red = (odd >> entry) & 1;
            var green = entry < 2 ? (odd >> (entry + 4)) & 1 : (even >> entry) & 1;
            var blue = (even >> (entry + 4)) & 1;
            var logical = first + (entry & 1) * 2 + (entry >> 1) * 8;
            _palette[logical] = red * C.ByteMask | (green * C.ByteMask << C.GreenShift) | (blue * C.ByteMask << C.BlueShift);
        }
    }

    public bool Tick()
    {
        if (_line < C.Height && _clock < C.ActiveCycles)
        {
            var low = LowBandwidth;
            if (DisplayFetching && (!low || (_clock & 1) == 0))
                _data = read(Wrap(_address + (_clock / (low ? 2 : 1)) * C.ActiveRasters));
            var bits = C.ModeBits[Mode];
            var width = bits * (low ? 2 : 1);
            var byteWidth = C.ByteBits * (low ? 2 : 1);
            var x = _clock * C.PixelsPerCycle;
            for (var pixel = 0; pixel < C.PixelsPerCycle; pixel++)
            {
                var shifted = _data << ((x + pixel) % byteWidth / width);
                var pen = ((shifted >> 7) & 1) << 3 | ((shifted >> 5) & 1) << 2
                    | ((shifted >> 3) & 1) << 1 | ((shifted >> 1) & 1);
                pen &= bits == C.OneBitPixel ? C.TwoColorMask : bits == C.TwoBitPixel ? C.FourColorMask : C.SixteenColorMask;
                WritePixel(x + pixel, _line, DisplayFetching ? _palette[pen] : 0);
            }
        }
        if (++_clock < C.CyclesPerLine) return false;
        _clock = 0;
        if (_raster < C.ActiveRasters) _address++;
        if (++_raster >= C.ModeRasters[Mode])
        {
            _raster = 0;
            var columns = LowBandwidth ? C.LowBandwidthColumns : C.HighBandwidthColumns;
            _address += (columns - 1) * C.ActiveRasters;
        }
        _address = Wrap(_address);
        if (++_line < C.LinesPerFrame)
        {
            LineStarted?.Invoke(_line);
            return false;
        }
        _line = _raster = 0;
        _address = _start;
        Frame++;
        (_drawing, _completed) = (_completed, _drawing);
        LineStarted?.Invoke(_line);
        return true;
    }

    private int Wrap(int address) => address >= C.VideoWrapAddress ? address - C.ModeBytes[Mode] : address;

    private void WritePixel(int x, int y, int color)
    {
        var offset = (y * C.Width + x) * C.PixelBytes;
        _drawing[offset] = (byte)(color >> C.BlueShift);
        _drawing[offset + 1] = (byte)(color >> C.GreenShift);
        _drawing[offset + 2] = (byte)color;
    }
}
