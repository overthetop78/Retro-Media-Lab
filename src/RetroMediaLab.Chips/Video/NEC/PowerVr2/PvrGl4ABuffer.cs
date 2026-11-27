namespace RetroMediaLab.Chips.Video.NEC.PowerVr2;

internal static unsafe partial class PvrGl4
{
    private static uint _pixelsBuffer;
    private static uint _pixelsPointers;
    private static uint _atomicBuffer;
    private static readonly PvrGl4PipelineShader[] ABufferFinalShaders = [new(), new()];
    private static readonly PvrGl4PipelineShader ABufferClearShader = new();
    private static readonly PvrGl4PipelineShader[] ABufferTrModvolShaders = [new(), new(), new(), new()];
    private static int _maxLayers;
    private static long _pixelBufferSize;
    private static PvrGlBuffer? _quadBuffer;
    private static PvrGlBuffer? _quadIndexBuffer;

    private sealed class AQuadVertexArray : PvrGlVertexArray
    {
        protected override void DefineVtxAttribs()
        {
            PvrGl.EnableVertexAttribArray(0);
            PvrGl.VertexAttribPointer(0, 3, PvrGl.Float, 0, sizeof(float) * 3, 0);
        }
    }

    private static readonly AQuadVertexArray QuadVertexArray = new();

