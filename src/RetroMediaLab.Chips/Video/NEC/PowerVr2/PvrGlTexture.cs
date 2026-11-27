namespace RetroMediaLab.Chips.Video.NEC.PowerVr2;

internal sealed unsafe partial class PvrGlTextureCacheData
{
    static PvrGlTextureCacheData()
    {
        _uploadToGpu = static (texture, width, height, buffer, mipmapped, mipmapsIncluded) =>
            texture.UploadToGpuGl2(width, height, buffer, mipmapped, mipmapsIncluded);
    }

    private static void GetOpenGlTexParams(PvrTextureType textureType, out uint bytesPerPixel,
        out uint glType, out uint components, out uint internalFormat)
    {
        components = PvrGl.Rgba;
        bytesPerPixel = 2;
        switch (textureType)
        {
            case PvrTextureType._5551:
                glType = PvrGl.UnsignedShort5551;
                internalFormat = PvrGl.Rgb5A1;
                break;
            case PvrTextureType._565:
                glType = PvrGl.UnsignedShort565;
                components = PvrGl.Rgb;
                internalFormat = PvrGl.Rgb565;
                break;
            case PvrTextureType._4444:
                glType = PvrGl.UnsignedShort4444;
                internalFormat = PvrGl.Rgba4;
                break;
            case PvrTextureType._8888:
                bytesPerPixel = 4;
                glType = PvrGl.UnsignedByte;
                internalFormat = PvrGles.Rgba8;
                break;
            case PvrTextureType._8:
                bytesPerPixel = 1;
                glType = PvrGl.UnsignedByte;
                components = PvrGles.Gl.SingleChannelFormat;
                internalFormat = PvrGles.R8;
                break;
            default:
                PvrRuntime.Die("Unsupported texture type");
                glType = 0;
                internalFormat = 0;
                break;
        }
    }

    private void UploadToGpuGl2(int width, int height, byte* buffer, bool mipmapped, bool mipmapsIncluded)
    {
        if (TextureId == 0)
            TextureId = PvrGlCache.Instance.GenTexture();
        PvrGlCache.Instance.BindTexture(PvrGl.Texture2D, TextureId);
        GetOpenGlTexParams(TextureType, out uint bytesPerPixel, out uint glType, out uint components, out _);

        if (mipmapsIncluded)
        {
            int levels = 0;
            int dimension = width;
            while (dimension != 0)
            {
                levels++;
                dimension >>= 1;
            }
            for (int i = 0; i < levels; i++)
            {
                PvrGl.TexImage2D(PvrGl.Texture2D, levels - i - 1, components, 1 << i, 1 << i, 0,
                    components, glType, buffer);
                buffer += (1 << (2 * i)) * bytesPerPixel;
            }
        }
        else
        {
            PvrGl.TexImage2D(PvrGl.Texture2D, 0, components, width, height, 0, components, glType, buffer);
            if (mipmapped)
                PvrGl.GenerateMipmap(PvrGl.Texture2D);
        }
    }

    private void UploadToGpuGl4(int width, int height, byte* buffer, bool mipmapped, bool mipmapsIncluded)
    {
        GetOpenGlTexParams(TextureType, out uint bytesPerPixel, out uint glType, out uint components, out uint internalFormat);
        int levels = 1;
        if (mipmapped)
        {
            levels = 0;
            int dimension = width;
            while (dimension != 0)
            {
                levels++;
                dimension >>= 1;
            }
        }
        int[] uploadStorage = [width, height, levels, (int)internalFormat];
        if (TextureId != 0 && !Storage.AsSpan().SequenceEqual(uploadStorage))
        {
            PvrGles.TexCache.DeleteLater(TextureId);
            TextureId = 0;
        }
        if (TextureId == 0)
        {
            TextureId = PvrGlCache.Instance.GenTexture();
            PvrGlCache.Instance.BindTexture(PvrGl.Texture2D, TextureId);
            PvrGl.TexStorage2D(PvrGl.Texture2D, levels, internalFormat, width, height);
            Storage = uploadStorage;
        }
        else
        {
            PvrGlCache.Instance.BindTexture(PvrGl.Texture2D, TextureId);
        }
        if (mipmapsIncluded)
        {
            for (int i = 0; i < levels; i++)
            {
                PvrGl.TexSubImage2D(PvrGl.Texture2D, levels - i - 1, 0, 0, 1 << i, 1 << i, components, glType, buffer);
                buffer += (1 << (2 * i)) * bytesPerPixel;
            }
        }
        else
        {
            PvrGl.TexSubImage2D(PvrGl.Texture2D, 0, 0, 0, width, height, components, glType, buffer);
            if (mipmapped)
                PvrGl.GenerateMipmap(PvrGl.Texture2D);
        }
    }

