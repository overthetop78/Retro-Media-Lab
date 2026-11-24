using RetroMediaLab.App.Rendering.Emulation.Processing;

namespace RetroMediaLab.App.Rendering.Emulation.Processing.OpenGl.Shaders;
internal static class OpenGlVideoShaderSource
{
    internal const string VertexSource = """
        #version 120
        varying vec2 TextureCoordinate;
        void main()
        {
            gl_Position = ftransform();
            TextureCoordinate = gl_MultiTexCoord0.xy;
        }
        """;
    private static readonly string FragmentTemplate = """
        #version 120
        uniform sampler2D Source;
        uniform sampler2D History;
        uniform vec4 Adjustments;
        uniform vec4 Processing;
        uniform vec4 Output;
        uniform vec4 CrtRaster;
        uniform vec4 CrtTuning;
        uniform vec4 CrtPhosphor;
        uniform vec4 CrtGeometry;
        uniform vec4 VectorPhosphor;
        uniform vec4 VectorBeam;
        uniform vec4 FixedDisplay;
        uniform vec4 FixedSpatial;
        uniform vec4 FixedTechnology;
        uniform vec4 FixedTemporal;
        uniform vec4 PlasmaEffect;
        uniform vec4 PlasmaTemporal;
        uniform vec4 PlasmaDisplay;
        uniform vec4 SegmentGeometry;
        uniform vec4 SegmentShape;
        uniform vec4 SegmentEmission;
        uniform vec4 SegmentOptical;
        uniform vec4 SegmentTemporal;
        uniform vec4 General;
        uniform vec4 Restoration;
        uniform vec4 Signal;
        uniform vec4 Stylistic;
        uniform vec4 Stylistic2;
        uniform vec4 VfdDisplay;
        uniform vec4 VfdStructure;
        uniform vec4 VfdOptical;
        uniform vec4 LedMatrixStructure;
        uniform vec4 DotMatrixGeometry;
        uniform vec4 DotMatrixEmission;
        uniform vec4 DotMatrixTemporal;
        uniform vec4 EPaperInkAndColor;
        uniform vec4 EPaperSurface;
        uniform vec4 EPaperTemporal;
        uniform vec4 Projection;
        uniform vec4 ProjectionScreen;
        varying vec2 TextureCoordinate;

        """ + VideoBrightnessParameterFunctions.Shader + VideoContrastParameterFunctions.Shader + VideoGammaParameterFunctions.Shader + VideoSaturationParameterFunctions.Shader + VideoSharpnessParameterFunctions.Shader + FilterBicubic.OpenGlShader + FilterNormal.OpenGlShader + FilterBilinear.OpenGlShader + FilterSharpBilinear.OpenGlShader + FilterJinc2.OpenGlShader + FilterLanczos.OpenGlShader + FilterXbr.OpenGlShader + FilterHqx.OpenGlShader + FilterXbrz.OpenGlShader + FilterHq2x.OpenGlShader + FilterHq3x.OpenGlShader + FilterHq4x.OpenGlShader + FilterTwoXSai.OpenGlShader + FilterSuperTwoXSai.OpenGlShader + FilterSuperEagle.OpenGlShader + FilterEpxScale2x.OpenGlShader + FilterScaleFx.OpenGlShader + FilterScaleNx.OpenGlShader + FilterSabr.OpenGlShader + "\n" + """
        #if DISPLAY_TECHNOLOGY == 2 || DISPLAY_TECHNOLOGY == 10
        vec3 panelDecodedColor(vec2 uv, bool history);
        #endif
        vec4 sampleConfigured(vec2 uv)
        {
        #if DISPLAY_TECHNOLOGY == 2 || DISPLAY_TECHNOLOGY == 10
            if (CrtGeometry.y > .5) return vec4(panelDecodedColor(uv, false), 1.0);
        #endif
            int sampling = int(Processing.y + 0.5);
            if (sampling == 0) return pointSample(uv);
            if (sampling == 1) return linearSample(uv);
            if (sampling == 2) return sharpBilinearSample(uv);
            if (sampling == 3) return bicubicSample(uv);
            if (sampling == 4) return xbrSample(uv);
            if (sampling == 5) return xbrzSample(uv);
            if (sampling == 6) return hqxSample(uv);
            if (sampling == 7) return scaleFxSample(uv);
            if (sampling == 8) return scaleNxSample(uv);
            if (sampling == 9) return sabrSample(uv);
            if (sampling == 10) return hq2xSample(uv);
            if (sampling == 11) return hq3xSample(uv);
            if (sampling == 12) return hq4xSample(uv);
            if (sampling == 13) return twoXSaiSample(uv);
            if (sampling == 14) return superTwoXSaiSample(uv);
            if (sampling == 15) return superEagleSample(uv);
            if (sampling == 16) return epxScale2xSample(uv);
            if (sampling == 17) return jinc2Sample(uv);
            if (sampling == 18) return lanczosSample(uv);
            return pointSample(uv);
        }

        float srgbToLinear(float value)
        {
            value = clamp(value, 0.0, 1.0);
            return value <= 0.04045 ? value / 12.92
                : pow((value + 0.055) / 1.055, 2.4);
        }

        float linearToSrgb(float value)
        {
            value = clamp(value, 0.0, 1.0);
            return value <= 0.0031308 ? value * 12.92
                : 1.055 * pow(value, 1.0 / 2.4) - 0.055;
        }

        vec3 restorationLinear(vec3 color)
        {
            return vec3(srgbToLinear(color.r), srgbToLinear(color.g), srgbToLinear(color.b));
        }
        vec3 restorationPoint(vec2 uv) { return restorationLinear(pointSample(uv).rgb); }
        """ + VideoImageRestorationShader.Source + """
        vec3 adjustLinear(vec3 linear)
        {
            linear = videoContrastParameter(linear, Adjustments.y);
        #if DISPLAY_TECHNOLOGY != 1 && DISPLAY_TECHNOLOGY != 4
            linear = videoBrightnessParameter(linear, Adjustments.x);
        #endif
            linear = videoGammaParameter(linear, Adjustments.z);
            return videoSaturationParameter(linear, Adjustments.w);
        }
        vec3 adjustColor(vec3 color) { return adjustLinear(restorationLinear(color)); }

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
        """ + "\n" + FilterSegmentDisplay.OpenGlShader + "\n" + """
        #endif
        #if DISPLAY_TECHNOLOGY == 9
        """ + "\n" + FilterEPaper.OpenGlShader + "\n" + """
        #endif
        #if DISPLAY_TECHNOLOGY == 2
        vec3 fixedPixel(vec3 color, vec2 uv)
        {
            int technology=int(FixedDisplay.y+.5);

            if(technology<=1)
            {
                vec2 panelSize=FixedSpatial.yz;
                color=filterLcdPixelStructure(color,uv,panelSize,Output.xy,int(FixedDisplay.z+.5));
                if(technology==1)
                {
                    int backlight=int(FixedTechnology.y+.5);
                    float zone=backlight>=2?texture2D(Source,uv).a:1.0;
                    return filterLedBacklitLcdDisplay(color,uv,FixedTechnology.x,int(FixedTechnology.w+.5),backlight,zone);
                }
                return filterLcdDisplay(color,vec2(gl_FragCoord.x,Output.y-gl_FragCoord.y),Output.xy,
                    FixedTechnology.x,int(FixedTechnology.w+.5));
            }
            float frameLight=texture2D(Source,.5/Processing.zw).a;
            color=filterOledDisplay(color,int(FixedTechnology.w+.5),frameLight);
            return filterOledPixelStructure(color,uv,FixedSpatial.yz,Output.xy,int(FixedDisplay.z+.5));
        }

        vec3 fixedPixelWithHistory(vec3 color, vec2 uv)
        {
            color=fixedPixel(color,uv);
            if(FixedTemporal.z<.5)return color;
            vec2 historyUv=uv;
            if(FixedDisplay.y<1.5)
                historyUv=(floor(lcdPanelSampleUv(uv,FixedSpatial.yz,Output.xy)*Processing.zw)+.5)/Processing.zw;
            vec3 previous=texture2D(History,
                clamp(historyUv,.5/Processing.zw,1.0-.5/Processing.zw)).rgb;
            if (CrtGeometry.y > .5) previous = panelDecodedColor(historyUv, true);
            previous=fixedPixel(adjustColor(previous),uv);
            float response=FixedTemporal.x;
            if(FixedDisplay.y<1.5)
            {
                if(FixedTemporal.w<=0.0)return color;
                response=lcdResponseTime(response,int(FixedTechnology.w+.5),
                    max(previous.r,max(previous.g,previous.b)),max(color.r,max(color.g,color.b)));
            }
            color=filterFixedPixelResponse(previous,color,response,FixedTemporal.w);
            return filterFixedPixelPersistence(color,previous,FixedTemporal.y);
        }
        #endif
        #if DISPLAY_TECHNOLOGY == 3
        vec3 plasmaEmission(vec3 color,vec2 uv)
        {
            color=filterPlasmaPhosphorIntensity(color,PlasmaDisplay.y);
            color=filterPlasmaAutomaticBrightnessLimiter(color,PlasmaDisplay.w,PlasmaTemporal.w);
            vec2 cell=floor(lcdPanelSampleUv(uv,FixedSpatial.yz,Output.xy)*FixedSpatial.yz);
            color=filterPlasmaTemporalDithering(color,cell,PlasmaEffect.w,PlasmaTemporal.z);
            return filterPlasmaCellStructure(filterPlasmaBlackDepth(color),uv,
                FixedSpatial.yz,Output.xy,int(PlasmaEffect.y+.5));
        }

        vec3 plasmaPixel(vec3 color,vec2 uv,vec2 sampleUv)
        {
            color=plasmaEmission(color,uv);
            if (PlasmaTemporal.y > 0.5)
            {
                vec2 historyUv=(floor(sampleUv*Processing.zw)+.5)/Processing.zw;
                vec3 previous=texture2D(History,
                    clamp(historyUv,.5/Processing.zw,1.0-.5/Processing.zw)).rgb;
                color=filterPlasmaPersistence(color,plasmaEmission(adjustColor(previous),uv),PlasmaTemporal.x);
            }
            return clamp(color,0.0,1.0);
        }
        #endif
        """ + "\n" + CrtSignal.Shader + "\n" + CrtPhosphorMask.Shader + "\n" + """
        #if DISPLAY_TECHNOLOGY == 2 || DISPLAY_TECHNOLOGY == 10
        """ + "\n" + PanelSignalVideoShader.Create(false) + "\n" + """
        #endif

        vec3 crtPixel(vec2 originalUv)
        {
            if (DISPLAY_TECHNOLOGY != 1)
            {
                vec2 sampleUv = originalUv;
        #if DISPLAY_TECHNOLOGY == 2
                if (FixedDisplay.x > .5)
                    sampleUv = lcdPanelSampleUv(originalUv, FixedSpatial.yz, Output.xy);
        #elif DISPLAY_TECHNOLOGY == 3
                sampleUv = lcdPanelSampleUv(originalUv, FixedSpatial.yz, Output.xy);
        #endif
                vec3 source = restorationLinear(sampleConfigured(sampleUv).rgb);
        #if DISPLAY_TECHNOLOGY == 0
                float recover = Processing.y > .5 && any(notEqual(Output.xy, Processing.zw)) ? Stylistic2.z : 0.0;
                source = restoreHostSource(source, originalUv, Processing.zw, Restoration.z, recover);
        #endif
                vec3 color = adjustLinear(source);
        #if DISPLAY_TECHNOLOGY == 2
                if (FixedDisplay.x > 0.5) return fixedPixelWithHistory(color, originalUv);
        #endif
        #if DISPLAY_TECHNOLOGY == 3
                if (PlasmaEffect.x > 0.5) return plasmaPixel(color,originalUv,sampleUv);
        #endif
                return color;
            }
            vec2 uv = crtSignalUv(originalUv, CrtRaster, CrtTuning);
            if (any(lessThan(uv, vec2(0.0))) || any(greaterThan(uv, vec2(1.0)))) return vec3(0.0);
            vec3 color = adjustColor(sampleConfigured(uv).rgb);
            int decoder = int(CrtTuning.y + .5);
            vec3 neighbor = color;
            if (decoder > 0) neighbor = adjustColor(sampleConfigured(crtChromaNeighbor(uv, Processing.zw, decoder)).rgb);
            color = crtDecode(color, neighbor, decoder, floor(uv.y * Processing.w));
            return min(vec3(1.0), color * (Adjustments.x * crtBeam(originalUv.y, Output.y, CrtRaster, Output.w, CrtPhosphor.w)));
        }
        float extraHash(vec2 p){return fract(sin(dot(p,vec2(12.9898,78.233))+General.z)*43758.5453);}
        vec3 extraRaw(vec2 uv){return adjustColor(sampleConfigured(uv).rgb);}
        vec3 restoreColor(vec3 color,vec2 uv)
        {
            if(abs(Processing.x)>0.0001)
            {
                vec2 stepSize=1.0/max(Output.xy,vec2(1.0));
                vec3 sum=color;
                for(int y=-1;y<=1;y++) for(int x=-1;x<=1;x++)
                    if(x!=0||y!=0)sum+=extraRaw(uv+vec2(float(x),float(y))*stepSize);
                float weight=9.0;
                if(Processing.x<0.0)
                {
                    vec2 dx=vec2(stepSize.x*2.0,0.0),dy=vec2(0.0,stepSize.y*2.0);
                    sum+=(extraRaw(uv-dx)+extraRaw(uv+dx)+extraRaw(uv-dy)+extraRaw(uv+dy))*.5;
                    weight=11.0;
                }
                color=videoSharpnessParameter(color,sum/weight,Processing.x);
            }
            return clamp(color,0.0,1.0);
        }
        #if DISPLAY_TECHNOLOGY == 5
        float vfdEmission(vec2 uv)
        {
            vec3 sampleColor=adjustColor(sampleConfigured(clamp(uv,vec2(0.0),vec2(1.0))).rgb);
            return filterVfdEmissionThreshold(dot(sampleColor,vec3(.2126,.7152,.0722)),VfdDisplay.z);
        }
        vec3 vfdPixel(vec3 source,vec2 uv)
        {
            float mask=filterVfdCellStructure(uv,Processing.zw,VfdStructure.x,
                VfdStructure.y,VfdStructure.z);
            float emission=filterVfdPhosphorIntensity(
                filterVfdEmissionThreshold(dot(source,vec3(.2126,.7152,.0722)),VfdDisplay.z)*mask,
                VfdDisplay.y);
            vec2 stepSize=1.0/max(Processing.zw,vec2(1.0));
            float radius=filterVfdHaloRadius(VfdOptical.x);
            float nearRadius=max(1.0,radius*.5);
            float nearEmission=(vfdEmission(uv+vec2(stepSize.x*nearRadius,0.0))
                +vfdEmission(uv-vec2(stepSize.x*nearRadius,0.0))
                +vfdEmission(uv+vec2(0.0,stepSize.y*nearRadius))
                +vfdEmission(uv-vec2(0.0,stepSize.y*nearRadius)))*.25;
            float farEmission=(vfdEmission(uv+vec2(stepSize.x*radius,stepSize.y*radius))
                +vfdEmission(uv+vec2(-stepSize.x*radius,stepSize.y*radius))
                +vfdEmission(uv+vec2(stepSize.x*radius,-stepSize.y*radius))
                +vfdEmission(uv-vec2(stepSize.x*radius,stepSize.y*radius)))*.25;
            float halo=filterVfdPhosphorIntensity(
                filterVfdHalo(nearEmission,farEmission,VfdStructure.w),VfdDisplay.y);
            return clamp(filterVfdGlass(source,VfdDisplay.w)
                +filterVfdPhosphorColor(emission,VfdDisplay.x)
                +filterVfdPhosphorColor(halo,VfdDisplay.x),0.0,1.0);
        }
        #endif
        #if DISPLAY_TECHNOLOGY == 6
        """ + "\n" + FilterLedMatrixCellStructure.CreateShader(false) + "\n" + """
        #endif
        #if DISPLAY_TECHNOLOGY == 7
        vec3 dotMatrixSample(vec2 uv,bool history)
        {
            vec2 sourceSize=max(Processing.zw,vec2(1.0));
            float pitch=filterDotMatrixPitch(DotMatrixGeometry.z);
            vec2 cell=floor(uv*sourceSize/pitch);
            vec2 centerUv=(cell+.5)*pitch/sourceSize;
            vec2 offset=vec2(pitch*.28)/sourceSize;
            if(history)
                return (adjustColor(texture2D(History,centerUv).rgb)*4.0
                    +adjustColor(texture2D(History,centerUv-vec2(offset.x,0.0)).rgb)
                    +adjustColor(texture2D(History,centerUv+vec2(offset.x,0.0)).rgb)
                    +adjustColor(texture2D(History,centerUv-vec2(0.0,offset.y)).rgb)
                    +adjustColor(texture2D(History,centerUv+vec2(0.0,offset.y)).rgb))/8.0;
            return (adjustColor(sampleConfigured(centerUv).rgb)*4.0
                +adjustColor(sampleConfigured(centerUv-vec2(offset.x,0.0)).rgb)
                +adjustColor(sampleConfigured(centerUv+vec2(offset.x,0.0)).rgb)
                +adjustColor(sampleConfigured(centerUv-vec2(0.0,offset.y)).rgb)
                +adjustColor(sampleConfigured(centerUv+vec2(0.0,offset.y)).rgb))/8.0;
        }
        vec3 dotMatrixPixel(vec2 uv,bool history)
        {
            vec2 sourceSize=max(Processing.zw,vec2(1.0));
            float pitch=filterDotMatrixPitch(DotMatrixGeometry.z);
            vec2 local=fract(uv*sourceSize/pitch)-.5;
            float distance=filterDotMatrixDistance(local,DotMatrixGeometry.y);
            float radius=filterDotMatrixRadius(DotMatrixGeometry.w,DotMatrixEmission.x);
            float edge=max(sourceSize.x/max(Output.x,1.0),sourceSize.y/max(Output.y,1.0))/pitch;
            float core=smoothstep(radius+edge,radius-edge,distance);
            float halo=filterDotMatrixHalo(distance,radius,DotMatrixEmission.w)*(1.0-core);
            vec3 source=dotMatrixSample(uv,history);
            float level=filterDotMatrixBrightness(filterDotMatrixContrast(
                dot(source,vec3(.2126,.7152,.0722)),DotMatrixEmission.y),DotMatrixEmission.z);
            return mix(filterDotMatrixBackground(DotMatrixGeometry.x),
                filterDotMatrixForeground(source,DotMatrixGeometry.x),
                clamp(level*(core+halo),0.0,1.0));
        }
        #endif
        #if DISPLAY_TECHNOLOGY == 10
        """ + "\n" + ProjectionVideoShader.Create(false) + "\n" + """
        #endif
        vec3 extraDisplay(vec3 color,vec2 uv){int t=int(General.x+.5);vec2 p=floor(uv*Output.xy);
        #if DISPLAY_TECHNOLOGY == 5
            if(t==5)color=vfdPixel(color,uv);
        #endif
        #if DISPLAY_TECHNOLOGY == 6
            if(t==6)color=ledMatrixPixel(uv);
        #endif
        #if DISPLAY_TECHNOLOGY == 7
            if(t==7)color=dotMatrixPixel(uv,false);
        #endif
        #if DISPLAY_TECHNOLOGY == 8
            if(t==8)color=segmentDisplayPixel(uv);
        #endif
        #if DISPLAY_TECHNOLOGY == 9
            if(t==9)color=ePaperPixel(uv);
        #endif
        #if DISPLAY_TECHNOLOGY == 10
            if(t==10)color=projectionPixel(uv);
        #endif
            return clamp(color,0.0,1.0);}
        """ + SignalConnectionRgbScart.Shader + SignalConnectionComponent.Shader + SignalConnectionSVideo.Shader + SignalConnectionComposite.Shader + SignalConnectionRf.Shader + SignalStandardPal.Shader + SignalStandardNtsc.Shader + SignalStandardSecam.Shader + FilterGrain.Shader + FilterVhs.Shader + FilterChromaticAberration.Shader + FilterBloom.Shader + FilterSepia.Shader + """
        vec3 signalEffects(vec3 color,vec2 uv)
        {
            if (Signal.x < .5 && Signal.w <= 0.0) return color;
            vec2 stepSize=1.0/max(Processing.zw,vec2(1.0));
            vec3 left=extraRaw(uv-vec2(stepSize.x,0.0)),right=extraRaw(uv+vec2(stepSize.x,0.0));
            vec3 up=extraRaw(uv-vec2(0.0,stepSize.y)),down=extraRaw(uv+vec2(0.0,stepSize.y));
            int connection=int(Signal.x+.5);float amount=Signal.y;
            int standard=int(Signal.z+.5);if(standard==0)standard=General.w>18.2?1:2;
            float phase=mod(floor(uv.x*Processing.z)+floor(uv.y*Processing.w)+General.z,2.0)*2.0-1.0;
            if(connection==1)color=signalConnectionRgbScart(color,left,amount);
            else if(connection==2)color=signalConnectionComponent(color,left,right,amount);
            else if(connection==3)color=signalConnectionSVideo(color,left,right,amount);
            else if(connection==4)color=signalConnectionComposite(color,left,right,amount,phase);
            else if(connection==5)color=signalConnectionRf(color,left,right,amount,extraHash(floor(uv*Output.xy))-.5,float(standard),floor(uv.y*Processing.w));
            if(standard==1)color=signalStandardPal(color,mod(floor(uv.y*Processing.w),2.0)<.5?down:up,Signal.w);
            else if(standard==2)color=signalStandardNtsc(color,left,Signal.w);
            else if(standard==3)color=signalStandardSecam(color,up,Signal.w,floor(uv.y*Processing.w));
            return color;
        }
        vec3 postColor(vec3 color,vec2 uv)
        {
            color=signalEffects(color,uv);vec2 s=1.0/max(Output.xy,vec2(1.0));
            if(Stylistic.y>0.0){float n=extraHash(floor(uv*Output.xy)+General.z)-.5;float w=(sin(uv.y*Processing.w*.071+General.z*.31)*4.0+n*4.0)*Stylistic.y;vec2 q=uv+vec2(w*s.x,0.0);color=filterVhs(color,extraRaw(q),extraRaw(q-vec2(s.x*2.0,0.0)),extraRaw(q+vec2(s.x*2.0,0.0)),Stylistic.y,n,floor(uv.y*Processing.w),uv.y);}
            if(Stylistic.z>0.0){float o=Stylistic.z*s.x*7.0;color=filterChromaticAberration(extraRaw(uv+vec2(o,0.0)),color,extraRaw(uv-vec2(o,0.0)));}
            if(Stylistic.w>0.0)color=filterBloom(color,extraRaw(uv+vec2(s.x*2.0,0.0)),extraRaw(uv-vec2(s.x*2.0,0.0)),extraRaw(uv+vec2(0.0,s.y*2.0)),extraRaw(uv-vec2(0.0,s.y*2.0)),extraRaw(uv+vec2(s.x*5.0,0.0)),extraRaw(uv-vec2(s.x*5.0,0.0)),extraRaw(uv+vec2(0.0,s.y*5.0)),extraRaw(uv-vec2(0.0,s.y*5.0)),Stylistic.w);
            color=filterSepia(color,Stylistic2.x);
            color=filterGrain(color,Stylistic.x,extraHash(floor(uv*Output.xy)+General.z)*2.0-1.0);
            return clamp(color,0.0,1.0);
        }
        #if DISPLAY_TECHNOLOGY == 1
        vec3 crtImageAt(vec2 uv)
        {
            vec2 sourceUv = crtSignalUv(uv, CrtRaster, CrtTuning);
            if (any(lessThan(sourceUv, vec2(0.0))) || any(greaterThan(sourceUv, vec2(1.0)))) return vec3(0.0);
            vec3 color = adjustLinear(restorationPoint(sourceUv));
            if (CrtTuning.y < .5) color = vec3(dot(color, vec3(.2126,.7152,.0722)));
            return min(vec3(1.0), color * (Adjustments.x * crtBeam(uv.y, Output.y, CrtRaster, Output.w, CrtPhosphor.w)));
        }
        #endif
        #if DISPLAY_TECHNOLOGY == 4
        """ + "\n" + VectorRasterVideoShader.Create(false) + "\n" + """
        #endif
        void main()
        {
            vec2 uv = TextureCoordinate;
        #if DISPLAY_TECHNOLOGY == 4
            uv = crtScreenUv(uv, VectorBeam.z);
            vec3 color = crtOnScreen(uv) ? postColor(vectorPixel(uv), uv) : vec3(0.0);
        #ifdef OUTPUT_ENCODES_SRGB
            gl_FragColor = vec4(color, 1.0);
        #else
            gl_FragColor = vec4(linearToSrgb(color.r), linearToSrgb(color.g), linearToSrgb(color.b), 1.0);
        #endif
        #else
        #if DISPLAY_TECHNOLOGY == 1
            if (CrtGeometry.x > 0.0)
            {
                uv = crtScreenUv(uv, CrtGeometry.x);
                if (!crtOnScreen(uv))
                {
                    gl_FragColor = vec4(0.0, 0.0, 0.0, 1.0);
                    return;
                }
            }
        #endif
            vec3 center=extraDisplay(restoreColor(crtPixel(uv),uv),uv);
            center=postColor(center,uv);
            vec2 historyUv=clamp(uv,.5/Processing.zw,1.0-.5/Processing.zw);
            vec3 previous=extraDisplay(adjustColor(texture2D(History,historyUv).rgb),uv);
            previous=postColor(previous,uv);
        #if DISPLAY_TECHNOLOGY == 7
            if(DotMatrixTemporal.z>.5)
            {
                previous=postColor(dotMatrixPixel(TextureCoordinate,true),TextureCoordinate);
                center=filterDotMatrixResponse(previous,center,DotMatrixTemporal.x);
                center=filterDotMatrixPersistence(center,previous,DotMatrixTemporal.y,
                    DotMatrixGeometry.x,filterDotMatrixBackground(DotMatrixGeometry.x));
            }
        #endif
        #if DISPLAY_TECHNOLOGY == 5
            if(VfdOptical.w>.5)
                center=filterVfdPersistence(center,previous,VfdOptical.y,VfdOptical.z);
        #endif
        #if DISPLAY_TECHNOLOGY == 1
            center = crtCompose(center, uv, vec2(CrtTuning.w, CrtRaster.y * 2.0),
                CrtPhosphor, Output.w, Adjustments.x, Output.xy, TextureCoordinate, CrtGeometry.x);
        #endif
            gl_FragColor=vec4(linearToSrgb(center.r),linearToSrgb(center.g),linearToSrgb(center.b),1.0);
        #endif
        }
        """;
    internal static string Fragment(EmulationVideoSampling sampling, EmulationVideoDisplayTechnology displayTechnology) => FragmentTemplate.Replace("#version 120", $"#version 120\n#define DISPLAY_TECHNOLOGY {(int)displayTechnology}");
}