    private const string FinalShaderSource = """

layout(binding = 0) uniform sampler2D tex;
uniform float shade_scale_factor;
#if DITHERING == 1
uniform vec4 ditherDivisor;
#endif

out vec4 FragColor;

uint pixel_list[MAX_PIXELS_PER_FRAGMENT];


int fillAndSortFragmentArray(ivec2 coords)
{
	uint idx = imageLoad(abufferPointerImg, coords).x;
	if (idx == EOL)
		return 0;
	int count = 1;
	pixel_list[0] = idx;
	idx = pixels[idx].next;
	for (; idx != EOL && count < MAX_PIXELS_PER_FRAGMENT; count++)
	{
		Pixel p = pixels[idx];
		int j = count - 1;
		Pixel jp = pixels[pixel_list[j]];
		while (j >= 0
			   && (jp.depth > p.depth
				   || (jp.depth == p.depth && getPolyIndex(jp) > getPolyIndex(p))))
		{
			pixel_list[j + 1] = pixel_list[j];
			j--;
			if (j >= 0)
				jp = pixels[pixel_list[j]];
		}
		pixel_list[j + 1] = idx;
		idx = p.next;
	}

	return count;
}

// Blend fragments back-to-front
vec4 resolveAlphaBlend(ivec2 coords) {
	
	// Copy and sort fragments into a local array
	int num_frag = fillAndSortFragmentArray(coords);
	
	vec4 finalColor = texture(tex, gl_FragCoord.xy / vec2(textureSize(tex, 0)));
	vec4 secondaryBuffer = vec4(0.0); // Secondary accumulation buffer
	
	for (int i = 0; i < num_frag; i++)
	{
		Pixel pixel = pixels[pixel_list[i]];
		PolyParam pp = tr_poly_params[getPolyNumber(pixel)];
		bool area1 = false;
		bool shadowed = false;
		if (isShadowed(pixel))
		{
			if (isTwoVolumes(pp))
				area1 = true;
			else
				shadowed = true;
		}
		vec4 srcColor;
		if (getSrcSelect(pp, area1))
			srcColor = secondaryBuffer;
		else
		{
			srcColor = unpackColors(pixel.color);
			if (shadowed)
				srcColor.rgb *= shade_scale_factor;
		}
		vec4 dstColor = getDstSelect(pp, area1) ? secondaryBuffer : finalColor;
		vec4 srcCoef;
		vec4 dstCoef;
		
		int srcBlend = getSrcBlendFunc(pp, area1);
		switch (srcBlend)
		{
			case ZERO:
				srcCoef = vec4(0.0);
				break;
			case ONE:
				srcCoef = vec4(1.0);
				break;
			case OTHER_COLOR:
				srcCoef = dstColor;
				break;
			case INVERSE_OTHER_COLOR:
				srcCoef = vec4(1.0) - dstColor;
				break;
			case SRC_ALPHA:
				srcCoef = vec4(srcColor.a);
				break;
			case INVERSE_SRC_ALPHA:
				srcCoef = vec4(1.0 - srcColor.a);
				break;
			case DST_ALPHA:
				srcCoef = vec4(dstColor.a);
				break;
			case INVERSE_DST_ALPHA:
				srcCoef = vec4(1.0 - dstColor.a);
				break;
		}
		int dstBlend = getDstBlendFunc(pp, area1);
		switch (dstBlend)
		{
			case ZERO:
				dstCoef = vec4(0.0);
				break;
			case ONE:
				dstCoef = vec4(1.0);
				break;
			case OTHER_COLOR:
				dstCoef = srcColor;
				break;
			case INVERSE_OTHER_COLOR:
				dstCoef = vec4(1.0) - srcColor;
				break;
			case SRC_ALPHA:
				dstCoef = vec4(srcColor.a);
				break;
			case INVERSE_SRC_ALPHA:
				dstCoef = vec4(1.0 - srcColor.a);
				break;
			case DST_ALPHA:
				dstCoef = vec4(dstColor.a);
				break;
			case INVERSE_DST_ALPHA:
				dstCoef = vec4(1.0 - dstColor.a);
				break;
		}
		vec4 result = clamp(dstColor * dstCoef + srcColor * srcCoef, 0.0, 1.0);
		if (getDstSelect(pp, area1))
			secondaryBuffer = result;
		else
			finalColor = result;
	}
#if DITHERING == 1
	float ditherTable[16] = float[](
		5., 13.,  7., 15.,
		9.,  1., 11.,  3.,
		6., 14.,  4., 12.,
		10., 2.,  8.,  0.
	);
	float r = ditherTable[int(mod(gl_FragCoord.y, 4.)) * 4 + int(mod(gl_FragCoord.x, 4.))];
	vec4 dv = vec4(r, r, r, 1.) / ditherDivisor;
	finalColor = clamp(floor(finalColor * 255. + dv) / 255., 0., 1.);
#endif
	
	return finalColor;
	
}

void main(void)
{
	ivec2 coords = ivec2(gl_FragCoord.xy);
	// Compute and output final color for the frame buffer
	// Visualize the number of layers in use
	//FragColor = vec4(float(fillAndSortFragmentArray(coords)) / float(MAX_PIXELS_PER_FRAGMENT * 4), 0, 0, 1);
	FragColor = resolveAlphaBlend(coords);

	// Reset pointers
	imageStore(abufferPointerImg, coords, uvec4(EOL));
}

""";

    private const string ClearShaderSource = """

void main(void)
{
	ivec2 coords = ivec2(gl_FragCoord.xy);

	// Reset pointers
	imageStore(abufferPointerImg, coords, uvec4(EOL));

	// Discard fragment so nothing is written to the framebuffer
	discard;
}

""";

