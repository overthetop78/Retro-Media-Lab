using System.IO;
using System.IO.Hashing;
using System.Runtime.InteropServices;
using System.Text;
using SharpGen.Runtime;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.D3DCompiler;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Direct3D11;

internal sealed unsafe partial class PvrDx11Shaders
{
    internal const string VertexShader =
        "\n" +
        "#if pp_Gouraud == 1\n" +
        "#define INTERPOLATION\n" +
        "#else\n" +
        "#define INTERPOLATION nointerpolation\n" +
        "#endif\n" +
        "\n" +
        "struct VertexIn\n" +
        "{\n" +
        "\tfloat4 pos : POSITION;\n" +
        "\tfloat4 col : COLOR0;\n" +
        "\tfloat4 spec : COLOR1;\n" +
        "\tfloat2 uv : TEXCOORD0;\n" +
        "};\n" +
        "\n" +
        "struct VertexOut\n" +
        "{\n" +
        "\tfloat4 pos : SV_POSITION;\n" +
        "\tfloat4 uv : TEXCOORD0;\n" +
        "\tINTERPOLATION float4 col : COLOR0;\n" +
        "\tINTERPOLATION float4 spec : COLOR1;\n" +
        "};\n" +
        "\n" +
        "cbuffer constantBuffer : register(b0)\n" +
        "{\n" +
        "\tfloat4x4 transMatrix;\n" +
        "\tfloat4 leftPlane;\n" +
        "\tfloat4 topPlane;\n" +
        "\tfloat4 rightPlane;\n" +
        "\tfloat4 bottomPlane;\n" +
        "};\n" +
        "\n" +
        "[clipplanes(leftPlane, topPlane, rightPlane, bottomPlane)]\n" +
        "VertexOut main(in VertexIn vin)\n" +
        "{\n" +
        "\tVertexOut vo;\n" +
        "\tvo.pos = mul(transMatrix, float4(vin.pos.xyz, 1.f));\n" +
        "#if DIV_POS_Z == 1\n" +
        "\tvo.pos /= vo.pos.z;\n" +
        "\tvo.pos.z = vo.pos.w;\n" +
        "#endif\n" +
        "\tvo.col = vin.col;\n" +
        "\tvo.spec = vin.spec;\n" +
        "#if pp_Gouraud == 1 && DIV_POS_Z != 1\n" +
        "\tvo.col *= vo.pos.z;\n" +
        "\tvo.spec *= vo.pos.z;\n" +
        "#endif\n" +
        "\tvo.uv.xyz = float3(vin.uv, 0.f);\n" +
        "\n" +
        "#if DIV_POS_Z == 1\n" +
        "\tvo.uv.w = vo.pos.w;\n" +
        "#else\n" +
        "\tvo.uv.xy *= vo.pos.z;\n" +
        "\tvo.uv.w = vo.pos.z;\n" +
        "\tvo.pos.w = 1.f;\n" +
        "\tvo.pos.z = 0.f;\n" +
        "#endif\n" +
        "\n" +
        "\treturn vo;\n" +
        "}\n" +
        "\n";

    internal const string ModVolVertexShader =
        "\n" +
        "struct VertexIn\n" +
        "{\n" +
        "\tfloat4 pos : POSITION;\n" +
        "};\n" +
        "\n" +
        "struct VertexOut\n" +
        "{\n" +
        "\tfloat4 pos : SV_POSITION;\n" +
        "\tfloat4 uv : TEXCOORD0;\n" +
        "};\n" +
        "\n" +
        "cbuffer constantBuffer : register(b0)\n" +
        "{\n" +
        "\tfloat4x4 transMatrix;\n" +
        "\tfloat4 leftPlane;\n" +
        "\tfloat4 topPlane;\n" +
        "\tfloat4 rightPlane;\n" +
        "\tfloat4 bottomPlane;\n" +
        "};\n" +
        "\n" +
        "VertexOut main(in VertexIn vin)\n" +
        "{\n" +
        "\tVertexOut vo;\n" +
        "\tvo.pos = mul(transMatrix, float4(vin.pos.xyz, 1.f));\n" +
        "#if DIV_POS_Z == 1\n" +
        "\tvo.pos /= vo.pos.z;\n" +
        "\tvo.pos.z = vo.pos.w;\n" +
        "\tvo.uv = float4(0.f, 0.f, 0.f, vo.pos.w);\n" +
        "#else\n" +
        "\tvo.uv = float4(0.f, 0.f, 0.f, vo.pos.z);\n" +
        "\tvo.pos.w = 1.f;\n" +
        "\tvo.pos.z = 0.f;\n" +
        "#endif\n" +
        "\treturn vo;\n" +
        "}\n" +
        "\n";

