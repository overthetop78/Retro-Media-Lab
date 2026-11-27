using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using RetroMediaLab.Chips.Audio.Yamaha.AICA;
using RetroMediaLab.Chips.Video.NEC.PowerVr2;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

internal sealed unsafe partial class Sh4AddressSpace
{
    private const uint HandlerMax = 0x1F;
    private const int HandlerCount = (int)HandlerMax + 1;
    private const uint Megabyte = 1024 * 1024;
    private const uint MaximumVramSize = 16 * Megabyte;
    private const uint MaximumAramSize = 8 * Megabyte;
    private const uint RamStartOffset = 0;

    private static uint _lastRegisteredHandler;
    private static readonly Func<uint, byte>?[] ReadHandlers8 = new Func<uint, byte>?[HandlerCount];
    private static readonly Action<uint, byte>?[] WriteHandlers8 = new Action<uint, byte>?[HandlerCount];
    private static readonly Func<uint, ushort>?[] ReadHandlers16 = new Func<uint, ushort>?[HandlerCount];
    private static readonly Action<uint, ushort>?[] WriteHandlers16 = new Action<uint, ushort>?[HandlerCount];
    private static readonly Func<uint, uint>?[] ReadHandlers32 = new Func<uint, uint>?[HandlerCount];
    private static readonly Action<uint, uint>?[] WriteHandlers32 = new Action<uint, uint>?[HandlerCount];
    private static readonly nuint[] MemoryInformation = new nuint[0x100];

    internal nuint ReadConstant(uint address, out bool isMemory, int size)
    {
        uint page = address >> 24;
        nuint mapping = MemoryInformation[page];
        nuint pointer = mapping & ~(nuint)HandlerMax;
        if (pointer == 0)
        {
            isMemory = false;
            switch (size)
            {
                case 1:
                    return NativeReadEntries8[(int)mapping];
                case 2:
                    return NativeReadEntries16[(int)mapping];
                case 4:
                    return NativeReadEntries32[(int)mapping];
                default:
                    Sh4HostRuntime.Die("Invalid size");
                    return 0;
            }
        }

        isMemory = true;
        int shift = (int)(mapping & HandlerMax);
        address <<= shift;
        address >>= shift;
        return pointer + address;
    }

    internal nuint WriteConstant(uint address, out bool isMemory, int size)
    {
        uint page = address >> 24;
        nuint mapping = MemoryInformation[page];
        nuint pointer = mapping & ~(nuint)HandlerMax;
        if (pointer == 0)
        {
            isMemory = false;
            switch (size)
            {
                case 1:
                    return NativeWriteEntries8[(int)mapping];
                case 2:
                    return NativeWriteEntries16[(int)mapping];
                case 4:
                    return NativeWriteEntries32[(int)mapping];
                default:
                    Sh4HostRuntime.Die("Invalid size");
                    return 0;
            }
        }

        isMemory = true;
        int shift = (int)(mapping & HandlerMax);
        address <<= shift;
        address >>= shift;
        return pointer + address;
    }

    internal T Read<T>(uint address) where T : unmanaged
    {
        uint page = address >> 24;
        nuint mapping = MemoryInformation[page];
        nuint pointer = mapping & ~(nuint)HandlerMax;
        if (pointer != 0)
        {
            int shift = (int)(mapping & HandlerMax);
            address <<= shift;
            address >>= shift;
            return *(T*)(pointer + address);
        }

        int id = (int)mapping;
        switch (sizeof(T))
        {
            case 1:
                return Unsafe.BitCast<byte, T>(ReadHandlers8[id]!(address));
            case 2:
                return Unsafe.BitCast<ushort, T>(ReadHandlers16[id]!(address));
            case 4:
                return Unsafe.BitCast<uint, T>(ReadHandlers32[id]!(address));
            case 8:
            {
                ulong value = ReadHandlers32[id]!(address);
                value |= (ulong)ReadHandlers32[id]!(address + 4) << 32;
                return Unsafe.BitCast<ulong, T>(value);
            }
            default:
                Sh4HostRuntime.Die("Invalid size");
                return default;
        }
    }

    internal void Write<T>(uint address, T value) where T : unmanaged
    {
        uint page = address >> 24;
        nuint mapping = MemoryInformation[page];
        nuint pointer = mapping & ~(nuint)HandlerMax;
        if (pointer != 0)
        {
            int shift = (int)(mapping & HandlerMax);
            address <<= shift;
            address >>= shift;
            *(T*)(pointer + address) = value;
            return;
        }

        int id = (int)mapping;
        switch (sizeof(T))
        {
            case 1:
                WriteHandlers8[id]!(address, Unsafe.BitCast<T, byte>(value));
                break;
            case 2:
                WriteHandlers16[id]!(address, Unsafe.BitCast<T, ushort>(value));
                break;
            case 4:
                WriteHandlers32[id]!(address, Unsafe.BitCast<T, uint>(value));
                break;
            case 8:
            {
                ulong bits = Unsafe.BitCast<T, ulong>(value);
                WriteHandlers32[id]!(address, (uint)bits);
                WriteHandlers32[id]!(address + 4, (uint)(bits >> 32));
                break;
            }
            default:
                Sh4HostRuntime.Die("Invalid size");
                break;
        }
    }

    internal byte Read8(uint address) => Read<byte>(address);
    internal ushort Read16(uint address) => Read<ushort>(address);
    internal uint Read32(uint address) => Read<uint>(address);
    internal ulong Read64(uint address) => Read<ulong>(address);

    internal void Write8(uint address, byte data) => Write(address, data);
    internal void Write16(uint address, ushort data) => Write(address, data);
    internal void Write32(uint address, uint data) => Write(address, data);
    internal void Write64(uint address, ulong data) => Write(address, data);

    private static T ReadMemoryNotMapped<T>(uint address) where T : unmanaged
    {
        Trace.TraceInformation($"[sh4]read{sizeof(T)} from {address:x8}, not mapped (default handler)");
        return default;
    }

    private static void WriteMemoryNotMapped<T>(uint address, T data) where T : unmanaged
    {
        Trace.TraceInformation($"[sh4]Write{sizeof(T)} to {address:x8} = {data:x}, not mapped (default handler)");
    }

    internal int RegisterHandler(
        Func<uint, byte>? read8, Func<uint, ushort>? read16, Func<uint, uint>? read32,
        Action<uint, byte>? write8, Action<uint, ushort>? write16, Action<uint, uint>? write32)
    {
        int id = (int)_lastRegisteredHandler++;
        Debug.Assert(id < HandlerCount);
        ReadHandlers8[id] = read8 ?? ReadMemoryNotMapped<byte>;
        ReadHandlers16[id] = read16 ?? ReadMemoryNotMapped<ushort>;
        ReadHandlers32[id] = read32 ?? ReadMemoryNotMapped<uint>;
        WriteHandlers8[id] = write8 ?? WriteMemoryNotMapped<byte>;
        WriteHandlers16[id] = write16 ?? WriteMemoryNotMapped<ushort>;
        WriteHandlers32[id] = write32 ?? WriteMemoryNotMapped<uint>;
        return id;
    }

    private static uint FindMask(uint mask)
    {
        uint allBits = uint.MaxValue;
        uint shift = 0;
        while (mask != allBits >> (int)shift)
            shift++;
        return shift;
    }

    internal void MapHandler(int handler, uint start, uint end)
    {
        Debug.Assert(start < 0x100);
        Debug.Assert(end < 0x100);
        Debug.Assert(start <= end);
        for (uint i = start; i <= end; i++)
            MemoryInformation[i] = (nuint)handler;
    }

    internal void MapBlock(nuint address, uint start, uint end, uint mask)
    {
        Debug.Assert(start < 0x100);
        Debug.Assert(end < 0x100);
        Debug.Assert(start <= end);
        Debug.Assert((address & 0xFF) == 0);
        Debug.Assert(address != 0);
        uint offset = 0;
        for (uint i = start; i <= end; i++)
        {
            MemoryInformation[i] = address + (offset & mask) + FindMask(mask) - (offset & mask);
            offset += 0x1000000;
        }
    }

    internal void MirrorMapping(uint newRegion, uint start, uint size)
    {
        uint end = start + size - 1;
        Debug.Assert(start < 0x100);
        Debug.Assert(end < 0x100);
        Debug.Assert(start <= end);
        Debug.Assert(!(start >= newRegion && end <= newRegion));
        uint destination = newRegion;
        for (uint i = start; i <= end; i++)
        {
            MemoryInformation[destination & 0xFF] = MemoryInformation[i & 0xFF];
            destination++;
        }
    }

    internal void InitializeAddressMap()
    {
        Array.Clear(ReadHandlers8);
        Array.Clear(ReadHandlers16);
        Array.Clear(ReadHandlers32);
        Array.Clear(WriteHandlers8);
        Array.Clear(WriteHandlers16);
        Array.Clear(WriteHandlers32);
        Array.Clear(MemoryInformation);
        _lastRegisteredHandler = 0;

        int defaultHandler = RegisterHandler(null, null, null, null, null, null);
        Debug.Assert(defaultHandler == 0);
    }

    internal void TerminateAddressMap()
    {
    }

    private static nuint _ramBase;
    private static nuint _recompilerControlBlock;

    internal nuint RecompilerControlBlock => _recompilerControlBlock;

    private static nuint AllocatePages(nuint size) =>
        (nuint)NativeMemory.AlignedAlloc(size, (nuint)Environment.SystemPageSize);

