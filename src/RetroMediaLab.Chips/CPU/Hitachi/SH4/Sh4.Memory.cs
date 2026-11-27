using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

internal sealed partial class Sh4AddressSpace
{
    internal static readonly RamRegion MainRam = new();

    private static Func<uint, byte> _readMemory8 = null!;
    private static Func<uint, ushort> _readMemory16 = null!;
    private static Func<uint, ushort> _readInstruction16 = null!;
    private static Func<uint, uint> _readMemory32 = null!;
    private static Func<uint, ulong> _readMemory64 = null!;

    private static Action<uint, byte> _writeMemory8 = null!;
    private static Action<uint, ushort> _writeMemory16 = null!;
    private static Action<uint, uint> _writeMemory32 = null!;
    private static Action<uint, ulong> _writeMemory64 = null!;

    private static int _area1Video32;

    private void InitializeArea1Map()
    {
        _area1Video32 = RegisterHandler(
            Video.Read32BitPort<byte>, Video.Read32BitPort<ushort>, Video.Read32BitPort<uint>,
            Video.Write32BitPort<byte>, Video.Write32BitPort<ushort>, Video.Write32BitPort<uint>);
    }

    private void MapArea1(uint region)
    {
        MapBlock(VideoRamBuffer, 0x04 | region, 0x04 | region, VideoRamMask);
        MapHandler(_area1Video32, 0x05 | region, 0x05 | region);
        MirrorMapping(0x06 | region, 0x04 | region, 0x02);
    }

    private static void InitializeArea3Map()
    {
    }

    private unsafe void MapArea3(uint region)
    {
        MapBlockMirror((nuint)Unsafe.AsPointer(ref MainRam[0]), 0x0C | region, 0x0F | region, RamSize);
    }

    private static int _area4Lower;
    private static int _area4Upper;

    private void InitializeArea4Map()
    {
        _area4Lower = RegisterHandler(
            address => Video.ReadArea4<byte>(address, false),
            address => Video.ReadArea4<ushort>(address, false),
            address => Video.ReadArea4<uint>(address, false),
            (address, data) => Video.WriteArea4<byte>(address, data, false),
            (address, data) => Video.WriteArea4<ushort>(address, data, false),
            (address, data) => Video.WriteArea4<uint>(address, data, false));
        _area4Upper = RegisterHandler(
            address => Video.ReadArea4<byte>(address, true),
            address => Video.ReadArea4<ushort>(address, true),
            address => Video.ReadArea4<uint>(address, true),
            (address, data) => Video.WriteArea4<byte>(address, data, true),
            (address, data) => Video.WriteArea4<ushort>(address, data, true),
            (address, data) => Video.WriteArea4<uint>(address, data, true));
    }

    private void MapArea4(uint region)
    {
        MapHandler(_area4Lower, 0x11 | region, 0x11 | region);
        MapHandler(_area4Upper, 0x13 | region, 0x13 | region);
    }

    private static T ReadExternalDevice<T>(uint address) where T : unmanaged
    {
        Trace.TraceInformation($"Read ext. device (Area 5) undefined @ {address:x8}");
        return default;
    }

    private static void WriteExternalDevice<T>(uint address, T data) where T : unmanaged, IBinaryInteger<T>
    {
        Trace.TraceInformation($"Write ext. device (Area 5) undefined @ {address:x8}: {uint.CreateTruncating(data):x}");
    }

    internal static int Area5Handler;

    private void InitializeArea5Map()
    {
        Area5Handler = RegisterHandler(
            ReadExternalDevice<byte>, ReadExternalDevice<ushort>, ReadExternalDevice<uint>,
            WriteExternalDevice<byte>, WriteExternalDevice<ushort>, WriteExternalDevice<uint>);
    }

    private void MapArea5(uint region)
    {
        MapHandler(Area5Handler, region | 0x14, region | 0x17);
    }

    private static void InitializeArea6Map()
    {
    }

    private static void MapArea6(uint region)
    {
    }

    internal void MapDefaultMemory()
    {
        InitializeAddressMap();

        SystemBus.InitializeArea0Map();
        InitializeArea1Map();
        Elan.InitializeMemoryMap();
        InitializeArea3Map();
        InitializeArea4Map();
        InitializeArea5Map();
        InitializeArea6Map();

        for (uint i = 0; i < 8; i++)
        {
            SystemBus.MapArea0(i << 5);
            MapArea1(i << 5);
            Elan.MapMemory(i << 5);
            MapArea3(i << 5);
            MapArea4(i << 5);
            MapArea5(i << 5);
            MapArea6(i << 5);
        }

        MapInternalRam();
        MapP4();
    }

    internal void InitializeMemory()
    {
        SystemBus.InitializeArea0();
        InitializeMemoryMappedRegisters();
    }

    internal void ResetMemory(bool hard)
    {
        if (hard)
            MainRam.Zero();

        SystemBus.ResetArea0(hard);
        ResetMemoryMappedRegisters(hard);
    }

    internal void TerminateMemory()
    {
        try
        {
            TerminateMemoryMappedRegisters();
        }
        finally
        {
            try
            {
                SystemBus.TerminateArea0();
            }
            finally
            {
                TerminateAddressMap();
            }
        }
    }