    internal const string PixelShaderCommon =
        "\n" +
        "#if pp_Gouraud == 1\n" +
        "#define INTERPOLATION\n" +
        "#else\n" +
        "#define INTERPOLATION nointerpolation\n" +
        "#endif\n" +
        "\n" +
        "#define PI 3.1415926f\n" +
        "\n" +
        "Texture2D texture0 : register(t0);\n" +
        "sampler sampler0 : register(s0);\n" +
        "\n" +
        "Texture2D paletteTexture : register(t1);\n" +
        "sampler paletteSampler : register(s1);\n" +
        "\n" +
        "Texture2D fogTexture : register(t2);\n" +
        "sampler fogSampler : register(s2);\n" +
        "\n" +
        "float fog_mode2(float w)\n" +
        "{\n" +
        "\tfloat z = clamp(\n" +
        "#if DIV_POS_Z == 1\n" +
        "\t\t\t\t\tfogDensity / w\n" +
        "#else\n" +
        "\t\t\t\t\tfogDensity * w\n" +
        "#endif\n" +
        "\t\t\t\t\t\t\t\t\t, 1.0f, 255.9999f);\n" +
        "\tfloat exp = floor(log2(z));\n" +
        "\tfloat m = z * 16.0f / pow(2.0, exp) - 16.0f;\n" +
        "\tfloat idx = floor(m) + exp * 16.0f + 0.5f;\n" +
        "\tfloat4 fogCoef = fogTexture.Sample(fogSampler, float2(idx / 128.0f, 0.75f - (m - floor(m)) / 2.0f));\n" +
        "\treturn fogCoef.a;\n" +
        "}\n" +
        "\n" +
        "float4 clampColor(float4 color)\n" +
        "{\n" +
        "#if FogClamping == 1\n" +
        "\treturn clamp(color, colorClampMin, colorClampMax);\n" +
        "#else\n" +
        "\treturn color;\n" +
        "#endif\n" +
        "}\n" +
        "\n" +
        "#if pp_Palette != 0\n" +
        "\n" +
        "float4 getPaletteEntry(float colIdx)\n" +
        "{\n" +
        "\tuint colorIdx = int(floor(colIdx * 255.0f + 0.5f) + paletteIndex);\n" +
        "    float2 c = float2((fmod(float(colorIdx), 32.0f) * 2.0f + 1.0f) / 64.0f, (float(colorIdx / 32) * 2.0f + 1.0f) / 64.0f);\n" +
        "\treturn paletteTexture.Sample(paletteSampler, c);\n" +
        "}\n" +
        "\n" +
        "#endif\n" +
        "\n" +
        "#if pp_Palette == 1\n" +
        "\n" +
        "float4 palettePixel(Texture2D tex, sampler texSampler, float2 coords)\n" +
        "{\n" +
        "\treturn getPaletteEntry(tex.Sample(texSampler, coords).a);\n" +
        "}\n" +
        "\n" +
        "#elif pp_Palette == 2\n" +
        "\n" +
        "float4 palettePixelBilinear(Texture2D tex, sampler texSampler, float2 coords)\n" +
        "{\n" +
        "\tfloat2 textureSize;\n" +
        "\ttex.GetDimensions(textureSize.x, textureSize.y);\n" +
        "\tfloat2 pixCoord = coords * textureSize - 0.5f;\t\t\t\t// coordinates of top left pixel\n" +
        "\tfloat2 originPixCoord = floor(pixCoord);\n" +
        "\n" +
        "\tfloat2 sampleUV = (originPixCoord + 0.5f) / textureSize;\t// UV coordinates of center of top left pixel\n" +
        "\n" +
        "    // Sample from all surrounding texels\n" +
        "    float4 c00 = getPaletteEntry(tex.Sample(texSampler, sampleUV).a);\n" +
        "    float4 c01 = getPaletteEntry(tex.Sample(texSampler, sampleUV, int2(0, 1)).a);\n" +
        "    float4 c11 = getPaletteEntry(tex.Sample(texSampler, sampleUV, int2(1, 1)).a);\n" +
        "    float4 c10 = getPaletteEntry(tex.Sample(texSampler, sampleUV, int2(1, 0)).a);\n" +
        "\n" +
        "\tfloat2 weight = pixCoord - originPixCoord;\n" +
        "\n" +
        "    // Bi-linear mixing\n" +
        "    float4 temp0 = lerp(c00, c10, weight.x);\n" +
        "    float4 temp1 = lerp(c01, c11, weight.x);\n" +
        "    return lerp(temp0, temp1, weight.y);\n" +
        "}\n" +
        "\n" +
        "#endif\n" +
        "\n";

