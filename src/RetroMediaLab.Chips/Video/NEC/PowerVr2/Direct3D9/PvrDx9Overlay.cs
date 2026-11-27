using System;
using System.Numerics;
using Vortice.Direct3D9;
using Vortice.Mathematics;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Direct3D9;

internal sealed unsafe partial class PvrDx9Overlay
{
    private void DrawQuad(in RawRect rect, uint color)
    {
        IDirect3DDevice9 device = _device!;
        device.SetTextureStageState(0, TextureStageState.Constant, color);
        Vertex* quad = stackalloc Vertex[4]
        {
            new(rect.Left, rect.Top, 0.5f, 0f, 0f),
            new(rect.Left, rect.Bottom, 0.5f, 0f, 1f),
            new(rect.Right, rect.Top, 0.5f, 1f, 0f),
            new(rect.Right, rect.Bottom, 0.5f, 1f, 1f)
        };
        device.DrawPrimitiveUP(PrimitiveType.TriangleStrip, 2, (nint)quad, (uint)sizeof(Vertex));
    }

    internal void Draw(uint width, uint height, bool vmu, bool crosshair)
    {
        IDirect3DDevice9 device = _device!;
        SetupRenderState(width, height);
        if (vmu)
        {
            float vmuPadding = 8f * PvrRuntime.Display.UiScale;
            float vmuHeight = 70f * PvrRuntime.Display.UiScale;
            float vmuWidth = 48f / 32f * vmuHeight;
            for (int i = 0; i < _vmuTextures.Length; i++)
            {
                ref IDirect3DTexture9? texture = ref _vmuTextures[i];
                if (!PvrOsd.VmuLcdStatus[i])
                {
                    texture?.Dispose();
                    texture = null;
                    continue;
                }

                if (texture == null || _vmuLastChanged[i] != PvrOsd.VmuLastChanged[i])
                {
                    texture?.Dispose();
                    texture = null;
                    device.CreateTexture(48, 32, 1, Usage.Dynamic, Format.A8R8G8B8, Pool.Default, out texture, 0);
                    if (texture!.LockRect(0, out LockedRectangle locked, null, 0).Success)
                    {
                        try
                        {
                            byte* destination = (byte*)locked.Bits;
                            for (int y = 0; y < 32; y++)
                            {
                                fixed (uint* source = &PvrOsd.VmuLcdData[i, (31 - y) * 48])
                                    Buffer.MemoryCopy(source, destination + y * locked.Pitch, 48 * 4, 48 * 4);
                            }
                        }
                        finally
                        {
                            texture.UnlockRect(0);
                        }
                        _vmuLastChanged[i] = PvrOsd.VmuLastChanged[i];
                    }
                }

                float x;
                if ((i & 2) != 0)
                    x = width - vmuPadding - vmuWidth;
                else
                    x = vmuPadding;
                float y;
                if ((i & 4) != 0)
                {
                    y = height - vmuPadding - vmuHeight;
                    if ((i & 1) != 0)
                        y -= vmuPadding + vmuHeight;
                }
                else
                {
                    y = vmuPadding;
                    if ((i & 1) != 0)
                        y += vmuPadding + vmuHeight;
                }

                device.SetTexture(0, texture);
                var area = new RawRect((int)x, (int)y, (int)(x + vmuWidth), (int)(y + vmuHeight));
                DrawQuad(in area, 0xc0ffffffu);
            }
        }

        if (crosshair)
        {
            for (int i = 0; i < PvrConfiguration.CrosshairColor.Length; i++)
            {
                if (!PvrOsd.CrosshairNeeded(i))
                    continue;

                if (_xhairTexture == null)
                {
                    ReadOnlySpan<uint> texData = PvrOsd.GetCrosshairTextureData();
                    device.CreateTexture(16, 16, 1, 0, Format.A8R8G8B8, Pool.Managed, out _xhairTexture, 0);
                    if (_xhairTexture!.LockRect(0, out LockedRectangle locked, null, 0).Success)
                    {
                        try
                        {
                            fixed (uint* source = texData)
                            {
                                if (locked.Pitch == 16 * sizeof(uint))
                                    Buffer.MemoryCopy(source, (void*)locked.Bits, 16 * 16 * sizeof(uint), 16 * 16 * sizeof(uint));
                                else
                                {
                                    byte* destination = (byte*)locked.Bits;
                                    for (int y = 0; y < 16; y++)
                                        Buffer.MemoryCopy(source + y * 16, destination + y * locked.Pitch, 16 * sizeof(uint), 16 * sizeof(uint));
                                }
                            }
                        }
                        finally
                        {
                            _xhairTexture.UnlockRect(0);
                        }
                    }
                }
                device.SetTexture(0, _xhairTexture);
                (float x, float y) = PvrOsd.GetCrosshairPosition(i);
                float halfWidth = PvrConfiguration.CrosshairSize * PvrRuntime.Display.UiScale / 2f;
                var area = new RawRect((int)(x - halfWidth), (int)(y - halfWidth), (int)(x + halfWidth), (int)(y + halfWidth));
                uint argb = PvrConfiguration.CrosshairColor[i];
                uint color = (argb & 0xff00ff00u) | ((argb >> 16) & 0xff) | ((argb & 0xff) << 16);
                DrawQuad(in area, color);
            }
        }
    }

