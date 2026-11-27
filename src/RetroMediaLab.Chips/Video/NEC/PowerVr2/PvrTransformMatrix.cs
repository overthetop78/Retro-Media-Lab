using System.Diagnostics;
using System.Numerics;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2;

internal sealed partial class PvrTransformMatrix
{
    private static void GetTaViewport(RenderContext context, out int width, out int height)
    {
        width = context.GlobClip.X;
        height = context.GlobClip.Y;
    }

    private static void MakePowerOfTwoSize(int width, int height, out int widthPowerOfTwo, out int heightPowerOfTwo)
    {
        widthPowerOfTwo = 8;
        heightPowerOfTwo = 8;
        for (int i = 0; i < 8; i++)
        {
            if (widthPowerOfTwo < width)
                widthPowerOfTwo *= 2;
            if (heightPowerOfTwo < height)
                heightPowerOfTwo *= 2;
        }
    }

    internal static partial void GetPvrFramebufferSize(RenderContext context, out int width, out int height)
    {
        if (!context.IsRtt)
        {
            GetTaViewport(context, out width, out height);
            if (!PvrConfiguration.EmulateFramebuffer)
            {
                int maxHeight = PvrRegisters.FB_R_CTRL.vclk_div == 0 && PvrRegisters.SPG_CONTROL.interlace == 0 ? 240 : 480;
                if (context.ScalerCtl.vscalefactor != 0
                    && (context.ScalerCtl.vscalefactor > 1025 || context.ScalerCtl.vscalefactor < 1024)
                    && PvrRegisters.SPG_CONTROL.interlace == 0)
                    maxHeight = (int)(maxHeight / (1024f / context.ScalerCtl.vscalefactor));
                if (PvrRegisters.FB_R_CTRL.fb_line_double != 0)
                    maxHeight /= 2;
                height = Math.Min(maxHeight, height);
            }
        }
        else
        {
            if (PvrConfiguration.RenderToTextureBuffer)
            {
                width = context.TileClip.BottomRight().X + 1;
                height = context.TileClip.BottomRight().Y + 1;
            }
            else
            {
                PvrRect clip = Intersect(context.TileClip, context.FbClip);
                MakePowerOfTwoSize(clip.BottomRight().X + 1, clip.BottomRight().Y + 1, out width, out height);
            }
        }
    }

    private partial bool IsClipped()
    {
        GetTaViewport(_renderingContext, out int width, out _);
        PvrRect clip = IntersectTileAndFramebufferScissor();
        return clip.Origin.X > 0 || clip.BottomRight().X < width - 1;
    }

    internal partial void CalcMatrices(RenderContext renderingContext, int width, int height)
    {
        int flipY = _directX ? -1 : 1;
        var renderViewport = new Vector2(width == 0 ? PvrRuntime.DisplayWidth : width,
            height == 0 ? PvrRuntime.DisplayHeight : height);

        _renderingContext = renderingContext;
        GetPvrFramebufferSize(renderingContext, out int framebufferWidth, out int framebufferHeight);
        var dcViewport = new Vector2(framebufferWidth, framebufferHeight);

        if (renderingContext.IsRtt)
        {
            _normalMatrix = Matrix4x4.CreateScale(2f / dcViewport.X, 2f / dcViewport.Y * flipY, 1f)
                * Matrix4x4.CreateTranslation(-1, -flipY, 0);
        }
        else
        {
            float widescreenShift;
            if (PvrConfiguration.Widescreen && !PvrConfiguration.Rotate90 && !PvrConfiguration.EmulateFramebuffer)
            {
                widescreenShift = 1f - dcViewport.X / dcViewport.Y * renderViewport.Y / renderViewport.X;
                if (PvrConfiguration.SuperWidescreen)
                    dcViewport.X *= (float)PvrRuntime.DisplayWidth / PvrRuntime.DisplayHeight / 4f * 3f;
                else
                    dcViewport.X *= 4f / 3f;
            }
            else
                widescreenShift = 0;

            Matrix4x4 translation = Matrix4x4.CreateTranslation(-1 + widescreenShift, -flipY, 0);
            float xCoefficient = 2f / dcViewport.X;
            float yCoefficient = 2f / dcViewport.Y * flipY;
            _normalMatrix = Matrix4x4.CreateScale(xCoefficient, yCoefficient, 1f) * translation;
        }

        _normalMatrix *= Matrix4x4.CreateScale(1, 1, 1f / PvrConfiguration.ExtraDepthScale);
        Matrix4x4 viewportTranslation = Matrix4x4.CreateTranslation(1, flipY, 0);
        viewportTranslation *= Matrix4x4.CreateScale(renderViewport.X / 2, renderViewport.Y / 2 * flipY, 1f);
        _viewportMatrix = _normalMatrix * viewportTranslation;
    }