    internal unsafe void CopyBlockWithoutMmu(uint destination, uint source, uint size)
    {
        void* destinationPointer = (void*)WriteConstant(destination, out bool destinationInMemory, 4);
        void* sourcePointer = (void*)ReadConstant(source, out bool sourceInMemory, 4);

        if (destinationInMemory && sourceInMemory)
        {
            NativeMemory.Copy(sourcePointer, destinationPointer, size);
        }
        else if (sourceInMemory)
        {
            WriteBlockWithoutMmu(destination, (uint*)sourcePointer, size);
        }
        else
        {
            Sh4HostRuntime.Verify(size % 4 == 0);
            for (uint i = 0; i < size; i += 4)
                Write32WithoutMmu(destination + i, Read32WithoutMmu(source + i));
        }
    }

    internal unsafe void WriteBlockWithoutMmu(uint destination, uint* source, uint size)
    {
        void* destinationPointer = (void*)WriteConstant(destination, out bool destinationInMemory, 4);

        if (destinationInMemory)
        {
            NativeMemory.Copy(source, destinationPointer, size);
        }
        else
        {
            for (uint i = 0; i < size;)
            {
                uint left = size - i;
                if (left >= 4)
                {
                    Write32WithoutMmu(destination + i, source[i >> 2]);
                    i += 4;
                }
                else if (left >= 2)
                {
                    Write16WithoutMmu(destination + i, ((ushort*)source)[i >> 1]);
                    i += 2;
                }
                else
                {
                    Write8WithoutMmu(destination + i, ((byte*)source)[i]);
                    i++;
                }
            }
        }
    }

    internal unsafe void WriteStoreQueueWithoutMmu(uint destination, ReadOnlySpan<byte> source)
    {
        byte* destinationPointer = GetMemoryPointer(destination, Sh4ContextLayout.StoreQueueSize);
        fixed (byte* sourcePointer = source)
        {
            if (destinationPointer != null)
            {
                NativeMemory.Copy(sourcePointer, destinationPointer, Sh4ContextLayout.StoreQueueSize);
            }
            else
            {
                for (uint i = 0; i < Sh4ContextLayout.StoreQueueSize; i += 4)
                    Write32WithoutMmu(destination + i, *(uint*)&sourcePointer[i]);
            }
        }
    }

    internal unsafe byte* GetMemoryPointer(uint address, uint size)
    {
        if (((address >> 29) & 7) == 7)
            return null;

        if (((address >> 26) & 7) == 3)
        {
            if (unchecked((address & RamMask) + size) > RamSize)
                return null;
            else
                return (byte*)Unsafe.AsPointer(ref MainRam[address & RamMask]);
        }

        return null;
    }

#if SH4_STRICT_MODE
    private static bool _interpreterMemoryActive;
#endif

    internal void SetMemoryHandlers()
    {
#if SH4_STRICT_MODE
        if (Configuration.DynarecEnabled && _interpreterMemoryActive)
        {
            OperandCache.WriteBackAll();
            InstructionCache.Invalidate();
        }

        if (!Configuration.DynarecEnabled)
        {
            _interpreterMemoryActive = true;
            _readInstruction16 = ReadCachedInstruction16;
            _readMemory8 = ReadCachedMemory<byte>;
            _readMemory16 = ReadCachedMemory<ushort>;
            _readMemory32 = ReadCachedMemory<uint>;
            _readMemory64 = ReadCachedMemory<ulong>;

            _writeMemory8 = WriteCachedMemory<byte>;
            _writeMemory16 = WriteCachedMemory<ushort>;
            _writeMemory32 = WriteCachedMemory<uint>;
            _writeMemory64 = WriteCachedMemory<ulong>;

            return;
        }
        _interpreterMemoryActive = false;
#endif
        if (Mmu.Enabled)
        {
            _readInstruction16 = Mmu.ReadInstruction16;
            _readMemory8 = Mmu.ReadMemory<byte>;
            _readMemory16 = Mmu.ReadMemory<ushort>;
            _readMemory32 = Mmu.ReadMemory<uint>;
            _readMemory64 = Mmu.ReadMemory<ulong>;

            _writeMemory8 = Mmu.WriteMemory<byte>;
            _writeMemory16 = Mmu.WriteMemory<ushort>;
            _writeMemory32 = Mmu.WriteMemory<uint>;
            _writeMemory64 = Mmu.WriteMemory<ulong>;
        }
        else
        {
            _readMemory8 = Read8;
            _readMemory16 = Read16;
            _readInstruction16 = Read16;
            _readMemory32 = Read32;
            _readMemory64 = Read64;

            _writeMemory8 = Write8;
            _writeMemory16 = Write16;
            _writeMemory32 = Write32;
            _writeMemory64 = Write64;
        }
    }

    internal unsafe Span<byte> GetMemory(uint address, uint size)
    {
        if (size > RamSize)
            return default;

        byte* pointer = GetMemoryPointer(address, size);
        return pointer == null ? default : new Span<byte>(pointer, checked((int)size));
    }

    internal bool MemoryHandlersInitialized => _readMemory8 is not null;
}
