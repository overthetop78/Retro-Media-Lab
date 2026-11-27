using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

internal sealed partial class Sh4AddressSpace
{
    internal unsafe nuint RamBuffer => (nuint)Unsafe.AsPointer(ref MainRam[0]);

    internal Func<uint, byte> ReadGuest8 => _readMemory8;
    internal Func<uint, ushort> ReadGuest16 => _readMemory16;
    internal Func<uint, ushort> ReadGuestInstruction => _readInstruction16;
    internal Func<uint, uint> ReadGuest32 => _readMemory32;
    internal Func<uint, ulong> ReadGuest64 => _readMemory64;

    internal Action<uint, byte> WriteGuest8 => _writeMemory8;
    internal Action<uint, ushort> WriteGuest16 => _writeMemory16;
    internal Action<uint, uint> WriteGuest32 => _writeMemory32;
    internal Action<uint, ulong> WriteGuest64 => _writeMemory64;

    internal byte Read8WithoutMmu(uint address) => Read8(address);
    internal ushort ReadMemory16WithoutMmu(uint address) => Read16(address);
    internal uint Read32WithoutMmu(uint address) => Read32(address);

    internal void Write8WithoutMmu(uint address, byte data) => Write8(address, data);
    internal void Write16WithoutMmu(uint address, ushort data) => Write16(address, data);
    internal void Write32WithoutMmu(uint address, uint data) => Write32(address, data);

#if !SH4_DYNAREC_NONE
    internal static unsafe nuint Read8Entry => RuntimeInformation.ProcessArchitecture == Architecture.X86
        ? (nuint)(delegate* unmanaged[Fastcall]<uint, byte>)&NativeRead8X86
        : (nuint)(delegate* unmanaged<uint, byte>)&NativeRead8;

    internal static unsafe nuint Read16Entry => RuntimeInformation.ProcessArchitecture == Architecture.X86
        ? (nuint)(delegate* unmanaged[Fastcall]<uint, ushort>)&NativeRead16X86
        : (nuint)(delegate* unmanaged<uint, ushort>)&NativeRead16;

    internal static unsafe nuint Read32Entry => RuntimeInformation.ProcessArchitecture == Architecture.X86
        ? (nuint)(delegate* unmanaged[Fastcall]<uint, uint>)&NativeRead32X86
        : (nuint)(delegate* unmanaged<uint, uint>)&NativeRead32;

    internal static unsafe nuint Read64Entry => RuntimeInformation.ProcessArchitecture == Architecture.X86
        ? (nuint)(delegate* unmanaged[Fastcall]<uint, ulong>)&NativeRead64X86
        : (nuint)(delegate* unmanaged<uint, ulong>)&NativeRead64;

    internal static unsafe nuint Write8Entry => RuntimeInformation.ProcessArchitecture == Architecture.X86
        ? (nuint)(delegate* unmanaged[Fastcall]<uint, byte, void>)&NativeWrite8X86
        : (nuint)(delegate* unmanaged<uint, byte, void>)&NativeWrite8;

    internal static unsafe nuint Write16Entry => RuntimeInformation.ProcessArchitecture == Architecture.X86
        ? (nuint)(delegate* unmanaged[Fastcall]<uint, ushort, void>)&NativeWrite16X86
        : (nuint)(delegate* unmanaged<uint, ushort, void>)&NativeWrite16;

    internal static unsafe nuint Write32Entry => RuntimeInformation.ProcessArchitecture == Architecture.X86
        ? (nuint)(delegate* unmanaged[Fastcall]<uint, uint, void>)&NativeWrite32X86
        : (nuint)(delegate* unmanaged<uint, uint, void>)&NativeWrite32;

    internal static unsafe nuint Write64Entry => RuntimeInformation.ProcessArchitecture == Architecture.X86
        ? (nuint)(delegate* unmanaged[Fastcall]<uint, ulong, void>)&NativeWrite64X86
        : (nuint)(delegate* unmanaged<uint, ulong, void>)&NativeWrite64;

    [UnmanagedCallersOnly]
    private static byte NativeRead8(uint address) =>
        Sh4RecompilerDriver.Current.AddressSpace.Read8(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static byte NativeRead8X86(uint address) =>
        Sh4RecompilerDriver.Current.AddressSpace.Read8(address);

    [UnmanagedCallersOnly]
    private static ushort NativeRead16(uint address) =>
        Sh4RecompilerDriver.Current.AddressSpace.Read16(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static ushort NativeRead16X86(uint address) =>
        Sh4RecompilerDriver.Current.AddressSpace.Read16(address);

    [UnmanagedCallersOnly]
    private static uint NativeRead32(uint address) =>
        Sh4RecompilerDriver.Current.AddressSpace.Read32(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static uint NativeRead32X86(uint address) =>
        Sh4RecompilerDriver.Current.AddressSpace.Read32(address);

    [UnmanagedCallersOnly]
    private static ulong NativeRead64(uint address) =>
        Sh4RecompilerDriver.Current.AddressSpace.Read64(address);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static ulong NativeRead64X86(uint address) =>
        Sh4RecompilerDriver.Current.AddressSpace.Read64(address);

    [UnmanagedCallersOnly]
    private static void NativeWrite8(uint address, byte data) =>
        Sh4RecompilerDriver.Current.AddressSpace.Write8(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void NativeWrite8X86(uint address, byte data) =>
        Sh4RecompilerDriver.Current.AddressSpace.Write8(address, data);

    [UnmanagedCallersOnly]
    private static void NativeWrite16(uint address, ushort data) =>
        Sh4RecompilerDriver.Current.AddressSpace.Write16(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void NativeWrite16X86(uint address, ushort data) =>
        Sh4RecompilerDriver.Current.AddressSpace.Write16(address, data);

    [UnmanagedCallersOnly]
    private static void NativeWrite32(uint address, uint data) =>
        Sh4RecompilerDriver.Current.AddressSpace.Write32(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void NativeWrite32X86(uint address, uint data) =>
        Sh4RecompilerDriver.Current.AddressSpace.Write32(address, data);

    [UnmanagedCallersOnly]
    private static void NativeWrite64(uint address, ulong data) =>
        Sh4RecompilerDriver.Current.AddressSpace.Write64(address, data);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void NativeWrite64X86(uint address, ulong data) =>
        Sh4RecompilerDriver.Current.AddressSpace.Write64(address, data);
#endif

    internal static bool IsOnRam(uint address)
    {
        return ((address >> 26) & 7) == 3 && ((address >> 29) & 7) != 7;
    }
}