    private partial PvrRect IntersectTileAndFramebufferScissor()
    {
        PvrInt2 bottomRight = _renderingContext.FbClip.Origin + _renderingContext.FbClip.Size;
        GetScissorScaling(out float xScale, out float yScale);
        PvrInt2 origin = Max(new PvrInt2((int)Round(_renderingContext.FbClip.Origin.X * xScale),
            (int)Round(_renderingContext.FbClip.Origin.Y * yScale)), _renderingContext.TileClip.Origin);
        bottomRight = Min(new PvrInt2((int)Round(bottomRight.X * xScale), (int)Round(bottomRight.Y * yScale)),
            _renderingContext.TileClip.Origin + _renderingContext.TileClip.Size);
        return new PvrRect(origin, Max(bottomRight - origin, new PvrInt2(0, 0)));
    }

    internal partial PvrRect GetBaseScissor()
    {
        bool widescreen = !_renderingContext.IsRtt && PvrConfiguration.Widescreen
            && !IsClipped() && !PvrConfiguration.Rotate90 && !PvrConfiguration.EmulateFramebuffer;
        if (widescreen)
            return new PvrRect(new PvrInt2(0, 0),
                new PvrInt2(_renderingContext.FramebufferWidth, _renderingContext.FramebufferHeight));

        Vector4 origin;
        Vector4 size;
        if ((!_renderingContext.IsRtt && PvrConfiguration.EmulateFramebuffer)
            || (_renderingContext.IsRtt && PvrConfiguration.RenderToTextureBuffer))
        {
            origin = new Vector4(_renderingContext.TileClip.Origin.X, _renderingContext.TileClip.Origin.Y, 0, 1);
            size = new Vector4(_renderingContext.TileClip.Size.X, _renderingContext.TileClip.Size.Y, 0, 0);
        }
        else
        {
            PvrRect rect = IntersectTileAndFramebufferScissor();
            origin = new Vector4(rect.Origin.X, rect.Origin.Y, 0, 1);
            size = new Vector4(rect.Size.X, rect.Size.Y, 0, 0);
        }
        origin = Vector4.Transform(origin, _viewportMatrix);
        size = Vector4.Transform(size, _viewportMatrix);

        if (size.X < 0)
        {
            origin.X += size.X;
            size.X = -size.X;
        }
        if (size.Y < 0)
        {
            origin.Y += size.Y;
            size.Y = -size.Y;
        }

        return new PvrRect(new PvrInt2((int)MathF.Max(Round(origin.X), 0), (int)MathF.Max(Round(origin.Y), 0)),
            new PvrInt2((int)MathF.Max(Round(size.X), 0), (int)MathF.Max(Round(size.Y), 0)));
    }

    private partial void GetScissorScaling(out float scaleX, out float scaleY)
    {
        scaleX = 1f;
        scaleY = 1f;
        if ((!_renderingContext.IsRtt && !PvrConfiguration.EmulateFramebuffer)
            || (_renderingContext.IsRtt && !PvrConfiguration.RenderToTextureBuffer))
        {
            if (_renderingContext.ScalerCtl.vscalefactor < 1024 || _renderingContext.ScalerCtl.vscalefactor > 1025)
                scaleY *= _renderingContext.ScalerCtl.vscalefactor / 1024f;
            if (_renderingContext.ScalerCtl.hscale == 1)
                scaleX *= 2f;
        }
    }

    internal partial TileClipping GetTileClip(uint value, ref PvrRect clipRect)
    {
        if (!PvrConfiguration.Clipping)
            return TileClipping.Off;

        uint clipMode = value >> 28;
        if (clipMode < 2)
            return TileClipping.Off;

        TileClipping mode;
        if ((clipMode & 1) != 0)
            mode = TileClipping.Inside;
        else
            mode = TileClipping.Outside;

        float startX = value & 63;
        float endX = (value >> 6) & 63;
        float startY = (value >> 12) & 31;
        float endY = (value >> 17) & 31;
        startX *= 32;
        endX = (endX + 1) * 32;
        startY *= 32;
        endY = (endY + 1) * 32;

        if (startX == 0 && startY == 0 && endX >= _renderingContext.GlobClip.X && endY >= _renderingContext.GlobClip.Y)
            return TileClipping.Off;

        var clipStart = new Vector4(startX, startY, 0, 1);
        var clipEnd = new Vector4(endX, endY, 0, 1);
        clipStart = Vector4.Transform(clipStart, _viewportMatrix);
        clipEnd = Vector4.Transform(clipEnd, _viewportMatrix);
        clipRect = new PvrRect(new PvrInt2((int)clipStart.X, (int)clipStart.Y),
            new PvrInt2((int)(clipEnd.X - clipStart.X), (int)(clipEnd.Y - clipStart.Y)));
        clipRect.Size.X++;
        clipRect.Size.Y = Math.Abs(clipRect.Size.Y) + 1;
        if (mode == TileClipping.Outside)
        {
            PvrRect baseScissor = GetBaseScissor();
            clipRect = Intersect(clipRect, baseScissor);
        }
        startX = clipStart.X;
        startY = clipStart.Y;
        endY = clipEnd.Y;
        endX = clipEnd.X;
        clipRect = new PvrRect(new PvrInt2(Math.Max(0, (int)Round(startX)), Math.Max(0, (int)Round(MathF.Min(startY, endY)))),
            new PvrInt2(Math.Max(0, (int)Round(endX - startX)), Math.Max(0, (int)Round(MathF.Abs(endY - startY)))));
        return mode;
    }

