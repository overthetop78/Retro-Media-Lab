using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Vortice.Direct3D9;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Direct3D9;

internal sealed unsafe partial class PvrDx9Shaders
{
    private const uint ShaderDebug = 0;
    private const string VertexShader = """

struct VertexIn
{
	float4 pos : POSITION;
	float4 col : COLOR0;
	float4 spec : COLOR1;
	float2 uv : TEXCOORD0;
};

struct VertexOut
{
	float4 pos : POSITION;
	float4 uv : TEXCOORD0;
	float4 col : COLOR0;
	float4 spec : COLOR1;
};

float4x4 transMatrix : register(c0);

VertexOut main(in VertexIn vin)
{
	VertexOut vo;
	vo.pos = mul(transMatrix, float4(vin.pos.xyz, 1.f));
#if DIV_POS_Z == 1
	vo.pos /= vo.pos.z;
	vo.pos.z = vo.pos.w;
#endif
	vo.col = vin.col;
	vo.spec = vin.spec;
#if pp_Gouraud == 1 && DIV_POS_Z != 1
	vo.col *= vo.pos.z;
	vo.spec *= vo.pos.z;
#endif
	vo.uv = float4(vin.uv, 0.f, vo.pos.z);

#if DIV_POS_Z != 1
	vo.uv.xy *= vo.pos.z;
	vo.pos.w = 1.f;
	vo.pos.z = 0.f;
#endif

	return vo;
}


""";

