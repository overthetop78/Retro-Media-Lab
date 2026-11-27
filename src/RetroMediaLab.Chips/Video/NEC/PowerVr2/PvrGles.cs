using System.Numerics;
using System.Runtime.InteropServices;
using static RetroMediaLab.Chips.Video.NEC.PowerVr2.PvrRegisters;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2;

internal static unsafe partial class PvrGles
{
    internal const string ShaderCompatSource = """

#define GLES2 0 							
#define GLES3 1 							
#define GL2 2								
#define GL3 3								
											
#if TARGET_GL == GL2 						
#define highp								
#define lowp								
#define mediump								
#endif										
#if TARGET_GL == GLES3						
out highp vec4 FragColor;					
#define gl_FragColor FragColor				
#define FOG_CHANNEL r						
#elif TARGET_GL == GL3						
out highp vec4 FragColor;					
#define gl_FragColor FragColor				
#define FOG_CHANNEL r						
#else										
#define texture texture2D					
#define FOG_CHANNEL a						
#endif										

""";

    internal const string VertexCompatShader = """

#if TARGET_GL == GLES2 || TARGET_GL == GL2
#define in attribute
#define out varying
#endif

""";

    internal const string PixelCompatShader = """

#if TARGET_GL == GLES2 || TARGET_GL == GL2
#define in varying
#endif

""";

    internal const string GouraudSource = """

#if (TARGET_GL == GL3 || TARGET_GL == GLES3) && pp_Gouraud == 0
	#define INTERPOLATION flat
#else
	#define INTERPOLATION
#endif

""";

    private const string VertexShaderSource = """

/* Vertex constants*/ 
uniform highp vec4 depth_scale;
uniform highp mat4 ndcMat;
uniform highp float sp_FOG_DENSITY;

/* Vertex input */
in highp vec4 in_pos;
in lowp vec4 in_base;
in lowp vec4 in_offs;
in highp vec2 in_uv;
/* output */
INTERPOLATION out highp vec4 vtx_base;
INTERPOLATION out highp vec4 vtx_offs;
out highp vec3 vtx_uv;

void main()
{
	highp vec4 vpos = ndcMat * in_pos;
	vtx_base = in_base;
	vtx_offs = in_offs;
#if TARGET_GL == GLES2
	vtx_uv = vec3(in_uv, vpos.z * sp_FOG_DENSITY);
	vpos.w = 1.0 / vpos.z;
	vpos.z = depth_scale.x + depth_scale.y * vpos.w;
	vpos.xy *= vpos.w;
#else
	#if DIV_POS_Z == 1
		vpos /= vpos.z;
		vpos.z = vpos.w;
	#endif
	#if pp_Gouraud == 1 && DIV_POS_Z != 1
		vtx_base *= vpos.z;
		vtx_offs *= vpos.z;
	#endif
	vtx_uv = vec3(in_uv, vpos.z);
	#if DIV_POS_Z != 1
		vtx_uv.xy *= vpos.z;
		vpos.w = 1.0;
		vpos.z = 0.0;
	#endif
#endif
	gl_Position = vpos;
}

""";

