using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using SharpGen.Runtime;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.D3DCompiler;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Direct3D11;

internal sealed unsafe partial class PvrDx11OitShaders
{
    private const string VertexShader = """

#if pp_Gouraud == 1
#define INTERPOLATION
#else
#define INTERPOLATION nointerpolation
#endif

struct VertexIn
{
	float4 pos : POSITION;
	float4 col : COLOR0;
	float4 spec : COLOR1;
	float2 uv : TEXCOORD0;
	float4 col1 : COLOR2;
	float4 spec1 : COLOR3;
	float2 uv1 : TEXCOORD1;
	float3 normal: NORMAL; // unused
	uint vertexId : SV_VertexID;
};

struct VertexOut
{
	float4 pos : SV_POSITION;
	float4 uv : TEXCOORD0;
	INTERPOLATION float4 col : COLOR0;
	INTERPOLATION float4 spec : COLOR1;
	float2 uv1 : TEXCOORD1;
	INTERPOLATION float4 col1 : COLOR2;
	INTERPOLATION float4 spec1 : COLOR3;
	nointerpolation uint index : BLENDINDICES0;
};

cbuffer shaderConstants : register(b0)
{
	float4x4 transMatrix;
	float4 leftPlane;
	float4 topPlane;
	float4 rightPlane;
	float4 bottomPlane;
};

cbuffer polyConstants : register(b1)
{
	int polyNumber;
};

[clipplanes(leftPlane, topPlane, rightPlane, bottomPlane)]
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
	vo.col1 = vin.col1;
	vo.spec1 = vin.spec1;
#if pp_Gouraud == 1 && DIV_POS_Z != 1
	vo.col *= vo.pos.z;
	vo.spec *= vo.pos.z;
	vo.col1 *= vo.pos.z;
	vo.spec1 *= vo.pos.z;
#endif
	vo.uv.xyz = float3(vin.uv, 0.f);
	vo.uv1 = vin.uv1;
	vo.index = uint(polyNumber) + vin.vertexId;

#if DIV_POS_Z == 1
	vo.uv.w = vo.pos.w;
#else
	vo.uv.xy *= vo.pos.z;
	vo.uv.w = vo.pos.z;
	vo.uv1 *= vo.pos.z;
	vo.pos.w = 1.f;
	vo.pos.z = 0.f;
#endif

	return vo;
}


""";

    private const string OITShaderHeader = """


cbuffer constantBuffer : register(b0)
{
	float4 colorClampMin;
	float4 colorClampMax;
	float4 FOG_COL_VERT;
	float4 FOG_COL_RAM;
	float4 ditherDivisor;
	float fogDensity;
	float shadowScale;
	float alphaTestValue;
};

RWTexture2D<uint> abufferPointers : register(u2);

struct Pixel {
	uint color;
	float depth;
	uint seq_num;
	uint next;
};
#define EOL 0xFFFFFFFFu

#define ZERO				0
#define ONE					1
#define OTHER_COLOR			2
#define INVERSE_OTHER_COLOR	3
#define SRC_ALPHA			4
#define INVERSE_SRC_ALPHA	5
#define DST_ALPHA			6
#define INVERSE_DST_ALPHA	7

float getFragDepth(float z)
{
#if DIV_POS_Z == 1
	float w = 100000.0 / z;
#else
	float w = 100000.0 * z;
#endif
	return log2(1.0 + max(w, -0.999999f)) / 34.0;
}

struct PolyParam {
	int tsp_isp_pcw;
	int tsp1;
};

#define GET_TSP_FOR_AREA int tsp = area1 ? pp.tsp1 : pp.tsp_isp_pcw;

int getSrcBlendFunc(in PolyParam pp, in bool area1)
{
	GET_TSP_FOR_AREA
	return (tsp >> 29) & 7;
}

int getDstBlendFunc(in PolyParam pp, in bool area1)
{
	GET_TSP_FOR_AREA
	return (tsp >> 26) & 7;
}

bool getSrcSelect(in PolyParam pp, in bool area1)
{
	GET_TSP_FOR_AREA
	return ((tsp >> 25) & 1) != 0;
}

bool getDstSelect(in PolyParam pp, in bool area1)
{
	GET_TSP_FOR_AREA
	return ((tsp >> 24) & 1) != 0;
}

int getFogControl(in PolyParam pp, in bool area1)
{
	GET_TSP_FOR_AREA
	return (tsp >> 22) & 3;
}

bool getUseAlpha(in PolyParam pp, in bool area1)
{
	GET_TSP_FOR_AREA
	return ((tsp >> 20) & 1) != 0;
}

bool getIgnoreTexAlpha(in PolyParam pp, in bool area1)
{
	GET_TSP_FOR_AREA
	return ((tsp >> 19) & 1) != 0;
}

int getShadingInstruction(in PolyParam pp, in bool area1)
{
	GET_TSP_FOR_AREA
	return (tsp >> 6) & 3;
}

int getDepthFunc(in PolyParam pp)
{
	return (pp.tsp_isp_pcw >> 13) & 7;
}

bool getDepthMask(in PolyParam pp)
{
	return ((pp.tsp_isp_pcw >> 10) & 1) != 1;
}

bool getShadowEnable(in PolyParam pp)
{
	return (pp.tsp_isp_pcw & 1) != 0;
}

uint getPolyIndex(in Pixel pixel)
{
	return pixel.seq_num & 0x3FFFFFFFu;
}

uint getPolyNumber(in Pixel pixel)
{
	return (pixel.seq_num & 0x3FFFFFFFu) >> 17;
}

#define SHADOW_STENCIL 0x40000000u
#define SHADOW_ACC	   0x80000000u

bool isShadowed(in Pixel pixel)
{
	return (pixel.seq_num & SHADOW_ACC) == SHADOW_ACC;
}

bool isTwoVolumes(in PolyParam pp)
{
	return pp.tsp1 != -1;
}

uint packColors(in float4 v)
{
	return (uint(round(v.r * 255.f)) << 24) | (uint(round(v.g * 255.f)) << 16) | (uint(round(v.b * 255.f)) << 8) | uint(round(v.a * 255.f));
}

float4 unpackColors(in uint u)
{
	return float4(float((u >> 24) & 255u) / 255.f, float((u >> 16) & 255u) / 255.f, float((u >> 8) & 255u) / 255.f, float(u & 255u) / 255.f);
}

RWStructuredBuffer<Pixel> Pixels : register(u1);

uint getNextPixelIndex()
{
	uint index = Pixels.IncrementCounter();
	uint count;
	uint stride;
	Pixels.GetDimensions(count, stride);
	if (index >= count)
		// Buffer overflow
		discard;
	
	return index;
}

StructuredBuffer<PolyParam> tr_poly_params : register(t5);


""";

