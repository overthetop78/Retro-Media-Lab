using Silk.NET.Vulkan;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Vulkan;

internal enum PvrVulkanOitPass { Depth, Color, Oit }

internal sealed partial class PvrVulkanOitShaders
{
    internal readonly record struct VertexShaderParams(bool Gouraud, bool Naomi2, bool LightOn, bool TwoVolume, bool Texture, bool DivPosZ)
    {
        internal uint Hash() => (Gouraud ? 1u : 0u) | ((Naomi2 ? 1u : 0u) << 1) | ((LightOn ? 1u : 0u) << 2)
            | ((TwoVolume ? 1u : 0u) << 3) | ((Texture ? 1u : 0u) << 4) | ((DivPosZ ? 1u : 0u) << 5);
    }

    internal readonly record struct FragmentShaderParams(bool AlphaTest, bool InsideClipTest, bool UseAlpha, bool Texture,
        bool IgnoreTexAlpha, int ShaderInstr, bool Offset, int Fog, bool Gouraud, bool Bumpmap, bool Clamping,
        bool TwoVolume, int Palette, bool DivPosZ, bool UseBda, PvrVulkanOitPass Pass)
    {
        internal uint Hash() => (AlphaTest ? 1u : 0u) | ((InsideClipTest ? 1u : 0u) << 1) | ((UseAlpha ? 1u : 0u) << 2)
            | ((Texture ? 1u : 0u) << 3) | ((IgnoreTexAlpha ? 1u : 0u) << 4) | ((uint)ShaderInstr << 5)
            | ((Offset ? 1u : 0u) << 7) | ((uint)Fog << 8) | ((Gouraud ? 1u : 0u) << 10)
            | ((Bumpmap ? 1u : 0u) << 11) | ((Clamping ? 1u : 0u) << 12) | ((TwoVolume ? 1u : 0u) << 13)
            | ((uint)Palette << 14) | ((uint)Pass << 16) | ((DivPosZ ? 1u : 0u) << 18) | ((UseBda ? 1u : 0u) << 19);
    }

    internal readonly record struct ModVolShaderParams(bool Naomi2, bool DivPosZ)
    {
        internal uint Hash() => (Naomi2 ? 1u : 0u) | ((DivPosZ ? 1u : 0u) << 1);
    }

    internal readonly record struct TrModVolShaderParams(PvrVulkanModVolMode Mode, bool DivPosZ, bool UseBda)
    {
        internal uint Hash() => (uint)Mode | ((DivPosZ ? 1u : 0u) << 3) | ((UseBda ? 1u : 0u) << 4);
    }

    internal readonly record struct FinalShaderParams(bool Dithering, bool UseBda)
    {
        internal uint Hash() => (Dithering ? 1u : 0u) | ((UseBda ? 1u : 0u) << 1);
    }

    internal ShaderModule GetVertexShader(in VertexShaderParams parameters) =>
        GetShader(_vertexShaders, parameters.Hash(), parameters, CompileShader);

    internal ShaderModule GetFragmentShader(in FragmentShaderParams parameters) =>
        GetShader(_fragmentShaders, parameters.Hash(), parameters, CompileShader);

    internal ShaderModule GetModVolVertexShader(in ModVolShaderParams parameters) =>
        GetShader(_modVolVertexShaders, parameters.Hash(), parameters, CompileShader);

    internal ShaderModule GetModVolShader(bool divPosZ)
    {
        int index = divPosZ ? 1 : 0;
        _modVolShaders[index] ??= CompileModVolFragmentShader(divPosZ);
        return _modVolShaders[index]!.Handle;
    }

    internal ShaderModule GetTrModVolShader(in TrModVolShaderParams parameters)
    {
        CheckMaxLayers();
        return GetShader(_trModVolShaders, parameters.Hash(), parameters, CompileShader);
    }

    internal ShaderModule GetFinalShader(in FinalShaderParams parameters)
    {
        CheckMaxLayers();
        return GetShader(_finalFragmentShaders, parameters.Hash(), parameters, CompileShader);
    }

    internal ShaderModule GetFinalVertexShader()
    {
        _finalVertexShader ??= CompileFinalVertexShader();
        return _finalVertexShader.Handle;
    }

    internal ShaderModule GetClearShader()
    {
        _clearShader ??= CompileClearShader();
        return _clearShader.Handle;
    }

    internal void Term()
    {
        ReleaseShaders(_vertexShaders);
        ReleaseShaders(_fragmentShaders);
        ReleaseShaders(_modVolVertexShaders);
        _modVolShaders[0]?.Dispose();
        _modVolShaders[0] = null;
        _modVolShaders[1]?.Dispose();
        _modVolShaders[1] = null;
        ReleaseShaders(_trModVolShaders);
        _finalVertexShader?.Dispose();
        _finalVertexShader = null;
        ReleaseShaders(_finalFragmentShaders);
        _clearShader?.Dispose();
        _clearShader = null;
    }

    private delegate PvrVulkanOwned<ShaderModule> ShaderCompilation<T>(in T parameters);

    private static ShaderModule GetShader<T>(SortedDictionary<uint, PvrVulkanOwned<ShaderModule>> shaders,
        uint key, T parameters, ShaderCompilation<T> compile)
    {
        if (shaders.TryGetValue(key, out PvrVulkanOwned<ShaderModule>? shader))
            return shader.Handle;
        shaders[key] = compile(parameters);
        return shaders[key].Handle;
    }

    private static void ReleaseShaders(SortedDictionary<uint, PvrVulkanOwned<ShaderModule>> shaders)
    {
        foreach (PvrVulkanOwned<ShaderModule> shader in shaders.Values)
            shader.Dispose();
        shaders.Clear();
    }

    private readonly SortedDictionary<uint, PvrVulkanOwned<ShaderModule>> _vertexShaders = [];
    private readonly SortedDictionary<uint, PvrVulkanOwned<ShaderModule>> _fragmentShaders = [];
    private readonly SortedDictionary<uint, PvrVulkanOwned<ShaderModule>> _modVolVertexShaders = [];
    private readonly PvrVulkanOwned<ShaderModule>?[] _modVolShaders = new PvrVulkanOwned<ShaderModule>?[2];
    private readonly SortedDictionary<uint, PvrVulkanOwned<ShaderModule>> _trModVolShaders = [];
    private readonly SortedDictionary<uint, PvrVulkanOwned<ShaderModule>> _finalFragmentShaders = [];
    private PvrVulkanOwned<ShaderModule>? _finalVertexShader;
    private PvrVulkanOwned<ShaderModule>? _clearShader;
    private int _maxLayers;
}
