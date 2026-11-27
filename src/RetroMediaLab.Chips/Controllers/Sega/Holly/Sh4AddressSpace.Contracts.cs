using RetroMediaLab.Chips.Audio.Yamaha.AICA;
using RetroMediaLab.Chips.Video.NEC.PowerVr2;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

internal sealed unsafe partial class Sh4AddressSpace
{
    internal nuint RamBase => _ramBase;

    internal bool VirtualMemoryEnabled => _ramBase != 0;

    internal uint PageSize => (uint)Environment.SystemPageSize;

    internal uint MaximumRamSize => 32 * 1024 * 1024;

    internal uint RamSize => Sh4DreamcastRuntime.Platform.RamSize;

    internal uint RamMask => Sh4DreamcastRuntime.Platform.RamMask;

    private static nuint VideoRamBuffer => (nuint)PvrMemory.VramPointer;

    private static uint VideoRamMask => Sh4DreamcastRuntime.Platform.VramMask;

    internal void MapBlockMirror(nuint address, uint start, uint end, uint blockLength)
    {
        uint blockSize = blockLength >> 24;
        for (uint region = start; region <= end; region += blockSize)
            MapBlock(address, region, region + blockSize - 1, blockLength - 1);
    }

    internal int Read8Signed32(uint address) => unchecked((sbyte)Read<byte>(address));

    internal int Read16Signed32(uint address) => unchecked((short)Read<ushort>(address));

    internal void LockRegion(nuint address, uint size) => Sh4VirtualMemory.LockRegion(address, size);

    internal void UnlockRegion(nuint address, uint size) => Sh4VirtualMemory.UnlockRegion(address, size);
}
