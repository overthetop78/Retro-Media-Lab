namespace RetroMediaLab.Chips.CPU.Hitachi.SH2.Contracts;

/// <summary>Borrowed stable arrays matching the SH7095 4-way, 64-set, 16-byte cache.</summary>
/// <remarks>Only existing hits in cache RAM, low ROM, cartridge ROM and SDRAM may bypass
/// the bus. Those reads have zero wait and no bus/polling/LRU side effects. The arrays
/// must remain the same for this CPU's lifetime; their contents remain live. Policy[0]
/// is nonzero only while this layout applies. The CPU pins each array during Execute.</remarks>
public readonly record struct Sh2NativeReadCache(ReadOnlyMemory<byte> Cache, ReadOnlyMemory<uint> Tags,
    ReadOnlyMemory<byte> Valid, ReadOnlyMemory<byte> Control, ReadOnlyMemory<byte> Policy);

public interface ISh2NativeReadCacheBus
{
    Sh2NativeReadCache GetNativeReadCache();
}
