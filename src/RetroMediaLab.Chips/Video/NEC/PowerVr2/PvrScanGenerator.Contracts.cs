using System.IO;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2;

internal static unsafe partial class PvrScanGenerator
{
    internal static partial bool Init();

    internal static partial void Term();

    internal static partial void Reset(bool hard);

    internal static partial void Serialize(BinaryWriter writer);

    internal static partial void Deserialize(BinaryReader reader, int version);

    internal static partial void CalculateSync();

    internal static partial void ReadLightgunPosition(int x, int y);

    internal static partial void ScheduleRenderDone(TaContext? context);

    internal static partial void Reschedule();
}
