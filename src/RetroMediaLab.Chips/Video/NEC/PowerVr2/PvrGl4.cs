using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static RetroMediaLab.Chips.Video.NEC.PowerVr2.PvrRegisters;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2;

internal static unsafe partial class PvrGl4
{
    internal const string ShaderHeader = """

layout(r32ui, binding = 4) uniform coherent restrict highp uimage2D abufferPointerImg;

layout(binding = 0, offset = 0) uniform atomic_uint buffer_index;

""" + PvrOitGlsl.OitPolyParam + """

layout (binding = 0, std430) coherent restrict buffer PixelBuffer {
	Pixel pixels[];
};

layout (binding = 1, std430) readonly buffer TrPolyParamBuffer {
	PolyParam tr_poly_params[];
};

""";

    private const string Gl4VertexShaderSource = """

#if pp_Gouraud == 0
#define INTERPOLATION flat
#else
#define INTERPOLATION
#endif

// Uniforms 
uniform mat4 ndcMat;
uniform int pp_Number;

// Input
in vec4 in_pos;
in vec4 in_base;
in vec4 in_offs;
in vec2 in_uv;
#if pp_TwoVolumes == 1
in vec4 in_base1;
in vec4 in_offs1;
in vec2 in_uv1;
#endif

// Output
INTERPOLATION out vec4 vtx_base;
INTERPOLATION out vec4 vtx_offs;
out vec3 vtx_uv;
#if pp_TwoVolumes == 1
INTERPOLATION out vec4 vtx_base1;
INTERPOLATION out vec4 vtx_offs1;
out vec2 vtx_uv1;
#endif
flat out uint vtx_index;

void main()
{
	vec4 vpos = ndcMat * in_pos;
	#if DIV_POS_Z == 1
		vpos /= vpos.z;
		vpos.z = vpos.w;
	#endif
	vtx_base = in_base;
	vtx_offs = in_offs;
	vtx_uv = vec3(in_uv, vpos.z);
	#if pp_TwoVolumes == 1
		vtx_base1 = in_base1;
		vtx_offs1 = in_offs1;
		vtx_uv1 = in_uv1;
	#endif
	vtx_index = uint(pp_Number) + uint(gl_VertexID);
	#if pp_Gouraud == 1 && DIV_POS_Z != 1
		vtx_base *= vpos.z;
		vtx_offs *= vpos.z;
		#if pp_TwoVolumes == 1
			vtx_base1 *= vpos.z;
			vtx_offs1 *= vpos.z;
		#endif
	#endif
	
	#if DIV_POS_Z != 1
		vtx_uv.xy *= vpos.z;
		#if pp_TwoVolumes == 1
			vtx_uv1 *= vpos.z;
		#endif
		vpos.w = 1.0;
		vpos.z = 0.0;
	#endif
	gl_Position = vpos;
}

""";

