using System.Numerics;

namespace RetroMediaLab.App.Rendering.Emulation.Processing;

internal static class FilterOledPixelStructure
{
    internal const string Shader = """
        float oledOpening(float center,float footprint,float first,float last)
        {
            return clamp((min(center+footprint*.5,last)-max(center-footprint*.5,first))/footprint,0.0,1.0);
        }
        float oledDiamond(vec2 position,vec2 footprint,vec2 center,vec2 radius)
        {
            vec2 offset=abs(position-center)/radius;
            vec2 filterWidth=footprint/radius;
            return clamp(.5+(1.0-offset.x-offset.y)/max(filterWidth.x,filterWidth.y),0.0,1.0);
        }
        vec3 filterOledPixelStructure(vec3 color,vec2 uv,vec2 panel,vec2 outputSize,int mode)
        {
            if(mode<=2||mode==6)
                return filterLcdPixelStructure(color,uv,panel,outputSize,mode==0?5:mode==6?0:mode);
            vec3 cell=filterLcdPixelStructure(vec3(1.0),uv,panel,outputSize,0);
            vec2 scale=outputSize/panel;
            float detail=mode==3?smoothstep(3.5,6.0,scale.x):smoothstep(2.0,4.0,min(scale.x,scale.y));
            if(detail<=0.0)return color*cell;
            vec2 point=uv*panel;
            vec2 position=fract(point);
            vec2 span=1.0/scale;
            vec3 emission;
            if(mode==3)
            {
                float white=min(color.r,min(color.g,color.b))*.60;
                vec3 rgb=vec3(oledOpening(position.x,span.x,.265,.500),
                    oledOpening(position.x,span.x,.500,.735),
                    oledOpening(position.x,span.x,.735,.970));
                emission=((color-white)*rgb+white*oledOpening(position.x,span.x,.030,.265))
                    *oledOpening(position.y,span.y,.030,.970);
            }
            else if(mode==4)
            {
                float lower=oledOpening(position.y,span.y,.56,.92);
                emission=color*vec3(
                    lower*oledOpening(position.x,span.x,.03,.47),
                    oledOpening(position.y,span.y,.08,.44)*oledOpening(position.x,span.x,.28,.72),
                    lower*oledOpening(position.x,span.x,.53,.97));
            }
            else
            {
                float green=oledDiamond(position,span,vec2(.23,.5),vec2(.20,.36));
                float chroma=oledDiamond(position,span,vec2(.69,.5),vec2(.30,.48));
                bool red=mod(floor(point.x)+floor(point.y),2.0)<.5;
                emission=color*vec3(red?chroma:0.0,green,red?0.0:chroma);
            }
            return mix(color*cell,emission,detail);
        }
        """;

    internal static void Apply(float[] colors, int panelWidth, int panelHeight, int width, int height,
        EmulationOledPixelStructure structure)
    {
        if (structure == EmulationOledPixelStructure.None) return;
        if (structure is EmulationOledPixelStructure.RgbVertical or EmulationOledPixelStructure.BgrVertical
            or EmulationOledPixelStructure.Monochrome)
        {
            var layout = structure switch
            {
                EmulationOledPixelStructure.RgbVertical => EmulationLcdPixelStructure.Rgb,
                EmulationOledPixelStructure.BgrVertical => EmulationLcdPixelStructure.Bgr,
                _ => EmulationLcdPixelStructure.Monochrome
            };
            FilterLcdPixelStructure.Apply(colors, panelWidth, panelHeight, width, height, new() { PixelStructure = layout });
            return;
        }

        var scaleX = width / (float)panelWidth;
        var scaleY = height / (float)panelHeight;
        var detail = structure == EmulationOledPixelStructure.Wrgb
            ? SmoothStep((scaleX - 3.5f) / 2.5f) : SmoothStep((Math.Min(scaleX, scaleY) - 2f) / 2f);
        var spanX = 1f / scaleX;
        var spanY = 1f / scaleY;
        for (var y = 0; y < height; y++)
        {
            var pointY = (y + .5f) * spanY;
            var cellY = (int)pointY;
            var positionY = pointY - cellY;
            var row = FilterLcdPixelStructure.NeutralAxis(y, panelHeight, height);
            for (var x = 0; x < width; x++)
            {
                var index = (y * width + x) * 3;
                var color = new Vector3(colors[index], colors[index + 1], colors[index + 2]);
                var cell = FilterLcdPixelStructure.NeutralAxis(x, panelWidth, width) * row;
                var result = color * cell;
                if (detail > 0f)
                {
                    var pointX = (x + .5f) * spanX;
                    var cellX = (int)pointX;
                    var positionX = pointX - cellX;
                    var emission = Emission(color, structure, positionX, positionY, spanX, spanY, (cellX + cellY) % 2 == 0);
                    result = Vector3.Lerp(result, emission, detail);
                }
                colors[index] = result.X;
                colors[index + 1] = result.Y;
                colors[index + 2] = result.Z;
            }
        }
    }

    private static Vector3 Emission(Vector3 color, EmulationOledPixelStructure structure,
        float x, float y, float spanX, float spanY, bool red)
    {
        if (structure == EmulationOledPixelStructure.Wrgb)
        {
            var white = Math.Min(color.X, Math.Min(color.Y, color.Z)) * .60f;
            var rgb = new Vector3(Opening(x, spanX, .265f, .500f),
                Opening(x, spanX, .500f, .735f), Opening(x, spanX, .735f, .970f));
            return ((color - new Vector3(white)) * rgb + new Vector3(white * Opening(x, spanX, .030f, .265f)))
                * Opening(y, spanY, .030f, .970f);
        }

        if (structure == EmulationOledPixelStructure.TriangularRgb)
        {
            var lower = Opening(y, spanY, .56f, .92f);
            return color * new Vector3(lower * Opening(x, spanX, .03f, .47f),
                Opening(y, spanY, .08f, .44f) * Opening(x, spanX, .28f, .72f),
                lower * Opening(x, spanX, .53f, .97f));
        }

        var green = Diamond(x, y, spanX, spanY, .23f, .20f, .36f);
        var chroma = Diamond(x, y, spanX, spanY, .69f, .30f, .48f);
        return color * new Vector3(red ? chroma : 0f, green, red ? 0f : chroma);
    }

    private static float Opening(float center, float span, float first, float last) =>
        Math.Clamp((Math.Min(center + span * .5f, last) - Math.Max(center - span * .5f, first)) / span, 0f, 1f);

    private static float Diamond(float x, float y, float spanX, float spanY, float centerX, float radiusX, float radiusY) =>
        Math.Clamp(.5f + (1f - MathF.Abs(x - centerX) / radiusX - MathF.Abs(y - .5f) / radiusY)
            / Math.Max(spanX / radiusX, spanY / radiusY), 0f, 1f);

    private static float SmoothStep(float value)
    {
        value = Math.Clamp(value, 0f, 1f);
        return value * value * (3f - 2f * value);
    }
}