    private const string PixelShader = """


#define PI 3.1415926f

struct pixel 
{
	float2 pos : VPOS;
	float4 uv : TEXCOORD0;
	float4 col : COLOR0;
#if pp_BumpMap == 1 || pp_Offset == 1
	float4 spec : COLOR1;
#endif
	
};

sampler2D samplr : register(s0);
sampler2D tex_pal : register(s1);
sampler2D fog_table : register(s2);

float4 paletteIndex : register(c0);
float4 FOG_COL_VERT : register(c1);
float4 FOG_COL_RAM : register(c2);
float4 FOG_DENSITY_SCALE : register(c3);
float4 clipTest : register(c4);
float4 trilinearAlpha : register(c5);
float4 colorClampMin : register(c6);
float4 colorClampMax : register(c7);
float4 ditherDivisor : register(c8);
float4 textureSize : register(c9);

float fog_mode2(float w)
{
	float z = clamp(
#if DIV_POS_Z == 1
					FOG_DENSITY_SCALE.x / w
#else
					FOG_DENSITY_SCALE.x * w
#endif
											, 1.0f, 255.9999f);
	float exp = floor(log2(z));
	float m = z * 16.0f / pow(2.0, exp) - 16.0f;
	float idx = floor(m) + exp * 16.0f + 0.5f;
	float4 fogCoef = tex2D(fog_table, float2(idx / 128.0f, 0.75f - (m - floor(m)) / 2.0f));
	return fogCoef.a;
}

float4 clampColor(float4 color)
{
#if FogClamping == 1
	return clamp(color, colorClampMin, colorClampMax);
#else
	return color;
#endif
}

#if pp_Palette != 0

float4 getPaletteEntry(float colIdx)
{
	int colorIdx = int(floor(colIdx * 255.0f + 0.5f) + paletteIndex.x);
    float2 c = float2((fmod(float(colorIdx), 32.0f) * 2.0f + 1.0f) / 64.0f, (float(colorIdx / 32) * 2.0f + 1.0f) / 64.0f);
	return tex2D(tex_pal, c);
}

#endif

#if pp_Palette == 1

float4 palettePixel(float4 coords)
{
#if DIV_POS_Z == 1
	return getPaletteEntry(tex2D(samplr, coords.xy).a);
#else
	return getPaletteEntry(tex2Dproj(samplr, coords).a);
#endif
}

#elif pp_Palette == 2

float4 palettePixelBilinear(float4 coords)
{
#if DIV_POS_Z == 0
	coords.xy /= coords.w;
#endif
	float2 pixCoord = coords.xy * textureSize.xy - 0.5f;			// coordinates of top left pixel
	float2 originPixCoord = floor(pixCoord);

	float2 sampleUV = (originPixCoord + 0.5f) / textureSize.xy;	// UV coordinates of center of top left pixel

    // Sample from all surrounding texels
    float4 c00 = getPaletteEntry(tex2D(samplr, sampleUV).a);
	sampleUV = (originPixCoord + float2(0.5f, 1.5f)) / textureSize.xy;
    float4 c01 = getPaletteEntry(tex2D(samplr, sampleUV).a);
	sampleUV = (originPixCoord + float2(1.5f, 1.5f)) / textureSize.xy;
    float4 c11 = getPaletteEntry(tex2D(samplr, sampleUV).a);
	sampleUV = (originPixCoord + float2(1.5f, 0.5f)) / textureSize.xy;
    float4 c10 = getPaletteEntry(tex2D(samplr, sampleUV).a);

	float2 weight = pixCoord - originPixCoord;

    // Bi-linear mixing
    float4 temp0 = lerp(c00, c10, weight.x);
    float4 temp1 = lerp(c01, c11, weight.x);
    return lerp(temp0, temp1, weight.y);
}

#endif

struct PSO
{
	float4 col : COLOR0;
	float z : DEPTH;
};

PSO main(in pixel inpix)
{ 
#if pp_ClipInside == 1
	// Clip inside the box
	if (inpix.pos.x >= clipTest.x && inpix.pos.x <= clipTest.z
			&& inpix.pos.y >= clipTest.y && inpix.pos.y <= clipTest.w)
		discard;
#endif

	float4 color = inpix.col;
	#if pp_BumpMap == 1 || pp_Offset == 1
		float4 specular = inpix.spec;
	#endif
	#if pp_Gouraud == 1 && DIV_POS_Z != 1
		color /= inpix.uv.w;
		#if pp_BumpMap == 1 || pp_Offset == 1
			specular /= inpix.uv.w;
		#endif
	#endif
	#if pp_UseAlpha == 0
		color.a = 1.0f;
	#endif
	#if pp_FogCtrl == 3
		color = float4(FOG_COL_RAM.rgb, fog_mode2(inpix.uv.w));
	#endif
	#if pp_Texture == 1
	{
		#if pp_Palette == 0
			#if DIV_POS_Z == 1
				float4 texcol = tex2D(samplr, inpix.uv.xy);
			#else
				float4 texcol = tex2Dproj(samplr, inpix.uv);
			#endif
		#elif pp_Palette == 1
			float4 texcol = palettePixel(inpix.uv);
		#else
			float4 texcol = palettePixelBilinear(inpix.uv);
		#endif
		
		#if pp_BumpMap == 1
			float s = PI / 2.0f * (texcol.a * 15.0f * 16.0f + texcol.r * 15.0f) / 255.0f;
			float r = 2.0f * PI * (texcol.g * 15.0f * 16.0f + texcol.b * 15.0f) / 255.0f;
			texcol[3] = clamp(specular.a + specular.r * sin(s) + specular.g * cos(s) * cos(r - 2.0f * PI * specular.b), 0.0f, 1.0f);
			texcol.rgb = float3(1.0f, 1.0f, 1.0f);	
		#else
			#if pp_IgnoreTexA == 1
				texcol.a = 1.0f;
			#endif
		#endif
		#if pp_ShadInstr == 0
			color = texcol;
		#endif
		#if pp_ShadInstr == 1
			color.rgb *= texcol.rgb;
			color.a = texcol.a;
		#endif
		#if pp_ShadInstr == 2
			color.rgb = lerp(color.rgb, texcol.rgb, texcol.a);
		#endif
		#if  pp_ShadInstr == 3
			color *= texcol;
		#endif
		
		#if pp_Offset == 1 && pp_BumpMap == 0
			color.rgb += specular.rgb;
		#endif
	}
	#endif
	
	color = clampColor(color);
	
	#if pp_FogCtrl == 0
		color.rgb = lerp(color.rgb, FOG_COL_RAM.rgb, fog_mode2(inpix.uv.w)); 
	#endif
	#if pp_FogCtrl == 1 && pp_Offset == 1 && pp_BumpMap == 0
		color.rgb = lerp(color.rgb, FOG_COL_VERT.rgb, specular.a);
	#endif
	
	#if pp_TriLinear == 1
	color *= trilinearAlpha;
	#endif

#if DITHERING == 1
	static const float ditherTable[16] = {
		5.0f, 13.0f,  7.0f, 15.0f,
		9.0f,  1.0f, 11.0f,  3.0f,
		6.0f, 14.0f,  4.0f, 12.0f,
		10.0f, 2.0f,  8.0f,  0.0f
	};
	float r = ditherTable[int(inpix.pos.y % 4.0f) * 4 + int(inpix.pos.x % 4.0f)];
	float4 dv = float4(r, r, r, 1.0f) / ditherDivisor;
	color = clamp(floor(color * 255.0f + dv) / 255.0f, 0.0f, 1.0f);

#endif
	PSO pso;
#if DIV_POS_Z == 1
	float w = 100000.0f / inpix.uv.w;
#else
	float w = 100000.0f * inpix.uv.w;
#endif
	pso.z = log2(1.0f + max(w, -0.999999f)) / 34.0f;
	pso.col = color;

	return pso;
}

PSO modifierVolume(float4 uv : TEXCOORD0)
{
	PSO pso;
#if DIV_POS_Z == 1
	float w = 100000.0f / uv.w;
#else
	float w = 100000.0f * uv.w;
#endif
	pso.z = log2(1.0f + max(w, -0.999999f)) / 34.0f;
	pso.col = float4(0, 0, 0, FOG_DENSITY_SCALE.y);

	return pso;
}

""";

