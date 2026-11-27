using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Vortice.Direct3D11;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Direct3D11;

internal static class PvrDx11Naomi2
{
    internal const string DX11N2VertexShader = """

#if pp_Gouraud == 1
#define INTERPOLATION
#else
#define INTERPOLATION nointerpolation
#endif

struct VertexIn
{
	float4 pos : POSITION;
#if POSITION_ONLY == 0
	float4 col : COLOR0;
	float4 spec : COLOR1;
	float2 uv : TEXCOORD0;
#if pp_TwoVolumes == 1
	float4 col1 : COLOR2;
	float4 spec1 : COLOR3;
	float2 uv1 : TEXCOORD1;
#endif
	float3 normal: NORMAL;
	uint vertexId : SV_VertexID;
#endif
};

struct VertexOut
{
	float4 pos : SV_POSITION;
	float4 uv : TEXCOORD0;
#if POSITION_ONLY == 0
	INTERPOLATION float4 col : COLOR0;
	INTERPOLATION float4 spec : COLOR1;
#if pp_TwoVolumes == 1
	float2 uv1 : TEXCOORD1;
	INTERPOLATION float4 col1 : COLOR2;
	INTERPOLATION float4 spec1 : COLOR3;
#endif
	nointerpolation uint index : BLENDINDICES0;
#endif
};

cbuffer shaderConstants : register(b0)
{
	float4x4 ndcMat;
	float4 leftPlane;
	float4 topPlane;
	float4 rightPlane;
	float4 bottomPlane;
};

cbuffer polyConstants : register(b1)
{
	float4x4 mvMat;
	float4x4 normalMat;
	float4x4 projMat;
	int envMapping0;
	int envMapping1;
	int bumpMapping;
	int polyNumber;

	float4 glossCoef;
	int4 constantColor;
};

void computeColors(inout float4 baseCol, inout float4 offsetCol, in int volIdx, in float3 position, in float3 normal);
void computeEnvMap(inout float2 uv, in float3 normal);
void computeBumpMap(inout float4 color0, in float4 color1, in float3 position, in float3 normal, in float4x4 normalMat);

[clipplanes(leftPlane, topPlane, rightPlane, bottomPlane)]
VertexOut main(in VertexIn vin)
{
	VertexOut vo;
	vo.pos = mul(mvMat, float4(vin.pos.xyz, 1.f));
#if POSITION_ONLY == 0
	vo.col = vin.col;
	vo.spec = vin.spec;
	#if LIGHT_ON == 1
	float3 vnorm = normalize(mul((float3x3)normalMat, vin.normal));
	#endif
	#if pp_TwoVolumes == 1
		vo.col1 = vin.col1;
		vo.spec1 = vin.spec1;
		vo.uv1 = vin.uv1;
		#if LIGHT_ON == 1
			// FIXME need offset0 and offset1 for bump maps
			if (bumpMapping == 1)
				computeBumpMap(vo.spec, vo.spec1, vo.pos.xyz, vnorm, normalMat);
			else
			{
				computeColors(vo.col1, vo.spec1, 1, vo.pos.xyz, vnorm);
				#if pp_Texture == 0
					vo.col1 += vo.spec1;
				#endif
			}
			if (envMapping1 == 1)
				computeEnvMap(vo.uv1.xy, vnorm);
		#endif
	#endif
	#if LIGHT_ON == 1
		if (bumpMapping == 0)
		{
			computeColors(vo.col, vo.spec, 0, vo.pos.xyz, vnorm);
			#if pp_Texture == 0
					vo.col += vo.spec;
			#endif
		}
	#endif
	vo.uv.xy = vin.uv;
	#if LIGHT_ON == 1
		if (envMapping0 == 1)
			computeEnvMap(vo.uv.xy, vnorm);
	#endif
	vo.index = uint(polyNumber) + vin.vertexId;
#endif
#if MODIFIER_VOLUME == 1
	vo.pos.z = min(vo.pos.z, -0.001f);
#endif
	vo.pos = mul(projMat, vo.pos);

	vo.pos = float4(vo.pos.xy / vo.pos.w, 1.f / vo.pos.w, 1.f);
	vo.pos = mul(ndcMat, vo.pos);
#if POSITION_ONLY == 1
	vo.uv = float4(0.f, 0.f, 0.f, vo.pos.z);
#else
#if pp_Gouraud == 1
	vo.col *= vo.pos.z;
	vo.spec *= vo.pos.z;
#if pp_TwoVolumes == 1
	vo.col1 *= vo.pos.z;
	vo.spec1 *= vo.pos.z;
#endif
#endif
	vo.uv = float4(vo.uv.xy * vo.pos.z, 0.f, vo.pos.z);
#if pp_TwoVolumes == 1
	vo.uv1 *= vo.pos.z;
#endif
#endif
	vo.pos.w = 1.f;
	vo.pos.z = 0.f;

	return vo;
}


""";

