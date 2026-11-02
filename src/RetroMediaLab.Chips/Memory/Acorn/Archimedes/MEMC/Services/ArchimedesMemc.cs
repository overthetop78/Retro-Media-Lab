using System.Buffers.Binary;
using B = RetroMediaLab.Chips.Custom.Acorn.Archimedes.Hardware.Constants.ArchimedesHardwareConstants;
using C = RetroMediaLab.Chips.Memory.Acorn.Archimedes.MEMC.Constants.ArchimedesMemcConstants;

namespace RetroMediaLab.Chips.Memory.Acorn.Archimedes.MEMC.Services;

/// <summary>MEMC : CAM, protections, DRAM et générateurs d'adresses DMA.</summary>
/// <remarks>Les cycles et l'arbitrage du bus appartiennent à la carte. La ROM est empruntée.
/// Les registres MEMC/CAM reçoivent l'adresse du transfert ARM, pas son mot de données.</remarks>
public sealed class ArchimedesMemc
:RetroMediaLab.Chips.Memory.Acorn.Archimedes.MEMC.Interfaces.IArchimedesMemc{
    private readonly ReadOnlyMemory<byte> _rom;
    private readonly byte[] _ram;
    private readonly int _controllerCount;
    private readonly int _bytesPerController;
    private readonly int _dramColumnBits;
    private readonly uint _dramColumnMask;
    private readonly uint _dramRowMask;
    private readonly int[] _logicalSlots = new int[C.LogicalPageCount];
    private readonly uint[] _camWords = new uint[C.PhysicalPageCount];
    private readonly bool[] _camValid = new bool[C.PhysicalPageCount];
    private readonly int[] _camLogical = new int[C.PhysicalPageCount];
    private readonly uint[] _camPhysical = new uint[C.PhysicalPageCount];
    private bool _bootLowSeen;
    private uint _videoInit;
    private uint _videoStart;
    private uint _videoEnd;
    private uint _videoPointer;
    private uint _cursorInit;
    private uint _cursorPointer;
    private uint _soundStart;
    private uint _soundEnd;
    private uint _soundCurrentStart;
    private uint _soundCurrentEnd;
    private uint _soundPointer;
    private bool _soundStartWritten;
    private bool _soundEndWritten;
    private bool _soundNextValid;
    private bool _soundInterrupt;

    public ArchimedesMemc(int ramBytes, ReadOnlyMemory<byte> rom)
    {
        if (ramBytes < C.MinimumRamBytes || ramBytes > C.ControllerBytes * C.MaximumControllers ||
            (ramBytes < C.ControllerBytes ? !System.Numerics.BitOperations.IsPow2(ramBytes) : ramBytes % C.ControllerBytes != 0))
            throw new ArgumentOutOfRangeException(nameof(ramBytes));
        if (rom.IsEmpty || rom.Length % B.WordBytes != 0) throw new ArgumentException(nameof(rom));
        _rom = rom;
        _ram = new byte[ramBytes];
        _controllerCount = Math.Max(1, ramBytes / C.ControllerBytes);
        _bytesPerController = Math.Min(ramBytes, C.ControllerBytes);
        _dramColumnBits = _bytesPerController <= B.Megabyte ? C.DramSmallColumnBits : C.DramLargeColumnBits;
        _dramColumnMask = (1u << _dramColumnBits) - 1;
        var rowBits = _bytesPerController < B.Megabyte ? C.DramSmallRowBits :
            _bytesPerController < C.ControllerBytes ? C.DramMediumRowBits : C.DramLargeRowBits;
        _dramRowMask = (1u << rowBits) - 1;
        Reset(cold: true);
    }

    public uint Control { get; private set; }
    public bool BootRomMapped { get; private set; }
    public int RamBytes => _ram.Length;
    public int PageShift => C.MinimumPageShift + (int)((Control >> C.PageSizeShift) & C.PageSizeMask);
    public bool VideoDmaEnabled => (Control & C.VideoDmaEnable) != 0;
    public bool SoundDmaEnabled => (Control & C.SoundDmaEnable) != 0;
    public bool CursorEnabled { get; private set; }
    public bool SoundInterrupt => _soundInterrupt;
    public uint VideoPointer => _videoPointer;
    public uint SoundPointer => _soundPointer;
    public Action<bool>? SoundInterruptChanged { get; set; }

    public void Reset(bool cold)
    {
        if (cold)
        {
            Array.Clear(_ram);
            Array.Clear(_camValid);
            _videoInit = _videoStart = _videoEnd = _videoPointer = 0;
            _cursorInit = _cursorPointer = 0;
            _soundStart = _soundEnd = _soundCurrentStart = _soundCurrentEnd = _soundPointer = 0;
            Control = 0;
            CursorEnabled = false;
        }
        // RESET ne modifie pas l'autorisation vidéo/curseur, mais coupe le son et le mode OS.
        Control &= C.VideoDmaEnable;
        _soundStartWritten = _soundEndWritten = _soundNextValid = false;
        BootRomMapped = true;
        _bootLowSeen = false;
        RebuildCam();
        _soundInterrupt = true;
        SoundInterruptChanged?.Invoke(true);
    }

    public void ObserveCpuAddress(uint address)
    {
        if (!BootRomMapped) return;
        if ((address & B.BootLowAddressMask) == 0) _bootLowSeen = true;
        else if (_bootLowSeen && (address & B.PhysicalBase) != 0) BootRomMapped = false;
    }

    public bool TryReadLogicalWord(uint address, bool user, out uint value)
    {
        ObserveCpuAddress(address);
        if (BootRomMapped) { value = ReadRomWord(address); return true; }
        if (!Translate(address, user, false, out var physical)) { value = 0; return false; }
        value = ReadPhysicalWord(physical);
        return true;
    }

    public bool TryReadLogicalByte(uint address, bool user, out byte value)
    {
        ObserveCpuAddress(address);
        if (BootRomMapped) { value = ReadRomByte(address); return true; }
        if (!Translate(address, user, false, out var physical)) { value = 0; return false; }
        value = ReadPhysicalByte(physical);
        return true;
    }

    public bool TryWriteLogicalWord(uint address, uint value, bool user)
    {
        ObserveCpuAddress(address);
        if (BootRomMapped) return true;
        if (!Translate(address, user, true, out var physical)) return false;
        WritePhysicalWord(physical, value);
        return true;
    }

    public bool TryWriteLogicalByte(uint address, byte value, bool user)
    {
        ObserveCpuAddress(address);
        if (BootRomMapped) return true;
        if (!Translate(address, user, true, out var physical)) return false;
        WritePhysicalByte(physical, value);
        return true;
    }

    private bool Translate(uint address, bool user, bool write, out uint physical)
    {
        physical = 0;
        if (address >= B.LogicalLimit) return false;
        var slot = _logicalSlots[address >> PageShift];
        if (slot == C.Unmapped) return false;
        var protection = (_camWords[slot] >> C.CamProtectionShift) & C.CamProtectionMask;
        if (user)
        {
            var os = (Control & C.OsModeEnable) != 0;
            var allowed = protection switch
            {
                0 => true,
                1 => os || !write,
                _ => os && !write
            };
            if (!allowed) return false;
        }
        physical = _camPhysical[slot] + (address & ((1u << PageShift) - 1));
        return true;
    }

    private int RamIndex(uint physical)
    {
        physical &= B.PhysicalLimit - B.PhysicalBase - 1;
        var controller = (int)(physical / C.ControllerBytes);
        if (controller >= _controllerCount) return C.Unmapped;
        var offset = physical % C.ControllerBytes;
        var size = PageShift - C.MinimumPageShift;
        var row = (offset >> C.DramRowShift) & ((1u << (C.DramSmallRowBits + (size == 0 ? 0 : size == 3 ? 2 : 1))) - 1);
        var column = (offset >> C.DramColumnLowShift) & C.DramColumnLowMask;
        if (size == 0) column |= (offset >> C.DramColumnMiddleShift) & C.DramColumnFourKMask;
        else
        {
            column |= (offset >> C.DramColumnBitTwoShift) & C.DramColumnBitTwo;
            column |= (offset >> C.DramColumnMiddleShift) & (size == 3 ? C.DramColumnThirtyTwoKMask : C.DramColumnEightKMask);
            if (size >= 2) column |= (offset >> C.DramColumnHighShift) & C.DramColumnBitNine;
            if (size == 3) column |= (offset >> C.DramColumnBitThreeShift) & C.DramColumnBitThree;
        }
        return controller * _bytesPerController + (int)(((row & _dramRowMask) << (_dramColumnBits + C.DramCasBits)) |
            ((column & _dramColumnMask) << C.DramCasBits) | (offset & C.DramCasMask));
    }

    public uint ReadPhysicalWord(uint physical)
    {
        var index = RamIndex(physical & ~(uint)(B.WordBytes - 1));
        return index == C.Unmapped ? B.OpenBusWord : BinaryPrimitives.ReadUInt32LittleEndian(_ram.AsSpan(index, B.WordBytes));
    }

    public byte ReadPhysicalByte(uint physical)
    {
        var index = RamIndex(physical);
        return index == C.Unmapped ? B.OpenBusByte : _ram[index];
    }

    public void WritePhysicalWord(uint physical, uint value)
    {
        var index = RamIndex(physical & ~(uint)(B.WordBytes - 1));
        if (index != C.Unmapped) BinaryPrimitives.WriteUInt32LittleEndian(_ram.AsSpan(index, B.WordBytes), value);
    }

    public void WritePhysicalByte(uint physical, byte value)
    {
        var index = RamIndex(physical);
        if (index != C.Unmapped) _ram[index] = value;
    }

    public uint ReadRomWord(uint address) => BinaryPrimitives.ReadUInt32LittleEndian(
        _rom.Span.Slice((int)((address & B.RomOffsetMask & ~(uint)(B.WordBytes - 1)) % (uint)_rom.Length), B.WordBytes));

    public byte ReadRomByte(uint address) => _rom.Span[(int)((address & B.RomOffsetMask) % (uint)_rom.Length)];

    public int RomCycles(uint address, bool sequential)
    {
        var shift = address >= B.HighRomBase || BootRomMapped ? C.RomHighTimingShift : C.RomLowTimingShift;
        return ((Control >> shift) & C.RomTimingMask) switch
        {
            0 => C.RomTiming450Ns,
            1 => C.RomTiming325Ns,
            2 => C.RomTiming200Ns,
            _ => sequential ? C.RomTimingNibble : C.RomTiming200Ns
        };
    }

    public void WriteCam(uint address)
    {
        var controller = (int)((address >> C.CamControllerLowShift) & 1);
        if (PageShift != C.MinimumPageShift) controller |= (int)((address >> C.CamControllerHighShift) & 1) << 1;
        if (controller >= _controllerCount) return;
        var slot = controller * C.PhysicalPagesPerController + (int)(address & C.CamPhysicalMask);
        if (_camValid[slot] && _logicalSlots[_camLogical[slot]] == slot) _logicalSlots[_camLogical[slot]] = C.Unmapped;
        _camWords[slot] = address;
        _camValid[slot] = true;
        DecodeCam(slot);
    }

    private void DecodeCam(int slot)
    {
        var word = _camWords[slot];
        var size = PageShift - C.MinimumPageShift;
        var page = (word & C.CamPhysicalMask) >> size;
        if (size == 1) page |= (word & 1) << 6;
        else if (size == 2) page |= (word & 3) << 5;
        else if (size == 3) page |= ((word & 1) << 4) | ((word & 2) << 5) | ((word & 4) << 3);
        var high = ((word >> C.CamLogicalHighShift) & C.CamLogicalHighMask) << (11 - size);
        var logical = (int)(((word >> PageShift) & ((1u << (11 - size)) - 1)) | high);
        _camLogical[slot] = logical;
        _camPhysical[slot] = (uint)(slot / C.PhysicalPagesPerController * C.ControllerBytes) + (page << PageShift);
        _logicalSlots[logical] = slot;
    }

    private void RebuildCam()
    {
        Array.Fill(_logicalSlots, C.Unmapped);
        for (var slot = 0; slot < _camValid.Length; slot++) if (_camValid[slot]) DecodeCam(slot);
    }

    public void WriteRegister(uint address)
    {
        var dma = ((address >> C.DmaInputShift) & C.DmaInputMask) << C.DmaAddressShift;
        switch ((address >> C.RegisterShift) & C.RegisterMask)
        {
            case C.VideoInitRegister: _videoInit = dma; break;
            case C.VideoStartRegister: _videoStart = dma; break;
            case C.VideoEndRegister: _videoEnd = dma; break;
            case C.CursorInitRegister: _cursorInit = dma; CursorEnabled = true; break;
            case C.SoundStartRegister:
                _soundStart = dma;
                _soundStartWritten = true;
                _soundNextValid = _soundEndWritten;
                SetSoundInterrupt(false);
                break;
            case C.SoundEndRegister:
                _soundEnd = dma;
                _soundEndWritten = true;
                _soundNextValid = _soundStartWritten;
                break;
            case C.SoundPointerRegister:
                SwitchSoundBuffer();
                break;
            case C.ControlRegister:
                var previousShift = PageShift;
                Control = address & C.ControlMask;
                if (previousShift != PageShift) RebuildCam();
                if (!VideoDmaEnabled) CursorEnabled = false;
                break;
        }
    }

    public void BeginVideoFrame()
    {
        _videoPointer = _videoInit;
    }

    public void BeginCursorFrame() => _cursorPointer = _cursorInit;

    public void ReadVideoBurst(Span<uint> words)
    {
        ReadDmaBurst(_videoPointer, words);
        _videoPointer = (_videoPointer + C.DmaBurstBytes) & C.DmaAddressMask;
        if (_videoPointer > _videoEnd) _videoPointer = _videoStart;
    }

    public void ReadCursorBurst(Span<uint> words)
    {
        ReadDmaBurst(_cursorPointer, words);
        _cursorPointer = (_cursorPointer + C.DmaBurstBytes) & C.DmaAddressMask;
    }

    public void ReadSoundBurst(Span<uint> words)
    {
        ReadDmaBurst(_soundPointer, words);
        _soundPointer = (_soundPointer + C.DmaBurstBytes) & C.DmaAddressMask;
        if (_soundPointer <= _soundCurrentEnd) return;
        if (_soundNextValid) SwitchSoundBuffer();
        else { _soundPointer = _soundCurrentStart; SetSoundInterrupt(true); }
    }

    private void ReadDmaBurst(uint pointer, Span<uint> words)
    {
        if (words.Length != C.DmaBurstBytes / B.WordBytes) throw new ArgumentException(nameof(words));
        for (var index = 0; index < words.Length; index++) words[index] = ReadPhysicalWord(pointer + (uint)(index * B.WordBytes));
    }

    private void SwitchSoundBuffer()
    {
        _soundCurrentStart = _soundPointer = _soundStart;
        _soundCurrentEnd = _soundEnd;
        _soundStartWritten = _soundEndWritten = _soundNextValid = false;
        SetSoundInterrupt(true);
    }

    private void SetSoundInterrupt(bool active)
    {
        if (_soundInterrupt == active) return;
        _soundInterrupt = active;
        SoundInterruptChanged?.Invoke(active);
    }
}
