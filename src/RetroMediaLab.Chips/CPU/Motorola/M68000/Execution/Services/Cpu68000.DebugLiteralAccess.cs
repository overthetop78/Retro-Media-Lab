#if DEBUG
using System.Reflection;
using RetroMediaLab.Chips.Common.Runtime;

namespace RetroMediaLab.Chips.CPU.Motorola.M68000.Execution;

internal static class Cpu68000LiteralAccess
{
    internal static readonly NativeLiteralAccessor<ushort>.Getter? InstructionMap =
        NativeLiteralAccessor<ushort>.Create(typeof(Cpu68000<ICpu68000Bus>)
            .GetMethod("get_DebugOriginalInstructionMap", BindingFlags.Static | BindingFlags.NonPublic));
}
#endif