    internal const string PixelShader =
        "\n" +
        "\n" +
        "cbuffer constantBuffer : register(b0)\n" +
        "{\n" +
        "\tfloat4 colorClampMin;\n" +
        "\tfloat4 colorClampMax;\n" +
        "\tfloat4 FOG_COL_VERT;\n" +
        "\tfloat4 FOG_COL_RAM;\n" +
        "\tfloat4 ditherDivisor;\n" +
        "\tfloat fogDensity;\n" +
        "\tfloat shadowScale;\n" +
        "\tfloat alphaTestValue;\n" +
        "};\n" +
        "\n" +
        "cbuffer polyConstantBuffer : register(b1)\n" +
        "{\n" +
        "\tfloat4 clipTest;\n" +
        "\tfloat paletteIndex;\n" +
        "\tfloat trilinearAlpha;\n" +
        "};\n" +
        "\n" +
        "#include \"pixel_common.hlsl\"\n" +
        "\n" +
        "struct Pixel \n" +
        "{\n" +
        "\tfloat4 pos : SV_POSITION;\n" +
        "\tfloat4 uv : TEXCOORD0;\n" +
        "\tINTERPOLATION float4 col : COLOR0;\n" +
        "\tINTERPOLATION float4 spec : COLOR1;\n" +
        "};\n" +
        "\n" +
        "struct PSO\n" +
        "{\n" +
        "\tfloat4 col : SV_TARGET;\n" +
        "\tfloat z : SV_DEPTH;\n" +
        "};\n" +
        "\n" +
        "PSO main(in Pixel inpix)\n" +
        "{\n" +
        "#if pp_ClipInside == 1\n" +
        "\t// Clip inside the box\n" +
        "\tif (inpix.pos.x >= clipTest.x && inpix.pos.x <= clipTest.z\n" +
        "\t\t\t&& inpix.pos.y >= clipTest.y && inpix.pos.y <= clipTest.w)\n" +
        "\t\tdiscard;\n" +
        "#endif\n" +
        "\tfloat4 color = inpix.col;\n" +
        "\t#if pp_BumpMap == 1 || pp_Offset == 1\n" +
        "\t\tfloat4 specular = inpix.spec;\n" +
        "\t#endif\n" +
        "\t#if pp_Gouraud == 1 && DIV_POS_Z != 1\n" +
        "\t\tcolor /= inpix.uv.w;\n" +
        "\t\t#if pp_BumpMap == 1 || pp_Offset == 1\n" +
        "\t\t\tspecular /= inpix.uv.w;\n" +
        "\t\t#endif\n" +
        "\t#endif\n" +
        "\t#if pp_UseAlpha == 0\n" +
        "\t\tcolor.a = 1.0f;\n" +
        "\t#endif\n" +
        "\t#if pp_FogCtrl == 3\n" +
        "\t\tcolor = float4(FOG_COL_RAM.rgb, fog_mode2(inpix.uv.w));\n" +
        "\t#endif\n" +
        "\t#if pp_Texture == 1\n" +
        "\t{\n" +
        "\t\tfloat2 uv = inpix.uv.xy;\n" +
        "\t\t#if DIV_POS_Z != 1\n" +
        "\t\t\tuv /= inpix.uv.w;\n" +
        "\t\t#endif\n" +
        "\t\t#if pp_Palette == 0\n" +
        "\t\t\tfloat4 texcol = texture0.Sample(sampler0, uv);\n" +
        "\t\t#elif pp_Palette == 1\n" +
        "\t\t\tfloat4 texcol = palettePixel(texture0, sampler0, uv);\n" +
        "\t\t#else\n" +
        "\t\t\tfloat4 texcol = palettePixelBilinear(texture0, sampler0, uv);\n" +
        "\t\t#endif\n" +
        "\t\t\n" +
        "\t\t#if pp_BumpMap == 1\n" +
        "\t\t\tfloat s = PI / 2.0f * (texcol.a * 15.0f * 16.0f + texcol.r * 15.0f) / 255.0f;\n" +
        "\t\t\tfloat r = 2.0f * PI * (texcol.g * 15.0f * 16.0f + texcol.b * 15.0f) / 255.0f;\n" +
        "\t\t\ttexcol[3] = clamp(specular.a + specular.r * sin(s) + specular.g * cos(s) * cos(r - 2.0f * PI * specular.b), 0.0f, 1.0f);\n" +
        "\t\t\ttexcol.rgb = float3(1.0f, 1.0f, 1.0f);\t\n" +
        "\t\t#else\n" +
        "\t\t\t#if pp_IgnoreTexA == 1\n" +
        "\t\t\t\ttexcol.a = 1.0f;\n" +
        "\t\t\t#endif\n" +
        "\t\t#endif\n" +
        "\t\t#if pp_ShadInstr == 0\n" +
        "\t\t\tcolor = texcol;\n" +
        "\t\t#endif\n" +
        "\t\t#if pp_ShadInstr == 1\n" +
        "\t\t\tcolor.rgb *= texcol.rgb;\n" +
        "\t\t\tcolor.a = texcol.a;\n" +
        "\t\t#endif\n" +
        "\t\t#if pp_ShadInstr == 2\n" +
        "\t\t\tcolor.rgb = lerp(color.rgb, texcol.rgb, texcol.a);\n" +
        "\t\t#endif\n" +
        "\t\t#if  pp_ShadInstr == 3\n" +
        "\t\t\tcolor *= texcol;\n" +
        "\t\t#endif\n" +
        "\t\t\n" +
        "\t\t#if pp_Offset == 1 && pp_BumpMap == 0\n" +
        "\t\t\tcolor.rgb += specular.rgb;\n" +
        "\t\t#endif\n" +
        "\t}\n" +
        "\t#endif\n" +
        "\t\n" +
        "\tcolor = clampColor(color);\n" +
        "\t\n" +
        "\t#if pp_FogCtrl == 0\n" +
        "\t\tcolor.rgb = lerp(color.rgb, FOG_COL_RAM.rgb, fog_mode2(inpix.uv.w)); \n" +
        "\t#endif\n" +
        "\t#if pp_FogCtrl == 1 && pp_Offset == 1 && pp_BumpMap == 0\n" +
        "\t\tcolor.rgb = lerp(color.rgb, FOG_COL_VERT.rgb, specular.a);\n" +
        "\t#endif\n" +
        "\t\n" +
        "\t#if pp_TriLinear == 1\n" +
        "\tcolor *= trilinearAlpha;\n" +
        "\t#endif\n" +
        "\n" +
        "\t#if cp_AlphaTest == 1\n" +
        "\t\tcolor.a = round(color.a * 255.0f) * 0.0039215688593685626983642578125; // 1 / 255\n" +
        "\t\tif (alphaTestValue > color.a)\n" +
        "\t\t\tdiscard;\n" +
        "\t\tcolor.a = 1.0f;\n" +
        "\t#endif\n" +
        "\n" +
        "#if DITHERING == 1\n" +
        "\tstatic const float ditherTable[16] = {\n" +
        "\t\t5.0f, 13.0f,  7.0f, 15.0f,\n" +
        "\t\t9.0f,  1.0f, 11.0f,  3.0f,\n" +
        "\t\t6.0f, 14.0f,  4.0f, 12.0f,\n" +
        "\t\t10.0f, 2.0f,  8.0f,  0.0f\n" +
        "\t};\n" +
        "\tfloat r = ditherTable[int(inpix.pos.y % 4.0f) * 4 + int(inpix.pos.x % 4.0f)];\n" +
        "\tfloat4 dv = float4(r, r, r, 1.0f) / ditherDivisor;\n" +
        "\tcolor = clamp(floor(color * 255.0f + dv) / 255.0f, 0.0f, 1.0f);\n" +
        "#endif\n" +
        "\tPSO pso;\n" +
        "#if DIV_POS_Z == 1\n" +
        "\tfloat w = 100000.0f / inpix.uv.w;\n" +
        "#else\n" +
        "\tfloat w = 100000.0f * inpix.uv.w;\n" +
        "#endif\n" +
        "\tpso.z = log2(1.0f + max(w, -0.999999f)) / 34.0f;\n" +
        "\tpso.col = color;\n" +
        "\n" +
        "\treturn pso;\n" +
        "}\n" +
        "\n" +
        "struct MVPixel \n" +
        "{\n" +
        "\tfloat4 pos : SV_POSITION;\n" +
        "\tfloat4 uv : TEXCOORD0;\n" +
        "};\n" +
        "\n" +
        "PSO modifierVolume(in MVPixel inpix)\n" +
        "{\n" +
        "\tPSO pso;\n" +
        "#if DIV_POS_Z == 1\n" +
        "\tfloat w = 100000.0f / inpix.uv.w;\n" +
        "#else\n" +
        "\tfloat w = 100000.0f * inpix.uv.w;\n" +
        "#endif\n" +
        "\tpso.z = log2(1.0f + max(w, -0.999999f)) / 34.0f;\n" +
        "\tpso.col = float4(0, 0, 0, 1.f - shadowScale);\n" +
        "\n" +
        "\treturn pso;\n" +
        "}\n";