    private static readonly string[] MacroValues = ["0", "1", "2", "3"];

    private static readonly (string? Name, string? Definition)[] VertexMacros =
    [
        ("pp_Gouraud", "1"),
        ("DIV_POS_Z", "0"),
        (null, null)
    ];

    private enum ShaderMacro
    {
        Gouraud,
        DivPosZ,
        Texture,
        Offset,
        ShadInstr,
        IgnoreTexA,
        UseAlpha,
        FogCtrl,
        FogClamping,
        Palette,
        BumpMap,
        TriLinear,
        ClipInside,
        Dithering
    }

    private static readonly (string? Name, string? Definition)[] PixelMacros =
    [
        ("pp_Gouraud", "1"),
        ("DIV_POS_Z", "0"),
        ("pp_Texture", "0"),
        ("pp_Offset", "0"),
        ("pp_ShadInstr", "0"),
        ("pp_IgnoreTexA", "0"),
        ("pp_UseAlpha", "0"),
        ("pp_FogCtrl", "0"),
        ("FogClamping", "0"),
        ("pp_Palette", "0"),
        ("pp_BumpMap", "0"),
        ("pp_TriLinear", "0"),
        ("pp_ClipInside", "0"),
        ("DITHERING", "0"),
        (null, null)
    ];

    internal IDirect3DPixelShader9? GetShader(bool texture, bool useAlpha, bool ignoreTexA, uint shadInstr,
        bool offset, uint fogCtrl, bool bumpMap, bool fogClamping, bool trilinear, int palette, bool gouraud,
        bool clipInside, bool dithering)
    {
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
            | ((clipInside ? 1u : 0u) << 14)
            | ((PvrConfiguration.NativeDepthInterpolation ? 1u : 0u) << 15)
            | ((dithering ? 1u : 0u) << 16);
        if (!_shaders.TryGetValue(hash, out IDirect3DPixelShader9? shader))
        {
            PvrRuntime.Verify(shadInstr < MacroValues.Length);
            PvrRuntime.Verify(fogCtrl < MacroValues.Length);
            PixelMacros[(int)ShaderMacro.Texture].Definition = MacroValues[texture ? 1 : 0];
            PixelMacros[(int)ShaderMacro.UseAlpha].Definition = MacroValues[useAlpha ? 1 : 0];
            PixelMacros[(int)ShaderMacro.IgnoreTexA].Definition = MacroValues[ignoreTexA ? 1 : 0];
            PixelMacros[(int)ShaderMacro.ShadInstr].Definition = MacroValues[shadInstr];
            PixelMacros[(int)ShaderMacro.Offset].Definition = MacroValues[offset ? 1 : 0];
            PixelMacros[(int)ShaderMacro.FogCtrl].Definition = MacroValues[fogCtrl];
            PixelMacros[(int)ShaderMacro.BumpMap].Definition = MacroValues[bumpMap ? 1 : 0];
            PixelMacros[(int)ShaderMacro.FogClamping].Definition = MacroValues[fogClamping ? 1 : 0];
            PixelMacros[(int)ShaderMacro.TriLinear].Definition = MacroValues[trilinear ? 1 : 0];
            PixelMacros[(int)ShaderMacro.Palette].Definition = MacroValues[palette];
            PixelMacros[(int)ShaderMacro.Gouraud].Definition = MacroValues[gouraud ? 1 : 0];
            PixelMacros[(int)ShaderMacro.ClipInside].Definition = MacroValues[clipInside ? 1 : 0];
            PixelMacros[(int)ShaderMacro.DivPosZ].Definition = MacroValues[PvrConfiguration.NativeDepthInterpolation ? 1 : 0];
            PixelMacros[(int)ShaderMacro.Dithering].Definition = MacroValues[dithering ? 1 : 0];
            shader = CompilePS(PixelShader, "main", PixelMacros);
            PvrRuntime.Verify(shader != null);
            _shaders.Add(hash, shader);
        }
        return shader;
    }

