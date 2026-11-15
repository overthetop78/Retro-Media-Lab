namespace RetroMediaLab.Chips.Video.Sega.VDP32X;

/// <summary>32X framebuffer VDP; owns both banks and palette, independent of the Mega Drive renderer.</summary>
public sealed partial class Vdp32X
{
    private readonly ushort[] _dram = new ushort[0x20000], _palette = new ushort[256];
    private ushort _mode, _shift, _length, _address, _fillData;
    private int _requestedBank, _activeBank, _fillCycles;
    public bool Pal { get; set; }
    public bool HBlank { get; private set; }
    public bool VBlank { get; private set; } = true;
    public bool Sh2Access { get; set; }
    public int Mode => _mode & 3;
    public int Height => (_mode & 0x40) != 0 && Pal ? 240 : 224;
    public bool Busy => _fillCycles > 0;
    public void Reset()
    {
        Array.Clear(_dram); Array.Clear(_palette); _mode = _shift = _length = _address = _fillData = 0;
        _requestedBank = _activeBank = _fillCycles = 0; HBlank = false; VBlank = true; Sh2Access = false;
    }
    public void Advance(int clocks) => _fillCycles = Math.Max(0, _fillCycles - clocks);
    public void SetBlank(bool horizontal, bool vertical)
    {
        HBlank = horizontal; VBlank = vertical;
        if (vertical || Mode == 0) _activeBank = _requestedBank;
    }
    public ushort ReadRegister(int offset) => (offset & 14) switch
    {
        0 => (ushort)(_mode | (Pal ? 0 : 0x8000)), 2 => _shift, 4 => _length, 6 => _address, 8 => _fillData,
        10 => (ushort)(_activeBank | (Busy ? 2 : 0) | ((HBlank || VBlank || Mode == 0) ? 0x2000 : 0) | (HBlank ? 0x4000 : 0) | (VBlank ? 0x8000 : 0)),
        _ => 0
    };
    public void WriteRegister(int offset, ushort value)
    {
        switch (offset & 14)
        {
            case 0: _mode = (ushort)(value & 0xc3); if (Mode == 0) _activeBank = _requestedBank; break;
            case 2: _shift = (ushort)(value & 1); break;
            case 4: _length = (ushort)(value & 255); break;
            case 6: _address = value; break;
            case 8:
                _fillData = value;
                if (Busy) break;
                for (int i = 0; i <= _length; i++) { _dram[_activeBank * 0x10000 + _address] = value; _address = (ushort)((_address & 0xff00) | ((_address + 1) & 255)); }
                _fillCycles = 7 + 3 * (_length + 1); break;
            case 10: _requestedBank = value & 1; if (VBlank || Mode == 0) _activeBank = _requestedBank; break;
        }
    }
    public ushort ReadPalette(uint address) => _palette[address >> 1 & 255];
    public void WritePalette(uint address, ushort value) => _palette[address >> 1 & 255] = value;
    public byte ReadFrame8(uint address) { ushort value = ReadFrame16(address); return (byte)(value >> ((1 - (int)(address & 1)) * 8)); }
    public ushort ReadFrame16(uint address) => _dram[_activeBank * 0x10000 + (address >> 1 & 65535)];
    public void WriteFrame8(uint address, byte value)
    {
        if (value == 0) return;
        int index = _activeBank * 0x10000 + (int)(address >> 1 & 65535), shift = (1 - (int)(address & 1)) * 8;
        _dram[index] = (ushort)((_dram[index] & ~(255 << shift)) | value << shift);
    }
    public void WriteFrame16(uint address, ushort value)
    {
        int index = _activeBank * 0x10000 + (int)(address >> 1 & 65535);
        if ((address & 0x20000) == 0) _dram[index] = value;
        else { ushort old = _dram[index]; if ((value & 0xff00) != 0) old = (ushort)((old & 255) | (value & 0xff00)); if ((value & 255) != 0) old = (ushort)((old & 0xff00) | (value & 255)); _dram[index] = old; }
    }
    public bool RenderLine(int line, Span<ushort> pixels)
    {
        if (Mode == 0 || line < 0 || line >= Height) return false;
        if (pixels.Length < 320) throw new ArgumentException(nameof(pixels));
        int bank = (1 - _activeBank) * 0x10000, address = _dram[bank + line];
        if (Mode == 1)
        {
            for (int x = 0; x < 320; x++) { int pixel = x + _shift; ushort word = _dram[bank + ((address + (pixel >> 1)) & 65535)]; pixels[x] = _palette[(word >> ((1 - (pixel & 1)) * 8)) & 255]; }
        }
        else if (Mode == 2) for (int x = 0; x < 320; x++) pixels[x] = _dram[bank + ((address + x) & 65535)];
        else
        {
            int x = 0; while (x < 320) { ushort word = _dram[bank + (address++ & 65535)]; int count = Math.Min((word >> 8) + 1, 320 - x); pixels.Slice(x, count).Fill(_palette[word & 255]); x += count; }
        }
        return true;
    }
    public bool OverridesMegaDrive(ushort pixel, bool backdrop) => backdrop || (((pixel >> 15) & 1) != ((_mode >> 7) & 1));
    public static uint ToXrgb8888(ushort color)
    {
        uint r = (uint)color & 31, g = (uint)color >> 5 & 31, b = (uint)color >> 10 & 31;
        return 0xff000000u | ((r << 3 | r >> 2) << 16) | ((g << 3 | g >> 2) << 8) | (b << 3 | b >> 2);
    }
}
