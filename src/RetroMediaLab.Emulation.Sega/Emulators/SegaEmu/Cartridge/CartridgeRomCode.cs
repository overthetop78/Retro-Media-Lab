using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Cartridge;

/// <summary>Associates ROM backing arrays with their existing cartridge owner, without retaining dead media.</summary>
internal static class CartridgeRomCode
{
    private static readonly ConditionalWeakTable<byte[], CartridgeState> Owners = new();
    private static readonly ConditionalWeakTable<byte[], FirmwareBacking> Firmware = new();
    internal sealed class FirmwareBacking { internal bool Ready; internal long Revision; }

    internal static void Register(byte[] bytes, CartridgeState owner) => Owners.Add(bytes, owner);

    /// <summary>Invalidates before a firmware load, then publishes only an explicitly completed write.</summary>
    internal static FirmwareWrite BeginFirmwareWrite(Memory<byte> memory)
    {
        FirmwareBacking? backing = null;
        if (MemoryMarshal.TryGetArray((ReadOnlyMemory<byte>)memory, out ArraySegment<byte> segment)
            && segment.Array is not null && segment.Offset == 0 && segment.Count == segment.Array.Length)
        {
            backing = Firmware.GetValue(segment.Array, static _ => new());
            backing.Ready = false;
            System.Threading.Interlocked.Increment(ref backing.Revision);
        }
        CartridgeMemoryBank.NotifyCodeContentChanged();
        return new FirmwareWrite(backing);
    }

    internal sealed class FirmwareWrite : IDisposable
    {
        private readonly FirmwareBacking? _backing;
        private bool _complete, _disposed;
        internal FirmwareWrite(FirmwareBacking? backing) => _backing = backing;
        internal void Complete()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _complete = true;
        }
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (_backing is not null)
            {
                _backing.Ready = _complete;
                System.Threading.Interlocked.Increment(ref _backing.Revision);
            }
            CartridgeMemoryBank.NotifyCodeContentChanged();
        }
    }

    internal static bool TryGetStableBacking(ReadOnlyMemory<byte> window, out byte[]? backing)
    {
        backing = null;
        if (!MemoryMarshal.TryGetArray(window, out ArraySegment<byte> segment) || segment.Array is null) return false;
        bool cartridge = Owners.TryGetValue(segment.Array, out var owner) && owner.NativeRomCodeReadOnly
            && ReferenceEquals(owner.rom, segment.Array);
        bool firmware = Firmware.TryGetValue(segment.Array, out var loaded) && loaded.Ready;
        if (!cartridge && !firmware) return false;
        backing = segment.Array;
        return true;
    }

    internal static long ContentVersion(ReadOnlyMemory<byte> memory)
    {
        if (!MemoryMarshal.TryGetArray(memory, out ArraySegment<byte> segment) || segment.Array is null) return 0;
        if (Owners.TryGetValue(segment.Array, out var owner) && ReferenceEquals(owner.rom, segment.Array))
            return owner.NativeRomContentVersion;
        return Firmware.TryGetValue(segment.Array, out var loaded)
            ? System.Threading.Volatile.Read(ref loaded.Revision) : 0;
    }

    internal static void NotifyContentChanged(Memory<byte> memory)
    {
        if (MemoryMarshal.TryGetArray((ReadOnlyMemory<byte>)memory, out ArraySegment<byte> segment) && segment.Array is not null)
        {
            if (Owners.TryGetValue(segment.Array, out var owner) && ReferenceEquals(owner.rom, segment.Array))
            {
                owner.NotifyRomCodeContentChanged();
                return;
            }
            if (Firmware.TryGetValue(segment.Array, out var loaded))
                System.Threading.Interlocked.Increment(ref loaded.Revision);
        }
        CartridgeMemoryBank.NotifyCodeContentChanged();
    }
}