    private const string PixelShader = """


#include "oit_header.hlsl"

#define PASS_DEPTH 0
#define PASS_COLOR 1
#define PASS_OIT 2

#if pp_TwoVolumes == 1
#define IF(x) if (x)
#else
#define IF(x)
#endif

Texture2D texture1 : register(t3);
sampler sampler1 : register(s3);

#if PASS == PASS_COLOR
Texture2D<uint2> stencilTexture : register(t4);
#endif
#if PASS == PASS_OIT
Texture2D<float> depthTexture : register(t4);
#endif

cbuffer polyConstantBuffer : register(b1)
{
	float4 clipTest;
	int2 blend_mode0;
	int2 blend_mode1;
	float paletteIndex;
	float trilinearAlpha;

	// two volume mode
	int shading_instr0;
	int shading_instr1;
	int fog_control0;
	int fog_control1;
	int use_alpha0;
	int use_alpha1;
	int ignore_tex_alpha0;
	int ignore_tex_alpha1;
};

#include "pixel_common.hlsl"

struct VertexIn 
{
	float4 pos : SV_POSITION;
	float4 uv : TEXCOORD0;
	INTERPOLATION float4 col : COLOR0;
	INTERPOLATION float4 spec : COLOR1;
	float2 uv1 : TEXCOORD1;
	INTERPOLATION float4 col1 : COLOR2;
	INTERPOLATION float4 spec1 : COLOR3;
	nointerpolation uint index : BLENDINDICES0;
};

struct PSO
{
	float4 col : SV_TARGET;
	float z : SV_DEPTH;
};

PSO main(in VertexIn inpix)
{
#if pp_ClipInside == 1
	// Clip inside the box
	if (inpix.pos.x >= clipTest.x && inpix.pos.x <= clipTest.z
			&& inpix.pos.y >= clipTest.y && inpix.pos.y <= clipTest.w)
		discard;
#endif
	PSO pso;
	pso.z = getFragDepth(inpix.uv.w);
	#if PASS == PASS_OIT
		// Manual depth testing
		float frontDepth = depthTexture[uint2(inpix.pos.xy)].r;
		if (pso.z < frontDepth)
			discard;
	#endif

	float4 color = inpix.col;
	float4 specular = inpix.spec;
	bool area1 = false;
	int2 cur_blend_mode = blend_mode0.xy;
	
	#if pp_TwoVolumes == 1
		bool cur_use_alpha = use_alpha0 != 0;
		bool cur_ignore_tex_alpha = ignore_tex_alpha0 != 0;
		int cur_shading_instr = shading_instr0;
		int cur_fog_control = fog_control0;
		#if PASS == PASS_COLOR
			uint stencil = stencilTexture[uint2(inpix.pos.xy)].g;
			if (stencil == 0x81u) {
				color = inpix.col1;
				specular = inpix.spec1;
				area1 = true;
				cur_blend_mode = blend_mode1.xy;
				cur_use_alpha = use_alpha1 != 0;
				cur_ignore_tex_alpha = ignore_tex_alpha1 != 0;
				cur_shading_instr = shading_instr1;
				cur_fog_control = fog_control1;
			}
		#endif
	#endif
	#if pp_Gouraud == 1 && DIV_POS_Z != 1
		color /= inpix.uv.w;
		specular /= inpix.uv.w;
	#endif
	#if pp_UseAlpha == 0 || pp_TwoVolumes == 1
		IF (!cur_use_alpha)
			color.a = 1.0;
	#endif
	#if pp_FogCtrl == 3 || pp_TwoVolumes == 1 // LUT Mode 2
		IF (cur_fog_control == 3)
			color = float4(FOG_COL_RAM.rgb, fog_mode2(inpix.uv.w));
	#endif
	#if pp_Texture==1
	{
		float2 uv;
		#if pp_TwoVolumes == 1
			if (area1)
				uv = inpix.uv1;
			else
		#endif
				uv = inpix.uv.xy;
		#if DIV_POS_Z != 1
			uv /= inpix.uv.w;
		#endif

		float4 texcol;
		#if pp_TwoVolumes == 1
			if (area1)
				texcol = texture1.Sample(sampler1, uv);
			else
		#endif
		#if pp_Palette == 0
				texcol = texture0.Sample(sampler0, uv);
		#elif pp_Palette == 1
				texcol = palettePixel(texture0, sampler0, uv);
		#else
				texcol = palettePixelBilinear(texture0, sampler0, uv);
		#endif
		#if pp_BumpMap == 1
			float s = PI / 2.0f * (texcol.a * 15.0f * 16.0f + texcol.r * 15.0f) / 255.0f;
			float r = 2.0f * PI * (texcol.g * 15.0f * 16.0f + texcol.b * 15.0f) / 255.0f;
			texcol[3] = clamp(specular.a + specular.r * sin(s) + specular.g * cos(s) * cos(r - 2.0f * PI * specular.b), 0.0f, 1.0f);
			texcol.rgb = float3(1.0f, 1.0f, 1.0f);	
		#else
			#if pp_IgnoreTexA==1 || pp_TwoVolumes == 1
				IF(cur_ignore_tex_alpha)
					texcol.a = 1.0f;
			#endif
		#endif
		#if pp_ShadInstr == 0 || pp_TwoVolumes == 1 // DECAL
		IF(cur_shading_instr == 0)
			color = texcol;
		#endif
		#if pp_ShadInstr == 1 || pp_TwoVolumes == 1 // MODULATE
		IF(cur_shading_instr == 1)
			color.rgb *= texcol.rgb;
			color.a = texcol.a;
		#endif
		#if pp_ShadInstr == 2 || pp_TwoVolumes == 1 // DECAL ALPHA
		IF(cur_shading_instr == 2)
			color.rgb = lerp(color.rgb, texcol.rgb, texcol.a);
		#endif
		#if pp_ShadInstr == 3 || pp_TwoVolumes == 1 // MODULATE ALPHA
		IF(cur_shading_instr == 3)
			color *= texcol;
		#endif
		
		#if pp_Offset == 1 && pp_BumpMap == 0
			color.rgb += specular.rgb;
		#endif
	}
	#endif
	#if PASS == PASS_COLOR && pp_TwoVolumes == 0
		uint stencil = stencilTexture[uint2(inpix.pos.xy)].g;
		if (stencil == 0x81u)
			color.rgb *= shadowScale;
	#endif

	color = clampColor(color);
	
	#if pp_FogCtrl == 0 || pp_TwoVolumes == 1 // LUT
		IF(cur_fog_control == 0)
			color.rgb = lerp(color.rgb, FOG_COL_RAM.rgb, fog_mode2(inpix.uv.w)); 
	#endif
	#if pp_Offset == 1 && pp_BumpMap == 0 && (pp_FogCtrl == 1 || pp_TwoVolumes == 1)  // Per vertex
		IF(cur_fog_control == 1)
			color.rgb = lerp(color.rgb, FOG_COL_VERT.rgb, specular.a);
	#endif
	
	color *= trilinearAlpha;

	#if cp_AlphaTest == 1
		color.a = round(color.a * 255.0f) * 0.0039215688593685626983642578125; // 1 / 255
		if (alphaTestValue > color.a)
			discard;
		color.a = 1.0f;
	#endif

	#if PASS == PASS_COLOR 
		pso.col = color;
	#elif PASS == PASS_OIT
		uint2 coords = uint2(inpix.pos.xy);
		uint idx =  getNextPixelIndex();
		
		Pixel pixel;
		pixel.color = packColors(clamp(color, 0.f, 1.f));
		pixel.depth = pso.z;
		pixel.seq_num = inpix.index;
		InterlockedExchange(abufferPointers[coords], idx, pixel.next);
		Pixels[idx] = pixel;
		
		pso.col = 0.f;
	#endif

	return pso;
}

struct MVPixel 
{
	float4 pos : SV_POSITION;
	float4 uv : TEXCOORD0;
};

float modifierVolume(in MVPixel inpix) : SV_Depth
{
	return getFragDepth(inpix.uv.w);
}

""";

