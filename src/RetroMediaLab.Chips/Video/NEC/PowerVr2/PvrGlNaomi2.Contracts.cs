using System.Runtime.InteropServices;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2;

internal sealed partial class PvrN2VertexSource : PvrOpenGlSource
{
}

internal abstract class PvrGlNaomi2Shader
{
    internal uint Program;
    internal int MvMat;
    internal int NormalMat;
    internal int ProjMat;
    internal int BumpMapping;
    internal int LightCount;
    internal int UseBaseOver;
    internal int BumpId0;
    internal int BumpId1;
    internal readonly int[] GlossCoef = new int[2];
    internal readonly int[] EnvMapping = new int[2];
    internal readonly int[] ConstantColor = new int[2];
    internal readonly int[] AmbientBase = new int[2];
    internal readonly int[] AmbientOffset = new int[2];
    internal readonly int[] AmbientMaterialBase = new int[2];
    internal readonly int[] AmbientMaterialOffset = new int[2];
    internal readonly PvrGlNaomi2LightUniforms[] Lights = new PvrGlNaomi2LightUniforms[16];
    internal int LastMvMat;
    internal int LastNormalMat;
    internal int LastProjMat;
    internal int LastLightModel;
}

internal unsafe struct PvrGlNaomi2LightUniforms
{
    internal int Color;
    internal int Direction;
    internal int Position;
    internal int Parallel;
    internal int Routing;
    internal int Dmode;
    internal int Smode;
    internal int DistAttnMode;
    internal int AttnDistA;
    internal int AttnDistB;
    internal int AttnAngleA;
    internal int AttnAngleB;
    internal fixed int Diffuse[2];
    internal fixed int Specular[2];
}

internal static unsafe partial class PvrGlNaomi2
{
    internal static void ResetN2UniformCache<T>(T shader) where T : PvrGlNaomi2Shader
    {
        shader.LastMvMat = -1;
        shader.LastProjMat = -1;
        shader.LastLightModel = -1;
        shader.LastNormalMat = -1;
    }

    internal static void InitN2Uniforms<T>(T shader) where T : PvrGlNaomi2Shader
    {
        shader.MvMat = PvrGl.GetUniformLocation(shader.Program, "mvMat");
        shader.NormalMat = PvrGl.GetUniformLocation(shader.Program, "normalMat");
        shader.ProjMat = PvrGl.GetUniformLocation(shader.Program, "projMat");
        shader.GlossCoef[0] = PvrGl.GetUniformLocation(shader.Program, "glossCoef[0]");
        shader.GlossCoef[1] = PvrGl.GetUniformLocation(shader.Program, "glossCoef[1]");
        shader.EnvMapping[0] = PvrGl.GetUniformLocation(shader.Program, "envMapping[0]");
        shader.EnvMapping[1] = PvrGl.GetUniformLocation(shader.Program, "envMapping[1]");
        shader.BumpMapping = PvrGl.GetUniformLocation(shader.Program, "bumpMapping");
        shader.ConstantColor[0] = PvrGl.GetUniformLocation(shader.Program, "constantColor[0]");
        shader.ConstantColor[1] = PvrGl.GetUniformLocation(shader.Program, "constantColor[1]");
        shader.LightCount = PvrGl.GetUniformLocation(shader.Program, "lightCount");
        shader.AmbientBase[0] = PvrGl.GetUniformLocation(shader.Program, "ambientBase[0]");
        shader.AmbientBase[1] = PvrGl.GetUniformLocation(shader.Program, "ambientBase[1]");
        shader.AmbientOffset[0] = PvrGl.GetUniformLocation(shader.Program, "ambientOffset[0]");
        shader.AmbientOffset[1] = PvrGl.GetUniformLocation(shader.Program, "ambientOffset[1]");
        shader.AmbientMaterialBase[0] = PvrGl.GetUniformLocation(shader.Program, "ambientMaterialBase[0]");
        shader.AmbientMaterialBase[1] = PvrGl.GetUniformLocation(shader.Program, "ambientMaterialBase[1]");
        shader.AmbientMaterialOffset[0] = PvrGl.GetUniformLocation(shader.Program, "ambientMaterialOffset[0]");
        shader.AmbientMaterialOffset[1] = PvrGl.GetUniformLocation(shader.Program, "ambientMaterialOffset[1]");
        shader.UseBaseOver = PvrGl.GetUniformLocation(shader.Program, "useBaseOver");
        shader.BumpId0 = PvrGl.GetUniformLocation(shader.Program, "bumpId0");
        shader.BumpId1 = PvrGl.GetUniformLocation(shader.Program, "bumpId1");
        for (int i = 0; i < shader.Lights.Length; i++)
        {
            ref PvrGlNaomi2LightUniforms light = ref shader.Lights[i];
            light.Color = PvrGl.GetUniformLocation(shader.Program, $"lights[{i}].color");
            light.Direction = PvrGl.GetUniformLocation(shader.Program, $"lights[{i}].direction");
            light.Position = PvrGl.GetUniformLocation(shader.Program, $"lights[{i}].position");
            light.Parallel = PvrGl.GetUniformLocation(shader.Program, $"lights[{i}].parallel");
            light.Diffuse[0] = PvrGl.GetUniformLocation(shader.Program, $"lights[{i}].diffuse[0]");
            light.Diffuse[1] = PvrGl.GetUniformLocation(shader.Program, $"lights[{i}].diffuse[1]");
            light.Specular[0] = PvrGl.GetUniformLocation(shader.Program, $"lights[{i}].specular[0]");
            light.Specular[1] = PvrGl.GetUniformLocation(shader.Program, $"lights[{i}].specular[1]");
            light.Routing = PvrGl.GetUniformLocation(shader.Program, $"lights[{i}].routing");
            light.Dmode = PvrGl.GetUniformLocation(shader.Program, $"lights[{i}].dmode");
            light.Smode = PvrGl.GetUniformLocation(shader.Program, $"lights[{i}].smode");
            light.DistAttnMode = PvrGl.GetUniformLocation(shader.Program, $"lights[{i}].distAttnMode");
            light.AttnDistA = PvrGl.GetUniformLocation(shader.Program, $"lights[{i}].attnDistA");
            light.AttnDistB = PvrGl.GetUniformLocation(shader.Program, $"lights[{i}].attnDistB");
            light.AttnAngleA = PvrGl.GetUniformLocation(shader.Program, $"lights[{i}].attnAngleA");
            light.AttnAngleB = PvrGl.GetUniformLocation(shader.Program, $"lights[{i}].attnAngleB");
        }
        ResetN2UniformCache(shader);
    }