    private static void FreePages(nuint pointer) => NativeMemory.AlignedFree((void*)pointer);

#if !SH4_DYNAREC_NONE
    internal static void ResetBlockPointers()
    {
        uint size = (uint)(Sh4ContextLayout.FpcbSize * sizeof(nuint));
        if (_ramBase != 0)
            Sh4VirtualMemory.ResetMemory(_recompilerControlBlock, size);
        else
            Sh4RecompilerDriver.FillAddressSpaceBlockPointers(new Sh4RecompilerControlBlock(_recompilerControlBlock).BlockPointers);
    }

    internal bool BlockManagerLockedWrite(nuint address)
    {
        if (_ramBase == 0)
            return false;

        nuint start = _recompilerControlBlock;
        nuint end = start + (nuint)Sh4ContextLayout.FpcbSize * (nuint)sizeof(nuint);
        if (address >= start && address < end)
        {
            uint pageSize = (uint)Environment.SystemPageSize;
            nuint aligned = address & ~((nuint)pageSize - 1);
            Sh4VirtualMemory.AllocateOnDemandPage(aligned, pageSize);
            Sh4RecompilerDriver.FillAddressSpaceBlockPointers(new Span<nuint>((void*)aligned, (int)(pageSize / sizeof(nuint))));
            return true;
        }
        return false;
    }
#endif

    internal bool ReserveAddressSpace()
    {
        if (_ramBase != 0)
            return true;

        Sh4VirtualMemory.Initialize(ref _ramBase, ref _recompilerControlBlock,
            MaximumRamSize + MaximumVramSize + MaximumAramSize + 32 * Megabyte);
        return true;
    }

    private static void TerminateMappings()
    {
        if (_ramBase == 0)
        {
            FreePages(_recompilerControlBlock);
            _recompilerControlBlock = 0;
            MainRam.Free();
            PvrMemory.Vram.Free();
            AicaInterface.RamRegion.Free();
            FreePages((nuint)PvrElan.Ram);
            PvrElan.Ram = null;
            Array.Clear(MemoryInformation);
        }
    }

    internal void InitializeMappings()
    {
        TerminateMappings();

        Sh4Platform platform = Sh4DreamcastRuntime.Platform;
        uint ramSize = platform.RamSize;
        uint vramSize = platform.VramSize;
        uint aramSize = platform.AramSize;
        uint eramSize = PvrElan.RamSize;
        if (_ramBase == 0)
        {
            Trace.TraceWarning("Warning! nvmem is DISABLED (due to failure or not being built-in)");
            _recompilerControlBlock = AllocatePages(Sh4ContextLayout.ControlBlockSize);
            var controlBlock = new Sh4RecompilerControlBlock(_recompilerControlBlock);
#if !SH4_DYNAREC_NONE
            Sh4RecompilerDriver.FillAddressSpaceBlockPointers(controlBlock.BlockPointers);
#endif
            controlBlock.Context = default;

            MainRam.Allocate(ramSize);
            PvrMemory.Vram.Allocate(vramSize);
            AicaInterface.RamRegion.Allocate(aramSize);
            PvrElan.Ram = (byte*)AllocatePages(eramSize);
        }
        else
        {
            Trace.TraceInformation("Info: nvmem is enabled");
            Trace.TraceInformation($"Info: p_sh4rcb: 0x{_recompilerControlBlock:x} ram_base: 0x{_ramBase:x}");
            uint vramStartOffset = RamStartOffset + ramSize;
            uint aramStartOffset = vramStartOffset + vramSize;
            uint eramStartOffset = aramStartOffset + aramSize;
            var mappings = new Sh4VirtualMemory.Mapping[]
            {
                new() { StartAddress = 0x00000000, EndAddress = 0x00800000 },
                new() { StartAddress = 0x00800000, EndAddress = 0x01000000, MemoryOffset = aramStartOffset, MemorySize = aramSize },
                new() { StartAddress = 0x01000000, EndAddress = 0x04000000 },
                new() { StartAddress = 0x04000000, EndAddress = 0x05000000, MemoryOffset = vramStartOffset, MemorySize = vramSize, AllowWrites = true },
                new() { StartAddress = 0x05000000, EndAddress = 0x06000000 },
                new() { StartAddress = 0x06000000, EndAddress = 0x07000000, MemoryOffset = vramStartOffset, MemorySize = vramSize, AllowWrites = true },
                new() { StartAddress = 0x07000000, EndAddress = 0x08000000 },
                new() { StartAddress = 0x08000000, EndAddress = 0x0A000000 },
                new() { StartAddress = 0x0A000000, EndAddress = 0x0C000000, MemoryOffset = eramStartOffset, MemorySize = eramSize, AllowWrites = true },
                new() { StartAddress = 0x0C000000, EndAddress = 0x10000000, MemoryOffset = RamStartOffset, MemorySize = ramSize, AllowWrites = true },
                new() { StartAddress = 0x10000000, EndAddress = 0x20000000 },
                new() { StartAddress = 0x20000000, EndAddress = 0x20800000, MemoryOffset = aramStartOffset, MemorySize = aramSize, AllowWrites = true }
            };
            Sh4VirtualMemory.CreateMappings(mappings);
            AicaInterface.RamRegion.SetRegion((byte*)(_ramBase + 0x20000000), aramSize);
            PvrMemory.Vram.SetRegion((byte*)(_ramBase + 0x04000000), vramSize);
            MainRam.SetRegion((byte*)(_ramBase + 0x0C000000), ramSize);
            PvrElan.Ram = (byte*)(_ramBase + 0x0A000000);
        }

        AicaInterface.RamRegion.Zero();
        PvrMemory.Vram.Zero();
        MainRam.Zero();
        Trace.TraceInformation(
            $"BASE 0x{_ramBase:x} RAM({ramSize / Megabyte} MB) 0x{(nuint)Unsafe.AsPointer(ref MainRam[0]):x} "
            + $"VRAM64({vramSize / Megabyte} MB) 0x{VideoRamBuffer:x} "
            + $"ARAM({aramSize / Megabyte} MB) 0x{(nuint)Unsafe.AsPointer(ref AicaInterface.RamRegion[0]):x}");
    }

    internal void ReleaseAddressSpace()
    {
        if (_ramBase != 0)
        {
            Sh4VirtualMemory.Destroy();
            _ramBase = 0;
        }
        else
        {
            UnprotectVram(0, PvrMemory.VramSize);
            TerminateMappings();
        }
    }

    internal static void ProtectVram(uint address, uint size)
    {
        address &= VideoRamMask;
        if (_ramBase != 0)
        {
            Sh4VirtualMemory.LockRegion(_ramBase + 0x04000000 + address, size);
            if (PvrMemory.VramSize == 0x800000)
                Sh4VirtualMemory.LockRegion(_ramBase + 0x04000000 + address + PvrMemory.VramSize, size);
        }
        else
        {
            Sh4VirtualMemory.LockRegion(VideoRamBuffer + address, size);
        }
    }

    internal static void UnprotectVram(uint address, uint size)
    {
        address &= VideoRamMask;
        if (_ramBase != 0)
        {
            Sh4VirtualMemory.UnlockRegion(_ramBase + 0x04000000 + address, size);
            if (PvrMemory.VramSize == 0x800000)
                Sh4VirtualMemory.UnlockRegion(_ramBase + 0x04000000 + address + PvrMemory.VramSize, size);
        }
        else
        {
            Sh4VirtualMemory.UnlockRegion(VideoRamBuffer + address, size);
        }
    }

    internal static uint GetVramOffset(nuint address)
    {
        if (_ramBase != 0)
        {
            nint offset = unchecked((nint)(address - _ramBase));
            if (offset < 0 || offset >= 0x20000000)
                return uint.MaxValue;
            if ((offset >> 24) != 4)
                return uint.MaxValue;
            return (uint)offset & VideoRamMask;
        }

        nuint vramOffset = Sh4VirtualMemory.Untag(address) - Sh4VirtualMemory.Untag(VideoRamBuffer);
        if (vramOffset >= PvrMemory.VramSize)
            return uint.MaxValue;
        return (uint)vramOffset;
    }

    internal static void GetAddress(out nuint ramBase, out nuint ram, out nuint vram, out nuint aica)
    {
        if (_ramBase != 0)
        {
            ramBase = _ramBase;
            ram = (nuint)Unsafe.AsPointer(ref MainRam[0]);
            vram = VideoRamBuffer;
            aica = (nuint)Unsafe.AsPointer(ref AicaInterface.RamRegion[0]);
        }
        else
        {
            ramBase = 0;
            ram = 0;
            vram = 0;
            aica = 0;
        }
    }

