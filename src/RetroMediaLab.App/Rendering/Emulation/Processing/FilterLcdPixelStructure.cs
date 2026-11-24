using System.Buffers;
using System.Numerics;

namespace RetroMediaLab.App.Rendering.Emulation.Processing;

internal static class FilterLcdPixelStructure
{
    private const int NoGrid = 5;
    private static readonly Vector3 Luminance = new(.2126f, .7152f, .0722f);

    internal const string Shader = """
        vec2 lcdPanelSampleUv(vec2 uv,vec2 panelSize,vec2 outputSize)
        {
            vec2 pixel=clamp(floor(uv*outputSize),vec2(0.0),outputSize-1.0);
            return (floor((pixel+.5)*panelSize/outputSize)+.5)/panelSize;
        }
        float lcdOpenArea(float position,float start,float end)
        {
            return floor(position)*(end-start)+clamp(fract(position)-start,0.0,end-start);
        }
        float lcdCoverage(float center,float span,float start,float end)
        {
            return clamp((lcdOpenArea(center+span*.5,start,end)-lcdOpenArea(center-span*.5,start,end))/span,0.0,1.0);
        }
        vec3 lcdCellAxisWithGap(float pixel,float panelSize,float outputSize,bool striped,float gap)
        {
            if(outputSize<=panelSize)return vec3(1.0);
            const float inset=.03;
            const float openWidth=1.0-2.0*inset;
            const float stripeWidth=openWidth/3.0;
            float span=panelSize/outputSize;
            float scale=outputSize/panelSize;
            float center=fract((pixel+.5)*span);
            float aperture=mix(openWidth,lcdCoverage(center,span,inset,1.0-inset),smoothstep(1.0,2.0,scale));
            float resolved=striped&&outputSize>=2.0*panelSize?.08+.92*smoothstep(3.0,5.0,scale):0.0;
            if(resolved<=0.0)return vec3(aperture);
            float cell=floor((pixel+.5)*panelSize/outputSize);
            float start=ceil(cell*outputSize/panelSize-.5);
            float end=ceil((cell+1.0)*outputSize/panelSize-.5);
            span=1.0/max(1.0,end-start);
            center=(pixel-start+.5)*span;
            vec3 openings=vec3(lcdCoverage(center,span,inset+gap,inset+stripeWidth-gap),
                lcdCoverage(center,span,inset+stripeWidth+gap,inset+2.0*stripeWidth-gap),
                lcdCoverage(center,span,inset+2.0*stripeWidth+gap,1.0-inset-gap));
            return mix(vec3(aperture),openings,resolved);
        }
        vec3 lcdCellAxis(float pixel,float panelSize,float outputSize,bool striped)
        {
            return lcdCellAxisWithGap(pixel,panelSize,outputSize,striped,0.0);
        }
        vec3 filterLcdPixelStructure(vec3 color,vec2 uv,vec2 panelSize,vec2 outputSize,int mode)
        {
            if(mode==5)return color;
            if(mode==0)color=vec3(dot(color,vec3(.2126,.7152,.0722)));
            if(all(lessThanEqual(outputSize,panelSize)))return color;
            bool horizontal=mode>=3;
            vec2 pixel=clamp(floor(uv*outputSize),vec2(0.0),outputSize-1.0);
            vec3 mask=lcdCellAxis(pixel.x,panelSize.x,outputSize.x,mode!=0&&!horizontal)
                *lcdCellAxis(pixel.y,panelSize.y,outputSize.y,mode!=0&&horizontal);
            if(mode==2||mode==4)mask=mask.bgr;
            return color*mask;
        }
        """;

    internal static int Mode(EmulationLcdVideoConfiguration lcd) => (lcd.PixelStructure, lcd.Orientation) switch
    {
        (EmulationLcdPixelStructure.None, _) => NoGrid,
        (EmulationLcdPixelStructure.Monochrome, _) => 0,
        (EmulationLcdPixelStructure.Rgb, EmulationLcdPixelOrientation.Horizontal) => 3,
        (EmulationLcdPixelStructure.Bgr, EmulationLcdPixelOrientation.Horizontal) => 4,
        (EmulationLcdPixelStructure.Bgr, _) => 2,
        _ => 1
    };

