namespace RetroMediaLab.App.Rendering.Emulation.Processing;

internal static class ProjectionVideoShader
{
    internal static string Create(bool veldrid)
    {
        var body = """
            float projectionAperture(float position,float cells,float pixels,float border)
            {
                if(border<=0.0||pixels<=cells)return 1.0;
                float span=cells/pixels;
                float center=fract((position+.5)*span);
                return mix(1.0,lcdCoverage(center,span,border,1.0-border),smoothstep(1.0,2.5,pixels/cells));
            }

            vec3 projectionSource(vec2 uv)
            {
                return SAMPLE(lcdPanelSampleUv(uv,PANEL_SIZE,OUT_SIZE));
            }

            vec3 projectionPixel(vec2 uv)
            {
                vec2 pixel=clamp(floor(uv*OUT_SIZE),vec2(0.0),OUT_SIZE-1.0);
                vec3 color=projectionSource(uv);
                if(OPTICS.y>0.0)
                {
                    // Sample neighboring panel cells for the optical spread of a CRT projector.
                    vec2 stepSize=max(1.0/OUT_SIZE,.55/PANEL_SIZE);
                    vec3 surround=(projectionSource(uv-vec2(stepSize.x,0.0))
                        +projectionSource(uv+vec2(stepSize.x,0.0))
                        +projectionSource(uv-vec2(0.0,stepSize.y))
                        +projectionSource(uv+vec2(0.0,stepSize.y)))*.25;
                    vec2 shift=vec2(OPTICS.w/SOURCE_SIZE.x,0.0);
                    color.r=mix(color.r,projectionSource(uv-vec2(1.0/SOURCE_SIZE.x,0.0)).r,shift.x*SOURCE_SIZE.x);
                    color.b=mix(color.b,projectionSource(uv+vec2(1.0/SOURCE_SIZE.x,0.0)).b,shift.x*SOURCE_SIZE.x);
                    color=mix(color,surround,OPTICS.y);
                }
                float area=projectionAperture(pixel.x,PANEL_SIZE.x,OUT_SIZE.x,OPTICS.z)
                    *projectionAperture(pixel.y,PANEL_SIZE.y,OUT_SIZE.y,OPTICS.z);
                color=mix(vec3(OPTICS.x),vec3(1.0),color*area);
                vec2 p=pixel+.5;
                float textureLevel=.5+.25*sin(p.x*.71+p.y*.31)+.25*sin(p.x*.17-p.y*.83);
                return mix(color,vec3(1.0),SCREEN.y)*SCREEN.x*(1.0-SCREEN.z*textureLevel);
            }
            """;
        var prefix = veldrid ? "Parameters." : "";
        return body.Replace("OUT_SIZE", prefix + "Output.xy").Replace("PANEL_SIZE", prefix + "FixedSpatial.yz")
            .Replace("SOURCE_SIZE", prefix + "Processing.zw").Replace("OPTICS", prefix + "Projection")
            .Replace("SCREEN", prefix + "ProjectionScreen").Replace("SAMPLE", veldrid ? "raw" : "extraRaw");
    }
}
