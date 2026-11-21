namespace RetroMediaLab.Chips.CPU.Hitachi.SH2.Contracts;

/// <summary>Borrowed 4 KiB physical read pages, modelled after Yabause's memory_map.</summary>
/// <remarks>
/// Nonzero entries point to complete, live, big-endian byte pages. Reads have
/// zero wait and no device, polling or clock side effects. The owner pins their
/// backing stores and serializes mapping changes with CPU execution. Writes
/// continue through the bus and its code-invalidation barriers.
/// An SH7095 supplies its live CCR register file and cache policy; a bare SH-2
/// can leave both empty. P0 cacheable accesses must still use the cache while
/// enabled; P1 accesses can use the physical table directly.
/// </remarks>
public readonly record struct Sh2NativeReadMap(ReadOnlyMemory<nint> Pages,
    ReadOnlyMemory<byte> Control = default, ReadOnlyMemory<byte> CachePolicy = default);

public interface ISh2NativeMappedBus
{
    Sh2NativeReadMap GetNativeReadMap();
}
