using System.Buffers.Binary;
using System.Text;
using Veldrid;
using Veldrid.SPIRV;
using Vortice.D3DCompiler;

namespace RetroMediaLab.App.Rendering.Emulation.Processing.Shaders;

internal static class VeldridShaderBytecode
{
    internal static Shader[] Create(ResourceFactory factory, string vertexSource, string fragmentSource)
    {
        var direct3D = factory.BackendType == GraphicsBackend.Direct3D11;
        var renderer = direct3D ? "Direct3D11" : "Vulkan";
        var compilerVersion = typeof(SpirvCompilation).Assembly.GetName().Version?.ToString() ?? "";
        var nativeVersion = direct3D ? typeof(Compiler).Assembly.GetName().Version?.ToString() ?? "" : "";
        var vertexPath = VideoShaderDiskCache.Entry(renderer, compilerVersion, nativeVersion, "vertex-v1", vertexSource);
        var fragmentPath = VideoShaderDiskCache.Entry(renderer, compilerVersion, nativeVersion, "fragment-v1",
            vertexSource, fragmentSource);
        var vertex = VideoShaderDiskCache.Read(vertexPath);
        var fragment = VideoShaderDiskCache.Read(fragmentPath);
        if (HasExpectedFormat(vertex, direct3D) && HasExpectedFormat(fragment, direct3D))
        {
            try { return CreateShaders(factory, vertex!, fragment!); }
            catch (Exception error) when (error is VeldridException or SharpGen.Runtime.SharpGenException)
            {
                vertex = null;
                fragment = null;
            }
        }

        if (direct3D)
        {
            var translated = SpirvCompilation.CompileVertexFragment(Encoding.UTF8.GetBytes(vertexSource),
                Encoding.UTF8.GetBytes(fragmentSource), CrossCompileTarget.HLSL, new CrossCompileOptions());
            vertex = CompileHlsl(translated.VertexShader, "vs_5_0");
            fragment = CompileHlsl(translated.FragmentShader, "ps_5_0");
        }
        else
        {
            vertex = HasExpectedFormat(vertex, false) ? vertex! : CompileSpirv(vertexSource, ShaderStages.Vertex, "video.vert");
            fragment = HasExpectedFormat(fragment, false) ? fragment! : CompileSpirv(fragmentSource, ShaderStages.Fragment, "video.frag");
        }

        var shaders = CreateShaders(factory, vertex, fragment);
        VideoShaderDiskCache.Write(vertexPath, vertex);
        VideoShaderDiskCache.Write(fragmentPath, fragment);
        return shaders;
    }

    private static bool HasExpectedFormat(byte[]? bytes, bool direct3D) => bytes is { Length: >= 20 }
        && (direct3D ? bytes.AsSpan(0, 4).SequenceEqual("DXBC"u8)
            : bytes.Length % 4 == 0 && BinaryPrimitives.ReadUInt32LittleEndian(bytes) == 0x07230203);

    private static byte[] CompileSpirv(string source, ShaderStages stage, string name) =>
        SpirvCompilation.CompileGlslToSpirv(source, name, stage, new GlslCompileOptions()).SpirvBytes;

    private static byte[] CompileHlsl(string source, string profile)
    {
        Compiler.Compile(Encoding.ASCII.GetBytes(source), null!, null!, "main", null!,
            profile, ShaderFlags.OptimizationLevel3, out var code, out var errors);
        using (code)
        using (errors)
        {
            if (code is null)
                throw new VeldridException(errors is null ? "D3D11 shader compilation failed." : Encoding.ASCII.GetString(errors.AsBytes()));
            return code.AsBytes();
        }
    }

    private static Shader[] CreateShaders(ResourceFactory factory, byte[] vertex, byte[] fragment)
    {
        Shader? first = factory.CreateShader(new ShaderDescription(ShaderStages.Vertex, vertex, "main"));
        try
        {
            var result = new[] { first, factory.CreateShader(new ShaderDescription(ShaderStages.Fragment, fragment, "main")) };
            first = null;
            return result;
        }
        finally { first?.Dispose(); }
    }
}
