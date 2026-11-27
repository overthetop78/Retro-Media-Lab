namespace RetroMediaLab.Chips.Video.NEC.PowerVr2;

internal sealed partial class PvrN2Vertex4Source
{
    private const string GouraudSource = """

#if pp_Gouraud == 0
#define INTERPOLATION flat
#else
#define INTERPOLATION
#endif

""";

    internal PvrN2Vertex4Source(PvrGl4PipelineShader? shader = null)
    {
        AddConstant("OIT_RENDER");
        AddConstant("DIV_POS_Z", 0);
        AddConstant("MODIFIER_VOLUME", 0);
        if (shader == null)
        {
            AddConstant("POSITION_ONLY", 1);
            AddConstant("pp_TwoVolumes", 0);
            AddConstant("pp_Gouraud", 0);
            AddConstant("pp_Texture", 0);
            AddConstant("LIGHT_ON", 0);
        }
        else
        {
            AddConstant("POSITION_ONLY", 0);
            AddConstant("pp_TwoVolumes", shader.TwoVolumes || shader.BumpMap ? 1 : 0);
            AddConstant("pp_Gouraud", shader.Gouraud ? 1 : 0);
            AddConstant("pp_Texture", shader.Texture ? 1 : 0);
            AddConstant("LIGHT_ON", shader.Pass != PvrGl4Pass.Depth ? 1 : 0);
        }

        AddSource(GouraudSource);
        if (shader != null && shader.Pass != PvrGl4Pass.Depth)
            AddSource(PvrGlNaomi2.N2ColorShader);
        AddSource(PvrGlNaomi2.N2VertexShader);
    }
}