    private const string OITFinalShaderSource = """

#include "oit_header.hlsl"

Texture2D opaqueTex : register(t0);
sampler opaqueSampler : register(s0);

int fillAndSortFragmentArray(in uint2 coords, out uint pixel_list[MAX_PIXELS_PER_FRAGMENT])
{
	uint idx = abufferPointers[coords];
	if (idx == EOL)
		return 0;
	int count = 1;
	pixel_list[0] = idx;
	idx = Pixels[idx].next;
	for (; idx != EOL && count < MAX_PIXELS_PER_FRAGMENT; count++)
	{
		int j = count - 1;
		uint jIdx = pixel_list[j];
		while (j >= 0
			   && (Pixels[jIdx].depth > Pixels[idx].depth
				   || (Pixels[jIdx].depth == Pixels[idx].depth && getPolyIndex(Pixels[jIdx]) > getPolyIndex(Pixels[idx]))))
		{
			pixel_list[j + 1] = pixel_list[j];
			j--;
			if (j >= 0)
				jIdx = pixel_list[j];
		}
		pixel_list[j + 1] = idx;
		idx = Pixels[idx].next;
	}
	// Reset pointer
	abufferPointers[coords] = EOL;

	return count;
}

// Blend fragments back-to-front
float4 resolveAlphaBlend(in float2 pos)
{
	// Copy and sort indexes into a local array
	uint2 coords = uint2(pos);
	uint pixel_list[MAX_PIXELS_PER_FRAGMENT];
	int num_frag = fillAndSortFragmentArray(coords, pixel_list);
	
	float2 dim;
	opaqueTex.GetDimensions(dim.x, dim.y);
	float4 finalColor = opaqueTex.Sample(opaqueSampler, pos / dim);
	float4 secondaryBuffer = 0.f; // Secondary accumulation buffer
	
	for (int i = 0; i < num_frag; i++)
	{
		uint pixIdx = pixel_list[i];
		const Pixel pixel = Pixels[pixIdx];
		const PolyParam pp = tr_poly_params[getPolyNumber(pixel)];
		bool area1 = false;
		bool shadowed = false;
		if (isShadowed(pixel))
		{
			if (isTwoVolumes(pp))
				area1 = true;
			else
				shadowed = true;
		}
		float4 srcColor;
		if (getSrcSelect(pp, area1))
			srcColor = secondaryBuffer;
		else
		{
			srcColor = unpackColors(pixel.color);
			if (shadowed)
				srcColor.rgb *= shadowScale;
		}
		float4 dstColor = getDstSelect(pp, area1) ? secondaryBuffer : finalColor;
		float4 srcCoef;
		float4 dstCoef;
		
		int srcBlend = getSrcBlendFunc(pp, area1);
		switch (srcBlend)
		{
			case ZERO:
				srcCoef = 0.f;
				break;
			case ONE:
				srcCoef = 1.f;
				break;
			case OTHER_COLOR:
				srcCoef = dstColor;
				break;
			case INVERSE_OTHER_COLOR:
				srcCoef = 1.f - dstColor;
				break;
			case SRC_ALPHA:
				srcCoef = srcColor.a;
				break;
			case INVERSE_SRC_ALPHA:
				srcCoef = 1.f - srcColor.a;
				break;
			case DST_ALPHA:
				srcCoef = dstColor.a;
				break;
			case INVERSE_DST_ALPHA:
				srcCoef = 1.f - dstColor.a;
				break;
		}
		int dstBlend = getDstBlendFunc(pp, area1);
		switch (dstBlend)
		{
			case ZERO:
				dstCoef = 0.f;
				break;
			case ONE:
				dstCoef = 1.f;
				break;
			case OTHER_COLOR:
				dstCoef = srcColor;
				break;
			case INVERSE_OTHER_COLOR:
				dstCoef = 1.f - srcColor;
				break;
			case SRC_ALPHA:
				dstCoef = srcColor.a;
				break;
			case INVERSE_SRC_ALPHA:
				dstCoef = 1.f - srcColor.a;
				break;
			case DST_ALPHA:
				dstCoef = dstColor.a;
				break;
			case INVERSE_DST_ALPHA:
				dstCoef = 1.f - dstColor.a;
				break;
		}
		const float4 result = clamp(dstColor * dstCoef + srcColor * srcCoef, 0.f, 1.f);
		if (getDstSelect(pp, area1))
			secondaryBuffer = result;
		else
			finalColor = result;
	}
#if DITHERING == 1
	static const float ditherTable[16] = {
		5.0f, 13.0f,  7.0f, 15.0f,
		9.0f,  1.0f, 11.0f,  3.0f,
		6.0f, 14.0f,  4.0f, 12.0f,
		10.0f, 2.0f,  8.0f,  0.0f
	};
	float r = ditherTable[int(pos.y % 4.0f) * 4 + int(pos.x % 4.0f)];
	float4 dv = float4(r, r, r, 1.0f) / ditherDivisor;
	finalColor = clamp(floor(finalColor * 255.0f + dv) / 255.0f, 0.0f, 1.0f);
#endif

	return finalColor;
}

float4 main(float4 pos : SV_Position) : SV_Target
{
	// Visualize the number of layers in use
	//uint pixel_list[MAX_PIXELS_PER_FRAGMENT];
	//return float4(float(fillAndSortFragmentArray(uint2(pos.xy), pixel_list)) / MAX_PIXELS_PER_FRAGMENT * 8.f, 0.f, 0.f, 1.f);

	// Compute and output final color for the frame buffer
	return resolveAlphaBlend(pos.xy);
}

""";