    private static readonly nuint[] NativeReadEntries8 = RuntimeInformation.ProcessArchitecture == Architecture.X86
        ? new nuint[]
        {
            (nuint)(delegate* unmanaged[Fastcall]<uint, byte>)&Read8Handler0X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, byte>)&Read8Handler1X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, byte>)&Read8Handler2X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, byte>)&Read8Handler3X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, byte>)&Read8Handler4X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, byte>)&Read8Handler5X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, byte>)&Read8Handler6X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, byte>)&Read8Handler7X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, byte>)&Read8Handler8X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, byte>)&Read8Handler9X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, byte>)&Read8Handler10X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, byte>)&Read8Handler11X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, byte>)&Read8Handler12X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, byte>)&Read8Handler13X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, byte>)&Read8Handler14X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, byte>)&Read8Handler15X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, byte>)&Read8Handler16X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, byte>)&Read8Handler17X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, byte>)&Read8Handler18X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, byte>)&Read8Handler19X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, byte>)&Read8Handler20X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, byte>)&Read8Handler21X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, byte>)&Read8Handler22X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, byte>)&Read8Handler23X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, byte>)&Read8Handler24X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, byte>)&Read8Handler25X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, byte>)&Read8Handler26X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, byte>)&Read8Handler27X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, byte>)&Read8Handler28X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, byte>)&Read8Handler29X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, byte>)&Read8Handler30X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, byte>)&Read8Handler31X86
        }
        : new nuint[]
        {
            (nuint)(delegate* unmanaged<uint, byte>)&Read8Handler0,
            (nuint)(delegate* unmanaged<uint, byte>)&Read8Handler1,
            (nuint)(delegate* unmanaged<uint, byte>)&Read8Handler2,
            (nuint)(delegate* unmanaged<uint, byte>)&Read8Handler3,
            (nuint)(delegate* unmanaged<uint, byte>)&Read8Handler4,
            (nuint)(delegate* unmanaged<uint, byte>)&Read8Handler5,
            (nuint)(delegate* unmanaged<uint, byte>)&Read8Handler6,
            (nuint)(delegate* unmanaged<uint, byte>)&Read8Handler7,
            (nuint)(delegate* unmanaged<uint, byte>)&Read8Handler8,
            (nuint)(delegate* unmanaged<uint, byte>)&Read8Handler9,
            (nuint)(delegate* unmanaged<uint, byte>)&Read8Handler10,
            (nuint)(delegate* unmanaged<uint, byte>)&Read8Handler11,
            (nuint)(delegate* unmanaged<uint, byte>)&Read8Handler12,
            (nuint)(delegate* unmanaged<uint, byte>)&Read8Handler13,
            (nuint)(delegate* unmanaged<uint, byte>)&Read8Handler14,
            (nuint)(delegate* unmanaged<uint, byte>)&Read8Handler15,
            (nuint)(delegate* unmanaged<uint, byte>)&Read8Handler16,
            (nuint)(delegate* unmanaged<uint, byte>)&Read8Handler17,
            (nuint)(delegate* unmanaged<uint, byte>)&Read8Handler18,
            (nuint)(delegate* unmanaged<uint, byte>)&Read8Handler19,
            (nuint)(delegate* unmanaged<uint, byte>)&Read8Handler20,
            (nuint)(delegate* unmanaged<uint, byte>)&Read8Handler21,
            (nuint)(delegate* unmanaged<uint, byte>)&Read8Handler22,
            (nuint)(delegate* unmanaged<uint, byte>)&Read8Handler23,
            (nuint)(delegate* unmanaged<uint, byte>)&Read8Handler24,
            (nuint)(delegate* unmanaged<uint, byte>)&Read8Handler25,
            (nuint)(delegate* unmanaged<uint, byte>)&Read8Handler26,
            (nuint)(delegate* unmanaged<uint, byte>)&Read8Handler27,
            (nuint)(delegate* unmanaged<uint, byte>)&Read8Handler28,
            (nuint)(delegate* unmanaged<uint, byte>)&Read8Handler29,
            (nuint)(delegate* unmanaged<uint, byte>)&Read8Handler30,
            (nuint)(delegate* unmanaged<uint, byte>)&Read8Handler31
        };

    private static readonly nuint[] NativeReadEntries16 = RuntimeInformation.ProcessArchitecture == Architecture.X86
        ? new nuint[]
        {
            (nuint)(delegate* unmanaged[Fastcall]<uint, ushort>)&Read16Handler0X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, ushort>)&Read16Handler1X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, ushort>)&Read16Handler2X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, ushort>)&Read16Handler3X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, ushort>)&Read16Handler4X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, ushort>)&Read16Handler5X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, ushort>)&Read16Handler6X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, ushort>)&Read16Handler7X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, ushort>)&Read16Handler8X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, ushort>)&Read16Handler9X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, ushort>)&Read16Handler10X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, ushort>)&Read16Handler11X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, ushort>)&Read16Handler12X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, ushort>)&Read16Handler13X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, ushort>)&Read16Handler14X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, ushort>)&Read16Handler15X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, ushort>)&Read16Handler16X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, ushort>)&Read16Handler17X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, ushort>)&Read16Handler18X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, ushort>)&Read16Handler19X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, ushort>)&Read16Handler20X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, ushort>)&Read16Handler21X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, ushort>)&Read16Handler22X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, ushort>)&Read16Handler23X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, ushort>)&Read16Handler24X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, ushort>)&Read16Handler25X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, ushort>)&Read16Handler26X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, ushort>)&Read16Handler27X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, ushort>)&Read16Handler28X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, ushort>)&Read16Handler29X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, ushort>)&Read16Handler30X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, ushort>)&Read16Handler31X86
        }
        : new nuint[]
        {
            (nuint)(delegate* unmanaged<uint, ushort>)&Read16Handler0,
            (nuint)(delegate* unmanaged<uint, ushort>)&Read16Handler1,
            (nuint)(delegate* unmanaged<uint, ushort>)&Read16Handler2,
            (nuint)(delegate* unmanaged<uint, ushort>)&Read16Handler3,
            (nuint)(delegate* unmanaged<uint, ushort>)&Read16Handler4,
            (nuint)(delegate* unmanaged<uint, ushort>)&Read16Handler5,
            (nuint)(delegate* unmanaged<uint, ushort>)&Read16Handler6,
            (nuint)(delegate* unmanaged<uint, ushort>)&Read16Handler7,
            (nuint)(delegate* unmanaged<uint, ushort>)&Read16Handler8,
            (nuint)(delegate* unmanaged<uint, ushort>)&Read16Handler9,
            (nuint)(delegate* unmanaged<uint, ushort>)&Read16Handler10,
            (nuint)(delegate* unmanaged<uint, ushort>)&Read16Handler11,
            (nuint)(delegate* unmanaged<uint, ushort>)&Read16Handler12,
            (nuint)(delegate* unmanaged<uint, ushort>)&Read16Handler13,
            (nuint)(delegate* unmanaged<uint, ushort>)&Read16Handler14,
            (nuint)(delegate* unmanaged<uint, ushort>)&Read16Handler15,
            (nuint)(delegate* unmanaged<uint, ushort>)&Read16Handler16,
            (nuint)(delegate* unmanaged<uint, ushort>)&Read16Handler17,
            (nuint)(delegate* unmanaged<uint, ushort>)&Read16Handler18,
            (nuint)(delegate* unmanaged<uint, ushort>)&Read16Handler19,
            (nuint)(delegate* unmanaged<uint, ushort>)&Read16Handler20,
            (nuint)(delegate* unmanaged<uint, ushort>)&Read16Handler21,
            (nuint)(delegate* unmanaged<uint, ushort>)&Read16Handler22,
            (nuint)(delegate* unmanaged<uint, ushort>)&Read16Handler23,
            (nuint)(delegate* unmanaged<uint, ushort>)&Read16Handler24,
            (nuint)(delegate* unmanaged<uint, ushort>)&Read16Handler25,
            (nuint)(delegate* unmanaged<uint, ushort>)&Read16Handler26,
            (nuint)(delegate* unmanaged<uint, ushort>)&Read16Handler27,
            (nuint)(delegate* unmanaged<uint, ushort>)&Read16Handler28,
            (nuint)(delegate* unmanaged<uint, ushort>)&Read16Handler29,
            (nuint)(delegate* unmanaged<uint, ushort>)&Read16Handler30,
            (nuint)(delegate* unmanaged<uint, ushort>)&Read16Handler31
        };

    private static readonly nuint[] NativeReadEntries32 = RuntimeInformation.ProcessArchitecture == Architecture.X86
        ? new nuint[]
        {
            (nuint)(delegate* unmanaged[Fastcall]<uint, uint>)&Read32Handler0X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, uint>)&Read32Handler1X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, uint>)&Read32Handler2X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, uint>)&Read32Handler3X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, uint>)&Read32Handler4X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, uint>)&Read32Handler5X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, uint>)&Read32Handler6X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, uint>)&Read32Handler7X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, uint>)&Read32Handler8X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, uint>)&Read32Handler9X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, uint>)&Read32Handler10X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, uint>)&Read32Handler11X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, uint>)&Read32Handler12X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, uint>)&Read32Handler13X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, uint>)&Read32Handler14X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, uint>)&Read32Handler15X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, uint>)&Read32Handler16X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, uint>)&Read32Handler17X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, uint>)&Read32Handler18X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, uint>)&Read32Handler19X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, uint>)&Read32Handler20X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, uint>)&Read32Handler21X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, uint>)&Read32Handler22X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, uint>)&Read32Handler23X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, uint>)&Read32Handler24X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, uint>)&Read32Handler25X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, uint>)&Read32Handler26X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, uint>)&Read32Handler27X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, uint>)&Read32Handler28X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, uint>)&Read32Handler29X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, uint>)&Read32Handler30X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, uint>)&Read32Handler31X86
        }
        : new nuint[]
        {
            (nuint)(delegate* unmanaged<uint, uint>)&Read32Handler0,
            (nuint)(delegate* unmanaged<uint, uint>)&Read32Handler1,
            (nuint)(delegate* unmanaged<uint, uint>)&Read32Handler2,
            (nuint)(delegate* unmanaged<uint, uint>)&Read32Handler3,
            (nuint)(delegate* unmanaged<uint, uint>)&Read32Handler4,
            (nuint)(delegate* unmanaged<uint, uint>)&Read32Handler5,
            (nuint)(delegate* unmanaged<uint, uint>)&Read32Handler6,
            (nuint)(delegate* unmanaged<uint, uint>)&Read32Handler7,
            (nuint)(delegate* unmanaged<uint, uint>)&Read32Handler8,
            (nuint)(delegate* unmanaged<uint, uint>)&Read32Handler9,
            (nuint)(delegate* unmanaged<uint, uint>)&Read32Handler10,
            (nuint)(delegate* unmanaged<uint, uint>)&Read32Handler11,
            (nuint)(delegate* unmanaged<uint, uint>)&Read32Handler12,
            (nuint)(delegate* unmanaged<uint, uint>)&Read32Handler13,
            (nuint)(delegate* unmanaged<uint, uint>)&Read32Handler14,
            (nuint)(delegate* unmanaged<uint, uint>)&Read32Handler15,
            (nuint)(delegate* unmanaged<uint, uint>)&Read32Handler16,
            (nuint)(delegate* unmanaged<uint, uint>)&Read32Handler17,
            (nuint)(delegate* unmanaged<uint, uint>)&Read32Handler18,
            (nuint)(delegate* unmanaged<uint, uint>)&Read32Handler19,
            (nuint)(delegate* unmanaged<uint, uint>)&Read32Handler20,
            (nuint)(delegate* unmanaged<uint, uint>)&Read32Handler21,
            (nuint)(delegate* unmanaged<uint, uint>)&Read32Handler22,
            (nuint)(delegate* unmanaged<uint, uint>)&Read32Handler23,
            (nuint)(delegate* unmanaged<uint, uint>)&Read32Handler24,
            (nuint)(delegate* unmanaged<uint, uint>)&Read32Handler25,
            (nuint)(delegate* unmanaged<uint, uint>)&Read32Handler26,
            (nuint)(delegate* unmanaged<uint, uint>)&Read32Handler27,
            (nuint)(delegate* unmanaged<uint, uint>)&Read32Handler28,
            (nuint)(delegate* unmanaged<uint, uint>)&Read32Handler29,
            (nuint)(delegate* unmanaged<uint, uint>)&Read32Handler30,
            (nuint)(delegate* unmanaged<uint, uint>)&Read32Handler31
        };

    private static readonly nuint[] NativeWriteEntries8 = RuntimeInformation.ProcessArchitecture == Architecture.X86
        ? new nuint[]
        {
            (nuint)(delegate* unmanaged[Fastcall]<uint, byte, void>)&Write8Handler0X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, byte, void>)&Write8Handler1X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, byte, void>)&Write8Handler2X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, byte, void>)&Write8Handler3X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, byte, void>)&Write8Handler4X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, byte, void>)&Write8Handler5X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, byte, void>)&Write8Handler6X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, byte, void>)&Write8Handler7X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, byte, void>)&Write8Handler8X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, byte, void>)&Write8Handler9X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, byte, void>)&Write8Handler10X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, byte, void>)&Write8Handler11X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, byte, void>)&Write8Handler12X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, byte, void>)&Write8Handler13X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, byte, void>)&Write8Handler14X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, byte, void>)&Write8Handler15X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, byte, void>)&Write8Handler16X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, byte, void>)&Write8Handler17X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, byte, void>)&Write8Handler18X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, byte, void>)&Write8Handler19X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, byte, void>)&Write8Handler20X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, byte, void>)&Write8Handler21X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, byte, void>)&Write8Handler22X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, byte, void>)&Write8Handler23X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, byte, void>)&Write8Handler24X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, byte, void>)&Write8Handler25X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, byte, void>)&Write8Handler26X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, byte, void>)&Write8Handler27X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, byte, void>)&Write8Handler28X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, byte, void>)&Write8Handler29X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, byte, void>)&Write8Handler30X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, byte, void>)&Write8Handler31X86
        }
        : new nuint[]
        {
            (nuint)(delegate* unmanaged<uint, byte, void>)&Write8Handler0,
            (nuint)(delegate* unmanaged<uint, byte, void>)&Write8Handler1,
            (nuint)(delegate* unmanaged<uint, byte, void>)&Write8Handler2,
            (nuint)(delegate* unmanaged<uint, byte, void>)&Write8Handler3,
            (nuint)(delegate* unmanaged<uint, byte, void>)&Write8Handler4,
            (nuint)(delegate* unmanaged<uint, byte, void>)&Write8Handler5,
            (nuint)(delegate* unmanaged<uint, byte, void>)&Write8Handler6,
            (nuint)(delegate* unmanaged<uint, byte, void>)&Write8Handler7,
            (nuint)(delegate* unmanaged<uint, byte, void>)&Write8Handler8,
            (nuint)(delegate* unmanaged<uint, byte, void>)&Write8Handler9,
            (nuint)(delegate* unmanaged<uint, byte, void>)&Write8Handler10,
            (nuint)(delegate* unmanaged<uint, byte, void>)&Write8Handler11,
            (nuint)(delegate* unmanaged<uint, byte, void>)&Write8Handler12,
            (nuint)(delegate* unmanaged<uint, byte, void>)&Write8Handler13,
            (nuint)(delegate* unmanaged<uint, byte, void>)&Write8Handler14,
            (nuint)(delegate* unmanaged<uint, byte, void>)&Write8Handler15,
            (nuint)(delegate* unmanaged<uint, byte, void>)&Write8Handler16,
            (nuint)(delegate* unmanaged<uint, byte, void>)&Write8Handler17,
            (nuint)(delegate* unmanaged<uint, byte, void>)&Write8Handler18,
            (nuint)(delegate* unmanaged<uint, byte, void>)&Write8Handler19,
            (nuint)(delegate* unmanaged<uint, byte, void>)&Write8Handler20,
            (nuint)(delegate* unmanaged<uint, byte, void>)&Write8Handler21,
            (nuint)(delegate* unmanaged<uint, byte, void>)&Write8Handler22,
            (nuint)(delegate* unmanaged<uint, byte, void>)&Write8Handler23,
            (nuint)(delegate* unmanaged<uint, byte, void>)&Write8Handler24,
            (nuint)(delegate* unmanaged<uint, byte, void>)&Write8Handler25,
            (nuint)(delegate* unmanaged<uint, byte, void>)&Write8Handler26,
            (nuint)(delegate* unmanaged<uint, byte, void>)&Write8Handler27,
            (nuint)(delegate* unmanaged<uint, byte, void>)&Write8Handler28,
            (nuint)(delegate* unmanaged<uint, byte, void>)&Write8Handler29,
            (nuint)(delegate* unmanaged<uint, byte, void>)&Write8Handler30,
            (nuint)(delegate* unmanaged<uint, byte, void>)&Write8Handler31
        };

    private static readonly nuint[] NativeWriteEntries16 = RuntimeInformation.ProcessArchitecture == Architecture.X86
        ? new nuint[]
        {
            (nuint)(delegate* unmanaged[Fastcall]<uint, ushort, void>)&Write16Handler0X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, ushort, void>)&Write16Handler1X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, ushort, void>)&Write16Handler2X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, ushort, void>)&Write16Handler3X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, ushort, void>)&Write16Handler4X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, ushort, void>)&Write16Handler5X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, ushort, void>)&Write16Handler6X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, ushort, void>)&Write16Handler7X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, ushort, void>)&Write16Handler8X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, ushort, void>)&Write16Handler9X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, ushort, void>)&Write16Handler10X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, ushort, void>)&Write16Handler11X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, ushort, void>)&Write16Handler12X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, ushort, void>)&Write16Handler13X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, ushort, void>)&Write16Handler14X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, ushort, void>)&Write16Handler15X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, ushort, void>)&Write16Handler16X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, ushort, void>)&Write16Handler17X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, ushort, void>)&Write16Handler18X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, ushort, void>)&Write16Handler19X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, ushort, void>)&Write16Handler20X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, ushort, void>)&Write16Handler21X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, ushort, void>)&Write16Handler22X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, ushort, void>)&Write16Handler23X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, ushort, void>)&Write16Handler24X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, ushort, void>)&Write16Handler25X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, ushort, void>)&Write16Handler26X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, ushort, void>)&Write16Handler27X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, ushort, void>)&Write16Handler28X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, ushort, void>)&Write16Handler29X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, ushort, void>)&Write16Handler30X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, ushort, void>)&Write16Handler31X86
        }
        : new nuint[]
        {
            (nuint)(delegate* unmanaged<uint, ushort, void>)&Write16Handler0,
            (nuint)(delegate* unmanaged<uint, ushort, void>)&Write16Handler1,
            (nuint)(delegate* unmanaged<uint, ushort, void>)&Write16Handler2,
            (nuint)(delegate* unmanaged<uint, ushort, void>)&Write16Handler3,
            (nuint)(delegate* unmanaged<uint, ushort, void>)&Write16Handler4,
            (nuint)(delegate* unmanaged<uint, ushort, void>)&Write16Handler5,
            (nuint)(delegate* unmanaged<uint, ushort, void>)&Write16Handler6,
            (nuint)(delegate* unmanaged<uint, ushort, void>)&Write16Handler7,
            (nuint)(delegate* unmanaged<uint, ushort, void>)&Write16Handler8,
            (nuint)(delegate* unmanaged<uint, ushort, void>)&Write16Handler9,
            (nuint)(delegate* unmanaged<uint, ushort, void>)&Write16Handler10,
            (nuint)(delegate* unmanaged<uint, ushort, void>)&Write16Handler11,
            (nuint)(delegate* unmanaged<uint, ushort, void>)&Write16Handler12,
            (nuint)(delegate* unmanaged<uint, ushort, void>)&Write16Handler13,
            (nuint)(delegate* unmanaged<uint, ushort, void>)&Write16Handler14,
            (nuint)(delegate* unmanaged<uint, ushort, void>)&Write16Handler15,
            (nuint)(delegate* unmanaged<uint, ushort, void>)&Write16Handler16,
            (nuint)(delegate* unmanaged<uint, ushort, void>)&Write16Handler17,
            (nuint)(delegate* unmanaged<uint, ushort, void>)&Write16Handler18,
            (nuint)(delegate* unmanaged<uint, ushort, void>)&Write16Handler19,
            (nuint)(delegate* unmanaged<uint, ushort, void>)&Write16Handler20,
            (nuint)(delegate* unmanaged<uint, ushort, void>)&Write16Handler21,
            (nuint)(delegate* unmanaged<uint, ushort, void>)&Write16Handler22,
            (nuint)(delegate* unmanaged<uint, ushort, void>)&Write16Handler23,
            (nuint)(delegate* unmanaged<uint, ushort, void>)&Write16Handler24,
            (nuint)(delegate* unmanaged<uint, ushort, void>)&Write16Handler25,
            (nuint)(delegate* unmanaged<uint, ushort, void>)&Write16Handler26,
            (nuint)(delegate* unmanaged<uint, ushort, void>)&Write16Handler27,
            (nuint)(delegate* unmanaged<uint, ushort, void>)&Write16Handler28,
            (nuint)(delegate* unmanaged<uint, ushort, void>)&Write16Handler29,
            (nuint)(delegate* unmanaged<uint, ushort, void>)&Write16Handler30,
            (nuint)(delegate* unmanaged<uint, ushort, void>)&Write16Handler31
        };

    private static readonly nuint[] NativeWriteEntries32 = RuntimeInformation.ProcessArchitecture == Architecture.X86
        ? new nuint[]
        {
            (nuint)(delegate* unmanaged[Fastcall]<uint, uint, void>)&Write32Handler0X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, uint, void>)&Write32Handler1X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, uint, void>)&Write32Handler2X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, uint, void>)&Write32Handler3X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, uint, void>)&Write32Handler4X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, uint, void>)&Write32Handler5X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, uint, void>)&Write32Handler6X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, uint, void>)&Write32Handler7X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, uint, void>)&Write32Handler8X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, uint, void>)&Write32Handler9X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, uint, void>)&Write32Handler10X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, uint, void>)&Write32Handler11X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, uint, void>)&Write32Handler12X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, uint, void>)&Write32Handler13X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, uint, void>)&Write32Handler14X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, uint, void>)&Write32Handler15X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, uint, void>)&Write32Handler16X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, uint, void>)&Write32Handler17X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, uint, void>)&Write32Handler18X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, uint, void>)&Write32Handler19X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, uint, void>)&Write32Handler20X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, uint, void>)&Write32Handler21X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, uint, void>)&Write32Handler22X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, uint, void>)&Write32Handler23X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, uint, void>)&Write32Handler24X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, uint, void>)&Write32Handler25X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, uint, void>)&Write32Handler26X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, uint, void>)&Write32Handler27X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, uint, void>)&Write32Handler28X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, uint, void>)&Write32Handler29X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, uint, void>)&Write32Handler30X86,
            (nuint)(delegate* unmanaged[Fastcall]<uint, uint, void>)&Write32Handler31X86
        }
        : new nuint[]
        {
            (nuint)(delegate* unmanaged<uint, uint, void>)&Write32Handler0,
            (nuint)(delegate* unmanaged<uint, uint, void>)&Write32Handler1,
            (nuint)(delegate* unmanaged<uint, uint, void>)&Write32Handler2,
            (nuint)(delegate* unmanaged<uint, uint, void>)&Write32Handler3,
            (nuint)(delegate* unmanaged<uint, uint, void>)&Write32Handler4,
            (nuint)(delegate* unmanaged<uint, uint, void>)&Write32Handler5,
            (nuint)(delegate* unmanaged<uint, uint, void>)&Write32Handler6,
            (nuint)(delegate* unmanaged<uint, uint, void>)&Write32Handler7,
            (nuint)(delegate* unmanaged<uint, uint, void>)&Write32Handler8,
            (nuint)(delegate* unmanaged<uint, uint, void>)&Write32Handler9,
            (nuint)(delegate* unmanaged<uint, uint, void>)&Write32Handler10,
            (nuint)(delegate* unmanaged<uint, uint, void>)&Write32Handler11,
            (nuint)(delegate* unmanaged<uint, uint, void>)&Write32Handler12,
            (nuint)(delegate* unmanaged<uint, uint, void>)&Write32Handler13,
            (nuint)(delegate* unmanaged<uint, uint, void>)&Write32Handler14,
            (nuint)(delegate* unmanaged<uint, uint, void>)&Write32Handler15,
            (nuint)(delegate* unmanaged<uint, uint, void>)&Write32Handler16,
            (nuint)(delegate* unmanaged<uint, uint, void>)&Write32Handler17,
            (nuint)(delegate* unmanaged<uint, uint, void>)&Write32Handler18,
            (nuint)(delegate* unmanaged<uint, uint, void>)&Write32Handler19,
            (nuint)(delegate* unmanaged<uint, uint, void>)&Write32Handler20,
            (nuint)(delegate* unmanaged<uint, uint, void>)&Write32Handler21,
            (nuint)(delegate* unmanaged<uint, uint, void>)&Write32Handler22,
            (nuint)(delegate* unmanaged<uint, uint, void>)&Write32Handler23,
            (nuint)(delegate* unmanaged<uint, uint, void>)&Write32Handler24,
            (nuint)(delegate* unmanaged<uint, uint, void>)&Write32Handler25,
            (nuint)(delegate* unmanaged<uint, uint, void>)&Write32Handler26,
            (nuint)(delegate* unmanaged<uint, uint, void>)&Write32Handler27,
            (nuint)(delegate* unmanaged<uint, uint, void>)&Write32Handler28,
            (nuint)(delegate* unmanaged<uint, uint, void>)&Write32Handler29,
            (nuint)(delegate* unmanaged<uint, uint, void>)&Write32Handler30,
            (nuint)(delegate* unmanaged<uint, uint, void>)&Write32Handler31
        };

    [UnmanagedCallersOnly]
    private static byte Read8Handler0(uint address) => ReadHandlers8[0]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static byte Read8Handler0X86(uint address) => ReadHandlers8[0]!(address);

    [UnmanagedCallersOnly]
    private static byte Read8Handler1(uint address) => ReadHandlers8[1]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static byte Read8Handler1X86(uint address) => ReadHandlers8[1]!(address);

    [UnmanagedCallersOnly]
    private static byte Read8Handler2(uint address) => ReadHandlers8[2]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static byte Read8Handler2X86(uint address) => ReadHandlers8[2]!(address);

    [UnmanagedCallersOnly]
    private static byte Read8Handler3(uint address) => ReadHandlers8[3]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static byte Read8Handler3X86(uint address) => ReadHandlers8[3]!(address);

    [UnmanagedCallersOnly]
    private static byte Read8Handler4(uint address) => ReadHandlers8[4]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static byte Read8Handler4X86(uint address) => ReadHandlers8[4]!(address);

    [UnmanagedCallersOnly]
    private static byte Read8Handler5(uint address) => ReadHandlers8[5]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static byte Read8Handler5X86(uint address) => ReadHandlers8[5]!(address);

    [UnmanagedCallersOnly]
    private static byte Read8Handler6(uint address) => ReadHandlers8[6]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static byte Read8Handler6X86(uint address) => ReadHandlers8[6]!(address);

    [UnmanagedCallersOnly]
    private static byte Read8Handler7(uint address) => ReadHandlers8[7]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static byte Read8Handler7X86(uint address) => ReadHandlers8[7]!(address);

    [UnmanagedCallersOnly]
    private static byte Read8Handler8(uint address) => ReadHandlers8[8]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static byte Read8Handler8X86(uint address) => ReadHandlers8[8]!(address);

    [UnmanagedCallersOnly]
    private static byte Read8Handler9(uint address) => ReadHandlers8[9]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static byte Read8Handler9X86(uint address) => ReadHandlers8[9]!(address);

    [UnmanagedCallersOnly]
    private static byte Read8Handler10(uint address) => ReadHandlers8[10]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static byte Read8Handler10X86(uint address) => ReadHandlers8[10]!(address);

    [UnmanagedCallersOnly]
    private static byte Read8Handler11(uint address) => ReadHandlers8[11]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static byte Read8Handler11X86(uint address) => ReadHandlers8[11]!(address);

    [UnmanagedCallersOnly]
    private static byte Read8Handler12(uint address) => ReadHandlers8[12]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static byte Read8Handler12X86(uint address) => ReadHandlers8[12]!(address);

    [UnmanagedCallersOnly]
    private static byte Read8Handler13(uint address) => ReadHandlers8[13]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static byte Read8Handler13X86(uint address) => ReadHandlers8[13]!(address);

    [UnmanagedCallersOnly]
    private static byte Read8Handler14(uint address) => ReadHandlers8[14]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static byte Read8Handler14X86(uint address) => ReadHandlers8[14]!(address);

    [UnmanagedCallersOnly]
    private static byte Read8Handler15(uint address) => ReadHandlers8[15]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static byte Read8Handler15X86(uint address) => ReadHandlers8[15]!(address);

    [UnmanagedCallersOnly]
    private static byte Read8Handler16(uint address) => ReadHandlers8[16]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static byte Read8Handler16X86(uint address) => ReadHandlers8[16]!(address);

    [UnmanagedCallersOnly]
    private static byte Read8Handler17(uint address) => ReadHandlers8[17]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static byte Read8Handler17X86(uint address) => ReadHandlers8[17]!(address);

    [UnmanagedCallersOnly]
    private static byte Read8Handler18(uint address) => ReadHandlers8[18]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static byte Read8Handler18X86(uint address) => ReadHandlers8[18]!(address);

    [UnmanagedCallersOnly]
    private static byte Read8Handler19(uint address) => ReadHandlers8[19]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static byte Read8Handler19X86(uint address) => ReadHandlers8[19]!(address);

    [UnmanagedCallersOnly]
    private static byte Read8Handler20(uint address) => ReadHandlers8[20]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static byte Read8Handler20X86(uint address) => ReadHandlers8[20]!(address);

    [UnmanagedCallersOnly]
    private static byte Read8Handler21(uint address) => ReadHandlers8[21]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static byte Read8Handler21X86(uint address) => ReadHandlers8[21]!(address);

    [UnmanagedCallersOnly]
    private static byte Read8Handler22(uint address) => ReadHandlers8[22]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static byte Read8Handler22X86(uint address) => ReadHandlers8[22]!(address);

    [UnmanagedCallersOnly]
    private static byte Read8Handler23(uint address) => ReadHandlers8[23]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static byte Read8Handler23X86(uint address) => ReadHandlers8[23]!(address);

    [UnmanagedCallersOnly]
    private static byte Read8Handler24(uint address) => ReadHandlers8[24]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static byte Read8Handler24X86(uint address) => ReadHandlers8[24]!(address);

    [UnmanagedCallersOnly]
    private static byte Read8Handler25(uint address) => ReadHandlers8[25]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static byte Read8Handler25X86(uint address) => ReadHandlers8[25]!(address);

    [UnmanagedCallersOnly]
    private static byte Read8Handler26(uint address) => ReadHandlers8[26]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static byte Read8Handler26X86(uint address) => ReadHandlers8[26]!(address);

    [UnmanagedCallersOnly]
    private static byte Read8Handler27(uint address) => ReadHandlers8[27]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static byte Read8Handler27X86(uint address) => ReadHandlers8[27]!(address);

    [UnmanagedCallersOnly]
    private static byte Read8Handler28(uint address) => ReadHandlers8[28]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static byte Read8Handler28X86(uint address) => ReadHandlers8[28]!(address);

    [UnmanagedCallersOnly]
    private static byte Read8Handler29(uint address) => ReadHandlers8[29]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static byte Read8Handler29X86(uint address) => ReadHandlers8[29]!(address);

    [UnmanagedCallersOnly]
    private static byte Read8Handler30(uint address) => ReadHandlers8[30]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static byte Read8Handler30X86(uint address) => ReadHandlers8[30]!(address);

    [UnmanagedCallersOnly]
    private static byte Read8Handler31(uint address) => ReadHandlers8[31]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static byte Read8Handler31X86(uint address) => ReadHandlers8[31]!(address);

    [UnmanagedCallersOnly]
    private static ushort Read16Handler0(uint address) => ReadHandlers16[0]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static ushort Read16Handler0X86(uint address) => ReadHandlers16[0]!(address);

    [UnmanagedCallersOnly]
    private static ushort Read16Handler1(uint address) => ReadHandlers16[1]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static ushort Read16Handler1X86(uint address) => ReadHandlers16[1]!(address);

    [UnmanagedCallersOnly]
    private static ushort Read16Handler2(uint address) => ReadHandlers16[2]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static ushort Read16Handler2X86(uint address) => ReadHandlers16[2]!(address);

    [UnmanagedCallersOnly]
    private static ushort Read16Handler3(uint address) => ReadHandlers16[3]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static ushort Read16Handler3X86(uint address) => ReadHandlers16[3]!(address);

    [UnmanagedCallersOnly]
    private static ushort Read16Handler4(uint address) => ReadHandlers16[4]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static ushort Read16Handler4X86(uint address) => ReadHandlers16[4]!(address);

    [UnmanagedCallersOnly]
    private static ushort Read16Handler5(uint address) => ReadHandlers16[5]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static ushort Read16Handler5X86(uint address) => ReadHandlers16[5]!(address);

    [UnmanagedCallersOnly]
    private static ushort Read16Handler6(uint address) => ReadHandlers16[6]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static ushort Read16Handler6X86(uint address) => ReadHandlers16[6]!(address);

    [UnmanagedCallersOnly]
    private static ushort Read16Handler7(uint address) => ReadHandlers16[7]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static ushort Read16Handler7X86(uint address) => ReadHandlers16[7]!(address);

    [UnmanagedCallersOnly]
    private static ushort Read16Handler8(uint address) => ReadHandlers16[8]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static ushort Read16Handler8X86(uint address) => ReadHandlers16[8]!(address);

    [UnmanagedCallersOnly]
    private static ushort Read16Handler9(uint address) => ReadHandlers16[9]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static ushort Read16Handler9X86(uint address) => ReadHandlers16[9]!(address);

    [UnmanagedCallersOnly]
    private static ushort Read16Handler10(uint address) => ReadHandlers16[10]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static ushort Read16Handler10X86(uint address) => ReadHandlers16[10]!(address);

    [UnmanagedCallersOnly]
    private static ushort Read16Handler11(uint address) => ReadHandlers16[11]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static ushort Read16Handler11X86(uint address) => ReadHandlers16[11]!(address);

    [UnmanagedCallersOnly]
    private static ushort Read16Handler12(uint address) => ReadHandlers16[12]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static ushort Read16Handler12X86(uint address) => ReadHandlers16[12]!(address);

    [UnmanagedCallersOnly]
    private static ushort Read16Handler13(uint address) => ReadHandlers16[13]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static ushort Read16Handler13X86(uint address) => ReadHandlers16[13]!(address);

    [UnmanagedCallersOnly]
    private static ushort Read16Handler14(uint address) => ReadHandlers16[14]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static ushort Read16Handler14X86(uint address) => ReadHandlers16[14]!(address);

    [UnmanagedCallersOnly]
    private static ushort Read16Handler15(uint address) => ReadHandlers16[15]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static ushort Read16Handler15X86(uint address) => ReadHandlers16[15]!(address);

    [UnmanagedCallersOnly]
    private static ushort Read16Handler16(uint address) => ReadHandlers16[16]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static ushort Read16Handler16X86(uint address) => ReadHandlers16[16]!(address);

    [UnmanagedCallersOnly]
    private static ushort Read16Handler17(uint address) => ReadHandlers16[17]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static ushort Read16Handler17X86(uint address) => ReadHandlers16[17]!(address);

    [UnmanagedCallersOnly]
    private static ushort Read16Handler18(uint address) => ReadHandlers16[18]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static ushort Read16Handler18X86(uint address) => ReadHandlers16[18]!(address);

    [UnmanagedCallersOnly]
    private static ushort Read16Handler19(uint address) => ReadHandlers16[19]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static ushort Read16Handler19X86(uint address) => ReadHandlers16[19]!(address);

    [UnmanagedCallersOnly]
    private static ushort Read16Handler20(uint address) => ReadHandlers16[20]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static ushort Read16Handler20X86(uint address) => ReadHandlers16[20]!(address);

    [UnmanagedCallersOnly]
    private static ushort Read16Handler21(uint address) => ReadHandlers16[21]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static ushort Read16Handler21X86(uint address) => ReadHandlers16[21]!(address);

    [UnmanagedCallersOnly]
    private static ushort Read16Handler22(uint address) => ReadHandlers16[22]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static ushort Read16Handler22X86(uint address) => ReadHandlers16[22]!(address);

    [UnmanagedCallersOnly]
    private static ushort Read16Handler23(uint address) => ReadHandlers16[23]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static ushort Read16Handler23X86(uint address) => ReadHandlers16[23]!(address);

    [UnmanagedCallersOnly]
    private static ushort Read16Handler24(uint address) => ReadHandlers16[24]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static ushort Read16Handler24X86(uint address) => ReadHandlers16[24]!(address);

    [UnmanagedCallersOnly]
    private static ushort Read16Handler25(uint address) => ReadHandlers16[25]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static ushort Read16Handler25X86(uint address) => ReadHandlers16[25]!(address);

    [UnmanagedCallersOnly]
    private static ushort Read16Handler26(uint address) => ReadHandlers16[26]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static ushort Read16Handler26X86(uint address) => ReadHandlers16[26]!(address);

    [UnmanagedCallersOnly]
    private static ushort Read16Handler27(uint address) => ReadHandlers16[27]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static ushort Read16Handler27X86(uint address) => ReadHandlers16[27]!(address);

    [UnmanagedCallersOnly]
    private static ushort Read16Handler28(uint address) => ReadHandlers16[28]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static ushort Read16Handler28X86(uint address) => ReadHandlers16[28]!(address);

    [UnmanagedCallersOnly]
    private static ushort Read16Handler29(uint address) => ReadHandlers16[29]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static ushort Read16Handler29X86(uint address) => ReadHandlers16[29]!(address);

    [UnmanagedCallersOnly]
    private static ushort Read16Handler30(uint address) => ReadHandlers16[30]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static ushort Read16Handler30X86(uint address) => ReadHandlers16[30]!(address);

    [UnmanagedCallersOnly]
    private static ushort Read16Handler31(uint address) => ReadHandlers16[31]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static ushort Read16Handler31X86(uint address) => ReadHandlers16[31]!(address);

    [UnmanagedCallersOnly]
    private static uint Read32Handler0(uint address) => ReadHandlers32[0]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static uint Read32Handler0X86(uint address) => ReadHandlers32[0]!(address);

    [UnmanagedCallersOnly]
    private static uint Read32Handler1(uint address) => ReadHandlers32[1]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static uint Read32Handler1X86(uint address) => ReadHandlers32[1]!(address);

    [UnmanagedCallersOnly]
    private static uint Read32Handler2(uint address) => ReadHandlers32[2]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static uint Read32Handler2X86(uint address) => ReadHandlers32[2]!(address);

    [UnmanagedCallersOnly]
    private static uint Read32Handler3(uint address) => ReadHandlers32[3]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static uint Read32Handler3X86(uint address) => ReadHandlers32[3]!(address);

    [UnmanagedCallersOnly]
    private static uint Read32Handler4(uint address) => ReadHandlers32[4]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static uint Read32Handler4X86(uint address) => ReadHandlers32[4]!(address);

    [UnmanagedCallersOnly]
    private static uint Read32Handler5(uint address) => ReadHandlers32[5]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static uint Read32Handler5X86(uint address) => ReadHandlers32[5]!(address);

    [UnmanagedCallersOnly]
    private static uint Read32Handler6(uint address) => ReadHandlers32[6]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static uint Read32Handler6X86(uint address) => ReadHandlers32[6]!(address);

    [UnmanagedCallersOnly]
    private static uint Read32Handler7(uint address) => ReadHandlers32[7]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static uint Read32Handler7X86(uint address) => ReadHandlers32[7]!(address);

    [UnmanagedCallersOnly]
    private static uint Read32Handler8(uint address) => ReadHandlers32[8]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static uint Read32Handler8X86(uint address) => ReadHandlers32[8]!(address);

    [UnmanagedCallersOnly]
    private static uint Read32Handler9(uint address) => ReadHandlers32[9]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static uint Read32Handler9X86(uint address) => ReadHandlers32[9]!(address);

    [UnmanagedCallersOnly]
    private static uint Read32Handler10(uint address) => ReadHandlers32[10]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static uint Read32Handler10X86(uint address) => ReadHandlers32[10]!(address);

    [UnmanagedCallersOnly]
    private static uint Read32Handler11(uint address) => ReadHandlers32[11]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static uint Read32Handler11X86(uint address) => ReadHandlers32[11]!(address);

    [UnmanagedCallersOnly]
    private static uint Read32Handler12(uint address) => ReadHandlers32[12]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static uint Read32Handler12X86(uint address) => ReadHandlers32[12]!(address);

    [UnmanagedCallersOnly]
    private static uint Read32Handler13(uint address) => ReadHandlers32[13]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static uint Read32Handler13X86(uint address) => ReadHandlers32[13]!(address);

    [UnmanagedCallersOnly]
    private static uint Read32Handler14(uint address) => ReadHandlers32[14]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static uint Read32Handler14X86(uint address) => ReadHandlers32[14]!(address);

    [UnmanagedCallersOnly]
    private static uint Read32Handler15(uint address) => ReadHandlers32[15]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static uint Read32Handler15X86(uint address) => ReadHandlers32[15]!(address);

    [UnmanagedCallersOnly]
    private static uint Read32Handler16(uint address) => ReadHandlers32[16]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static uint Read32Handler16X86(uint address) => ReadHandlers32[16]!(address);

    [UnmanagedCallersOnly]
    private static uint Read32Handler17(uint address) => ReadHandlers32[17]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static uint Read32Handler17X86(uint address) => ReadHandlers32[17]!(address);

    [UnmanagedCallersOnly]
    private static uint Read32Handler18(uint address) => ReadHandlers32[18]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static uint Read32Handler18X86(uint address) => ReadHandlers32[18]!(address);

    [UnmanagedCallersOnly]
    private static uint Read32Handler19(uint address) => ReadHandlers32[19]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static uint Read32Handler19X86(uint address) => ReadHandlers32[19]!(address);

    [UnmanagedCallersOnly]
    private static uint Read32Handler20(uint address) => ReadHandlers32[20]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static uint Read32Handler20X86(uint address) => ReadHandlers32[20]!(address);

    [UnmanagedCallersOnly]
    private static uint Read32Handler21(uint address) => ReadHandlers32[21]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static uint Read32Handler21X86(uint address) => ReadHandlers32[21]!(address);

    [UnmanagedCallersOnly]
    private static uint Read32Handler22(uint address) => ReadHandlers32[22]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static uint Read32Handler22X86(uint address) => ReadHandlers32[22]!(address);

    [UnmanagedCallersOnly]
    private static uint Read32Handler23(uint address) => ReadHandlers32[23]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static uint Read32Handler23X86(uint address) => ReadHandlers32[23]!(address);

    [UnmanagedCallersOnly]
    private static uint Read32Handler24(uint address) => ReadHandlers32[24]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static uint Read32Handler24X86(uint address) => ReadHandlers32[24]!(address);

    [UnmanagedCallersOnly]
    private static uint Read32Handler25(uint address) => ReadHandlers32[25]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static uint Read32Handler25X86(uint address) => ReadHandlers32[25]!(address);

    [UnmanagedCallersOnly]
    private static uint Read32Handler26(uint address) => ReadHandlers32[26]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static uint Read32Handler26X86(uint address) => ReadHandlers32[26]!(address);

    [UnmanagedCallersOnly]
    private static uint Read32Handler27(uint address) => ReadHandlers32[27]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static uint Read32Handler27X86(uint address) => ReadHandlers32[27]!(address);

    [UnmanagedCallersOnly]
    private static uint Read32Handler28(uint address) => ReadHandlers32[28]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static uint Read32Handler28X86(uint address) => ReadHandlers32[28]!(address);

    [UnmanagedCallersOnly]
    private static uint Read32Handler29(uint address) => ReadHandlers32[29]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static uint Read32Handler29X86(uint address) => ReadHandlers32[29]!(address);

    [UnmanagedCallersOnly]
    private static uint Read32Handler30(uint address) => ReadHandlers32[30]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static uint Read32Handler30X86(uint address) => ReadHandlers32[30]!(address);

    [UnmanagedCallersOnly]
    private static uint Read32Handler31(uint address) => ReadHandlers32[31]!(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static uint Read32Handler31X86(uint address) => ReadHandlers32[31]!(address);

    [UnmanagedCallersOnly]
    private static void Write8Handler0(uint address, byte data) => WriteHandlers8[0]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write8Handler0X86(uint address, byte data) => WriteHandlers8[0]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write8Handler1(uint address, byte data) => WriteHandlers8[1]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write8Handler1X86(uint address, byte data) => WriteHandlers8[1]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write8Handler2(uint address, byte data) => WriteHandlers8[2]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write8Handler2X86(uint address, byte data) => WriteHandlers8[2]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write8Handler3(uint address, byte data) => WriteHandlers8[3]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write8Handler3X86(uint address, byte data) => WriteHandlers8[3]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write8Handler4(uint address, byte data) => WriteHandlers8[4]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write8Handler4X86(uint address, byte data) => WriteHandlers8[4]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write8Handler5(uint address, byte data) => WriteHandlers8[5]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write8Handler5X86(uint address, byte data) => WriteHandlers8[5]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write8Handler6(uint address, byte data) => WriteHandlers8[6]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write8Handler6X86(uint address, byte data) => WriteHandlers8[6]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write8Handler7(uint address, byte data) => WriteHandlers8[7]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write8Handler7X86(uint address, byte data) => WriteHandlers8[7]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write8Handler8(uint address, byte data) => WriteHandlers8[8]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write8Handler8X86(uint address, byte data) => WriteHandlers8[8]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write8Handler9(uint address, byte data) => WriteHandlers8[9]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write8Handler9X86(uint address, byte data) => WriteHandlers8[9]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write8Handler10(uint address, byte data) => WriteHandlers8[10]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write8Handler10X86(uint address, byte data) => WriteHandlers8[10]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write8Handler11(uint address, byte data) => WriteHandlers8[11]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write8Handler11X86(uint address, byte data) => WriteHandlers8[11]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write8Handler12(uint address, byte data) => WriteHandlers8[12]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write8Handler12X86(uint address, byte data) => WriteHandlers8[12]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write8Handler13(uint address, byte data) => WriteHandlers8[13]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write8Handler13X86(uint address, byte data) => WriteHandlers8[13]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write8Handler14(uint address, byte data) => WriteHandlers8[14]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write8Handler14X86(uint address, byte data) => WriteHandlers8[14]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write8Handler15(uint address, byte data) => WriteHandlers8[15]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write8Handler15X86(uint address, byte data) => WriteHandlers8[15]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write8Handler16(uint address, byte data) => WriteHandlers8[16]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write8Handler16X86(uint address, byte data) => WriteHandlers8[16]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write8Handler17(uint address, byte data) => WriteHandlers8[17]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write8Handler17X86(uint address, byte data) => WriteHandlers8[17]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write8Handler18(uint address, byte data) => WriteHandlers8[18]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write8Handler18X86(uint address, byte data) => WriteHandlers8[18]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write8Handler19(uint address, byte data) => WriteHandlers8[19]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write8Handler19X86(uint address, byte data) => WriteHandlers8[19]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write8Handler20(uint address, byte data) => WriteHandlers8[20]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write8Handler20X86(uint address, byte data) => WriteHandlers8[20]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write8Handler21(uint address, byte data) => WriteHandlers8[21]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write8Handler21X86(uint address, byte data) => WriteHandlers8[21]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write8Handler22(uint address, byte data) => WriteHandlers8[22]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write8Handler22X86(uint address, byte data) => WriteHandlers8[22]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write8Handler23(uint address, byte data) => WriteHandlers8[23]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write8Handler23X86(uint address, byte data) => WriteHandlers8[23]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write8Handler24(uint address, byte data) => WriteHandlers8[24]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write8Handler24X86(uint address, byte data) => WriteHandlers8[24]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write8Handler25(uint address, byte data) => WriteHandlers8[25]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write8Handler25X86(uint address, byte data) => WriteHandlers8[25]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write8Handler26(uint address, byte data) => WriteHandlers8[26]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write8Handler26X86(uint address, byte data) => WriteHandlers8[26]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write8Handler27(uint address, byte data) => WriteHandlers8[27]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write8Handler27X86(uint address, byte data) => WriteHandlers8[27]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write8Handler28(uint address, byte data) => WriteHandlers8[28]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write8Handler28X86(uint address, byte data) => WriteHandlers8[28]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write8Handler29(uint address, byte data) => WriteHandlers8[29]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write8Handler29X86(uint address, byte data) => WriteHandlers8[29]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write8Handler30(uint address, byte data) => WriteHandlers8[30]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write8Handler30X86(uint address, byte data) => WriteHandlers8[30]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write8Handler31(uint address, byte data) => WriteHandlers8[31]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write8Handler31X86(uint address, byte data) => WriteHandlers8[31]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write16Handler0(uint address, ushort data) => WriteHandlers16[0]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write16Handler0X86(uint address, ushort data) => WriteHandlers16[0]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write16Handler1(uint address, ushort data) => WriteHandlers16[1]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write16Handler1X86(uint address, ushort data) => WriteHandlers16[1]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write16Handler2(uint address, ushort data) => WriteHandlers16[2]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write16Handler2X86(uint address, ushort data) => WriteHandlers16[2]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write16Handler3(uint address, ushort data) => WriteHandlers16[3]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write16Handler3X86(uint address, ushort data) => WriteHandlers16[3]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write16Handler4(uint address, ushort data) => WriteHandlers16[4]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write16Handler4X86(uint address, ushort data) => WriteHandlers16[4]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write16Handler5(uint address, ushort data) => WriteHandlers16[5]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write16Handler5X86(uint address, ushort data) => WriteHandlers16[5]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write16Handler6(uint address, ushort data) => WriteHandlers16[6]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write16Handler6X86(uint address, ushort data) => WriteHandlers16[6]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write16Handler7(uint address, ushort data) => WriteHandlers16[7]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write16Handler7X86(uint address, ushort data) => WriteHandlers16[7]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write16Handler8(uint address, ushort data) => WriteHandlers16[8]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write16Handler8X86(uint address, ushort data) => WriteHandlers16[8]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write16Handler9(uint address, ushort data) => WriteHandlers16[9]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write16Handler9X86(uint address, ushort data) => WriteHandlers16[9]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write16Handler10(uint address, ushort data) => WriteHandlers16[10]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write16Handler10X86(uint address, ushort data) => WriteHandlers16[10]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write16Handler11(uint address, ushort data) => WriteHandlers16[11]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write16Handler11X86(uint address, ushort data) => WriteHandlers16[11]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write16Handler12(uint address, ushort data) => WriteHandlers16[12]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write16Handler12X86(uint address, ushort data) => WriteHandlers16[12]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write16Handler13(uint address, ushort data) => WriteHandlers16[13]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write16Handler13X86(uint address, ushort data) => WriteHandlers16[13]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write16Handler14(uint address, ushort data) => WriteHandlers16[14]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write16Handler14X86(uint address, ushort data) => WriteHandlers16[14]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write16Handler15(uint address, ushort data) => WriteHandlers16[15]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write16Handler15X86(uint address, ushort data) => WriteHandlers16[15]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write16Handler16(uint address, ushort data) => WriteHandlers16[16]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write16Handler16X86(uint address, ushort data) => WriteHandlers16[16]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write16Handler17(uint address, ushort data) => WriteHandlers16[17]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write16Handler17X86(uint address, ushort data) => WriteHandlers16[17]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write16Handler18(uint address, ushort data) => WriteHandlers16[18]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write16Handler18X86(uint address, ushort data) => WriteHandlers16[18]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write16Handler19(uint address, ushort data) => WriteHandlers16[19]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write16Handler19X86(uint address, ushort data) => WriteHandlers16[19]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write16Handler20(uint address, ushort data) => WriteHandlers16[20]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write16Handler20X86(uint address, ushort data) => WriteHandlers16[20]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write16Handler21(uint address, ushort data) => WriteHandlers16[21]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write16Handler21X86(uint address, ushort data) => WriteHandlers16[21]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write16Handler22(uint address, ushort data) => WriteHandlers16[22]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write16Handler22X86(uint address, ushort data) => WriteHandlers16[22]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write16Handler23(uint address, ushort data) => WriteHandlers16[23]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write16Handler23X86(uint address, ushort data) => WriteHandlers16[23]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write16Handler24(uint address, ushort data) => WriteHandlers16[24]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write16Handler24X86(uint address, ushort data) => WriteHandlers16[24]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write16Handler25(uint address, ushort data) => WriteHandlers16[25]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write16Handler25X86(uint address, ushort data) => WriteHandlers16[25]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write16Handler26(uint address, ushort data) => WriteHandlers16[26]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write16Handler26X86(uint address, ushort data) => WriteHandlers16[26]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write16Handler27(uint address, ushort data) => WriteHandlers16[27]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write16Handler27X86(uint address, ushort data) => WriteHandlers16[27]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write16Handler28(uint address, ushort data) => WriteHandlers16[28]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write16Handler28X86(uint address, ushort data) => WriteHandlers16[28]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write16Handler29(uint address, ushort data) => WriteHandlers16[29]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write16Handler29X86(uint address, ushort data) => WriteHandlers16[29]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write16Handler30(uint address, ushort data) => WriteHandlers16[30]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write16Handler30X86(uint address, ushort data) => WriteHandlers16[30]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write16Handler31(uint address, ushort data) => WriteHandlers16[31]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write16Handler31X86(uint address, ushort data) => WriteHandlers16[31]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write32Handler0(uint address, uint data) => WriteHandlers32[0]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write32Handler0X86(uint address, uint data) => WriteHandlers32[0]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write32Handler1(uint address, uint data) => WriteHandlers32[1]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write32Handler1X86(uint address, uint data) => WriteHandlers32[1]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write32Handler2(uint address, uint data) => WriteHandlers32[2]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write32Handler2X86(uint address, uint data) => WriteHandlers32[2]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write32Handler3(uint address, uint data) => WriteHandlers32[3]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write32Handler3X86(uint address, uint data) => WriteHandlers32[3]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write32Handler4(uint address, uint data) => WriteHandlers32[4]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write32Handler4X86(uint address, uint data) => WriteHandlers32[4]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write32Handler5(uint address, uint data) => WriteHandlers32[5]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write32Handler5X86(uint address, uint data) => WriteHandlers32[5]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write32Handler6(uint address, uint data) => WriteHandlers32[6]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write32Handler6X86(uint address, uint data) => WriteHandlers32[6]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write32Handler7(uint address, uint data) => WriteHandlers32[7]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write32Handler7X86(uint address, uint data) => WriteHandlers32[7]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write32Handler8(uint address, uint data) => WriteHandlers32[8]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write32Handler8X86(uint address, uint data) => WriteHandlers32[8]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write32Handler9(uint address, uint data) => WriteHandlers32[9]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write32Handler9X86(uint address, uint data) => WriteHandlers32[9]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write32Handler10(uint address, uint data) => WriteHandlers32[10]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write32Handler10X86(uint address, uint data) => WriteHandlers32[10]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write32Handler11(uint address, uint data) => WriteHandlers32[11]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write32Handler11X86(uint address, uint data) => WriteHandlers32[11]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write32Handler12(uint address, uint data) => WriteHandlers32[12]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write32Handler12X86(uint address, uint data) => WriteHandlers32[12]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write32Handler13(uint address, uint data) => WriteHandlers32[13]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write32Handler13X86(uint address, uint data) => WriteHandlers32[13]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write32Handler14(uint address, uint data) => WriteHandlers32[14]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write32Handler14X86(uint address, uint data) => WriteHandlers32[14]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write32Handler15(uint address, uint data) => WriteHandlers32[15]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write32Handler15X86(uint address, uint data) => WriteHandlers32[15]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write32Handler16(uint address, uint data) => WriteHandlers32[16]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write32Handler16X86(uint address, uint data) => WriteHandlers32[16]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write32Handler17(uint address, uint data) => WriteHandlers32[17]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write32Handler17X86(uint address, uint data) => WriteHandlers32[17]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write32Handler18(uint address, uint data) => WriteHandlers32[18]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write32Handler18X86(uint address, uint data) => WriteHandlers32[18]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write32Handler19(uint address, uint data) => WriteHandlers32[19]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write32Handler19X86(uint address, uint data) => WriteHandlers32[19]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write32Handler20(uint address, uint data) => WriteHandlers32[20]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write32Handler20X86(uint address, uint data) => WriteHandlers32[20]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write32Handler21(uint address, uint data) => WriteHandlers32[21]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write32Handler21X86(uint address, uint data) => WriteHandlers32[21]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write32Handler22(uint address, uint data) => WriteHandlers32[22]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write32Handler22X86(uint address, uint data) => WriteHandlers32[22]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write32Handler23(uint address, uint data) => WriteHandlers32[23]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write32Handler23X86(uint address, uint data) => WriteHandlers32[23]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write32Handler24(uint address, uint data) => WriteHandlers32[24]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write32Handler24X86(uint address, uint data) => WriteHandlers32[24]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write32Handler25(uint address, uint data) => WriteHandlers32[25]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write32Handler25X86(uint address, uint data) => WriteHandlers32[25]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write32Handler26(uint address, uint data) => WriteHandlers32[26]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write32Handler26X86(uint address, uint data) => WriteHandlers32[26]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write32Handler27(uint address, uint data) => WriteHandlers32[27]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write32Handler27X86(uint address, uint data) => WriteHandlers32[27]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write32Handler28(uint address, uint data) => WriteHandlers32[28]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write32Handler28X86(uint address, uint data) => WriteHandlers32[28]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write32Handler29(uint address, uint data) => WriteHandlers32[29]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write32Handler29X86(uint address, uint data) => WriteHandlers32[29]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write32Handler30(uint address, uint data) => WriteHandlers32[30]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write32Handler30X86(uint address, uint data) => WriteHandlers32[30]!(address, data);

    [UnmanagedCallersOnly]
    private static void Write32Handler31(uint address, uint data) => WriteHandlers32[31]!(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void Write32Handler31X86(uint address, uint data) => WriteHandlers32[31]!(address, data);
}

#if !SH4_DYNAREC_NONE
internal sealed unsafe partial class Sh4RecompilerDriver
{
    internal static void FillAddressSpaceBlockPointers(Span<nuint> entries) => entries.Fill(_failedToFindBlock);
}
#endif
