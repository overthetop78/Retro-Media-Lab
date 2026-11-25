using System.Numerics;
using RetroMediaLab.VideoPresentation.Enums;

namespace RetroMediaLab.App.Rendering.Emulation.Processing;

internal static class FilterEPaper
{
    private const string Shader = """
        vec3 ePaperPigment(vec3 color, int mode, vec4 surface)
        {
            color=clamp(color,vec3(0.0),vec3(1.0));
            float y=dot(color,vec3(.2126,.7152,.0722));
            if(mode==2||mode==5)
            {
                float levels=mode==2?15.0:31.0;
                vec3 ink=floor(color*levels+.5)/levels;
                float gray=dot(ink,vec3(.2126,.7152,.0722));
                ink=mix(vec3(gray),ink,mode==2?.56:.94);
                return mix(vec3(surface.w),surface.rgb,ink);
            }
            if(mode==3 && color.r>max(color.g,color.b)+.18 && color.r>.36)
                return vec3(.60,.031,.026);
            if(mode==4 && min(color.r,color.g)>color.b+.17 && min(color.r,color.g)>.36)
                return vec3(.81,.65,.041);
            float level=mode==1?floor(y*15.0+.5)/15.0:step(.48,y);
            return mix(vec3(surface.w),surface.rgb,level);
        }
        vec3 ePaperPixel(vec2 uv)
        {
            vec4 surface=EPaperSurface;
            vec4 refresh=EPaperTemporal;
            vec2 panel=EPaperInkAndColor.xy;
            vec2 cell=clamp(floor(uv*panel),vec2(0.0),panel-1.0);
            vec2 center=(cell+.5)/panel;
            int mode=int(EPaperInkAndColor.z+.5);
            vec3 current=ePaperPigment(CURRENT,mode,surface);
            if(refresh.w<.5)return current;
            vec3 previous=ePaperPigment(PREVIOUS,mode,surface);
            vec3 pigment=mix(previous,current,refresh.x);
            if(refresh.y>0.0 && length(current-previous)>.01)
                pigment=mix(pigment,clamp(surface.rgb+surface.w-pigment,vec3(0.0),vec3(1.0)),refresh.y);
            return pigment;
        }
        """;

    internal static readonly string VeldridShader = Shader
        .Replace("EPaper", "Parameters.EPaper", StringComparison.Ordinal)
        .Replace("CURRENT", "adjust(texture(sampler2D(Source,PointSampler),center).rgb)", StringComparison.Ordinal)
        .Replace("PREVIOUS", "adjust(texture(sampler2D(History,PointSampler),center).rgb)", StringComparison.Ordinal);

    internal static readonly string OpenGlShader = Shader
        .Replace("CURRENT", "adjustColor(texture2D(Source,center).rgb)", StringComparison.Ordinal)
        .Replace("PREVIOUS", "adjustColor(texture2D(History,center).rgb)", StringComparison.Ordinal);

    internal static Vector4 Surface(EmulationEPaperColorMode mode) => mode switch
    {
        EmulationEPaperColorMode.Monochrome => new(.83f, .82f, .76f, .025f),
        EmulationEPaperColorMode.Grayscale16 => new(.82f, .81f, .76f, .035f),
        EmulationEPaperColorMode.TricolorRed or EmulationEPaperColorMode.TricolorYellow => new(.84f, .83f, .77f, .030f),
        EmulationEPaperColorMode.Color4096 => new(.67f, .67f, .63f, .072f),
        _ => new(.83f, .82f, .77f, .032f)
    };

    internal static Vector3 Pigment(Vector3 color, EmulationEPaperColorMode mode, Vector4 surface)
    {
        color = Vector3.Clamp(color, Vector3.Zero, Vector3.One);
        var weights = new Vector3(.2126f, .7152f, .0722f);
        var luminance = Vector3.Dot(color, weights);
        var paper = new Vector3(surface.X, surface.Y, surface.Z);
        var black = new Vector3(surface.W);
        if (mode is EmulationEPaperColorMode.Color4096 or EmulationEPaperColorMode.Gallery)
        {
            var levels = mode == EmulationEPaperColorMode.Color4096 ? 15f : 31f;
            var ink = new Vector3(Quantize(color.X, levels), Quantize(color.Y, levels), Quantize(color.Z, levels));
            ink = Vector3.Lerp(new Vector3(Vector3.Dot(ink, weights)), ink,
                mode == EmulationEPaperColorMode.Color4096 ? .56f : .94f);
            return black + (paper - black) * ink;
        }

        if (mode == EmulationEPaperColorMode.TricolorRed && color.X > Math.Max(color.Y, color.Z) + .18f && color.X > .36f)
            return new(.60f, .031f, .026f);
        if (mode == EmulationEPaperColorMode.TricolorYellow && Math.Min(color.X, color.Y) > color.Z + .17f && Math.Min(color.X, color.Y) > .36f)
            return new(.81f, .65f, .041f);

        var level = mode == EmulationEPaperColorMode.Grayscale16 ? Quantize(luminance, 15f) : luminance >= .48f ? 1f : 0f;
        return Vector3.Lerp(black, paper, level);
    }

    internal static void Apply(float[] colors, EmulationEPaperColorMode mode)
    {
        var surface = Surface(mode);
        for (var index = 0; index < colors.Length; index += 3)
        {
            var color = Pigment(new(colors[index], colors[index + 1], colors[index + 2]), mode, surface);
            colors[index] = color.X;
            colors[index + 1] = color.Y;
            colors[index + 2] = color.Z;
        }
    }

    private static float Quantize(float value, float levels) => MathF.Floor(value * levels + .5f) / levels;
}