    private const string TrModvolShaderSource = """

in vec3 vtx_uv;

// Must match ModifierVolumeMode enum values
#define MV_XOR		 0
#define MV_OR		 1
#define MV_INCLUSION 2
#define MV_EXCLUSION 3

void main(void)
{
#if MV_MODE == MV_XOR || MV_MODE == MV_OR
	setFragDepth(vtx_uv.z);
#endif
	ivec2 coords = ivec2(gl_FragCoord.xy);
	
	uint idx = imageLoad(abufferPointerImg, coords).x;
	int list_len = 0;
	while (idx != EOL && list_len < MAX_PIXELS_PER_FRAGMENT)
	{
		Pixel pixel = pixels[idx];
		PolyParam pp = tr_poly_params[getPolyNumber(pixel)];
		if (getShadowEnable(pp))
		{
#if MV_MODE == MV_XOR
			if (gl_FragDepth >= pixel.depth)
				atomicXor(pixels[idx].seq_num, SHADOW_STENCIL);
#elif MV_MODE == MV_OR
			if (gl_FragDepth >= pixel.depth)
				atomicOr(pixels[idx].seq_num, SHADOW_STENCIL);
#elif MV_MODE == MV_INCLUSION
			uint prev_val = atomicAnd(pixels[idx].seq_num, ~(SHADOW_STENCIL));
			if ((prev_val & (SHADOW_STENCIL|SHADOW_ACC)) == SHADOW_STENCIL)
				pixels[idx].seq_num = bitfieldInsert(pixel.seq_num, 1u, 31, 1);
#elif MV_MODE == MV_EXCLUSION
			uint prev_val = atomicAnd(pixels[idx].seq_num, ~(SHADOW_STENCIL|SHADOW_ACC));
			if ((prev_val & (SHADOW_STENCIL|SHADOW_ACC)) == SHADOW_ACC)
				pixels[idx].seq_num = bitfieldInsert(pixel.seq_num, 1u, 31, 1);
#endif
		}
		idx = pixel.next;
		list_len++;
	}
	
	discard;
}

""";

    private const string VertexShaderSource = """

in vec3 in_pos;

void main()
{
	gl_Position = vec4(in_pos, 1.0);
}

""";

    private static void CompileFinalAndModVolShaders()
    {
        int layers = Math.Clamp(PvrConfiguration.PerPixelLayers, 1, 256);
        if (_maxLayers != layers)
        {
            _maxLayers = layers;
            foreach (PvrGl4PipelineShader shader in ABufferFinalShaders)
            {
                PvrGlCache.Instance.DeleteProgram(shader.Program);
                shader.Program = 0;
            }
            foreach (PvrGl4PipelineShader shader in ABufferTrModvolShaders)
            {
                PvrGlCache.Instance.DeleteProgram(shader.Program);
                shader.Program = 0;
            }
        }
        if (ABufferFinalShaders[0].Program == 0)
        {
            var vertexShader = new PvrOpenGl4Source();
            vertexShader.AddSource(VertexShaderSource);
            for (int i = 0; i < ABufferFinalShaders.Length; i++)
            {
                var finalShader = new PvrOpenGl4Source();
                finalShader.AddConstant("MAX_PIXELS_PER_FRAGMENT", _maxLayers)
                    .AddConstant("DITHERING", i)
                    .AddConstant("DIV_POS_Z", 0)
                    .AddSource(ShaderHeader)
                    .AddSource(FinalShaderSource);
                CompilePipelineShader(ABufferFinalShaders[i], finalShader.Generate(), vertexShader.Generate());
            }
        }
        if (ABufferTrModvolShaders[0].Program == 0)
        {
            var modVolShader = new PvrOpenGl4Source();
            modVolShader.AddConstant("MAX_PIXELS_PER_FRAGMENT", _maxLayers)
                .AddConstant("DIV_POS_Z", PvrConfiguration.NativeDepthInterpolation ? 1 : 0)
                .AddSource(ShaderHeader)
                .AddSource(TrModvolShaderSource);
            for (int mode = 0; mode < (int)PvrGlModifierVolumeMode.ModeCount; mode++)
            {
                modVolShader.SetConstant("MV_MODE", mode);
                ABufferTrModvolShaders[mode].Gouraud = false;
                CompilePipelineShader(ABufferTrModvolShaders[mode], modVolShader.Generate(), null);
            }
        }
    }

