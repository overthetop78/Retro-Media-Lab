using RetroMediaLab.Emulation.Enums;

namespace RetroMediaLab.App.Rendering.Emulation.Processing;

internal static class FilterSegmentDisplayColor
{
    internal const string Shader = "vec3 filterSegmentColor(float color){if(color<.5)return vec3(1.0,.025,.01);if(color<1.5)return vec3(.04,1.0,.08);if(color<2.5)return vec3(1.0,.42,.015);if(color<3.5)return vec3(.03,.28,1.0);if(color<4.5)return vec3(1.0);if(color<5.5)return vec3(.012,.015,.010);if(color<6.5)return vec3(.16,.17,.155);return vec3(.085,1.0,.73); }";

    internal static (float R, float G, float B) Apply(EmulationSegmentDisplayColor color) =>
        color switch
        {
            EmulationSegmentDisplayColor.Green => (.04f, 1f, .08f),
            EmulationSegmentDisplayColor.Amber => (1f, .42f, .015f),
            EmulationSegmentDisplayColor.Blue => (.03f, .28f, 1f),
            EmulationSegmentDisplayColor.Black => (.012f, .015f, .010f),
            EmulationSegmentDisplayColor.Gray => (.16f, .17f, .155f),
            EmulationSegmentDisplayColor.BlueGreen => (.085f, 1f, .73f),
            EmulationSegmentDisplayColor.White => (1f, 1f, 1f),
            _ => (1f, .025f, .01f)
        };
}
