namespace RetroMediaLab.App.Rendering.Emulation.Processing;
internal static partial class VeldridVideoProcessingShaders
{
    private const string FragmentHeader = """
        #version 450
        layout(set=0,binding=0) uniform VideoParameters
        {
            vec4 Adjustments; vec4 Processing; vec4 Output;
            vec4 CrtRaster; vec4 CrtTuning; vec4 CrtPhosphor; vec4 CrtGeometry;
            vec4 VectorPhosphor; vec4 VectorBeam;
            vec4 FixedDisplay; vec4 FixedSpatial; vec4 FixedTechnology; vec4 FixedTemporal;
            vec4 PlasmaEffect; vec4 PlasmaTemporal; vec4 PlasmaDisplay;
            vec4 Restoration; vec4 SegmentGeometry; vec4 SegmentShape;
            vec4 SegmentEmission; vec4 SegmentOptical; vec4 SegmentTemporal;
            vec4 General; vec4 Restoration2;
            vec4 Signal; vec4 Stylistic; vec4 Stylistic2;
            vec4 VfdDisplay; vec4 VfdStructure; vec4 VfdOptical;
            vec4 LedMatrixStructure;
            vec4 DotMatrixGeometry; vec4 DotMatrixEmission; vec4 DotMatrixTemporal;
            vec4 EPaperInkAndColor; vec4 EPaperSurface; vec4 EPaperTemporal;
            vec4 Projection; vec4 ProjectionScreen;
        } Parameters;
        layout(set=0,binding=1) uniform texture2D Source;
        layout(set=0,binding=2) uniform texture2D History;
        layout(set=0,binding=3) uniform sampler PointSampler;
        layout(set=0,binding=4) uniform sampler LinearSampler;
        layout(location=0) in vec2 fsin_TexCoord;
        layout(location=0) out vec4 fsout_Color;

        """;
    private static readonly string FragmentBody = """
        float srgbToLinear(float value)
        {
            value=clamp(value,0.0,1.0);
            return value<=.04045 ? value/12.92 : pow((value+.055)/1.055,2.4);
        }
        float linearToSrgb(float value)
        {
            value=clamp(value,0.0,1.0);
            return value<=.0031308 ? value*12.92 : 1.055*pow(value,1.0/2.4)-.055;
        }
        vec3 restorationLinear(vec3 color)
        {
            return vec3(srgbToLinear(color.r), srgbToLinear(color.g), srgbToLinear(color.b));
        }
        vec3 restorationPoint(vec2 uv)
        {
            ivec2 size = textureSize(sampler2D(Source, PointSampler), 0);
            ivec2 coordinate = clamp(ivec2(floor(uv * vec2(size))), ivec2(0), size - 1);
            return restorationLinear(texelFetch(sampler2D(Source, PointSampler), coordinate, 0).rgb);
        }
        """ + VideoImageRestorationShader.Source + """
        vec3 adjustLinear(vec3 linear)
        {
            linear=videoContrastParameter(linear,Parameters.Adjustments.y);
        #if DISPLAY_TECHNOLOGY != 1 && DISPLAY_TECHNOLOGY != 4
            linear=videoBrightnessParameter(linear,Parameters.Adjustments.x);
        #endif
            linear=videoGammaParameter(linear,Parameters.Adjustments.z);
            return videoSaturationParameter(linear,Parameters.Adjustments.w);
        }
        vec3 adjust(vec3 color) { return adjustLinear(restorationLinear(color)); }
        float hash(vec2 p){return fract(sin(dot(p,vec2(12.9898,78.233))+Parameters.General.z)*43758.5453);}
        #if DISPLAY_TECHNOLOGY == 2 || DISPLAY_TECHNOLOGY == 10
        vec3 panelDecodedColor(vec2 uv, bool history);
        #endif
        vec3 displaySourceColor(vec2 uv)
        {
        #if DISPLAY_TECHNOLOGY == 2 || DISPLAY_TECHNOLOGY == 10
            if (Parameters.CrtGeometry.y > .5) return panelDecodedColor(uv, false);
        #endif
            return sourceColor(uv);
        }
        vec3 raw(vec2 uv){return adjust(displaySourceColor(uv));}
        vec3 restored(vec2 uv)
        {
            vec3 source = restorationLinear(displaySourceColor(uv));
        #if DISPLAY_TECHNOLOGY == 0
            float recover = Parameters.Processing.y > .5 && any(notEqual(Parameters.Output.xy, Parameters.Processing.zw))
                ? Parameters.Restoration.x : 0.0;
            source = restoreHostSource(source, uv, Parameters.Processing.zw, Parameters.Restoration2.z, recover);
        #endif
            vec3 c = adjustLinear(source);
            if(abs(Parameters.Processing.x)>0.0001)
            {
                vec2 stepSize=1.0/max(Parameters.Output.xy,vec2(1.0));
                vec3 sum=c;
                for(int y=-1;y<=1;y++) for(int x=-1;x<=1;x++)
                    if(x!=0||y!=0)sum+=raw(uv+vec2(x,y)*stepSize);
                float weight=9.0;
                if(Parameters.Processing.x<0.0)
                {
                    vec2 dx=vec2(stepSize.x*2.0,0.0),dy=vec2(0.0,stepSize.y*2.0);
                    sum+=(raw(uv-dx)+raw(uv+dx)+raw(uv-dy)+raw(uv+dy))*.5;
                    weight=11.0;
                }
                c=videoSharpnessParameter(c,sum/weight,Parameters.Processing.x);
            }
            return clamp(c,0.0,1.0);
        }
        """ + "\n" + """
        #if DISPLAY_TECHNOLOGY == 2 || DISPLAY_TECHNOLOGY == 3 || DISPLAY_TECHNOLOGY == 10
        """ + "\n" + FilterLcdPixelStructure.Shader + "\n" + """
        #endif
        #if DISPLAY_TECHNOLOGY == 2
        """ + "\n" + FilterOledPixelStructure.Shader + FilterLcdDisplay.Shader + FilterLedBacklitLcdDisplay.Shader + FilterOledDisplay.Shader + FilterFixedPixelResponse.Shader + FilterFixedPixelPersistence.Shader + "\n" + """
        #endif
        #if DISPLAY_TECHNOLOGY == 3
        """ + "\n" + FilterPlasmaCellStructure.Shader + FilterPlasmaTemporalDithering.Shader + FilterPlasmaPersistence.Shader + FilterPlasmaBlackDepth.Shader + FilterPlasmaPhosphorIntensity.Shader + FilterPlasmaAutomaticBrightnessLimiter.Shader + "\n" + """
        #endif
        #if DISPLAY_TECHNOLOGY == 5
        """ + "\n" + FilterVfdEmissionThreshold.Shader + FilterVfdPhosphorIntensity.Shader + FilterVfdPhosphorColor.Shader + FilterVfdGlass.Shader + FilterVfdCellStructure.Shader + FilterVfdHaloRadius.Shader + FilterVfdHalo.Shader + FilterVfdPersistence.Shader + "\n" + """
        #endif
        #if DISPLAY_TECHNOLOGY == 7
        """ + "\n" + FilterDotMatrixCellSize.Shader + FilterDotMatrixCellGap.Shader + FilterDotMatrixShape.Shader + FilterDotMatrixDotSize.Shader + FilterDotMatrixContrast.Shader + FilterDotMatrixBrightness.Shader + FilterDotMatrixPalette.Shader + FilterDotMatrixHalo.Shader + FilterDotMatrixResponse.Shader + FilterDotMatrixPersistence.Shader + "\n" + """
        #endif
        #if DISPLAY_TECHNOLOGY == 8
        """ + "\n" + FilterSegmentDisplay.VeldridShader + "\n" + """
        #endif
        #if DISPLAY_TECHNOLOGY == 9
        """ + "\n" + FilterEPaper.VeldridShader + "\n" + """
        #endif
        #if DISPLAY_TECHNOLOGY == 3
        vec3 plasmaPixel(vec3 color,vec2 uv)
        {
            color=filterPlasmaPhosphorIntensity(color,Parameters.PlasmaDisplay.y);
            color=filterPlasmaAutomaticBrightnessLimiter(color,Parameters.PlasmaDisplay.w,Parameters.PlasmaTemporal.w);
            vec2 panel=Parameters.FixedSpatial.yz;
            vec2 outputSize=Parameters.Output.xy;
            vec2 cell=floor(lcdPanelSampleUv(uv,panel,outputSize)*panel);
            color=filterPlasmaTemporalDithering(color,cell,Parameters.PlasmaEffect.w,Parameters.PlasmaTemporal.z);
            return filterPlasmaCellStructure(filterPlasmaBlackDepth(color),uv,panel,outputSize,int(Parameters.PlasmaEffect.y+.5));
        }
        #endif
        #if DISPLAY_TECHNOLOGY == 5
        float vfdEmission(vec2 uv)
        {
            vec3 sampleColor=adjust(sourceColor(clamp(uv,vec2(0.0),vec2(1.0))));
            return filterVfdEmissionThreshold(dot(sampleColor,vec3(.2126,.7152,.0722)),
                Parameters.VfdDisplay.z);
        }
        vec3 vfdPixel(vec3 source,vec2 uv)
        {
            float mask=filterVfdCellStructure(uv,Parameters.Processing.zw,
                Parameters.VfdStructure.x,Parameters.VfdStructure.y,
                Parameters.VfdStructure.z);
            float emission=filterVfdPhosphorIntensity(filterVfdEmissionThreshold(
                dot(source,vec3(.2126,.7152,.0722)),Parameters.VfdDisplay.z)*mask,
                Parameters.VfdDisplay.y);
            vec2 stepSize=1.0/max(Parameters.Processing.zw,vec2(1.0));
            float radius=filterVfdHaloRadius(Parameters.VfdOptical.x);
            float nearRadius=max(1.0,radius*.5);
            float nearEmission=(vfdEmission(uv+vec2(stepSize.x*nearRadius,0.0))
                +vfdEmission(uv-vec2(stepSize.x*nearRadius,0.0))
                +vfdEmission(uv+vec2(0.0,stepSize.y*nearRadius))
                +vfdEmission(uv-vec2(0.0,stepSize.y*nearRadius)))*.25;
            float farEmission=(vfdEmission(uv+vec2(stepSize.x*radius,stepSize.y*radius))
                +vfdEmission(uv+vec2(-stepSize.x*radius,stepSize.y*radius))
                +vfdEmission(uv+vec2(stepSize.x*radius,-stepSize.y*radius))
                +vfdEmission(uv-vec2(stepSize.x*radius,stepSize.y*radius)))*.25;
            float halo=filterVfdPhosphorIntensity(filterVfdHalo(nearEmission,farEmission,
                Parameters.VfdStructure.w),Parameters.VfdDisplay.y);
            return clamp(filterVfdGlass(source,Parameters.VfdDisplay.w)
                +filterVfdPhosphorColor(emission,Parameters.VfdDisplay.x)
                +filterVfdPhosphorColor(halo,Parameters.VfdDisplay.x),0.0,1.0);
        }
        #endif
        #if DISPLAY_TECHNOLOGY == 6
        """ + "\n" + FilterLedMatrixCellStructure.CreateShader(true) + "\n" + """
        #endif
        #if DISPLAY_TECHNOLOGY == 7
        vec3 dotMatrixSample(vec2 uv,bool history)
        {
            vec2 sourceSize=max(Parameters.Processing.zw,vec2(1.0));
            float pitch=filterDotMatrixPitch(Parameters.DotMatrixGeometry.z);
            vec2 cell=floor(uv*sourceSize/pitch);
            vec2 centerUv=(cell+.5)*pitch/sourceSize;
            vec2 offset=vec2(pitch*.28)/sourceSize;
            if(history)
                return (adjust(texture(sampler2D(History,LinearSampler),centerUv).rgb)*4.0
                    +adjust(texture(sampler2D(History,LinearSampler),centerUv-vec2(offset.x,0)).rgb)
                    +adjust(texture(sampler2D(History,LinearSampler),centerUv+vec2(offset.x,0)).rgb)
                    +adjust(texture(sampler2D(History,LinearSampler),centerUv-vec2(0,offset.y)).rgb)
                    +adjust(texture(sampler2D(History,LinearSampler),centerUv+vec2(0,offset.y)).rgb))/8.0;
            return (raw(centerUv)*4.0+raw(centerUv-vec2(offset.x,0))
                +raw(centerUv+vec2(offset.x,0))+raw(centerUv-vec2(0,offset.y))
                +raw(centerUv+vec2(0,offset.y)))/8.0;
        }
        vec3 dotMatrixPixel(vec2 uv,bool history)
        {
            vec2 sourceSize=max(Parameters.Processing.zw,vec2(1.0));
            float pitch=filterDotMatrixPitch(Parameters.DotMatrixGeometry.z);
            vec2 local=fract(uv*sourceSize/pitch)-.5;
            float distance=filterDotMatrixDistance(local,Parameters.DotMatrixGeometry.y);
            float radius=filterDotMatrixRadius(Parameters.DotMatrixGeometry.w,
                Parameters.DotMatrixEmission.x);
            float edge=max(sourceSize.x/max(Parameters.Output.x,1.0),
                sourceSize.y/max(Parameters.Output.y,1.0))/pitch;
            float core=smoothstep(radius+edge,radius-edge,distance);
            float halo=filterDotMatrixHalo(distance,radius,
                Parameters.DotMatrixEmission.w)*(1.0-core);
            vec3 source=dotMatrixSample(uv,history);
            float level=filterDotMatrixBrightness(filterDotMatrixContrast(
                dot(source,vec3(.2126,.7152,.0722)),Parameters.DotMatrixEmission.y),
                Parameters.DotMatrixEmission.z);
            return mix(filterDotMatrixBackground(Parameters.DotMatrixGeometry.x),
                filterDotMatrixForeground(source,Parameters.DotMatrixGeometry.x),
                clamp(level*(core+halo),0.0,1.0));
        }
        #endif
        #if DISPLAY_TECHNOLOGY == 10
        """ + "\n" + ProjectionVideoShader.Create(true) + "\n" + """
        #endif
        """ + "\n" + CrtSignal.Shader + "\n" + CrtPhosphorMask.Shader + "\n" + """
        #if DISPLAY_TECHNOLOGY == 2 || DISPLAY_TECHNOLOGY == 10
        """ + "\n" + PanelSignalVideoShader.Create(true) + "\n" + """
        #endif

        vec3 displayEffect(vec3 c,vec2 uv){int t=int(Parameters.General.x+.5);vec2 p=floor(uv*Parameters.Output.xy),l=fract(uv*Parameters.Processing.zw);
        #if DISPLAY_TECHNOLOGY == 1
            if(t==1)
            {
                vec2 sourceUv = crtSignalUv(uv, Parameters.CrtRaster, Parameters.CrtTuning);
                if (any(lessThan(sourceUv, vec2(0.0))) || any(greaterThan(sourceUv, vec2(1.0)))) return vec3(0.0);
                c = raw(sourceUv);
                int decoder = int(Parameters.CrtTuning.y + .5);
                vec3 neighbor = decoder > 0 ? raw(crtChromaNeighbor(sourceUv, Parameters.Processing.zw, decoder)) : c;
                c = crtDecode(c, neighbor, decoder, floor(sourceUv.y * Parameters.Processing.w));
                c = min(vec3(1.0), c * (Parameters.Adjustments.x * crtBeam(uv.y, Parameters.Output.y, Parameters.CrtRaster, Parameters.Output.w, Parameters.CrtPhosphor.w)));
            }
        #endif
        #if DISPLAY_TECHNOLOGY == 2
            if(t==2)
            {
                int technology=int(Parameters.FixedDisplay.y+.5);

                if(technology<=1)
                {
                    vec2 panelSize=Parameters.FixedSpatial.yz;
                    c=filterLcdPixelStructure(c,uv,panelSize,Parameters.Output.xy,int(Parameters.FixedDisplay.z+.5));
                    if(technology==1)
                    {
                        int backlight=int(Parameters.FixedTechnology.y+.5);
                        float zone=backlight>=2?texture(sampler2D(Source,PointSampler),uv).a:1.0;
                        return filterLedBacklitLcdDisplay(c,uv,Parameters.FixedTechnology.x,
                            int(Parameters.FixedTechnology.w+.5),backlight,zone);
                    }
                    return filterLcdDisplay(c,gl_FragCoord.xy,Parameters.Output.xy,
                        Parameters.FixedTechnology.x,int(Parameters.FixedTechnology.w+.5));
                }
                float frameLight=texture(sampler2D(Source,PointSampler),.5/Parameters.Processing.zw).a;
                c=filterOledDisplay(c,int(Parameters.FixedTechnology.w+.5),frameLight);
                c=filterOledPixelStructure(c,uv,Parameters.FixedSpatial.yz,Parameters.Output.xy,int(Parameters.FixedDisplay.z+.5));
            }
        #endif
        #if DISPLAY_TECHNOLOGY == 3
            if(t==3)c=plasmaPixel(c,uv);
        #endif
        #if DISPLAY_TECHNOLOGY == 5
            if(t==5)c=vfdPixel(c,uv);
        #endif
        #if DISPLAY_TECHNOLOGY == 6
            if(t==6)c=ledMatrixPixel(uv);
        #endif
        #if DISPLAY_TECHNOLOGY == 7
            if(t==7)c=dotMatrixPixel(uv,false);
        #endif
        #if DISPLAY_TECHNOLOGY == 8
            if(t==8)c=segmentDisplayPixel(uv);
        #endif
        #if DISPLAY_TECHNOLOGY == 9
            if(t==9)c=ePaperPixel(uv);
        #endif
        #if DISPLAY_TECHNOLOGY == 10
            if(t==10)c=projectionPixel(uv);
        #endif
            return clamp(c,0.0,1.0);}
        """ + SignalConnectionRgbScart.Shader + SignalConnectionComponent.Shader + SignalConnectionSVideo.Shader + SignalConnectionComposite.Shader + SignalConnectionRf.Shader + SignalStandardPal.Shader + SignalStandardNtsc.Shader + SignalStandardSecam.Shader + FilterGrain.Shader + FilterVhs.Shader + FilterChromaticAberration.Shader + FilterBloom.Shader + FilterSepia.Shader + """
        vec3 signalEffects(vec3 color,vec2 uv)
        {
            if (Parameters.Signal.x < .5 && Parameters.Signal.w <= 0.0) return color;
            vec2 stepSize=1.0/max(Parameters.Processing.zw,vec2(1.0));
            vec3 left=raw(uv-vec2(stepSize.x,0.0)),right=raw(uv+vec2(stepSize.x,0.0));
            vec3 up=raw(uv-vec2(0.0,stepSize.y)),down=raw(uv+vec2(0.0,stepSize.y));
            int connection=int(Parameters.Signal.x+.5);float amount=Parameters.Signal.y;
            int standard=int(Parameters.Signal.z+.5);if(standard==0)standard=Parameters.General.w>18.2?1:2;
            float phase=mod(floor(uv.x*Parameters.Processing.z)+floor(uv.y*Parameters.Processing.w)+Parameters.General.z,2.0)*2.0-1.0;
            if(connection==1)color=signalConnectionRgbScart(color,left,amount);
            else if(connection==2)color=signalConnectionComponent(color,left,right,amount);
            else if(connection==3)color=signalConnectionSVideo(color,left,right,amount);
            else if(connection==4)color=signalConnectionComposite(color,left,right,amount,phase);
            else if(connection==5)color=signalConnectionRf(color,left,right,amount,hash(floor(uv*Parameters.Output.xy))-.5,float(standard),floor(uv.y*Parameters.Processing.w));
            if(standard==1)color=signalStandardPal(color,mod(floor(uv.y*Parameters.Processing.w),2.0)<.5?down:up,Parameters.Signal.w);
            else if(standard==2)color=signalStandardNtsc(color,left,Parameters.Signal.w);
            else if(standard==3)color=signalStandardSecam(color,up,Parameters.Signal.w,floor(uv.y*Parameters.Processing.w));
            return color;
        }
        vec3 postEffect(vec3 c,vec2 uv)
        {
            c=signalEffects(c,uv);vec2 s=1.0/max(Parameters.Output.xy,vec2(1));
            if(Parameters.Stylistic.y>0.0){float n=hash(floor(uv*Parameters.Output.xy)+Parameters.General.z)-.5;float w=(sin(uv.y*Parameters.Processing.w*.071+Parameters.General.z*.31)*4.0+n*4.0)*Parameters.Stylistic.y;vec2 q=uv+vec2(w*s.x,0);c=filterVhs(c,raw(q),raw(q-vec2(s.x*2.0,0)),raw(q+vec2(s.x*2.0,0)),Parameters.Stylistic.y,n,floor(uv.y*Parameters.Processing.w),uv.y);}
            if(Parameters.Stylistic.z>0.0){float o=Parameters.Stylistic.z*s.x*7.0;c=filterChromaticAberration(raw(uv+vec2(o,0)),c,raw(uv-vec2(o,0)));}
            if(Parameters.Stylistic.w>0.0)c=filterBloom(c,raw(uv+vec2(s.x*2.0,0)),raw(uv-vec2(s.x*2.0,0)),raw(uv+vec2(0,s.y*2.0)),raw(uv-vec2(0,s.y*2.0)),raw(uv+vec2(s.x*5.0,0)),raw(uv-vec2(s.x*5.0,0)),raw(uv+vec2(0,s.y*5.0)),raw(uv-vec2(0,s.y*5.0)),Parameters.Stylistic.w);
            c=filterSepia(c,Parameters.Stylistic2.x);
            c=filterGrain(c,Parameters.Stylistic.x,hash(floor(uv*Parameters.Output.xy)+Parameters.General.z)*2.0-1.0);
            return clamp(c,0.0,1.0);
        }
        #if DISPLAY_TECHNOLOGY == 1
        vec3 crtImageAt(vec2 uv)
        {
            vec2 sourceUv = crtSignalUv(uv, Parameters.CrtRaster, Parameters.CrtTuning);
            if (any(lessThan(sourceUv, vec2(0.0))) || any(greaterThan(sourceUv, vec2(1.0)))) return vec3(0.0);
            vec3 color = adjustLinear(restorationPoint(sourceUv));
            if (Parameters.CrtTuning.y < .5) color = vec3(dot(color, vec3(.2126,.7152,.0722)));
            return min(vec3(1.0), color * (Parameters.Adjustments.x * crtBeam(uv.y, Parameters.Output.y, Parameters.CrtRaster, Parameters.Output.w, Parameters.CrtPhosphor.w)));
        }
        #endif
        #if DISPLAY_TECHNOLOGY == 4
        """ + "\n" + VectorRasterVideoShader.Create(true) + "\n" + """
        #endif
        void main()
        {
            vec2 uv = fsin_TexCoord;
        #if DISPLAY_TECHNOLOGY == 4
            uv = crtScreenUv(uv, Parameters.VectorBeam.z);
            vec3 color = crtOnScreen(uv) ? postEffect(vectorPixel(uv), uv) : vec3(0.0);
        #ifdef OUTPUT_ENCODES_SRGB
            fsout_Color = vec4(color, 1.0);
        #else
            fsout_Color = vec4(linearToSrgb(color.r), linearToSrgb(color.g), linearToSrgb(color.b), 1.0);
        #endif
        #else
        #if DISPLAY_TECHNOLOGY == 1
            if (Parameters.CrtGeometry.x > 0.0)
            {
                uv = crtScreenUv(uv, Parameters.CrtGeometry.x);
                if (!crtOnScreen(uv))
                {
                    fsout_Color = vec4(0.0, 0.0, 0.0, 1.0);
                    return;
                }
            }
        #endif
            vec2 sampleUv=uv;
        #if DISPLAY_TECHNOLOGY == 2
            bool fixedPanel=Parameters.FixedDisplay.x>.5;
            if(fixedPanel)sampleUv=lcdPanelSampleUv(uv,Parameters.FixedSpatial.yz,Parameters.Output.xy);
        #elif DISPLAY_TECHNOLOGY == 3
            sampleUv=lcdPanelSampleUv(uv,Parameters.FixedSpatial.yz,Parameters.Output.xy);
        #endif
            vec3 c=displayEffect(restored(sampleUv),uv);
            c=clamp(postEffect(c,uv),0.0,1.0);
            vec2 size=vec2(textureSize(sampler2D(History,LinearSampler),0));
            vec2 historyUv=clamp(sampleUv,.5/size,1.0-.5/size);
            vec3 historyColor;
        #if DISPLAY_TECHNOLOGY == 2
            if(fixedPanel)historyColor=texture(sampler2D(History,PointSampler),historyUv).rgb;
            else
        #endif
        #if DISPLAY_TECHNOLOGY == 3
                historyColor=texture(sampler2D(History,PointSampler),historyUv).rgb;
        #else
                historyColor=texture(sampler2D(History,LinearSampler),historyUv).rgb;
        #endif
        #if DISPLAY_TECHNOLOGY == 2
            if (Parameters.CrtGeometry.y > .5 && Parameters.FixedTemporal.z > .5)
                historyColor = panelDecodedColor(historyUv, true);
        #endif
            vec3 previous=displayEffect(adjust(historyColor),uv);
            previous=clamp(postEffect(previous,uv),0.0,1.0);
        #if DISPLAY_TECHNOLOGY == 7
            if(Parameters.DotMatrixTemporal.z>.5)
            {
                previous=clamp(postEffect(dotMatrixPixel(fsin_TexCoord,true),fsin_TexCoord),0.0,1.0);
                c=filterDotMatrixResponse(previous,c,Parameters.DotMatrixTemporal.x);
                c=filterDotMatrixPersistence(c,previous,Parameters.DotMatrixTemporal.y,
                    Parameters.DotMatrixGeometry.x,
                    filterDotMatrixBackground(Parameters.DotMatrixGeometry.x));
            }
        #endif
        #if DISPLAY_TECHNOLOGY == 3
            if(Parameters.PlasmaTemporal.y>.5)
                c=filterPlasmaPersistence(c,previous,Parameters.PlasmaTemporal.x);
        #endif
        #if DISPLAY_TECHNOLOGY == 5
            if(Parameters.VfdOptical.w>.5)
                c=filterVfdPersistence(c,previous,Parameters.VfdOptical.y,
                    Parameters.VfdOptical.z);
        #endif
        #if DISPLAY_TECHNOLOGY == 2
            if(Parameters.FixedDisplay.x>.5&&Parameters.FixedTemporal.z>.5)
            {
                float response=Parameters.FixedTemporal.x;
                if(Parameters.FixedDisplay.y<1.5)
                {
                    response=lcdResponseTime(response,int(Parameters.FixedTechnology.w+.5),
                        max(previous.r,max(previous.g,previous.b)),max(c.r,max(c.g,c.b)));
                    if(Parameters.FixedTemporal.w<=0.0)response=0.0;
                }
                c=filterFixedPixelResponse(previous,c,response,Parameters.FixedTemporal.w);
                c=filterFixedPixelPersistence(c,previous,Parameters.FixedTemporal.y);
            }
        #endif
        #if DISPLAY_TECHNOLOGY == 1
            c = crtCompose(c, uv, vec2(Parameters.CrtTuning.w, Parameters.CrtRaster.y * 2.0),
                Parameters.CrtPhosphor, Parameters.Output.w, Parameters.Adjustments.x,
                Parameters.Output.xy, fsin_TexCoord, Parameters.CrtGeometry.x);
        #endif
        #ifdef OUTPUT_ENCODES_SRGB
            fsout_Color=vec4(c,1.0);
        #else
            fsout_Color=vec4(linearToSrgb(c.r),linearToSrgb(c.g),linearToSrgb(c.b),1.0);
        #endif
        #endif
        }
        """;
}