    internal const string PixelPipelineShader = """

#define PI 3.1415926

/* Shader program params*/
/* gles has no alpha test stage, so its emulated on the shader */
uniform highp float cp_AlphaTestValue;
uniform lowp vec4 pp_ClipTest;
uniform lowp vec3 sp_FOG_COL_RAM,sp_FOG_COL_VERT;
uniform highp float sp_FOG_DENSITY;
uniform sampler2D tex,fog_table;
uniform lowp float trilinear_alpha;
uniform lowp vec4 fog_clamp_min;
uniform lowp vec4 fog_clamp_max;
#if pp_Palette != 0
uniform sampler2D palette;
uniform mediump int palette_index;
#if TARGET_GL == GLES2 || TARGET_GL == GL2
uniform lowp vec2 texSize;
#endif
#endif
#if DITHERING == 1
uniform lowp vec4 ditherDivisor;
#endif

/* Vertex input*/
INTERPOLATION in highp vec4 vtx_base;
INTERPOLATION in highp vec4 vtx_offs;
in highp vec3 vtx_uv;

lowp float fog_mode2(highp float w)
{
	highp float z = clamp(
#if TARGET_GL == GLES2
						  vtx_uv.z
#elif DIV_POS_Z == 1
						  sp_FOG_DENSITY / w
#else
						  sp_FOG_DENSITY * w
#endif
											, 1.0, 255.9999);
	mediump float exp = floor(log2(z));
	highp float m = z * 16.0 / pow(2.0, exp) - 16.0;
	mediump float idx = floor(m) + exp * 16.0 + 0.5;
	highp vec4 fog_coef = texture(fog_table, vec2(idx / 128.0, 0.75 - (m - floor(m)) / 2.0));
	return fog_coef.FOG_CHANNEL;
}

highp vec4 fog_clamp(lowp vec4 col)
{
#if FogClamping == 1
	return clamp(col, fog_clamp_min, fog_clamp_max);
#else
	return col;
#endif
}

#if pp_Palette != 0

lowp vec4 getPaletteEntry(highp float colorIndex)
{
	highp int color_idx = int(floor(colorIndex * 255.0 + 0.5)) + palette_index;
#if TARGET_GL == GLES2 || TARGET_GL == GL2
    highp vec2 c = vec2((mod(float(color_idx), 32.0) * 2.0 + 1.0) / 64.0, (float(color_idx / 32) * 2.0 + 1.0) / 64.0);
	return texture(palette, c);
#else
    highp ivec2 c = ivec2(color_idx % 32, color_idx / 32);
	return texelFetch(palette, c, 0);
#endif
}

#endif

#if pp_Palette == 1		// Nearest filtering

lowp vec4 palettePixel(highp vec3 coords)
{
#if TARGET_GL == GLES2 || TARGET_GL == GL2 || DIV_POS_Z == 1
	return getPaletteEntry(texture(tex, coords.xy).FOG_CHANNEL);
#else
	return getPaletteEntry(textureProj(tex, coords).FOG_CHANNEL);
#endif

}

#elif pp_Palette == 2		// Bi-linear filtering

lowp vec4 palettePixelBilinear(highp vec3 coords)
{
#if TARGET_GL != GLES2 && TARGET_GL != GL2 && DIV_POS_Z != 1
	coords.xy /= coords.z;
#endif
#if TARGET_GL != GLES2 && TARGET_GL != GL2
	lowp vec2 texSize = vec2(textureSize(tex, 0));
#endif
	highp vec2 pixCoord = coords.xy * texSize - 0.5;		// coordinates of top left pixel
	highp vec2 originPixCoord = floor(pixCoord);

	highp vec2 sampleUV = (originPixCoord + 0.5) / texSize;	// UV coordinates of center of top left pixel

    // Sample from all surrounding texels
    lowp vec4 c00 = getPaletteEntry(texture(tex, sampleUV).FOG_CHANNEL);
#if TARGET_GL != GLES2 && TARGET_GL != GL2
    lowp vec4 c01 = getPaletteEntry(textureOffset(tex, sampleUV, ivec2(0, 1)).FOG_CHANNEL);
    lowp vec4 c11 = getPaletteEntry(textureOffset(tex, sampleUV, ivec2(1, 1)).FOG_CHANNEL);
    lowp vec4 c10 = getPaletteEntry(textureOffset(tex, sampleUV, ivec2(1, 0)).FOG_CHANNEL);
#else
	sampleUV = (originPixCoord + vec2(0.5, 1.5)) / texSize;
    lowp vec4 c01 = getPaletteEntry(texture(tex, sampleUV).FOG_CHANNEL);
	sampleUV = (originPixCoord + vec2(1.5, 1.5)) / texSize;
    lowp vec4 c11 = getPaletteEntry(texture(tex, sampleUV).FOG_CHANNEL);
	sampleUV = (originPixCoord + vec2(1.5, 0.5)) / texSize;
    lowp vec4 c10 = getPaletteEntry(texture(tex, sampleUV).FOG_CHANNEL);
#endif

	highp vec2 weight = pixCoord - originPixCoord;

    // Bi-linear mixing
    lowp vec4 temp0 = mix(c00, c10, weight.x);
    lowp vec4 temp1 = mix(c01, c11, weight.x);
    return mix(temp0, temp1, weight.y);
}

#endif

#if TARGET_GL == GLES2
#define depth gl_FragCoord.w
#else
#define depth vtx_uv.z
#endif

void main()
{
	// Clip inside the box
	#if pp_ClipInside == 1
		if (gl_FragCoord.x >= pp_ClipTest.x && gl_FragCoord.x <= pp_ClipTest.z
				&& gl_FragCoord.y >= pp_ClipTest.y && gl_FragCoord.y <= pp_ClipTest.w)
			discard;
	#endif
	
	highp vec4 color = vtx_base;
	highp vec4 offset = vtx_offs;
	#if pp_Gouraud == 1 && TARGET_GL != GLES2 && DIV_POS_Z != 1
		color /= vtx_uv.z;
		offset /= vtx_uv.z;
	#endif
	#if pp_UseAlpha==0
		color.a=1.0;
	#endif
	#if pp_FogCtrl==3
		color = vec4(sp_FOG_COL_RAM.rgb, fog_mode2(depth));
	#endif
	#if pp_Texture==1
	{
		#if pp_Palette == 0
		  #if TARGET_GL == GLES2 || TARGET_GL == GL2 || DIV_POS_Z == 1
			lowp vec4 texcol = texture(tex, vtx_uv.xy);
		  #else
			lowp vec4 texcol = textureProj(tex, vtx_uv);
		  #endif
		#elif pp_Palette == 1
			lowp vec4 texcol = palettePixel(vtx_uv);
		#else
			lowp vec4 texcol = palettePixelBilinear(vtx_uv);
		#endif
		
		#if pp_BumpMap == 1
			highp float s = PI / 2.0 * (texcol.a * 15.0 * 16.0 + texcol.r * 15.0) / 255.0;
			highp float r = 2.0 * PI * (texcol.g * 15.0 * 16.0 + texcol.b * 15.0) / 255.0;
			texcol.a = clamp(offset.a + offset.r * sin(s) + offset.g * cos(s) * cos(r - 2.0 * PI * offset.b), 0.0, 1.0);
			texcol.rgb = vec3(1.0, 1.0, 1.0);	
		#else
			#if pp_IgnoreTexA==1
				texcol.a=1.0;	
			#endif
		#endif
		#if pp_ShadInstr==0
		{
			color=texcol;
		}
		#endif
		#if pp_ShadInstr==1
		{
			color.rgb*=texcol.rgb;
			color.a=texcol.a;
		}
		#endif
		#if pp_ShadInstr==2
		{
			color.rgb=mix(color.rgb,texcol.rgb,texcol.a);
		}
		#endif
		#if  pp_ShadInstr==3
		{
			color*=texcol;
		}
		#endif
		
		#if pp_Offset==1 && pp_BumpMap == 0
			color.rgb += offset.rgb;
		#endif
	}
	#endif
	
	color = fog_clamp(color);
	
	#if pp_FogCtrl == 0
		color.rgb = mix(color.rgb, sp_FOG_COL_RAM.rgb, fog_mode2(depth)); 
	#endif
	#if pp_FogCtrl == 1 && pp_Offset==1 && pp_BumpMap == 0
		color.rgb = mix(color.rgb, sp_FOG_COL_VERT.rgb, offset.a);
	#endif
	
	#if pp_TriLinear == 1
	color *= trilinear_alpha;
	#endif
	
	#if cp_AlphaTest == 1
		color.a = floor(color.a * 255.0 + 0.5) / 255.0;
		if (cp_AlphaTestValue > color.a)
			discard;
		color.a = 1.0;
	#endif 

	//color.rgb = vec3(vtx_uv.z * sp_FOG_DENSITY / 128.0);
#if TARGET_GL != GLES2
#if DIV_POS_Z == 1
	highp float w = 100000.0 / vtx_uv.z;
#else
	highp float w = 100000.0 * vtx_uv.z;
#endif
	gl_FragDepth = log2(1.0 + max(w, -0.999999)) / 34.0;

#if DITHERING == 1
	mediump float ditherTable[16] = float[](
		5., 13.,  7., 15.,
		9.,  1., 11.,  3.,
		6., 14.,  4., 12.,
		10., 2.,  8.,  0.
	);
	mediump float r = ditherTable[int(mod(gl_FragCoord.y, 4.)) * 4 + int(mod(gl_FragCoord.x, 4.))];
	mediump vec4 dv = vec4(r, r, r, 1.) / ditherDivisor;
	color = clamp(floor(color * 255. + dv) / 255., 0., 1.);
#endif
#endif
	gl_FragColor = color;
}

""";