    internal const string DX11N2ColorShader = """

#define PI 3.1415926f

#define LMODE_SINGLE_SIDED 0
#define LMODE_DOUBLE_SIDED 1
#define LMODE_DOUBLE_SIDED_WITH_TOLERANCE 2
#define LMODE_SPECIAL_EFFECT 3
#define LMODE_THIN_SURFACE 4
#define LMODE_BUMP_MAP 5

#define ROUTING_SPEC_TO_OFFSET 1
#define ROUTING_DIFF_TO_OFFSET 2
#define ROUTING_ATTENUATION 1	// not handled
#define ROUTING_FOG 2			// not handled
#define ROUTING_ALPHA 4
#define ROUTING_SUB 8

struct N2Light
{
	float4 color;
	float4 direction;
	float4 position;

	int parallel;
	int routing;
	int dmode;
	int smode;

	int4 diffuse_specular;		// diffuse0, diffuse1, specular0, specular1

	float attnDistA;
	float attnDistB;
	float attnAngleA;
	float attnAngleB;
	int distAttnMode;
	int3 _pad;
};

cbuffer lightConstants : register(b2)
{
	N2Light lights[16];

	float4 ambientBase[2];
	float4 ambientOffset[2];
	int4 ambientMaterial;		// base0, base1, offset0, offset1

	int lightCount;
	int useBaseOver;
	int bumpId0;
	int bumpId1;
}

void computeColors(inout float4 baseCol, inout float4 offsetCol, in int volIdx, in float3 position, in float3 normal)
{
	if (constantColor[volIdx] == 1)
		return;
	float3 diffuse = float3(0.f, 0.f, 0.f);
	float3 specular = float3(0.f, 0.f, 0.f);
	float diffuseAlpha = 0.f;
	float specularAlpha = 0.f;
	float3 reflectDir = reflect(normalize(position), normal);
	const float BASE_FACTOR = 2.0f;

	for (int i = 0; i < lightCount; i++)
	{
		N2Light light = lights[i];
		float3 lightDir; // direction to the light
		float3 lightColor = light.color.rgb;
		if (light.parallel == 1)
		{
			lightDir = normalize(light.direction.xyz);
		}
		else
		{
			lightDir = normalize(light.position.xyz - position);
			if (light.attnDistA != 1.f || light.attnDistB != 0.f)
			{
				float distance = length(light.position.xyz - position);
				if (light.distAttnMode == 0)
					distance = 1.f / distance;
				lightColor *= saturate(light.attnDistB * distance + light.attnDistA);
			}
			if (light.attnAngleA != 1.f || light.attnAngleB != 0.f)
			{
				float3 spotDir = light.direction.xyz;
				float cosAngle = 1.f - max(0.f, dot(lightDir, spotDir));
				lightColor *= saturate(cosAngle * light.attnAngleB + light.attnAngleA);
			}
		}
		int routing = light.routing;
		if (light.diffuse_specular[volIdx] == 1) // If light contributes to diffuse
		{
			float factor = (routing & ROUTING_SUB) != 0 ? -BASE_FACTOR : BASE_FACTOR;
			switch (light.dmode)
			{
			case LMODE_SINGLE_SIDED:
				factor *= max(dot(normal, lightDir), 0.f);
				break;
			case LMODE_DOUBLE_SIDED:
				factor *= abs(dot(normal, lightDir));
				break;
			}
			if ((routing & ROUTING_ALPHA) != 0)
				diffuseAlpha += lightColor.r * factor;
			else
			{
				if ((routing & ROUTING_DIFF_TO_OFFSET) == 0)
					diffuse += lightColor * factor * baseCol.rgb;
				else
					specular += lightColor * factor * baseCol.rgb;
			}
		}
		if (light.diffuse_specular[2 + volIdx] == 1) // If light contributes to specular
		{
			float factor = (routing & ROUTING_SUB) != 0 ? -BASE_FACTOR : BASE_FACTOR;
			switch (light.smode)
			{
			case LMODE_SINGLE_SIDED:
				factor *= saturate(pow(max(dot(lightDir, reflectDir), 0.f), glossCoef[volIdx]));
				break;
			case LMODE_DOUBLE_SIDED:
				factor *= saturate(pow(abs(dot(lightDir, reflectDir)), glossCoef[volIdx]));
				break;
			}
			if ((routing & ROUTING_ALPHA) != 0)
				specularAlpha += lightColor.r * factor;
			else
			{
				if ((routing & ROUTING_SPEC_TO_OFFSET) == 0)
					diffuse += lightColor * factor * offsetCol.rgb;
				else
					specular += lightColor * factor * offsetCol.rgb;
			}
		}
	}
	// ambient light
	if (ambientMaterial[volIdx] == 1)
		diffuse += ambientBase[volIdx].rgb * baseCol.rgb;
	else
		diffuse += ambientBase[volIdx].rgb;
	if (ambientMaterial[volIdx + 2] == 1)
		specular += ambientOffset[volIdx].rgb * offsetCol.rgb;
	else
		specular += ambientOffset[volIdx].rgb;

	baseCol.rgb = diffuse;
	offsetCol.rgb = specular;

	baseCol.a += diffuseAlpha;
	offsetCol.a += specularAlpha;
	if (useBaseOver == 1)
	{
		float4 overflow = max(0.f, baseCol - 1.f);
		offsetCol += overflow;
	}
	baseCol = saturate(baseCol);
	offsetCol = saturate(offsetCol);
}

void computeEnvMap(inout float2 uv, in float3 normal)
{
	// Cheap env mapping
	uv += normal.xy / 2.f + 0.5f;
	uv = saturate(uv);
}

void computeBumpMap(inout float4 color0, in float4 color1, in float3 position, in float3 normal, in float4x4 normalMat)
{
	// TODO
	//if (bumpId0 == -1)
		return;
	float3 tangent = color0.xyz;
	if (tangent.x > 0.5f)
		tangent.x -= 1.f;
	if (tangent.y > 0.5f)
		tangent.y -= 1.f;
	if (tangent.z > 0.5f)
		tangent.z -= 1.f;
	tangent = normalize(mul(normalMat, float4(tangent, 0.f))).xyz;
	float3 bitangent = color1.xyz;
	if (bitangent.x > 0.5f)
		bitangent.x -= 1.f;
	if (bitangent.y > 0.5f)
		bitangent.y -= 1.f;
	if (bitangent.z > 0.5f)
		bitangent.z -= 1.f;
	bitangent = normalize(mul(normalMat, float4(bitangent, 0.f))).xyz;

	float scaleDegree = color0.w;
	float scaleOffset = color1.w;

	N2Light light = lights[bumpId0];
	float3 lightDir; // direction to the light
	if (light.parallel == 1)
		lightDir = normalize(light.direction.xyz);
	else
		lightDir = normalize(light.position.xyz - position);

	float n = dot(lightDir, normal);
	float cosQ = dot(lightDir, tangent);
	float sinQ = dot(lightDir, bitangent);

	float sinT = saturate(n);
	float k1 = 1.f - scaleDegree;
	float k2 = scaleDegree * sinT;
	float k3 = scaleDegree * sqrt(1.f - sinT * sinT); // cos T

	float q = acos(cosQ);
	if (sinQ < 0.f)
		q = 2.f * PI - q;

	color0.r = k2;
	color0.g = k3;
	color0.b = q / PI / 2.f;
	color0.a = k1;
}


""";
}