    internal const string QuadVertexShader =
        "\n" +
        "struct VertexIn\n" +
        "{\n" +
        "\tfloat2 pos : POSITION;\n" +
        "\tfloat2 uv : TEXCOORD0;\n" +
        "};\n" +
        "\n" +
        "struct VertexOut\n" +
        "{\n" +
        "\tfloat4 pos : SV_POSITION;\n" +
        "\tfloat2 uv : TEXCOORD0;\n" +
        "};\n" +
        "\n" +
        "VertexOut main(in VertexIn vin)\n" +
        "{\n" +
        "\tVertexOut vo;\n" +
        "#if ROTATE == 0\n" +
        "\tvo.pos = float4(vin.pos, 0.f, 1.f);\n" +
        "#else\n" +
        "\tvo.pos = float4(-vin.pos.y, vin.pos.x, 0.f, 1.f);\n" +
        "#endif\n" +
        "\tvo.uv = vin.uv;\n" +
        "\n" +
        "\treturn vo;\n" +
        "}\n";

    internal const string QuadPixelShader =
        "\n" +
        "cbuffer constantBuffer : register(b0)\n" +
        "{\n" +
        "\tfloat4 color;\n" +
        "};\n" +
        "\n" +
        "struct VertexIn\n" +
        "{\n" +
        "\tfloat4 pos : SV_POSITION;\n" +
        "\tfloat2 uv : TEXCOORD0;\n" +
        "};\n" +
        "\n" +
        "sampler sampler0;\n" +
        "Texture2D texture0;\n" +
        "\n" +
        "float4 main(in VertexIn vin) : SV_Target\n" +
        "{\n" +
        "\treturn color * texture0.Sample(sampler0, vin.uv);\n" +
        "}\n" +
        "\n";

