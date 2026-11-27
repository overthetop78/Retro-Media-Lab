using System.Numerics;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2;

internal enum TileClipping
{
    Inside,
    Off,
    Outside
}

internal sealed partial class PvrTransformMatrix
{
    private RenderContext _renderingContext = null!;
    private readonly bool _directX;
    private Matrix4x4 _normalMatrix;
    private Matrix4x4 _viewportMatrix;

    internal PvrTransformMatrix(bool directX = false) => _directX = directX;

    internal PvrTransformMatrix(RenderContext renderingContext, int width, int height, bool directX = false)
    {
        _directX = directX;
        CalcMatrices(renderingContext, width, height);
    }

    internal Matrix4x4 GetNormalMatrix() => _normalMatrix;
    internal Matrix4x4 GetViewportMatrix() => _viewportMatrix;

    internal partial void CalcMatrices(RenderContext renderingContext, int width = 0, int height = 0);
    internal partial PvrRect GetBaseScissor();
    internal partial TileClipping GetTileClip(uint value, ref PvrRect clipRect);

    private partial bool IsClipped();
    private partial void GetScissorScaling(out float scaleX, out float scaleY);
    private partial PvrRect IntersectTileAndFramebufferScissor();

    internal static partial void GetPvrFramebufferSize(RenderContext context, out int width, out int height);
    internal static partial void GetScaledFramebufferSize(RenderContext context, out int width, out int height);
    internal static partial float GetOutputFramebufferAspectRatio();
    internal static partial void GetDcFramebufferReadSize(in PvrFramebufferInfo info, out int width, out int height);
    internal static partial void GetWriteFramebufferToVramParams(RenderContext context, out PvrInt2 scaledSize, out PvrRect finalClip);
    internal static partial void GetWindowboxDimensions(int outWidth, int outHeight, float renderAspect, ref int dx, ref int dy, bool rotate);
    internal static partial void GetVideoShift(out float x, out float y);

    internal static PvrRect Intersect(PvrRect left, PvrRect right)
    {
        PvrInt2 origin = Max(left.Origin, right.Origin);
        return new PvrRect(origin,
            Max(Min(left.BottomRight(), right.BottomRight()) - origin + new PvrInt2(1, 1), new PvrInt2(0, 0)));
    }

    internal static float GetDcFramebufferAspectRatio()
    {
        float aspectRatio = PvrConfiguration.Rotate90 ? 3f / 4f : 4f / 3f;
        return aspectRatio * PvrConfiguration.ScreenStretching / 100f;
    }

    private static PvrInt2 Min(PvrInt2 left, PvrInt2 right) =>
        new(Math.Min(left.X, right.X), Math.Min(left.Y, right.Y));

    private static PvrInt2 Max(PvrInt2 left, PvrInt2 right) =>
        new(Math.Max(left.X, right.X), Math.Max(left.Y, right.Y));
}