    internal const string Gl4PixelPipelineShader = """

#define PI 3.1415926

#define PASS_DEPTH 0
#define PASS_COLOR 1
#define PASS_OIT 2

#if PASS == PASS_DEPTH || PASS == PASS_COLOR || NOUVEAU == 1
out vec4 FragColor;
#endif

#if pp_TwoVolumes == 1
#define IF(x) if (x)
#else
#define IF(x)
#endif

#if pp_Gouraud == 0
#define INTERPOLATION flat
#else
#define INTERPOLATION
#endif

// Uniforms
uniform float cp_AlphaTestValue;
uniform vec4 pp_ClipTest;
uniform vec3 sp_FOG_COL_RAM,sp_FOG_COL_VERT;
uniform float sp_FOG_DENSITY;
uniform float shade_scale_factor;
uniform sampler2D tex0, tex1;
layout(binding = 5) uniform sampler2D fog_table;
uniform highp usampler2D shadow_stencil;
uniform sampler2D DepthTex;
uniform float trilinear_alpha;
uniform vec4 fog_clamp_min;
uniform vec4 fog_clamp_max;
#if pp_Palette != 0
uniform sampler2D palette;
uniform int palette_index;
#endif

uniform ivec2 blend_mode[2];
#if pp_TwoVolumes == 1
uniform bool use_alpha[2];
uniform bool ignore_tex_alpha[2];
uniform int shading_instr[2];
uniform int fog_control[2];
#endif

// Input
INTERPOLATION in vec4 vtx_base;
INTERPOLATION in vec4 vtx_offs;
in vec3 vtx_uv;
#if pp_TwoVolumes == 1
INTERPOLATION in vec4 vtx_base1;
INTERPOLATION in vec4 vtx_offs1;
in vec2 vtx_uv1;
#endif
flat in uint vtx_index;

float fog_mode2(float w)
{
	float z = clamp(
#if DIV_POS_Z == 1
					sp_FOG_DENSITY / w
#else
					sp_FOG_DENSITY * w
#endif
									  , 1.0, 255.9999);
	float exp = floor(log2(z));
	float m = z * 16.0 / pow(2.0, exp) - 16.0;
	float idx = floor(m) + exp * 16.0 + 0.5;
	vec4 fog_coef = texture(fog_table, vec2(idx / 128.0, 0.75 - (m - floor(m)) / 2.0));
	return fog_coef.r;
}

vec4 fog_clamp(vec4 col)
{
#if FogClamping == 1
	return clamp(col, fog_clamp_min, fog_clamp_max);
#else
	return col;
#endif
}

#if pp_Palette != 0

vec4 getPaletteEntry(float colIdx)
{
	int color_idx = int(floor(colIdx * 255.0 + 0.5)) + palette_index;
	ivec2 c = ivec2(color_idx % 32, color_idx / 32);
	return texelFetch(palette, c, 0);
}

#endif

#if pp_Palette == 1

vec4 palettePixel(sampler2D tex, vec3 coords)
{
#if DIV_POS_Z == 1
	return getPaletteEntry(texture(tex, coords.xy).r);
#else
	return getPaletteEntry(textureProj(tex, coords).r);
#endif
}

#elif pp_Palette == 2

vec4 palettePixelBilinear(sampler2D tex, vec3 coords)
{
#if DIV_POS_Z == 0
	coords.xy /= coords.z;
#endif
	vec2 texSize = vec2(textureSize(tex, 0));
	vec2 pixCoord = coords.xy * texSize - 0.5;			// coordinates of top left pixel
	vec2 originPixCoord = floor(pixCoord);

	vec2 sampleUV = (originPixCoord + 0.5) / texSize;	// UV coordinates of center of top left pixel

    // Sample from all surrounding texels
    vec4 c00 = getPaletteEntry(texture(tex, sampleUV).r);
    vec4 c01 = getPaletteEntry(textureOffset(tex, sampleUV, ivec2(0, 1)).r);
    vec4 c11 = getPaletteEntry(textureOffset(tex, sampleUV, ivec2(1, 1)).r);
    vec4 c10 = getPaletteEntry(textureOffset(tex, sampleUV, ivec2(1, 0)).r);

	vec2 weight = pixCoord - originPixCoord;

    // Bi-linear mixing
    vec4 temp0 = mix(c00, c10, weight.x);
    vec4 temp1 = mix(c01, c11, weight.x);
    return mix(temp0, temp1, weight.y);
}

#endif

#if PASS == PASS_OIT

uint getNextPixelIndex()
{
	uint index = atomicCounterIncrement(buffer_index);
	if (index >= uint(pixels.length()))
		// Buffer overflow
		discard;

	return index;
}

#endif

void main()
{
	setFragDepth(vtx_uv.z);
	
	#if PASS == PASS_OIT
		// Manual depth testing
		float frontDepth = texture(DepthTex, gl_FragCoord.xy / vec2(textureSize(DepthTex, 0))).r;
		if (gl_FragDepth < frontDepth)
			discard;
	#endif
	
	// Clip inside the box
	#if pp_ClipInside == 1
		if (gl_FragCoord.x >= pp_ClipTest.x && gl_FragCoord.x <= pp_ClipTest.z
				&& gl_FragCoord.y >= pp_ClipTest.y && gl_FragCoord.y <= pp_ClipTest.w)
			discard;
	#endif
	
	vec4 color = vtx_base;
	vec4 offset = vtx_offs;
	bool area1 = false;
	ivec2 cur_blend_mode = blend_mode[0];
	
	#if pp_TwoVolumes == 1
		bool cur_use_alpha = use_alpha[0];
		bool cur_ignore_tex_alpha = ignore_tex_alpha[0];
		int cur_shading_instr = shading_instr[0];
		int cur_fog_control = fog_control[0];
		#if PASS == PASS_COLOR
			uvec4 stencil = texture(shadow_stencil, gl_FragCoord.xy / vec2(textureSize(shadow_stencil, 0)));
			if (stencil.r == 0x81u) {
				color = vtx_base1;
				offset = vtx_offs1;
				area1 = true;
				cur_blend_mode = blend_mode[1];
				cur_use_alpha = use_alpha[1];
				cur_ignore_tex_alpha = ignore_tex_alpha[1];
				cur_shading_instr = shading_instr[1];
				cur_fog_control = fog_control[1];
			}
		#endif
	#endif
	#if pp_Gouraud == 1 && DIV_POS_Z != 1
		color /= vtx_uv.z;
		offset /= vtx_uv.z;
	#endif
	
	#if pp_UseAlpha==0 || pp_TwoVolumes == 1
		IF(!cur_use_alpha)
			color.a=1.0;
	#endif
	#if pp_FogCtrl==3 || pp_TwoVolumes == 1 // LUT Mode 2
		IF(cur_fog_control == 3)
			color = vec4(sp_FOG_COL_RAM.rgb, fog_mode2(vtx_uv.z));
	#endif
	#if pp_Texture==1
	{
		vec4 texcol;
		#if pp_TwoVolumes == 1
			if (area1)
				#if DIV_POS_Z == 1
					texcol = texture(tex1, vtx_uv1);
				#else
					texcol = textureProj(tex1, vec3(vtx_uv1.xy, vtx_uv.z));
				#endif
			else
		#endif
		#if pp_Palette == 0
			#if DIV_POS_Z == 1
				texcol = texture(tex0, vtx_uv.xy);
			#else
				texcol = textureProj(tex0, vtx_uv);
			#endif
		#elif pp_Palette == 1
			texcol = palettePixel(tex0, vtx_uv);
		#else
			texcol = palettePixelBilinear(tex0, vtx_uv);
		#endif

		#if pp_BumpMap == 1
			float s = PI / 2.0 * (texcol.a * 15.0 * 16.0 + texcol.r * 15.0) / 255.0;
			float r = 2.0 * PI * (texcol.g * 15.0 * 16.0 + texcol.b * 15.0) / 255.0;
			texcol.a = clamp(offset.a + offset.r * sin(s) + offset.g * cos(s) * cos(r - 2.0 * PI * offset.b), 0.0, 1.0);
			texcol.rgb = vec3(1.0, 1.0, 1.0);	
		#else
			#if pp_IgnoreTexA==1 || pp_TwoVolumes == 1
				IF(cur_ignore_tex_alpha)
					texcol.a=1.0;	
			#endif
		#endif
		#if pp_ShadInstr==0 || pp_TwoVolumes == 1 // DECAL
		IF(cur_shading_instr == 0)
		{
			color=texcol;
		}
		#endif
		#if pp_ShadInstr==1 || pp_TwoVolumes == 1 // MODULATE
		IF(cur_shading_instr == 1)
		{
			color.rgb*=texcol.rgb;
			color.a=texcol.a;
		}
		#endif
		#if pp_ShadInstr==2 || pp_TwoVolumes == 1 // DECAL ALPHA
		IF(cur_shading_instr == 2)
		{
			color.rgb=mix(color.rgb,texcol.rgb,texcol.a);
		}
		#endif
		#if  pp_ShadInstr==3 || pp_TwoVolumes == 1 // MODULATE ALPHA
		IF(cur_shading_instr == 3)
		{
			color*=texcol;
		}
		#endif
		
		#if pp_Offset==1 && pp_BumpMap == 0
		{
			color.rgb += offset.rgb;
		}
		#endif
	}
	#endif
	#if PASS == PASS_COLOR && pp_TwoVolumes == 0
		uvec4 stencil = texture(shadow_stencil, gl_FragCoord.xy / vec2(textureSize(shadow_stencil, 0)));
		if (stencil.r == 0x81u)
			color.rgb *= shade_scale_factor;
	#endif
	
	color = fog_clamp(color);
	
	#if pp_FogCtrl==0 || pp_TwoVolumes == 1 // LUT
		IF(cur_fog_control == 0)
		{
			color.rgb = mix(color.rgb, sp_FOG_COL_RAM.rgb, fog_mode2(vtx_uv.z)); 
		}
	#endif
	#if pp_Offset==1 && pp_BumpMap == 0 && (pp_FogCtrl == 1 || pp_TwoVolumes == 1)  // Per vertex
		IF(cur_fog_control == 1)
		{
			color.rgb=mix(color.rgb, sp_FOG_COL_VERT.rgb, offset.a);
		}
	#endif
	
	color *= trilinear_alpha;
	
	#if cp_AlphaTest == 1
		color.a = round(color.a * 255.0) / 255.0;
		if (cp_AlphaTestValue > color.a)
			discard;
		color.a = 1.0;
	#endif

	//color.rgb=vec3(gl_FragCoord.w * sp_FOG_DENSITY / 128.0);
	
	#if PASS == PASS_COLOR 
		FragColor = color;
	#elif PASS == PASS_OIT
		ivec2 coords = ivec2(gl_FragCoord.xy);
		uint idx =  getNextPixelIndex();
		
		Pixel pixel;
		pixel.color = packColors(clamp(color, vec4(0.0), vec4(1.0)));
		pixel.depth = gl_FragDepth;
		pixel.seq_num = vtx_index;
		pixel.next = imageAtomicExchange(abufferPointerImg, coords, idx);
		pixels[idx] = pixel;
		
#if NOUVEAU == 0
		discard;
#else
		// nouveau may be optimizing a bit too aggressively here
		FragColor = vec4(0.0);
#endif
		
	#endif
}

""";