    private sealed class IncludeManager : CallbackBase, Include
    {
        public Stream Open(IncludeType includeType, string fileName, Stream? parentStream)
        {
            if (fileName == "pixel_common.hlsl")
                return new MemoryStream(Encoding.UTF8.GetBytes(PixelShaderCommon), writable: false);

            throw new SharpGenException(Result.Fail);
        }

        public void Close(Stream data) => data.Dispose();
    }

    private static readonly string[] MacroValues = ["0", "1", "2", "3"];

    private enum VertexMacro
    {
        Gouraud,
        DivPosZ,
        PositionOnly,
        TwoVolumes,
        LightOn,
        ModifierVolume
    }

    private static readonly ShaderMacro[] VertexMacros =
    [
        new("pp_Gouraud", "1"),
        new("DIV_POS_Z", "0"),
        new("POSITION_ONLY", "0"),
        new("pp_TwoVolumes", "0"),
        new("LIGHT_ON", "1"),
        new("MODIFIER_VOLUME", "0"),
        new(null, null)
    ];

    private enum PixelMacro
    {
        Gouraud,
        DivPosZ,
        Texture,
        UseAlpha,
        IgnoreTexA,
        ShadInstr,
        Offset,
        FogCtrl,
        BumpMap,
        FogClamping,
        TriLinear,
        Palette,
        AlphaTest,
        ClipInside,
        Dithering
    }

    private static readonly ShaderMacro[] PixelMacros =
    [
        new("pp_Gouraud", "1"),
        new("DIV_POS_Z", "0"),
        new("pp_Texture", "0"),
        new("pp_UseAlpha", "0"),
        new("pp_IgnoreTexA", "0"),
        new("pp_ShadInstr", "0"),
        new("pp_Offset", "0"),
        new("pp_FogCtrl", "0"),
        new("pp_BumpMap", "0"),
        new("FogClamping", "0"),
        new("pp_TriLinear", "0"),
        new("pp_Palette", "0"),
        new("cp_AlphaTest", "0"),
        new("pp_ClipInside", "0"),
        new("DITHERING", "0"),
        new(null, null)
    ];

