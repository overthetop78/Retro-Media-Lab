namespace RetroMediaLab.Chips.Video.NEC.PowerVr2;

internal unsafe sealed partial class PvrGlQuadDrawer : IDisposable
{
    private sealed class VertexArray : PvrGlVertexArray
    {
        protected override void DefineVtxAttribs()
        {
            PvrGl.EnableVertexAttribArray(PvrGles.VertexPosArray);
            PvrGl.VertexAttribPointer(PvrGles.VertexPosArray, 3, PvrGl.Float, 0, sizeof(float) * 5, 0);
            PvrGl.EnableVertexAttribArray(PvrGles.VertexUvArray);
            PvrGl.VertexAttribPointer(PvrGles.VertexUvArray, 2, PvrGl.Float, 0, sizeof(float) * 5, sizeof(float) * 3);
            PvrGl.DisableVertexAttribArray(PvrGles.VertexColBaseArray);
            PvrGl.DisableVertexAttribArray(PvrGles.VertexColOffsArray);
            PvrGl.DisableVertexAttribArray(PvrGles.VertexColBase1Array);
            PvrGl.DisableVertexAttribArray(PvrGles.VertexColOffs1Array);
            PvrGl.DisableVertexAttribArray(PvrGles.VertexUv1Array);
        }
    }

    private uint _shader;
    private int _tintUniform;
    private uint _rot90Shader;
    private int _rot90TintUniform;
    private readonly VertexArray _quadVertexArray = new();
    private readonly VertexArray _quadVertexArraySwapY = new();
    private PvrGlBuffer? _quadBuffer;
    private PvrGlBuffer? _quadBufferSwapY;
    private PvrGlBuffer? _quadIndexBuffer;
    private PvrGlBuffer? _quadBufferCustom;
    private readonly VertexArray _quadVertexArrayCustom = new();
}