    private static void MakePixelBuffer()
    {
        if (_pixelsBuffer == 0 || _pixelBufferSize != (long)PvrConfiguration.PixelBufferSize)
        {
            long maxSize;
            PvrGl.GetInteger64v(MaxShaderStorageBlockSize, &maxSize);
            _pixelBufferSize = (long)PvrConfiguration.PixelBufferSize;
            nint size = (nint)Math.Min(_pixelBufferSize, maxSize);

            if (_pixelsBuffer == 0)
                fixed (uint* pixels = &_pixelsBuffer)
                    PvrGl.GenBuffers(1, pixels);
            PvrGl.BindBuffer(ShaderStorageBuffer, _pixelsBuffer);
            PvrGl.BufferData(ShaderStorageBuffer, size, null, PvrGl.DynamicCopy);
            PvrGl.BindBufferBase(ShaderStorageBuffer, 0, _pixelsBuffer);
            PvrGles.Check();
        }
    }

    internal static void InitABuffer()
    {
        if (MaxImageWidth > 0 && MaxImageHeight > 0)
        {
            if (_pixelsPointers == 0)
                _pixelsPointers = PvrGlCache.Instance.GenTexture();
            PvrGl.ActiveTexture(PvrGl.Texture4);
            PvrGlCache.Instance.BindTexture(PvrGl.Texture2D, _pixelsPointers);
            PvrGlCache.Instance.TexParameteri(PvrGl.Texture2D, PvrGl.TextureMinFilter, PvrGl.Nearest);
            PvrGlCache.Instance.TexParameteri(PvrGl.Texture2D, PvrGl.TextureMagFilter, PvrGl.Nearest);
            PvrGl.TexStorage2D(PvrGl.Texture2D, 1, PvrGl.R32Ui, MaxImageWidth, MaxImageHeight);
            PvrGl.BindImageTexture(4, _pixelsPointers, 0, false, 0, ReadWrite, PvrGl.R32Ui);
            PvrGles.Check();
        }

        MakePixelBuffer();

        if (_atomicBuffer == 0)
        {
            fixed (uint* atomic = &_atomicBuffer)
                PvrGl.GenBuffers(1, atomic);
            PvrGl.BindBuffer(AtomicCounterBuffer, _atomicBuffer);
            PvrGl.BufferData(AtomicCounterBuffer, 4, null, PvrGl.DynamicCopy);
            PvrGl.BindBufferBase(AtomicCounterBuffer, 0, _atomicBuffer);
            int zero = 0;
            PvrGl.BufferSubData(AtomicCounterBuffer, 0, sizeof(int), &zero);
            PvrGles.Check();
        }

        CompileFinalAndModVolShaders();
        if (ABufferClearShader.Program == 0)
        {
            var vertexShader = new PvrOpenGl4Source();
            vertexShader.AddSource(VertexShaderSource);
            var clearShader = new PvrOpenGl4Source();
            clearShader.AddConstant("DIV_POS_Z", 0)
                .AddSource(ShaderHeader)
                .AddSource(ClearShaderSource);
            CompilePipelineShader(ABufferClearShader, clearShader.Generate(), vertexShader.Generate());
        }
        if (_quadBuffer == null)
        {
            _quadBuffer = new PvrGlBuffer(PvrGl.ArrayBuffer, PvrGl.StaticDraw);
            float* vertices = stackalloc float[12]
            {
                -1, 1, 1,
                -1, -1, 1,
                1, 1, 1,
                1, -1, 1
            };
            _quadBuffer.Update(vertices, sizeof(float) * 12);
        }
        if (_quadIndexBuffer == null)
        {
            _quadIndexBuffer = new PvrGlBuffer(PvrGl.ElementArrayBuffer, PvrGl.StaticDraw);
            ushort* indices = stackalloc ushort[5] { 0, 1, 2, 1, 3 };
            _quadIndexBuffer.Update(indices, sizeof(ushort) * 5);
        }

        QuadVertexArray.Bind(_quadBuffer, _quadIndexBuffer);
        PvrGlVertexArray.Unbind();

        if (_pixelsPointers != 0)
        {
            PvrGlCache.Instance.UseProgram(ABufferClearShader.Program);
            ShaderUniforms.Set(ABufferClearShader);
            PvrGlCache.Instance.Disable(PvrGl.DepthTest);
            PvrGlCache.Instance.Disable(PvrGl.CullFaceMode);
            PvrGlCache.Instance.Disable(PvrGl.ScissorTest);

            ABufferDrawQuad();
            PvrGl.MemoryBarrier(ShaderImageAccessBarrierBit);
        }

        PvrGles.Check();
    }