    internal ID3D11PixelShader? GetShader(
        bool texture,
        bool useAlpha,
        bool ignoreTexA,
        uint shadInstr,
        bool offset,
        uint fogCtrl,
        bool bumpMap,
        bool fogClamping,
        bool trilinear,
        int palette,
        bool gouraud,
        bool alphaTest,
        bool clipInside,
        bool dithering)
    {
        bool divPosZ = !PvrConfiguration.IsNaomi2 && PvrConfiguration.NativeDepthInterpolation;
        uint hash = (texture ? 1u : 0u)
            | ((useAlpha ? 1u : 0u) << 1)
            | ((ignoreTexA ? 1u : 0u) << 2)
            | (shadInstr << 3)
            | ((offset ? 1u : 0u) << 5)
            | (fogCtrl << 6)
            | ((bumpMap ? 1u : 0u) << 8)
            | ((fogClamping ? 1u : 0u) << 9)
            | ((trilinear ? 1u : 0u) << 10)
            | ((uint)palette << 11)
            | ((gouraud ? 1u : 0u) << 13)
            | ((alphaTest ? 1u : 0u) << 14)
            | ((clipInside ? 1u : 0u) << 15)
            | ((divPosZ ? 1u : 0u) << 16)
            | ((dithering ? 1u : 0u) << 17);

        _shaders.TryGetValue(hash, out ID3D11PixelShader? shader);
        if (shader == null)
        {
            PvrRuntime.Verify(shadInstr < MacroValues.Length);
            PvrRuntime.Verify(fogCtrl < MacroValues.Length);
            PixelMacros[(int)PixelMacro.Gouraud].Definition = MacroValues[gouraud ? 1 : 0];
            PixelMacros[(int)PixelMacro.Texture].Definition = MacroValues[texture ? 1 : 0];
            PixelMacros[(int)PixelMacro.UseAlpha].Definition = MacroValues[useAlpha ? 1 : 0];
            PixelMacros[(int)PixelMacro.IgnoreTexA].Definition = MacroValues[ignoreTexA ? 1 : 0];
            PixelMacros[(int)PixelMacro.ShadInstr].Definition = MacroValues[shadInstr];
            PixelMacros[(int)PixelMacro.Offset].Definition = MacroValues[offset ? 1 : 0];
            PixelMacros[(int)PixelMacro.FogCtrl].Definition = MacroValues[fogCtrl];
            PixelMacros[(int)PixelMacro.BumpMap].Definition = MacroValues[bumpMap ? 1 : 0];
            PixelMacros[(int)PixelMacro.FogClamping].Definition = MacroValues[fogClamping ? 1 : 0];
            PixelMacros[(int)PixelMacro.TriLinear].Definition = MacroValues[trilinear ? 1 : 0];
            PixelMacros[(int)PixelMacro.Palette].Definition = MacroValues[palette];
            PixelMacros[(int)PixelMacro.AlphaTest].Definition = MacroValues[alphaTest ? 1 : 0];
            PixelMacros[(int)PixelMacro.ClipInside].Definition = MacroValues[clipInside ? 1 : 0];
            PixelMacros[(int)PixelMacro.DivPosZ].Definition = MacroValues[divPosZ ? 1 : 0];
            PixelMacros[(int)PixelMacro.Dithering].Definition = MacroValues[dithering ? 1 : 0];

            _shaders[hash] = shader = CompilePS(PixelShader, "main", PixelMacros);
            PvrRuntime.Verify(shader != null);
        }
        return shader;
    }

    internal ID3D11VertexShader? GetVertexShader(bool gouraud, bool naomi2)
    {
        bool divPosZ = !PvrConfiguration.IsNaomi2 && PvrConfiguration.NativeDepthInterpolation;
        int index = (gouraud ? 1 : 0) | ((naomi2 ? 1 : 0) << 1) | ((divPosZ ? 1 : 0) << 2);
        ref ID3D11VertexShader? vertexShader = ref _vertexShaders[index];
        if (vertexShader == null)
        {
            VertexMacros[(int)VertexMacro.Gouraud].Definition = MacroValues[gouraud ? 1 : 0];
            if (!naomi2)
            {
                VertexMacros[(int)VertexMacro.DivPosZ].Definition = MacroValues[divPosZ ? 1 : 0];
                vertexShader = CompileVS(VertexShader, "main", VertexMacros);
            }
            else
            {
                VertexMacros[(int)VertexMacro.PositionOnly].Definition = MacroValues[0];
                VertexMacros[(int)VertexMacro.TwoVolumes].Definition = MacroValues[0];
                VertexMacros[(int)VertexMacro.LightOn].Definition = MacroValues[1];
                VertexMacros[(int)VertexMacro.ModifierVolume].Definition = MacroValues[0];
                string source = PvrDx11Naomi2.DX11N2VertexShader + "\n" + PvrDx11Naomi2.DX11N2ColorShader;
                vertexShader = CompileVS(source, "main", VertexMacros);
            }
        }

        return vertexShader;
    }

    internal ID3D11VertexShader? GetMVVertexShader(bool naomi2)
    {
        bool divPosZ = !PvrConfiguration.IsNaomi2 && PvrConfiguration.NativeDepthInterpolation;
        int index = (naomi2 ? 1 : 0) | ((divPosZ ? 1 : 0) << 1);
        if (_modVolVertexShaders[index] == null)
        {
            if (!naomi2)
            {
                VertexMacros[(int)VertexMacro.DivPosZ].Definition = MacroValues[divPosZ ? 1 : 0];
                _modVolVertexShaders[index] = CompileVS(ModVolVertexShader, "main", VertexMacros);
            }
            else
            {
                VertexMacros[(int)VertexMacro.Gouraud].Definition = MacroValues[0];
                VertexMacros[(int)VertexMacro.PositionOnly].Definition = MacroValues[1];
                VertexMacros[(int)VertexMacro.TwoVolumes].Definition = MacroValues[0];
                VertexMacros[(int)VertexMacro.LightOn].Definition = MacroValues[0];
                VertexMacros[(int)VertexMacro.ModifierVolume].Definition = MacroValues[1];
                _modVolVertexShaders[index] = CompileVS(PvrDx11Naomi2.DX11N2VertexShader, "main", VertexMacros);
            }
        }

        return _modVolVertexShaders[index];
    }