    private const string ModifierVolumeShader = """

in vec3 vtx_uv;

void main()
{
	setFragDepth(vtx_uv.z);
}

""";

    private sealed class Vertex4Source : PvrOpenGl4Source
    {
        internal Vertex4Source(bool gouraud, bool divPosZ, bool twoVolumes)
        {
            AddConstant("pp_TwoVolumes", twoVolumes ? 1 : 0);
            AddConstant("pp_Gouraud", gouraud ? 1 : 0);
            AddConstant("DIV_POS_Z", divPosZ ? 1 : 0);
            AddSource(Gl4VertexShaderSource);
        }
    }

    private sealed class Fragment4ShaderSource : PvrOpenGl4Source
    {
        internal Fragment4ShaderSource(PvrGl4PipelineShader shader)
        {
            AddConstant("cp_AlphaTest", shader.AlphaTest ? 1 : 0);
            AddConstant("pp_ClipInside", shader.InsideClipping ? 1 : 0);
            AddConstant("pp_UseAlpha", shader.UseAlpha ? 1 : 0);
            AddConstant("pp_Texture", shader.Texture ? 1 : 0);
            AddConstant("pp_IgnoreTexA", shader.IgnoreTexA ? 1 : 0);
            AddConstant("pp_ShadInstr", (int)shader.ShadInstr);
            AddConstant("pp_Offset", shader.Offset ? 1 : 0);
            AddConstant("pp_FogCtrl", (int)shader.FogCtrl);
            AddConstant("pp_TwoVolumes", shader.TwoVolumes ? 1 : 0);
            AddConstant("pp_Gouraud", shader.Gouraud ? 1 : 0);
            AddConstant("pp_BumpMap", shader.BumpMap ? 1 : 0);
            AddConstant("FogClamping", shader.FogClamping ? 1 : 0);
            AddConstant("pp_Palette", shader.Palette);
            AddConstant("NOUVEAU", PvrGles.Gl.MesaNouveau ? 1 : 0);
            AddConstant("PASS", (int)shader.Pass);
            AddConstant("DIV_POS_Z", shader.DivPosZ ? 1 : 0);

            AddSource(ShaderHeader);
            AddSource(Gl4PixelPipelineShader);
        }
    }