    internal static partial void GetScaledFramebufferSize(RenderContext context, out int width, out int height)
    {
        GetPvrFramebufferSize(context, out width, out height);
        if (!context.IsRtt)
        {
            if (!PvrConfiguration.EmulateFramebuffer)
            {
                float upscaling = PvrConfiguration.RenderResolution / 480f;
                float scaledWidth = width * upscaling;
                float scaledHeight = height * upscaling;
                if (PvrConfiguration.Widescreen && !PvrConfiguration.Rotate90)
                {
                    if (PvrConfiguration.SuperWidescreen)
                        scaledWidth *= (float)PvrRuntime.DisplayWidth / PvrRuntime.DisplayHeight / 4f * 3f;
                    else
                        scaledWidth *= 4f / 3f;
                }
                if (!PvrConfiguration.Rotate90)
                    scaledWidth = Round(scaledWidth / 2f) * 2f;
                scaledHeight = Round(scaledHeight);
                width = (int)scaledWidth;
                height = (int)scaledHeight;
            }
        }
        else
        {
            if (!PvrConfiguration.RenderToTextureBuffer)
            {
                if (!PvrConfiguration.EmulateFramebuffer)
                {
                    float upscaling = PvrConfiguration.RenderResolution / 480f;
                    float scaledWidth = width * upscaling;
                    float scaledHeight = height * upscaling;
                    width = (int)Round(scaledWidth);
                    height = (int)Round(scaledHeight);
                }
            }
            else
            {
                if (context.ScalerCtl.vscalefactor < 1024 || context.ScalerCtl.vscalefactor > 1025)
                    height = height * 1024 / (int)context.ScalerCtl.vscalefactor;
                if (context.ScalerCtl.hscale == 1)
                    width /= 2;
            }
        }
    }

    internal static partial float GetOutputFramebufferAspectRatio()
    {
        float aspectRatio;
        if (PvrConfiguration.Rotate90)
            aspectRatio = 3f / 4f;
        else
        {
            if (PvrConfiguration.Widescreen && !PvrConfiguration.EmulateFramebuffer)
            {
                if (PvrConfiguration.SuperWidescreen)
                    aspectRatio = (float)PvrRuntime.DisplayWidth / PvrRuntime.DisplayHeight;
                else
                    aspectRatio = 16f / 9f;
            }
            else
                aspectRatio = 4f / 3f;
        }
        return aspectRatio * PvrConfiguration.ScreenStretching / 100f;
    }

    internal static partial void GetDcFramebufferReadSize(in PvrFramebufferInfo info, out int width, out int height)
    {
        width = ((int)info.ReadSize.fb_x_size + 1) * 2;
        height = (int)info.ReadSize.fb_y_size + 1;
        int modulus = ((int)info.ReadSize.fb_modulus - 1) * 2;

        int bytesPerPixel;
        switch ((PvrRegisters.fb_depth_enum)info.ReadControl.fb_depth)
        {
            case PvrRegisters.fb_depth_enum.fbde_0555:
            case PvrRegisters.fb_depth_enum.fbde_565:
                bytesPerPixel = 2;
                break;
            case PvrRegisters.fb_depth_enum.fbde_888:
                bytesPerPixel = 3;
                width = width * 2 / 3;
                modulus = modulus * 2 / 3;
                break;
            case PvrRegisters.fb_depth_enum.fbde_C888:
                bytesPerPixel = 4;
                width /= 2;
                modulus /= 2;
                break;
            default:
                bytesPerPixel = 2;
                break;
        }

        if (info.SpgControl.interlace != 0 && width == modulus
            && info.ReadStart2 == unchecked(info.ReadStart1 + (uint)(width * bytesPerPixel)))
            height *= 2;
    }