    private void SetupRenderState(uint displayWidth, uint displayHeight)
    {
        IDirect3DDevice9 device = _device!;
        device.SetPixelShader(null);
        device.SetVertexShader(null);
        device.SetRenderState(RenderState.CullMode, (uint)Cull.None);
        device.SetRenderState(RenderState.ZEnable, 0);
        device.SetRenderState(RenderState.AlphaBlendEnable, 1);
        device.SetRenderState(RenderState.AlphaTestEnable, 0);
        device.SetRenderState(RenderState.BlendOp, (uint)BlendOperation.Add);
        device.SetRenderState(RenderState.SrcBlend, (uint)Blend.SourceAlpha);
        device.SetRenderState(RenderState.DestBlend, (uint)Blend.InverseSourceAlpha);
        device.SetRenderState(RenderState.SeparateAlphaBlendEnable, 0);
        device.SetRenderState(RenderState.ScissorTestEnable, 0);
        device.SetRenderState(RenderState.ShadeMode, (uint)ShadeMode.Gouraud);
        device.SetTextureStageState(0, TextureStageState.ColorOp, (uint)TextureOperation.Modulate);
        device.SetTextureStageState(0, TextureStageState.ColorArg1, (uint)TextureArgument.Texture);
        device.SetTextureStageState(0, TextureStageState.ColorArg2, (uint)TextureArgument.Constant);
        device.SetTextureStageState(0, TextureStageState.AlphaOp, (uint)TextureOperation.Modulate);
        device.SetTextureStageState(0, TextureStageState.AlphaArg1, (uint)TextureArgument.Texture);
        device.SetTextureStageState(0, TextureStageState.AlphaArg2, (uint)TextureArgument.Constant);
        device.SetSamplerState(0, SamplerState.MinFilter, (uint)TextureFilter.Point);
        device.SetSamplerState(0, SamplerState.MagFilter, (uint)TextureFilter.Point);

        Matrix4x4 identity = Matrix4x4.Identity;
        Matrix4x4 projection = Matrix4x4.CreateScale(2f / displayWidth, -2f / displayHeight, 1f)
            * Matrix4x4.CreateTranslation(-1f - 1f / displayWidth, 1f + 1f / displayHeight, 0f);
        device.SetTransform(TransformState.World, in identity);
        device.SetTransform(TransformState.View, in identity);
        device.SetTransform(TransformState.Projection, in projection);
        device.SetFVF(FlexibleVertexFormat.Xyz | FlexibleVertexFormat.Tex1);
    }
}