    internal IDirect3DVertexShader9? GetVertexShader(bool gouraud)
    {
        int index = (gouraud ? 1 : 0) | ((PvrConfiguration.NativeDepthInterpolation ? 1 : 0) << 1);
        ref IDirect3DVertexShader9? shader = ref _vertexShaders[index];
        if (shader == null)
        {
            VertexMacros[(int)ShaderMacro.Gouraud].Definition = MacroValues[gouraud ? 1 : 0];
            VertexMacros[(int)ShaderMacro.DivPosZ].Definition = MacroValues[PvrConfiguration.NativeDepthInterpolation ? 1 : 0];
            shader = CompileVS(VertexShader, "main", VertexMacros);
        }
        return shader;
    }

    internal IDirect3DPixelShader9? GetModVolShader()
    {
        ref IDirect3DPixelShader9? shader = ref _modVolShaders[PvrConfiguration.NativeDepthInterpolation ? 1 : 0];
        if (shader == null)
        {
            PixelMacros[(int)ShaderMacro.DivPosZ].Definition = MacroValues[PvrConfiguration.NativeDepthInterpolation ? 1 : 0];
            shader = CompilePS(PixelShader, "modifierVolume", PixelMacros);
        }
        return shader;
    }

    private nint CompileShader(string source, string function, nint profile, (string? Name, string? Definition)[] defines)
    {
        nint errors = 0;
        nint shader = 0;
        nint constants = 0;
        nint entryPoint = 0;
        bool returned = false;
        Span<NativeMacro> nativeDefines = stackalloc NativeMacro[defines.Length];
        nativeDefines.Clear();
        try
        {
            for (int i = 0; i < defines.Length; i++)
            {
                (string? name, string? definition) = defines[i];
                nativeDefines[i].Name = name == null ? 0 : Marshal.StringToCoTaskMemUTF8(name);
                nativeDefines[i].Definition = definition == null ? 0 : Marshal.StringToCoTaskMemUTF8(definition);
            }
            entryPoint = Marshal.StringToCoTaskMemUTF8(function);
            byte[] sourceBytes = Encoding.UTF8.GetBytes(source);
            fixed (byte* data = sourceBytes)
            fixed (NativeMacro* macros = nativeDefines)
                _compileShader!((nint)data, (uint)sourceBytes.Length, macros, 0, entryPoint, profile,
                    ShaderDebug, out shader, out errors, out constants);
            if (errors != 0)
                PvrRuntime.Log.Warn("{0}", Marshal.PtrToStringAnsi(GetBufferPointer(errors)));
            returned = true;
            return shader;
        }
        finally
        {
            if (constants != 0)
                Marshal.Release(constants);
            if (errors != 0)
                Marshal.Release(errors);
            if (!returned && shader != 0)
                Marshal.Release(shader);
            Marshal.FreeCoTaskMem(entryPoint);
            foreach (NativeMacro macro in nativeDefines)
            {
                Marshal.FreeCoTaskMem(macro.Name);
                Marshal.FreeCoTaskMem(macro.Definition);
            }
        }
    }