    private const string OITFinalVertexShaderSource = """


float4 main(uint vertexId : SV_VertexID) : SV_Position
{
	float4 output;
    
	if (vertexId == 0)
		output = float4(-1.f, -1.f, 0.f, 1.f);
	else if (vertexId == 1)
		output = float4(-1.f, 1.f, 0.f, 1.f);
	else if (vertexId == 2)
		output = float4(1.f, -1.f, 0.f, 1.f);
	else
		output = float4(1.f, 1.f, 0.f, 1.f);
    
	return output;
}

""";

    private const string OITTranslucentModvolShaderSource = """

#include "oit_header.hlsl"

struct MVPixel 
{
	float4 pos : SV_POSITION;
	float4 uv : TEXCOORD0;
};

// Must match ModifierVolumeMode enum values
#define MV_XOR		 0
#define MV_OR		 1
#define MV_INCLUSION 2
#define MV_EXCLUSION 3

void main(in MVPixel inpix)
{
	uint2 coords = uint2(inpix.pos.xy);
#if MV_MODE == MV_XOR || MV_MODE == MV_OR
	float depth = getFragDepth(inpix.uv.w);
#endif
	
	uint idx = abufferPointers[coords];
	int list_len = 0;
	while (idx != EOL && list_len < MAX_PIXELS_PER_FRAGMENT)
	{
		const Pixel pixel = Pixels[idx];
		const PolyParam pp = tr_poly_params[getPolyNumber(pixel)];
		if (getShadowEnable(pp))
		{
			uint prev_val;
#if MV_MODE == MV_XOR
			if (depth >= pixel.depth)
				InterlockedXor(Pixels[idx].seq_num, SHADOW_STENCIL, prev_val);
#elif MV_MODE == MV_OR
			if (depth >= pixel.depth)
				InterlockedOr(Pixels[idx].seq_num, SHADOW_STENCIL, prev_val);
#elif MV_MODE == MV_INCLUSION
			InterlockedAnd(Pixels[idx].seq_num, ~(SHADOW_STENCIL), prev_val);
			if ((prev_val & (SHADOW_STENCIL|SHADOW_ACC)) == SHADOW_STENCIL)
				InterlockedOr(Pixels[idx].seq_num, SHADOW_ACC, prev_val);
#elif MV_MODE == MV_EXCLUSION
			InterlockedAnd(Pixels[idx].seq_num, ~(SHADOW_STENCIL|SHADOW_ACC), prev_val);
			if ((prev_val & (SHADOW_STENCIL|SHADOW_ACC)) == SHADOW_ACC)
				InterlockedOr(Pixels[idx].seq_num, SHADOW_ACC, prev_val);
#endif
		}
		idx = pixel.next;
		list_len++;
	}
}

""";