    internal ID3D11PixelShader? GetModVolShader()
    {
        if (_modVolShader == null)
            _modVolShader = CompilePS(PixelShader, "modifierVolume", PixelMacros);

        return _modVolShader;
    }

    internal ID3D11VertexShader? GetQuadVertexShader(bool rotate)
    {
        ref ID3D11VertexShader? shader = ref (rotate ? ref _quadRotateVertexShader : ref _quadVertexShader);
        if (shader == null)
        {
            ShaderMacro[] macros = [new("ROTATE", rotate ? "1" : "0"), new(null, null)];
            shader = CompileVS(QuadVertexShader, "main", macros);
        }

        return shader;
    }

    internal ID3D11PixelShader? GetQuadPixelShader()
    {
        if (_quadPixelShader == null)
            _quadPixelShader = CompilePS(QuadPixelShader, "main", null);

        return _quadPixelShader;
    }

    private Blob? CompileShader(string source, string function, string profile, ShaderMacro[]? defines)
    {
        ulong hash = HashShader(source, function, profile, defines);

        if (!LookupShader(hash, out Blob? shaderBlob))
        {
            using var includeManager = new IncludeManager();
            byte[] input = Encoding.UTF8.GetBytes(source);
            fixed (byte* sourcePointer = input)
            {
                Result result = _d3dCompile!((IntPtr)sourcePointer, (nuint)input.Length, null, defines, includeManager,
                    function, profile, 0, 0, out shaderBlob, out Blob? errorBlob);
                using (errorBlob)
                {
                    if (result.Failure)
                        PvrRuntime.Log.Error("Shader compilation failed: {0}", Marshal.PtrToStringAnsi(errorBlob!.BufferPointer));
                    else
                        CacheShader(hash, shaderBlob!);
                }
            }
        }

        return shaderBlob;
    }

    private ID3D11VertexShader? CompileVS(string source, string function, ShaderMacro[]? defines)
    {
        using Blob? blob = CompileShader(source, function, "vs_4_0", defines);
        ID3D11VertexShader? shader = null;
        if (blob != null)
        {
            if (_device!.CreateVertexShader(blob.BufferPointer, blob.BufferSize, null, out shader).Failure)
                PvrRuntime.Log.Error("Vertex shader creation failed");
        }

        return shader;
    }

    private ID3D11PixelShader? CompilePS(string source, string function, ShaderMacro[]? defines)
    {
        using Blob? blob = CompileShader(source, function, "ps_4_0", defines);
        ID3D11PixelShader? shader = null;
        if (blob != null)
        {
            if (_device!.CreatePixelShader(blob.BufferPointer, blob.BufferSize, null, out shader).Failure)
                PvrRuntime.Log.Error("Pixel shader creation failed");
        }

        return shader;
    }

    internal Blob? GetVertexShaderBlob()
    {
        VertexMacros[(int)VertexMacro.Gouraud].Definition = MacroValues[1];
        VertexMacros[(int)VertexMacro.PositionOnly].Definition = MacroValues[0];
        VertexMacros[(int)VertexMacro.TwoVolumes].Definition = MacroValues[0];
        VertexMacros[(int)VertexMacro.ModifierVolume].Definition = MacroValues[0];
        string source = PvrDx11Naomi2.DX11N2VertexShader + "\n" + PvrDx11Naomi2.DX11N2ColorShader;
        return CompileShader(source, "main", "vs_4_0", VertexMacros);
    }

    internal Blob? GetMVVertexShaderBlob()
    {
        VertexMacros[(int)VertexMacro.Gouraud].Definition = MacroValues[0];
        VertexMacros[(int)VertexMacro.PositionOnly].Definition = MacroValues[1];
        VertexMacros[(int)VertexMacro.TwoVolumes].Definition = MacroValues[0];
        VertexMacros[(int)VertexMacro.ModifierVolume].Definition = MacroValues[1];
        return CompileShader(PvrDx11Naomi2.DX11N2VertexShader, "main", "vs_4_0", VertexMacros);
    }

    internal Blob? GetQuadVertexShaderBlob() => CompileShader(QuadVertexShader, "main", "vs_4_0", null);

