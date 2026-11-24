using System.Numerics;

namespace RetroMediaLab.App.Rendering.Emulation.Processing;

internal static class FilterLcdDisplay
{
    internal const string Shader = """
        float lcdBand(float distance)
        {
            float coverage=clamp(1.0-abs(distance),0.0,1.0);
            return coverage*coverage*(3.0-2.0*coverage);
        }
        vec3 filterLcdDisplay(vec3 color,vec2 screenPixel,vec2 screenSize,float backlight,int panel)
        {
            vec2 panelLight=panel==0?vec2(.005,.0036):panel==1?vec2(.0018,.0048):vec2(.0004,.0011);
            float u=screenPixel.x/screenSize.x;
            float top=.20+.55*lcdBand((u-.28)*2.8)+.20*lcdBand((u-.87)*4.5);
            float bottom=.12+.65*lcdBand((u-.63)*2.5)+.27*lcdBand((u-.10)*3.0);
            float spread=max(1.0,screenSize.y*.055);
            float topDistance=screenPixel.y/spread;
            float bottomDistance=(screenSize.y-screenPixel.y)/spread;
            float topFalloff=mix(exp(-3.6*topDistance),exp(-2.0*topDistance),top);
            float bottomFalloff=mix(exp(-3.8*bottomDistance),exp(-2.3*bottomDistance),bottom);
            float edgeLight=.76*top*topFalloff+bottom*bottomFalloff;
            float dark=clamp(1.0-dot(color,vec3(.2126,.7152,.0722))/.22,0.0,1.0);
            float blackFloor=panelLight.x+panelLight.y*edgeLight*dark*dark;
            return clamp(pow(backlight,.8)*(vec3(blackFloor)+color*(1.0-blackFloor)),0.0,1.0);
        }
        float lcdResponseTime(float baseMs,int panel,float previous,float current)
        {
            return panel==2 ? baseMs*(1.0+2.2*(1.0-clamp(min(previous,current)/.2,0.0,1.0))) : baseMs;
        }
        """;

    internal static int ResponseTime(EmulationLcdPanelType panel) => panel switch
    {
        EmulationLcdPanelType.Tn => 5,
        EmulationLcdPanelType.Va => 11,
        _ => 8
    };

    internal static float ResponseTime(EmulationLcdPanelType panel, float previous, float current)
    {
        var response = ResponseTime(panel);
        return panel == EmulationLcdPanelType.Va
            ? response * (1f + 2.2f * (1f - Math.Clamp(MathF.Min(previous, current) / .2f, 0f, 1f))) : response;
    }

    internal static void Apply(float[] colors, int width, int height, EmulationLcdVideoConfiguration lcd)
    {
        var gain = MathF.Pow(lcd.BacklightIntensity / 100f, .8f);
        var (panelBlack, leak) = lcd.PanelType switch
        {
            EmulationLcdPanelType.Tn => (.005f, .0036f),
            EmulationLcdPanelType.Ips => (.0018f, .0048f),
            _ => (.0004f, .0011f)
        };
        var spread = MathF.Max(1f, height * .055f);
        for (var y = 0; y < height; y++)
        {
            var topDistance = (y + .5f) / spread;
            var bottomDistance = (height - y - .5f) / spread;
            var topTight = MathF.Exp(-3.6f * topDistance);
            var topBroad = MathF.Exp(-2f * topDistance);
            var bottomTight = MathF.Exp(-3.8f * bottomDistance);
            var bottomBroad = MathF.Exp(-2.3f * bottomDistance);
            for (var x = 0; x < width; x++)
            {
                var u = (x + .5f) / width;
                var top = .20f + .55f * Band((u - .28f) * 2.8f) + .20f * Band((u - .87f) * 4.5f);
                var bottom = .12f + .65f * Band((u - .63f) * 2.5f) + .27f * Band((u - .10f) * 3f);
                var edgeLight = .76f * top * float.Lerp(topTight, topBroad, top)
                    + bottom * float.Lerp(bottomTight, bottomBroad, bottom);
                var index = (y * width + x) * 3;
                var color = new Vector3(colors[index], colors[index + 1], colors[index + 2]);
                var dark = Math.Clamp(1f - Vector3.Dot(color, new Vector3(.2126f, .7152f, .0722f)) / .22f, 0f, 1f);
                var blackFloor = panelBlack + leak * edgeLight * dark * dark;
                color = Vector3.Clamp(gain * (new Vector3(blackFloor) + color * (1f - blackFloor)), Vector3.Zero, Vector3.One);
                colors[index] = color.X;
                colors[index + 1] = color.Y;
                colors[index + 2] = color.Z;
            }
        }
    }

    private static float Band(float distance)
    {
        var coverage = Math.Clamp(1f - MathF.Abs(distance), 0f, 1f);
        return coverage * coverage * (3f - 2f * coverage);
    }
}