    private const string ModifierVolumeShader = """

uniform lowp float sp_ShaderColor;

/* Vertex input*/
in highp vec3 vtx_uv;

void main()
{
#if TARGET_GL != GLES2
#if DIV_POS_Z == 1
	highp float w = 100000.0 / vtx_uv.z;
#else
	highp float w = 100000.0 * vtx_uv.z;
#endif
	gl_FragDepth = log2(1.0 + max(w, -0.999999)) / 34.0;
#endif
	gl_FragColor=vec4(0.0, 0.0, 0.0, sp_ShaderColor);
}

""";

    private static uint _fogTextureId;
    private static uint _paletteTextureId;
    private static readonly List<ushort> ShortIndices = [];

    private static void DeleteShaders()
    {
        PvrGlCache cache = PvrGlCache.Instance;
        foreach (PvrGlPipelineShader shader in Gl.Shaders.Values)
            if (shader.Program != 0)
                cache.DeleteProgram(shader.Program);
        Gl.Shaders.Clear();
        cache.DeleteProgram(Gl.ModvolShader.Program);
        Gl.ModvolShader.Program = 0;
        cache.DeleteProgram(Gl.N2ModvolShader.Program);
        Gl.N2ModvolShader.Program = 0;
    }

    internal static void TermGlCommon()
    {
        try
        {
            PvrVideoRouting.TerminateGl();
            Gl.QuadDrawer?.Dispose();
            Gl.QuadDrawer = null;
            fixed (uint* fog = &_fogTextureId, palette = &_paletteTextureId, dc = &Gl.Dcfb.Texture)
            {
                PvrGlCache.Instance.DeleteTextures(1, fog);
                _fogTextureId = 0;
                PvrGlCache.Instance.DeleteTextures(1, palette);
                _paletteTextureId = 0;
                Gl.Rtt.Framebuffer?.Dispose();
                Gl.Rtt.Framebuffer = null;
                Gl.Ofbo.Framebuffer?.Dispose();
                Gl.Ofbo.Framebuffer = null;
                PvrGlCache.Instance.DeleteTextures(1, dc);
                Gl.Dcfb.Texture = 0;
            }
            Gl.Ofbo2.Framebuffer?.Dispose();
            Gl.Ofbo2.Framebuffer = null;
            Gl.FbScaling.Framebuffer?.Dispose();
            Gl.FbScaling.Framebuffer = null;
            Gl.Routing.Framebuffer?.Dispose();
            Gl.Routing.Framebuffer = null;
        }
        finally
        {
            TermVmuLightgun();
        }
    }

    internal static void TermGles()
    {
        try
        {
            Gl.Vbo.MainVao.Term();
            Gl.Vbo.ModvolVao.Term();
            Gl.Vbo.Geometry?.Dispose();
            Gl.Vbo.Geometry = null;
            Gl.Vbo.Modvols?.Dispose();
            Gl.Vbo.Modvols = null;
            Gl.Vbo.Indices?.Dispose();
            Gl.Vbo.Indices = null;
            TermGlCommon();
            DeleteShaders();
        }
        finally
        {
            if (Gl.Ofbo.OriginalFbo != 0)
                PvrGl.BindFramebuffer(PvrGl.Framebuffer, Gl.Ofbo.OriginalFbo);
            PvrGl.ColorMask(1, 1, 1, 1);
            PvrGl.ActiveTexture(PvrGl.Texture0);
        }
    }

    internal static void FindGlVersion()
    {
        Gl.IndexType = PvrGl.UnsignedInt;
        PvrGlGraphicsContext context = PvrGlGraphicsContext.Instance;
        Gl.Major = context.GetMajorVersion();
        Gl.Minor = context.GetMinorVersion();
        Gl.IsGles = context.IsGles();
        if (Gl.IsGles)
        {
            Gl.BorderClampSupported = false;
            if (Gl.Major >= 3)
            {
                Gl.Version = "GLES3";
                Gl.GlslVersionHeader = "#version 300 es";
                if (Gl.Major > 3 || Gl.Minor >= 2)
                    Gl.BorderClampSupported = true;
                Gl.PrimRestartSupported = false;
                Gl.PrimRestartFixedSupported = true;
                Gl.SingleChannelFormat = PvrGl.Red;
            }
            else
            {
                Gl.Version = "GLES2";
                Gl.GlslVersionHeader = "";
                Gl.IndexType = PvrGl.UnsignedShort;
                Gl.PrimRestartSupported = false;
                Gl.PrimRestartFixedSupported = false;
                Gl.SingleChannelFormat = PvrGl.Alpha;
            }

            string extensions = PvrGl.GetString(PvrGl.Extensions)!;
            if (extensions.Contains("GL_OES_packed_depth_stencil", StringComparison.Ordinal))
                Gl.PackedDepthStencilSupported = true;
            if (extensions.Contains("GL_OES_depth24", StringComparison.Ordinal))
                Gl.Depth24Supported = true;
            if (!Gl.PackedDepthStencilSupported && Gl.Major < 3)
                PvrRuntime.Log.Info("Packed depth/stencil not supported: no modifier volumes when rendering to a texture");
            int* ranges = stackalloc int[2];
            int precision;
            PvrGl.GetShaderPrecisionFormat(PvrGl.FragmentShader, PvrGl.HighFloat, ranges, &precision);
            Gl.HighpFloatSupported = (ranges[0] != 0 || ranges[1] != 0) && precision != 0;
            if (!Gl.BorderClampSupported)
                Gl.BorderClampSupported = extensions.Contains("GL_EXT_texture_border_clamp", StringComparison.Ordinal);
        }
        else
        {
            if (Gl.Major >= 3)
            {
                Gl.Version = "GL3";
                Gl.GlslVersionHeader = "#version 130";
                Gl.SingleChannelFormat = PvrGl.Red;
                Gl.PrimRestartSupported = Gl.Major > 3 || Gl.Minor >= 1;
                Gl.PrimRestartFixedSupported = Gl.Major > 4 || (Gl.Major == 4 && Gl.Minor >= 3);
            }
            else
            {
                Gl.Version = "GL2";
                Gl.GlslVersionHeader = "#version 120";
                Gl.SingleChannelFormat = PvrGl.Alpha;
                Gl.PrimRestartSupported = false;
                Gl.PrimRestartFixedSupported = false;
            }
            Gl.HighpFloatSupported = true;
            Gl.BorderClampSupported = true;
        }
        Gl.MaxAnisotropy = 1f;
        if (Gl.Major >= 3)
        {
            bool anisotropicExtension = false;
            string? extensions = PvrGl.GetString(PvrGl.Extensions);
            if (extensions == null)
            {
                int count = 0;
                PvrGl.GetIntegerv(PvrGl.NumExtensions, &count);
                for (int i = 0; i < count; i++)
                {
                    string? extension = PvrGl.GetStringi(PvrGl.Extensions, (uint)i);
                    if (extension == "GL_EXT_texture_filter_anisotropic")
                    {
                        anisotropicExtension = true;
                        break;
                    }
                }
            }
            else if (extensions.Contains("GL_EXT_texture_filter_anisotropic", StringComparison.Ordinal))
                anisotropicExtension = true;
            if (anisotropicExtension)
                fixed (float* value = &Gl.MaxAnisotropy)
                    PvrGl.GetFloatv(MaxTextureMaxAnisotropy, value);
        }

        string? vendor = PvrGl.GetString(PvrGl.Vendor);
        string? renderer = PvrGl.GetString(PvrGl.Renderer);
        Gl.MesaNouveau = string.Equals(vendor, "nouveau", StringComparison.OrdinalIgnoreCase)
            || (string.Equals(vendor, "Mesa", StringComparison.OrdinalIgnoreCase) && renderer!.StartsWith("NV", StringComparison.Ordinal));
        Gl.Mali = string.Equals(vendor, "arm", StringComparison.OrdinalIgnoreCase);
        PvrRuntime.Log.Notice("OpenGL{0} version {1}.{2}", Gl.IsGles ? " ES" : "", Gl.Major, Gl.Minor);
        PvrRuntime.Log.Notice("Vendor '{0}' Renderer '{1}' Version '{2}'", vendor, renderer, PvrGl.GetString(PvrGl.Version));
        while (PvrGl.GetError() != PvrGl.NoError)
        {
        }
        Gl.BogusBlitFramebuffer = true;
        if (Gl.Major >= 3)
        {
            Gl.BogusBlitFramebuffer = !TestBlitFramebuffer();
            if (Gl.BogusBlitFramebuffer)
                PvrRuntime.Log.Warn("glBlitFramebuffer is bogus. Using quad drawer instead");
            else
                PvrRuntime.Log.Notice("glBlitFramebuffer test successful");
        }
    }

