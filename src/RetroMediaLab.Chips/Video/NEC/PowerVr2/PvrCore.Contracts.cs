using System.IO;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2;

internal static partial class PvrCore
{
    internal static partial void Init();

    internal static partial void Term();

    internal static partial void Reset(bool hard);

    internal static partial void Serialize(BinaryWriter writer, bool rollback, bool dryRun = false);

    internal static partial void Deserialize(BinaryReader reader, int version, bool rollback);
}
