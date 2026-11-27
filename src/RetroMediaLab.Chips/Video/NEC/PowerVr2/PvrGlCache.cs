namespace RetroMediaLab.Chips.Video.NEC.PowerVr2;

internal sealed unsafe class PvrGlCache
{
    private const int TextureIdCacheSize = 32;

    internal PvrGlCache()
    {
        Reset();
    }

    internal void BindTexture(uint target, uint texture)
    {
        if (target == PvrGl.Texture2D && !_disableCache)
        {
            if (texture != _texture)
            {
                PvrGl.BindTexture(target, texture);
                _texture = texture;
            }
        }
        else
        {
            PvrGl.BindTexture(target, texture);
        }
    }

    internal void BlendFunc(uint sfactor, uint dfactor)
    {
        if (sfactor != _srcBlendFactor || dfactor != _dstBlendFactor || _disableCache)
        {
            _srcBlendFactor = sfactor;
            _dstBlendFactor = dfactor;
            PvrGl.BlendFunc(sfactor, dfactor);
        }
    }

    internal void ClearColor(float red, float green, float blue, float alpha)
    {
        if (red != _clearR || green != _clearG || blue != _clearB || alpha != _clearA || _disableCache)
        {
            _clearR = red;
            _clearG = green;
            _clearB = blue;
            _clearA = alpha;
            PvrGl.ClearColor(red, green, blue, alpha);
        }
    }

    internal void CullFace(uint mode)
    {
        if (mode != _cullFace || _disableCache)
        {
            _cullFace = mode;
            PvrGl.CullFace(mode);
        }
    }

    internal void DeleteTextures(int count, uint* textures)
    {
        for (int i = 0; i < count; i++)
        {
            _textureParams.Remove(textures[i]);
            if (textures[i] == _texture)
                _texture = 0;
        }
        PvrGl.DeleteTextures(count, textures);
    }

    internal void DepthFunc(uint func)
    {
        if (func != _depthFunc || _disableCache)
        {
            _depthFunc = func;
            PvrGl.DepthFunc(func);
        }
    }

    internal void DepthMask(byte flag)
    {
        if (flag != _depthMask || _disableCache)
        {
            _depthMask = flag;
            PvrGl.DepthMask(flag);
        }
    }

    internal void Enable(uint capability)
    {
        SetCapability(capability, 1);
    }

    internal void Disable(uint capability)
    {
        SetCapability(capability, 0);
    }

    internal void UseProgram(uint program)
    {
        if (program != _program || _disableCache)
        {
            _program = program;
            PvrGl.UseProgram(program);
        }
    }

    internal void StencilFunc(uint func, int reference, uint mask)
    {
        if (_stencilFunc != func || _stencilRef != reference || _stencilFmask != mask || _disableCache)
        {
            _stencilFunc = func;
            _stencilRef = reference;
            _stencilFmask = mask;
            PvrGl.StencilFunc(func, reference, mask);
        }
    }

    internal void StencilOp(uint sfail, uint dpfail, uint dppass)
    {
        if (_stencilSfail != sfail || _stencilDpfail != dpfail || _stencilDppass != dppass || _disableCache)
        {
            _stencilSfail = sfail;
            _stencilDpfail = dpfail;
            _stencilDppass = dppass;
            PvrGl.StencilOp(sfail, dpfail, dppass);
        }
    }

    internal void StencilMask(uint mask)
    {
        if (_stencilMask != mask || _disableCache)
        {
            _stencilMask = mask;
            PvrGl.StencilMask(mask);
        }
    }

    internal void Scissor(int x, int y, int width, int height)
    {
        if (x != _scissor.X || y != _scissor.Y || width != _scissor.Width || height != _scissor.Height || _disableCache)
        {
            _scissor.X = x;
            _scissor.Y = y;
            _scissor.Width = width;
            _scissor.Height = height;
            PvrGl.Scissor(x, y, width, height);
        }
    }

    internal void TexParameteri(uint target, uint pname, int param)
    {
        if (target == PvrGl.Texture2D && !_disableCache)
        {
            if (!_textureParams.TryGetValue(_texture, out TextureParameters? current))
                _textureParams.Add(_texture, current = new TextureParameters());

            switch (pname)
            {
                case PvrGl.TextureMinFilter:
                    if (current.MinFilter == unchecked((uint)param))
                        return;
                    current.MinFilter = unchecked((uint)param);
                    break;
                case PvrGl.TextureMagFilter:
                    if (current.MagFilter == unchecked((uint)param))
                        return;
                    current.MagFilter = unchecked((uint)param);
                    break;
                case PvrGl.TextureWrapS:
                    if (current.WrapS == unchecked((uint)param))
                        return;
                    current.WrapS = unchecked((uint)param);
                    break;
                case PvrGl.TextureWrapT:
                    if (current.WrapT == unchecked((uint)param))
                        return;
                    current.WrapT = unchecked((uint)param);
                    break;
            }
        }
        PvrGl.TexParameteri(target, pname, param);
    }