    internal static bool CompilePipelineShader(PvrGl4PipelineShader shader, string? fragmentSource = null,
        string? vertexSource = null)
    {
        string defaultVertexSource = shader.Naomi2
            ? new PvrN2Vertex4Source(shader).Generate()
            : new Vertex4Source(shader.Gouraud, shader.DivPosZ, shader.TwoVolumes).Generate();
        var defaultFragmentSource = new Fragment4ShaderSource(shader);

        shader.Program = PvrGles.CompileAndLink(vertexSource ?? defaultVertexSource,
            fragmentSource ?? defaultFragmentSource.Generate());
        uint program = shader.Program;
        int uniform = PvrGl.GetUniformLocation(program, "tex0");
        if (shader.Texture && uniform != -1)
            PvrGl.Uniform1i(uniform, 0);
        uniform = PvrGl.GetUniformLocation(program, "tex1");
        if (shader.Texture && uniform != -1)
            PvrGl.Uniform1i(uniform, 1);

        shader.ClipTest = PvrGl.GetUniformLocation(program, "pp_ClipTest");
        shader.FogDensity = PvrGl.GetUniformLocation(program, "sp_FOG_DENSITY");
        shader.AlphaTestValue = PvrGl.GetUniformLocation(program, "cp_AlphaTestValue");
        shader.FogColorVertex = shader.FogCtrl == 1 && shader.Texture ? PvrGl.GetUniformLocation(program, "sp_FOG_COL_VERT") : -1;
        shader.FogColorRam = shader.FogCtrl is 0 or 3 ? PvrGl.GetUniformLocation(program, "sp_FOG_COL_RAM") : -1;
        shader.ShadeScaleFactor = PvrGl.GetUniformLocation(program, "shade_scale_factor");

        uniform = PvrGl.GetUniformLocation(program, "DepthTex");
        if (uniform != -1)
            PvrGl.Uniform1i(uniform, 2);

        shader.TrilinearAlpha = PvrGl.GetUniformLocation(program, "trilinear_alpha");
        if (shader.FogClamping)
        {
            shader.FogClampMin = PvrGl.GetUniformLocation(program, "fog_clamp_min");
            shader.FogClampMax = PvrGl.GetUniformLocation(program, "fog_clamp_max");
        }
        else
        {
            shader.FogClampMin = -1;
            shader.FogClampMax = -1;
        }
        shader.NdcMat = PvrGl.GetUniformLocation(program, "ndcMat");

        uniform = PvrGl.GetUniformLocation(program, "shadow_stencil");
        if (uniform != -1)
            PvrGl.Uniform1i(uniform, 3);

        shader.PolyNumber = PvrGl.GetUniformLocation(program, "pp_Number");
        shader.BlendMode = PvrGl.GetUniformLocation(program, "blend_mode");
        shader.UseAlphaUniform = PvrGl.GetUniformLocation(program, "use_alpha");
        shader.IgnoreTexAlpha = PvrGl.GetUniformLocation(program, "ignore_tex_alpha");
        shader.ShadingInstr = PvrGl.GetUniformLocation(program, "shading_instr");
        shader.FogControl = PvrGl.GetUniformLocation(program, "fog_control");

        uniform = PvrGl.GetUniformLocation(program, "palette");
        if (uniform != -1)
            PvrGl.Uniform1i(uniform, 6);
        shader.PaletteIndex = PvrGl.GetUniformLocation(program, "palette_index");
        shader.DitherDivisor = PvrGl.GetUniformLocation(program, "ditherDivisor");

        if (shader.Naomi2)
            PvrGlNaomi2.InitN2Uniforms(shader);

        return PvrGl.IsProgram(program) != 0;
    }