    internal override void UploadToGpu(int width, int height, byte* buffer, bool mipmapped, bool mipmapsIncluded = false)
    {
        _uploadToGpu!(this, width, height, buffer, mipmapped, mipmapsIncluded);
        PvrGles.Check();
    }

    internal static void SetUploadToGpuFlavor()
    {
        PvrGlContext gl = PvrGles.Gl;
        if (gl.Major > 4 || (gl.Major == 4 && gl.Minor >= 2) || (gl.IsGles && gl.Major >= 3))
            _uploadToGpu = static (texture, width, height, buffer, mipmapped, mipmapsIncluded) =>
                texture.UploadToGpuGl4(width, height, buffer, mipmapped, mipmapsIncluded);
    }

    internal override bool Delete()
    {
        if (!base.Delete())
            return false;
        if (TextureId != 0)
        {
            fixed (uint* id = &TextureId)
                PvrGlCache.Instance.DeleteTextures(1, id);
            TextureId = 0;
        }
        return true;
    }
}

internal static unsafe partial class PvrGles
{
    internal static uint BindRtt(bool withDepthBuffer = true)
    {
        RenderContext context = Gl.RendContext!;
        uint channels;
        uint format;
        switch (context.FbWCtrl.fb_packmode)
        {
            case 0:
            case 2:
            case 3:
                channels = PvrGl.Rgba;
                format = PvrGl.UnsignedByte;
                break;
            case 1:
                channels = PvrGl.Rgb;
                format = PvrGl.UnsignedShort565;
                break;
            case 4:
            case 5:
            case 6:
                PvrRuntime.Log.Warn("Unsupported render to texture format: {0}", context.FbWCtrl.fb_packmode);
                return 0;
            default:
                PvrRuntime.Log.Warn("Invalid framebuffer format: 7");
                return 0;
        }

        int width = (int)context.FramebufferWidth;
        int height = (int)context.FramebufferHeight;
        uint address = context.FbWSof1 & PvrMemory.VramMask;
        PvrRuntime.Log.Debug("RTT packmode={0} stride={1} - {2} x {3} @ {4:x6}", context.FbWCtrl.fb_packmode,
            context.FbWLineStride * 8, width, height, address);
        Gl.Rtt.Framebuffer?.Dispose();
        Gl.Rtt.Framebuffer = null;

        uint texture = PvrGlCache.Instance.GenTexture();
        PvrGlCache.Instance.BindTexture(PvrGl.Texture2D, texture);
        PvrGl.TexImage2D(PvrGl.Texture2D, 0, channels, width, height, 0, channels, format, null);
        Gl.Rtt.Framebuffer = new PvrGlFramebuffer(width, height, withDepthBuffer, texture);
        PvrGl.Viewport(0, 0, width, height);

        return Gl.Rtt.Framebuffer.GetFramebuffer();
    }

    internal static void ReadRttBuffer()
    {
        RenderContext context = Gl.RendContext!;
        uint width = context.FramebufferWidth;
        uint height = context.FramebufferHeight;
        uint packMode = context.FbWCtrl.fb_packmode;
        uint address = context.FbWSof1 & PvrMemory.VramMask;

        if (PvrConfiguration.RenderToTextureBuffer)
        {
            PvrGl.PixelStorei(PvrGl.PackAlignment, 1);
            ushort* destination = (ushort*)(PvrMemory.VramPointer + address);
            uint stride = context.FbWLineStride * 8;
            if (stride == 0)
                stride = width * 2;

            int colorFormat;
            int colorType;
            PvrGl.GetIntegerv(PvrGl.ImplementationColorReadFormat, &colorFormat);
            PvrGl.GetIntegerv(PvrGl.ImplementationColorReadType, &colorType);
            if (packMode == 1
                && stride == width * 2
                && colorFormat == PvrGl.Rgb
                && colorType == PvrGl.UnsignedShort565
                && context.FbClip.Origin.X == 0
                && context.FbClip.Origin.Y == 0
                && context.FbClip.Size.X >= (int)width
                && context.FbClip.Size.Y >= (int)height)
            {
                PvrGl.ReadPixels(0, 0, (int)width, (int)height, PvrGl.Rgb, PvrGl.UnsignedShort565, destination);
            }
            else
            {
                using var buffer = new PvrPixelBuffer<uint>();
                buffer.Init(width, height);
                byte* pixels = (byte*)buffer.Data();
                PvrGl.ReadPixels(0, 0, (int)width, (int)height, PvrGl.Rgba, PvrGl.UnsignedByte, pixels);
                PvrTextureCache.WriteTextureToVram(width, height, pixels, destination, context.FbWCtrl, stride, context.FbClip);
            }
            Check();
        }
        else
        {
            PvrTransformMatrix.GetPvrFramebufferSize(context, out int widthPowerOf2, out int heightPowerOf2);
            if (widthPowerOf2 <= 1024 && heightPowerOf2 <= 1024)
            {
                PvrGlTextureCacheData data = TexCache.GetRenderToTexture(address, packMode, (uint)widthPowerOf2, (uint)heightPowerOf2);
                fixed (uint* id = &data.TextureId)
                    PvrGlCache.Instance.DeleteTextures(1, id);
                data.TextureId = Gl.Rtt.Framebuffer!.DetachTexture();
                Array.Clear(data.Storage);
                data.Dirty = 0;
                data.UnprotectVram();
            }
        }
        PvrGl.BindFramebuffer(PvrGl.Framebuffer, Gl.Ofbo.OriginalFbo);
    }