    internal uint GenTexture()
    {
        if (_textureCacheSize == 0)
        {
            _textureCacheSize = TextureIdCacheSize;
            fixed (uint* ids = _textureIds)
                PvrGl.GenTextures(_textureCacheSize, ids);
        }
        return _textureIds[--_textureCacheSize];
    }

    internal void DeleteProgram(uint program)
    {
        if (program == 0)
            return;
        PvrGl.DeleteProgram(program);
        if (_program == program)
            _program = 0;
    }

    internal void Reset()
    {
        _texture = uint.MaxValue;
        _textureParams.Clear();
        _srcBlendFactor = uint.MaxValue;
        _dstBlendFactor = uint.MaxValue;
        _clearR = -1f;
        _clearG = -1f;
        _clearB = -1f;
        _clearA = -1f;
        _enBlend = byte.MaxValue;
        _enCullFace = byte.MaxValue;
        _enDepthTest = byte.MaxValue;
        _enScissorTest = byte.MaxValue;
        _enStencilTest = byte.MaxValue;
        _cullFace = uint.MaxValue;
        _depthFunc = uint.MaxValue;
        _depthMask = byte.MaxValue;
        _program = uint.MaxValue;
        _stencilFunc = uint.MaxValue;
        _stencilRef = -1;
        _stencilFmask = 0;
        _stencilSfail = uint.MaxValue;
        _stencilDpfail = uint.MaxValue;
        _stencilDppass = uint.MaxValue;
        _stencilMask = 0;
        _scissor.X = -1;
        _scissor.Y = -1;
        _scissor.Width = -1;
        _scissor.Height = -1;
        if (_textureCacheSize > 0)
        {
            fixed (uint* ids = _textureIds)
                PvrGl.DeleteTextures(_textureCacheSize, ids);
            _textureCacheSize = 0;
        }
    }

    internal void DisableCache()
    {
        _disableCache = true;
        Reset();
    }

    internal void EnableCache()
    {
        _disableCache = false;
        Reset();
    }

    private sealed class TextureParameters
    {
        internal uint MinFilter = uint.MaxValue;
        internal uint MagFilter = uint.MaxValue;
        internal uint WrapS = uint.MaxValue;
        internal uint WrapT = uint.MaxValue;
    }

    private void SetCapability(uint capability, byte value)
    {
        fixed (byte* enBlend = &_enBlend, enCullFace = &_enCullFace, enDepthTest = &_enDepthTest,
            enScissorTest = &_enScissorTest, enStencilTest = &_enStencilTest)
        {
            byte* state = capability switch
            {
                PvrGl.Blend => enBlend,
                PvrGl.CullFaceMode => enCullFace,
                PvrGl.DepthTest => enDepthTest,
                PvrGl.ScissorTest => enScissorTest,
                PvrGl.StencilTest => enStencilTest,
                _ => null
            };
            if (state != null)
            {
                if (*state == value && !_disableCache)
                    return;
                *state = value;
            }
        }

        switch (capability)
        {
            case PvrGl.Blend:
                if (value != 0)
                    PvrGl.Enable(PvrGl.Blend);
                else
                    PvrGl.Disable(PvrGl.Blend);
                break;
            case PvrGl.CullFaceMode:
                if (value != 0)
                    PvrGl.Enable(PvrGl.CullFaceMode);
                else
                    PvrGl.Disable(PvrGl.CullFaceMode);
                break;
            case PvrGl.DepthTest:
                if (value != 0)
                    PvrGl.Enable(PvrGl.DepthTest);
                else
                    PvrGl.Disable(PvrGl.DepthTest);
                break;
            case PvrGl.ScissorTest:
                if (value != 0)
                    PvrGl.Enable(PvrGl.ScissorTest);
                else
                    PvrGl.Disable(PvrGl.ScissorTest);
                break;
            case PvrGl.StencilTest:
                if (value != 0)
                    PvrGl.Enable(PvrGl.StencilTest);
                else
                    PvrGl.Disable(PvrGl.StencilTest);
                break;
        }
    }

    private uint _texture;
    private uint _srcBlendFactor;
    private uint _dstBlendFactor;
    private float _clearR;
    private float _clearG;
    private float _clearB;
    private float _clearA;
    private byte _enBlend;
    private byte _enCullFace;
    private byte _enDepthTest;
    private byte _enScissorTest;
    private byte _enStencilTest;
    private uint _cullFace;
    private uint _depthFunc;
    private byte _depthMask;
    private uint _program;
    private uint _stencilFunc;
    private int _stencilRef;
    private uint _stencilFmask;
    private uint _stencilSfail;
    private uint _stencilDpfail;
    private uint _stencilDppass;
    private uint _stencilMask;
    private (int X, int Y, int Width, int Height) _scissor;
    private readonly uint[] _textureIds = new uint[TextureIdCacheSize];
    private int _textureCacheSize;
    private readonly Dictionary<uint, TextureParameters> _textureParams = [];
    private bool _disableCache;

    internal static readonly PvrGlCache Instance = new();
}
