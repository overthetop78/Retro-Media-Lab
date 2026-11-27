using System.IO;
using RetroMediaLab.Chips.CPU.Hitachi.SH4;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2;

internal static unsafe partial class PvrMemory
{
    internal static uint ReadRegister(uint address) => PvrRegisters.Read(address);
    internal static void WriteRegister(uint address, uint data) => PvrRegisters.Write(address, data);

    internal static partial void TaWrite(uint address, Sh4StoreQueueBuffer* data, uint count);
    internal static partial void TaWriteSq(uint address, Sh4StoreQueueBuffer* storeQueues);
    internal static partial void YuvInit();
    internal static partial void YuvSerialize(BinaryWriter writer);
    internal static partial void YuvDeserialize(BinaryReader reader);
    internal static partial void YuvReset();

    internal static partial T Read32<T>(uint address) where T : unmanaged;
    internal static partial void Write32<T>(uint address, T data, bool internalWrite = false) where T : unmanaged;
    internal static partial T ReadArea4<T>(uint address, bool upper) where T : unmanaged;
    internal static partial void WriteArea4<T>(uint address, T data, bool upper) where T : unmanaged;
}
