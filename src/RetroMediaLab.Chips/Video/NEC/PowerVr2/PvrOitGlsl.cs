namespace RetroMediaLab.Chips.Video.NEC.PowerVr2;

internal static class PvrOitGlsl
{
    internal const string OitPolyParam = """
 
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
 
void setFragDepth(float z) 
{ 
#if DIV_POS_Z == 1 
	float w = 100000.0 / z; 
#else 
	float w = 100000.0 * z; 
#endif 
	gl_FragDepth = log2(1.0 + max(w, -0.999999)) / 34.0; 
} 

struct PolyParam { 
	int tsp_isp_pcw; 
	int tsp1; 
}; 
 
#define GET_TSP_FOR_AREA(pp, area1) ((area1) ? (pp).tsp1 : (pp).tsp_isp_pcw) 
 
#define getSrcBlendFunc(pp, area1) ((GET_TSP_FOR_AREA(pp, area1) >> 29) & 7) 
 
#define getDstBlendFunc(pp, area1) ((GET_TSP_FOR_AREA(pp, area1) >> 26) & 7) 
 
#define getSrcSelect(pp, area1) (((GET_TSP_FOR_AREA(pp, area1) >> 25) & 1) != 0) 
 
#define getDstSelect(pp, area1) (((GET_TSP_FOR_AREA(pp, area1) >> 24) & 1) != 0) 
 
#define getFogControl(pp, area1) ((GET_TSP_FOR_AREA(pp, area1) >> 22) & 3) 
 
#define getUseAlpha(pp, area1) (((GET_TSP_FOR_AREA(pp, area1) >> 20) & 1) != 0) 
 
#define getIgnoreTexAlpha(pp, area1) (((GET_TSP_FOR_AREA(pp, area1) >> 19) & 1) != 0) 
 
#define getShadingInstruction(pp, area1) ((GET_TSP_FOR_AREA(pp, area1) >> 6) & 3) 
 
#define getDepthFunc(pp) (((pp).tsp_isp_pcw >> 13) & 7) 
 
#define getDepthMask(pp) ((((pp).tsp_isp_pcw >> 10) & 1) != 1) 
 
#define getShadowEnable(pp) (((pp).tsp_isp_pcw & 1) != 0) 
 
#define getPolyNumber(pixel) (((pixel).seq_num & 0x3FFFFFFFu) >> 17) 
 
#define getPolyIndex(pixel) ((pixel).seq_num & 0x3FFFFFFFu) 
 
#define SHADOW_STENCIL 0x40000000u 
#define SHADOW_ACC	   0x80000000u 
 
#define isShadowed(pixel) (((pixel).seq_num & SHADOW_ACC) == SHADOW_ACC) 
 
#define isTwoVolumes(pp) ((pp).tsp1 != -1) 
 
uint packColors(vec4 v) 
{ 
	return (uint(round(v.r * 255.0)) << 24) | (uint(round(v.g * 255.0)) << 16) | (uint(round(v.b * 255.0)) << 8) | uint(round(v.a * 255.0)); 
} 
 
vec4 unpackColors(uint u) 
{ 
	return vec4(float((u >> 24) & 255u) / 255.0, float((u >> 16) & 255u) / 255.0, float((u >> 8) & 255u) / 255.0, float(u & 255u) / 255.0); 
} 

""";
}
