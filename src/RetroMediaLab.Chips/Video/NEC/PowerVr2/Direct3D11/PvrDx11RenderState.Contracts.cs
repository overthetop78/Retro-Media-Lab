using Vortice.Direct3D11;
using static RetroMediaLab.Chips.Video.NEC.PowerVr2.Direct3D11.PvrDx11RenderStates;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Direct3D11;

internal static class PvrDx11RenderStates
{
    internal static readonly ComparisonFunction[] Zfunction =
    [
        ComparisonFunction.Never,
        ComparisonFunction.Less,
        ComparisonFunction.Equal,
        ComparisonFunction.LessEqual,
        ComparisonFunction.Greater,
        ComparisonFunction.NotEqual,
        ComparisonFunction.GreaterEqual,
        ComparisonFunction.Always
    ];

    internal static readonly Blend[] DestBlend =
    [
        Blend.Zero,
        Blend.One,
        Blend.SourceColor,
        Blend.InverseSourceColor,
        Blend.SourceAlpha,
        Blend.InverseSourceAlpha,
        Blend.DestinationAlpha,
        Blend.InverseDestinationAlpha,
        Blend.InverseBlendFactor
    ];

    internal static readonly Blend[] SrcBlend =
    [
        Blend.Zero,
        Blend.One,
        Blend.DestinationColor,
        Blend.InverseDestinationColor,
        Blend.SourceAlpha,
        Blend.InverseSourceAlpha,
        Blend.DestinationAlpha,
        Blend.InverseDestinationAlpha,
        Blend.BlendFactor
    ];

    internal static readonly Blend[] DestBlendAlpha =
    [
        Blend.Zero,
        Blend.One,
        Blend.SourceAlpha,
        Blend.InverseSourceAlpha,
        Blend.SourceAlpha,
        Blend.InverseSourceAlpha,
        Blend.DestinationAlpha,
        Blend.InverseDestinationAlpha,
        Blend.InverseBlendFactor
    ];

    internal static readonly Blend[] SrcBlendAlpha =
    [
        Blend.Zero,
        Blend.One,
        Blend.DestinationAlpha,
        Blend.InverseDestinationAlpha,
        Blend.SourceAlpha,
        Blend.InverseSourceAlpha,
        Blend.DestinationAlpha,
        Blend.InverseDestinationAlpha,
        Blend.BlendFactor
    ];
}

internal sealed partial class PvrDx11DepthStencilStates
{
    internal enum ModifierVolumeMode
    {
        Xor,
        Or,
        Inclusion,
        Exclusion,
        Final,
        Count
    }

    internal ID3D11DepthStencilState? GetState(bool depth, bool depthWrite, int depthFunc, bool stencil)
    {
        int hash = (depthFunc << 3) | (depth ? 1 : 0) | ((depthWrite ? 1 : 0) << 1) | ((stencil ? 1 : 0) << 2);
        _states.TryGetValue(hash, out ID3D11DepthStencilState? state);
        if (state == null)
        {
            DepthStencilDescription desc = default;
            desc.DepthEnable = depth;
            desc.DepthWriteMask = depthWrite ? DepthWriteMask.All : DepthWriteMask.Zero;
            desc.DepthFunc = Zfunction[depthFunc];
            desc.StencilEnable = stencil;
            desc.FrontFace.StencilFailOp = desc.FrontFace.StencilDepthFailOp = StencilOperation.Keep;
            desc.FrontFace.StencilPassOp = StencilOperation.Replace;
            desc.FrontFace.StencilFunc = ComparisonFunction.Always;
            desc.BackFace = desc.FrontFace;
            desc.StencilWriteMask = 0xff;
            CreateDepthStencilState(in desc, out state);
            _states[hash] = state;
        }

        return state;
    }

