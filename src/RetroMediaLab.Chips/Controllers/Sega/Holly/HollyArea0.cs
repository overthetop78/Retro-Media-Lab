using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using RetroMediaLab.Chips.Audio.Yamaha.AICA;
using RetroMediaLab.Chips.CPU.Hitachi.SH4;
using RetroMediaLab.Chips.Video.NEC.PowerVr2;

namespace RetroMediaLab.Chips.Dreamcast.Holly;

internal sealed unsafe partial class HollySystemBus
{
    private static int _area0Handler;
    private static int _area0MirrorHandler;

    private static T ReadArea0<T>(uint physicalAddress, Sh4PlatformSystem system, bool mirror)
        where T : unmanaged, IBinaryInteger<T>
    {
        uint size = (uint)sizeof(T);
        uint address = physicalAddress & 0x01ffffff;
        uint area = address >> 21;

        switch (area)
        {
            case 0:
                if (address < (system == Sh4PlatformSystem.Atomiswave ? 0x20000u : 0x200000u))
                {
                    if (mirror)
                    {
                        Trace.TraceInformation($"Read from area0 BIOS mirror [Unassigned], addr={address:x}");
                        return T.Zero;
                    }
                    return T.CreateTruncating(DreamcastNvMemory.ReadBios(address, size));
                }
                break;

            case 1:
                if (system != Sh4PlatformSystem.SystemSp && address < 0x00200000 + Sh4DreamcastRuntime.Platform.FlashSize)
                {
                    if (mirror)
                    {
                        Trace.TraceInformation($"Read from area0 Flash mirror [Unassigned], addr={address:x}");
                        return T.Zero;
                    }
                    return T.CreateTruncating(DreamcastNvMemory.ReadFlash(address, size));
                }
                break;

            case 2:
                if (address >= 0x005f7000 && address <= 0x005f70ff)
                {
                    if (system == Sh4PlatformSystem.Dreamcast)
                        return T.CreateTruncating(GdRomInterface.ReadMem(address, size));
                    return T.CreateTruncating(NaomiInterface.ReadMem(address, size));
                }
                if (address >= 0x005f6800 && address <= 0x005f7cff)
                    return T.CreateTruncating(Read(physicalAddress));
                if (address >= 0x005f8000 && address <= 0x005f9fff)
                {
                    if (size != 4)
                        return T.Zero;
                    return T.CreateTruncating(PvrMemory.ReadRegister(physicalAddress));
                }
                break;

            case 3:
                if (address <= 0x006007ff)
                {
                    if (system == Sh4PlatformSystem.Dreamcast)
                    {
                        if (!Sh4Configuration.EmulateBba)
                            return T.CreateTruncating(ModemInterface.ReadMemArea0_006(address, size));
                        return T.Zero;
                    }
                    if (system == Sh4PlatformSystem.Atomiswave)
                        return T.CreateTruncating(AtomiswaveInterface.ReadMemArea0_006(address, size));
                }
                if (address >= 0x00700000 && address <= 0x00707fff)
                    return AicaInterface.ReadAicaRegister<T>(address);
                if (address >= 0x00710000 && address <= 0x0071000b)
                    return AicaInterface.ReadRtcRegister<T>(address);
                break;

            case 4:
            case 5:
            case 6:
            case 7:
                return Unsafe.ReadUnaligned<T>(AicaInterface.Ram + (address & Sh4DreamcastRuntime.Platform.AramMask & ~(size - 1)));

            default:
                if (system is Sh4PlatformSystem.Naomi or Sh4PlatformSystem.Naomi2)
                    return T.CreateTruncating(NaomiInterface.ReadG2ExternalMem(address, size));
                if (system == Sh4PlatformSystem.SystemSp)
                    return SystemSpInterface.ReadMemArea0<T>(address);
                if (Sh4Configuration.EmulateBba)
                    return T.CreateTruncating(BroadbandAdapter.ReadMem(address, size));
                return T.Zero;
        }
        Trace.TraceInformation($"Read from area0<{size}> not implemented [Unassigned], addr={address:x}");
        return T.Zero;
    }