    private static void DeleteShaders()
    {
        PvrGlCache cache = PvrGlCache.Instance;
        foreach (PvrGl4PipelineShader shader in Gl.Shaders.Values)
            if (shader.Program != 0)
                cache.DeleteProgram(shader.Program);
        Gl.Shaders.Clear();
        cache.DeleteProgram(Gl.ModvolShader.Program);
        Gl.ModvolShader.Program = 0;
        cache.DeleteProgram(Gl.N2ModvolShader.Program);
        Gl.N2ModvolShader.Program = 0;
    }

    internal static void Term()
    {
        try
        {
            foreach (PvrGlBuffer? buffer in Gl.Vbo.Geometry)
                buffer?.Dispose();
            foreach (PvrGlBuffer? buffer in Gl.Vbo.Modvols)
                buffer?.Dispose();
            foreach (PvrGlBuffer? buffer in Gl.Vbo.Indices)
                buffer?.Dispose();
            foreach (PvrGlBuffer? buffer in Gl.Vbo.TrPolyParams)
                buffer?.Dispose();
            Array.Clear(Gl.Vbo.Geometry);
            Array.Clear(Gl.Vbo.Modvols);
            Array.Clear(Gl.Vbo.Indices);
            Array.Clear(Gl.Vbo.TrPolyParams);
            DeleteShaders();
            foreach (PvrGl4MainVertexArray vao in Gl.Vbo.MainVao)
                vao.Term();
            foreach (PvrGl4ModvolVertexArray vao in Gl.Vbo.ModvolVao)
                vao.Term();
        }
        finally
        {
            if (PvrGles.Gl.Ofbo.OriginalFbo != 0)
                PvrGl.BindFramebuffer(PvrGl.Framebuffer, PvrGles.Gl.Ofbo.OriginalFbo);
            PvrGl.ColorMask(1, 1, 1, 1);
            PvrGl.ActiveTexture(PvrGl.Texture0);
            PvrGl.BindSampler(0, 0);
        }
    }

