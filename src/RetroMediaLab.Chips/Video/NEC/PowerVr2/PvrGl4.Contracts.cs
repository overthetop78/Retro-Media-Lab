using System.Numerics;
using static RetroMediaLab.Chips.Video.NEC.PowerVr2.PvrRegisters;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2;

internal static unsafe partial class PvrGl4
{
    internal const uint MaxShaderStorageBlockSize = 0x90de;
    internal const uint ShaderStorageBuffer = 0x90d2;
    internal const uint ReadWrite = 0x88ba;
    internal const uint DepthStencilTextureMode = 0x90ea;
    internal const uint StencilIndex = 0x1901;
    internal const uint AtomicCounterBuffer = 0x92c0;
    internal const uint ShaderImageAccessBarrierBit = 0x00000020;
    internal const uint BufferUpdateBarrierBit = 0x00000200;

    internal static readonly PvrGl4Context Gl = new();
    internal static readonly PvrGl4ShaderUniforms ShaderUniforms = new();

    internal static int MaxImageWidth;
    internal static int MaxImageHeight;

    internal static uint StencilTextureId;
    internal static uint DepthTextureId;
    internal static readonly uint[] OpaqueTextureId = new uint[2];
    internal static readonly uint[] GeometryFbo = new uint[2];
    internal static readonly uint[] TextureSamplers = new uint[2];
}

internal enum PvrGl4Pass { Depth, Color, Oit }

internal sealed class PvrGl4PipelineShader : PvrGlNaomi2Shader
{
    internal int ClipTest;
    internal int AlphaTestValue;
    internal int FogColorRam;
    internal int FogColorVertex;
    internal int FogDensity;
    internal int ShadeScaleFactor;
    internal int PolyNumber;
    internal int BlendMode;
    internal int UseAlphaUniform;
    internal int IgnoreTexAlpha;
    internal int ShadingInstr;
    internal int FogControl;
    internal int TrilinearAlpha;
    internal int FogClampMin;
    internal int FogClampMax;
    internal int NdcMat;
    internal int PaletteIndex;
    internal int DitherDivisor;
    internal bool AlphaTest;
    internal bool InsideClipping;
    internal bool Texture;
    internal bool UseAlpha;
    internal bool IgnoreTexA;
    internal uint ShadInstr;
    internal bool Offset;
    internal uint FogCtrl;
    internal PvrGl4Pass Pass;
    internal bool TwoVolumes;
    internal bool Gouraud;
    internal bool BumpMap;
    internal bool FogClamping;
    internal int Palette;
    internal bool Naomi2;
    internal bool DivPosZ;
}

internal sealed unsafe partial class PvrGl4MainVertexArray : PvrGlVertexArray { }
internal sealed unsafe partial class PvrGl4ModvolVertexArray : PvrGlVertexArray { }

internal sealed class PvrGl4Context
{
    internal struct ModifierVolumeShader
    {
        internal uint Program;
        internal uint NdcMat;
    }

    internal struct Naomi2ModifierVolumeShader
    {
        internal uint Program;
        internal uint NdcMat;
        internal int MvMat;
        internal int ProjMat;
    }

    internal sealed class VertexBuffers
    {
        internal readonly PvrGlBuffer?[] Geometry = new PvrGlBuffer[2];
        internal readonly PvrGlBuffer?[] Modvols = new PvrGlBuffer[2];
        internal readonly PvrGlBuffer?[] Indices = new PvrGlBuffer[2];
        internal readonly PvrGl4MainVertexArray[] MainVao = [new(), new()];
        internal readonly PvrGl4ModvolVertexArray[] ModvolVao = [new(), new()];
        internal readonly PvrGlBuffer?[] TrPolyParams = new PvrGlBuffer[2];
        internal int BufferIndex;

        internal PvrGlBuffer GetVertexBuffer() => Geometry[BufferIndex]!;
        internal PvrGlBuffer GetIndexBuffer() => Indices[BufferIndex]!;
        internal PvrGlBuffer GetModVolBuffer() => Modvols[BufferIndex]!;
        internal PvrGlBuffer GetPolyParamBuffer() => TrPolyParams[BufferIndex]!;
        internal PvrGl4MainVertexArray GetMainVao() => MainVao[BufferIndex];
        internal PvrGl4ModvolVertexArray GetModVolVao() => ModvolVao[BufferIndex];

