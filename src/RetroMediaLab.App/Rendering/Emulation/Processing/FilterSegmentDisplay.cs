using System.Numerics;
using RetroMediaLab.Emulation.Contracts;

namespace RetroMediaLab.App.Rendering.Emulation.Processing;

internal static class FilterSegmentDisplay
{
    private const string OpticalShader = """
        vec3 segmentOpticalColor(float level,float core,float halo,vec3 tint,float technology,float background)
        {
            if(technology>.5 && technology<1.5)
            {
                vec3 lcd=vec3(.57,.61,.48);
                return mix(lcd,mix(lcd*.92,tint,level),core);
            }
            vec3 panel=technology>1.5?vec3(.002,.0045,.0055):vec3(background);
            return min(panel+tint*(core*(.006+.94*level)+halo),vec3(1.0));
        }
        """;

    internal static readonly string VeldridShader = FilterSegmentDisplayCellSize.Shader
        + FilterSegmentDisplayHorizontalGap.Shader + FilterSegmentDisplayVerticalGap.Shader
        + FilterSegmentDisplaySegmentGap.Shader
        + FilterSegmentDisplayThickness.Shader + FilterSegmentDisplayEndShape.Shader
        + FilterSegmentDisplayActivationThreshold.Shader + FilterSegmentDisplayContrast.Shader
        + FilterSegmentDisplayColor.Shader
        + FilterSegmentDisplayBrightness.Shader + FilterSegmentDisplayBlackDepth.Shader
        + FilterSegmentDisplayOffSegmentVisibility.Shader
        + FilterSegmentDisplayHaloRadius.Shader + FilterSegmentDisplayGlow.Shader
        + FilterSegmentDisplayLayout.Shader + OpticalShader + """
        vec3 segmentSource(vec2 uv,float history)
        {
            if(history>.5)return adjust(texture(sampler2D(History,LinearSampler),clamp(uv,vec2(0.0),vec2(1.0))).rgb);
            return raw(clamp(uv,vec2(0.0),vec2(1.0)));
        }
        float segmentActivation(vec2 cell,vec2 cellSize,vec4 line,float point,float history)
        {
            float sum=0.0;
            for(int sampleIndex=0;sampleIndex<5;sampleIndex++)
            {
                float position=point>.5?0.0:(float(sampleIndex)+.5)/5.0;
                vec2 local=mix(line.xy,line.zw,position);
                vec2 uv=(cell+local)*cellSize/max(Parameters.Processing.zw,vec2(1.0));
                sum+=dot(segmentSource(uv,history),vec3(.2126,.7152,.0722));
            }
            return filterSegmentContrast(filterSegmentThreshold(sum/5.0,
                Parameters.SegmentEmission.z),Parameters.SegmentEmission.w);
        }
        vec3 segmentDisplayPixel(vec2 uv)
        {
            vec4 optics=Parameters.SegmentOptics;
            vec3 tint=filterSegmentColor(Parameters.SegmentEmission.x);
            if(optics.w>.5)
            {
                float red=sourceColor(uv).r;
                float level=clamp((red-optics.y)/(optics.z-optics.y),0.0,1.0);
                float core=step(.5/255.0,red);
                float halo=0.0;
                if(optics.x>1.5)
                {
                    vec2 spread=1.35/max(Parameters.Processing.zw,vec2(1.0));
                    float nearby=(sourceColor(uv+vec2(spread.x,0.0)).r + sourceColor(uv-vec2(spread.x,0.0)).r + sourceColor(uv+vec2(0.0,spread.y)).r + sourceColor(uv-vec2(0.0,spread.y)).r)*.25;
                    float scattered=clamp((nearby-optics.y)/(optics.z-optics.y),0.0,1.0);
                    halo=scattered*scattered*(1.0-core)*.14;
                }
                return segmentOpticalColor(level,core,halo,tint,optics.x,.004);
            }
            float cellWidth=filterSegmentCellWidth(Parameters.SegmentGeometry.y);
            vec2 cellSize=vec2(cellWidth,max(9.0,floor(cellWidth*1.55+.5)));
            vec2 logical=uv*Parameters.Processing.zw;
            vec2 cell=floor(logical/cellSize);
            vec2 fraction=fract(logical/cellSize);
            vec2 local=vec2(filterSegmentHorizontalGap(fraction.x,Parameters.SegmentGeometry.z),
                filterSegmentVerticalGap(fraction.y,Parameters.SegmentGeometry.w));
            int segmentLayout=int(Parameters.SegmentGeometry.x+.5);
            int count=filterSegmentCount(segmentLayout,Parameters.SegmentShape.w);
            float nearestDistance=1000.0,nearestPoint=0.0;vec4 nearestLine=vec4(0.0);
            for(int element=0;element<19;element++)
            {
                if(element>=count)continue;
                float point;vec4 line=filterSegmentLine(element,segmentLayout,Parameters.SegmentShape.w,point);
                line=filterSegmentGap(line,Parameters.SegmentShape.y);
                float distance=filterSegmentDistance(local,line,Parameters.SegmentShape.z);
                if(distance<nearestDistance){nearestDistance=distance;nearestLine=line;nearestPoint=point;}
            }
            float current=segmentActivation(cell,cellSize,nearestLine,nearestPoint,0.0);
            if(Parameters.SegmentTemporal.z>.5)
            {
                float previous=segmentActivation(cell,cellSize,nearestLine,nearestPoint,1.0);
                current=current>=previous?mix(previous,current,Parameters.SegmentTemporal.x)
                    :max(current,previous*Parameters.SegmentTemporal.y);
            }
            float radius=filterSegmentThickness(Parameters.SegmentShape.x);
            float edge=max(Parameters.Processing.z/max(Parameters.Output.x,1.0),
                Parameters.Processing.w/max(Parameters.Output.y,1.0))/min(cellSize.x,cellSize.y);
            float core=1.0-smoothstep(radius-edge,radius+edge,nearestDistance);
            float extent=(optics.x>1.5?1.0:.34)*Parameters.SegmentOptical.w;
            float glow=(optics.x>1.5?.32:.09)*current*current;
            float halo=filterSegmentHaloFalloff(max(0.0,nearestDistance-radius),extent)*glow*(1.0-core);
            return segmentOpticalColor(current,core,halo,tint,optics.x,filterSegmentBlackDepth(Parameters.SegmentOptical.y));
        }
        """;