    internal static void Apply(float[] colors, int panelWidth, int panelHeight, int width, int height,
        EmulationLcdVideoConfiguration lcd)
    {
        var mode = Mode(lcd);
        if (mode == NoGrid) return;
        var horizontal = mode >= 3;
        var monochrome = mode == 0;
        var gridVisible = width > panelWidth || height > panelHeight;
        if (!gridVisible && !monochrome) return;
        var reverse = mode is 2 or 4;
        var columns = ArrayPool<Vector3>.Shared.Rent(width);
        var neutralAxis = Vector3.One;
        try
        {
            for (var x = 0; x < width; x++)
                columns[x] = gridVisible ? Axis(x, panelWidth, width, !monochrome && !horizontal, reverse) : neutralAxis;
            for (var y = 0; y < height; y++)
            {
                var row = gridVisible ? Axis(y, panelHeight, height, !monochrome && horizontal, reverse) : neutralAxis;
                for (var x = 0; x < width; x++)
                {
                    var index = (y * width + x) * 3;
                    var color = new Vector3(colors[index], colors[index + 1], colors[index + 2]);
                    if (monochrome) color = new Vector3(Vector3.Dot(color, Luminance));
                    color *= columns[x] * row;
                    colors[index] = color.X;
                    colors[index + 1] = color.Y;
                    colors[index + 2] = color.Z;
                }
            }
        }
        finally
        {
            ArrayPool<Vector3>.Shared.Return(columns);
        }
    }

    internal static float NeutralAxis(int pixel, int panelSize, int outputSize) =>
        Axis(pixel, panelSize, outputSize, striped: false, reverse: false).X;

    internal static Vector3 Axis(int pixel, int panelSize, int outputSize, bool striped, bool reverse, float gap = 0f)
    {
        if (outputSize <= panelSize) return Vector3.One;
        const float inset = .03f;
        const float openWidth = 1f - 2f * inset;
        const float stripeWidth = openWidth / 3f;
        var span = panelSize / (float)outputSize;
        var scale = outputSize / (float)panelSize;
        var position = (pixel + .5f) * span;
        var center = position - MathF.Floor(position);
        var aperture = float.Lerp(openWidth, Coverage(center, span, inset, 1f - inset), SmoothStep(scale - 1f));
        if (!striped || outputSize < 2L * panelSize) return new Vector3(aperture);
        var resolved = .08f + .92f * SmoothStep((scale - 3f) / 2f);

        // Keep the three openings together when neighbouring cells cover different numbers
        // of output pixels. Each cell integrates the same amount of each primary.
        var cell = (int)((2L * pixel + 1) * panelSize / (2L * outputSize));
        var first = Boundary(cell, outputSize, panelSize);
        var last = Boundary(cell + 1, outputSize, panelSize);
        span = 1f / (last - first);
        center = (pixel - first + .5f) * span;

        var openings = new Vector3(
            Coverage(center, span, inset + gap, inset + stripeWidth - gap),
            Coverage(center, span, inset + stripeWidth + gap, inset + 2f * stripeWidth - gap),
            Coverage(center, span, inset + 2f * stripeWidth + gap, 1f - inset - gap));
        if (reverse) openings = new Vector3(openings.Z, openings.Y, openings.X);
        return Vector3.Lerp(new Vector3(aperture), openings, resolved);
    }

    private static int Boundary(int cell, int outputSize, int panelSize) =>
        (int)((2L * cell * outputSize + panelSize - 1) / (2L * panelSize));

    internal static float Coverage(float center, float span, float start, float end) =>
        Math.Clamp((OpenArea(center + span * .5f, start, end) -
            OpenArea(center - span * .5f, start, end)) / span, 0f, 1f);

    private static float OpenArea(float position, float start, float end)
    {
        var cell = MathF.Floor(position);
        return cell * (end - start) + Math.Clamp(position - cell - start, 0f, end - start);
    }

    private static float SmoothStep(float value)
    {
        value = Math.Clamp(value, 0f, 1f);
        return value * value * (3f - 2f * value);
    }
}
