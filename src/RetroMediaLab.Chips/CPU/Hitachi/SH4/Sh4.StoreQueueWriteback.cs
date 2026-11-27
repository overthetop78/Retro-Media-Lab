using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

internal sealed unsafe partial class Sh4AddressSpace
{
    private const uint MaximumRamMask = 32 * 1024 * 1024 - 1;
    private static readonly uint[] QueueAddressTranslation = new uint[2];

    private void WriteTranslatedStoreQueue(uint destination, Sh4Context context, bool mmuEnabled)
    {
        uint address;
        if (mmuEnabled)
        {
            address = Mmu.TranslateStoreQueueWriteBack(destination);
        }
        else
        {
            uint queueAddress = QueueAddressTranslation[0];
            address = unchecked(queueAddress + (destination & ~31u));
        }

        if (((address >> 26) & 7) != 4)
        {
            WriteStoreQueueWithoutMmu(address, GetStoreQueue(context, destination));
        }
        else
        {
            WriteTileAcceleratorStoreQueue(address, context);
        }
    }

    private static void WriteStoreQueueRamMapped(uint destination, Sh4Context context)
    {
        nuint ram = context.StoreQueueBufferEnd + 0x0C000000;
        uint index = (destination & MaximumRamMask) >> 5;
        nuint address = ram + index * Sh4ContextLayout.StoreQueueSize;
        GetStoreQueue(context, destination).CopyTo(new Span<byte>((void*)address, Sh4ContextLayout.StoreQueueSize));
    }

    private void WriteStoreQueueRamUnmapped(uint destination, Sh4Context context)
    {
        nuint address = RamBuffer + (destination & (RamMask - 31));
        GetStoreQueue(context, destination).CopyTo(new Span<byte>((void*)address, Sh4ContextLayout.StoreQueueSize));
    }

    private void WriteTileAcceleratorStoreQueue(uint address, Sh4Context context)
    {
        SystemBus.WriteTileAcceleratorStoreQueue(address, context.StoreQueues);
    }

    internal void SetStoreQueueWriteHandler()
    {
        Sh4Context context = RecompilerContext;
        bool x86 = RuntimeInformation.ProcessArchitecture == Architecture.X86;
        if ((CacheRegisters.MmuControl & 1) == 1)
        {
            context.ManagedStoreQueueWriter = &WriteStoreQueueMmu;
            context.StoreQueueWriter = x86
                ? (nuint)(delegate* unmanaged[Fastcall]<uint, nint, void>)&WriteStoreQueueMmuNativeX86
                : (nuint)(delegate* unmanaged<uint, nint, void>)&WriteStoreQueueMmuNative;
        }
        else
        {
            uint area = (CacheRegisters.QueueAddress0 >> 2) & 7;
            QueueAddressTranslation[0] = unchecked((area << 26) - 0xE0000000u);
            switch (area)
            {
                case 3:
                    if (VirtualMemoryEnabled)
                    {
                        context.ManagedStoreQueueWriter = &WriteStoreQueueRamMapped;
                        context.StoreQueueWriter = x86
                            ? (nuint)(delegate* unmanaged[Fastcall]<uint, nint, void>)&WriteStoreQueueMappedNativeX86
                            : (nuint)(delegate* unmanaged<uint, nint, void>)&WriteStoreQueueMappedNative;
                    }
                    else
                    {
                        context.ManagedStoreQueueWriter = &WriteStoreQueueUnmapped;
                        context.StoreQueueWriter = x86
                            ? (nuint)(delegate* unmanaged[Fastcall]<uint, nint, void>)&WriteStoreQueueUnmappedNativeX86
                            : (nuint)(delegate* unmanaged<uint, nint, void>)&WriteStoreQueueUnmappedNative;
                    }
                    break;
                case 4:
                    context.ManagedStoreQueueWriter = &WriteStoreQueueTa;
                    context.StoreQueueWriter = x86
                        ? (nuint)(delegate* unmanaged[Fastcall]<uint, nint, void>)&WriteStoreQueueTaNativeX86
                        : (nuint)(delegate* unmanaged<uint, nint, void>)&WriteStoreQueueTaNative;
                    break;
                default:
                    context.ManagedStoreQueueWriter = &WriteStoreQueueNoMmu;
                    context.StoreQueueWriter = x86
                        ? (nuint)(delegate* unmanaged[Fastcall]<uint, nint, void>)&WriteStoreQueueNoMmuNativeX86
                        : (nuint)(delegate* unmanaged<uint, nint, void>)&WriteStoreQueueNoMmuNative;
                    break;
            }
        }
    }

    private static Span<byte> GetStoreQueue(Sh4Context context, uint address)
    {
        int index = (int)((address >> 5) & 1);
        return context.StoreQueues.Slice(index * Sh4ContextLayout.StoreQueueSize, Sh4ContextLayout.StoreQueueSize);
    }

    private static void WriteStoreQueueMmu(uint address, Sh4Context context) =>
        context.AddressSpace.WriteTranslatedStoreQueue(address, context, mmuEnabled: true);

    private static void WriteStoreQueueNoMmu(uint address, Sh4Context context) =>
        context.AddressSpace.WriteTranslatedStoreQueue(address, context, mmuEnabled: false);

    private static void WriteStoreQueueUnmapped(uint address, Sh4Context context) =>
        context.AddressSpace.WriteStoreQueueRamUnmapped(address, context);

    private static void WriteStoreQueueTa(uint address, Sh4Context context) =>
        context.AddressSpace.WriteTileAcceleratorStoreQueue(address, context);

    [UnmanagedCallersOnly]
    private static void WriteStoreQueueMmuNative(uint address, nint context) =>
        WriteStoreQueueMmu(address, Sh4Context.FromNative(context));

    [UnmanagedCallersOnly]
    private static void WriteStoreQueueNoMmuNative(uint address, nint context) =>
        WriteStoreQueueNoMmu(address, Sh4Context.FromNative(context));

    [UnmanagedCallersOnly]
    private static void WriteStoreQueueMappedNative(uint address, nint context) =>
        WriteStoreQueueRamMapped(address, Sh4Context.FromNative(context));

    [UnmanagedCallersOnly]
    private static void WriteStoreQueueUnmappedNative(uint address, nint context) =>
        WriteStoreQueueUnmapped(address, Sh4Context.FromNative(context));

    [UnmanagedCallersOnly]
    private static void WriteStoreQueueTaNative(uint address, nint context) =>
        WriteStoreQueueTa(address, Sh4Context.FromNative(context));

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void WriteStoreQueueMmuNativeX86(uint address, nint context) =>
        WriteStoreQueueMmu(address, Sh4Context.FromNative(context));

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void WriteStoreQueueMappedNativeX86(uint address, nint context) =>
        WriteStoreQueueRamMapped(address, Sh4Context.FromNative(context));

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void WriteStoreQueueUnmappedNativeX86(uint address, nint context) =>
        WriteStoreQueueUnmapped(address, Sh4Context.FromNative(context));

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void WriteStoreQueueTaNativeX86(uint address, nint context) =>
        WriteStoreQueueTa(address, Sh4Context.FromNative(context));

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void WriteStoreQueueNoMmuNativeX86(uint address, nint context) =>
        WriteStoreQueueNoMmu(address, Sh4Context.FromNative(context));
}
