#if DEBUG
using System.Reflection;
using System.Runtime.CompilerServices;
using RetroMediaLab.Chips.CPU.Motorola.M68000.Execution;
using RetroMediaLab.Chips.Video.Sega.VDP;

namespace RetroMediaLab.Chips.Tests.SegaEmu;

public sealed class LiteralAccessTests
{
    private delegate ReadOnlySpan<ushort> InstructionGetter();
    private delegate ReadOnlySpan<uint> AttributeGetter();

    [Fact]
    public void EveryInstructionMappingIsUnchangedAndNativeAccessorIsAvailable()
    {
        Type type = typeof(Cpu68000<ICpu68000Bus>);
        var original = type.GetMethod("get_DebugOriginalInstructionMap", BindingFlags.Static | BindingFlags.NonPublic)!.CreateDelegate<InstructionGetter>();
        var active = type.GetMethod("get_InstructionMap", BindingFlags.Static | BindingFlags.NonPublic)!.CreateDelegate<InstructionGetter>();
        Assert.Equal(65536, active().Length);
        Assert.True(original().SequenceEqual(active()));
        object? native = type.Assembly.GetType("RetroMediaLab.Chips.CPU.Motorola.M68000.Execution.Cpu68000LiteralAccess")!
            .GetField("InstructionMap", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null);
        if (RuntimeFeature.IsDynamicCodeSupported) Assert.NotNull(native);
        else Assert.Null(native);
    }

    [Fact]
    public void EveryVideoAttributeIsUnchangedAndNativeAccessorIsAvailable()
    {
        Type type = typeof(Vdp);
        var original = type.GetMethod("get_DebugOriginalAttributeTable", BindingFlags.Static | BindingFlags.NonPublic)!.CreateDelegate<AttributeGetter>();
        var active = type.GetMethod("get_atex_table", BindingFlags.Static | BindingFlags.NonPublic)!.CreateDelegate<AttributeGetter>();
        Assert.Equal(8, active().Length);
        Assert.True(original().SequenceEqual(active()));
        object? native = type.Assembly.GetType("RetroMediaLab.Chips.Video.Sega.VDP.VdpLiteralAccess")!
            .GetField("AttributeTable", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null);
        if (RuntimeFeature.IsDynamicCodeSupported) Assert.NotNull(native);
        else Assert.Null(native);
    }
}
#endif
