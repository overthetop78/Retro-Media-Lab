namespace RetroMediaLab.Chips.Video.NEC.PowerVr2;

internal sealed unsafe partial class PvrOpenGlDriver : PvrImGuiDriver
{
    internal override void SetFrameRendered()
    {
        _frameRendered = true;
    }

    internal override nint GetTexture(string name)
    {
        if (_textures.TryGetValue(name, out nint texture))
            return texture;
        return 0;
    }

    private bool _frameRendered;
    private readonly Dictionary<string, nint> _textures = [];
}