    internal static void GlReadFramebuffer(in PvrFramebufferInfo info)
    {
        using var pixels = new PvrPixelBuffer<uint>();
        PvrTextureCache.ReadFramebuffer<PvrRgbaPacker>(info, pixels, out Gl.Dcfb.Width, out Gl.Dcfb.Height);
        if (Gl.Dcfb.Texture == 0)
            Gl.Dcfb.Texture = PvrGlCache.Instance.GenTexture();
        PvrGlCache.Instance.BindTexture(PvrGl.Texture2D, Gl.Dcfb.Texture);
        PvrGlCache.Instance.TexParameteri(PvrGl.Texture2D, PvrGl.TextureWrapS, PvrGl.ClampToEdge);
        PvrGlCache.Instance.TexParameteri(PvrGl.Texture2D, PvrGl.TextureWrapT, PvrGl.ClampToEdge);
        PvrGlCache.Instance.TexParameteri(PvrGl.Texture2D, PvrGl.TextureMinFilter, PvrGl.Linear);
        PvrGlCache.Instance.TexParameteri(PvrGl.Texture2D, PvrGl.TextureMagFilter, PvrGl.Linear);
        PvrGl.TexImage2D(PvrGl.Texture2D, 0, PvrGl.Rgba, Gl.Dcfb.Width, Gl.Dcfb.Height, 0, PvrGl.Rgba,
            PvrGl.UnsignedByte, pixels.Data());
    }

    internal static uint InitOutputFramebuffer(int width, int height)
    {
        if (Gl.Ofbo.Framebuffer is { } framebuffer && (width != framebuffer.GetWidth() || height != framebuffer.GetHeight()))
        {
            framebuffer.Dispose();
            Gl.Ofbo.Framebuffer = null;
        }
        if (Gl.Ofbo.Framebuffer == null)
        {
            uint texture = PvrGlCache.Instance.GenTexture();
            PvrGlCache.Instance.BindTexture(PvrGl.Texture2D, texture);
            PvrGl.TexImage2D(PvrGl.Texture2D, 0, PvrGl.Rgba, width, height, 0, PvrGl.Rgba, PvrGl.UnsignedByte, null);
            PvrGlCache.Instance.TexParameteri(PvrGl.Texture2D, PvrGl.TextureMinFilter, PvrGl.Linear);
            PvrGlCache.Instance.TexParameteri(PvrGl.Texture2D, PvrGl.TextureMagFilter, PvrGl.Linear);
            PvrGlCache.Instance.TexParameteri(PvrGl.Texture2D, PvrGl.TextureWrapS, PvrGl.ClampToEdge);
            PvrGlCache.Instance.TexParameteri(PvrGl.Texture2D, PvrGl.TextureWrapT, PvrGl.ClampToEdge);
            Gl.Ofbo.Framebuffer = new PvrGlFramebuffer(width, height, true, texture);
            PvrGlCache.Instance.Disable(PvrGl.ScissorTest);
            PvrGlCache.Instance.ClearColor(0f, 0f, 0f, 0f);
            PvrGl.Clear(PvrGl.ColorBufferBit);
        }
        else
        {
            Gl.Ofbo.Framebuffer.Bind();
        }
        PvrGl.Viewport(0, 0, width, height);
        Check();

        return Gl.Ofbo.Framebuffer.GetFramebuffer();
    }

