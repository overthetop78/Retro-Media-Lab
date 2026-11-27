namespace RetroMediaLab.Chips.Video.NEC.PowerVr2;

internal sealed class PvrNoRenderer : PvrRenderBackend
{
    private RenderContext? _renderContext;

    internal override bool Init() => true;

    internal override void Term()
    {
    }

    internal override void Process(TaContext context)
    {
        _renderContext = context.Rend;
        PowerVr2Ta.Parse(context, true);
    }

    internal override bool Render() => !_renderContext!.IsRtt;

    internal override void RenderFramebuffer(in PvrFramebufferInfo info)
    {
        _renderContext = null;
    }
}
