using SharpGen.Runtime;
using Vortice.Direct3D11;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Direct3D11;

internal sealed partial class PvrDx11DepthStencilStates
{
    private static Result CreateDepthStencilState(in DepthStencilDescription desc, out ID3D11DepthStencilState? state)
    {
        return PvrDx11Context.Instance.Device.CreateDepthStencilState(desc, out state);
    }
}

internal sealed partial class PvrDx11BlendStates
{
    private static Result CreateBlendState(in BlendDescription desc, out ID3D11BlendState? state)
    {
        return PvrDx11Context.Instance.Device.CreateBlendState(desc, out state);
    }
}
