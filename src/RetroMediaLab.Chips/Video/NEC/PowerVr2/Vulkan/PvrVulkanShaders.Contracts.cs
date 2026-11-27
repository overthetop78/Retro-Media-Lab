using System.Numerics;
using System.Runtime.InteropServices;
using Silk.NET.Vulkan;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Vulkan;

internal record struct PvrVulkanVertexShaderParams(bool Gouraud, bool Naomi2, bool DivPosZ, bool Texture)
{
    internal readonly uint Hash() => (Gouraud ? 1u : 0u) | ((Naomi2 ? 1u : 0u) << 1)
        | ((DivPosZ ? 1u : 0u) << 2) | ((Texture ? 1u : 0u) << 3);
}

internal record struct PvrVulkanFragmentShaderParams(
    bool AlphaTest, bool InsideClipTest, bool UseAlpha, bool Texture, bool IgnoreTexAlpha, int ShaderInstr,
    bool Offset, int Fog, bool Gouraud, bool Bumpmap, bool Clamping, bool Trilinear, int Palette, bool DivPosZ, bool Dithering)
{
    internal readonly uint Hash() => (AlphaTest ? 1u : 0u) | ((InsideClipTest ? 1u : 0u) << 1) | ((UseAlpha ? 1u : 0u) << 2)
        | ((Texture ? 1u : 0u) << 3) | ((IgnoreTexAlpha ? 1u : 0u) << 4) | ((uint)ShaderInstr << 5)
        | ((Offset ? 1u : 0u) << 7) | ((uint)Fog << 8) | ((Gouraud ? 1u : 0u) << 10)
        | ((Bumpmap ? 1u : 0u) << 11) | ((Clamping ? 1u : 0u) << 12) | ((Trilinear ? 1u : 0u) << 13)
        | ((uint)Palette << 14) | ((DivPosZ ? 1u : 0u) << 16) | ((Dithering ? 1u : 0u) << 17);
}

internal record struct PvrVulkanModVolShaderParams(bool Naomi2, bool DivPosZ)
{
    internal readonly uint Hash() => (Naomi2 ? 1u : 0u) | ((DivPosZ ? 1u : 0u) << 1);
}

[StructLayout(LayoutKind.Sequential)]
internal struct PvrVulkanVertexShaderUniforms
{
    internal Matrix4x4 NdcMat;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct PvrVulkanFragmentShaderUniforms
{
    internal fixed float ColorClampMin[4];
    internal fixed float ColorClampMax[4];
    internal fixed float FogColRam[4];
    internal fixed float FogColVert[4];
    internal fixed float DitherDivisor[4];
    internal float AlphaTestValue;
    internal float FogDensity;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct PvrVulkanN2VertexShaderUniforms
{
    internal Matrix4x4 MvMat;
    internal Matrix4x4 NormalMat;
    internal Matrix4x4 ProjMat;
    internal fixed int EnvMapping[2];
    internal int BumpMapping;
    internal int PolyNumber;
    internal fixed float GlossCoef[2];
    internal fixed int ConstantColor[2];
}

internal sealed partial class PvrVulkanShaders
{
    internal ShaderModule GetVertexShader(PvrVulkanVertexShaderParams parameters) =>
        GetShader(_vertexShaders, parameters.Hash(), parameters, CompileShader);

    internal ShaderModule GetFragmentShader(PvrVulkanFragmentShaderParams parameters) =>
        GetShader(_fragmentShaders, parameters.Hash(), parameters, CompileShader);

    internal ShaderModule GetModVolVertexShader(PvrVulkanModVolShaderParams parameters) =>
        GetShader(_modVolVertexShaders, parameters.Hash(), parameters, CompileShader);

    internal ShaderModule GetModVolShader(bool divPosZ)
    {
        ref PvrVulkanOwned<ShaderModule>? shader = ref _modVolShaders[divPosZ ? 1 : 0];
        shader ??= CompileModVolFragmentShader(divPosZ);
        return shader.Handle;
    }

    internal ShaderModule GetQuadVertexShader(bool rotate = false)
    {
        if (rotate)
        {
            _quadRotateVertexShader ??= CompileQuadVertexShader(true);
            return _quadRotateVertexShader.Handle;
        }

        _quadVertexShader ??= CompileQuadVertexShader(false);
        return _quadVertexShader.Handle;
    }

    internal ShaderModule GetQuadFragmentShader(bool ignoreTexAlpha)
    {
        if (ignoreTexAlpha)
        {
            _quadNoAlphaFragmentShader ??= CompileQuadFragmentShader(true);
            return _quadNoAlphaFragmentShader.Handle;
        }

        _quadFragmentShader ??= CompileQuadFragmentShader(false);
        return _quadFragmentShader.Handle;
    }

    internal void Term()
    {
        try
        {
            Clear(_vertexShaders);
            Clear(_fragmentShaders);
            Clear(_modVolVertexShaders);
        }
        finally
        {
            Reset(ref _modVolShaders[0]);
            Reset(ref _modVolShaders[1]);
            Reset(ref _quadVertexShader);
            Reset(ref _quadRotateVertexShader);
            Reset(ref _quadFragmentShader);
            Reset(ref _quadNoAlphaFragmentShader);
        }
    }

    private static void Clear(SortedDictionary<uint, PvrVulkanOwned<ShaderModule>> shaders)
    {
        try
        {
            foreach (var shader in shaders.Values)
                shader.Dispose();
        }
        finally
        {
            shaders.Clear();
        }
    }

    private static void Reset(ref PvrVulkanOwned<ShaderModule>? shader)
    {
        try
        {
            shader?.Dispose();
        }
        finally
        {
            shader = null;
        }
    }

    private static ShaderModule GetShader<T>(SortedDictionary<uint, PvrVulkanOwned<ShaderModule>> shaders,
        uint hash, T parameters, Func<T, PvrVulkanOwned<ShaderModule>> compile)
    {
        if (shaders.TryGetValue(hash, out var shader))
            return shader.Handle;
        shaders[hash] = compile(parameters);
        return shaders[hash].Handle;
    }

    private readonly SortedDictionary<uint, PvrVulkanOwned<ShaderModule>> _vertexShaders = [];
    private readonly SortedDictionary<uint, PvrVulkanOwned<ShaderModule>> _fragmentShaders = [];
    private readonly SortedDictionary<uint, PvrVulkanOwned<ShaderModule>> _modVolVertexShaders = [];
    private readonly PvrVulkanOwned<ShaderModule>?[] _modVolShaders = new PvrVulkanOwned<ShaderModule>?[2];
    private PvrVulkanOwned<ShaderModule>? _quadVertexShader;
    private PvrVulkanOwned<ShaderModule>? _quadRotateVertexShader;
    private PvrVulkanOwned<ShaderModule>? _quadFragmentShader;
    private PvrVulkanOwned<ShaderModule>? _quadNoAlphaFragmentShader;
}
