using Silk.NET.Vulkan;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Vulkan;

internal static partial class PvrVulkanShaderCompiler
{
    internal static void Init()
    {
        if (_initCount++ == 0)
        {
            bool initialized = Glslang.InitializeProcess();
            PvrRuntime.Verify(initialized);
        }
    }

    internal static void Term()
    {
        if (--_initCount == 0)
            Glslang.FinalizeProcess();
        _initCount = Math.Max(_initCount, 0);
    }

    private static GlslangLanguage TranslateShaderStage(ShaderStageFlags stage)
    {
        switch (stage)
        {
            case ShaderStageFlags.VertexBit: return GlslangLanguage.Vertex;
            case ShaderStageFlags.TessellationControlBit: return GlslangLanguage.TessControl;
            case ShaderStageFlags.TessellationEvaluationBit: return GlslangLanguage.TessEvaluation;
            case ShaderStageFlags.GeometryBit: return GlslangLanguage.Geometry;
            case ShaderStageFlags.FragmentBit: return GlslangLanguage.Fragment;
            case ShaderStageFlags.ComputeBit: return GlslangLanguage.Compute;
            case ShaderStageFlags.RaygenBitNV: return GlslangLanguage.RayGenNV;
            case ShaderStageFlags.AnyHitBitNV: return GlslangLanguage.AnyHitNV;
            case ShaderStageFlags.ClosestHitBitNV: return GlslangLanguage.ClosestHitNV;
            case ShaderStageFlags.MissBitNV: return GlslangLanguage.MissNV;
            case ShaderStageFlags.IntersectionBitNV: return GlslangLanguage.IntersectNV;
            case ShaderStageFlags.CallableBitNV: return GlslangLanguage.CallableNV;
            case ShaderStageFlags.TaskBitNV: return GlslangLanguage.TaskNV;
            case ShaderStageFlags.MeshBitNV: return GlslangLanguage.MeshNV;
            default:
                PvrRuntime.Die("Unknown shader stage");
                return GlslangLanguage.Vertex;
        }
    }

    private static bool GlslToSpv(ShaderStageFlags shaderType, string glslShader, List<uint> spvShader)
    {
        GlslangLanguage stage = TranslateShaderStage(shaderType);
        using var shader = new GlslangShader(stage);
        shader.SetStrings([glslShader], 1);
        const GlslangMessages messages = GlslangMessages.SpvRules | GlslangMessages.VulkanRules;
        if (!shader.Parse(Glslang.GetDefaultResources(), 100, false, messages))
        {
            PvrRuntime.Log.Error("{0}", shader.GetInfoLog());
            PvrRuntime.Log.Error("{0}", shader.GetInfoDebugLog());
            return false;
        }

        using var program = new GlslangProgram();
        program.AddShader(shader);
        if (!program.Link(messages))
        {
            PvrRuntime.Log.Error("{0}", shader.GetInfoLog());
            PvrRuntime.Log.Error("{0}", shader.GetInfoDebugLog());
            return false;
        }

        Glslang.GlslangToSpv(program.GetIntermediate(stage), spvShader);
        return true;
    }

    internal static PvrVulkanOwned<ShaderModule> Compile(ShaderStageFlags shaderStage, string shaderText)
    {
        List<uint> spirv = [];
        bool ok = GlslToSpv(shaderStage, shaderText, spirv);
        PvrRuntime.Verify(ok);
        return PvrVulkanContext.Instance.GetDevice().CreateShaderModuleUnique(spirv);
    }
}