    internal static void SetN2Uniforms<T>(in PolyParam polygon, T shader, RenderContext context) where T : PvrGlNaomi2Shader
    {
        if (polygon.MvMatrix != shader.LastMvMat)
        {
            shader.LastMvMat = polygon.MvMatrix;
            fixed (float* matrix = CollectionsMarshal.AsSpan(context.Matrices)[polygon.MvMatrix].Mat)
                PvrGl.UniformMatrix4fv(shader.MvMat, 1, 0, matrix);
        }
        if (polygon.NormalMatrix != shader.LastNormalMat)
        {
            shader.LastNormalMat = polygon.NormalMatrix;
            fixed (float* matrix = CollectionsMarshal.AsSpan(context.Matrices)[polygon.NormalMatrix].Mat)
                PvrGl.UniformMatrix4fv(shader.NormalMat, 1, 0, matrix);
        }
        if (polygon.ProjMatrix != shader.LastProjMat)
        {
            shader.LastProjMat = polygon.ProjMatrix;
            fixed (float* matrix = CollectionsMarshal.AsSpan(context.Matrices)[polygon.ProjMatrix].Mat)
                PvrGl.UniformMatrix4fv(shader.ProjMat, 1, 0, matrix);
        }
        for (int i = 0; i < 2; i++)
        {
            PvrGl.Uniform1f(shader.GlossCoef[i], polygon.GlossCoef[i]);
            PvrGl.Uniform1i(shader.EnvMapping[i], polygon.EnvMapping[i] ? 1 : 0);
            PvrGl.Uniform1i(shader.ConstantColor[i], polygon.ConstantColor[i] ? 1 : 0);
        }

        if (polygon.LightModel != shader.LastLightModel)
        {
            shader.LastLightModel = polygon.LightModel;
            ref N2LightModel model = ref CollectionsMarshal.AsSpan(context.LightModels)[polygon.LightModel];
            for (int volume = 0; volume < 2; volume++)
            {
                PvrGl.Uniform1i(shader.AmbientMaterialBase[volume], model.AmbientMaterialBase[volume]);
                PvrGl.Uniform1i(shader.AmbientMaterialOffset[volume], model.AmbientMaterialOffset[volume]);
                fixed (float* color = &model.AmbientBase[volume][0])
                    PvrGl.Uniform4fv(shader.AmbientBase[volume], 1, color);
                fixed (float* color = &model.AmbientOffset[volume][0])
                    PvrGl.Uniform4fv(shader.AmbientOffset[volume], 1, color);
            }

            PvrGl.Uniform1i(shader.UseBaseOver, model.UseBaseOver);
            PvrGl.Uniform1i(shader.BumpId0, model.BumpId1);
            PvrGl.Uniform1i(shader.BumpId1, model.BumpId2);
            PvrGl.Uniform1i(shader.LightCount, model.LightCount);
            for (int i = 0; i < model.LightCount; i++)
            {
                ref N2Light light = ref model.Lights[i];
                ref PvrGlNaomi2LightUniforms uniforms = ref shader.Lights[i];
                PvrGl.Uniform1i(uniforms.Parallel, light.Parallel);
                fixed (float* color = light.Color)
                    PvrGl.Uniform4fv(uniforms.Color, 1, color);
                fixed (float* direction = light.Direction)
                    PvrGl.Uniform4fv(uniforms.Direction, 1, direction);
                fixed (float* position = light.Position)
                    PvrGl.Uniform4fv(uniforms.Position, 1, position);

                for (int volume = 0; volume < 2; volume++)
                {
                    PvrGl.Uniform1i(uniforms.Diffuse[volume], light.Diffuse[volume]);
                    PvrGl.Uniform1i(uniforms.Specular[volume], light.Specular[volume]);
                }
                PvrGl.Uniform1i(uniforms.Routing, light.Routing);
                PvrGl.Uniform1i(uniforms.Dmode, light.Dmode);
                PvrGl.Uniform1i(uniforms.Smode, light.Smode);
                PvrGl.Uniform1i(uniforms.DistAttnMode, light.DistAttnMode);

                PvrGl.Uniform1f(uniforms.AttnDistA, light.AttnDistA);
                PvrGl.Uniform1f(uniforms.AttnDistB, light.AttnDistB);
                PvrGl.Uniform1f(uniforms.AttnAngleA, light.AttnAngleA);
                PvrGl.Uniform1f(uniforms.AttnAngleB, light.AttnAngleB);
            }
        }
        PvrGl.Uniform1i(shader.BumpMapping,
            polygon.Pcw.Texture == 1 && polygon.Tcw.PixelFmt == (uint)PixelFormat.PixelBumpMap ? 1 : 0);
    }
}