    internal static readonly string OpenGlShader = FilterSegmentDisplayCellSize.Shader
        + FilterSegmentDisplayHorizontalGap.Shader + FilterSegmentDisplayVerticalGap.Shader
        + FilterSegmentDisplaySegmentGap.Shader
        + FilterSegmentDisplayThickness.Shader + FilterSegmentDisplayEndShape.Shader
        + FilterSegmentDisplayActivationThreshold.Shader + FilterSegmentDisplayContrast.Shader
        + FilterSegmentDisplayColor.Shader
        + FilterSegmentDisplayBrightness.Shader + FilterSegmentDisplayBlackDepth.Shader
        + FilterSegmentDisplayOffSegmentVisibility.Shader
        + FilterSegmentDisplayHaloRadius.Shader + FilterSegmentDisplayGlow.Shader
        + FilterSegmentDisplayLayout.Shader + OpticalShader + """
        vec3 segmentSource(vec2 uv,float history)
        {
            if(history>.5)return adjustColor(texture2D(History,clamp(uv,vec2(0.0),vec2(1.0))).rgb);
            return adjustColor(sampleConfigured(clamp(uv,vec2(0.0),vec2(1.0))).rgb);
        }
        float segmentActivation(vec2 cell,vec2 cellSize,vec4 line,float point,float history)
        {
            float sum=0.0;
            for(int sampleIndex=0;sampleIndex<5;sampleIndex++)
            {
                float position=point>.5?0.0:(float(sampleIndex)+.5)/5.0;
                vec2 local=mix(line.xy,line.zw,position);
                vec2 sampleUv=(cell+local)*cellSize/max(Processing.zw,vec2(1.0));
                sum+=dot(segmentSource(sampleUv,history),vec3(.2126,.7152,.0722));
            }
            return filterSegmentContrast(filterSegmentThreshold(sum/5.0,
                SegmentEmission.z),SegmentEmission.w);
        }
        vec3 segmentDisplayPixel(vec2 uv)
        {
            vec4 optics=SegmentOptics;
            vec3 tint=filterSegmentColor(SegmentEmission.x);
            if(optics.w>.5)
            {
                float red=pointSample(uv).r;
                float level=clamp((red-optics.y)/(optics.z-optics.y),0.0,1.0);
                float core=step(.5/255.0,red);
                float halo=0.0;
                if(optics.x>1.5)
                {
                    vec2 spread=1.35/max(Processing.zw,vec2(1.0));
                    float nearby=(pointSample(uv+vec2(spread.x,0.0)).r + pointSample(uv-vec2(spread.x,0.0)).r + pointSample(uv+vec2(0.0,spread.y)).r + pointSample(uv-vec2(0.0,spread.y)).r)*.25;
                    float scattered=clamp((nearby-optics.y)/(optics.z-optics.y),0.0,1.0);
                    halo=scattered*scattered*(1.0-core)*.14;
                }
                return segmentOpticalColor(level,core,halo,tint,optics.x,.004);
            }
            float cellWidth=filterSegmentCellWidth(SegmentGeometry.y);
            vec2 cellSize=vec2(cellWidth,max(9.0,floor(cellWidth*1.55+.5)));
            vec2 logical=uv*Processing.zw;
            vec2 cell=floor(logical/cellSize);
            vec2 fraction=fract(logical/cellSize);
            vec2 local=vec2(filterSegmentHorizontalGap(fraction.x,SegmentGeometry.z),
                filterSegmentVerticalGap(fraction.y,SegmentGeometry.w));
            int segmentLayout=int(SegmentGeometry.x+.5);
            int count=filterSegmentCount(segmentLayout,SegmentShape.w);
            float nearestDistance=1000.0,nearestPoint=0.0;vec4 nearestLine=vec4(0.0);
            for(int element=0;element<19;element++)
            {
                if(element>=count)continue;
                float point;vec4 line=filterSegmentLine(element,segmentLayout,SegmentShape.w,point);
                line=filterSegmentGap(line,SegmentShape.y);
                float distance=filterSegmentDistance(local,line,SegmentShape.z);
                if(distance<nearestDistance){nearestDistance=distance;nearestLine=line;nearestPoint=point;}
            }
            float current=segmentActivation(cell,cellSize,nearestLine,nearestPoint,0.0);
            if(SegmentTemporal.z>.5)
            {
                float previous=segmentActivation(cell,cellSize,nearestLine,nearestPoint,1.0);
                current=current>=previous?mix(previous,current,SegmentTemporal.x)
                    :max(current,previous*SegmentTemporal.y);
            }
            float radius=filterSegmentThickness(SegmentShape.x);
            float edge=max(Processing.z/max(Output.x,1.0),
                Processing.w/max(Output.y,1.0))/min(cellSize.x,cellSize.y);
            float core=1.0-smoothstep(radius-edge,radius+edge,nearestDistance);
            float extent=(optics.x>1.5?1.0:.34)*SegmentOptical.w;
            float glow=(optics.x>1.5?.32:.09)*current*current;
            float halo=filterSegmentHaloFalloff(max(0.0,nearestDistance-radius),extent)*glow*(1.0-core);
            return segmentOpticalColor(current,core,halo,tint,optics.x,filterSegmentBlackDepth(SegmentOptical.y));
        }
        """;