    internal ID3D11DepthStencilState? GetMVState(ModifierVolumeMode mode)
    {
        ref ID3D11DepthStencilState? state = ref _mvStates[(int)mode];
        if (state == null)
        {
            DepthStencilDescription desc = default;
            desc.DepthWriteMask = DepthWriteMask.Zero;
            desc.DepthFunc = ComparisonFunction.Greater;
            desc.StencilEnable = true;
            desc.FrontFace.StencilFailOp = desc.FrontFace.StencilDepthFailOp = desc.FrontFace.StencilPassOp = StencilOperation.Keep;
            desc.FrontFace.StencilFunc = ComparisonFunction.Always;
            switch (mode)
            {
                case ModifierVolumeMode.Xor:
                    desc.DepthEnable = true;
                    desc.FrontFace.StencilFunc = ComparisonFunction.Always;
                    desc.FrontFace.StencilPassOp = StencilOperation.Invert;
                    desc.StencilWriteMask = 2;
                    break;
                case ModifierVolumeMode.Or:
                    desc.DepthEnable = true;
                    desc.FrontFace.StencilFunc = ComparisonFunction.Always;
                    desc.FrontFace.StencilPassOp = StencilOperation.Replace;
                    desc.StencilWriteMask = 2;
                    break;
                case ModifierVolumeMode.Inclusion:
                    desc.FrontFace.StencilFunc = ComparisonFunction.LessEqual;
                    desc.FrontFace.StencilPassOp = StencilOperation.Replace;
                    desc.FrontFace.StencilFailOp = StencilOperation.Zero;
                    desc.StencilReadMask = 3;
                    desc.StencilWriteMask = 3;
                    break;
                case ModifierVolumeMode.Exclusion:
                    desc.FrontFace.StencilFunc = ComparisonFunction.LessEqual;
                    desc.FrontFace.StencilFailOp = StencilOperation.Zero;
                    desc.StencilReadMask = 3;
                    desc.StencilWriteMask = 3;
                    break;
                case ModifierVolumeMode.Final:
                    desc.FrontFace.StencilFunc = ComparisonFunction.Equal;
                    desc.FrontFace.StencilPassOp = StencilOperation.Zero;
                    desc.FrontFace.StencilFailOp = StencilOperation.Zero;
                    desc.StencilReadMask = 0x81;
                    desc.StencilWriteMask = 3;
                    break;
                default:
                    break;
            }
            desc.BackFace = desc.FrontFace;
            CreateDepthStencilState(in desc, out state);
        }

        return state;
    }

    internal void Term()
    {
        foreach (ID3D11DepthStencilState? state in _states.Values)
            state?.Dispose();
        _states.Clear();
        for (int i = 0; i < _mvStates.Length; i++)
        {
            _mvStates[i]?.Dispose();
            _mvStates[i] = null;
        }
    }

    private readonly Dictionary<int, ID3D11DepthStencilState?> _states = [];
    private readonly ID3D11DepthStencilState?[] _mvStates = new ID3D11DepthStencilState?[(int)ModifierVolumeMode.Count];
}

internal sealed partial class PvrDx11BlendStates
{
    internal ID3D11BlendState? GetState(bool enable, int srcBlend = 0, int destBlend = 0,
        bool disableWrite = false, bool writeAlpha = true)
    {
        int hash = (enable ? 1 : 0) | (srcBlend << 1) | (destBlend << 5)
            | ((disableWrite ? 1 : 0) << 9) | ((writeAlpha ? 1 : 0) << 10);
        _states.TryGetValue(hash, out ID3D11BlendState? state);
        if (state == null)
        {
            BlendDescription desc = default;
            if (disableWrite)
                desc.RenderTarget[0].RenderTargetWriteMask = 0;
            else if (writeAlpha)
                desc.RenderTarget[0].RenderTargetWriteMask = ColorWriteEnable.All;
            else
                desc.RenderTarget[0].RenderTargetWriteMask = ColorWriteEnable.Red | ColorWriteEnable.Green | ColorWriteEnable.Blue;
            desc.RenderTarget[0].BlendEnable = enable;
            desc.RenderTarget[0].SourceBlend = SrcBlend[srcBlend];
            desc.RenderTarget[0].DestinationBlend = DestBlend[destBlend];
            desc.RenderTarget[0].BlendOperation = BlendOperation.Add;
            desc.RenderTarget[0].SourceBlendAlpha = SrcBlendAlpha[srcBlend];
            desc.RenderTarget[0].DestinationBlendAlpha = DestBlendAlpha[destBlend];
            desc.RenderTarget[0].BlendOperationAlpha = BlendOperation.Add;
            CreateBlendState(in desc, out state);
            _states[hash] = state;
        }

        return state;
    }

    internal void Term()
    {
        foreach (ID3D11BlendState? state in _states.Values)
            state?.Dispose();
        _states.Clear();
    }

    private readonly Dictionary<int, ID3D11BlendState?> _states = [];
}