internal unsafe sealed partial class PvrDx11Naomi2Helper
{
    [StructLayout(LayoutKind.Sequential, Size = 240)]
    private struct N2PolyConstants
    {
        internal fixed float MvMat[16];
        internal fixed float NormalMat[16];
        internal fixed float ProjMat[16];
        internal fixed int EnvMapping[2];
        internal int BumpMapping;
        internal int PolyNumber;
        internal fixed float GlossCoef[4];
        internal fixed int ConstantColor[4];
    }

    internal void Init(ID3D11Device device, ID3D11DeviceContext deviceContext)
    {
        _deviceContext = deviceContext;
        BufferDescription description = new()
        {
            ByteWidth = (uint)((((sizeof(N2PolyConstants) - 1) >> 4) + 1) << 4),
            Usage = ResourceUsage.Dynamic,
            BindFlags = BindFlags.ConstantBuffer,
            CPUAccessFlags = CpuAccessFlags.Write
        };
        if (device.CreateBuffer(description, null, out _polyConstantsBuffer).Failure)
            PvrRuntime.Warn("Per-polygon constants buffer creation failed");

        description.ByteWidth = (uint)((((sizeof(N2LightModel) - 1) >> 4) + 1) << 4);
        if (device.CreateBuffer(description, null, out _lightConstantsBuffer).Failure)
            PvrRuntime.Warn("Light constants buffer creation failed");
        ResetCache();
    }

