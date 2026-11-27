using System.IO;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2;

internal partial struct PvrFramebufferInfo
{
    internal PvrRegisters.FB_R_SIZE_type ReadSize;
    internal PvrRegisters.FB_R_CTRL_type ReadControl;
    internal PvrRegisters.SPG_CONTROL_type SpgControl;
    internal PvrRegisters.SPG_STATUS_type SpgStatus;
    internal uint ReadStart1;
    internal uint ReadStart2;
    internal PvrRegisters.VO_CONTROL_type VideoControl;
    internal PvrRegisters.VO_BORDER_COL_type VideoBorderColor;

    internal void Update()
    {
        ReadSize.full = PvrRegisters.FB_R_SIZE.full;
        ReadControl.full = PvrRegisters.FB_R_CTRL.full;
        SpgControl.full = PvrRegisters.SPG_CONTROL.full;
        SpgStatus.full = PvrRegisters.SPG_STATUS.full;
        ReadStart1 = PvrRegisters.FB_R_SOF1;
        ReadStart2 = PvrRegisters.FB_R_SOF2;
        VideoControl.full = PvrRegisters.VO_CONTROL.full;
        VideoBorderColor.full = PvrRegisters.VO_BORDER_COL.full;
    }
}

internal abstract class PvrRenderBackend : IDisposable
{
    protected PvrRenderBackend()
    {
        PvrRuntime.Events.Listen(PvrEvent.Terminate, OnEvent, this);
        PvrRuntime.Events.Listen(PvrEvent.LoadState, OnEvent, this);
    }

    public void Dispose()
    {
        try
        {
            PvrRuntime.Events.Unlisten(PvrEvent.Terminate, OnEvent, this);
        }
        finally
        {
            PvrRuntime.Events.Unlisten(PvrEvent.LoadState, OnEvent, this);
        }
    }

    internal abstract bool Init();
    internal abstract void Term();
    internal abstract void Process(TaContext context);
    internal abstract bool Render();
    internal abstract void RenderFramebuffer(in PvrFramebufferInfo info);
    internal virtual bool RenderLastFrame() => false;
    internal virtual bool GetLastFrame(List<byte> data, ref int width, ref int height) => false;
    internal virtual bool Present() => true;
    internal virtual PvrBaseTextureCacheData? GetTexture(TSP tsp, TCW tcw, int area = 0) => null;

    protected internal bool ResetTextureCache;
    protected internal bool ClearLastFrame;
    protected internal bool UpdatePalette = true;
    protected internal bool UpdateFogTable = true;

    private static void OnEvent(PvrEvent @event, object argument)
    {
        var renderer = (PvrRenderBackend)argument;
        renderer.ResetTextureCache = true;
        renderer.UpdatePalette = true;
        renderer.UpdateFogTable = true;
        if (@event == PvrEvent.Terminate)
            renderer.ClearLastFrame = true;
    }
}

internal static partial class PvrRenderer
{
    internal static partial bool Init();
    internal static partial void Term();
    internal static partial void Vblank();
    internal static partial void StartRender();
    internal static partial int EndRender(int tag, int cycles, int jitter, nint argument);
    internal static partial void CancelEmulationWait();
    internal static partial bool SingleFrame(in bool enabled);
    internal static partial void SwapFrame(uint framebufferReadStart);
    internal static partial void SetFramebufferWriteAddress(uint framebufferWriteStart);
    internal static partial void Reset();
    internal static partial void DisableRollback();
    internal static partial void StartRollback();
    internal static partial void AllowRollback();
    internal static partial void Enable(bool enabled);
    internal static partial bool IsEnabled();
    internal static partial void Serialize(BinaryWriter serializer);
    internal static partial void Deserialize(BinaryReader deserializer, int version);
    internal static partial void CheckFramebufferWrite();

    internal static void UpdatePalette()
    {
        if (Backend is not null)
            Backend.UpdatePalette = true;
    }

    internal static void UpdateFogTable()
    {
        if (Backend is not null)
            Backend.UpdateFogTable = true;
    }
}

internal sealed class PvrRendererException(string reason) : Exception(reason);
