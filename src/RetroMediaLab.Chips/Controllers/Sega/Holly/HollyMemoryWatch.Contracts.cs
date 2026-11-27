using RetroMediaLab.Chips.Audio.Yamaha.AICA;
using RetroMediaLab.Chips.Video.NEC.PowerVr2;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

internal static unsafe partial class Sh4MemoryWatch
{
    private static uint PageSize => Sh4DreamcastRuntime.AddressSpace.PageSize;
    private static uint PageMask => Sh4DreamcastRuntime.AddressSpace.PageMask;

    internal sealed class Page
    {
        internal readonly byte[] Data = new byte[(int)PageSize];
    }

    internal abstract class Watcher
    {
        private bool _started;
        private Dictionary<uint, Page> _pages = [];

        internal void Protect()
        {
            if (!_started)
            {
                ProtectMemory(0, uint.MaxValue);
                _started = true;
            }
            else
            {
                foreach (uint offset in _pages.Keys)
                    ProtectMemory(offset, PageSize);
            }
        }

        internal void Unprotect()
        {
            UnprotectMemory(0, uint.MaxValue);
        }

        internal void Reset()
        {
            _started = false;
            _pages.Clear();
        }

        internal bool Hit(nuint address)
        {
            uint offset = GetMemoryOffset(address);
            if (offset == uint.MaxValue)
                return false;
            offset &= ~PageMask;
            ref Page? page = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(_pages, offset, out bool exists);
            if (exists)
                return true;
            page = new Page();
            new ReadOnlySpan<byte>(GetMemoryPage(offset), (int)PageSize).CopyTo(page.Data);
            UnprotectMemory(offset, PageSize);
            return true;
        }

        internal void GetPages(ref Dictionary<uint, Page> other)
        {
            (_pages, other) = (other, _pages);
            _pages = [];
        }

        internal abstract void ProtectMemory(uint address, uint size);

        internal abstract void UnprotectMemory(uint address, uint size);

        internal abstract uint GetMemoryOffset(nuint pointer);

        internal abstract byte* GetMemoryPage(uint address);
    }

    internal sealed class VramWatcher : Watcher
    {
        internal override void ProtectMemory(uint address, uint size)
        {
            Sh4AddressSpace.ProtectVram(address, Math.Min(Sh4DreamcastRuntime.Platform.VramSize - address, size) & ~PageMask);
        }

        internal override void UnprotectMemory(uint address, uint size)
        {
            Sh4AddressSpace.UnprotectVram(address, Math.Min(Sh4DreamcastRuntime.Platform.VramSize - address, size) & ~PageMask);
        }

        internal override uint GetMemoryOffset(nuint pointer) => Sh4AddressSpace.GetVramOffset(pointer);

        internal override byte* GetMemoryPage(uint address) => PvrMemory.VramPointer + address;
    }

    internal sealed class RamWatcher : Watcher
    {
        private static Sh4BlockManager BlockManager => Sh4RecompilerDriver.Current.BlockManager;

        internal override void ProtectMemory(uint address, uint size)
        {
            BlockManager.LockPage(address, Math.Min(Sh4DreamcastRuntime.Platform.RamSize - address, size) & ~PageMask);
        }

        internal override void UnprotectMemory(uint address, uint size)
        {
            BlockManager.UnlockPage(address, Math.Min(Sh4DreamcastRuntime.Platform.RamSize - address, size) & ~PageMask);
        }

        internal override uint GetMemoryOffset(nuint pointer) => BlockManager.GetRamOffset(pointer);

        internal override byte* GetMemoryPage(uint address) => (byte*)Sh4DreamcastRuntime.AddressSpace.RamBuffer + address;
    }

    internal sealed partial class AicaRamWatcher : Watcher
    {
        internal override partial void ProtectMemory(uint address, uint size);

        internal override partial void UnprotectMemory(uint address, uint size);

        internal override partial uint GetMemoryOffset(nuint pointer);

        internal override byte* GetMemoryPage(uint address) => AicaInterface.Ram + address;
    }

    internal sealed partial class ElanRamWatcher : Watcher
    {
        internal override partial void ProtectMemory(uint address, uint size);

        internal override partial void UnprotectMemory(uint address, uint size);

        internal override partial uint GetMemoryOffset(nuint pointer);

        internal override byte* GetMemoryPage(uint address) => PvrElan.Ram + address;
    }

    internal static bool WriteAccess(nuint address)
    {
        if (!Sh4Configuration.GgpoEnabled)
            return false;
        if (Ram.Hit(address))
        {
            Sh4BlockManager.HandleRamWriteAccess(address);
            return true;
        }
        if (Vram.Hit(address))
        {
            PvrTextureCache.VramLockedWrite((byte*)address);
            return true;
        }
        if (Sh4DreamcastRuntime.Platform.IsNaomi2 && ElanRam.Hit(address))
            return true;
        return Aram.Hit(address);
    }

    internal static void Protect()
    {
        if (!Sh4Configuration.GgpoEnabled)
            return;
        Vram.Protect();
        Ram.Protect();
        Aram.Protect();
        ElanRam.Protect();
    }

    internal static void Unprotect()
    {
        Vram.Unprotect();
        Ram.Unprotect();
        Aram.Unprotect();
        ElanRam.Unprotect();
    }

    internal static void Reset()
    {
        Vram.Reset();
        Ram.Reset();
        Aram.Reset();
        ElanRam.Reset();
    }
}