    internal static void Apply(float[] colors, int sourceWidth, int sourceHeight, int width, int height, EmulationSegmentDisplayVideoConfiguration configuration, VideoFrame frame)
    {
        var tintComponents = FilterSegmentDisplayColor.Apply(configuration.Color);
        var tint = new Vector3(tintComponents.R, tintComponents.G, tintComponents.B);
        var background = FilterSegmentDisplayBlackDepth.Apply(configuration.BlackDepth);
        if (frame.SegmentLevels is { } levels && levels.OnRed > levels.OffRed
            && frame.PixelFormat == EmulationPixelFormat.Xrgb8888)
        {
            var pixels = frame.Pixels.Span;
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var sourceX = Math.Min(frame.Width - 1, (int)((x + .5f) * frame.Width / width));
                var sourceY = Math.Min(frame.Height - 1, (int)((y + .5f) * frame.Height / height));
                var red = pixels[sourceY * frame.Pitch + sourceX * 4 + 2];
                var intensity = Math.Clamp((red - levels.OffRed) / (float)(levels.OnRed - levels.OffRed), 0f, 1f);
                Compose(intensity, red > 0 ? 1f : 0f, 0f, tint, configuration.Technology, .004f)
                    .CopyTo(colors, (y * width + x) * 3);
            }
            return;
        }

