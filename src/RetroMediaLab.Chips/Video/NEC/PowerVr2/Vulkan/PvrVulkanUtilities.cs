using Silk.NET.Vulkan;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Vulkan;

internal enum PvrVulkanModVolMode
{
    Xor,
    Or,
    Inclusion,
    Exclusion,
    Final
}

internal static class PvrVulkanUtilities
{
    internal static readonly CompareOp[] DepthOps =
    [
        CompareOp.Never,
        CompareOp.Less,
        CompareOp.Equal,
        CompareOp.LessOrEqual,
        CompareOp.Greater,
        CompareOp.NotEqual,
        CompareOp.GreaterOrEqual,
        CompareOp.Always
    ];

    internal static BlendFactor GetBlendFactor(uint instruction, bool source)
    {
        switch (instruction)
        {
            case 0: return BlendFactor.Zero;
            case 1: return BlendFactor.One;
            case 2: return source ? BlendFactor.DstColor : BlendFactor.SrcColor;
            case 3: return source ? BlendFactor.OneMinusDstColor : BlendFactor.OneMinusSrcColor;
            case 4: return BlendFactor.SrcAlpha;
            case 5: return BlendFactor.OneMinusSrcAlpha;
            case 6: return BlendFactor.DstAlpha;
            case 7: return BlendFactor.OneMinusDstAlpha;
            default:
                PvrRuntime.Die("Unsupported blend instruction");
                return BlendFactor.Zero;
        }
    }

    internal static uint FindMemoryType(in PhysicalDeviceMemoryProperties properties, uint typeBits, MemoryPropertyFlags requirementsMask)
    {
        uint typeIndex = uint.MaxValue;
        for (uint i = 0; i < properties.MemoryTypeCount; i++)
        {
            if ((typeBits & 1) != 0 && (properties.MemoryTypes[(int)i].PropertyFlags & requirementsMask) == requirementsMask)
            {
                typeIndex = i;
                break;
            }
            typeBits >>= 1;
        }
        PvrRuntime.Verify(typeIndex != uint.MaxValue);
        return typeIndex;
    }

    internal const string GouraudSource = """

#if pp_Gouraud == 0
#define INTERPOLATION flat
#else
#define INTERPOLATION
#endif

""";

    internal static ClearColorValue GetBorderColor() =>
        new(PvrRegisters.VO_BORDER_COL.Red(), PvrRegisters.VO_BORDER_COL.Green(), PvrRegisters.VO_BORDER_COL.Blue(), 1f);

    internal static uint Align(ulong offset, uint alignment)
    {
        uint pad = (uint)(offset & (alignment - 1));
        return pad == 0 ? 0 : alignment - pad;
    }
}

internal sealed class PvrVulkanSource
{
    private readonly PvrShaderSource _source = new("#version 430");

    internal PvrVulkanSource AddSource(string source)
    {
        _source.AddSource(source);
        return this;
    }

    internal PvrVulkanSource AddConstant(string name, int value)
    {
        _source.AddConstant(name, value);
        return this;
    }

    internal PvrVulkanSource AddConstant(string name, string value = "0")
    {
        _source.AddConstant(name, value);
        return this;
    }

    internal PvrVulkanSource SetConstant(string name, int value)
    {
        _source.SetConstant(name, value);
        return this;
    }

    internal PvrVulkanSource SetConstant(string name, string value)
    {
        _source.SetConstant(name, value);
        return this;
    }

    internal string Generate() => _source.Generate();
}