    internal static void TermABuffer()
    {
        try
        {
            if (_pixelsPointers != 0)
            {
                fixed (uint* pixels = &_pixelsPointers)
                    PvrGlCache.Instance.DeleteTextures(1, pixels);
                _pixelsPointers = 0;
            }
            if (_pixelsBuffer != 0)
            {
                fixed (uint* pixels = &_pixelsBuffer)
                    PvrGl.DeleteBuffers(1, pixels);
                _pixelsBuffer = 0;
            }
            if (_atomicBuffer != 0)
            {
                fixed (uint* atomic = &_atomicBuffer)
                    PvrGl.DeleteBuffers(1, atomic);
                _atomicBuffer = 0;
            }
            QuadVertexArray.Term();
        }
        finally
        {
            try
            {
                _quadBuffer?.Dispose();
            }
            finally
            {
                _quadBuffer = null;
                _quadIndexBuffer?.Dispose();
                _quadIndexBuffer = null;
            }
        }
        foreach (PvrGl4PipelineShader shader in ABufferFinalShaders)
        {
            PvrGlCache.Instance.DeleteProgram(shader.Program);
            shader.Program = 0;
        }
        PvrGlCache.Instance.DeleteProgram(ABufferClearShader.Program);
        ABufferClearShader.Program = 0;
        foreach (PvrGl4PipelineShader shader in ABufferTrModvolShaders)
        {
            PvrGlCache.Instance.DeleteProgram(shader.Program);
            shader.Program = 0;
        }
        _maxLayers = 0;
    }

    internal static void ReshapeABuffer(int width, int height)
    {
        if (_pixelsPointers != 0)
        {
            fixed (uint* pixels = &_pixelsPointers)
                PvrGlCache.Instance.DeleteTextures(1, pixels);
            _pixelsPointers = 0;
        }
        InitABuffer();
    }

    private static void ABufferDrawQuad()
    {
        QuadVertexArray.Bind(_quadBuffer!, _quadIndexBuffer);
        try
        {
            PvrGl.DrawElements(PvrGl.TriangleStrip, 5, PvrGl.UnsignedShort, 0);
        }
        finally
        {
            PvrGlVertexArray.Unbind();
        }
        PvrGles.Check();
    }

