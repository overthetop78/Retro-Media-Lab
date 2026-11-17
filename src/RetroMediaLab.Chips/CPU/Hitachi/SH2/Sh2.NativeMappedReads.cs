using RetroMediaLab.Chips.CPU.Hitachi.SH2.Contracts;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH2;

public partial class Sh2<TBus> where TBus : ISh2Bus
{
    private bool _useNativeMappedReads;
    private Sh2NativeReadMap _nativeReadMap;
    /// <summary>Select outside Execute. The bus owns and pins every page; unsupported buses retain their callbacks.</summary>
    public bool UseNativeMappedReads
    {
        get => _useNativeMappedReads;
        set
        {
            if (value == _useNativeMappedReads) return;
            if (_nativeExecutionDepth != 0) throw new InvalidOperationException("Change native memory policy outside CPU execution.");
            Sh2NativeReadMap map = value && _bus is ISh2NativeMappedBus provider ? provider.GetNativeReadMap() : default;
            if (!map.Pages.IsEmpty && map.Pages.Length < Sh2ReadPageMap.PageCount)
                throw new InvalidOperationException("Native SH-2 reads require the complete physical page table.");
            if (map.Control.IsEmpty != map.CachePolicy.IsEmpty || !map.Control.IsEmpty && (map.Control.Length < 0x93 || map.CachePolicy.Length < 1))
                throw new InvalidOperationException("Native SH-2 cache control and policy must describe the same controller.");
            ClearCodeCache(); _nativeReadMap = map; _useNativeMappedReads = value;
        }
    }
}