    internal static uint CompileShader(string shader, uint type)
    {
        uint id = PvrGl.CreateShader(type);
        PvrGl.ShaderSource(id, shader);
        PvrGl.CompileShader(id);
        int result;
        int length;
        PvrGl.GetShaderiv(id, PvrGl.CompileStatus, &result);
        PvrGl.GetShaderiv(id, PvrGl.InfoLogLength, &length);
        if (result == 0 && length > 0)
        {
            byte* log = (byte*)NativeMemory.Alloc((nuint)length);
            try
            {
                *log = 0;
                PvrGl.GetShaderInfoLog(id, length, &length, log);
                PvrRuntime.Log.Warn("Shader: {0} \n{1}", result != 0 ? "compiled!" : "failed to compile", Marshal.PtrToStringAnsi((nint)log));
            }
            finally
            {
                NativeMemory.Free(log);
            }
        }
        return id;
    }

    internal static uint CompileAndLink(string vertexShader, string fragmentShader)
    {
        uint vertex = 0;
        uint fragment = 0;
        uint program = 0;
        bool linked = false;
        try
        {
            vertex = CompileShader(vertexShader, PvrGl.VertexShader);
            fragment = CompileShader(fragmentShader, PvrGl.FragmentShader);
            program = PvrGl.CreateProgram();
            PvrGl.AttachShader(program, vertex);
            PvrGl.AttachShader(program, fragment);

            PvrGl.BindAttribLocation(program, VertexPosArray, "in_pos");
            PvrGl.BindAttribLocation(program, VertexColBaseArray, "in_base");
            PvrGl.BindAttribLocation(program, VertexColOffsArray, "in_offs");
            PvrGl.BindAttribLocation(program, VertexUvArray, "in_uv");
            PvrGl.BindAttribLocation(program, VertexColBase1Array, "in_base1");
            PvrGl.BindAttribLocation(program, VertexColOffs1Array, "in_offs1");
            PvrGl.BindAttribLocation(program, VertexUv1Array, "in_uv1");
            PvrGl.BindAttribLocation(program, VertexNormArray, "in_normal");
            if (!Gl.IsGles && Gl.Major >= 3)
                PvrGl.BindFragDataLocation(program, 0, "FragColor");

            PvrGl.LinkProgram(program);
            int result;
            int length;
            PvrGl.GetProgramiv(program, PvrGl.LinkStatus, &result);
            PvrGl.GetProgramiv(program, PvrGl.InfoLogLength, &length);
            if (result == 0 && length > 0)
            {
                length += 1024;
                byte* log = (byte*)NativeMemory.Alloc((nuint)length);
                try
                {
                    *log = 0;
                    PvrGl.GetProgramInfoLog(program, length, &length, log);
                    PvrRuntime.Log.Warn("Shader linking: {0} \n ({1} bytes), - {2} -",
                        result != 0 ? "linked" : "failed to link", length, Marshal.PtrToStringAnsi((nint)log));
                }
                finally
                {
                    NativeMemory.Free(log);
                }
                PvrRuntime.Log.Info("// VERTEX SHADER\n{0}\n// END", vertexShader);
                PvrRuntime.Log.Info("// FRAGMENT SHADER\n{0}\n// END", fragmentShader);
                throw new PvrRendererException(PvrRuntime.Translate("OpenGL shader compilation failed"));
            }
            PvrGl.DetachShader(program, vertex);
            PvrGl.DetachShader(program, fragment);
            linked = true;
        }
        finally
        {
            if (vertex != 0)
                PvrGl.DeleteShader(vertex);
            if (fragment != 0)
                PvrGl.DeleteShader(fragment);
            if (!linked && program != 0)
                PvrGlCache.Instance.DeleteProgram(program);
        }
        PvrGlCache.Instance.UseProgram(program);

        return program;
    }