    private sealed class OitIncludeManager : CallbackBase, Include
    {
        public Stream Open(IncludeType includeType, string fileName, Stream? parentStream)
        {
            string source = fileName switch
            {
                "oit_header.hlsl" => OITShaderHeader,
                "pixel_common.hlsl" => PvrDx11Shaders.PixelShaderCommon,
                _ => throw new SharpGenException(Result.Fail)
            };
            return new MemoryStream(Encoding.UTF8.GetBytes(source), writable: false);
        }

        public void Close(Stream data) => data.Dispose();
    }

    private static readonly string[] MacroValues = ["0", "1", "2", "3"];

    private enum VertexMacro
    {
        Gouraud,
        TwoVolumes,
        DivPosZ,
        PositionOnly,
        LightOn,
        ModifierVolume
    }

    private static readonly ShaderMacro[] VertexMacros =
    [
        new("pp_Gouraud", "1"),
        new("pp_TwoVolumes", "0"),
        new("DIV_POS_Z", "0"),
        new("POSITION_ONLY", "0"),
        new("LIGHT_ON", "1"),
        new("MODIFIER_VOLUME", "0"),
        new(null, null)
    ];

    private enum PixelMacro
    {
        Gouraud,
        TwoVolumes,
        DivPosZ,
        Texture,
        UseAlpha,
        IgnoreTexA,
        ShadInstr,
        Offset,
        FogCtrl,
        BumpMap,
        FogClamping,
        Palette,
        AlphaTest,
        ClipInside,
        Pass
    }

