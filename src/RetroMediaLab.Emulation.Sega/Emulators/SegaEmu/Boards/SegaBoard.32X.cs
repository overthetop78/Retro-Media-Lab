using System.Buffers.Binary;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Cartridge;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Constants;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.ThirtyTwoX.Hardware;
using RetroMediaLab.Chips.Video.Sega.VDP32X;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Boards;

internal abstract partial class SegaBoard
{
    private Mega32X? _mega32X;
    private readonly HashSet<int> _mapped32XPages = [];
    private readonly List<Action> _unmap32X = [];
    private byte[] _machineBitmap = null!;
    private int _machinePitch;
    public bool Has32X => _mega32X is not null;
    internal Mega32X? Adapter32X => _mega32X;
    private void Configure32X()
    {
        if (!config.Enable32X || system_hw is not (SystemHardware.MegaDrive or SystemHardware.MegaCd)) return;
        _mega32X ??= new(address => cart.romsize == 0 ? (byte)255 : cart.ReadByte(address % cart.romsize),
            address => cart.romsize < 2 ? (ushort)0xffff : BinaryPrimitives.ReadUInt16LittleEndian(cart.rom.AsSpan((int)((address % cart.romsize) & ~1u), 2)),
            () => unchecked((uint)main.cycles), _mainAudio, _loadFirmware, cart.romsize > 0 && (system_hw != SystemHardware.MegaCd || cd_boot != 0), vdp_pal != 0);
        _mega32X.SetCartridgeView(Create32XCartridgeView());
        Map32XBanks(); _vdp.LineRendered = Compose32XLine;
    }
    private ReadOnlyMemory<byte> Create32XCartridgeView()
    {
        // The MD keeps word-swapped ROM bytes; the SH-2 fetch mapper consumes
        // big-endian bytes. Convert once, preserving the existing 4 MiB mirrors.
        if (cart.romsize == 0 || (cart.romsize & 1) != 0 || system_hw == SystemHardware.MegaCd && cd_boot == 0)
            return ReadOnlyMemory<byte>.Empty;
        byte[] source = cart.rom, view = new byte[0x400000];
        int length = (int)Math.Min(cart.romsize, (uint)view.Length);
        for (int i = 0; i < length; i += 2)
        {
            view[i] = source[i + 1]; view[i + 1] = source[i];
        }
        for (int offset = length; offset < view.Length; offset += length)
            Array.Copy(view, 0, view, offset, Math.Min(length, view.Length - offset));
        return view;
    }
    private void Map32XBanks()
    {
        if (_mega32X is null) return;
        foreach (int page in new[] { 0, 0xa1 }.Concat(Enumerable.Range(0x84, 0x1c)))
        {
            if (!_mapped32XPages.Add(page)) continue;
            CartridgeMemoryBank bank = memory_map[page];
            bool mappedHandlers = bank.MappedReadsUseHandlers; bank.MappedReadsUseHandlers = true;
            var r8 = bank.read8; var r16 = bank.read16; var w8 = bank.write8; var w16 = bank.write16;
            uint Read8(uint a) => r8 is null ? bank.Data.Span[(int)((a & 65535) ^ 1)] : r8(a);
            uint Read16(uint a) => r16 is null ? BinaryPrimitives.ReadUInt16LittleEndian(bank.Data.Span[(int)(a & 65534)..]) : r16(a);
            void Write8(uint a, uint v) { if (w8 is not null) w8(a, v); else bank.Data.Span[(int)((a & 65535) ^ 1)] = (byte)v; }
            void Write16(uint a, uint v) { if (w16 is not null) w16(a, v); else BinaryPrimitives.WriteUInt16LittleEndian(bank.Data.Span[(int)(a & 65534)..], (ushort)v); }
            bool Handle(uint a) => _mega32X.Handles68k(a) && (a >= 0x100 || _mega32X.Enabled);
            bank.read8 = a => Handle(a) ? _mega32X.Read68k8(a) : Read8(a);
            bank.read16 = a => Handle(a) ? _mega32X.Read68k16(a) : Read16(a);
            bank.write8 = (a, v) => { if (Handle(a)) _mega32X.Write68k8(a, (byte)v); else Write8(a, v); };
            bank.write16 = (a, v) => { if (Handle(a)) _mega32X.Write68k16(a, (ushort)v); else Write16(a, v); };
            var oldZRead = zbank_memory_map[page].read; var oldZWrite = zbank_memory_map[page].write;
            zbank_memory_map[page].read = a => Handle(a) ? _mega32X.Read68k8(a) : oldZRead?.Invoke(a) ?? Read8(a);
            zbank_memory_map[page].write = (a, v) => { if (Handle(a)) _mega32X.Write68k8(a, (byte)v); else if (oldZWrite is not null) oldZWrite(a, v); else Write8(a, v); };
            _unmap32X.Add(() => { bank.MappedReadsUseHandlers = mappedHandlers; bank.read8 = r8; bank.read16 = r16; bank.write8 = w8; bank.write16 = w16; zbank_memory_map[page].read = oldZRead; zbank_memory_map[page].write = oldZWrite; });
        }
    }
    private void Unmap32XBanks()
    {
        foreach (var undo in _unmap32X) undo(); _unmap32X.Clear(); _mapped32XPages.Clear();
    }
    private void Compose32XLine(int line)
    {
        var adapter = _mega32X!; _vdp.ExternalOutputWidth = 0;
        Span<ushort> colors = stackalloc ushort[320];
        if (!adapter.Enabled || !adapter.Video.RenderLine(line, colors)) return;
        int rowIndex = _vdp.OutputLineIndex(line), depth = _vdp.EffectivePixelDepth, stride = depth == 32 ? 4 : depth == 8 ? 1 : 2;
        if (rowIndex < 0 || rowIndex * _machinePitch + _machinePitch > _machineBitmap.Length) return;
        int nativeWidth = _vdp.OutputWidth, outputWidth = config.Video.ntsc == 0 ? 320 + 2 * _vdp.ViewportX : nativeWidth;
        _vdp.ExternalOutputWidth = outputWidth;
        Span<byte> row = _machineBitmap.AsSpan(rowIndex * _machinePitch, _machinePitch);
        var indices = _vdp.CurrentLineIndices;
        // Output is at least as wide as the MD line. Walking backwards preserves every unread source pixel.
        for (int x = outputWidth - 1; x >= 0; x--)
        {
            int source = Math.Clamp(x * nativeWidth / outputWidth, 0, nativeWidth - 1);
            int active = x * (_vdp.ViewportWidth + 2 * _vdp.ViewportX) / outputWidth - _vdp.ViewportX;
            uint pixel = stride == 4 ? BinaryPrimitives.ReadUInt32LittleEndian(row[(source * 4)..]) : stride == 2 ? BinaryPrimitives.ReadUInt16LittleEndian(row[(source * 2)..]) : row[source];
            if ((uint)active < _vdp.ViewportWidth)
            {
                ushort color = colors[active * 320 / _vdp.ViewportWidth];
                bool backdrop = (_vdp.reg[1] & 0x40) == 0 || (indices[active] & 15) == 0;
                if (adapter.Video.OverridesMegaDrive(color, backdrop))
                {
                    uint rgb = Vdp32X.ToXrgb8888(color);
                    pixel = depth switch { 16 => ((rgb >> 19 & 31) << 11) | ((rgb >> 10 & 63) << 5) | (rgb >> 3 & 31),
                        15 => 0x8000 | ((rgb >> 19 & 31) << 10) | ((rgb >> 11 & 31) << 5) | (rgb >> 3 & 31),
                        8 => ((rgb >> 21 & 7) << 5) | ((rgb >> 13 & 7) << 2) | (rgb >> 6 & 3), _ => rgb };
                }
            }
            if (stride == 4) BinaryPrimitives.WriteUInt32LittleEndian(row[(x * 4)..], pixel);
            else if (stride == 2) BinaryPrimitives.WriteUInt16LittleEndian(row[(x * 2)..], (ushort)pixel); else row[x] = (byte)pixel;
        }
    }
}