    internal static partial void GetWindowboxDimensions(int outWidth, int outHeight, float renderAspect, ref int dx, ref int dy, bool rotate)
    {
        if (outWidth == 0 || outHeight == 0 || renderAspect == 0f)
        {
            dx = dy = 0;
            return;
        }
        if (PvrConfiguration.IntegerScale)
        {
            int framebufferHeight = PvrConfiguration.RenderResolution;
            if (framebufferHeight == 0)
            {
                dx = dy = 0;
                return;
            }
            int framebufferWidth = (int)((rotate ? 1 / renderAspect : renderAspect) * framebufferHeight);
            if (rotate)
                (framebufferWidth, framebufferHeight) = (framebufferHeight, framebufferWidth);

            int scale = Math.Min(outWidth / framebufferWidth, outHeight / framebufferHeight);
            if (scale == 0)
            {
                scale = Math.Max(framebufferWidth / outWidth, framebufferHeight / outHeight) + 1;
                dx = (outWidth - framebufferWidth / scale) / 2;
                dy = (outHeight - framebufferHeight / scale) / 2;
            }
            else
            {
                dx = (outWidth - framebufferWidth * scale) / 2;
                dy = (outHeight - framebufferHeight * scale) / 2;
            }
        }
        else
        {
            float screenAspect = (float)outWidth / outHeight;
            if (renderAspect > screenAspect)
                dy = (int)Round(outHeight * (1 - screenAspect / renderAspect) / 2f);
            else
                dx = (int)Round(outWidth * (1 - renderAspect / screenAspect) / 2f);
        }
    }

    internal static partial void GetVideoShift(out float x, out float y)
    {
        bool vga = PvrRegisters.FB_R_CTRL.vclk_div == 1;
        switch (PvrRegisters.SPG_LOAD.hcount)
        {
            case 857:
                x = (int)PvrRegisters.VO_STARTX.HStart - (vga ? 0xa8 : 0xa4);
                break;
            case 863:
                x = (int)PvrRegisters.VO_STARTX.HStart - 0xae;
                break;
            case 851:
            case 850:
                x = (int)PvrRegisters.VO_STARTX.HStart - 0xa5;
                break;
            default:
                x = 0;
                Trace.TraceInformation($"unknown video mode: hcount {PvrRegisters.SPG_LOAD.hcount}");
                break;
        }

        switch (PvrRegisters.SPG_LOAD.vcount)
        {
            case 524:
                y = (int)PvrRegisters.VO_STARTY.VStart_field1 - (vga ? 0x28 : 0x12);
                break;
            case 262:
                y = (int)PvrRegisters.VO_STARTY.VStart_field1 - 0x11;
                break;
            case 624:
                y = (int)PvrRegisters.VO_STARTY.VStart_field1 - 0x2d;
                break;
            case 312:
                y = (int)PvrRegisters.VO_STARTY.VStart_field1 - 0x2e;
                break;
            case 529:
            case 528:
                y = (int)PvrRegisters.VO_STARTY.VStart_field1 - 0x24;
                break;
            case 536:
            case 268:
                y = (int)PvrRegisters.VO_STARTY.VStart_field1 - 0x17;
                break;
            default:
                y = 0;
                Trace.TraceInformation($"unknown video mode: vcount {PvrRegisters.SPG_LOAD.vcount}");
                break;
        }

        if (!PvrConfiguration.EmulateFramebuffer)
        {
            x *= PvrConfiguration.RenderResolution / 480f;
            y *= PvrConfiguration.RenderResolution / 480f;
        }
        x *= PvrConfiguration.ScreenStretching / 100f;
    }

    internal static partial void GetWriteFramebufferToVramParams(RenderContext context, out PvrInt2 scaledSize, out PvrRect finalClip)
    {
        Vector2 scale;
        scale.X = context.ScalerCtl.hscale == 1 ? 0.5f : 1f;
        if (context.ScalerCtl.vscalefactor != 0 && context.ScalerCtl.vscalefactor != 1024
            && context.ScalerCtl.vscalefactor != 1025)
            scale.Y = 1024f / context.ScalerCtl.vscalefactor;
        else
            scale.Y = 1;

        scaledSize = new PvrInt2((int)(context.GlobClip.X * scale.X), (int)(context.GlobClip.Y * scale.Y));
        finalClip = new PvrRect(new PvrInt2((int)(context.TileClip.Origin.X * scale.X), (int)(context.TileClip.Origin.Y * scale.Y)),
            new PvrInt2((int)(context.TileClip.Size.X * scale.X), (int)(context.TileClip.Size.Y * scale.Y)));
        finalClip = Intersect(finalClip, context.FbClip);
    }

    private static float Round(float value) => MathF.Round(value, MidpointRounding.AwayFromZero);
}