    internal static bool TestBlitFramebuffer()
    {
        int original = 0;
        PvrGl.GetIntegerv(PvrGl.FramebufferBinding, &original);
        uint texture = PvrGlCache.Instance.GenTexture();
        PvrGlCache.Instance.BindTexture(PvrGl.Texture2D, texture);

        uint* pixels = stackalloc uint[32 * 32];
        for (int i = 0; i < 16 * 32; i++)
            pixels[i] = 0xff0000ff;
        for (int i = 16 * 32; i < 32 * 32; i++)
            pixels[i] = 0xff00ff00;
        PvrGl.TexImage2D(PvrGl.Texture2D, 0, PvrGl.Rgba, 32, 32, 0, PvrGl.Rgba, PvrGl.UnsignedByte, pixels);
        using var source = new PvrGlFramebuffer(32, 32, false, texture);
        using var destination = new PvrGlFramebuffer(32, 64, false, true);

        source.Bind(PvrGl.ReadFramebuffer);
        uint error = PvrGl.CheckFramebufferStatus(PvrGl.ReadFramebuffer);
        if (error != PvrGl.FramebufferComplete)
        {
            PvrRuntime.Log.Warn("testBlitFramebuffer: Source framebuffer error {0:x}", error);
            return false;
        }
        destination.Bind(PvrGl.DrawFramebuffer);
        error = PvrGl.CheckFramebufferStatus(PvrGl.DrawFramebuffer);
        if (error != PvrGl.FramebufferComplete)
        {
            PvrRuntime.Log.Warn("testBlitFramebuffer: Destination framebuffer error {0:x}", error);
            return false;
        }

        PvrGlCache.Instance.Disable(PvrGl.ScissorTest);
        PvrGlCache.Instance.ClearColor(0, 0, 0, 1);
        PvrGl.Clear(PvrGl.ColorBufferBit);
        PvrGl.BlitFramebuffer(0, -1, 32, 31, 0, 64, 32, 0, PvrGl.ColorBufferBit, PvrGl.Nearest);
        uint* output = stackalloc uint[32 * 64];
        destination.Bind(PvrGl.ReadFramebuffer);
        PvrGl.PixelStorei(PvrGl.PackAlignment, 1);
        PvrGl.ReadPixels(0, 0, 32, 64, PvrGl.Rgba, PvrGl.UnsignedByte, output);
        PvrGl.BindFramebuffer(PvrGl.Framebuffer, unchecked((uint)original));
        error = PvrGl.GetError();
        if (error != PvrGl.NoError)
        {
            PvrRuntime.Log.Warn("testBlitFramebuffer: OpenGL error {0:x}", error);
            return false;
        }
        if (output[32 * 2] != 0xff00ff00)
        {
            PvrRuntime.Log.Warn("testBlitFramebuffer: Expected 0xFF00FF00 but was {0:x8}", output[0]);
            return false;
        }
        if (output[31 * 64 - 1] != 0xff0000ff)
        {
            PvrRuntime.Log.Warn("testBlitFramebuffer: Expected 0xFF0000FF but was {0:x8}", output[32 * 64 - 1]);
            return false;
        }
        return true;
    }
}

internal unsafe partial class PvrOpenGlRenderer
{
    internal override PvrGlTextureCacheData? GetTexture(TSP tsp, TCW tcw, int area)
    {
        PvrGlTextureCacheData? texture = PvrGles.TexCache.GetTextureCacheData(tsp, tcw, area);
        if (texture.NeedsUpdate())
        {
            if (!texture.Update())
                texture = null;
        }
        else if (texture.IsCustomTextureAvailable())
        {
            PvrGles.TexCache.DeleteLater(texture.TextureId);
            texture.TextureId = 0;
            texture.CheckCustomTexture();
        }
        return texture;
    }
}

internal sealed unsafe partial class PvrGlFramebuffer
{
    internal PvrGlFramebuffer(int width, int height, bool withDepth = false, uint texture = 0)
    {
        _width = width;
        _height = height;
        _texture = texture;
        bool initialized = false;
        try
        {
            if (_texture == 0)
            {
                if (PvrGles.Gl.Major < 3)
                    MakeColorTexture();
                else
                    MakeColorBuffer();
            }
            MakeFramebuffer(withDepth);
            initialized = true;
        }
        finally
        {
            if (!initialized)
                Dispose();
        }
    }