    internal static void DrawTranslucentModVols(int first, int count, bool useOpaqueGeom)
    {
        RenderContext context = PvrGles.Gl.RendContext!;
        if (count == 0 || context.ModTrig.Count == 0)
            return;
        CompileFinalAndModVolShaders();
        SetupModvolVbo();

        PvrGl.ActiveTexture(PvrGl.Texture2);
        PvrGl.BindTexture(PvrGl.Texture2D, 0);
        PvrGl.ActiveTexture(PvrGl.Texture3);
        PvrGl.BindTexture(PvrGl.Texture2D, 0);
        PvrGl.ActiveTexture(PvrGl.Texture1);
        PvrGl.BindTexture(PvrGl.Texture2D, 0);
        PvrGl.ActiveTexture(PvrGl.Texture0);
        PvrGlCache.Instance.BindTexture(PvrGl.Texture2D, 0);

        PvrGlCache.Instance.Disable(PvrGl.DepthTest);
        PvrGlCache.Instance.Disable(PvrGl.StencilTest);
        PvrGles.Check();

        List<ModifierVolumeParam> parameters = useOpaqueGeom ? context.GlobalParamMvo : context.GlobalParamMvoTr;
        PvrGl.MemoryBarrier(ShaderImageAccessBarrierBit | BufferUpdateBarrierBit);
        int modBase = -1;

        for (int i = 0; i < count; i++)
        {
            ModifierVolumeParam param = parameters[first + i];
            if (param.Count == 0)
                continue;
            uint mode = param.Isp.DepthMode;
            if (param.First + param.Count > (uint)context.ModTrig.Count)
            {
                PvrRuntime.Log.Error("Corrupted translucent poly[{0}]: first {1} end {2}", i, param.First, param.First + param.Count);
                continue;
            }
            if (modBase == -1)
                modBase = (int)param.First;

            PvrGl4PipelineShader shader = ABufferTrModvolShaders[param.Isp.VolumeLast == 0 && mode > 0
                ? (int)PvrGlModifierVolumeMode.Or : (int)PvrGlModifierVolumeMode.Xor];
            PvrGlCache.Instance.UseProgram(shader.Program);
            ShaderUniforms.Set(shader);
            PvrGles.SetCull(param.Isp.CullMode);
            PvrGles.Check();
            PvrGl.MemoryBarrier(BufferUpdateBarrierBit);
            PvrGl.DrawArrays(PvrGl.Triangles, (int)param.First * 3, (int)param.Count * 3);
            PvrGles.Check();

            if (mode == 1 || mode == 2)
            {
                shader = ABufferTrModvolShaders[mode == 1 ? (int)PvrGlModifierVolumeMode.Inclusion : (int)PvrGlModifierVolumeMode.Exclusion];
                PvrGlCache.Instance.UseProgram(shader.Program);
                ShaderUniforms.Set(shader);
                PvrGl.MemoryBarrier(BufferUpdateBarrierBit);
                PvrGl.DrawArrays(PvrGl.Triangles, modBase * 3, ((int)(param.First + param.Count) - modBase) * 3);
                PvrGles.Check();
                modBase = -1;
            }
        }
        SetupMainVbo();
    }

    internal static void CheckOverflowAndReset()
    {
        uint maxPixelIndex = 0;
        PvrGl.BufferSubData(AtomicCounterBuffer, 0, sizeof(uint), &maxPixelIndex);
    }

    internal static void RenderABuffer(bool lastPass)
    {
        CompileFinalAndModVolShaders();
        var control = PvrGles.Gl.RendContext!.FbWCtrl;
        if (lastPass && PvrConfiguration.EmulateFramebuffer && control.fb_dither != 0 && control.fb_packmode <= 3)
        {
            PvrGlCache.Instance.UseProgram(ABufferFinalShaders[1].Program);
            switch (control.fb_packmode)
            {
                case 0:
                case 3:
                    ShaderUniforms.DitherDivisor[0] = ShaderUniforms.DitherDivisor[1] = ShaderUniforms.DitherDivisor[2] = 2f;
                    break;
                case 1:
                    ShaderUniforms.DitherDivisor[0] = ShaderUniforms.DitherDivisor[2] = 2f;
                    ShaderUniforms.DitherDivisor[1] = 4f;
                    break;
                case 2:
                    ShaderUniforms.DitherDivisor[0] = ShaderUniforms.DitherDivisor[1] = ShaderUniforms.DitherDivisor[2] = 1f;
                    break;
            }
            ShaderUniforms.DitherDivisor[3] = 1f;
            ShaderUniforms.Set(ABufferFinalShaders[1]);
        }
        else
        {
            PvrGlCache.Instance.UseProgram(ABufferFinalShaders[0].Program);
            ShaderUniforms.Set(ABufferFinalShaders[0]);
        }

        PvrGlCache.Instance.Disable(PvrGl.DepthTest);
        PvrGlCache.Instance.Disable(PvrGl.CullFaceMode);
        PvrGlCache.Instance.Disable(PvrGl.ScissorTest);
        PvrGl.MemoryBarrier(ShaderImageAccessBarrierBit | BufferUpdateBarrierBit);
        PvrGlCache.Instance.Disable(PvrGl.Blend);
        ABufferDrawQuad();
        PvrGl.ActiveTexture(PvrGl.Texture0);
        MakePixelBuffer();
        PvrGles.Check();
    }
}