    internal static PvrGlPipelineShader GetProgram(bool alphaTest, bool insideClipping, bool texture,
        bool useAlpha, bool ignoreTexA, uint shadInstr, bool offset, uint fogCtrl, bool gouraud,
        bool bumpMap, bool fogClamping, bool trilinear, int palette, bool naomi2, bool dithering)
    {
        uint key = 0;
        key |= insideClipping ? 1u : 0u;
        key = (key << 1) | (alphaTest ? 1u : 0u);
        key = (key << 1) | (texture ? 1u : 0u);
        key = (key << 1) | (useAlpha ? 1u : 0u);
        key = (key << 1) | (ignoreTexA ? 1u : 0u);
        key = (key << 2) | shadInstr;
        key = (key << 1) | (offset ? 1u : 0u);
        key = (key << 2) | fogCtrl;
        key = (key << 1) | (gouraud ? 1u : 0u);
        key = (key << 1) | (bumpMap ? 1u : 0u);
        key = (key << 1) | (fogClamping ? 1u : 0u);
        key = (key << 1) | (trilinear ? 1u : 0u);
        key = (key << 2) | (uint)palette;
        key = (key << 1) | (naomi2 ? 1u : 0u);
        bool divPosZ = !PvrConfiguration.IsNaomi2 && PvrConfiguration.NativeDepthInterpolation;
        key = (key << 1) | (divPosZ ? 1u : 0u);
        key = (key << 1) | (dithering ? 1u : 0u);

        if (!Gl.Shaders.TryGetValue(key, out PvrGlPipelineShader? shader))
            Gl.Shaders.Add(key, shader = new PvrGlPipelineShader());
        if (shader.Program == 0)
        {
            shader.AlphaTest = alphaTest;
            shader.InsideClipping = insideClipping;
            shader.Texture = texture;
            shader.UseAlpha = useAlpha;
            shader.IgnoreTexA = ignoreTexA;
            shader.ShadInstr = shadInstr;
            shader.Offset = offset;
            shader.FogCtrl = fogCtrl;
            shader.Gouraud = gouraud;
            shader.BumpMap = bumpMap;
            shader.FogClamping = fogClamping;
            shader.Trilinear = trilinear;
            shader.Palette = palette;
            shader.Naomi2 = naomi2;
            shader.DivPosZ = divPosZ;
            shader.Dithering = dithering;
            CompilePipelineShader(shader);
        }
        return shader;
    }

    private sealed class VertexSource : PvrOpenGlSource
    {
        internal VertexSource(bool gouraud, bool divPosZ)
        {
            AddConstant("pp_Gouraud", gouraud ? 1 : 0);
            AddConstant("DIV_POS_Z", divPosZ ? 1 : 0);
            AddSource(VertexCompatShader);
            AddSource(GouraudSource);
            AddSource(VertexShaderSource);
        }
    }

    private sealed class FragmentShaderSource : PvrOpenGlSource
    {
        internal FragmentShaderSource(PvrGlPipelineShader shader)
        {
            AddConstant("cp_AlphaTest", shader.AlphaTest ? 1 : 0);
            AddConstant("pp_ClipInside", shader.InsideClipping ? 1 : 0);
            AddConstant("pp_UseAlpha", shader.UseAlpha ? 1 : 0);
            AddConstant("pp_Texture", shader.Texture ? 1 : 0);
            AddConstant("pp_IgnoreTexA", shader.IgnoreTexA ? 1 : 0);
            AddConstant("pp_ShadInstr", (int)shader.ShadInstr);
            AddConstant("pp_Offset", shader.Offset ? 1 : 0);
            AddConstant("pp_FogCtrl", (int)shader.FogCtrl);
            AddConstant("pp_Gouraud", shader.Gouraud ? 1 : 0);
            AddConstant("pp_BumpMap", shader.BumpMap ? 1 : 0);
            AddConstant("FogClamping", shader.FogClamping ? 1 : 0);
            AddConstant("pp_TriLinear", shader.Trilinear ? 1 : 0);
            AddConstant("pp_Palette", shader.Palette);
            AddConstant("DIV_POS_Z", shader.DivPosZ ? 1 : 0);
            AddConstant("DITHERING", shader.Dithering ? 1 : 0);

            AddSource(PixelCompatShader);
            AddSource(GouraudSource);
            AddSource(PixelPipelineShader);
        }
    }

    internal static bool CompilePipelineShader(PvrGlPipelineShader shader)
    {
        string vertex = shader.Naomi2
            ? new PvrN2VertexSource(shader.Gouraud, false, shader.Texture).Generate()
            : new VertexSource(shader.Gouraud, shader.DivPosZ).Generate();
        var fragment = new FragmentShaderSource(shader);
        shader.Program = CompileAndLink(vertex, fragment.Generate());
        uint program = shader.Program;

        int uniform = PvrGl.GetUniformLocation(program, "tex");
        if (shader.Texture)
            PvrGl.Uniform1i(uniform, 0);

        shader.DepthScale = PvrGl.GetUniformLocation(program, "depth_scale");
        shader.ClipTest = PvrGl.GetUniformLocation(program, "pp_ClipTest");
        shader.FogDensity = PvrGl.GetUniformLocation(program, "sp_FOG_DENSITY");
        shader.AlphaTestValue = PvrGl.GetUniformLocation(program, "cp_AlphaTestValue");
        shader.FogColorVertex = shader.FogCtrl == 1 && shader.Texture ? PvrGl.GetUniformLocation(program, "sp_FOG_COL_VERT") : -1;
        shader.FogColorRam = shader.FogCtrl is 0 or 3 ? PvrGl.GetUniformLocation(program, "sp_FOG_COL_RAM") : -1;

        uniform = PvrGl.GetUniformLocation(program, "fog_table");
        if (uniform != -1)
            PvrGl.Uniform1i(uniform, 1);
        uniform = PvrGl.GetUniformLocation(program, "palette");
        if (uniform != -1)
            PvrGl.Uniform1i(uniform, 2);
        shader.PaletteIndex = PvrGl.GetUniformLocation(program, "palette_index");
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
        shader.DitherDivisor = PvrGl.GetUniformLocation(program, "ditherDivisor");
        shader.TexSize = PvrGl.GetUniformLocation(program, "texSize");

        if (shader.Naomi2)
            PvrGlNaomi2.InitN2Uniforms(shader);
        ShaderUniforms.Set(shader);
        return true;
    }

