using RetroMediaLab.Chips.CPU.Hitachi.SH2.Contracts;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH2;

public partial class Sh2<TBus> where TBus : ISh2Bus
{
    private bool _useNativeCachedReads;
    private Sh2NativeReadCache _nativeReadCache;
    /// <summary>Opt-in cache-hit reads; select on the owning thread outside Execute. Unsupported buses keep callbacks.</summary>
    public bool UseNativeCachedReads
    {
        get => _useNativeCachedReads;
        set
        {
            if (value == _useNativeCachedReads) return;
            var view = value && _deferClockUpdates && _bus is ISh2NativeReadCacheBus provider ? provider.GetNativeReadCache() : default;
            if (!view.Cache.IsEmpty && (view.Cache.Length < 4096 || view.Tags.Length < 256
                || view.Valid.Length < 256 || view.Control.Length < 512 || view.Policy.Length < 1))
                throw new InvalidOperationException("Native SH-2 reads require the complete SH7095 cache layout.");
            ClearCodeCache(); _nativeReadCache = view; _useNativeCachedReads = value;
        }
    }
    private unsafe void RunWithNativeReadCache(Compilation.Sh2X64Loop run, nint registers, nint context, nint services)
    {
        fixed (byte* cache = _nativeReadCache.Cache.Span)
        fixed (uint* tags = _nativeReadCache.Tags.Span)
        fixed (byte* valid = _nativeReadCache.Valid.Span)
        fixed (byte* control = _nativeReadCache.Control.Span)
        fixed (byte* policy = _nativeReadCache.Policy.Span)
        fixed (nint* pages = _nativeReadMap.Pages.Span)
        fixed (byte* mapControl = _nativeReadMap.Control.Span)
        fixed (byte* mapPolicy = _nativeReadMap.CachePolicy.Span)
        {
            FillNativeReadDescriptor(services, cache, tags, valid, control, policy);
            FillNativeMappedDescriptor(services, pages, mapControl, mapPolicy);
            run(registers, context, services);
        }
    }
    private unsafe void PayloadWithNativeReadCache(Compilation.Sh2X64Block block, Span<uint> state, nint services)
    {
        fixed (byte* cache = _nativeReadCache.Cache.Span)
        fixed (uint* tags = _nativeReadCache.Tags.Span)
        fixed (byte* valid = _nativeReadCache.Valid.Span)
        fixed (byte* control = _nativeReadCache.Control.Span)
        fixed (byte* policy = _nativeReadCache.Policy.Span)
        fixed (nint* pages = _nativeReadMap.Pages.Span)
        fixed (byte* mapControl = _nativeReadMap.Control.Span)
        fixed (byte* mapPolicy = _nativeReadMap.CachePolicy.Span)
        {
            FillNativeReadDescriptor(services, cache, tags, valid, control, policy);
            FillNativeMappedDescriptor(services, pages, mapControl, mapPolicy);
            block.Execute(R, state, services);
        }
    }
    private static unsafe void FillNativeReadDescriptor(nint services, byte* cache, uint* tags, byte* valid, byte* control, byte* policy)
    {
        nint* fields = (nint*)services;
        fields[2] = (nint)cache; fields[3] = (nint)tags; fields[4] = (nint)valid;
        fields[5] = (nint)control; fields[6] = (nint)policy;
    }
    private static unsafe void FillNativeMappedDescriptor(nint services, nint* pages, byte* control, byte* policy)
    {
        nint* fields = (nint*)services;
        fields[7] = (nint)pages; fields[8] = (nint)control; fields[9] = (nint)policy;
    }
}