    private void MakeFramebuffer(bool withDepth)
    {
        fixed (uint* framebuffer = &_framebuffer)
            PvrGl.GenFramebuffers(1, framebuffer);
        Bind();

        if (withDepth)
        {
            fixed (uint* depth = &_depthBuffer)
                PvrGl.GenRenderbuffers(1, depth);
            PvrGl.BindRenderbuffer(PvrGl.Renderbuffer, _depthBuffer);
            PvrGlContext gl = PvrGles.Gl;
            if (gl.IsGles)
            {
                if (gl.Major >= 3)
                    PvrGl.RenderbufferStorage(PvrGl.Renderbuffer, PvrGl.Depth24Stencil8, _width, _height);
                else if (gl.PackedDepthStencilSupported)
                    PvrGl.RenderbufferStorage(PvrGl.Renderbuffer, PvrGl.Depth24Stencil8Oes, _width, _height);
                else if (gl.Depth24Supported)
                    PvrGl.RenderbufferStorage(PvrGl.Renderbuffer, PvrGl.DepthComponent24Oes, _width, _height);
                else
                    PvrGl.RenderbufferStorage(PvrGl.Renderbuffer, PvrGl.DepthComponent16, _width, _height);
            }
            else
            {
                PvrGl.RenderbufferStorage(PvrGl.Renderbuffer, PvrGl.Depth24Stencil8, _width, _height);
            }
            PvrGl.FramebufferRenderbuffer(PvrGl.Framebuffer, PvrGl.DepthAttachment, PvrGl.Renderbuffer, _depthBuffer);
            if (!gl.IsGles || gl.Major >= 3 || gl.PackedDepthStencilSupported)
                PvrGl.FramebufferRenderbuffer(PvrGl.Framebuffer, PvrGl.StencilAttachment, PvrGl.Renderbuffer, _depthBuffer);
        }

        if (_texture != 0)
            PvrGl.FramebufferTexture2D(PvrGl.Framebuffer, PvrGl.ColorAttachment0, PvrGl.Texture2D, _texture, 0);
        else
            PvrGl.FramebufferRenderbuffer(PvrGl.Framebuffer, PvrGl.ColorAttachment0, PvrGl.Renderbuffer, _colorBuffer);
    }

    internal PvrGlFramebuffer(int width, int height, bool withDepth, bool withTexture)
    {
        _width = width;
        _height = height;
        bool initialized = false;
        try
        {
            if (PvrGles.Gl.Major < 3 || withTexture)
                MakeColorTexture();
            else
                MakeColorBuffer();
            MakeFramebuffer(withDepth);
            initialized = true;
        }
        finally
        {
            if (!initialized)
                Dispose();
        }
    }

    private void MakeColorTexture()
    {
        _texture = PvrGlCache.Instance.GenTexture();
        PvrGlCache.Instance.BindTexture(PvrGl.Texture2D, _texture);
        PvrGl.TexImage2D(PvrGl.Texture2D, 0, PvrGl.Rgba, _width, _height, 0, PvrGl.Rgba, PvrGl.UnsignedByte, null);
        PvrGlCache.Instance.TexParameteri(PvrGl.Texture2D, PvrGl.TextureMinFilter, PvrGl.Linear);
        PvrGlCache.Instance.TexParameteri(PvrGl.Texture2D, PvrGl.TextureMagFilter, PvrGl.Linear);
        PvrGlCache.Instance.TexParameteri(PvrGl.Texture2D, PvrGl.TextureWrapS, PvrGl.ClampToEdge);
        PvrGlCache.Instance.TexParameteri(PvrGl.Texture2D, PvrGl.TextureWrapT, PvrGl.ClampToEdge);
    }

    private void MakeColorBuffer()
    {
        fixed (uint* buffer = &_colorBuffer)
            PvrGl.GenRenderbuffers(1, buffer);
        PvrGl.BindRenderbuffer(PvrGl.Renderbuffer, _colorBuffer);
        PvrGl.RenderbufferStorage(PvrGl.Renderbuffer, PvrGles.Rgba8, _width, _height);
    }

    public void Dispose()
    {
        fixed (uint* framebuffer = &_framebuffer, depth = &_depthBuffer, texture = &_texture, color = &_colorBuffer)
        {
            try
            {
                PvrGl.DeleteFramebuffers(1, framebuffer);
                PvrGl.DeleteRenderbuffers(1, depth);
                PvrGlCache.Instance.DeleteTextures(1, texture);
            }
            finally
            {
                PvrGl.DeleteRenderbuffers(1, color);
            }
        }
        _framebuffer = 0;
        _depthBuffer = 0;
        _texture = 0;
        _colorBuffer = 0;
    }
}