    internal static void CreateModvolShader()
    {
        if (Gl.ModvolShader.Program != 0)
            return;
        bool nativeDepthInterpolation = PvrConfiguration.NativeDepthInterpolation;
        var vertexShader = new Vertex4Source(false, nativeDepthInterpolation, false);
        var fragmentShader = new PvrOpenGl4Source();
        fragmentShader.AddConstant("DIV_POS_Z", nativeDepthInterpolation ? 1 : 0)
            .AddSource(ShaderHeader)
            .AddSource(ModifierVolumeShader);

        Gl.ModvolShader.Program = PvrGles.CompileAndLink(vertexShader.Generate(), fragmentShader.Generate());
        Gl.ModvolShader.NdcMat = unchecked((uint)PvrGl.GetUniformLocation(Gl.ModvolShader.Program, "ndcMat"));
        var n2VertexShader = new PvrN2Vertex4Source();
        n2VertexShader.SetConstant("MODIFIER_VOLUME", 1);
        fragmentShader.SetConstant("DIV_POS_Z", 0);
        Gl.N2ModvolShader.Program = PvrGles.CompileAndLink(n2VertexShader.Generate(), fragmentShader.Generate());
        Gl.N2ModvolShader.NdcMat = unchecked((uint)PvrGl.GetUniformLocation(Gl.N2ModvolShader.Program, "ndcMat"));
        Gl.N2ModvolShader.MvMat = PvrGl.GetUniformLocation(Gl.N2ModvolShader.Program, "mvMat");
        Gl.N2ModvolShader.ProjMat = PvrGl.GetUniformLocation(Gl.N2ModvolShader.Program, "projMat");
    }

    internal static void CreateResources()
    {
        if (Gl.Vbo.Geometry[0] != null)
            return;
        for (int i = 0; i < Gl.Vbo.Geometry.Length; i++)
        {
            Gl.Vbo.Geometry[i] = new PvrGlBuffer(PvrGl.ArrayBuffer);
            Gl.Vbo.Modvols[i] = new PvrGlBuffer(PvrGl.ArrayBuffer);
            Gl.Vbo.Indices[i] = new PvrGlBuffer(PvrGl.ElementArrayBuffer);
            Gl.Vbo.TrPolyParams[i] = new PvrGlBuffer(ShaderStorageBuffer);
            Gl.Vbo.BufferIndex = i;
            SetupMainVbo();
            SetupModvolVbo();
        }
        PvrGlVertexArray.Unbind();
        PvrGles.Gl.QuadDrawer = new PvrGlQuadDrawer();
        PvrGles.Check();
    }

    internal static void Resize(int width, int height)
    {
        if (width > MaxImageWidth || height > MaxImageHeight || StencilTextureId == 0)
        {
            if (width > MaxImageWidth)
                MaxImageWidth = width;
            if (height > MaxImageHeight)
                MaxImageHeight = height;
            PvrGlCache cache = PvrGlCache.Instance;
            if (StencilTextureId != 0)
            {
                fixed (uint* stencil = &StencilTextureId)
                    cache.DeleteTextures(1, stencil);
                StencilTextureId = 0;
            }
            if (DepthTextureId != 0)
            {
                fixed (uint* depth = &DepthTextureId)
                    cache.DeleteTextures(1, depth);
                DepthTextureId = 0;
            }
            if (OpaqueTextureId[0] != 0)
            {
                fixed (uint* opaque = OpaqueTextureId)
                    cache.DeleteTextures(2, opaque);
                OpaqueTextureId[0] = 0;
            }
            CreateTextures(MaxImageWidth, MaxImageHeight);
            ReshapeABuffer(MaxImageWidth, MaxImageHeight);
        }
    }

    internal static PvrRenderBackend CreateRenderer() => new PvrOpenGl4Renderer();
}

internal sealed unsafe class PvrOpenGl4Renderer : PvrOpenGlRenderer
{
    internal override bool Init()
    {
        PvrGles.FindGlVersion();
        PvrGlContext gl = PvrGles.Gl;
        if ((!gl.IsGles && (gl.Major < 4 || (gl.Major == 4 && gl.Minor < 3)))
            || (gl.IsGles && (gl.Major < 3 || (gl.Major == 3 && gl.Minor < 2))))
        {
            PvrRuntime.Log.Warn("Warning: OpenGL version doesn't support per-pixel sorting.");
            return false;
        }
        PvrRuntime.Log.Info("Per-pixel sorting enabled");
        PvrGlCache.Instance.DisableCache();
        PvrGl4.CreateResources();
        PvrGl4.InitABuffer();

        if (PvrConfiguration.TextureUpscale > 1)
        {
            uint* source = stackalloc uint[4] { 0x11111111, 0x22222222, 0x33333333, 0x44444444 };
            uint* destination = stackalloc uint[16];
            PvrTextureCache.UpscaleXbrz(2, source, destination, 2, 2, false);
        }
        UpdateFogTable = true;
        PvrGlTextureCacheData.SetDirectXColorOrder(false);
        PvrGlTextureCacheData.SetUploadToGpuFlavor();
        return true;
    }