        var source = colors.ToArray();
        var cellWidth = FilterSegmentDisplayCellSize.Width(configuration.CellSize);
        var cellHeight = FilterSegmentDisplayCellSize.Height(configuration.CellSize);
        var columns = Math.Max(1, (sourceWidth + cellWidth - 1) / cellWidth);
        var rows = Math.Max(1, (sourceHeight + cellHeight - 1) / cellHeight);
        var (end, gap) = FilterSegmentDisplayEndShape.ForTechnology(configuration.Technology, configuration.EndShape, configuration.SegmentGap);
        var elements = FilterSegmentDisplayLayout.Elements(configuration.Layout, configuration.DecimalPoint, configuration.Colon)
            .Select(element => FilterSegmentDisplaySegmentGap.Apply(element, gap)).ToArray();
        var activations = SampleActivations(source, sourceWidth, sourceHeight, width, height, columns, rows, cellWidth, cellHeight, elements, configuration);
        var radius = FilterSegmentDisplayThickness.Radius(configuration.Thickness);

        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            var logicalX = (x + .5f) * sourceWidth / width;
            var logicalY = (y + .5f) * sourceHeight / height;
            var cellX = Math.Min(columns - 1, (int)(logicalX / cellWidth));
            var cellY = Math.Min(rows - 1, (int)(logicalY / cellHeight));
            var localX = FilterSegmentDisplayHorizontalGap.Apply(logicalX / cellWidth - MathF.Floor(logicalX / cellWidth), configuration.HorizontalGap);
            var localY = FilterSegmentDisplayVerticalGap.Apply(logicalY / cellHeight - MathF.Floor(logicalY / cellHeight), configuration.VerticalGap);
            var nearest = 0;
            var distance = float.MaxValue;
            for (var element = 0; element < elements.Length; element++)
            {
                var candidate = FilterSegmentDisplayEndShape.Distance(localX, localY, elements[element], end);
                if (candidate >= distance) continue;
                distance = candidate;
                nearest = element;
            }
            var edge = MathF.Max(sourceWidth / (float)width, sourceHeight / (float)height) / Math.Min(cellWidth, cellHeight);
            var core = SmoothStep(radius + edge, radius - edge, distance);
            var fluorescent = configuration.Technology == EmulationSegmentDisplayTechnology.Vfd;
            var haloRadius = fluorescent ? configuration.HaloRadius : configuration.HaloRadius / 3;
            var cell = (cellY * columns + cellX) * elements.Length + nearest;
            var activation = activations[cell];
            var halo = FilterSegmentDisplayHaloRadius.Apply(MathF.Max(0f, distance - radius), haloRadius)
                * (fluorescent ? .32f : .09f) * activation * activation * (1f - core);
            Compose(activation, core, halo, tint, configuration.Technology, background)
                .CopyTo(colors, (y * width + x) * 3);
        }
    }

    private static Vector3 Compose(float level, float core, float halo, Vector3 tint,
        EmulationSegmentDisplayTechnology technology, float background)
    {
        if (technology == EmulationSegmentDisplayTechnology.Lcd)
        {
            var lcd = new Vector3(.57f, .61f, .48f);
            return Vector3.Lerp(lcd, Vector3.Lerp(lcd * .92f, tint, level), core);
        }

        var panel = technology == EmulationSegmentDisplayTechnology.Vfd
            ? new Vector3(.002f, .0045f, .0055f) : new Vector3(background);
        return Vector3.Min(panel + tint * (core * (.006f + .94f * level) + halo), Vector3.One);
    }

    private static float[] SampleActivations(float[] source, int sourceWidth, int sourceHeight,
        int width, int height, int columns, int rows, int cellWidth, int cellHeight,
        SegmentDisplayElement[] elements, EmulationSegmentDisplayVideoConfiguration configuration)
    {
        var result = new float[checked(columns * rows * elements.Length)];
        for (var cellY = 0; cellY < rows; cellY++)
        for (var cellX = 0; cellX < columns; cellX++)
        for (var elementIndex = 0; elementIndex < elements.Length; elementIndex++)
        {
            var element = elements[elementIndex];
            var sum = 0f;
            const int samples = 5;
            for (var sample = 0; sample < samples; sample++)
            {
                var position = element.IsPoint ? 0f : (sample + .5f) / samples;
                var localX = element.StartX + (element.EndX - element.StartX) * position;
                var localY = element.StartY + (element.EndY - element.StartY) * position;
                var logicalX = Math.Clamp((cellX + localX) * cellWidth, 0f, sourceWidth - 1f);
                var logicalY = Math.Clamp((cellY + localY) * cellHeight, 0f, sourceHeight - 1f);
                var x = Math.Clamp((int)(logicalX * width / sourceWidth), 0, width - 1);
                var y = Math.Clamp((int)(logicalY * height / sourceHeight), 0, height - 1);
                var index = (y * width + x) * 3;
                sum += source[index] * .2126f + source[index + 1] * .7152f + source[index + 2] * .0722f;
            }
            result[(cellY * columns + cellX) * elements.Length + elementIndex] = FilterSegmentDisplayContrast.Apply(FilterSegmentDisplayActivationThreshold.Apply(sum / samples, configuration.ActivationThreshold), configuration.Contrast);
        }
        return result;
    }

    private static float SmoothStep(float edge0, float edge1, float value)
    {
        if (Math.Abs(edge1 - edge0) < float.Epsilon) return value < edge0 ? 0f : 1f;
        var position = Math.Clamp((value - edge0) / (edge1 - edge0), 0f, 1f);
        return position * position * (3f - 2f * position);
    }
}
