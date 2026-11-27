namespace RetroMediaLab.Chips.Video.NEC.PowerVr2;

internal sealed unsafe partial class PvrOpenGlDriver
{
    private const uint TextureBorderColor = 0x1004;
    private const int ClampToBorder = 0x812d;

    internal PvrOpenGlDriver()
    {
        PvrImGuiOpenGl3.Init();
    }

    public override void Dispose()
    {
        try
        {
            var textureIds = new List<uint>(_textures.Count);
            foreach (var entry in _textures)
                textureIds.Add(unchecked((uint)(nuint)entry.Value));
            if (textureIds.Count != 0)
                fixed (uint* ids = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(textureIds))
                    PvrGlCache.Instance.DeleteTextures(textureIds.Count, ids);
        }
        finally
        {
            _textures.Clear();
            PvrImGuiOpenGl3.Shutdown();
        }
    }

    internal override void NewFrame()
    {
        PvrImGuiOpenGl3.NewFrame();
    }

    internal override void RenderDrawData(nint drawData, bool guiOpen)
    {
        if (guiOpen)
        {
            PvrGl.BindFramebuffer(PvrGl.Framebuffer, 0);
            PvrGlCache.Instance.Disable(PvrGl.ScissorTest);
            PvrGlCache.Instance.ClearColor(0, 0, 0, 0);
            PvrGl.Clear(PvrGl.ColorBufferBit);
            PvrRenderer.Backend?.RenderLastFrame();
        }
        PvrImGuiOpenGl3.RenderDrawData(drawData);
        if (guiOpen)
            _frameRendered = true;
    }

    internal override void Present()
    {
        if (_frameRendered)
            PvrGlGraphicsContext.Instance.Swap();
        _frameRendered = false;
    }

    internal override nint UpdateTexture(string name, byte* data, int width, int height, bool nearestSampling)
    {
        uint oldId = unchecked((uint)(nuint)GetTexture(name));
        if (oldId != 0)
            PvrGlCache.Instance.DeleteTextures(1, &oldId);
        PvrGlCache cache = PvrGlCache.Instance;
        uint texture = cache.GenTexture();
        cache.BindTexture(PvrGl.Texture2D, texture);
        if (nearestSampling)
        {
            cache.TexParameteri(PvrGl.Texture2D, PvrGl.TextureMinFilter, PvrGl.Nearest);
            cache.TexParameteri(PvrGl.Texture2D, PvrGl.TextureMagFilter, PvrGl.Nearest);
        }
        else
        {
            cache.TexParameteri(PvrGl.Texture2D, PvrGl.TextureMinFilter, PvrGl.Linear);
            cache.TexParameteri(PvrGl.Texture2D, PvrGl.TextureMagFilter, PvrGl.Linear);
        }
        if (PvrGles.Gl.BorderClampSupported)
        {
            float* color = stackalloc float[4] { 0, 0, 0, 0 };
            PvrGl.TexParameterfv(PvrGl.Texture2D, TextureBorderColor, color);
            cache.TexParameteri(PvrGl.Texture2D, PvrGl.TextureWrapS, ClampToBorder);
            cache.TexParameteri(PvrGl.Texture2D, PvrGl.TextureWrapT, ClampToBorder);
        }
        else
        {
            cache.TexParameteri(PvrGl.Texture2D, PvrGl.TextureWrapS, PvrGl.ClampToEdge);
            cache.TexParameteri(PvrGl.Texture2D, PvrGl.TextureWrapT, PvrGl.ClampToEdge);
        }
        PvrGl.TexImage2D(PvrGl.Texture2D, 0, PvrGl.Rgba, width, height, 0, PvrGl.Rgba, PvrGl.UnsignedByte, data);

        return _textures[name] = unchecked((nint)(nuint)texture);
    }

    internal override void DeleteTexture(string name)
    {
        if (_textures.TryGetValue(name, out nint value))
        {
            uint id = unchecked((uint)(nuint)value);
            PvrGlCache.Instance.DeleteTextures(1, &id);
            _textures.Remove(name);
        }
    }
}