    internal override void Term()
    {
        try
        {
            PvrGl4.TermABuffer();
            PvrGlCache cache = PvrGlCache.Instance;
            fixed (uint* stencil = &PvrGl4.StencilTextureId)
                cache.DeleteTextures(1, stencil);
            PvrGl4.StencilTextureId = 0;
            fixed (uint* depth = &PvrGl4.DepthTextureId)
                cache.DeleteTextures(1, depth);
            PvrGl4.DepthTextureId = 0;
            fixed (uint* opaque = PvrGl4.OpaqueTextureId)
                cache.DeleteTextures(2, opaque);
            PvrGl4.OpaqueTextureId[0] = PvrGl4.OpaqueTextureId[1] = 0;
            fixed (uint* fbo = PvrGl4.GeometryFbo)
                PvrGl.DeleteFramebuffers(2, fbo);
            PvrGl4.GeometryFbo[0] = PvrGl4.GeometryFbo[1] = 0;
            fixed (uint* samplers = PvrGl4.TextureSamplers)
                PvrGl.DeleteSamplers(2, samplers);
            PvrGl4.TextureSamplers[0] = PvrGl4.TextureSamplers[1] = 0;
            PvrGles.TexCache.Clear();
            PvrGles.TermGlCommon();
        }
        finally
        {
            PvrGl4.Term();
        }
    }

    internal override bool Render()
    {
        SaveCurrentFramebuffer();
        try
        {
            RenderContext context = PvrGles.Gl.RendContext!;
            RenderFrame((int)context.FramebufferWidth, (int)context.FramebufferHeight);
            if (context.IsRtt)
                return false;
            if (!PvrConfiguration.EmulateFramebuffer)
            {
                FrameRendered = true;
                ClearLastFrame = false;
                DrawOsd();
                RenderVideoRouting();
            }
            return true;
        }
        finally
        {
            RestoreCurrentFramebuffer();
        }
    }

    protected override uint GetFogTextureSlot() => PvrGl.Texture5;
    protected override uint GetPaletteTextureSlot() => PvrGl.Texture6;