        internal void NextBuffer() => BufferIndex = (BufferIndex + 1) % Geometry.Length;
    }

    internal ModifierVolumeShader ModvolShader;
    internal Naomi2ModifierVolumeShader N2ModvolShader;
    internal readonly Dictionary<uint, PvrGl4PipelineShader> Shaders = [];
    internal readonly VertexBuffers Vbo = new();
}

internal class PvrOpenGl4Source : PvrOpenGlSource
{
    internal PvrOpenGl4Source() : base(PvrGles.Gl.IsGles ? "#version 320 es" : "#version 430")
    {
        if (PvrGles.Gl.IsGles)
            AddSource("precision highp float;\n"
                + "precision highp int;\n"
                + "precision highp sampler2D;");
    }
}

internal sealed unsafe class PvrGl4ShaderUniforms
{
    internal float PtAlpha;
    internal float FogDensity;
    internal readonly float[] FogColorRam = new float[3];
    internal readonly float[] FogColorVertex = new float[3];
    internal int PolyNumber;
    internal float TrilinearAlpha;
    internal TSP Tsp0;
    internal TSP Tsp1;
    internal TCW Tcw0;
    internal TCW Tcw1;
    internal readonly float[] FogClampMin = new float[4];
    internal readonly float[] FogClampMax = new float[4];
    internal Matrix4x4 NdcMat;
    internal PvrGlBaseClipping BaseClipping;
    internal int PaletteIndex;
    internal readonly float[] DitherDivisor = new float[4];

    private static void SetUniformArray(int location, int a, int b)
    {
        int* values = stackalloc int[2] { a, b };
        PvrGl.Uniform1iv(location, 2, values);
    }

    internal void Set(PvrGl4PipelineShader shader)
    {
        PvrGl.Uniform1f(shader.AlphaTestValue, PtAlpha);
        PvrGl.Uniform1f(shader.FogDensity, FogDensity);
        fixed (float* values = FogColorRam)
            PvrGl.Uniform3fv(shader.FogColorRam, 1, values);
        fixed (float* values = FogColorVertex)
            PvrGl.Uniform3fv(shader.FogColorVertex, 1, values);
        PvrGl.Uniform1f(shader.ShadeScaleFactor, FPU_SHAD_SCALE.scale_factor / 256f);

        if (shader.BlendMode != -1)
        {
            uint* blendMode = stackalloc uint[4] { Tsp0.SrcInstr, Tsp0.DstInstr, Tsp1.SrcInstr, Tsp1.DstInstr };
            PvrGl.Uniform2iv(shader.BlendMode, 2, (int*)blendMode);
        }

        SetUniformArray(shader.UseAlphaUniform, (int)Tsp0.UseAlpha, (int)Tsp1.UseAlpha);
        SetUniformArray(shader.IgnoreTexAlpha, (int)Tsp0.IgnoreTexA, (int)Tsp1.IgnoreTexA);
        SetUniformArray(shader.ShadingInstr, (int)Tsp0.ShadInstr, (int)Tsp1.ShadInstr);
        SetUniformArray(shader.FogControl, (int)Tsp0.FogCtrl, (int)Tsp1.FogCtrl);
        PvrGl.Uniform1i(shader.PolyNumber, PolyNumber);
        PvrGl.Uniform1f(shader.TrilinearAlpha, TrilinearAlpha);
        fixed (float* values = FogClampMin)
            PvrGl.Uniform4fv(shader.FogClampMin, 1, values);
        fixed (float* values = FogClampMax)
            PvrGl.Uniform4fv(shader.FogClampMax, 1, values);
        fixed (Matrix4x4* matrix = &NdcMat)
            PvrGl.UniformMatrix4fv(shader.NdcMat, 1, 0, (float*)matrix);
        PvrGl.Uniform1i(shader.PaletteIndex, PaletteIndex);
        fixed (float* values = DitherDivisor)
            PvrGl.Uniform4fv(shader.DitherDivisor, 1, values);
    }
}
