using RetroMediaLab.Chips.Audio.Yamaha.AICA;
using RetroMediaLab.Chips.Video.NEC.PowerVr2;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

internal static unsafe partial class Sh4MemoryWatch
{
    internal static readonly VramWatcher Vram = new();
    internal static readonly RamWatcher Ram = new();
    internal static readonly AicaRamWatcher Aram = new();
    internal static readonly ElanRamWatcher ElanRam = new();

    internal sealed partial class AicaRamWatcher
    {
        internal override partial void ProtectMemory(uint address, uint size)
        {
            size = Math.Min(Sh4DreamcastRuntime.Platform.AramSize - address, size) & ~PageMask;
            Sh4VirtualMemory.LockRegion((nuint)(AicaInterface.Ram + address), size);
        }

        internal override partial void UnprotectMemory(uint address, uint size)
        {
            size = Math.Min(Sh4DreamcastRuntime.Platform.AramSize - address, size) & ~PageMask;
            Sh4VirtualMemory.UnlockRegion((nuint)(AicaInterface.Ram + address), size);
        }

        internal override partial uint GetMemoryOffset(nuint pointer)
        {
            nuint offset = Sh4VirtualMemory.Untag(pointer) - Sh4VirtualMemory.Untag((nuint)AicaInterface.Ram);
            if (offset >= Sh4DreamcastRuntime.Platform.AramSize)
                return uint.MaxValue;
            return (uint)offset;
        }
    }

    internal sealed partial class ElanRamWatcher
    {
        internal override partial void ProtectMemory(uint address, uint size)
        {
            if (PvrElan.RamSize != 0)
            {
                size = Math.Min(PvrElan.RamSize - address, size) & ~PageMask;
                Sh4VirtualMemory.LockRegion((nuint)(PvrElan.Ram + address), size);
            }
        }

        internal override partial void UnprotectMemory(uint address, uint size)
        {
            if (PvrElan.RamSize != 0)
            {
                size = Math.Min(PvrElan.RamSize - address, size) & ~PageMask;
                Sh4VirtualMemory.UnlockRegion((nuint)(PvrElan.Ram + address), size);
            }
        }

        internal override partial uint GetMemoryOffset(nuint pointer)
        {
            nuint offset = Sh4VirtualMemory.Untag(pointer) - Sh4VirtualMemory.Untag((nuint)PvrElan.Ram);
            if (offset >= PvrElan.RamSize)
                return uint.MaxValue;
            return (uint)offset;
        }
    }
}