    internal static void CreateModvolShader()
    {
        if (Gl.ModvolShader.Program != 0)
            return;
        bool nativeDepth = PvrConfiguration.NativeDepthInterpolation;
        var vertex = new VertexSource(false, nativeDepth);
        var fragment = new PvrOpenGlSource();
        fragment.AddConstant("pp_Gouraud", 0)
            .AddConstant("DIV_POS_Z", nativeDepth ? 1 : 0)
            .AddSource(PixelCompatShader)
            .AddSource(GouraudSource)
            .AddSource(ModifierVolumeShader);

        Gl.ModvolShader.Program = CompileAndLink(vertex.Generate(), fragment.Generate());
        Gl.ModvolShader.NdcMat = PvrGl.GetUniformLocation(Gl.ModvolShader.Program, "ndcMat");
        Gl.ModvolShader.ShaderColor = PvrGl.GetUniformLocation(Gl.ModvolShader.Program, "sp_ShaderColor");
        Gl.ModvolShader.DepthScale = PvrGl.GetUniformLocation(Gl.ModvolShader.Program, "depth_scale");

        if (Gl.Major >= 3)
        {
            var n2Vertex = new PvrN2VertexSource(false, true, false);
            fragment.SetConstant("DIV_POS_Z", 0);
            Gl.N2ModvolShader.Program = CompileAndLink(n2Vertex.Generate(), fragment.Generate());
            Gl.N2ModvolShader.NdcMat = PvrGl.GetUniformLocation(Gl.N2ModvolShader.Program, "ndcMat");
            Gl.N2ModvolShader.ShaderColor = PvrGl.GetUniformLocation(Gl.N2ModvolShader.Program, "sp_ShaderColor");
            Gl.N2ModvolShader.DepthScale = PvrGl.GetUniformLocation(Gl.N2ModvolShader.Program, "depth_scale");
            Gl.N2ModvolShader.MvMat = PvrGl.GetUniformLocation(Gl.N2ModvolShader.Program, "mvMat");
            Gl.N2ModvolShader.ProjMat = PvrGl.GetUniformLocation(Gl.N2ModvolShader.Program, "projMat");
        }
    }

    internal static void CreateResources()
    {
        if (Gl.Vbo.Geometry != null)
            return;
        FindGlVersion();

        if (Gl.Major >= 3 && !PvrGl.HasGenVertexArrays)
        {
            PvrRuntime.Log.Error("glGenVertexArrays function is null");
            throw new PvrRendererException("OpenGL initialization failed");
        }

        Gl.Vbo.Geometry = new PvrGlBuffer(PvrGl.ArrayBuffer);
        Gl.Vbo.Modvols = new PvrGlBuffer(PvrGl.ArrayBuffer);
        Gl.Vbo.Indices = new PvrGlBuffer(PvrGl.ElementArrayBuffer);
        Gl.QuadDrawer = new PvrGlQuadDrawer();
    }

    internal static void DebugOutput(uint source, uint type, uint id, uint severity, int length, byte* message, void* userParam)
    {
        if (id == 131185)
            return;
        string? text = Marshal.PtrToStringAnsi((nint)message);
        switch (severity)
        {
            default:
            case PvrGl.DebugSeverityNotification:
            case PvrGl.DebugSeverityLow:
                PvrRuntime.Log.Debug("opengl:[{0}] {1}", id, text);
                break;
            case PvrGl.DebugSeverityMedium:
                PvrRuntime.Log.Info("opengl:[{0}] {1}", id, text);
                break;
            case PvrGl.DebugSeverityHigh:
                PvrRuntime.Log.Warn("opengl:[{0}] {1}", id, text);
                break;
        }
    }

    internal static void UpdateFogTexture(uint textureSlot, uint imageFormat)
    {
        PvrGlCache cache = PvrGlCache.Instance;
        PvrGl.ActiveTexture(textureSlot);
        if (_fogTextureId == 0)
        {
            _fogTextureId = cache.GenTexture();
            cache.BindTexture(PvrGl.Texture2D, _fogTextureId);
            cache.TexParameteri(PvrGl.Texture2D, PvrGl.TextureMinFilter, PvrGl.Linear);
            cache.TexParameteri(PvrGl.Texture2D, PvrGl.TextureMagFilter, PvrGl.Linear);
            cache.TexParameteri(PvrGl.Texture2D, PvrGl.TextureWrapS, PvrGl.ClampToEdge);
            cache.TexParameteri(PvrGl.Texture2D, PvrGl.TextureWrapT, PvrGl.ClampToEdge);
        }
        else
        {
            cache.BindTexture(PvrGl.Texture2D, _fogTextureId);
        }

        byte* pixels = stackalloc byte[256];
        PvrTextureCache.MakeFogTexture(pixels);
        PvrGl.PixelStorei(PvrGl.UnpackAlignment, 1);
        uint internalFormat = Gl.IsGles && imageFormat == PvrGl.Red ? R8 : imageFormat;
        PvrGl.TexImage2D(PvrGl.Texture2D, 0, internalFormat, 128, 2, 0, imageFormat, PvrGl.UnsignedByte, pixels);
        Check();
        PvrGl.ActiveTexture(PvrGl.Texture0);
    }

    internal static void UpdatePaletteTexture(uint textureSlot)
    {
        PvrGlCache cache = PvrGlCache.Instance;
        PvrGl.ActiveTexture(textureSlot);
        if (_paletteTextureId == 0)
        {
            _paletteTextureId = cache.GenTexture();
            cache.BindTexture(PvrGl.Texture2D, _paletteTextureId);
            cache.TexParameteri(PvrGl.Texture2D, PvrGl.TextureMinFilter, PvrGl.Nearest);
            cache.TexParameteri(PvrGl.Texture2D, PvrGl.TextureMagFilter, PvrGl.Nearest);
            cache.TexParameteri(PvrGl.Texture2D, PvrGl.TextureWrapS, PvrGl.ClampToEdge);
            cache.TexParameteri(PvrGl.Texture2D, PvrGl.TextureWrapT, PvrGl.ClampToEdge);
        }
        else
        {
            cache.BindTexture(PvrGl.Texture2D, _paletteTextureId);
        }
        PvrGl.PixelStorei(PvrGl.UnpackAlignment, 1);
        fixed (uint* pixels = PvrTextureConversion.Palette32Ram)
            PvrGl.TexImage2D(PvrGl.Texture2D, 0, PvrGl.Rgba, 32, 32, 0, PvrGl.Rgba, PvrGl.UnsignedByte, pixels);
        Check();
        PvrGl.ActiveTexture(PvrGl.Texture0);
    }

