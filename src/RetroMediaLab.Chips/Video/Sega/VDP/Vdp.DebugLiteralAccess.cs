#if DEBUG
using System.Reflection;
using RetroMediaLab.Chips.Common.Runtime;

namespace RetroMediaLab.Chips.Video.Sega.VDP;

internal static class VdpLiteralAccess
{
    internal static readonly NativeLiteralAccessor<uint>.Getter? AttributeTable =
        NativeLiteralAccessor<uint>.Create(typeof(Vdp)
            .GetMethod("get_DebugOriginalAttributeTable", BindingFlags.Static | BindingFlags.NonPublic));
}
#endif
