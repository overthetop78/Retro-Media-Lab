namespace RetroMediaLab.Chips.Video.NEC.PowerVr2;

internal unsafe sealed partial class PvrGlQuadDrawer
{
    private const string VertexShader = """

in highp vec3 in_pos;
in mediump vec2 in_uv;
out mediump vec2 vtx_uv;

void main()
{
	vtx_uv = in_uv;
#if ROTATE == 1
	gl_Position = vec4(-in_pos.y, in_pos.x, in_pos.z, 1.0);
#else
	gl_Position = vec4(in_pos, 1.0);
#endif
}

""";

    private const string FragmentShader = """

in mediump vec2 vtx_uv;

uniform sampler2D tex;
uniform mediump vec4 tint;

void main()
{
	gl_FragColor = tint * texture(tex, vtx_uv);
}

""";

    internal PvrGlQuadDrawer()
    {
        bool initialized = false;
        try
        {
            var fragmentShader = new PvrOpenGlSource();
            fragmentShader.AddSource(PvrGles.PixelCompatShader).AddSource(FragmentShader);
            var vertexShader = new PvrOpenGlSource();
            vertexShader.AddConstant("ROTATE", 0).AddSource(PvrGles.VertexCompatShader).AddSource(VertexShader);

            string fragmentGlsl = fragmentShader.Generate();
            _shader = PvrGles.CompileAndLink(vertexShader.Generate(), fragmentGlsl);
            int texture = PvrGl.GetUniformLocation(_shader, "tex");
            PvrGl.Uniform1i(texture, 0);
            _tintUniform = PvrGl.GetUniformLocation(_shader, "tint");

            vertexShader.SetConstant("ROTATE", 1);
            _rot90Shader = PvrGles.CompileAndLink(vertexShader.Generate(), fragmentGlsl);
            texture = PvrGl.GetUniformLocation(_rot90Shader, "tex");
            PvrGl.Uniform1i(texture, 0);
            _rot90TintUniform = PvrGl.GetUniformLocation(_rot90Shader, "tint");

            _quadIndexBuffer = new PvrGlBuffer(PvrGl.ElementArrayBuffer, PvrGl.StaticDraw);
            ushort* indices = stackalloc ushort[5] { 0, 1, 2, 1, 3 };
            _quadIndexBuffer.Update(indices, sizeof(ushort) * 5);

            _quadBuffer = new PvrGlBuffer(PvrGl.ArrayBuffer, PvrGl.StaticDraw);
            {
                float* vertices = stackalloc float[20]
                {
                    -1f,  1f, 1f, 0f, 1f,
                    -1f, -1f, 1f, 0f, 0f,
                     1f,  1f, 1f, 1f, 1f,
                     1f, -1f, 1f, 1f, 0f
                };
                _quadBuffer.Update(vertices, sizeof(float) * 20);
            }
            _quadBufferSwapY = new PvrGlBuffer(PvrGl.ArrayBuffer, PvrGl.StaticDraw);
            {
                float* vertices = stackalloc float[20]
                {
                    -1f,  1f, 1f, 0f, 0f,
                    -1f, -1f, 1f, 0f, 1f,
                     1f,  1f, 1f, 1f, 0f,
                     1f, -1f, 1f, 1f, 1f
                };
                _quadBufferSwapY.Update(vertices, sizeof(float) * 20);
            }
            _quadBufferCustom = new PvrGlBuffer(PvrGl.ArrayBuffer);
            PvrGl.Check();
            initialized = true;
        }
        finally
        {
            if (!initialized)
                Dispose();
        }
    }

    public void Dispose()
    {
        try
        {
            _quadIndexBuffer?.Dispose();
            _quadBuffer?.Dispose();
            _quadBufferSwapY?.Dispose();
            _quadBufferCustom?.Dispose();
        }
        finally
        {
            _quadIndexBuffer = null;
            _quadBuffer = null;
            _quadBufferSwapY = null;
            _quadBufferCustom = null;
            try
            {
                _quadVertexArray.Term();
                _quadVertexArraySwapY.Term();
                _quadVertexArrayCustom.Term();
            }
            finally
            {
                PvrGlCache.Instance.DeleteProgram(_shader);
                _shader = 0;
                PvrGlCache.Instance.DeleteProgram(_rot90Shader);
                _rot90Shader = 0;
            }
        }
    }

    internal void Draw(uint textureId, bool rotate = false, bool swapY = false, float* coords = null, float* color = null)
    {
        PvrGlCache cache = PvrGlCache.Instance;
        cache.Disable(PvrGl.ScissorTest);
        cache.Disable(PvrGl.DepthTest);
        cache.Disable(PvrGl.StencilTest);
        cache.Disable(PvrGl.CullFaceMode);

        cache.UseProgram(rotate ? _rot90Shader : _shader);

        PvrGl.ActiveTexture(PvrGl.Texture0);
        cache.BindTexture(PvrGl.Texture2D, textureId);

        float* white = stackalloc float[4] { 1f, 1f, 1f, 1f };
        if (color == null)
            color = white;
        PvrGl.Uniform4fv(rotate ? _rot90TintUniform : _tintUniform, 1, color);

        if (coords == null)
        {
            if (swapY)
                _quadVertexArraySwapY.Bind(_quadBufferSwapY!, _quadIndexBuffer!);
            else
                _quadVertexArray.Bind(_quadBuffer!, _quadIndexBuffer!);
        }
        else
        {
            _quadBufferCustom!.Update(coords, sizeof(float) * 4 * 5);
            _quadVertexArrayCustom.Bind(_quadBufferCustom, _quadIndexBuffer!);
        }

        try
        {
            PvrGl.DrawElements(PvrGl.TriangleStrip, 5, PvrGl.UnsignedShort, 0);
        }
        finally
        {
            PvrGlVertexArray.Unbind();
        }
        PvrGl.Check();
    }
}