    private IDirect3DVertexShader9? CompileVS(string source, string function, (string? Name, string? Definition)[] defines)
    {
        nint buffer = CompileShader(source, function, _getVertexShaderProfile!(_device!.NativePointer), defines);
        if (buffer == 0)
            return null;
        try
        {
            _device.CreateVertexShader((uint*)GetBufferPointer(buffer), out IDirect3DVertexShader9? shader);
            return shader;
        }
        finally
        {
            Marshal.Release(buffer);
        }
    }

    private IDirect3DPixelShader9? CompilePS(string source, string function, (string? Name, string? Definition)[] defines)
    {
        nint buffer = CompileShader(source, function, _getPixelShaderProfile!(_device!.NativePointer), defines);
        if (buffer == 0)
            return null;
        try
        {
            _device.CreatePixelShader((uint*)GetBufferPointer(buffer), out IDirect3DPixelShader9? shader);
            return shader;
        }
        finally
        {
            Marshal.Release(buffer);
        }
    }

    internal void Init(IDirect3DDevice9 device)
    {
        device.AddRef();
        _device = new IDirect3DDevice9(device.NativePointer);
        bool loaded = false;
        for (int version = 43; version >= 24; version--)
        {
            string name = "d3dx9_" + version.ToString(CultureInfo.InvariantCulture) + ".dll";
            if (_d3dx9Library.Load(name))
            {
                loaded = true;
                PvrRuntime.Log.Debug("Loaded {0}", name);
                break;
            }
        }
        if (!loaded)
        {
            PvrRuntime.Log.Error("Cannot load d3dx9_??.dll");
            throw new PvrRendererException("Cannot load d3dx9_??.dll");
        }
        _compileShader = _d3dx9Library.GetFunction<CompileShaderEntry>("D3DXCompileShader");
        _getVertexShaderProfile = _d3dx9Library.GetFunction<ShaderProfileEntry>("D3DXGetVertexShaderProfile");
        _getPixelShaderProfile = _d3dx9Library.GetFunction<ShaderProfileEntry>("D3DXGetPixelShaderProfile");
        if (_compileShader == null || _getVertexShaderProfile == null || _getPixelShaderProfile == null)
        {
            PvrRuntime.Log.Error("Cannot find entry point in d3dx9_??.dll");
            throw new PvrRendererException("Cannot load d3dx9_??.dll");
        }
    }

    internal void Term()
    {
        try
        {
            foreach (IDirect3DPixelShader9? shader in _shaders.Values)
                shader?.Dispose();
            _shaders.Clear();
            for (int i = 0; i < _vertexShaders.Length; i++)
            {
                _vertexShaders[i]?.Dispose();
                _vertexShaders[i] = null;
            }
            for (int i = 0; i < _modVolShaders.Length; i++)
            {
                _modVolShaders[i]?.Dispose();
                _modVolShaders[i] = null;
            }
        }
        finally
        {
            _device?.Dispose();
            _device = null;
        }
    }

    private static nint GetBufferPointer(nint buffer)
    {
        nint* vtable = *(nint**)buffer;
        return ((delegate* unmanaged[Stdcall]<nint, nint>)vtable[3])(buffer);
    }
}