    [SkipLocalsInit]
    internal void SetConstants(in PolyParam poly, uint polyNumber, RenderContext context)
    {
        Unsafe.SkipInit(out N2PolyConstants constants);
        N2Matrix mv = context.Matrices[poly.MvMatrix];
        N2Matrix normal = context.Matrices[poly.NormalMatrix];
        N2Matrix proj = context.Matrices[poly.ProjMatrix];
        Buffer.MemoryCopy(mv.Mat, constants.MvMat, 64, 64);
        Buffer.MemoryCopy(normal.Mat, constants.NormalMat, 64, 64);
        Buffer.MemoryCopy(proj.Mat, constants.ProjMat, 64, 64);
        constants.EnvMapping[0] = poly.EnvMapping[0] ? 1 : 0;
        constants.EnvMapping[1] = poly.EnvMapping[1] ? 1 : 0;
        constants.BumpMapping = poly.Pcw.Texture == 1 && poly.Tcw.PixelFmt == (uint)PixelFormat.PixelBumpMap ? 1 : 0;
        constants.PolyNumber = unchecked((int)polyNumber);
        for (int i = 0; i < 2; i++)
        {
            constants.GlossCoef[i] = poly.GlossCoef[i];
            constants.ConstantColor[i] = poly.ConstantColor[i] ? 1 : 0;
        }

        SetConstBuffer(_polyConstantsBuffer!, constants);
        _deviceContext!.VSSetConstantBuffer(1, _polyConstantsBuffer);
        if (poly.LightModel != _lastModel)
        {
            _lastModel = poly.LightModel;
            SetConstBuffer(_lightConstantsBuffer!, context.LightModels[poly.LightModel]);
            _deviceContext.VSSetConstantBuffer(2, _lightConstantsBuffer);
        }
    }

    [SkipLocalsInit]
    internal void SetConstants(float* mvMatrix, float* projMatrix)
    {
        Unsafe.SkipInit(out N2PolyConstants constants);
        Buffer.MemoryCopy(mvMatrix, constants.MvMat, 64, 64);
        Buffer.MemoryCopy(projMatrix, constants.ProjMat, 64, 64);
        SetConstBuffer(_polyConstantsBuffer!, constants);
        _deviceContext!.VSSetConstantBuffer(1, _polyConstantsBuffer);
    }
}