    internal static void UploadVertexIndices()
    {
        RenderContext context = Gl.RendContext!;
        if (Gl.IndexType == PvrGl.UnsignedShort)
        {
            ShortIndices.Clear();
            ShortIndices.EnsureCapacity(context.Idx.Count);
            foreach (uint index in context.Idx)
                ShortIndices.Add(unchecked((ushort)index));
            fixed (ushort* indices = CollectionsMarshal.AsSpan(ShortIndices))
                Gl.Vbo.Indices!.Update(indices, ShortIndices.Count * sizeof(ushort));
        }
        else
        {
            fixed (uint* indices = CollectionsMarshal.AsSpan(context.Idx))
                Gl.Vbo.Indices!.Update(indices, context.Idx.Count * sizeof(uint));
        }
        Check();
    }

    internal static PvrRenderBackend CreateRenderer() => new PvrOpenGlRenderer();
}

internal unsafe partial class PvrOpenGlRenderer
{
    internal override bool Init()
    {
        PvrGlCache.Instance.EnableCache();
        PvrGles.CreateResources();

        if (PvrGles.Gl.IsGles)
            PvrGl.Hint(PvrGl.GenerateMipmapHint, PvrGl.Fastest);
        PvrGles.Check();

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

    protected void DrawOsd()
    {
        PvrGles.DrawVmusAndCrosshairs(Width, Height);
        PvrRuntime.Gui.DisplayOsd();
    }

    internal override void Process(TaContext context)
    {
        PvrGlContext gl = PvrGles.Gl;
        if (gl.Major < 3 && PvrConfiguration.IsNaomi2)
            throw new PvrRendererException(PvrRuntime.Translate("OpenGL ES 3.0+ required for Naomi 2"));

        gl.RendContext = context.Rend;
        if (ResetTextureCache)
        {
            PvrGles.TexCache.Clear();
            ResetTextureCache = false;
        }
        PvrGles.TexCache.Cleanup();
        if (UpdateFogTable && PvrConfiguration.Fog)
        {
            UpdateFogTable = false;
            PvrGles.UpdateFogTexture(GetFogTextureSlot(), gl.SingleChannelFormat);
        }
        if (UpdatePalette)
        {
            PvrGles.UpdatePaletteTexture(GetPaletteTextureSlot());
            UpdatePalette = false;
        }
        if (!context.Rend.IsRtt && context.Rend.SwapInterval > 0)
            PvrGlGraphicsContext.Instance.SetSwapInterval(context.Rend.SwapInterval);
        PowerVr2Ta.Parse(context, gl.PrimRestartFixedSupported || gl.PrimRestartSupported);
    }

    private bool RenderFrame(int width, int height)
    {
        if (!PvrConfiguration.EmulateFramebuffer)
            InitVideoRoutingFramebuffer();

        PvrGlContext gl = PvrGles.Gl;
        RenderContext context = gl.RendContext!;
        PvrGlShaderUniforms uniforms = PvrGles.ShaderUniforms;
        PvrGlCache cache = PvrGlCache.Instance;
        bool isRtt = context.IsRtt;
        float maxFz = context.FZMax;
        if (BitConverter.SingleToInt32Bits(maxFz) < 0 || BitConverter.SingleToUInt32Bits(maxFz) > 0x49800000)
            maxFz = 10 * 1024;
        maxFz *= 1.001f;

        gl.Matrices.CalcMatrices(context, width, height);
        uniforms.NdcMat = gl.Matrices.GetNormalMatrix();
        uniforms.DepthCoefs[0] = 2f / maxFz;
        uniforms.DepthCoefs[1] = -1f;
        uniforms.DepthCoefs[2] = 0;
        uniforms.DepthCoefs[3] = 0;

        FOG_COL_VERT.GetRGBColor(uniforms.FogColorVertex);
        FOG_COL_RAM.GetRGBColor(uniforms.FogColorRam);
        uniforms.FogDensity = FOG_DENSITY.Get() * PvrConfiguration.ExtraDepthScale;
        context.FogClampMin.GetRGBAColor(uniforms.FogClampMin);
        context.FogClampMax.GetRGBAColor(uniforms.FogClampMax);

        if (PvrConfiguration.ModifierVolumes)
        {
            PvrGles.CreateModvolShader();
            cache.UseProgram(gl.ModvolShader.Program);
            if (gl.ModvolShader.DepthScale != -1)
                fixed (float* depth = uniforms.DepthCoefs)
                    PvrGl.Uniform4fv(gl.ModvolShader.DepthScale, 1, depth);
            fixed (Matrix4x4* normal = &uniforms.NdcMat)
                PvrGl.UniformMatrix4fv(gl.ModvolShader.NdcMat, 1, 0, (float*)normal);
            PvrGl.Uniform1f(gl.ModvolShader.ShaderColor, 1 - FPU_SHAD_SCALE.scale_factor / 256f);

            cache.UseProgram(gl.N2ModvolShader.Program);
            if (gl.N2ModvolShader.DepthScale != -1)
                fixed (float* depth = uniforms.DepthCoefs)
                    PvrGl.Uniform4fv(gl.N2ModvolShader.DepthScale, 1, depth);
            fixed (Matrix4x4* normal = &uniforms.NdcMat)
                PvrGl.UniformMatrix4fv(gl.N2ModvolShader.NdcMat, 1, 0, (float*)normal);
            PvrGl.Uniform1f(gl.N2ModvolShader.ShaderColor, 1 - FPU_SHAD_SCALE.scale_factor / 256f);
        }

        uniforms.PtAlpha = (PT_ALPHA_REF & 0xff) / 255f;
        if (PvrConfiguration.EmulateFramebuffer && context.FbWCtrl.fb_dither != 0 && context.FbWCtrl.fb_packmode <= 3)
        {
            uniforms.Dithering = true;
            switch (context.FbWCtrl.fb_packmode)
            {
                case 0:
                case 3:
                    uniforms.DitherDivisor[0] = uniforms.DitherDivisor[1] = uniforms.DitherDivisor[2] = 2f;
                    break;
                case 1:
                    uniforms.DitherDivisor[0] = uniforms.DitherDivisor[2] = 2f;
                    uniforms.DitherDivisor[1] = 4f;
                    break;
                case 2:
                    uniforms.DitherDivisor[0] = uniforms.DitherDivisor[1] = uniforms.DitherDivisor[2] = 1f;
                    break;
            }
            uniforms.DitherDivisor[3] = 1f;
        }
        else
        {
            uniforms.Dithering = false;
        }

        foreach (PvrGlPipelineShader shader in gl.Shaders.Values)
        {
            cache.UseProgram(shader.Program);
            uniforms.Set(shader);
            PvrGlNaomi2.ResetN2UniformCache(shader);
        }

        if (gl.PrimRestartFixedSupported)
        {
            PvrGl.Enable(PvrGles.PrimitiveRestartFixedIndex);
        }
        else if (gl.PrimRestartSupported)
        {
            PvrGl.Enable(PvrGl.PrimitiveRestart);
            PvrGl.PrimitiveRestartIndex(uint.MaxValue);
        }

        if (isRtt)
        {
            if (PvrGles.BindRtt() == 0)
                return false;
        }
        else
        {
            Width = width;
            Height = height;
            PvrTransformMatrix.GetVideoShift(out gl.Ofbo.ShiftX, out gl.Ofbo.ShiftY);
            if (PvrGles.InitOutputFramebuffer(width, height) == 0)
                return false;
        }

        cache.Disable(PvrGl.ScissorTest);
        cache.DepthMask(1);
        PvrGl.ClearDepthf(0f);
        PvrGl.StencilMask(0xff);
        PvrGles.Check();
        PvrGl.ClearStencil(0);
        PvrGl.Clear(PvrGl.StencilBufferBit | PvrGl.DepthBufferBit);
        PvrGles.Check();
        if (!isRtt)
            cache.ClearColor(VO_BORDER_COL.Red(), VO_BORDER_COL.Green(), VO_BORDER_COL.Blue(), 1f);
        else
            cache.ClearColor(0f, 0f, 0f, 0f);
        if (isRtt || context.ClearFramebuffer)
            PvrGl.Clear(PvrGl.ColorBufferBit);

        fixed (TaVertex* vertices = CollectionsMarshal.AsSpan(context.Verts))
            gl.Vbo.Geometry!.Update(vertices, context.Verts.Count * sizeof(TaVertex));
        PvrGles.UploadVertexIndices();
        if (context.ModTrig.Count != 0)
            fixed (ModTriangle* modifiers = CollectionsMarshal.AsSpan(context.ModTrig))
                gl.Vbo.Modvols!.Update(modifiers, context.ModTrig.Count * sizeof(ModTriangle));

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
            PvrGles.DrawStrips();
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

    protected static void InitVideoRoutingFramebuffer()
    {
        if (PvrConfiguration.VideoRouting)
        {
            int targetWidth = PvrConfiguration.VideoRoutingScale
                ? PvrConfiguration.VideoRoutingVRes * PvrRuntime.Display.Width / PvrRuntime.Display.Height : PvrRuntime.Display.Width;
            int targetHeight = PvrConfiguration.VideoRoutingScale ? PvrConfiguration.VideoRoutingVRes : PvrRuntime.Display.Height;
            PvrGlContext gl = PvrGles.Gl;
            if (gl.Routing.Framebuffer is { } framebuffer &&
                (framebuffer.GetWidth() != targetWidth || framebuffer.GetHeight() != targetHeight))
            {
                framebuffer.Dispose();
                gl.Routing.Framebuffer = null;
            }
            if (gl.Routing.Framebuffer == null)
            {
                PvrGlCache cache = PvrGlCache.Instance;
                uint texture = cache.GenTexture();
                cache.BindTexture(PvrGl.Texture2D, texture);
                PvrGl.TexImage2D(PvrGl.Texture2D, 0, PvrGl.Rgb8, targetWidth, targetHeight, 0, PvrGl.Rgb, PvrGl.UnsignedByte, null);
                cache.TexParameteri(PvrGl.Texture2D, PvrGl.TextureMinFilter, PvrGl.Linear);
                cache.TexParameteri(PvrGl.Texture2D, PvrGl.TextureMagFilter, PvrGl.Linear);
                cache.TexParameteri(PvrGl.Texture2D, PvrGl.TextureWrapS, PvrGl.ClampToEdge);
                cache.TexParameteri(PvrGl.Texture2D, PvrGl.TextureWrapT, PvrGl.ClampToEdge);
                gl.Routing.Framebuffer = new PvrGlFramebuffer(targetWidth, targetHeight, true, texture);
            }
        }
    }

    internal override void Term()
    {
        try
        {
            PvrGles.TexCache.Clear();
        }
        finally
        {
            PvrGles.TermGles();
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

    protected static void RenderVideoRouting()
    {
        if (PvrConfiguration.VideoRouting)
        {
            PvrGlContext gl = PvrGles.Gl;
            PvrGl.BindFramebuffer(PvrGl.ReadFramebuffer, gl.Ofbo.OriginalFbo);
            gl.Routing.Framebuffer!.Bind(PvrGl.DrawFramebuffer);
            PvrGlCache.Instance.Disable(PvrGl.ScissorTest);
            int targetWidth = PvrConfiguration.VideoRoutingScale
                ? PvrConfiguration.VideoRoutingVRes * PvrRuntime.Display.Width / PvrRuntime.Display.Height : PvrRuntime.Display.Width;
            int targetHeight = PvrConfiguration.VideoRoutingScale ? PvrConfiguration.VideoRoutingVRes : PvrRuntime.Display.Height;
            PvrGl.BlitFramebuffer(0, 0, PvrRuntime.Display.Width, PvrRuntime.Display.Height,
                0, 0, targetWidth, targetHeight, PvrGl.ColorBufferBit, PvrGl.Linear);
            PvrVideoRouting.PublishFrameTexture(gl.Routing.Framebuffer.GetTexture(), PvrGl.Texture2D, targetWidth, targetHeight);
        }
        else
        {
            PvrVideoRouting.TerminateGl();
        }
    }
}