    internal void Init(ID3D11Device device, PvrDx11ShaderCompiler d3dCompile)
    {
        device.AddRef();
        _device = new ID3D11Device(device.NativePointer);
        _d3dCompile = d3dCompile;
        EnableCache(!PvrDx11Context.Instance.HasShaderCache());
        LoadCache(CacheFile);
    }

    internal void Term()
    {
        try
        {
            SaveCache(CacheFile);
        }
        finally
        {
            foreach (ID3D11PixelShader? shader in _shaders.Values)
                shader?.Dispose();
            _shaders.Clear();
            for (int i = 0; i < _vertexShaders.Length; i++)
            {
                _vertexShaders[i]?.Dispose();
                _vertexShaders[i] = null;
            }
            _modVolShader?.Dispose();
            _modVolShader = null;
            for (int i = 0; i < _modVolVertexShaders.Length; i++)
            {
                _modVolVertexShaders[i]?.Dispose();
                _modVolVertexShaders[i] = null;
            }
            _quadVertexShader?.Dispose();
            _quadVertexShader = null;
            _quadRotateVertexShader?.Dispose();
            _quadRotateVertexShader = null;
            _quadPixelShader?.Dispose();
            _quadPixelShader = null;
            _device?.Dispose();
            _device = null;
        }
    }
}

internal unsafe partial class PvrDx11CachedShaders
{
    protected void SaveCache(string filename)
    {
        if (!_enabled)
            return;
        string path = PvrRuntime.GetShaderCachePath(filename);
        FileStream file;
        try
        {
            file = File.Open(path, FileMode.Create, FileAccess.Write);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            PvrRuntime.Log.Warn("Cannot save shader cache to {0}", path);
            return;
        }
        using (file)
        using (var stream = new BinaryWriter(file, Encoding.UTF8, leaveOpen: true))
        {
            foreach (var pair in _shaderCache)
            {
                try
                {
                    stream.Write(pair.Key);
                    stream.Write(pair.Value.Size);
                    stream.Write(pair.Value.Blob, 0, (int)pair.Value.Size);
                }
                catch (IOException)
                {
                    PvrRuntime.Log.Warn("Error saving shader cache to {0}", path);
                    break;
                }
            }
            PvrRuntime.Log.Notice("Saved {0} shaders to {1}", _shaderCache.Count, path);
        }
    }

    protected void LoadCache(string filename)
    {
        if (!_enabled)
            return;
        string path = PvrRuntime.GetShaderCachePath(filename);
        FileStream file;
        try
        {
            file = File.Open(path, FileMode.Open, FileAccess.Read);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return;
        }
        using (file)
        using (var stream = new BinaryReader(file, Encoding.UTF8, leaveOpen: true))
        {
            while (true)
            {
                try
                {
                    ulong hash = stream.ReadUInt64();
                    uint size = stream.ReadUInt32();
                    byte[] blob = stream.ReadBytes((int)size);
                    if (blob.Length != size)
                        break;
                    _shaderCache[hash] = new ShaderBlob(size, blob);
                }
                catch (IOException)
                {
                    break;
                }
            }
        }
        PvrRuntime.Log.Notice("Loaded {0} shaders from {1}", _shaderCache.Count, path);
    }

    protected bool LookupShader(ulong hash, out Blob? blob)
    {
        blob = null;
        if (!_enabled)
            return false;

        if (!_shaderCache.TryGetValue(hash, out ShaderBlob item))
            return false;

        Compiler.D3DCreateBlob(item.Size, out blob);
        item.Blob.AsSpan().CopyTo(new Span<byte>((void*)blob!.BufferPointer, (int)item.Size));

        return true;
    }

    protected void CacheShader(ulong hash, Blob blob)
    {
        if (!_enabled)
            return;
        uint size = (uint)blob.BufferSize;
        byte[] data = new ReadOnlySpan<byte>((void*)blob.BufferPointer, (int)size).ToArray();
        _shaderCache[hash] = new ShaderBlob(size, data);
    }

    protected ulong HashShader(string source, string function, string profile,
        ShaderMacro[]? defines = null, string? includeFile = null)
    {
        if (!_enabled)
            return 0;

        var xxh = new XxHash3();
        xxh.Reset();
        xxh.Append(Encoding.UTF8.GetBytes(source));
        xxh.Append(Encoding.UTF8.GetBytes(function));
        if (defines != null)
            foreach (ShaderMacro define in defines)
            {
                if (define.Name == null)
                    break;
                xxh.Append(Encoding.UTF8.GetBytes(define.Name));
                xxh.Append(Encoding.UTF8.GetBytes(define.Definition));
            }
        if (includeFile != null)
            xxh.Append(Encoding.UTF8.GetBytes(includeFile));
        ulong hash = xxh.GetCurrentHashAsUInt64();

        return hash;
    }
}
