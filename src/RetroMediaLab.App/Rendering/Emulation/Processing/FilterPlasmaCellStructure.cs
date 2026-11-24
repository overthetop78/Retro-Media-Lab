using System.Numerics;

namespace RetroMediaLab.App.Rendering.Emulation.Processing;

internal static class FilterPlasmaCellStructure
{
    private static readonly Vector3 Luminance = new(.2126f, .7152f, .0722f);
    private static readonly Vector3 Amber = new(1f, .29f, .025f);

    internal const string Shader = """
        vec3 filterPlasmaCellStructure(vec3 color, vec2 uv, vec2 panel, vec2 outputSize, int mode)
        {
            vec2 pixel=clamp(floor(uv*outputSize),vec2(0.0),outputSize-1.0);
            vec2 neutral=vec2(lcdCellAxis(pixel.x,panel.x,outputSize.x,false).x,
                lcdCellAxis(pixel.y,panel.y,outputSize.y,false).x);
            if(mode==3)return dot(color,vec3(.2126,.7152,.0722))*vec3(1.0,.29,.025)*neutral.x*neutral.y;
            if(mode==2)
            {
                vec2 span=panel/outputSize;
                vec2 center=fract((pixel+.5)*span);
                float amount=smoothstep(2.0,5.0,min(outputSize.x/panel.x,outputSize.y/panel.y));
                if(amount<=0.0)return color*neutral.x*neutral.y;
                vec3 x=vec3(lcdCoverage(center.x,span.x,.04,.46),lcdCoverage(center.x,span.x,.29,.71),
                    lcdCoverage(center.x,span.x,.54,.96));
                vec2 y=vec2(lcdCoverage(center.y,span.y,.54,.96),lcdCoverage(center.y,span.y,.04,.46));
                return color*mix(vec3(neutral.x*neutral.y),x*y.xyx,amount);
            }
            vec3 columns=lcdCellAxisWithGap(pixel.x,panel.x,outputSize.x,true,mode==1?.018:0.0);
            return color*columns*(mode==1?neutral.y:1.0);
        }
        """;

    internal static void Apply(float[] colors, int panelWidth, int panelHeight, int width, int height,
        EmulationPlasmaCellStructure structure)
    {
        if (width <= panelWidth && height <= panelHeight && structure != EmulationPlasmaCellStructure.Monochrome)
            return;

        var spanX = panelWidth / (float)width;
        var spanY = panelHeight / (float)height;
        var deltaAmount = structure == EmulationPlasmaCellStructure.Delta
            ? FilterSampling.SmoothStep(2f, 5f, 1f / Math.Max(spanX, spanY)) : 0f;
        var columns = new Vector3[width];
        var neutralColumns = new float[width];
        for (var x = 0; x < width; x++)
        {
            neutralColumns[x] = FilterLcdPixelStructure.NeutralAxis(x, panelWidth, width);
            columns[x] = structure switch
            {
                EmulationPlasmaCellStructure.Stripe or EmulationPlasmaCellStructure.Waffle =>
                    FilterLcdPixelStructure.Axis(x, panelWidth, width, striped: true, reverse: false,
                        gap: structure == EmulationPlasmaCellStructure.Waffle ? .018f : 0f),
                EmulationPlasmaCellStructure.Delta when deltaAmount > 0f => DeltaColumns(x, spanX),
                _ => new Vector3(neutralColumns[x])
            };
        }

        for (var y = 0; y < height; y++)
        {
            var neutralRow = FilterLcdPixelStructure.NeutralAxis(y, panelHeight, height);
            var phase = Phase(y, spanY);
            var lower = deltaAmount > 0f ? FilterLcdPixelStructure.Coverage(phase, spanY, .54f, .96f) : 0f;
            var upper = deltaAmount > 0f ? FilterLcdPixelStructure.Coverage(phase, spanY, .04f, .46f) : 0f;
            var deltaRows = new Vector3(lower, upper, lower);
            for (var x = 0; x < width; x++)
            {
                var index = (y * width + x) * 3;
                var color = new Vector3(colors[index], colors[index + 1], colors[index + 2]);
                var aperture = structure switch
                {
                    EmulationPlasmaCellStructure.Stripe => columns[x],
                    EmulationPlasmaCellStructure.Delta when deltaAmount > 0f => Vector3.Lerp(
                        new Vector3(neutralColumns[x] * neutralRow), columns[x] * deltaRows, deltaAmount),
                    _ => columns[x] * neutralRow
                };
                if (structure == EmulationPlasmaCellStructure.Monochrome) color = Vector3.Dot(color, Luminance) * Amber;
                color *= aperture;
                colors[index] = color.X;
                colors[index + 1] = color.Y;
                colors[index + 2] = color.Z;
            }
        }
    }

    private static Vector3 DeltaColumns(int x, float span)
    {
        var phase = Phase(x, span);
        return new Vector3(FilterLcdPixelStructure.Coverage(phase, span, .04f, .46f),
            FilterLcdPixelStructure.Coverage(phase, span, .29f, .71f),
            FilterLcdPixelStructure.Coverage(phase, span, .54f, .96f));
    }

    private static float Phase(int pixel, float span)
    {
        var position = (pixel + .5f) * span;
        return position - MathF.Floor(position);
    }
}