    private static void WriteArea0<T>(uint physicalAddress, T data, Sh4PlatformSystem system, bool mirror)
        where T : unmanaged, IBinaryInteger<T>
    {
        uint size = (uint)sizeof(T);
        uint address = physicalAddress & 0x01ffffff;
        uint area = address >> 21;
        uint value = uint.CreateTruncating(data);

        switch (area)
        {
            case 0:
                if (!mirror)
                {
                    if (system == Sh4PlatformSystem.Atomiswave)
                    {
                        if (address < 0x20000)
                        {
                            DreamcastNvMemory.WriteAwBios(address, value, size);
                            return;
                        }
                    }
                    else if (address < 0x200000)
                    {
                        Trace.TraceInformation($"Write to [Boot ROM] is not possible, addr={address:x}, data={value:x}, size={size}");
                        return;
                    }
                }
                break;

            case 1:
                if (!mirror && address < 0x00200000 + Sh4DreamcastRuntime.Platform.FlashSize)
                {
                    DreamcastNvMemory.WriteFlash(address, value, size);
                    return;
                }
                break;

            case 2:
                if (address >= 0x005f7000 && address <= 0x005f70ff)
                {
                    if (system == Sh4PlatformSystem.Dreamcast)
                        GdRomInterface.WriteMem(address, value, size);
                    else
                        NaomiInterface.WriteMem(address, value, size);
                    return;
                }
                if (address >= 0x005f6800 && address <= 0x005f7cff)
                {
                    Write(physicalAddress, value);
                    return;
                }
                if (address >= 0x005f8000 && address <= 0x005f9fff && size == 4)
                {
                    PvrMemory.WriteRegister(physicalAddress, value);
                    return;
                }
                break;

            case 3:
                if (address <= 0x006007ff)
                {
                    if (system == Sh4PlatformSystem.Dreamcast)
                    {
                        if (!Sh4Configuration.EmulateBba)
                            ModemInterface.WriteMemArea0_006(address, value, size);
                        return;
                    }
                    if (system == Sh4PlatformSystem.Atomiswave)
                    {
                        AtomiswaveInterface.WriteMemArea0_006(address, value, size);
                        return;
                    }
                }
                if (address >= 0x00700000 && address <= 0x00707fff)
                {
                    AicaInterface.WriteAicaRegister(address, data);
                    return;
                }
                if (address >= 0x00710000 && address <= 0x0071000b)
                {
                    AicaInterface.WriteRtcRegister(address, data);
                    return;
                }
                break;

            case 4:
            case 5:
            case 6:
            case 7:
                Unsafe.WriteUnaligned(AicaInterface.Ram + (address & Sh4DreamcastRuntime.Platform.AramMask & ~(size - 1)), data);
                return;

            default:
                if (system is Sh4PlatformSystem.Naomi or Sh4PlatformSystem.Naomi2)
                    NaomiInterface.WriteG2ExternalMem(address, value, size);
                else if (system == Sh4PlatformSystem.SystemSp)
                    SystemSpInterface.WriteMemArea0(address, data);
                else if (Sh4Configuration.EmulateBba)
                    BroadbandAdapter.WriteMem(address, value, size);
                return;
        }
        Trace.TraceInformation($"Write to area0_32 not implemented [Unassigned], addr={address:x},data={value:x},size={size}");
    }

    internal partial void InitializeArea0()
    {
        Init();
        DreamcastNvMemory.Init();
    }

    internal partial void ResetArea0(bool hard)
    {
        if (hard)
        {
            DreamcastNvMemory.Term();
            DreamcastNvMemory.Init();
        }
        else
        {
            DreamcastNvMemory.Reset();
        }
        Reset(hard);
    }

    internal partial void TerminateArea0()
    {
        try
        {
            DreamcastNvMemory.Term();
        }
        finally
        {
            Term();
        }
    }

    internal partial void InitializeArea0Map()
    {
        switch (Sh4DreamcastRuntime.Platform.System)
        {
            default:
            case Sh4PlatformSystem.Dreamcast:
                _area0Handler = RegisterArea0Handler(Sh4PlatformSystem.Dreamcast, false);
                _area0MirrorHandler = RegisterArea0Handler(Sh4PlatformSystem.Dreamcast, true);
                break;

            case Sh4PlatformSystem.Naomi:
                _area0Handler = RegisterArea0Handler(Sh4PlatformSystem.Naomi, false);
                _area0MirrorHandler = RegisterArea0Handler(Sh4PlatformSystem.Naomi, true);
                break;

            case Sh4PlatformSystem.Naomi2:
                _area0Handler = RegisterArea0Handler(Sh4PlatformSystem.Naomi2, false);
                _area0MirrorHandler = RegisterArea0Handler(Sh4PlatformSystem.Naomi2, true);
                break;

            case Sh4PlatformSystem.Atomiswave:
                _area0Handler = RegisterArea0Handler(Sh4PlatformSystem.Atomiswave, false);
                _area0MirrorHandler = RegisterArea0Handler(Sh4PlatformSystem.Atomiswave, true);
                break;

            case Sh4PlatformSystem.SystemSp:
                _area0Handler = RegisterArea0Handler(Sh4PlatformSystem.SystemSp, false);
                _area0MirrorHandler = RegisterArea0Handler(Sh4PlatformSystem.SystemSp, true);
                break;
        }
    }

    private static int RegisterArea0Handler(Sh4PlatformSystem system, bool mirror)
    {
        return Sh4DreamcastRuntime.AddressSpace.RegisterHandler(
            address => ReadArea0<byte>(address, system, mirror),
            address => ReadArea0<ushort>(address, system, mirror),
            address => ReadArea0<uint>(address, system, mirror),
            (address, data) => WriteArea0(address, data, system, mirror),
            (address, data) => WriteArea0(address, data, system, mirror),
            (address, data) => WriteArea0(address, data, system, mirror));
    }

    internal partial void MapArea0(uint baseAddress)
    {
        Sh4AddressSpace addressSpace = Sh4DreamcastRuntime.AddressSpace;
        addressSpace.MapHandler(_area0Handler, 0x00 | baseAddress, 0x01 | baseAddress);
        addressSpace.MapHandler(_area0MirrorHandler, 0x02 | baseAddress, 0x03 | baseAddress);
    }
}