    private static readonly ShaderMacro[] PixelMacros =
    [
        new("pp_Gouraud", "1"),
        new("pp_TwoVolumes", "0"),
        new("DIV_POS_Z", "0"),
        new("pp_Texture", "0"),
        new("pp_UseAlpha", "0"),
        new("pp_IgnoreTexA", "0"),
        new("pp_ShadInstr", "0"),
        new("pp_Offset", "0"),
        new("pp_FogCtrl", "0"),
        new("pp_BumpMap", "0"),
        new("FogClamping", "0"),
        new("pp_Palette", "0"),
        new("cp_AlphaTest", "0"),
        new("pp_ClipInside", "0"),
        new("PASS", "0"),
        new(null, null)
    ];

    internal ID3D11PixelShader? GetShader(bool texture, bool useAlpha, bool ignoreTexA, uint shadInstr,
        bool offset, uint fogCtrl, bool bumpMap, bool fogClamping,
        int palette, bool gouraud, bool alphaTest, bool clipInside, bool twoVolumes, Pass pass)
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
            | ((uint)palette << 10)
            | ((gouraud ? 1u : 0u) << 12)
            | ((alphaTest ? 1u : 0u) << 13)
            | ((clipInside ? 1u : 0u) << 14)
            | ((twoVolumes ? 1u : 0u) << 15)
            | ((uint)pass << 16)
            | ((divPosZ ? 1u : 0u) << 18);
        _shaders.TryGetValue(hash, out ID3D11PixelShader? shader);
        if (shader == null)
        {
            PvrRuntime.Verify(shadInstr < MacroValues.Length);
            PvrRuntime.Verify(fogCtrl < MacroValues.Length);
            PvrRuntime.Verify((int)pass < MacroValues.Length);
            PixelMacros[(int)PixelMacro.Gouraud].Definition = MacroValues[gouraud ? 1 : 0];
            PixelMacros[(int)PixelMacro.Texture].Definition = MacroValues[texture ? 1 : 0];
            PixelMacros[(int)PixelMacro.UseAlpha].Definition = MacroValues[useAlpha ? 1 : 0];
            PixelMacros[(int)PixelMacro.IgnoreTexA].Definition = MacroValues[ignoreTexA ? 1 : 0];
            PixelMacros[(int)PixelMacro.ShadInstr].Definition = MacroValues[shadInstr];
            PixelMacros[(int)PixelMacro.Offset].Definition = MacroValues[offset ? 1 : 0];
            PixelMacros[(int)PixelMacro.FogCtrl].Definition = MacroValues[fogCtrl];
            PixelMacros[(int)PixelMacro.BumpMap].Definition = MacroValues[bumpMap ? 1 : 0];
            PixelMacros[(int)PixelMacro.FogClamping].Definition = MacroValues[fogClamping ? 1 : 0];
            PixelMacros[(int)PixelMacro.Palette].Definition = MacroValues[palette];
            PixelMacros[(int)PixelMacro.AlphaTest].Definition = MacroValues[alphaTest ? 1 : 0];
            PixelMacros[(int)PixelMacro.ClipInside].Definition = MacroValues[clipInside ? 1 : 0];
            PixelMacros[(int)PixelMacro.TwoVolumes].Definition = MacroValues[twoVolumes ? 1 : 0];
            PixelMacros[(int)PixelMacro.DivPosZ].Definition = MacroValues[divPosZ ? 1 : 0];
            PixelMacros[(int)PixelMacro.Pass].Definition = MacroValues[(int)pass];
            _shaders[hash] = shader = CompilePS(PixelShader, "main", PixelMacros);
            PvrRuntime.Verify(shader != null);
        }
        return shader;
    }

    internal ID3D11VertexShader? GetVertexShader(bool gouraud, bool naomi2, bool positionOnly, bool lightOn, bool twoVolumes = true)
    {
        bool divPosZ = !PvrConfiguration.IsNaomi2 && PvrConfiguration.NativeDepthInterpolation;
        uint hash = (gouraud ? 1u : 0u)
            | ((naomi2 ? 1u : 0u) << 1)
            | ((positionOnly ? 1u : 0u) << 2)
            | ((lightOn ? 1u : 0u) << 3)
            | ((twoVolumes ? 1u : 0u) << 4)
            | ((divPosZ ? 1u : 0u) << 5);
        _vertexShaders.TryGetValue(hash, out ID3D11VertexShader? shader);
        if (shader == null)
        {
            VertexMacros[(int)VertexMacro.Gouraud].Definition = MacroValues[gouraud ? 1 : 0];
            if (!naomi2)
            {
                VertexMacros[(int)VertexMacro.DivPosZ].Definition = MacroValues[divPosZ ? 1 : 0];
                shader = CompileVS(VertexShader, "main", VertexMacros);
            }
            else
            {
                VertexMacros[(int)VertexMacro.DivPosZ].Definition = MacroValues[0];
                VertexMacros[(int)VertexMacro.PositionOnly].Definition = MacroValues[positionOnly ? 1 : 0];
                VertexMacros[(int)VertexMacro.TwoVolumes].Definition = MacroValues[twoVolumes ? 1 : 0];
                VertexMacros[(int)VertexMacro.LightOn].Definition = MacroValues[lightOn ? 1 : 0];
                VertexMacros[(int)VertexMacro.ModifierVolume].Definition = MacroValues[0];
                string source = PvrDx11Naomi2.DX11N2VertexShader;
                if (!positionOnly && lightOn)
                    source += "\n" + PvrDx11Naomi2.DX11N2ColorShader;
                shader = CompileVS(source, "main", VertexMacros);
            }
            _vertexShaders[hash] = shader;
        }
        return shader;
    }

    internal ID3D11VertexShader? GetMVVertexShader(bool naomi2)
    {
        bool divPosZ = !PvrConfiguration.IsNaomi2 && PvrConfiguration.NativeDepthInterpolation;
        ref ID3D11VertexShader? shader = ref _modVolVertexShaders[(naomi2 ? 1 : 0) | ((divPosZ ? 1 : 0) << 1)];
        if (shader == null)
        {
            if (!naomi2)
            {
                VertexMacros[(int)VertexMacro.DivPosZ].Definition = MacroValues[divPosZ ? 1 : 0];
                shader = CompileVS(PvrDx11Shaders.ModVolVertexShader, "main", VertexMacros);
            }
            else
            {
                VertexMacros[(int)VertexMacro.Gouraud].Definition = MacroValues[0];
                VertexMacros[(int)VertexMacro.PositionOnly].Definition = MacroValues[1];
                VertexMacros[(int)VertexMacro.TwoVolumes].Definition = MacroValues[0];
                VertexMacros[(int)VertexMacro.LightOn].Definition = MacroValues[0];
                VertexMacros[(int)VertexMacro.ModifierVolume].Definition = MacroValues[1];
                shader = CompileVS(PvrDx11Naomi2.DX11N2VertexShader, "main", VertexMacros);
            }
        }
        return shader;
    }

    internal ID3D11PixelShader? GetModVolShader()
    {
        bool divPosZ = !PvrConfiguration.IsNaomi2 && PvrConfiguration.NativeDepthInterpolation;
        ref ID3D11PixelShader? shader = ref _modVolShaders[divPosZ ? 1 : 0];
        if (shader == null)
        {
            PixelMacros[(int)PixelMacro.DivPosZ].Definition = MacroValues[divPosZ ? 1 : 0];
            shader = CompilePS(PixelShader, "modifierVolume", PixelMacros);
        }
        return shader;
    }

    private void CheckMaxLayers()
    {
        int layers = Math.Clamp(PvrConfiguration.PerPixelLayers, 1, 256);
        if (_maxLayers != layers)
        {
            ReleaseShaders(_finalShaders);
            ReleaseShaders(_trModVolShaders);
            _maxLayers = layers;
        }
    }

    internal ID3D11PixelShader? GetFinalShader(bool dithering)
    {
        CheckMaxLayers();
        int index = dithering ? 1 : 0;
        if (_finalShaders[index] == null)
        {
            string maxLayers = _maxLayers.ToString(CultureInfo.InvariantCulture);
            ShaderMacro[] macros =
            [
                new("MAX_PIXELS_PER_FRAGMENT", maxLayers),
                new("DITHERING", MacroValues[index]),
                new(null, null)
            ];
            _finalShaders[index] = CompilePS(OITFinalShaderSource, "main", macros);
        }
        return _finalShaders[index];
    }

    internal ID3D11VertexShader? GetFinalVertexShader()
    {
        if (_finalVertexShader == null)
            _finalVertexShader = CompileVS(OITFinalVertexShaderSource, "main", null);
        return _finalVertexShader;
    }

    internal ID3D11PixelShader? GetTrModVolShader(int type)
    {
        CheckMaxLayers();
        bool divPosZ = !PvrConfiguration.IsNaomi2 && PvrConfiguration.NativeDepthInterpolation;
        ref ID3D11PixelShader? shader = ref _trModVolShaders[type | ((divPosZ ? 1 : 0) << 3)];
        if (shader == null)
        {
            string maxLayers = _maxLayers.ToString(CultureInfo.InvariantCulture);
            ShaderMacro[] macros =
            [
                new("MV_MODE", MacroValues[type]),
                new("MAX_PIXELS_PER_FRAGMENT", maxLayers),
                new("DIV_POS_Z", MacroValues[divPosZ ? 1 : 0]),
                new(null, null)
            ];
            shader = CompilePS(OITTranslucentModvolShaderSource, "main", macros);
        }
        return shader;
    }

    private Blob? CompileShader(string source, string function, string profile, ShaderMacro[]? defines)
    {
        ulong hash = HashShader(source, function, profile, defines, OITShaderHeader);
        if (!LookupShader(hash, out Blob? shaderBlob))
        {
            using var includeManager = new OitIncludeManager();
            byte[] input = Encoding.UTF8.GetBytes(source);
            fixed (byte* sourcePointer = input)
            {
                Result result = _d3dCompile!((nint)sourcePointer, (nuint)input.Length, null, defines,
                    includeManager, function, profile, 0, 0, out shaderBlob, out Blob? errorBlob);
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
        using Blob? blob = CompileShader(source, function, "vs_5_0", defines);
        ID3D11VertexShader? shader = null;
        if (blob != null && _device!.CreateVertexShader(blob.BufferPointer, blob.BufferSize, null, out shader).Failure)
            PvrRuntime.Log.Error("Vertex shader creation failed");
        return shader;
    }

    private ID3D11PixelShader? CompilePS(string source, string function, ShaderMacro[]? defines)
    {
        using Blob? blob = CompileShader(source, function, "ps_5_0", defines);
        ID3D11PixelShader? shader = null;
        if (blob != null && _device!.CreatePixelShader(blob.BufferPointer, blob.BufferSize, null, out shader).Failure)
            PvrRuntime.Log.Error("Pixel shader creation failed");
        return shader;
    }

    internal Blob? GetVertexShaderBlob()
    {
        VertexMacros[(int)VertexMacro.Gouraud].Definition = MacroValues[1];
        VertexMacros[(int)VertexMacro.PositionOnly].Definition = MacroValues[0];
        VertexMacros[(int)VertexMacro.TwoVolumes].Definition = MacroValues[1];
        VertexMacros[(int)VertexMacro.ModifierVolume].Definition = MacroValues[0];
        string source = PvrDx11Naomi2.DX11N2VertexShader + "\n" + PvrDx11Naomi2.DX11N2ColorShader;
        return CompileShader(source, "main", "vs_5_0", VertexMacros);
    }

    internal Blob? GetMVVertexShaderBlob()
    {
        VertexMacros[(int)VertexMacro.Gouraud].Definition = MacroValues[0];
        VertexMacros[(int)VertexMacro.PositionOnly].Definition = MacroValues[1];
        VertexMacros[(int)VertexMacro.TwoVolumes].Definition = MacroValues[0];
        VertexMacros[(int)VertexMacro.ModifierVolume].Definition = MacroValues[1];
        return CompileShader(PvrDx11Naomi2.DX11N2VertexShader, "main", "vs_5_0", VertexMacros);
    }

    internal Blob? GetFinalVertexShaderBlob() => CompileShader(OITFinalVertexShaderSource, "main", "vs_5_0", null);

    internal void Init(ID3D11Device device, PvrDx11ShaderCompiler d3dCompile)
    {
        _device = device;
        _d3dCompile = d3dCompile;
        EnableCache(!PvrDx11Context.Instance.HasShaderCache());
        LoadCache(CacheFile);
    }

    private static void ReleaseShaders<T>(T?[] shaders) where T : class, IDisposable
    {
        for (int i = 0; i < shaders.Length; i++)
        {
            shaders[i]?.Dispose();
            shaders[i] = null;
        }
    }
}