    private bool RenderFrame(int renderingWidth, int renderingHeight)
    {
        if (!PvrConfiguration.EmulateFramebuffer)
            InitVideoRoutingFramebuffer();

        PvrGlContext gl = PvrGles.Gl;
        RenderContext context = gl.RendContext!;
        PvrGl4ShaderUniforms uniforms = PvrGl4.ShaderUniforms;
        PvrGlCache cache = PvrGlCache.Instance;
        bool isRtt = context.IsRtt;
        gl.Matrices.CalcMatrices(context, renderingWidth, renderingHeight);
        uniforms.NdcMat = gl.Matrices.GetNormalMatrix();

        if (!isRtt)
            PvrTransformMatrix.GetVideoShift(out gl.Ofbo.ShiftX, out gl.Ofbo.ShiftY);
        PvrGl4.Resize(renderingWidth, renderingHeight);

        FOG_COL_VERT.GetRGBColor(uniforms.FogColorVertex);
        FOG_COL_RAM.GetRGBColor(uniforms.FogColorRam);
        uniforms.FogDensity = FOG_DENSITY.Get() * PvrConfiguration.ExtraDepthScale;
        context.FogClampMin.GetRGBAColor(uniforms.FogClampMin);
        context.FogClampMax.GetRGBAColor(uniforms.FogClampMax);

        if (PvrConfiguration.ModifierVolumes)
        {
            PvrGl4.CreateModvolShader();
            cache.UseProgram(PvrGl4.Gl.ModvolShader.Program);
            fixed (Matrix4x4* matrix = &uniforms.NdcMat)
                PvrGl.UniformMatrix4fv(unchecked((int)PvrGl4.Gl.ModvolShader.NdcMat), 1, 0, (float*)matrix);

            cache.UseProgram(PvrGl4.Gl.N2ModvolShader.Program);
            fixed (Matrix4x4* matrix = &uniforms.NdcMat)
                PvrGl.UniformMatrix4fv(unchecked((int)PvrGl4.Gl.N2ModvolShader.NdcMat), 1, 0, (float*)matrix);
        }
        foreach (PvrGl4PipelineShader shader in PvrGl4.Gl.Shaders.Values)
            PvrGlNaomi2.ResetN2UniformCache(shader);
        uniforms.PtAlpha = (PT_ALPHA_REF & 0xff) / 255.0f;
        PvrGl.Enable(PvrGles.PrimitiveRestartFixedIndex);

        uint outputFbo;
        if (isRtt)
        {
            outputFbo = PvrGles.BindRtt(false);
        }
        else
        {
            Width = renderingWidth;
            Height = renderingHeight;
            outputFbo = PvrGles.InitOutputFramebuffer(renderingWidth, renderingHeight);
        }
        if (outputFbo == 0)
            return false;

        PvrGl4Context.VertexBuffers vbo = PvrGl4.Gl.Vbo;
        vbo.NextBuffer();
        cache.Disable(PvrGl.ScissorTest);
        if (!isRtt)
            cache.ClearColor(VO_BORDER_COL.Red(), VO_BORDER_COL.Green(), VO_BORDER_COL.Blue(), 1f);

        fixed (TaVertex* vertices = CollectionsMarshal.AsSpan(context.Verts))
            vbo.GetVertexBuffer().Update(vertices, context.Verts.Count * sizeof(TaVertex));
        fixed (uint* indices = CollectionsMarshal.AsSpan(context.Idx))
            vbo.GetIndexBuffer().Update(indices, context.Idx.Count * sizeof(uint));
        if (context.ModTrig.Count != 0)
            fixed (ModTriangle* modifiers = CollectionsMarshal.AsSpan(context.ModTrig))
                vbo.GetModVolBuffer().Update(modifiers, context.ModTrig.Count * sizeof(ModTriangle));
        if (context.GlobalParamTr.Count != 0)
        {
            uint[] parameters = new uint[context.GlobalParamTr.Count * 2];
            int i = 0;
            foreach (PolyParam polygon in context.GlobalParamTr)
            {
                parameters[i++] = (polygon.Tsp.full & 0xffff00c0) | ((polygon.Isp.full >> 16) & 0xe400) | ((polygon.Pcw.full >> 7) & 1);
                parameters[i++] = polygon.Tsp1.full;
            }
            fixed (uint* data = parameters)
                vbo.GetPolyParamBuffer().Update(data, parameters.Length * sizeof(uint));
        }
        else
        {
            Unsafe.SkipInit(out uint dummy);
            vbo.GetPolyParamBuffer().Update(&dummy, sizeof(uint));
        }
        PvrGl.BindBufferBase(PvrGl4.ShaderStorageBuffer, 1, vbo.GetPolyParamBuffer().GetName());
        PvrGles.Check();

        PvrRect scissor = gl.Matrices.GetBaseScissor();
        uniforms.BaseClipping.X = scissor.Origin.X;
        uniforms.BaseClipping.Y = scissor.Origin.Y;
        uniforms.BaseClipping.Width = scissor.Size.X;
        uniforms.BaseClipping.Height = scissor.Size.Y;
        uniforms.BaseClipping.Enabled = isRtt || scissor.Origin.X != 0 || scissor.Origin.Y != 0
            || scissor.Size.X < (int)context.FramebufferWidth || scissor.Size.Y < (int)context.FramebufferHeight;
        if (uniforms.BaseClipping.Enabled)
        {
            cache.Scissor(uniforms.BaseClipping.X, uniforms.BaseClipping.Y, uniforms.BaseClipping.Width, uniforms.BaseClipping.Height);
            cache.Enable(PvrGl.ScissorTest);
        }

        try
        {
            PvrGl4.DrawStrips(outputFbo, renderingWidth, renderingHeight);
            if (isRtt)
                PvrGles.ReadRttBuffer();
            else if (PvrConfiguration.EmulateFramebuffer)
                PvrGles.WriteFramebufferToVram();
            else
            {
                gl.Ofbo.AspectRatio = PvrTransformMatrix.GetOutputFramebufferAspectRatio();
                gl.Ofbo2.Ready = false;
                RenderLastFrameCore();
            }
        }
        finally
        {
            PvrGlVertexArray.Unbind();
        }

        return !isRtt;
    }
}
