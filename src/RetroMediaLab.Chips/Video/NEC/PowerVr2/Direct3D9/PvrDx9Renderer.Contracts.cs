using System;
using SharpGen.Runtime;
using Vortice.Direct3D9;
using Vortice.Mathematics;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Direct3D9;

internal sealed class PvrDx9RenderStateCache
{
    private IDirect3DDevice9? _device;
    private readonly uint[] _renderState = new uint[210];
    private readonly uint[] _sampler0State = new uint[14];
    private IDirect3DVertexShader9? _vertexShader;
    private IDirect3DPixelShader9? _pixelShader;
    private IDirect3DBaseTexture9? _texture;

    internal void SetDevice(IDirect3DDevice9 device)
    {
        _device = device;
        Reset();
    }

    internal void Reset()
    {
        Array.Fill(_renderState, 0xfefefefeu);
        Array.Fill(_sampler0State, 0xfefefefeu);
        _vertexShader = null;
        _pixelShader = null;
        _texture = null;
    }

    internal Result SetRenderState(RenderState state, uint value)
    {
        if ((uint)state < _renderState.Length)
        {
            if (_renderState[(int)state] == value)
                return Result.Ok;
            _renderState[(int)state] = value;
        }
        return _device!.SetRenderState(state, value);
    }

    internal Result SetSamplerState(uint sampler, SamplerState type, uint value)
    {
        if (sampler == 0 && (uint)type < _sampler0State.Length)
        {
            if (_sampler0State[(int)type] == value)
                return Result.Ok;
            _sampler0State[(int)type] = value;
        }
        return _device!.SetSamplerState(sampler, type, value);
    }

    internal Result SetVertexShader(IDirect3DVertexShader9? shader)
    {
        if ((shader?.NativePointer ?? 0) == (_vertexShader?.NativePointer ?? 0))
            return Result.Ok;
        _vertexShader = shader;
        return _device!.SetVertexShader(shader);
    }

    internal Result SetPixelShader(IDirect3DPixelShader9? shader)
    {
        if ((shader?.NativePointer ?? 0) == (_pixelShader?.NativePointer ?? 0))
            return Result.Ok;
        _pixelShader = shader;
        return _device!.SetPixelShader(shader);
    }

    internal Result SetTexture(uint stage, IDirect3DBaseTexture9? texture)
    {
        if (stage == 0)
        {
            if ((texture?.NativePointer ?? 0) == (_texture?.NativePointer ?? 0))
                return Result.Ok;
            _texture = texture;
        }
        return _device!.SetTexture(stage, texture);
    }
}

internal sealed unsafe partial class PvrDx9Renderer : PvrRenderBackend
{
    internal override bool Present()
    {
        if (!_frameRendered || ClearLastFrame)
            return false;
        PvrDx9Context.Instance.ImGuiDriver!.SetFrameRendered();
        _frameRendered = false;
        return true;
    }

    private enum ModifierVolumeMode { Xor, Or, Inclusion, Exclusion, ModeCount }

    private readonly PvrDx9RenderStateCache _devCache = new();
    private IDirect3DDevice9? _device;
    private IDirect3DVertexBuffer9? _vertexBuffer;
    private uint _vertexBufferSize;
    private IDirect3DVertexBuffer9? _modvolBuffer;
    private uint _modvolBufferSize;
    private IDirect3DIndexBuffer9? _indexBuffer;
    private uint _indexBufferSize;
    private IDirect3DVertexDeclaration9? _mainVtxDecl;
    private IDirect3DVertexDeclaration9? _modVolVtxDecl;

    private IDirect3DTexture9? _framebufferTexture;
    private IDirect3DSurface9? _framebufferSurface;
    private IDirect3DSurface9? _backbuffer;
    private IDirect3DTexture9? _paletteTexture;
    private IDirect3DTexture9? _fogTexture;
    private IDirect3DTexture9? _dcfbTexture;
    private IDirect3DSurface9? _dcfbSurface;
    private IDirect3DTexture9? _rttTexture;
    private IDirect3DSurface9? _rttSurface;
    private IDirect3DSurface9? _depthSurface;
    private IDirect3DTexture9? _fbScaledTexture;
    private IDirect3DSurface9? _fbScaledSurface;

    private uint _width;
    private uint _height;
    private readonly PvrTransformMatrix _matrices = new(true);
    private readonly PvrDx9TextureCache _texCache = new();
    private readonly PvrDx9Shaders _shaders = new();
    private RawRect _scissorRect;
    private bool _scissorEnable;
    private bool _frameRendered;
    private bool _frameRenderedOnce;
    private int _maxAnisotropy = 1;
    private float _aspectRatio = 4f / 3f;
    private bool _dithering;
    private RenderContext _rendContext = null!;
}
